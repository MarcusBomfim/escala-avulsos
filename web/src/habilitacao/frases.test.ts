import { describe, expect, test } from 'vitest';
import {
  manhaDeDezembro,
  noiteQueNaoAbriu,
  tardeAberta,
  tardeHabilitado,
} from '../testes/exemplosDaHabilitacao';
import { funcoesNaFrase, motivosNaFrase, situacaoDaHabilitacao } from './frases';

const QuintaAs8h = new Date('2026-11-05T08:00:00-03:00');
const QuintaAs11h = new Date('2026-11-05T11:00:00-03:00');
const Quinta = '2026-11-05';

describe('a habilitação dita para o trabalhador', () => {
  test('as funções, juntas como se fala', () => {
    expect(funcoesNaFrase(['Sinaleiro'])).toBe('sinaleiro');
    expect(funcoesNaFrase(['Sinaleiro', 'Estivador'])).toBe('sinaleiro e estivador');
    expect(funcoesNaFrase(['Sinaleiro', 'Estivador', 'Contramestre de terno'])).toBe(
      'sinaleiro, estivador e contramestre de terno',
    );
  });

  test('antes de abrir, aberta, e habilitado: cada momento com o seu prazo', () => {
    expect(situacaoDaHabilitacao(noiteQueNaoAbriu, QuintaAs8h, Quinta)).toEqual({
      tom: 'neutro',
      selo: 'Ainda não abriu',
      frase: 'A habilitação abre amanhã, às 7h, e fecha amanhã, às 16h.',
    });
    expect(situacaoDaHabilitacao(tardeAberta, QuintaAs8h, Quinta)).toEqual({
      tom: 'atencao',
      selo: 'Habilitação aberta',
      frase: 'A habilitação fecha hoje, às 10h.',
    });
    expect(situacaoDaHabilitacao(tardeHabilitado, QuintaAs8h, Quinta)).toEqual({
      tom: 'sucesso',
      selo: 'Você está habilitado',
      frase: 'Como sinaleiro e estivador. Pode trocar as funções ou desistir até hoje, às 10h.',
    });
  });

  test('depois de fechar, com ou sem habilitação', () => {
    const fechada = { ...tardeAberta, janelaAberta: false };

    expect(situacaoDaHabilitacao(fechada, QuintaAs11h, Quinta).frase).toBe(
      'A habilitação fechou hoje, às 10h, e você não se habilitou para este período.',
    );
    expect(
      situacaoDaHabilitacao({ ...tardeHabilitado, janelaAberta: false }, QuintaAs11h, Quinta).frase,
    ).toBe(
      'Como sinaleiro e estivador. A habilitação fechou hoje, às 10h: a escala sai em seguida, na tela Minha escala.',
    );
  });

  test('a função que ele não pode exercer, com o motivo do domínio', () => {
    const [estivador] = manhaDeDezembro.opcoes;

    expect(estivador && motivosNaFrase(estivador)).toBe(
      'Não pode: o exame médico (ASO) vale só até 30/11/2026.',
    );
  });
});
