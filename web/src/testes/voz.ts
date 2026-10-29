import { vi } from 'vitest';

/** O que a voz de mentira disse, e como fazê-la terminar. */
export interface VozDeTeste {
  readonly ditas: SpeechSynthesisUtterance[];
  readonly cancelar: ReturnType<typeof vi.fn>;
  /** A fala atual chega ao fim, como chegaria no celular. */
  readonly terminar: () => void;
}

/**
 * Uma voz no aparelho dos testes — o jsdom não tem nenhuma. Guarda o que foi
 * dito, sem dizer.
 */
export function instalarVoz(): VozDeTeste {
  const ditas: SpeechSynthesisUtterance[] = [];
  const cancelar = vi.fn();

  class Fala {
    lang = '';
    rate = 1;
    voice: SpeechSynthesisVoice | null = null;
    onend: (() => void) | null = null;
    onerror: (() => void) | null = null;

    constructor(readonly text: string) {}
  }

  vi.stubGlobal('SpeechSynthesisUtterance', Fala);
  vi.stubGlobal('speechSynthesis', {
    speak: (fala: SpeechSynthesisUtterance) => ditas.push(fala),
    cancel: cancelar,
    getVoices: () => [{ lang: 'pt-BR', name: 'Português do Brasil' }],
  });

  return {
    ditas,
    cancelar,
    terminar: () => {
      (ditas.at(-1) as unknown as Fala | undefined)?.onend?.();
    },
  };
}
