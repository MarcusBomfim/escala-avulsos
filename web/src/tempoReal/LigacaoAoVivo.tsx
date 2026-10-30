import { useSessao } from '../sessao/contexto';
import { useLigacaoAoVivo } from './ligacao';
import estilos from './LigacaoAoVivo.module.css';

/** O aviso, na barra de cima, de que a tela se atualiza sozinha: só enquanto a ligação está de pé. */
export function LigacaoAoVivo() {
  const { sessao } = useSessao();
  const aoVivo = useLigacaoAoVivo(sessao?.token);

  return aoVivo ? <p className={estilos.aoVivo}>Atualiza sozinho</p> : null;
}
