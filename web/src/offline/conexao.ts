import { onlineManager } from '@tanstack/react-query';

/**
 * O TanStack Query começa achando que há internet, e só muda de ideia
 * quando o navegador avisa que ela caiu ou voltou. Aberto já sem sinal — no
 * cais —, o aplicativo começa pelo que o aparelho sabe: mostra o que está
 * guardado, avisa que está sem internet, e não tenta ler a API à toa.
 */
export function comecarPelaConexaoDoAparelho(): void {
  onlineManager.setOnline(navigator.onLine);
}
