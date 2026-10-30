import type { Esquemas } from '../api/cliente';

/** O terno da tarde de quinta, da Cais Forte: um sinaleiro e dois estivadores. */
function ternoDaCaisForte(
  escalados: Esquemas['FuncaoNoQuadro'][],
  situacao: Esquemas['SituacaoDoTernoNoQuadro'],
): Esquemas['TernoNoQuadro'] {
  const escaladas = escalados.reduce((total, funcao) => total + funcao.escalados.length, 0);

  return {
    requisicao: '0199e3b4-0000-7000-8000-0000000000a1',
    operador: { nome: 'Cais Forte Operações Portuárias', cnpj: '11.222.333/0001-81' },
    navio: { nome: 'Atlântico Sul', imo: 'IMO 9876543' },
    local: { terminal: 'Terminal Cais Leste', berco: '4' },
    terno: 1,
    pedidas: 3,
    escaladas,
    emAberto: situacao === 'AguardandoAEscala' || situacao === 'Incompleto' ? 3 - escaladas : 0,
    situacao,
    funcoes: escalados,
  };
}

const periodo: Esquemas['PeriodoResposta'] = {
  dia: '2026-11-05',
  turno: 'Tarde',
  descricao: '05/11/2026, das 13h às 19h',
  inicio: '2026-11-05T16:00:00+00:00',
  fim: '2026-11-05T22:00:00+00:00',
};

/** Quinta às 9h: a habilitação da estiva aberta, com seis habilitados, e o terno esperando a escala. */
export const quadroComAHabilitacaoAberta: Esquemas['QuadroResposta'] = {
  periodo,
  categorias: [
    {
      categoria: 'Estiva',
      situacao: 'HabilitacaoAberta',
      abreEm: '2026-11-05T04:00:00+00:00',
      fechaEm: '2026-11-05T13:00:00+00:00',
      habilitados: 6,
      ternos: [
        ternoDaCaisForte(
          [
            { funcao: 'Sinaleiro', pedidas: 1, escalados: [] },
            { funcao: 'Estivador', pedidas: 2, escalados: [] },
          ],
          'AguardandoAEscala',
        ),
      ],
    },
  ],
};

/** Quinta às 11h: a escala saiu, e o Edson desistiu sem ninguém na espera — falta um estivador. */
export const quadroDepoisDaEscala: Esquemas['QuadroResposta'] = {
  periodo,
  categorias: [
    {
      categoria: 'Estiva',
      situacao: 'Escalada',
      abreEm: '2026-11-05T04:00:00+00:00',
      fechaEm: '2026-11-05T13:00:00+00:00',
      habilitados: 6,
      ternos: [
        ternoDaCaisForte(
          [
            { funcao: 'Sinaleiro', pedidas: 1, escalados: [{ matricula: 4127, nome: 'Antônio Ferreira' }] },
            { funcao: 'Estivador', pedidas: 2, escalados: [{ matricula: 4130, nome: 'Benedito Lima' }] },
          ],
          'Incompleto',
        ),
      ],
    },
  ],
};
