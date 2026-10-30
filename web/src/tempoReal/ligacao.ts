import { useQueryClient, type QueryClient } from '@tanstack/react-query';
import { createContext, useContext, useEffect, useState } from 'react';
import { escutar, type EventoDoServidor } from './eventos';

/**
 * Se o aplicativo escuta os eventos do servidor. Ligado no aplicativo de
 * verdade; nos testes, só nos que tratam disso — os outros não têm o
 * servidor de eventos.
 */
export const TempoRealLigado = createContext(true);

/** Quanto esperar antes de ligar de novo: de um segundo até trinta, dobrando a cada falha. */
const PrimeiraEspera = 1_000;
const MaiorEspera = 30_000;

/**
 * O que ler de novo a cada evento (ADR-0020). O "pronto" — ao conectar, ou
 * quando a escuta do servidor recomeça — lê tudo: o que mudou no meio não
 * chegou. O "mudou" lê só o que aquela mudança mexe.
 */
export function oQueLerDeNovo(evento: EventoDoServidor): readonly (readonly string[])[] | 'tudo' {
  if (evento.tipo === 'pronto') {
    return 'tudo';
  }

  switch (evento.dados.origem) {
    case 'Avisos':
      return [['get', '/api/eu/avisos']];
    case 'Escalas':
      return [
        ['get', '/api/eu/escalas'],
        ['get', '/api/eu/habilitacoes'],
        ['get', '/api/eu/meses/{mes}'],
        ['get', '/api/quadro/{dia}/{turno}'],
      ];
    case 'Escalacoes':
    case 'Requisicoes':
      return [['get', '/api/quadro/{dia}/{turno}']];
    default:
      return [];
  }
}

function lerDeNovo(consultas: QueryClient, evento: EventoDoServidor) {
  const oQue = oQueLerDeNovo(evento);

  if (oQue === 'tudo') {
    void consultas.invalidateQueries();

    return;
  }

  for (const chave of oQue) {
    void consultas.invalidateQueries({ queryKey: chave });
  }
}

function esperar(milissegundos: number, parar: AbortSignal): Promise<void> {
  return new Promise((resolver) => {
    const relogio = setTimeout(resolver, milissegundos);
    parar.addEventListener('abort', () => {
      clearTimeout(relogio);
      resolver();
    });
  });
}

/**
 * A ligação com o servidor enquanto o aplicativo está aberto: a cada evento,
 * lê de novo o que mudou; se a ligação cai, liga de novo, esperando cada vez
 * mais. Sem ligação, o aplicativo funciona como antes: lê ao abrir, e quando
 * se pede.
 */
export function useLigacaoAoVivo(token: string | undefined): boolean {
  const ligado = useContext(TempoRealLigado);
  const consultas = useQueryClient();
  const [aoVivo, definirAoVivo] = useState(false);

  useEffect(() => {
    if (!ligado || !token) {
      return;
    }

    const parar = new AbortController();

    void (async () => {
      let espera = PrimeiraEspera;

      while (!parar.signal.aborted) {
        try {
          await escutar(
            token,
            (evento) => {
              definirAoVivo(true);
              espera = PrimeiraEspera;
              lerDeNovo(consultas, evento);
            },
            parar.signal,
          );
        } catch {
          // Sem sinal, servidor fora do ar, token recusado: liga de novo daqui a pouco.
        }

        definirAoVivo(false);
        await esperar(espera, parar.signal);
        espera = Math.min(espera * 2, MaiorEspera);
      }
    })();

    return () => {
      parar.abort();
    };
  }, [ligado, token, consultas]);

  return aoVivo;
}
