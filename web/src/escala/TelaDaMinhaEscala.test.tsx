import { screen, waitFor, within } from '@testing-library/react';
import { userEvent } from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { afterEach, beforeEach, describe, expect, test, vi } from 'vitest';
import type { Esquemas } from '../api/cliente';
import { abrir } from '../testes/abrir';
import { problemasDeAcessibilidade } from '../testes/acessibilidade';
import {
  doAntonio,
  doAntonioQueTrabalhou,
  doCarlos,
  doCarlosSubstituto,
  doEdson,
  doEdsonQueDesistiu,
  Ids,
} from '../testes/exemplos';
import { servidor } from '../testes/servidor';
import { tokenDeTeste } from '../testes/tokens';
import { instalarVoz } from '../testes/voz';

// Quinta, 05/11/2026, às 10h05 no porto: a escala da tarde acabou de sair.
const QuintaAs10h05 = new Date('2026-11-05T10:05:00-03:00');

beforeEach(() => {
  vi.useFakeTimers({ now: QuintaAs10h05, toFake: ['Date'] });
});

afterEach(() => {
  vi.useRealTimers();
});

function responder(escalas: Esquemas['MinhaEscalaResposta'][]) {
  servidor.use(http.get('/api/eu/escalas', () => HttpResponse.json(escalas)));
}

const cartao = (nome: RegExp) => screen.findByRole('article', { name: nome });

