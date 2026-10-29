import { api } from '../api/cliente';
import { comoProblema } from '../api/problemas';
import { Atualizacao } from '../comum/Atualizacao';
import { AvisoDeProblema } from '../comum/AvisoDeProblema';
import { Carregando } from '../comum/Carregando';
import { hojeNoPorto } from '../comum/datas';
import { CartaoDaHabilitacao } from './CartaoDaHabilitacao';
import estilos from './TelaDaHabilitacao.module.css';

/**
 * Onde ele pode se habilitar: as escalações abertas da categoria dele que
 * ainda não começaram, na ordem dos períodos — sem precisar saber o dia e o
 * turno de cor (ADR-0017).
 */
export function TelaDaHabilitacao() {
  const escalacoes = api.useQuery('get', '/api/eu/habilitacoes');
  const agora = new Date();
  const hoje = hojeNoPorto(agora);

  return (
    <section aria-labelledby="titulo-da-habilitacao">
      <h1 id="titulo-da-habilitacao">Habilitar</h1>
      <p className="subtitulo">
        Escolha o período e as funções. A vez na escala é pela matrícula, e não por quem chega primeiro: não
        precisa correr.
      </p>

      {escalacoes.isPending && <Carregando rotulo="Buscando as escalações abertas" />}
      {escalacoes.isError && <AvisoDeProblema problema={comoProblema(escalacoes.error)} />}

      {escalacoes.data && (
        <>
          <Atualizacao consulta={escalacoes} oQue="a lista" hoje={hoje} />

          {escalacoes.data.length === 0 ? (
            <div className="vazio">
              <p>
                <strong>Nenhuma escalação aberta para a sua categoria agora.</strong>
              </p>
              <p>O órgão gestor abre a escalação de cada período antes da habilitação. Volte mais tarde.</p>
            </div>
          ) : (
            <div className={estilos.lista}>
              {escalacoes.data.map((minha) => (
                <CartaoDaHabilitacao
                  key={`${minha.periodo.dia}-${minha.periodo.turno}`}
                  minha={minha}
                  hoje={hoje}
                  agora={agora}
                />
              ))}
            </div>
          )}
        </>
      )}
    </section>
  );
}
