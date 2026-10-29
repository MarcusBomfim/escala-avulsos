import { describe, expect, test } from 'vitest';
import { cadastroDoAntonio } from '../testes/exemplosDoCadastro';
import { validadeDaQualificacao, validadeDoExame } from './avisos';

const { aso } = cadastroDoAntonio;

describe('o que vence no cadastro, dito antes', () => {
  test('o exame em dia, perto de vencer, no último dia e vencido', () => {
    expect(validadeDoExame(aso, '2026-10-01')).toEqual({
      tom: 'sucesso',
      selo: 'Em dia',
      frase: 'O exame vale até 30/11/2026.',
    });
    expect(validadeDoExame(aso, '2026-10-31')).toEqual({
      tom: 'atencao',
      selo: 'Vence logo',
      frase:
        'O exame vale até 30/11/2026 — vence em 30 dias. Renove antes, para não ficar de fora da escala.',
    });
    expect(validadeDoExame(aso, '2026-11-29').frase).toContain('vence em 1 dia');
    expect(validadeDoExame(aso, '2026-11-30').frase).toContain('vence hoje');
    expect(validadeDoExame(aso, '2026-12-01')).toEqual({
      tom: 'perigo',
      selo: 'Vencido',
      frase: 'O exame venceu em 30/11/2026: sem renovar, você não entra na escala.',
    });
  });

  test('sem exame, diz o que fazer', () => {
    expect(validadeDoExame(null, '2026-11-05').frase).toBe(
      'Não há exame médico registrado: sem ele, você não entra na escala. Procure o órgão gestor.',
    );
  });

  test('a qualificação, com ou sem prazo de reciclagem', () => {
    const [estivador, sinaleiro] = cadastroDoAntonio.qualificacoes;

    expect(estivador && validadeDaQualificacao(estivador, '2026-11-05').frase).toBe(
      'Desde 01/03/1994, sem prazo para renovar.',
    );
    expect(sinaleiro && validadeDaQualificacao(sinaleiro, '2026-11-05').frase).toBe(
      'Desde 10/05/2019. A reciclagem vale até 31/05/2027.',
    );
  });
});
