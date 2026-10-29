import { createContext, useContext } from 'react';
import type { Preferencias } from './preferencias';

export interface EstadoDaLeitura {
  readonly preferencias: Preferencias;
  readonly aumentarLetra: () => void;
  readonly alternarContraste: () => void;
}

export const ContextoDaLeitura = createContext<EstadoDaLeitura | null>(null);

export function useLeitura(): EstadoDaLeitura {
  const contexto = useContext(ContextoDaLeitura);

  if (!contexto) {
    throw new Error('useLeitura só funciona dentro do ProvedorDaLeitura.');
  }

  return contexto;
}
