import type { Esquemas } from '../api/cliente';

/*
 * Como cada valor dos enums da API se escreve na tela, e como se fala. A API
 * manda o nome do enum — "Manha", "FaltaJustificada" —, que é contrato, e não
 * texto para gente. Um enum novo no contrato sem rótulo aqui é erro de
 * compilação.
 */

type Turno = Esquemas['Turno'];

export interface DizerDoTurno {
  /** "Tarde". */
  readonly nome: string;
  /** "das 13h às 19h", como se escreve. */
  readonly horario: string;
  /** "da uma da tarde às sete da noite", como se fala — "13h" lido por máquina vira "treze agá". */
  readonly falado: string;
}

export const Turnos: Readonly<Record<Turno, DizerDoTurno>> = {
  Madrugada: { nome: 'Madrugada', horario: 'da 1h às 7h', falado: 'da uma às sete da manhã' },
  Manha: { nome: 'Manhã', horario: 'das 7h às 13h', falado: 'das sete da manhã à uma da tarde' },
  Tarde: { nome: 'Tarde', horario: 'das 13h às 19h', falado: 'da uma da tarde às sete da noite' },
  Noite: { nome: 'Noite', horario: 'das 19h à 1h', falado: 'das sete da noite à uma da manhã' },
};

export const Categorias: Readonly<Record<Esquemas['Categoria'], string>> = {
  Capatazia: 'capatazia',
  Estiva: 'estiva',
  ConferenciaDeCarga: 'conferência de carga',
  ConsertoDeCarga: 'conserto de carga',
  VigilanciaDeEmbarcacoes: 'vigilância de embarcações',
  Bloco: 'bloco',
};

export const Inscricoes: Readonly<Record<Esquemas['Inscricao'], string>> = {
  Cadastrado: 'cadastrado',
  Registrado: 'registrado',
};

export const Impedimentos: Readonly<Record<Esquemas['TipoDeImpedimento'], string>> = {
  Ferias: 'Férias',
  Afastamento: 'Afastamento',
  Suspensao: 'Suspensão',
};

export const SituacoesDaFrequencia: Readonly<Record<Esquemas['SituacaoDaFrequencia'], string>> = {
  Cumprida: 'Frequência cumprida',
  EmAndamento: 'Mês em andamento',
  Descumprida: 'Frequência não cumprida',
};

/** A função no meio da frase: "como sinaleiro", e não "como Sinaleiro". */
export function funcaoNaFrase(funcao: string): string {
  return funcao.charAt(0).toLowerCase() + funcao.slice(1);
}

const Ordinais = [
  'primeiro',
  'segundo',
  'terceiro',
  'quarto',
  'quinto',
  'sexto',
  'sétimo',
  'oitavo',
  'nono',
  'décimo',
];

/** "1º" na tela; "primeiro" na voz, que lê "1º" de jeitos diferentes em cada celular. */
export function ordinal(posicao: number, { falado = false }: { readonly falado?: boolean } = {}): string {
  return falado ? (Ordinais[posicao - 1] ?? `número ${posicao}`) : `${posicao}º`;
}
