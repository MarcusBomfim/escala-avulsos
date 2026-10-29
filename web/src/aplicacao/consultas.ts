import { QueryClient } from '@tanstack/react-query';

function status(erro: unknown): number | undefined {
  if (typeof erro === 'object' && erro !== null && 'status' in erro && typeof erro.status === 'number') {
    return erro.status;
  }

  return undefined;
}

/**
 * O cache das leituras da API. O que vem da API é estado do servidor, e
 * mora aqui — não num estado global do aplicativo (ADR-0017).
 *
 * Tenta de novo o que pode ter sido passageiro — rede, erro do servidor —,
 * e não o que a API recusou com motivo: um 404 ou um 422 não mudam com
 * insistência.
 *
 * O que o trabalhador manda — a habilitação, a desistência — vai na hora, ou
 * falha na hora: sem sinal, ele fica sabendo que não foi, em vez de o pedido
 * esperar o sinal voltar e chegar quando já não vale.
 */
export function criarConsultas({ tentativas = 2 }: { readonly tentativas?: number } = {}): QueryClient {
  return new QueryClient({
    defaultOptions: {
      queries: {
        staleTime: 15_000,
        // O que ficou guardado no aparelho vale por uma semana (offline/guarda.ts).
        gcTime: 7 * 24 * 60 * 60 * 1000,
        retry: (falhas, erro) => {
          const codigo = status(erro);

          return falhas < tentativas && (codigo === undefined || codigo >= 500);
        },
      },
      mutations: { networkMode: 'always' },
    },
  });
}
