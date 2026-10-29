import { parseArgs } from 'node:util';
import { tokenDoTrabalhador } from './identidade.ts';

/*
 * Um token de trabalhador para entrar pelo navegador na API do ensaio, que
 * aceita a chave dos testes. Roda com o Node 24, sem instalar nada:
 *
 *   node e2e/apoio/token.ts --matricula 4127 --nome "Antônio Ferreira"
 */

const { values } = parseArgs({
  options: {
    matricula: { type: 'string', default: '4127' },
    nome: { type: 'string', default: 'Antônio Ferreira' },
  },
});

console.log(tokenDoTrabalhador(Number(values.matricula), values.nome));
