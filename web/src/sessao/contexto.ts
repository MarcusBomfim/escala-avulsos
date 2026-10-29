import { createContext, useContext } from 'react';
import type { Sessao } from './token';

export interface EstadoDaSessao {
  readonly sessao: Sessao | null;
  /** Por que a sessão anterior acabou, para a tela de entrada contar. */
  readonly aviso: string | null;
  /** Entra com o token colado; devolve o motivo, se ele não serve. */
  readonly entrar: (token: string) => string | null;
  readonly sair: (aviso?: string) => void;
}

export const ContextoDaSessao = createContext<EstadoDaSessao | null>(null);

export function useSessao(): EstadoDaSessao {
  const contexto = useContext(ContextoDaSessao);

  if (!contexto) {
    throw new Error('useSessao só funciona dentro do ProvedorDaSessao.');
  }

  return contexto;
}

/** A sessão de quem está dentro da moldura — que só deixa entrar quem tem uma. */
export function useQuemEntrou() {
  const { sessao } = useSessao();

  if (!sessao) {
    throw new Error('useQuemEntrou só funciona dentro da moldura, com alguém dentro.');
  }

  return sessao;
}
