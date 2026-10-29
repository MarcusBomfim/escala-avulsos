import { describe, expect, test } from 'vitest';
import {
  doAntonio,
  doAntonioQueTrabalhou,
  doCarlos,
  doCarlosSubstituto,
  doEdsonQueDesistiu,
  noiteDeSexta,
  vaga,
} from '../testes/exemplos';
import { esperaNaTela, onde, situacaoNaTela, textoParaOuvir } from './frases';

const Quinta = '2026-11-05';

describe('a escala dita para o trabalhador', () => {
  test('cada situação tem o seu selo, com a palavra e a cor', () => {
    expect(situacaoNaTela(doAntonio)).toEqual({ tom: 'sucesso', selo: 'Você vai trabalhar' });
    expect(situacaoNaTela(doCarlosSubstituto)).toEqual({ tom: 'sucesso', selo: 'Você entrou na escala' });
    expect(situacaoNaTela(doCarlos)).toEqual({ tom: 'atencao', selo: 'Você ficou de fora' });
    expect(situacaoNaTela(doEdsonQueDesistiu)).toEqual({ tom: 'neutro', selo: 'Você desistiu da vaga' });
    expect(situacaoNaTela(doAntonioQueTrabalhou)).toEqual({
      tom: 'sucesso',
      selo: 'Presença confirmada',
    });

    const comFalta = {
      ...doAntonio,
      situacao: { ...doAntonio.situacao, comparecimento: 'Falta' as const },
    };
    expect(situacaoNaTela(comFalta)).toEqual({ tom: 'perigo', selo: 'Falta' });
  });

  test('o lugar, com o berço quando há um', () => {
    expect(onde(vaga('Sinaleiro'))).toBe('Terminal Cais Leste, berço 4');
    expect(onde({ ...vaga('Sinaleiro'), local: { terminal: 'Armazém 12', berco: null } })).toBe('Armazém 12');
  });

  test('a espera, uma função ou mais', () => {
    expect(esperaNaTela(doCarlos.espera)).toBe(
      'Você é o 1º da espera de sinaleiro e o 1º da espera de estivador.',
    );
    expect(esperaNaTela([{ funcao: 'Estivador', posicao: 3 }])).toBe('Você é o 3º da espera de estivador.');
    expect(esperaNaTela([])).toBe('');
  });

  test('para ouvir: o dia, o horário por extenso e a vaga', () => {
    expect(textoParaOuvir(doAntonio, Quinta)).toBe(
      'Hoje, quinta-feira, 5 de novembro, da uma da tarde às sete da noite. Você vai trabalhar como sinaleiro, no terno 1 do navio Atlântico Sul. Local: Terminal Cais Leste, berço 4.',
    );
    expect(textoParaOuvir(doCarlosSubstituto, '2026-11-04')).toBe(
      'Amanhã, quinta-feira, 5 de novembro, da uma da tarde às sete da noite. Você entrou na escala no lugar de quem desistiu: vai trabalhar como estivador, no terno 1 do navio Atlântico Sul. Local: Terminal Cais Leste, berço 4.',
    );
  });

  test('para ouvir: de fora, os motivos e a espera, com o lugar dito por extenso', () => {
    expect(textoParaOuvir({ ...doCarlos, periodo: noiteDeSexta }, Quinta)).toBe(
      'Amanhã, sexta-feira, 6 de novembro, das sete da noite à uma da manhã. Você ficou de fora. Como sinaleiro, o rodízio parou 1 posição antes da sua vez. Como estivador, o rodízio parou 1 posição antes da sua vez. Você é o primeiro da espera de sinaleiro e o primeiro da espera de estivador.',
    );
  });

  test('para ouvir: a desistência, com a hora', () => {
    expect(textoParaOuvir(doEdsonQueDesistiu, Quinta)).toBe(
      'Hoje, quinta-feira, 5 de novembro, da uma da tarde às sete da noite. Você desistiu da vaga de estivador hoje, às 11h.',
    );
  });
});
