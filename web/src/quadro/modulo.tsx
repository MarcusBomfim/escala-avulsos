import type { Modulo } from '../aplicacao/modulos';
import { TelaDoQuadro } from './TelaDoQuadro';

export const modulo: Modulo = {
  rotas: [{ path: 'quadro', element: <TelaDoQuadro /> }],
  menu: {
    rotulo: 'Quadro',
    icone: 'quadro',
    caminho: '/quadro',
    ordem: 1,
    papeis: ['escalador', 'operador'],
  },
};
