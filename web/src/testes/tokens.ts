function base64url(texto: string): string {
  return btoa(String.fromCharCode(...new TextEncoder().encode(texto)))
    .replaceAll('+', '-')
    .replaceAll('/', '_')
    .replace(/=+$/, '');
}

/**
 * Um JWT com a assinatura qualquer: o aplicativo não confere assinatura —
 * quem confere é a API. O do Antônio, matrícula 4127, válido por uma hora,
 * a menos que a carga diga outra coisa.
 */
export function tokenDeTeste(carga: Record<string, unknown> = {}): string {
  const completa = {
    name: 'Antônio Ferreira',
    role: 'trabalhador',
    matricula: '4127',
    exp: Math.floor(Date.now() / 1000) + 3600,
    ...carga,
  };

  return `${base64url(JSON.stringify({ alg: 'HS256', typ: 'JWT' }))}.${base64url(JSON.stringify(completa))}.assinatura`;
}
