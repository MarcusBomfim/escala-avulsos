import { describe, expect, test } from 'vitest';
import { novembroDoAntonio } from '../testes/exemplosDoMes';
import { faltasNaFrase, frequenciaNaFrase } from './frases';

const { frequencia } = novembroDoAntonio;

describe('o mês dito para o trabalhador', () => {
  test('a frequência, com o que falta para o mínimo', () => {
    expect(frequenciaNaFrase(frequencia)).toBe(
      'Você se habilitou 1 vez neste mês. O mínimo é 12: faltam 11.',
    );
    expect(frequenciaNaFrase({ ...frequencia, habilitacoes: 11, habilitacoesQueFaltam: 1 })).toBe(
      'Você se habilitou 11 vezes neste mês. O mínimo é 12: falta 1.',
    );
    expect(frequenciaNaFrase({ ...frequencia, habilitacoes: 12, habilitacoesQueFaltam: 0 })).toBe(
      'Você se habilitou 12 vezes neste mês. O mínimo é 12, e você já chegou lá.',
    );
  });

  test('com dias impedidos, o mínimo diz que já os descontou', () => {
    expect(
      frequenciaNaFrase({
        ...frequencia,
        diasImpedidos: 10,
        minimoDeHabilitacoes: 8,
        habilitacoesQueFaltam: 7,
      }),
    ).toBe(
      'Você se habilitou 1 vez neste mês. O mínimo é 8, contados só os dias em que você podia trabalhar: faltam 7.',
    );
  });

  test('as faltas, de quantas são aceitas', () => {
    expect(faltasNaFrase(frequencia)).toBe('0 de 2 aceitas');
    expect(faltasNaFrase({ ...frequencia, faltas: 3 })).toBe('3 de 2 aceitas — acima do limite');
    expect(faltasNaFrase({ ...frequencia, maximoDeFaltas: 1 })).toBe('0 de 1 aceita');
  });
});
