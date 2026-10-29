import type { Modulo } from '../aplicacao/modulos';
import { TelaDaHabilitacao } from './TelaDaHabilitacao';

export const modulo: Modulo = {
  rotas: [{ path: 'habilitacao', element: <TelaDaHabilitacao /> }],
  menu: { rotulo: 'Habilitar', icone: 'habilitar', caminho: '/habilitacao', ordem: 2 },
};
