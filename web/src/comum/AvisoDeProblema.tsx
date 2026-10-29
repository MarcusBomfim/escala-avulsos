import type { Problema } from '../api/problemas';
import estilos from './AvisoDeProblema.module.css';
import { Icone } from './Icone';

/** O problema como a API o explicou — o título e o motivo, em português. */
export function AvisoDeProblema({ problema }: { readonly problema: Problema }) {
  return (
    <div className={estilos.aviso} role="alert">
      <Icone nome="alerta" />
      <div>
        <p className={estilos.titulo}>{problema.titulo}</p>
        {problema.detalhe && <p className={estilos.detalhe}>{problema.detalhe}</p>}
      </div>
    </div>
  );
}
