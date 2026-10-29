import { test as preparacao } from '@playwright/test';
import {
  acertarORelogio,
  cadastrarEstivador,
  doEscalador,
  doOperador,
  doTrabalhador,
  enviar,
} from '../apoio/api';
import { Quinta, Trabalhadores } from '../apoio/tardeDeQuinta';

/*
 * A tarde de quinta, 05/11/2026, do exemplo do README, até a véspera de o
 * Antônio se habilitar — o que ele faz pelo celular, no primeiro teste:
 *
 * - na quarta às 10h, o órgão gestor cadastra os seis, a Cais Forte pede um
 *   sinaleiro e dois estivadores para o Atlântico Sul, e a escalação abre;
 * - na quinta às 6h, os outros cinco se habilitam.
 */

preparacao('a tarde de quinta entra pela API', async () => {
  await acertarORelogio('2026-11-04T10:00:00-03:00');
  const escalador = await doEscalador();
  const { antonio, benedito, carlos, dirceu, edson, joao } = Trabalhadores;

  await cadastrarEstivador(escalador, antonio.matricula, antonio.nome, { outrasFuncoes: ['Sinaleiro'] });
  await cadastrarEstivador(escalador, benedito.matricula, benedito.nome);
  await cadastrarEstivador(escalador, carlos.matricula, carlos.nome, { outrasFuncoes: ['Sinaleiro'] });
  await cadastrarEstivador(escalador, dirceu.matricula, dirceu.nome);
  await cadastrarEstivador(escalador, edson.matricula, edson.nome);
  await cadastrarEstivador(escalador, joao.matricula, joao.nome, { inscricao: 'Cadastrado' });

  const operador = await doOperador();
  await enviar(operador, 'POST', '/api/requisicoes', {
    chave: true,
    corpo: {
      categoria: 'Estiva',
      navio: { nome: 'Atlântico Sul', imo: 'IMO 9876543' },
      local: { terminal: 'Terminal Cais Leste', berco: '4' },
      dia: Quinta,
      turno: 'Tarde',
      ternos: [
        [
          { funcao: 'Sinaleiro', quantidade: 1 },
          { funcao: 'Estivador', quantidade: 2 },
        ],
      ],
    },
  });
  await enviar(escalador, 'PUT', `/api/escalacoes/Estiva/${Quinta}/Tarde`);

  await acertarORelogio('2026-11-05T06:00:00-03:00');
  for (const [quem, funcoes] of [
    [benedito, ['Estivador']],
    [carlos, ['Sinaleiro', 'Estivador']],
    [dirceu, ['Estivador']],
    [edson, ['Estivador']],
    [joao, ['Estivador']],
  ] as const) {
    const trabalhador = await doTrabalhador(quem.matricula, quem.nome);
    await enviar(trabalhador, 'PUT', `/api/eu/habilitacoes/${Quinta}/Tarde`, { corpo: { funcoes } });
    await trabalhador.dispose();
  }

  await escalador.dispose();
  await operador.dispose();
});
