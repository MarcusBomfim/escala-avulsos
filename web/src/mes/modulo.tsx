import type { Modulo } from '../aplicacao/modulos';
import { TelaDoMes } from './TelaDoMes';

export const modulo: Modulo = {
  rotas: [
    { path: 'mes', element: <TelaDoMes /> },
    { path: 'mes/:mes', element: <TelaDoMes /> },
  ],
  menu: { rotulo: 'Meu mês', icone: 'mes', caminho: '/mes', ordem: 3 },
};
