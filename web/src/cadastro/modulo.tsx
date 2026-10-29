import type { Modulo } from '../aplicacao/modulos';
import { TelaDoMeuCadastro } from './TelaDoMeuCadastro';

export const modulo: Modulo = {
  rotas: [{ path: 'meus-dados', element: <TelaDoMeuCadastro /> }],
  menu: { rotulo: 'Meus dados', icone: 'dados', caminho: '/meus-dados', ordem: 4 },
};
