import { screen } from '@testing-library/react';
import { userEvent } from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { describe, expect, test } from 'vitest';
import { abrir } from '../testes/abrir';
import { problemasDeAcessibilidade } from '../testes/acessibilidade';
import { servidor } from '../testes/servidor';
import { tokenDeTeste } from '../testes/tokens';
import { lerToken } from './token';

describe('a entrada', () => {
  test('sem sessão, a escala manda para a entrada, e volta para onde ia', async () => {
    servidor.use(http.get('/api/eu/escalas', () => HttpResponse.json([])));
    const { roteador } = abrir('/', { token: null });

    expect(await screen.findByRole('heading', { name: 'Minha escala', level: 1 })).toBeInTheDocument();
    expect(roteador.state.location.pathname).toBe('/entrar');
    expect(await problemasDeAcessibilidade()).toEqual([]);

    await userEvent.click(screen.getByLabelText('Token de acesso'));
    await userEvent.paste(tokenDeTeste());
    await userEvent.click(screen.getByRole('button', { name: 'Entrar' }));

    expect(await screen.findByText('Nenhuma escala nestes dias.')).toBeInTheDocument();
    expect(roteador.state.location.pathname).toBe('/');
    expect(screen.getByText('Antônio Ferreira')).toBeInTheDocument();
    expect(screen.getByText('Matrícula 4127')).toBeInTheDocument();
  });

  test('o token que não serve é recusado com o motivo, ali mesmo', async () => {
    abrir('/entrar', { token: null });

    await userEvent.click(screen.getByLabelText('Token de acesso'));
    await userEvent.paste(tokenDeTeste({ role: 'operador' }));
    await userEvent.click(screen.getByRole('button', { name: 'Entrar' }));

    expect(screen.getByRole('alert')).toHaveTextContent(
      'Este aplicativo é do trabalhador, e o token não é de trabalhador.',
    );
    expect(screen.getByLabelText('Token de acesso')).toHaveAttribute('aria-invalid', 'true');
  });

  test('a letra e o contraste já se ajustam na entrada', () => {
    abrir('/entrar', { token: null });

    expect(screen.getByRole('button', { name: 'Letra: normal' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Contraste alto' })).toBeInTheDocument();
  });

  test('a API recusando o token, volta para a entrada dizendo por quê, e esquece o token', async () => {
    servidor.use(http.get('/api/eu/escalas', () => new HttpResponse(null, { status: 401 })));
    abrir('/');

    expect(await screen.findByText('O seu acesso venceu. Entre de novo.')).toBeInTheDocument();
    expect(lerToken()).toBeNull();
  });

  test('sair apaga o token e a escala guardada no aparelho', async () => {
    servidor.use(http.get('/api/eu/escalas', () => HttpResponse.json([])));
    localStorage.setItem('escala-avulsos.consultas', '{"guardado":true}');
    const { roteador } = abrir('/');

    await userEvent.click(await screen.findByRole('button', { name: 'Sair do aplicativo' }));

    expect(roteador.state.location.pathname).toBe('/entrar');
    expect(lerToken()).toBeNull();
    expect(localStorage.getItem('escala-avulsos.consultas')).toBeNull();
  });

  test('entrar apaga a escala que outro deixou guardada no aparelho', async () => {
    servidor.use(http.get('/api/eu/escalas', () => HttpResponse.json([])));
    localStorage.setItem('escala-avulsos.consultas', '{"de":"Carlos Mota"}');
    abrir('/entrar', { token: null });

    await userEvent.click(screen.getByLabelText('Token de acesso'));
    await userEvent.paste(tokenDeTeste());
    await userEvent.click(screen.getByRole('button', { name: 'Entrar' }));

    expect(localStorage.getItem('escala-avulsos.consultas')).toBeNull();
  });
});
