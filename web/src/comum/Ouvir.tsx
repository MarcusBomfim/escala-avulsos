import { useEffect, useRef, useState } from 'react';
import { Icone } from './Icone';

/** A voz do aparelho, se ele tiver uma — quase todo celular tem. */
function sintese(): SpeechSynthesis | undefined {
  return 'speechSynthesis' in globalThis ? globalThis.speechSynthesis : undefined;
}

/**
 * Lê o texto em voz alta, em português e um pouco mais devagar: para quem
 * não enxerga bem, ou está com as mãos ocupadas no cais. Um toque começa,
 * outro para. Sem voz no aparelho, o botão não aparece — não há o que
 * prometer.
 */
export function Ouvir({ texto, rotulo = 'Ouvir' }: { readonly texto: string; readonly rotulo?: string }) {
  const [falando, definirFalando] = useState(false);
  const minha = useRef<SpeechSynthesisUtterance | null>(null);
  const voz = sintese();

  // Saindo da tela, a voz para — se é esta que está falando.
  useEffect(
    () => () => {
      if (minha.current) {
        sintese()?.cancel();
      }
    },
    [],
  );

  if (!voz) {
    return null;
  }

  function alternar(voz: SpeechSynthesis) {
    voz.cancel();

    if (falando) {
      minha.current = null;
      definirFalando(false);

      return;
    }

    const fala = new SpeechSynthesisUtterance(texto);
    fala.lang = 'pt-BR';
    fala.rate = 0.9;
    const portugues = voz.getVoices().find((candidata) => candidata.lang.replace('_', '-') === 'pt-BR');

    if (portugues) {
      fala.voice = portugues;
    }

    fala.onend = () => {
      if (minha.current === fala) {
        minha.current = null;
        definirFalando(false);
      }
    };
    fala.onerror = fala.onend;
    minha.current = fala;
    definirFalando(true);
    voz.speak(fala);
  }

  return (
    <button
      type="button"
      className="secundario"
      onClick={() => {
        alternar(voz);
      }}
    >
      <Icone nome={falando ? 'parar' : 'ouvir'} />
      {falando ? 'Parar de ouvir' : rotulo}
    </button>
  );
}
