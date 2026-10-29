import type { Esquemas } from '../api/cliente';
import { CartaoDoPeriodo } from '../comum/CartaoDoPeriodo';
import { aHora, diaParaQuemLe } from '../comum/datas';
import { Ouvir } from '../comum/Ouvir';
import { Selo } from '../comum/Selo';
import estilos from './CartaoDaEscala.module.css';
import { Desistencia } from './Desistencia';
import {
  desistenciaNaTela,
  esperaNaTela,
  funcaoNoTerno,
  motivoNaTela,
  onde,
  situacaoNaTela,
  textoParaOuvir,
} from './frases';

type MinhaEscala = Esquemas['MinhaEscalaResposta'];

/**
 * Uma escala, do jeito que o trabalhador precisa: o dia e o horário em
 * cima, grandes; a situação num selo com a palavra; e, se ele vai
 * trabalhar, tudo o que precisa para chegar — a função, o navio, o lugar e
 * quem pediu. Se ficou de fora, por quê, e em que lugar da espera.
 */
export function CartaoDaEscala({
  minha,
  hoje,
  agora,
}: {
  readonly minha: MinhaEscala;
  readonly hoje: string;
  readonly agora: number;
}) {
  const { situacao, periodo } = minha;
  const { tom, selo } = situacaoNaTela(minha);
  const dia = diaParaQuemLe(periodo.dia, hoje);
  const podeDesistir =
    situacao.vaga !== null &&
    situacao.comparecimento === 'Aguardando' &&
    !minha.encerrada &&
    agora < new Date(periodo.inicio).getTime();

  return (
    <CartaoDoPeriodo periodo={periodo} hoje={hoje} tom={tom} chave={minha.escala}>
      <Selo tom={tom}>{selo}</Selo>

      {situacao.vaga && (
        <>
          {situacao.substituto && <p className={estilos.nota}>Você entrou no lugar de quem desistiu.</p>}
          <dl className={estilos.detalhes}>
            <div>
              <dt>Função</dt>
              <dd>{funcaoNoTerno(situacao.vaga)}</dd>
            </div>
            <div>
              <dt>Navio</dt>
              <dd>{situacao.vaga.navio.nome}</dd>
            </div>
            <div>
              <dt>Local</dt>
              <dd>{onde(situacao.vaga)}</dd>
            </div>
            <div>
              <dt>Quem pediu</dt>
              <dd>{situacao.vaga.operador.nome}</dd>
            </div>
          </dl>
          {situacao.presenteDesde && (
            <p className={estilos.nota}>Presença confirmada {aHora(situacao.presenteDesde)}.</p>
          )}
          {situacao.justificativaDaFalta && (
            <p className={estilos.nota}>Falta justificada: {situacao.justificativaDaFalta}.</p>
          )}
        </>
      )}

      {!situacao.vaga && situacao.vagaDesistida && (
        <p className={estilos.nota}>
          {desistenciaNaTela(minha, hoje)} A vaga passou para quem estava na espera.
        </p>
      )}

      {!situacao.vaga && !situacao.vagaDesistida && (
        <>
          <p className={estilos.porque}>Por quê:</p>
          <ul className={estilos.motivos}>
            {situacao.motivos.map((motivo) => (
              <li key={`${motivo.tipo}-${motivo.funcao ?? ''}`}>{motivoNaTela(motivo.descricao)}</li>
            ))}
          </ul>
          {minha.espera.length > 0 && (
            <p className={estilos.nota}>
              {esperaNaTela(minha.espera)} Se alguém desistir, a vaga pode ser sua.
            </p>
          )}
        </>
      )}

      {minha.encerrada && <p className={estilos.encerrada}>Esta escala já foi encerrada.</p>}

      <div className="acoes">
        <Ouvir texto={textoParaOuvir(minha, hoje)} />
        {podeDesistir && <Desistencia minha={minha} dia={dia} />}
      </div>
    </CartaoDoPeriodo>
  );
}
