import type { Esquemas } from '../api/cliente';

/*
 * A tarde de quinta, 05/11/2026, do exemplo do README, como a API a devolve
 * para cada um: o Antônio sinaleiro, o Carlos de fora e primeiro da espera,
 * o Edson que desistiu e o Carlos que entrou no lugar dele. Tipada pelo
 * contrato: se a API mudar o formato, estes exemplos deixam de compilar.
 */

export const Ids = {
  daTarde: '01a16700-0000-7000-8000-00000000e001',
  daNoite: '01a16700-0000-7000-8000-00000000e002',
  requisicao: '01a16700-0000-7000-8000-00000000a001',
} as const;

export const tardeDeQuinta: Esquemas['PeriodoResposta'] = {
  dia: '2026-11-05',
  turno: 'Tarde',
  descricao: '05/11/2026, das 13h às 19h',
  inicio: '2026-11-05T13:00:00-03:00',
  fim: '2026-11-05T19:00:00-03:00',
};

export const noiteDeSexta: Esquemas['PeriodoResposta'] = {
  dia: '2026-11-06',
  turno: 'Noite',
  descricao: '06/11/2026, das 19h à 1h',
  inicio: '2026-11-06T19:00:00-03:00',
  fim: '2026-11-07T01:00:00-03:00',
};

export function vaga(funcao: string, terno = 1): Esquemas['VagaResposta'] {
  return {
    funcao,
    terno,
    requisicao: Ids.requisicao,
    navio: { nome: 'Atlântico Sul', imo: 'IMO 9876543' },
    local: { terminal: 'Terminal Cais Leste', berco: '4' },
    operador: { nome: 'Cais Forte Operações Portuárias', cnpj: '11.222.333/0001-81' },
  };
}

function situacao(
  matricula: number,
  nome: string,
  frase: string,
  outras: Partial<Esquemas['SituacaoResposta']> = {},
): Esquemas['SituacaoResposta'] {
  return {
    matricula,
    nome,
    frase,
    vaga: null,
    substituto: false,
    vagaDesistida: null,
    desistiuEm: null,
    comparecimento: 'Aguardando',
    presenteDesde: null,
    justificativaDaFalta: null,
    motivos: [],
    ...outras,
  };
}

function minha(
  situacaoNaEscala: Esquemas['SituacaoResposta'],
  outras: Partial<Esquemas['MinhaEscalaResposta']> = {},
): Esquemas['MinhaEscalaResposta'] {
  return {
    escala: Ids.daTarde,
    categoria: 'Estiva',
    periodo: tardeDeQuinta,
    encerrada: false,
    situacao: situacaoNaEscala,
    espera: [],
    ...outras,
  };
}

/** O Antônio, sinaleiro no terno 1. */
export const doAntonio = minha(
  situacao(
    4127,
    'Antônio Ferreira',
    'Antônio Ferreira (matrícula 4127) está na escala: sinaleiro no terno 1 do navio Atlântico Sul (IMO 9876543), no Terminal Cais Leste, berço 4, em 05/11/2026, das 13h às 19h.',
    { vaga: vaga('Sinaleiro') },
  ),
);

/** O Carlos, de fora: o rodízio parou uma posição antes dele nas duas funções. */
export const doCarlos = minha(
  situacao(
    4152,
    'Carlos Mota',
    'Carlos Mota (matrícula 4152) ficou fora da escala: como sinaleiro, o rodízio parou 1 posição antes da sua vez; como estivador, o rodízio parou 1 posição antes da sua vez.',
    {
      motivos: [
        {
          tipo: 'NaoAlcancado',
          funcao: 'Sinaleiro',
          descricao: 'como sinaleiro, o rodízio parou 1 posição antes da sua vez',
        },
        {
          tipo: 'NaoAlcancado',
          funcao: 'Estivador',
          descricao: 'como estivador, o rodízio parou 1 posição antes da sua vez',
        },
      ],
    },
  ),
  {
    espera: [
      { funcao: 'Sinaleiro', posicao: 1 },
      { funcao: 'Estivador', posicao: 1 },
    ],
  },
);

/** O Edson, depois de desistir às 11h. */
export const doEdsonQueDesistiu = minha(
  situacao(
    4101,
    'Edson Prado',
    'Edson Prado (matrícula 4101) desistiu da vaga de estivador em 05/11/2026, às 11h.',
    { vagaDesistida: vaga('Estivador'), desistiuEm: '2026-11-05T11:00:00-03:00' },
  ),
);

/** O Edson, estivador no terno 1, antes de desistir. */
export const doEdson = minha(
  situacao(
    4101,
    'Edson Prado',
    'Edson Prado (matrícula 4101) está na escala: estivador no terno 1 do navio Atlântico Sul (IMO 9876543), no Terminal Cais Leste, berço 4, em 05/11/2026, das 13h às 19h.',
    { vaga: vaga('Estivador') },
  ),
);

/** O Carlos, no lugar do Edson. */
export const doCarlosSubstituto = minha(
  situacao(
    4152,
    'Carlos Mota',
    'Carlos Mota (matrícula 4152) está na escala, no lugar de quem desistiu: estivador no terno 1 do navio Atlântico Sul (IMO 9876543), no Terminal Cais Leste, berço 4, em 05/11/2026, das 13h às 19h.',
    { vaga: vaga('Estivador'), substituto: true },
  ),
);

/** O Antônio no fim da tarde: presença confirmada às 12h40, escala encerrada. */
export const doAntonioQueTrabalhou: Esquemas['MinhaEscalaResposta'] = {
  ...doAntonio,
  encerrada: true,
  situacao: {
    ...doAntonio.situacao,
    comparecimento: 'Presente',
    presenteDesde: '2026-11-05T12:40:00-03:00',
  },
};
