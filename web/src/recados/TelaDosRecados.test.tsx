import { screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { afterEach, beforeEach, describe, expect, test, vi } from 'vitest';
import { abrir } from '../testes/abrir';
import { problemasDeAcessibilidade } from '../testes/acessibilidade';
import { recadosDoAntonio } from '../testes/exemplosDosRecados';
import { servidor } from '../testes/servidor';
import { bytesDaChave } from './push';

// Quinta, 05/11/2026, às 10h05: a escala acabou de sair.
beforeEach(() => {
  vi.useFakeTimers({ now: new Date('2026-11-05T10:05:00-03:00'), toFake: ['Date'] });
});

afterEach(() => {
  vi.useRealTimers();
});

const semPush = http.get('/api/eu/avisos/push', () =>
  HttpResponse.json(
    {
      title: 'Não encontrado.',
      detail: 'Este servidor não manda os avisos ao celular: os recados ficam só no aplicativo.',
      status: 404,
    },
    { status: 404 },
  ),
);

describe('os recados', () => {
  test('a aba conta os novos, e a tela os mostra do mais novo para o mais antigo, com o que resolver', async () => {
    let lidos = 0;
    servidor.use(
      http.get('/api/eu/avisos', () => HttpResponse.json(recadosDoAntonio)),
      http.post('/api/eu/avisos/lidos', () => {
        lidos += 1;

        return new HttpResponse(null, { status: 204 });
      }),
      semPush,
    );
    abrir('/recados');

    const recados = await screen.findAllByRole('article');

    expect(recados.map((recado) => within(recado).getByRole('heading').textContent)).toEqual([
      'Você vai trabalhar',
      'O exame médico vai vencer',
      'Você ficou de fora da escala',
    ]);
    expect(recados[0]).toHaveTextContent('NovoHoje, às 10h');
    expect(recados[0]).toHaveTextContent('sinaleiro no terno 1 do navio Atlântico Sul');
    expect(
      within(recados[0] ?? document.body).getByRole('link', { name: 'Ver a minha escala' }),
    ).toHaveAttribute('href', '/');
    expect(
      within(recados[1] ?? document.body).getByRole('link', { name: 'Ver os meus dados' }),
    ).toHaveAttribute('href', '/meus-dados');
    expect(recados[2]).not.toHaveTextContent('Novo');

    // Abrir os recados os marca como lidos: a aba para de contar, e o "Novo" fica até sair da tela.
    await waitFor(() => {
      expect(lidos).toBe(1);
    });
    const principal = screen.getByRole('navigation', { name: 'Principal' });
    await waitFor(() => {
      expect(within(principal).getByRole('link', { name: 'Recados' })).toBeInTheDocument();
    });
    expect(screen.getAllByText('Novo')).toHaveLength(2);
    expect(await problemasDeAcessibilidade()).toEqual([]);
  });

  test('em outra tela, a aba diz quantos recados novos há — e a palavra vai junto, para o leitor de tela', async () => {
    servidor.use(
      http.get('/api/eu/avisos', () => HttpResponse.json(recadosDoAntonio)),
      http.get('/api/eu/escalas', () => HttpResponse.json([])),
    );
    abrir('/');

    const principal = screen.getByRole('navigation', { name: 'Principal' });

    expect(await within(principal).findByRole('link', { name: 'Recados 2 novos' })).toHaveAttribute(
      'href',
      '/recados',
    );
  });

  test('sem recado, a tela diz quando eles aparecem; com o push ligado, o navegador sem push é avisado do que fazer', async () => {
    servidor.use(
      http.get('/api/eu/avisos/push', () =>
        HttpResponse.json({ chavePublica: 'BCVxsr7N_eNgVRqvHtD0zTZsEc6' }),
      ),
    );
    abrir('/recados');

    expect(await screen.findByText('Nenhum recado por enquanto.')).toBeInTheDocument();
    // O jsdom, como um navegador antigo, não tem push: a tela explica, e não oferece o botão que não funcionaria.
    expect(
      await screen.findByText(/Este navegador não recebe os recados como notificação/),
    ).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Receber os recados no celular' })).not.toBeInTheDocument();
    expect(await problemasDeAcessibilidade()).toEqual([]);
  });

  test('a chave do servidor vira os bytes que o navegador pede para se inscrever', () => {
    expect([...bytesDaChave('BCVx')]).toEqual([0x04, 0x25, 0x71]);
    expect([...bytesDaChave('_-8')]).toEqual([0xff, 0xef]);
  });
});
