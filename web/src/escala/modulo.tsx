import type { Modulo } from '../aplicacao/modulos';
import { TelaDaMinhaEscala } from './TelaDaMinhaEscala';

export const modulo: Modulo = {
  rotas: [{ index: true, element: <TelaDaMinhaEscala /> }],
  menu: { rotulo: 'Minha escala', icone: 'escala', caminho: '/', ordem: 1 },
};
