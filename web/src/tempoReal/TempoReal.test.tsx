import { screen, waitFor } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { describe, expect, test } from 'vitest';
import { abrir } from '../testes/abrir';
import { servidor } from '../testes/servidor';
import { lerEventos } from './eventos';
import { oQueLerDeNovo } from './ligacao';

describe('os eventos do servidor', () => {
  test('saem inteiros do texto que chegou, e o pedaço do próximo espera o resto', () => {
    const { eventos, resto } = lerEventos(
      'event: pronto\ndata: {}\n\n' +
        'event: vivo\ndata: {}\n\n' +
        'event: mudou\r\ndata: {"origem":"Escalas","categoria":"Estiva","dia":"2026-11-05","turno":"Tarde"}\r\n\r\n' +
        'event: mudou\ndata: {"origem":"Avi',
    );

    expect(eventos).toEqual([
      { tipo: 'pronto', dados: { origem: null, categoria: null, dia: null, turno: null } },
      { tipo: 'mudou', dados: { origem: 'Escalas', categoria: 'Estiva', dia: '2026-11-05', turno: 'Tarde' } },
    ]);
    expect(resto).toBe('event: mudou\ndata: {"origem":"Avi');
    expect(lerEventos(`${resto}sos"}\n\n`).eventos).toEqual([
      { tipo: 'mudou', dados: { origem: 'Avisos', categoria: null, dia: null, turno: null } },
    ]);
  });

  test('cada mudança lê de novo só o que ela mexe; o pronto lê tudo', () => {
    const mudou = (origem: 'Avisos' | 'Escalas' | 'Escalacoes' | 'Requisicoes') =>
      oQueLerDeNovo({ tipo: 'mudou', dados: { origem, categoria: null, dia: null, turno: null } });

    expect(
      oQueLerDeNovo({ tipo: 'pronto', dados: { origem: null, categoria: null, dia: null, turno: null } }),
    ).toBe('tudo');
    expect(mudou('Avisos')).toEqual([['get', '/api/eu/avisos']]);
    expect(mudou('Escalas')).toContainEqual(['get', '/api/eu/escalas']);
    expect(mudou('Escalas')).toContainEqual(['get', '/api/quadro/{dia}/{turno}']);
    expect(mudou('Escalacoes')).toEqual([['get', '/api/quadro/{dia}/{turno}']]);
    expect(mudou('Requisicoes')).toEqual([['get', '/api/quadro/{dia}/{turno}']]);
  });

  test('com a ligação de pé, a tela diz que se atualiza sozinha, e a escala que saiu aparece sem tocar em nada', async () => {
    let leituras = 0;
    let enviar: ((texto: string) => void) | undefined;

    servidor.use(
      http.get('/api/eu/escalas', () => {
        leituras += 1;

        return HttpResponse.json([]);
      }),
      http.get('/api/eventos', ({ request }) => {
        expect(request.headers.get('Authorization')).toMatch(/^Bearer /);
        const fluxo = new ReadableStream<Uint8Array>({
          start(controle) {
            const codificador = new TextEncoder();
            enviar = (texto) => {
              controle.enqueue(codificador.encode(texto));
            };
            enviar('event: pronto\ndata: {}\n\n');
          },
        });

        return new HttpResponse(fluxo, { headers: { 'Content-Type': 'text/event-stream' } });
      }),
    );
    abrir('/', { tempoReal: true });

    expect(await screen.findByText('Atualiza sozinho')).toBeInTheDocument();
    const antes = leituras;

    enviar?.(
      'event: mudou\ndata: {"origem":"Escalas","categoria":"Estiva","dia":"2026-11-05","turno":"Tarde"}\n\n',
    );

    await waitFor(() => {
      expect(leituras).toBeGreaterThan(antes);
    });
  });

  test('sem a ligação, a tela não promete o que não faz', async () => {
    servidor.use(
      http.get('/api/eu/escalas', () => HttpResponse.json([])),
      http.get('/api/eventos', () => new HttpResponse(null, { status: 503 })),
    );
    abrir('/', { tempoReal: true });

    expect(await screen.findByRole('heading', { name: 'Minha escala', level: 1 })).toBeInTheDocument();
    expect(screen.queryByText('Atualiza sozinho')).not.toBeInTheDocument();
  });
});
