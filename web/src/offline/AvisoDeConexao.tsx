import { onlineManager } from '@tanstack/react-query';
import { useSyncExternalStore } from 'react';
import { Icone } from '../comum/Icone';
import estilos from './AvisoDeConexao.module.css';

function useConectado(): boolean {
  return useSyncExternalStore(
    (avisar) => onlineManager.subscribe(avisar),
    () => onlineManager.isOnline(),
  );
}

/**
 * Sem internet, avisa logo em cima — e diz que o que aparece é o que o
 * aplicativo guardou, e não a escala de agora. Quando o sinal volta, o aviso
 * some e as telas leem de novo sozinhas.
 */
export function AvisoDeConexao() {
  if (useConectado()) {
    return null;
  }

  return (
    <div className={estilos.aviso} role="status">
      <Icone nome="semSinal" />
      <p>
        <strong>Sem internet.</strong> Você está vendo o que o aplicativo guardou da última vez. Quando o
        sinal voltar, a escala se atualiza sozinha.
      </p>
    </div>
  );
}
