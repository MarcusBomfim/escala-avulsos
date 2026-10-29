import type { Esquemas } from '../api/cliente';

/*
 * O cadastro do Antônio, como a API o devolve em /api/eu: estivador
 * registrado desde 1994, sinaleiro com a reciclagem até maio de 2027, o
 * exame médico até 30/11/2026, e as férias de janeiro marcadas.
 */

export const cadastroDoAntonio: Esquemas['TrabalhadorResposta'] = {
  matricula: 4127,
  nome: 'Antônio Ferreira',
  cpf: '529.984.127-26',
  categoria: 'Estiva',
  inscricao: 'Registrado',
  qualificacoes: [
    { funcao: 'Estivador', desde: '1994-03-01', validaAte: null },
    { funcao: 'Sinaleiro', desde: '2019-05-10', validaAte: '2027-05-31' },
  ],
  aso: { emitidoEm: '2025-12-01', validoAte: '2026-11-30' },
  impedimentos: [
    {
      tipo: 'Ferias',
      de: '2027-01-04',
      ate: '2027-02-02',
      observacao: null,
      descricao: 'férias de 04/01/2027 a 02/02/2027',
    },
  ],
  cancelamento: null,
};
