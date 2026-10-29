import { describe, expect, test } from 'vitest';
import { tokenDeTeste } from '../testes/tokens';
import { lerSessao, type LeituraDoToken, type Sessao } from './token';

function sessao(leitura: LeituraDoToken): Sessao {
  if ('motivo' in leitura) {
    throw new Error(`Esperava uma sessão, e veio: ${leitura.motivo}`);
  }

  return leitura.sessao;
}

function motivo(leitura: LeituraDoToken): string {
  if ('sessao' in leitura) {
    throw new Error('Esperava a recusa do token.');
  }

  return leitura.motivo;
}

describe('o token de quem entra', () => {
  test('diz o nome, a matrícula e até quando vale', () => {
    const lida = sessao(lerSessao(tokenDeTeste({ exp: 1_893_456_000 })));

    expect(lida).toMatchObject({ nome: 'Antônio Ferreira', matricula: 4127 });
    expect(lida.venceEm).toEqual(new Date(1_893_456_000 * 1000));
  });

  test('aceita o papel em lista, a matrícula em número, o nome do dotnet user-jwts e o "Bearer" na frente', () => {
    const token = tokenDeTeste({
      role: ['trabalhador'],
      matricula: 4127,
      name: undefined,
      unique_name: 'antonio',
    });
    const lida = sessao(lerSessao(`Bearer ${token}\n`));

    expect(lida).toMatchObject({ nome: 'antonio', matricula: 4127, token });
    expect(sessao(lerSessao(tokenDeTeste({ name: undefined }))).nome).toBeUndefined();
  });

  test('recusa o que não é token, o de outro papel, o sem matrícula e o vencido', () => {
    const agora = new Date('2026-11-05T12:00:00Z');

    expect(motivo(lerSessao('não-é-token'))).toContain('três partes');
    expect(motivo(lerSessao(tokenDeTeste({ role: 'escalador' })))).toBe(
      'Este aplicativo é do trabalhador, e o token não é de trabalhador.',
    );
    expect(motivo(lerSessao(tokenDeTeste({ matricula: undefined })))).toBe(
      'O token não diz a sua matrícula.',
    );
    expect(motivo(lerSessao(tokenDeTeste({ matricula: '0' })))).toBe('O token não diz a sua matrícula.');
    expect(motivo(lerSessao(tokenDeTeste({ matricula: '41-27' })))).toBe('O token não diz a sua matrícula.');
    expect(motivo(lerSessao(tokenDeTeste({ exp: agora.getTime() / 1000 }), agora))).toContain('venceu');
  });

  test('vale até o segundo antes do exp: no exp, já venceu (RFC 7519)', () => {
    const agora = new Date('2026-11-05T12:00:00Z');
    const exp = agora.getTime() / 1000;

    expect(motivo(lerSessao(tokenDeTeste({ exp }), agora))).toContain('venceu');
    expect(sessao(lerSessao(tokenDeTeste({ exp: exp + 1 }), agora)).venceEm).toEqual(
      new Date((exp + 1) * 1000),
    );
  });
});
