import type { Esquemas } from '../api/cliente';
import { FusoDoPorto } from '../comum/datas';

type Turno = Esquemas['Turno'];

const Partes = new Intl.DateTimeFormat('en-CA', {
  timeZone: FusoDoPorto,
  year: 'numeric',
  month: '2-digit',
  day: '2-digit',
  hour: '2-digit',
  hourCycle: 'h23',
});

/**
 * O período de agora no relógio do porto — o que o quadro abre primeiro. A
 * meia-noite e meia ainda é a noite do dia anterior: ela só termina à 1h
 * (ADR-0002).
 */
export function periodoDeAgora(agora: Date = new Date()): { readonly dia: string; readonly turno: Turno } {
  const partes = Object.fromEntries(Partes.formatToParts(agora).map((parte) => [parte.type, parte.value]));
  const hora = Number(partes.hour);
  const hoje = `${partes.year ?? ''}-${partes.month ?? ''}-${partes.day ?? ''}`;

  if (hora < 1) {
    const ontem = new Date(Date.UTC(Number(partes.year), Number(partes.month) - 1, Number(partes.day) - 1));

    return { dia: ontem.toISOString().slice(0, 10), turno: 'Noite' };
  }

  const turno: Turno = hora < 7 ? 'Madrugada' : hora < 13 ? 'Manha' : hora < 19 ? 'Tarde' : 'Noite';

  return { dia: hoje, turno };
}
