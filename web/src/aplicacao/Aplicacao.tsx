import { QueryClientProvider, type QueryClient } from '@tanstack/react-query';
import {
  PersistQueryClientProvider,
  type PersistQueryClientOptions,
} from '@tanstack/react-query-persist-client';
import type { ReactNode } from 'react';
import type { createBrowserRouter } from 'react-router';
import { RouterProvider } from 'react-router/dom';
import { ProvedorDaLeitura } from '../leitura/ProvedorDaLeitura';
import { ProvedorDaSessao } from '../sessao/ProvedorDaSessao';

export type Roteador = ReturnType<typeof createBrowserRouter>;

export type Guarda = Omit<PersistQueryClientOptions, 'queryClient'>;

/**
 * A leitura, o cache das consultas — guardado no aparelho, se houver guarda
 * —, a sessão e as rotas. Tudo recebido de fora, para os testes trocarem o
 * roteador e escolherem se guardam.
 */
export function Aplicacao({
  roteador,
  consultas,
  guarda,
}: {
  readonly roteador: Roteador;
  readonly consultas: QueryClient;
  readonly guarda?: Guarda | undefined;
}) {
  const dentro: ReactNode = (
    <ProvedorDaSessao>
      <RouterProvider router={roteador} />
    </ProvedorDaSessao>
  );

  return (
    <ProvedorDaLeitura>
      {guarda ? (
        <PersistQueryClientProvider client={consultas} persistOptions={guarda}>
          {dentro}
        </PersistQueryClientProvider>
      ) : (
        <QueryClientProvider client={consultas}>{dentro}</QueryClientProvider>
      )}
    </ProvedorDaLeitura>
  );
}
