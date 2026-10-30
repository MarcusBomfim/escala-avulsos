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
import { TempoRealLigado } from '../tempoReal/ligacao';

export type Roteador = ReturnType<typeof createBrowserRouter>;

export type Guarda = Omit<PersistQueryClientOptions, 'queryClient'>;

/**
 * A leitura, o cache das consultas — guardado no aparelho, se houver guarda
 * —, a sessão, a ligação ao vivo com o servidor e as rotas. Tudo recebido de
 * fora, para os testes trocarem o roteador e escolherem se guardam e se
 * escutam o servidor.
 */
export function Aplicacao({
  roteador,
  consultas,
  guarda,
  tempoReal = true,
}: {
  readonly roteador: Roteador;
  readonly consultas: QueryClient;
  readonly guarda?: Guarda | undefined;
  readonly tempoReal?: boolean | undefined;
}) {
  const dentro: ReactNode = (
    <ProvedorDaSessao>
      <TempoRealLigado value={tempoReal}>
        <RouterProvider router={roteador} />
      </TempoRealLigado>
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
