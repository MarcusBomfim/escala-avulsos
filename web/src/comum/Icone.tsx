/*
 * Os ícones, sempre ao lado de uma palavra: o ícone ajuda a achar, a palavra
 * diz o que é. Por isso são escondidos do leitor de tela — ele lê a palavra.
 * Traços grossos e formas simples, que se reconhecem com a vista cansada.
 */

const Desenhos = {
  // A âncora, da marca do aplicativo — que não é a do órgão gestor.
  marca: (
    <>
      <circle cx="12" cy="5" r="2" />
      <path d="M12 7v14M8 11h8M5 14a7 7 0 0 0 14 0" />
    </>
  ),
  relogio: (
    <>
      <circle cx="12" cy="12" r="9" />
      <path d="M12 7v5l3 2" />
    </>
  ),
  escala: (
    <>
      <rect x="3" y="5" width="18" height="16" rx="2" />
      <path d="M3 10h18M8 3v4M16 3v4" />
    </>
  ),
  habilitar: (
    <>
      <circle cx="12" cy="12" r="9" />
      <path d="m8 12 3 3 5-6" />
    </>
  ),
  mes: (
    <>
      <rect x="3" y="6" width="18" height="13" rx="2" />
      <path d="M3 10h18M7 15h4" />
    </>
  ),
  dados: (
    <>
      <circle cx="12" cy="8" r="4" />
      <path d="M4 21c0-4 4-6 8-6s8 2 8 6" />
    </>
  ),
  ouvir: (
    <>
      <path d="M4 9v6h4l5 4V5L8 9H4z" />
      <path d="M16 9a4 4 0 0 1 0 6M18.5 6.5a8 8 0 0 1 0 11" />
    </>
  ),
  parar: <rect x="6" y="6" width="12" height="12" rx="1" />,
  letra: (
    <>
      <path d="M3 19 8 5l5 14M4.8 14h6.4" />
      <path d="M15 19l3-8 3 8M15.9 16.5h4.2" />
    </>
  ),
  contraste: (
    <>
      <circle cx="12" cy="12" r="9" />
      <path d="M12 3a9 9 0 0 1 0 18z" fill="currentColor" />
    </>
  ),
  sair: (
    <>
      <path d="M14 4h5v16h-5" />
      <path d="M10 8l-4 4 4 4M6 12h10" />
    </>
  ),
  alerta: (
    <>
      <path d="M12 3 2 20h20L12 3z" />
      <path d="M12 10v4M12 17v.5" />
    </>
  ),
  semSinal: (
    <>
      <path d="M2 8.5a15 15 0 0 1 20 0M5 12a10 10 0 0 1 14 0M8.5 15.5a5 5 0 0 1 7 0" />
      <path d="M12 19v.5M3 3l18 18" />
    </>
  ),
} as const;

export type NomeDoIcone = keyof typeof Desenhos;

export function Icone({ nome }: { readonly nome: NomeDoIcone }) {
  return (
    <svg
      className="icone"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="2.2"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
      focusable="false"
    >
      {Desenhos[nome]}
    </svg>
  );
}
