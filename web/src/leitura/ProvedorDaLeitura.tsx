import { useMemo, useState, type ReactNode } from 'react';
import { ContextoDaLeitura, type EstadoDaLeitura } from './contexto';
import {
  aplicarPreferencias,
  guardarPreferencias,
  lerPreferencias,
  letraSeguinte,
  type Preferencias,
} from './preferencias';

/** O tamanho da letra e o contraste: guardados no aparelho, e aplicados na raiz do documento a cada troca. */
export function ProvedorDaLeitura({ children }: { readonly children: ReactNode }) {
  const [preferencias, definirPreferencias] = useState<Preferencias>(lerPreferencias);

  const estado = useMemo<EstadoDaLeitura>(() => {
    function trocar(novas: Preferencias) {
      guardarPreferencias(novas);
      aplicarPreferencias(novas);
      definirPreferencias(novas);
    }

    return {
      preferencias,
      aumentarLetra: () => {
        trocar({ ...preferencias, letra: letraSeguinte(preferencias.letra) });
      },
      alternarContraste: () => {
        trocar({ ...preferencias, contraste: preferencias.contraste === 'alto' ? 'normal' : 'alto' });
      },
    };
  }, [preferencias]);

  return <ContextoDaLeitura value={estado}>{children}</ContextoDaLeitura>;
}