describe('minha escala', () => {
  test('quem vai trabalhar vê o dia, o horário e tudo o que precisa para chegar', async () => {
    responder([doAntonio]);
    abrir('/');

    const tarde = await cartao(/^Hoje, quinta-feira, 5 de novembro/);

    expect(tarde).toHaveAccessibleName('Hoje, quinta-feira, 5 de novembro, Tarde, das 13h às 19h');
    expect(within(tarde).getByText('Você vai trabalhar')).toBeInTheDocument();
    expect(
      within(tarde)
        .getAllByRole('definition')
        .map((dado) => dado.textContent),
    ).toEqual([
      'Sinaleiro, no terno 1',
      'Atlântico Sul',
      'Terminal Cais Leste, berço 4',
      'Cais Forte Operações Portuárias',
    ]);
    expect(screen.getByText('Última atualização: hoje, às 10h05.')).toBeInTheDocument();
    expect(await problemasDeAcessibilidade()).toEqual([]);
  });

  test('quem ficou de fora vê por quê, e o lugar na espera', async () => {
    responder([doCarlos]);
    abrir('/', { token: tokenDeTeste({ name: 'Carlos Mota', matricula: '4152' }) });

    const tarde = await cartao(/^Hoje/);

    expect(within(tarde).getByText('Você ficou de fora')).toBeInTheDocument();
    expect(
      within(tarde)
        .getAllByRole('listitem')
        .map((motivo) => motivo.textContent),
    ).toEqual([
      'Como sinaleiro, o rodízio parou 1 posição antes da sua vez.',
      'Como estivador, o rodízio parou 1 posição antes da sua vez.',
    ]);
    expect(tarde).toHaveTextContent(
      'Você é o 1º da espera de sinaleiro e o 1º da espera de estivador. Se alguém desistir, a vaga pode ser sua.',
    );
    expect(within(tarde).queryByRole('button', { name: 'Desistir da vaga' })).not.toBeInTheDocument();
    expect(await problemasDeAcessibilidade()).toEqual([]);
  });

  test('o substituto, quem desistiu e quem já trabalhou, cada um com a sua frase', async () => {
    responder([
      { ...doCarlosSubstituto, escala: Ids.daTarde },
      { ...doEdsonQueDesistiu, escala: Ids.daNoite },
      { ...doAntonioQueTrabalhou, escala: '01a16700-0000-7000-8000-00000000e003' },
    ]);
    abrir('/');

    const [substituto, desistiu, trabalhou] = await screen.findAllByRole('article');

    expect(substituto).toHaveTextContent('Você entrou na escala');
    expect(substituto).toHaveTextContent('Você entrou no lugar de quem desistiu.');
    expect(desistiu).toHaveTextContent(
      'Você desistiu da vaga de estivador hoje, às 11h. A vaga passou para quem estava na espera.',
    );
    expect(trabalhou).toHaveTextContent('Presença confirmada às 12h40.');
    expect(trabalhou).toHaveTextContent('Esta escala já foi encerrada.');
  });

  test('o botão "Ouvir" lê o cartão inteiro, com o horário por extenso', async () => {
    const voz = instalarVoz();
    responder([doAntonio]);
    abrir('/');

    await userEvent.click(within(await cartao(/^Hoje/)).getByRole('button', { name: 'Ouvir' }));

    expect(voz.ditas.map((fala) => fala.text)).toEqual([
      'Hoje, quinta-feira, 5 de novembro, da uma da tarde às sete da noite. Você vai trabalhar como sinaleiro, no terno 1 do navio Atlântico Sul. Local: Terminal Cais Leste, berço 4.',
    ]);
  });

  test('desistir pergunta antes, por inteiro, e a vaga passa para a espera', async () => {
    let desistiu = false;
    servidor.use(
      http.get('/api/eu/escalas', () => HttpResponse.json([desistiu ? doEdsonQueDesistiu : doEdson])),
      http.post(`/api/eu/escalas/${Ids.daTarde}/desistencia`, () => {
        desistiu = true;

        return HttpResponse.json(doEdsonQueDesistiu);
      }),
    );
    abrir('/', { token: tokenDeTeste({ name: 'Edson Prado', matricula: '4101' }) });

    await userEvent.click(within(await cartao(/^Hoje/)).getByRole('button', { name: 'Desistir da vaga' }));

    const pergunta = screen.getByRole('group', {
      name: 'Desistir da vaga de estivador de hoje, quinta-feira, 5 de novembro, das 13h às 19h?',
    });
    expect(pergunta).toHaveTextContent('A vaga passa para o primeiro da espera');
    expect(
      within(pergunta)
        .getByText(/^Desistir da vaga de estivador/)
        .closest('p'),
    ).toHaveFocus();
    expect(await problemasDeAcessibilidade()).toEqual([]);

    await userEvent.click(within(pergunta).getByRole('button', { name: 'Sim, desistir da vaga' }));

    expect(await screen.findByText('Você desistiu da vaga')).toBeInTheDocument();
    expect(desistiu).toBe(true);
  });

  test('o "não" desfaz a pergunta, e nada é enviado', async () => {
    responder([doEdson]);
    abrir('/', { token: tokenDeTeste({ name: 'Edson Prado', matricula: '4101' }) });

    await userEvent.click(within(await cartao(/^Hoje/)).getByRole('button', { name: 'Desistir da vaga' }));
    await userEvent.click(screen.getByRole('button', { name: 'Não, vou trabalhar' }));

    expect(screen.getByRole('button', { name: 'Desistir da vaga' })).toBeInTheDocument();
  });

  test('o que o domínio recusa aparece com a frase dele', async () => {
    servidor.use(
      http.get('/api/eu/escalas', () => HttpResponse.json([doEdson])),
      http.post(`/api/eu/escalas/${Ids.daTarde}/desistencia`, () =>
        HttpResponse.json(
          {
            title: 'O pedido fere uma regra do trabalho portuário.',
            status: 422,
            detail:
              'O período de 05/11/2026, das 13h às 19h, já começou: quem não vai trabalhar fica com falta, e não desiste da vaga.',
          },
          { status: 422, headers: { 'Content-Type': 'application/problem+json' } },
        ),
      ),
    );
    abrir('/', { token: tokenDeTeste({ name: 'Edson Prado', matricula: '4101' }) });

    await userEvent.click(within(await cartao(/^Hoje/)).getByRole('button', { name: 'Desistir da vaga' }));
    await userEvent.click(screen.getByRole('button', { name: 'Sim, desistir da vaga' }));

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'quem não vai trabalhar fica com falta, e não desiste da vaga.',
    );
  });

  test('depois que o período começa, não se oferece desistir', async () => {
    vi.setSystemTime(new Date('2026-11-05T13:00:00-03:00'));
    responder([doEdson]);
    abrir('/');

    expect(
      within(await cartao(/^Hoje/)).queryByRole('button', { name: 'Desistir da vaga' }),
    ).not.toBeInTheDocument();
  });

  test('sem escala na semana, diz como entrar numa', async () => {
    responder([]);
    abrir('/');

    expect(await screen.findByText('Nenhuma escala nestes dias.')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Ver onde posso me habilitar' })).toHaveAttribute(
      'href',
      '/habilitacao',
    );
    expect(await problemasDeAcessibilidade()).toEqual([]);
  });

  test('o botão de atualizar lê a escala de novo', async () => {
    let leituras = 0;
    servidor.use(
      http.get('/api/eu/escalas', () => {
        leituras += 1;

        return HttpResponse.json([doAntonio]);
      }),
    );
    abrir('/');

    await userEvent.click(await screen.findByRole('button', { name: 'Atualizar a escala' }));

    await waitFor(() => {
      expect(leituras).toBe(2);
    });
  });
});
