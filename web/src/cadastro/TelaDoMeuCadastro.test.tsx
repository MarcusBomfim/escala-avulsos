import { screen, waitFor } from '@testing-library/react';
import { userEvent } from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { afterEach, beforeEach, describe, expect, test, vi } from 'vitest';
import type { Esquemas } from '../api/cliente';
import { criarGuarda } from '../offline/guarda';
import { abrir } from '../testes/abrir';
import { problemasDeAcessibilidade } from '../testes/acessibilidade';
import { doAntonio } from '../testes/exemplos';
import { cadastroDoAntonio } from '../testes/exemplosDoCadastro';
import { servidor } from '../testes/servidor';

// Quinta, 05/11/2026: o exame do Antônio vence em 25 dias.
beforeEach(() => {
  vi.useFakeTimers({ now: new Date('2026-11-05T10:00:00-03:00'), toFake: ['Date'] });
});

afterEach(() => {
  vi.useRealTimers();
});

function responder(cadastro: Esquemas['TrabalhadorResposta']) {
  servidor.use(http.get('/api/eu', () => HttpResponse.json(cadastro)));
}

describe('meus dados', () => {
  test('o cadastro, com o CPF, não fica guardado no aparelho; a escala, sim', async () => {
    // O relógio de verdade: o aplicativo espera um segundo entre uma gravação e outra do que guarda.
    vi.useRealTimers();
    responder(cadastroDoAntonio);
    servidor.use(http.get('/api/eu/escalas', () => HttpResponse.json([doAntonio])));
    abrir('/meus-dados', { guarda: criarGuarda() });
    expect(await screen.findByText('529.984.127-26')).toBeInTheDocument();

    await userEvent.click(screen.getByRole('link', { name: 'Minha escala' }));

    await waitFor(
      () => {
        expect(localStorage.getItem('escala-avulsos.consultas')).toContain('Atlântico Sul');
      },
      { timeout: 4000 },
    );
    expect(localStorage.getItem('escala-avulsos.consultas')).not.toContain('529.984.127-26');
  });

  test('a matrícula, as funções e as férias — e o exame que está para vencer, avisado antes', async () => {
    responder(cadastroDoAntonio);
    abrir('/meus-dados');

    const exame = await screen.findByRole('region', { name: 'Exame médico (ASO)' });

    expect(exame).toHaveTextContent('Vence logo');
    expect(exame).toHaveTextContent(
      'O exame vale até 30/11/2026 — vence em 25 dias. Renove antes, para não ficar de fora da escala.',
    );
    expect(screen.getByRole('region', { name: 'Você' })).toHaveTextContent(
      'Inscrição' + 'RegistradoNa escala, os registrados vêm antes dos cadastrados.',
    );
    expect(screen.getByRole('region', { name: 'Funções' })).toHaveTextContent(
      'Desde 10/05/2019. A reciclagem vale até 31/05/2027.',
    );
    expect(screen.getByRole('region', { name: 'Férias, afastamentos e suspensões' })).toHaveTextContent(
      'Férias de 04/01/2027 a 02/02/2027.',
    );
    expect(await problemasDeAcessibilidade()).toEqual([]);
  });

  test('a inscrição cancelada vem antes de tudo, com o motivo', async () => {
    responder({
      ...cadastroDoAntonio,
      cancelamento: { aPartirDe: '2026-12-01', motivo: 'Aposentadoria por tempo de contribuição' },
    });
    abrir('/meus-dados');

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'A sua inscrição foi cancelada a partir de 01/12/2026.Aposentadoria por tempo de contribuição',
    );
  });

  test('a matrícula que não está no cadastro volta com a frase da API', async () => {
    servidor.use(
      http.get('/api/eu', () =>
        HttpResponse.json(
          {
            title: 'O que foi pedido não existe.',
            status: 404,
            detail: 'A matrícula 4127 não está no cadastro.',
          },
          { status: 404, headers: { 'Content-Type': 'application/problem+json' } },
        ),
      ),
    );
    abrir('/meus-dados');

    expect(await screen.findByRole('alert')).toHaveTextContent('A matrícula 4127 não está no cadastro.');
  });
});
