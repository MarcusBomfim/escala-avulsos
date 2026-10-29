/*
 * O token de acesso: um JWT emitido pelo provedor de identidade, com o papel
 * e a matrícula de quem entrou. Em produção, é o provedor que pede a
 * matrícula e a senha; em desenvolvimento, o token é o do `dotnet
 * user-jwts` (ADR-0014, ADR-0017).
 *
 * Aqui ele só é lido, para mostrar quem entrou e saber quando vence. Quem
 * confere a assinatura é a API, a cada pedido — o que o aplicativo decide
 * com o token é só o que mostrar.
 */

const Chave = 'escala-avulsos.token';

export interface Sessao {
  readonly token: string;
  /** O nome de quem entrou; nulo se o token não o traz. */
  readonly nome: string | undefined;
  readonly matricula: number;
  readonly venceEm: Date | undefined;
}

export type LeituraDoToken = { readonly sessao: Sessao } | { readonly motivo: string };

/**
 * Guardado no aparelho, e não só na aba: o celular é de uma pessoa, e
 * fechar o aplicativo não pode ser sair — quem tem mais idade não deveria
 * entrar de novo a cada vez que abre a escala. Sai-se pelo botão, ou quando
 * o token vence. Sem acesso ao armazenamento — navegação privada,
 * armazenamento bloqueado —, a sessão só não sobrevive a fechar a página.
 */
export function lerToken(): string | null {
  try {
    return localStorage.getItem(Chave);
  } catch {
    return null;
  }
}

export function guardarToken(token: string): void {
  try {
    localStorage.setItem(Chave, token);
  } catch {
    // Sem armazenamento, a sessão vive só na memória.
  }
}

export function esquecerToken(): void {
  try {
    localStorage.removeItem(Chave);
  } catch {
    // Nada guardado, nada a esquecer.
  }
}

function lerCarga(token: string): Record<string, unknown> | undefined {
  const partes = token.split('.');

  if (partes.length !== 3 || !partes[1]) {
    return undefined;
  }

  try {
    const base64 = partes[1].replaceAll('-', '+').replaceAll('_', '/');
    const bytes = Uint8Array.from(atob(base64), (caractere) => caractere.charCodeAt(0));
    const carga: unknown = JSON.parse(new TextDecoder().decode(bytes));

    return typeof carga === 'object' && carga !== null ? (carga as Record<string, unknown>) : undefined;
  } catch {
    return undefined;
  }
}

function textos(valor: unknown): string[] {
  if (typeof valor === 'string') {
    return [valor];
  }

  if (typeof valor === 'number') {
    return [String(valor)];
  }

  return Array.isArray(valor) ? valor.filter((item): item is string => typeof item === 'string') : [];
}

/** A matrícula como a API a lê: um número de 1 a 9999999. */
function matriculaDe(texto: string | undefined): number | undefined {
  if (!texto || !/^\d{1,7}$/.test(texto.trim())) {
    return undefined;
  }

  const numero = Number(texto);

  return numero > 0 ? numero : undefined;
}

/** Lê o token colado por quem entra; o motivo, se ele não serve. */
export function lerSessao(token: string, agora: Date = new Date()): LeituraDoToken {
  const limpo = token.trim().replace(/^Bearer\s+/i, '');
  const carga = lerCarga(limpo);

  if (!carga) {
    return { motivo: 'Isso não é um token de acesso: são três partes separadas por ponto.' };
  }

  if (!textos(carga.role).includes('trabalhador')) {
    return { motivo: 'Este aplicativo é do trabalhador, e o token não é de trabalhador.' };
  }

  const matricula = matriculaDe(textos(carga.matricula)[0]);

  if (matricula === undefined) {
    return { motivo: 'O token não diz a sua matrícula.' };
  }

  const venceEm = typeof carga.exp === 'number' ? new Date(carga.exp * 1000) : undefined;

  if (venceEm && venceEm <= agora) {
    return { motivo: 'O token venceu. Peça outro e entre de novo.' };
  }

  const [nome] = [...textos(carga.name), ...textos(carga.unique_name)];

  return { sessao: { token: limpo, nome, matricula, venceEm } };
}
