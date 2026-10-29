import { act, screen } from '@testing-library/react';
import { onlineManager } from '@tanstack/react-query';
import { http, HttpResponse } from 'msw';
import { afterEach, describe, expect, test, vi } from 'vitest';
import { abrir } from '../testes/abrir';
import { doAntonio } from '../testes/exemplos';
import { servidor } from '../testes/servidor';
import { comecarPelaConexaoDoAparelho } from './conexao';

afterEach(() => {
  onlineManager.setOnline(true);
});

describe('sem internet', () => {
  test('o aviso aparece quando o sinal cai, e some quando volta — e a escala lida continua na tela', async () => {
    servidor.use(http.get('/api/eu/escalas', () => HttpResponse.json([doAntonio])));
    abrir('/');
    expect(await screen.findByText('Você vai trabalhar')).toBeInTheDocument();

    act(() => {
      onlineManager.setOnline(false);
    });

    expect(screen.getByText('Sem internet.')).toBeInTheDocument();
    expect(screen.getByText('Você vai trabalhar')).toBeInTheDocument();

    act(() => {
      onlineManager.setOnline(true);
    });

    expect(screen.queryByText('Sem internet.')).not.toBeInTheDocument();
  });

  test('aberto já sem sinal, começa pelo que o aparelho sabe', () => {
    vi.spyOn(navigator, 'onLine', 'get').mockReturnValue(false);

    comecarPelaConexaoDoAparelho();

    expect(onlineManager.isOnline()).toBe(false);
  });
});
