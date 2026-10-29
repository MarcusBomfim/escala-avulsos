import { screen, within } from '@testing-library/react';
import { userEvent } from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { afterEach, beforeEach, describe, expect, test, vi } from 'vitest';
import { abrir } from '../testes/abrir';
import { problemasDeAcessibilidade } from '../testes/acessibilidade';
import { novembroDoAntonio, outubroVazio } from '../testes/exemplosDoMes';
import { servidor } from '../testes/servidor';

// Sexta, 06/11/2026, às 9h: a tarde de quinta já está no extrato.
beforeEach(() => {
  vi.useFakeTimers({ now: new Date('2026-11-06T09:00:00-03:00'), toFake: ['Date'] });
});

afterEach(() => {
  vi.useRealTimers();
});

describe('meu mês', () => {
  test('sem mês no endereço, o mês de hoje: a frequência e o extrato, com o prazo do pagamento', async () => {
    servidor.use(http.get('/api/eu/meses/2026-11', () => HttpResponse.json(novembroDoAntonio)));
    abrir('/mes');

    const frequencia = await screen.findByRole('region', { name: 'Frequência' });
    const extrato = screen.getByRole('region', { name: 'Extrato' });

    expect(screen.getByText('Novembro de 2026')).toBeInTheDocument();
    expect(frequencia).toHaveTextContent('Mês em andamento');
    expect(frequencia).toHaveTextContent('Você se habilitou 1 vez neste mês. O mínimo é 12: faltam 11.');
    expect(frequencia).toHaveTextContent('Faltas sem justificativa0 de 2 aceitas');

    const [linha] = within(extrato).getAllByRole('listitem');
    expect(linha).toHaveTextContent('Quinta-feira, 5 de novembro');
    expect(linha).toHaveTextContent('Sinaleiro, no terno 1 do navio Atlântico Sul');
    expect(linha).toHaveTextContent('R$ 260,00');
    expect(linha).toHaveTextContent('Pago até amanhã, às 19h.');
    expect(extrato).toHaveTextContent('FGTS — o Fundo de Garantia, depositado na sua contaR$ 24,84');
    expect(await problemasDeAcessibilidade()).toEqual([]);
  });

  test('o mês anterior pelo botão, e de lá o próximo; o mês que ainda não chegou não se oferece', async () => {
    servidor.use(
      http.get('/api/eu/meses/2026-11', () => HttpResponse.json(novembroDoAntonio)),
      http.get('/api/eu/meses/2026-10', () => HttpResponse.json(outubroVazio)),
    );
    const { roteador } = abrir('/mes');

    expect(await screen.findByRole('region', { name: 'Extrato' })).toBeInTheDocument();
    expect(screen.queryByRole('link', { name: 'Próximo mês' })).not.toBeInTheDocument();

    await userEvent.click(screen.getByRole('link', { name: 'Mês anterior' }));

    expect(await screen.findByText('Nenhum período trabalhado neste mês.')).toBeInTheDocument();
    expect(screen.getByText('Outubro de 2026')).toBeInTheDocument();
    expect(screen.getByText('Frequência não cumprida')).toBeInTheDocument();
    expect(roteador.state.location.pathname).toBe('/mes/2026-10');
    expect(screen.getByRole('link', { name: 'Meu mês' })).toHaveAttribute('aria-current', 'page');
    expect(screen.getByRole('link', { name: 'Próximo mês' })).toHaveAttribute('href', '/mes/2026-11');
  });

  test('o mês fora do formato volta com a explicação da API', async () => {
    servidor.use(
      http.get('/api/eu/meses/11-2026', () =>
        HttpResponse.json(
          {
            title: 'O pedido está malformado.',
            status: 400,
            detail: 'O valor de mês é um mês, como 2026-11; veio "11-2026".',
          },
          { status: 400, headers: { 'Content-Type': 'application/problem+json' } },
        ),
      ),
    );
    abrir('/mes/11-2026');

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'O valor de mês é um mês, como 2026-11; veio "11-2026".',
    );
  });
});
