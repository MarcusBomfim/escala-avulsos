import { describe, expect, test } from 'vitest';
import {
  aHora,
  diaNoPorto,
  diaParaQuemLe,
  diaPorExtenso,
  formatarData,
  formatarDinheiro,
  hojeNoPorto,
  horaNoPorto,
  mesPorExtenso,
  quando,
  somarDias,
  somarMeses,
} from './datas';

// Os testes rodam no fuso de Manaus (vite.config.ts), uma hora atrás do porto.

describe('os dias e as horas do porto', () => {
  test('hoje é o dia do porto, e não o do aparelho', () => {
    // 23h30 em Manaus já é 0h30 em Santos.
    expect(hojeNoPorto(new Date('2026-11-05T03:30:00Z'))).toBe('2026-11-05');
    expect(hojeNoPorto(new Date('2026-11-05T02:59:00Z'))).toBe('2026-11-04');
    expect(diaNoPorto('2026-11-06T03:00:00+00:00')).toBe('2026-11-06');
  });

  test('o dia por extenso, e perto de hoje dito como se fala', () => {
    expect(diaPorExtenso('2026-11-05')).toBe('quinta-feira, 5 de novembro');
    expect(diaParaQuemLe('2026-11-05', '2026-11-05')).toBe('Hoje, quinta-feira, 5 de novembro');
    expect(diaParaQuemLe('2026-11-06', '2026-11-05')).toBe('Amanhã, sexta-feira, 6 de novembro');
    expect(diaParaQuemLe('2026-11-04', '2026-11-05')).toBe('Ontem, quarta-feira, 4 de novembro');
    expect(diaParaQuemLe('2026-11-07', '2026-11-05')).toBe('Sábado, 7 de novembro');
    expect(() => diaPorExtenso('05/11/2026')).toThrow('aaaa-mm-dd');
  });

  test('a hora no porto, com a preposição certa', () => {
    expect(aHora('2026-11-05T13:00:00+00:00')).toBe('às 10h');
    expect(aHora('2026-11-05T15:40:00+00:00')).toBe('às 12h40');
    expect(aHora('2026-11-05T04:00:00+00:00')).toBe('à 1h');
    expect(aHora('2026-11-05T15:00:00+00:00')).toBe('ao meio-dia');
    expect(aHora('2026-11-06T03:00:00+00:00')).toBe('à meia-noite');
    expect(aHora('2026-11-06T03:05:00+00:00')).toBe('à 0h05');
  });

  test('quando: o dia, perto de hoje por extenso, e a hora', () => {
    expect(quando('2026-11-05T13:00:00+00:00', '2026-11-05')).toBe('hoje, às 10h');
    expect(quando('2026-11-06T04:00:00+00:00', '2026-11-05')).toBe('amanhã, à 1h');
    expect(quando('2026-11-09T13:00:00+00:00', '2026-11-05')).toBe('segunda-feira, 9 de novembro, às 10h');
  });

  test('as contas de dias e meses, nas viradas', () => {
    expect(somarDias('2026-10-31', 1)).toBe('2026-11-01');
    expect(somarDias('2026-11-05', 6)).toBe('2026-11-11');
    expect(somarMeses('2026-12', 1)).toBe('2027-01');
    expect(somarMeses('2026-01', -1)).toBe('2025-12');
    expect(mesPorExtenso('2026-11')).toBe('novembro de 2026');
  });

  test('o relógio do alto da tela marca a hora do porto, e não a do aparelho', () => {
    // 9h05 em Manaus já é 10h05 em Santos.
    expect(horaNoPorto(new Date('2026-11-05T13:05:00+00:00'))).toBe('10h05');
    expect(horaNoPorto(new Date('2026-11-05T15:00:00+00:00'))).toBe('meio-dia');
    expect(horaNoPorto(new Date('2026-11-05T04:00:00+00:00'))).toBe('1h');
  });

  test('a data curta e o dinheiro', () => {
    expect(formatarData('2026-11-30')).toBe('30/11/2026');
    // O Intl separa o símbolo da moeda com um espaço que não quebra.
    expect(formatarDinheiro(260)).toBe('R$\u00a0260,00');
  });
});
