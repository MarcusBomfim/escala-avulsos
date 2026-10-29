/**
 * Registra o service worker, que guarda a casca do aplicativo — a página e
 * os arquivos dela — para ele abrir sem sinal (public/sw.js). Só no
 * aplicativo compilado: em desenvolvimento, o Vite troca os arquivos a cada
 * mudança, e uma casca guardada só atrapalharia.
 */
export function registrarOServicoOffline(): void {
  if (!import.meta.env.PROD || !('serviceWorker' in navigator)) {
    return;
  }

  globalThis.addEventListener('load', () => {
    navigator.serviceWorker.register('/sw.js').catch((erro: unknown) => {
      // Sem o service worker, o aplicativo funciona igual — só não abre sem sinal.
      console.warn('O aplicativo não vai abrir sem internet: o service worker não foi registrado.', erro);
    });
  });
}
