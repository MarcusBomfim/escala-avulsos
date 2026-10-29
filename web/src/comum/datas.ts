/*
 * Os dias e as horas como o porto os diz: no horário de Brasília, onde quer
 * que esteja o celular — o trabalhador de férias em Manaus vê a escala na
 * hora de Santos —, e por extenso, como se fala: "hoje, às 10h", "amanhã,
 * quinta-feira, 5 de novembro", "à 1h", "ao meio-dia".
 *
 * Dia sem hora — o DateOnly da API, "2026-11-05" — é um dia do calendário, e
 * nunca vira instante: em UTC−3, a meia-noite de UTC já é a véspera.
 */

export const FusoDoPorto = 'America/Sao_Paulo';

const PartesNoPorto = new Intl.DateTimeFormat('en-CA', {
  timeZone: FusoDoPorto,
  year: 'numeric',
  month: '2-digit',
  day: '2-digit',
  hour: '2-digit',
  minute: '2-digit',
  hourCycle: 'h23',
});

const DiaPorExtenso = new Intl.DateTimeFormat('pt-BR', {
  timeZone: 'UTC',
  weekday: 'long',
  day: 'numeric',
  month: 'long',
});

const MesPorExtenso = new Intl.DateTimeFormat('pt-BR', { timeZone: 'UTC', month: 'long', year: 'numeric' });

const Dinheiro = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });

const UmDia = 24 * 60 * 60 * 1000;

interface NoPorto {
  readonly dia: string;
  readonly hora: number;
  readonly minuto: number;
}

function noPorto(instante: Date | string): NoPorto {
  const partes = Object.fromEntries(
    PartesNoPorto.formatToParts(new Date(instante)).map((parte) => [parte.type, parte.value]),
  );

  return {
    dia: `${partes.year ?? ''}-${partes.month ?? ''}-${partes.day ?? ''}`,
    hora: Number(partes.hour),
    minuto: Number(partes.minute),
  };
}

/** "2026-11-05" ao meio-dia de UTC: um dia do calendário, longe de qualquer virada de fuso. */
function comoData(dia: string): Date {
  const partes = /^(\d{4})-(\d{2})-(\d{2})$/.exec(dia);

  if (!partes) {
    throw new Error(`Dia fora do formato aaaa-mm-dd: ${dia}`);
  }

  return new Date(Date.UTC(Number(partes[1]), Number(partes[2]) - 1, Number(partes[3]), 12));
}

export function comMaiuscula(texto: string): string {
  return texto.charAt(0).toUpperCase() + texto.slice(1);
}

/** O dia de hoje no relógio do porto: "2026-11-05". */
export function hojeNoPorto(agora: Date = new Date()): string {
  return noPorto(agora).dia;
}

/** O dia do porto em que cai o instante. */
export function diaNoPorto(instante: string): string {
  return noPorto(instante).dia;
}

export function somarDias(dia: string, dias: number): string {
  return new Date(comoData(dia).getTime() + dias * UmDia).toISOString().slice(0, 10);
}

/** Quantos dias do primeiro ao segundo: 1 de hoje para amanhã, -1 para ontem. */
export function diasEntre(de: string, ate: string): number {
  return Math.round((comoData(ate).getTime() - comoData(de).getTime()) / UmDia);
}

/** "quinta-feira, 5 de novembro". */
export function diaPorExtenso(dia: string): string {
  return DiaPorExtenso.format(comoData(dia));
}

const Perto: Readonly<Record<number, string>> = { [-1]: 'ontem', 0: 'hoje', 1: 'amanhã' };

/** "hoje", "amanhã" ou "ontem", perto de hoje; longe, nada. */
function perto(dia: string, hoje: string): string | undefined {
  return Perto[diasEntre(hoje, dia)];
}

/** "Hoje, quinta-feira, 5 de novembro"; "Sábado, 7 de novembro". */
export function diaParaQuemLe(dia: string, hoje: string): string {
  const relativo = perto(dia, hoje);

  return comMaiuscula(relativo ? `${relativo}, ${diaPorExtenso(dia)}` : diaPorExtenso(dia));
}

/** "10h", "12h40", "meio-dia", "meia-noite". */
function horaPorExtenso({ hora, minuto }: NoPorto): string {
  if (minuto === 0 && (hora === 0 || hora === 12)) {
    return hora === 0 ? 'meia-noite' : 'meio-dia';
  }

  return minuto === 0 ? `${hora}h` : `${hora}h${String(minuto).padStart(2, '0')}`;
}

/** A hora de agora no relógio do porto, para o relógio do alto da tela: "10h05", "meio-dia". */
export function horaNoPorto(agora: Date = new Date()): string {
  return horaPorExtenso(noPorto(agora));
}

/** A hora do instante, no porto, com a preposição: "às 10h", "à 1h", "ao meio-dia", "à meia-noite". */
export function aHora(instante: string): string {
  const lida = noPorto(instante);
  const texto = horaPorExtenso(lida);

  if (texto === 'meio-dia') {
    return 'ao meio-dia';
  }

  return texto === 'meia-noite' || lida.hora <= 1 ? `à ${texto}` : `às ${texto}`;
}

/** "hoje, às 10h"; "amanhã, à 1h"; "segunda-feira, 9 de novembro, às 10h". */
export function quando(instante: string, hoje: string): string {
  const dia = diaNoPorto(instante);

  return `${perto(dia, hoje) ?? diaPorExtenso(dia)}, ${aHora(instante)}`;
}

/** "30/11/2026". */
export function formatarData(dia: string): string {
  const [ano, mes, numero] = dia.split('-');

  return `${numero ?? ''}/${mes ?? ''}/${ano ?? ''}`;
}

/** "2026-11" → "novembro de 2026". */
export function mesPorExtenso(mes: string): string {
  return MesPorExtenso.format(comoData(`${mes}-01`));
}

/** "2026-11" → "2026-12", ou para trás. */
export function somarMeses(mes: string, meses: number): string {
  const data = comoData(`${mes}-01`);
  data.setUTCMonth(data.getUTCMonth() + meses);

  return data.toISOString().slice(0, 7);
}

/** "R$ 260,00". */
export function formatarDinheiro(valor: number): string {
  return Dinheiro.format(valor);
}
