import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';
import type { Esquemas } from '../api/cliente';

/**
 * O que a API responde sem o teste pedir: nenhum recado. Toda tela do
 * trabalhador pergunta quantos recados novos ele tem, para a aba; quem trata
 * dos recados diz outra coisa, com servidor.use(...).
 */
const semRecados: Esquemas['MeusAvisosResposta'] = { naoLidos: 0, avisos: [] };

/** A API dos testes: cada teste diz o que ela responde, com servidor.use(...). */
export const servidor = setupServer(http.get('/api/eu/avisos', () => HttpResponse.json(semRecados)));
