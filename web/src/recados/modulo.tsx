import type { Modulo } from '../aplicacao/modulos';
import { ContagemDosRecados } from './ContagemDosRecados';
import { TelaDosRecados } from './TelaDosRecados';

export const modulo: Modulo = {
  rotas: [{ path: 'recados', element: <TelaDosRecados /> }],
  menu: { rotulo: 'Recados', icone: 'recados', caminho: '/recados', ordem: 3, Contagem: ContagemDosRecados },
};
