import type { Esquemas } from '../api/cliente';
import { noiteDeSexta, tardeDeQuinta } from './exemplos';

/*
 * As escalações em que o Antônio pode se habilitar, como a API as devolve: a
 * tarde de quinta, com a habilitação aberta da 1h às 10h; a noite de sexta,
 * que ainda não abriu; e a manhã de 1º de dezembro, quando o exame dele já
 * terá vencido.
 */

const DoAntonio: Esquemas['OpcaoDeFuncao'][] = [
  { funcao: 'Estivador', pode: true, motivos: [] },
  { funcao: 'Sinaleiro', pode: true, motivos: [] },
];

export const tardeAberta: Esquemas['MinhaHabilitacaoResposta'] = {
  periodo: tardeDeQuinta,
  abreEm: '2026-11-05T01:00:00-03:00',
  fechaEm: '2026-11-05T10:00:00-03:00',
  janelaAberta: true,
  habilitado: false,
  funcoes: [],
  em: null,
  opcoes: DoAntonio,
};

export const tardeHabilitado: Esquemas['MinhaHabilitacaoResposta'] = {
  ...tardeAberta,
  habilitado: true,
  funcoes: ['Sinaleiro', 'Estivador'],
  em: '2026-11-05T08:00:00-03:00',
};

export const noiteQueNaoAbriu: Esquemas['MinhaHabilitacaoResposta'] = {
  periodo: noiteDeSexta,
  abreEm: '2026-11-06T07:00:00-03:00',
  fechaEm: '2026-11-06T16:00:00-03:00',
  janelaAberta: false,
  habilitado: false,
  funcoes: [],
  em: null,
  opcoes: DoAntonio,
};

const ExameVencido = 'o exame médico (ASO) vale só até 30/11/2026';

export const manhaDeDezembro: Esquemas['MinhaHabilitacaoResposta'] = {
  periodo: {
    dia: '2026-12-01',
    turno: 'Manha',
    descricao: '01/12/2026, das 7h às 13h',
    inicio: '2026-12-01T07:00:00-03:00',
    fim: '2026-12-01T13:00:00-03:00',
  },
  abreEm: '2026-11-30T19:00:00-03:00',
  fechaEm: '2026-12-01T04:00:00-03:00',
  janelaAberta: true,
  habilitado: false,
  funcoes: [],
  em: null,
  opcoes: [
    { funcao: 'Estivador', pode: false, motivos: [ExameVencido] },
    { funcao: 'Sinaleiro', pode: false, motivos: [ExameVencido] },
  ],
};
