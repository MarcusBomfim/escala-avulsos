import type { Esquemas } from '../api/cliente';
import { tardeDeQuinta } from './exemplos';

/*
 * O mês do Antônio, na sexta, 06/11/2026, como a API o devolve: a tarde de
 * quinta trabalhada como sinaleiro, a R$ 260,00, e a frequência do mês em
 * andamento.
 */

export const novembroDoAntonio: Esquemas['MeuMesResposta'] = {
  mes: '11/2026',
  frequencia: {
    descricao:
      'Antônio Ferreira (matrícula 4127), em 11/2026: 1 habilitação, e o mínimo é 12 — faltam 11 até 30/11/2026. Trabalhou 1 período; 0 faltas sem justificativa, de 2 aceitas.',
    situacao: 'EmAndamento',
    habilitacoes: 1,
    minimoDeHabilitacoes: 12,
    habilitacoesQueFaltam: 11,
    trabalhados: 1,
    faltas: 0,
    faltasJustificadas: 0,
    maximoDeFaltas: 2,
    desistencias: 0,
    diasImpedidos: 0,
  },
  extrato: {
    descricao:
      'Antônio Ferreira (matrícula 4127), em 11/2026: 1 período, R$ 260,00; férias com 1/3, R$ 28,89; 13º, R$ 21,67; FGTS depositado, R$ 24,84.',
    linhas: [
      {
        descricao:
          '05/11/2026, das 13h às 19h — sinaleiro no navio Atlântico Sul (IMO 9876543): R$ 260,00, pago até 07/11/2026, às 19h',
        periodo: tardeDeQuinta,
        funcao: 'Sinaleiro',
        navio: { nome: 'Atlântico Sul', imo: 'IMO 9876543' },
        terno: 1,
        valor: 260,
        adicionalNoturno: 0,
        total: 260,
        pagoAte: '2026-11-07T19:00:00-03:00',
      },
    ],
    remuneracao: 260,
    ferias: 28.89,
    decimoTerceiro: 21.67,
    fgts: 24.84,
  },
};

/** Outubro, sem nenhum período trabalhado, e com a frequência que ficou faltando. */
export const outubroVazio: Esquemas['MeuMesResposta'] = {
  mes: '10/2026',
  frequencia: {
    descricao:
      'Antônio Ferreira (matrícula 4127), em 10/2026: 0 habilitações, e o mínimo era 12 — ficaram faltando 12.',
    situacao: 'Descumprida',
    habilitacoes: 0,
    minimoDeHabilitacoes: 12,
    habilitacoesQueFaltam: 12,
    trabalhados: 0,
    faltas: 0,
    faltasJustificadas: 0,
    maximoDeFaltas: 2,
    desistencias: 0,
    diasImpedidos: 0,
  },
  extrato: {
    descricao: 'Antônio Ferreira (matrícula 4127), em 10/2026: nenhum período trabalhado.',
    linhas: [],
    remuneracao: 0,
    ferias: 0,
    decimoTerceiro: 0,
    fgts: 0,
  },
};
