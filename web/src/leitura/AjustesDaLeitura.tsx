import { Icone } from '../comum/Icone';
import estilos from './AjustesDaLeitura.module.css';
import { useLeitura } from './contexto';
import { NomesDaLetra } from './preferencias';

/**
 * A letra, que aumenta com um toque — normal, grande, muito grande, e volta
 * —, em cima de toda tela. O botão diz por escrito a letra que está
 * valendo, e o leitor de tela anuncia a troca.
 */
export function BotaoDaLetra() {
  const { preferencias, aumentarLetra } = useLeitura();
  const letra = NomesDaLetra[preferencias.letra];

  return (
    <>
      <button type="button" className={`secundario ${estilos.ajuste ?? ''}`} onClick={aumentarLetra}>
        <Icone nome="letra" />
        <span>
          Letra: <strong>{letra}</strong>
        </span>
      </button>
      <p className="somente-leitor" role="status">
        Letra {letra}.
      </p>
    </>
  );
}

/** O contraste alto, que liga e desliga — e o botão diz, para o leitor de tela, se está ligado. */
export function BotaoDoContraste() {
  const { preferencias, alternarContraste } = useLeitura();

  return (
    <button
      type="button"
      className={`secundario ${estilos.ajuste ?? ''}`}
      aria-pressed={preferencias.contraste === 'alto'}
      onClick={alternarContraste}
    >
      <Icone nome="contraste" />
      Contraste alto
    </button>
  );
}

/** Os dois juntos, na tela de entrada: é a primeira que se lê. */
export function AjustesDaLeitura() {
  return (
    <div className={estilos.ajustes} role="group" aria-label="Como ler">
      <BotaoDaLetra />
      <BotaoDoContraste />
    </div>
  );
}
