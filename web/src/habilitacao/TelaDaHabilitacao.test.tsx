import { screen, waitFor, within } from '@testing-library/react';
import { userEvent } from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { afterEach, beforeEach, describe, expect, test, vi } from 'vitest';
import type { Esquemas } from '../api/cliente';
import { abrir } from '../testes/abrir';
import { problemasDeAcessibilidade } from '../testes/acessibilidade';
import {
  manhaDeDezembro,
  noiteQueNaoAbriu,
  tardeAberta,
  tardeHabilitado,
} from '../testes/exemplosDaHabilitacao';
import { servidor } from '../testes/servidor';

// Quinta, 05/11/2026, às 8h no porto: a habilitação da tarde está aberta até as 10h.
beforeEach(() => {
  vi.useFakeTimers({ now: new Date('2026-11-05T08:00:00-03:00'), toFake: ['Date'] });
});

afterEach(() => {
  vi.useRealTimers();
});

function responder(lista: Esquemas['MinhaHabilitacaoResposta'][]) {
  servidor.use(http.get('/api/eu/habilitacoes', () => HttpResponse.json(lista)));
}

const tarde = () => screen.findByRole('article', { name: /^Hoje, quinta-feira, 5 de novembro, Tarde/ });

describe('habilitar', () => {
  test('as escalações abertas, cada uma com a janela e as funções dele já marcadas', async () => {
    responder([tardeAberta, noiteQueNaoAbriu]);
    abrir('/habilitacao');

    const daTarde = await tarde();
    const daNoite = screen.getByRole('article', { name: /^Amanhã, sexta-feira, 6 de novembro, Noite/ });

    expect(daTarde).toHaveTextContent('Habilitação aberta');
    expect(daTarde).toHaveTextContent('A habilitação fecha hoje, às 10h.');
    expect(within(daTarde).getByRole('checkbox', { name: 'Estivador' })).toBeChecked();
    expect(within(daTarde).getByRole('checkbox', { name: 'Sinaleiro' })).toBeChecked();
    expect(daNoite).toHaveTextContent('A habilitação abre amanhã, às 7h, e fecha amanhã, às 16h.');
    expect(within(daNoite).queryByRole('checkbox')).not.toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Habilitar' })).toHaveAttribute('aria-current', 'page');
    expect(await problemasDeAcessibilidade()).toEqual([]);
  });

  test('habilitar-se manda as funções marcadas, sem versão, e diz como ficou', async () => {
    const pedidos: { readonly corpo: unknown; readonly ifMatch: string | null }[] = [];
    let habilitado = false;
    servidor.use(
      http.get('/api/eu/habilitacoes', () => HttpResponse.json([habilitado ? tardeHabilitado : tardeAberta])),
      http.put('/api/eu/habilitacoes/2026-11-05/Tarde', async ({ request }) => {
        pedidos.push({ corpo: await request.json(), ifMatch: request.headers.get('If-Match') });
        habilitado = true;

        return HttpResponse.json({ ...tardeHabilitado, funcoes: ['Sinaleiro'] });
      }),
    );
    abrir('/habilitacao');
    const daTarde = await tarde();

    await userEvent.click(within(daTarde).getByRole('checkbox', { name: 'Estivador' }));
    await userEvent.click(within(daTarde).getByRole('button', { name: 'Habilitar-me' }));

    expect(await within(daTarde).findByRole('status')).toHaveTextContent(
      'Pronto: você está habilitado como sinaleiro.',
    );
    expect(pedidos).toEqual([{ corpo: { funcoes: ['Sinaleiro'] }, ifMatch: null }]);
    expect(await within(daTarde).findByText('Você está habilitado')).toBeInTheDocument();
  });

  test('habilitado, troca as funções ou desiste; sem mudança, não há o que trocar', async () => {
    let desistiu = false;
    servidor.use(
      http.get('/api/eu/habilitacoes', () => HttpResponse.json([desistiu ? tardeAberta : tardeHabilitado])),
      http.delete('/api/eu/habilitacoes/2026-11-05/Tarde', () => {
        desistiu = true;

        return HttpResponse.json(tardeAberta);
      }),
    );
    abrir('/habilitacao');
    const daTarde = await tarde();

    expect(daTarde).toHaveTextContent(
      'Como sinaleiro e estivador. Pode trocar as funções ou desistir até hoje, às 10h.',
    );
    expect(within(daTarde).getByRole('button', { name: 'Trocar as funções' })).toBeDisabled();

    await userEvent.click(within(daTarde).getByRole('button', { name: 'Desistir da habilitação' }));

    expect(
      await within(daTarde).findByText('Pronto: você desistiu da habilitação para este período.'),
    ).toBeInTheDocument();
    await waitFor(() => {
      expect(within(daTarde).getByRole('button', { name: 'Habilitar-me' })).toBeInTheDocument();
    });
  });

  test('a função que ele não pode exercer vem desmarcada, com o motivo — antes de ele tentar', async () => {
    responder([manhaDeDezembro]);
    abrir('/habilitacao');

    const dezembro = await screen.findByRole('article', { name: /^Terça-feira, 1 de dezembro, Manhã/ });
    const estivador = within(dezembro).getByRole('checkbox', { name: 'Estivador' });

    expect(estivador).toBeDisabled();
    expect(estivador).not.toBeChecked();
    expect(estivador).toHaveAccessibleDescription('Não pode: o exame médico (ASO) vale só até 30/11/2026.');
    expect(within(dezembro).getByRole('button', { name: 'Habilitar-me' })).toBeDisabled();
    expect(await problemasDeAcessibilidade()).toEqual([]);
  });

  test('o que o domínio recusa aparece com a frase dele', async () => {
    servidor.use(
      http.get('/api/eu/habilitacoes', () => HttpResponse.json([tardeAberta])),
      http.put('/api/eu/habilitacoes/2026-11-05/Tarde', () =>
        HttpResponse.json(
          {
            title: 'O pedido fere uma regra do trabalho portuário.',
            status: 422,
            detail:
              'A habilitação para a escala da estiva de 05/11/2026, das 13h às 19h, fechou em 05/11/2026, às 10h.',
          },
          { status: 422, headers: { 'Content-Type': 'application/problem+json' } },
        ),
      ),
    );
    abrir('/habilitacao');

    await userEvent.click(within(await tarde()).getByRole('button', { name: 'Habilitar-me' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('fechou em 05/11/2026, às 10h.');
  });

  test('sem escalação aberta, diz que o órgão gestor ainda vai abrir', async () => {
    responder([]);
    abrir('/habilitacao');

    expect(
      await screen.findByText('Nenhuma escalação aberta para a sua categoria agora.'),
    ).toBeInTheDocument();
  });
});
