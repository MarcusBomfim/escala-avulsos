/*
 * Como cada um prefere ler: o tamanho da letra e o contraste. Ficam no
 * aparelho, valem também na tela de entrada, e são aplicados na raiz do
 * documento antes da primeira pintura — quem precisa de letra grande não
 * deveria ver a tela pequena nem por um instante.
 *
 * Tudo na tela é medido em rem: a letra maior aumenta os botões, os
 * espaços e os ícones junto, e nada se sobrepõe.
 */

export type Letra = 'normal' | 'grande' | 'muito-grande';

export type Contraste = 'normal' | 'alto';

export interface Preferencias {
  readonly letra: Letra;
  readonly contraste: Contraste;
}

const Chave = 'escala-avulsos.leitura';

const Letras: readonly Letra[] = ['normal', 'grande', 'muito-grande'];

export const NomesDaLetra: Readonly<Record<Letra, string>> = {
  normal: 'normal',
  grande: 'grande',
  'muito-grande': 'muito grande',
};

/** A letra seguinte, num toque: da maior, volta à normal. */
export function letraSeguinte(letra: Letra): Letra {
  return Letras[(Letras.indexOf(letra) + 1) % Letras.length] ?? 'normal';
}

/** Sem escolha guardada, o contraste do aparelho: quem já pediu mais contraste no celular começa com ele. */
function contrasteDoAparelho(): Contraste {
  return globalThis.matchMedia('(prefers-contrast: more)').matches ? 'alto' : 'normal';
}

export function lerPreferencias(): Preferencias {
  let guardadas: Partial<Record<keyof Preferencias, unknown>> = {};

  try {
    guardadas = JSON.parse(localStorage.getItem(Chave) ?? '{}') as typeof guardadas;
  } catch {
    // Sem armazenamento, ou com lixo nele: as preferências de fábrica.
  }

  const letra = Letras.find((candidata) => candidata === guardadas.letra) ?? 'normal';
  const contraste =
    guardadas.contraste === 'alto' || guardadas.contraste === 'normal' ? guardadas.contraste : undefined;

  return { letra, contraste: contraste ?? contrasteDoAparelho() };
}

export function guardarPreferencias(preferencias: Preferencias): void {
  try {
    localStorage.setItem(Chave, JSON.stringify(preferencias));
  } catch {
    // Sem armazenamento, vale até fechar o aplicativo.
  }
}

/** Na raiz do documento, onde o CSS as lê: data-letra e data-contraste. */
export function aplicarPreferencias(
  preferencias: Preferencias,
  raiz: HTMLElement = document.documentElement,
): void {
  raiz.dataset.letra = preferencias.letra;
  raiz.dataset.contraste = preferencias.contraste;
}
