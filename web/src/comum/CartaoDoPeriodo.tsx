import type { ReactNode } from 'react';
import type { Esquemas } from '../api/cliente';
import estilos from './CartaoDoPeriodo.module.css';
import { diaParaQuemLe } from './datas';
import { Turnos } from './rotulos';
import type { TomDoSelo } from './Selo';

/**
 * O cartão de um período, o mesmo na escala e na habilitação: em cima, o
 * dia e o horário, grandes — "Hoje, quinta-feira, 5 de novembro", "Tarde,
 * das 13h às 19h" —, que dão nome ao cartão para o leitor de tela; na
 * borda, a cor da situação, que o selo de dentro diz por escrito.
 */
export function CartaoDoPeriodo({
  periodo,
  hoje,
  tom,
  chave,
  children,
}: {
  readonly periodo: Esquemas['PeriodoResposta'];
  readonly hoje: string;
  readonly tom: TomDoSelo;
  /** O que distingue este cartão dos outros da tela, para o título ter um id só dele. */
  readonly chave: string;
  readonly children: ReactNode;
}) {
  const titulo = `periodo-${chave}`;
  const turno = Turnos[periodo.turno];

  return (
    <article className={`${estilos.cartao ?? ''} ${estilos[tom] ?? ''}`} aria-labelledby={titulo}>
      <h2 id={titulo} className={estilos.quando}>
        {/* A vírgula separa o dia do turno também na voz do leitor de tela. */}
        <span>{diaParaQuemLe(periodo.dia, hoje)},</span>{' '}
        <span className={estilos.turno}>
          {turno.nome}, {turno.horario}
        </span>
      </h2>
      {children}
    </article>
  );
}
