import type { Esquemas } from '../api/cliente';

/** Os recados do Antônio na quinta: a escala que saiu, às 10h, e o exame que vence, avisado às 9h. */
export const recadosDoAntonio: Esquemas['MeusAvisosResposta'] = {
  naoLidos: 2,
  avisos: [
    {
      tipo: 'VaiTrabalhar',
      titulo: 'Você vai trabalhar',
      texto:
        'Quinta-feira, 5 de novembro, das 13h às 19h: sinaleiro no terno 1 do navio Atlântico Sul, no Terminal Cais Leste, berço 4.',
      criadoEm: '2026-11-05T13:00:00+00:00',
      lido: false,
    },
    {
      tipo: 'ExameVaiVencer',
      titulo: 'O exame médico vai vencer',
      texto: 'O seu exame médico (ASO) vale até 30/11/2026. Sem ele em dia, você não entra na escala.',
      criadoEm: '2026-11-05T12:00:00+00:00',
      lido: false,
    },
    {
      tipo: 'FicouDeFora',
      titulo: 'Você ficou de fora da escala',
      texto: 'Quarta-feira, 4 de novembro, das 7h às 13h: o rodízio parou 1 posição antes da sua vez.',
      criadoEm: '2026-11-04T07:00:00+00:00',
      lido: true,
    },
  ],
};
