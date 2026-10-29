import { describe, expect, test } from 'vitest';
import { destinoSeguro } from './destino';

describe('para onde voltar depois de entrar', () => {
  test('um caminho do aplicativo, como veio', () => {
    expect(destinoSeguro('/mes/2026-10')).toBe('/mes/2026-10');
    expect(destinoSeguro('/habilitacao?de=hoje')).toBe('/habilitacao?de=hoje');
  });

  test('um endereço de fora, ou nenhum, vira a escala', () => {
    expect(destinoSeguro('//site-que-engana.com')).toBe('/');
    expect(destinoSeguro('/\\site-que-engana.com')).toBe('/');
    expect(destinoSeguro('https://site-que-engana.com')).toBe('/');
    expect(destinoSeguro(null)).toBe('/');
  });
});
