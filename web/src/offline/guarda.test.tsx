import { screen, waitFor } from '@testing-library/react';
import { userEvent } from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { describe, expect, test } from 'vitest';
import { abrir } from '../testes/abrir';
import { doAntonio } from '../testes/exemplos';
import { servidor } from '../testes/servidor';
import { tokenDeTeste } from '../testes/tokens';
import { criarGuarda } from './guarda';

describe('a escala guardada no aparelho', () => {
  test('a escala lida fica guardada no aparelho', async () => {
    servidor.use(http.get('/api/eu/escalas', () => HttpResponse.json([doAntonio])));
    abrir('/', { guarda: criarGuarda() });

    expect(await screen.findByText('Você vai trabalhar')).toBeInTheDocument();

    await waitFor(() => {
      expect(localStorage.getItem('escala-avulsos.consultas')).toContain('Atlântico Sul');
    });
  });

  test('entrando pela tela de entrada, a escala também fica guardada', async () => {
    servidor.use(http.get('/api/eu/escalas', () => HttpResponse.json([doAntonio])));
    abrir('/', { token: null, guarda: criarGuarda() });

    await userEvent.click(await screen.findByLabelText('Token de acesso'));
    await userEvent.paste(tokenDeTeste());
    await userEvent.click(screen.getByRole('button', { name: 'Entrar' }));

    expect(await screen.findByText('Você vai trabalhar')).toBeInTheDocument();
    await waitFor(
      () => {
        expect(localStorage.getItem('escala-avulsos.consultas')).toContain('Atlântico Sul');
      },
      { timeout: 4000 },
    );
  });
});
