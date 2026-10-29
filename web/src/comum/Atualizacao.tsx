import estilos from './Atualizacao.module.css';
import { quando } from './datas';

interface Consulta {
  readonly isFetching: boolean;
  readonly dataUpdatedAt: number;
  readonly refetch: () => Promise<unknown>;
}

/**
 * Quando a tela foi lida — sem sinal, é o que ficou guardado — e o botão de
 * ler de novo, para quem não conhece o gesto de puxar a tela para baixo.
 */
export function Atualizacao({
  consulta,
  oQue,
  hoje,
}: {
  readonly consulta: Consulta;
  /** "a escala", "a lista": o que o botão atualiza. */
  readonly oQue: string;
  readonly hoje: string;
}) {
  return (
    <div className={estilos.atualizacao}>
      <p role="status">
        {consulta.isFetching
          ? 'Atualizando…'
          : `Última atualização: ${quando(new Date(consulta.dataUpdatedAt).toISOString(), hoje)}.`}
      </p>
      <button
        type="button"
        className="secundario"
        disabled={consulta.isFetching}
        onClick={() => {
          void consulta.refetch();
        }}
      >
        Atualizar {oQue}
      </button>
    </div>
  );
}
