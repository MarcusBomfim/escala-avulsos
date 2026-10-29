/**
 * Para onde voltar depois de entrar: só um caminho deste aplicativo. Um
 * endereço de fora — `//site.com`, `/\site.com` —, posto no link por quem
 * quer enganar, vira a escala.
 */
export function destinoSeguro(destino: string | null): string {
  return destino?.startsWith('/') && !destino.startsWith('//') && !destino.startsWith('/\\') ? destino : '/';
}
