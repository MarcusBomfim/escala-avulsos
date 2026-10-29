import { describe, expect, test } from 'vitest';
import { comoProblema } from './problemas';

describe('o problema mostrado a quem usa', () => {
  test('o ProblemDetails da API vem como ela o escreveu', () => {
    expect(
      comoProblema({
        title: 'O pedido fere uma regra do trabalho portuário.',
        detail: 'O período de 05/11/2026, das 13h às 19h, já começou.',
        status: 422,
      }),
    ).toEqual({
      titulo: 'O pedido fere uma regra do trabalho portuário.',
      detalhe: 'O período de 05/11/2026, das 13h às 19h, já começou.',
      status: 422,
    });
  });

  test('sem sinal, diz o que conferir', () => {
    expect(comoProblema(new TypeError('Failed to fetch'))).toEqual({
      titulo: 'Sem conexão com a escala.',
      detalhe: 'Confira se o celular está com internet e tente de novo.',
    });
  });

  test('o resto, pela mensagem do erro', () => {
    expect(comoProblema(new Error('quebrou'))).toEqual({
      titulo: 'Algo deu errado.',
      detalhe: 'quebrou',
    });
    expect(comoProblema('nada')).toEqual({ titulo: 'Algo deu errado.', detalhe: undefined });
  });
});
