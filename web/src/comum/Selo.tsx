import estilos from './Selo.module.css';

export type TomDoSelo = 'sucesso' | 'atencao' | 'perigo' | 'neutro';

/** A situação escrita, com a cor dela — a cor nunca sozinha, porque nem todo olho a distingue. */
export function Selo({ tom, children }: { readonly tom: TomDoSelo; readonly children: string }) {
  return <span className={`${estilos.selo ?? ''} ${estilos[tom] ?? ''}`}>{children}</span>;
}
