/*
 * O celular que recebe os recados: a inscrição no serviço de push do
 * navegador, com a chave pública do servidor (ADR-0020). Quem mostra a
 * notificação é o service worker (public/sw.js), mesmo com o aplicativo
 * fechado.
 */

/** Se este navegador recebe push: precisa do service worker, do push e das notificações — no iPhone, com o aplicativo na tela inicial. */
export function pushNoNavegador(): boolean {
  return 'serviceWorker' in navigator && 'PushManager' in globalThis && 'Notification' in globalThis;
}

/** "BCVx…" (base64url) nos bytes que o navegador pede em applicationServerKey. */
export function bytesDaChave(base64url: string): Uint8Array<ArrayBuffer> {
  const base64 = base64url.replaceAll('-', '+').replaceAll('_', '/');
  const completo = base64 + '='.repeat((4 - (base64.length % 4)) % 4);

  return Uint8Array.from(atob(completo), (caractere) => caractere.charCodeAt(0));
}

async function gerenciador(): Promise<PushManager> {
  return (await navigator.serviceWorker.ready).pushManager;
}

/** A inscrição deste navegador, se ele já aceitou receber. */
export async function inscricaoAtual(): Promise<PushSubscription | null> {
  return pushNoNavegador() ? (await gerenciador()).getSubscription() : null;
}

/** O que a API guarda da inscrição: o endereço e as duas chaves, como o navegador as dá. */
export interface InscricaoParaAApi {
  readonly endereco: string;
  readonly chavePublica: string;
  readonly segredo: string;
}

/** Pede a permissão — o navegador pergunta ao trabalhador — e inscreve este navegador; nulo, se ele não deixou. */
export async function inscrever(chaveDoServidor: string): Promise<InscricaoParaAApi | null> {
  if ((await Notification.requestPermission()) !== 'granted') {
    return null;
  }

  const inscricao = await (
    await gerenciador()
  ).subscribe({
    userVisibleOnly: true,
    applicationServerKey: bytesDaChave(chaveDoServidor),
  });
  const { endpoint, keys } = inscricao.toJSON();

  if (!endpoint || !keys?.p256dh || !keys.auth) {
    throw new Error('O navegador não deu a inscrição inteira.');
  }

  return { endereco: endpoint, chavePublica: keys.p256dh, segredo: keys.auth };
}

/** Tira a inscrição deste navegador; o endereço dela, para a API apagar também. */
export async function desinscrever(): Promise<string | null> {
  const inscricao = await inscricaoAtual();

  if (!inscricao) {
    return null;
  }

  await inscricao.unsubscribe();

  return inscricao.endpoint;
}
