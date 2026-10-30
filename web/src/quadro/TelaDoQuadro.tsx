import { useState } from 'react';
import { api, type Esquemas } from '../api/cliente';
import { comoProblema } from '../api/problemas';
import { Atualizacao } from '../comum/Atualizacao';
import { AvisoDeProblema } from '../comum/AvisoDeProblema';
import { Carregando } from '../comum/Carregando';
import { aHora, diaParaQuemLe, hojeNoPorto, quando } from '../comum/datas';
import { Categorias, Turnos } from '../comum/rotulos';
import { Selo, type TomDoSelo } from '../comum/Selo';
import { useSessao } from '../sessao/contexto';
import { periodoDeAgora } from './periodo';
import estilos from './TelaDoQuadro.module.css';

type Turno = Esquemas['Turno'];
type CategoriaNoQuadro = Esquemas['CategoriaNoQuadro'];
type TernoNoQuadro = Esquemas['TernoNoQuadro'];

/** A situação da escalação como o quadro de hoje a escreve — "Travada" é a habilitação fechada, esperando a escala. */
const DaCategoria: Readonly<
  Record<CategoriaNoQuadro['situacao'], { readonly palavra: string; readonly tom: TomDoSelo }>
> = {
  SemEscalacao: { palavra: 'Sem escalação', tom: 'neutro' },
  HabilitacaoVaiAbrir: { palavra: 'A habilitação vai abrir', tom: 'neutro' },
  HabilitacaoAberta: { palavra: 'Habilitação aberta', tom: 'atencao' },
  Travada: { palavra: 'Travada', tom: 'perigo' },
  Escalada: { palavra: 'Escalada', tom: 'sucesso' },
  Encerrada: { palavra: 'Encerrada', tom: 'neutro' },
};

const DoTerno: Readonly<Record<TernoNoQuadro['situacao'], TomDoSelo>> = {
  AguardandoAEscala: 'neutro',
  Completo: 'sucesso',
  Incompleto: 'perigo',
  Cancelado: 'neutro',
  ForaDoPrazo: 'neutro',
};

function palavraDoTerno(terno: TernoNoQuadro): string {
  switch (terno.situacao) {
    case 'AguardandoAEscala':
      return 'Aguardando a escala';
    case 'Completo':
      return 'Completo';
    case 'Incompleto':
      return terno.emAberto === 1 ? 'Falta 1' : `Faltam ${String(terno.emAberto)}`;
    case 'Cancelado':
      return 'Cancelado';
    case 'ForaDoPrazo':
      return 'Fora do prazo';
  }
}

/**
 * O quadro do período, como o de parede da Escala Digital de hoje
 * (ADR-0021): cada categoria com a situação escrita e a bolinha da cor, e em
 * cada terno pedido as vagas, as escaladas e as em aberto — e, depois da
 * escala, quem está em cada vaga. Muda sozinho, a cada habilitação e a cada
 * escala (ADR-0020). O operador vê só os ternos dele.
 */
export function TelaDoQuadro() {
  const { sessao } = useSessao();
  const [periodo, definirPeriodo] = useState(periodoDeAgora);
  const quadro = api.useQuery('get', '/api/quadro/{dia}/{turno}', { params: { path: periodo } });
  const hoje = hojeNoPorto();
  const doEscalador = sessao?.papel === 'escalador';
  const turno = Turnos[periodo.turno];

  return (
    <section aria-labelledby="titulo-do-quadro">
      <h1 id="titulo-do-quadro">Quadro</h1>
      <p className="subtitulo">
        {doEscalador
          ? 'As requisições do período, terno a terno: as vagas pedidas, as escaladas e as em aberto.'
          : 'Os ternos que a sua empresa pediu para o período, e quem está em cada vaga.'}
      </p>

      <form
        className={estilos.periodo}
        aria-label="Período do quadro"
        onSubmit={(evento) => {
          evento.preventDefault();
        }}
      >
        <label>
          Dia
          <input
            type="date"
            required
            value={periodo.dia}
            onChange={(evento) => {
              if (evento.target.value) {
                definirPeriodo({ ...periodo, dia: evento.target.value });
              }
            }}
          />
        </label>
        <label>
          Turno
          <select
            value={periodo.turno}
            onChange={(evento) => {
              definirPeriodo({ ...periodo, turno: evento.target.value as Turno });
            }}
          >
            {(Object.keys(Turnos) as Turno[]).map((opcao) => (
              <option key={opcao} value={opcao}>
                {Turnos[opcao].nome}, {Turnos[opcao].horario}
              </option>
            ))}
          </select>
        </label>
      </form>

      <p className={estilos.qual}>
        {comoFrase(diaParaQuemLe(periodo.dia, hoje))}, {turno.nome.toLowerCase()}, {turno.horario}
      </p>

      {quadro.isPending && <Carregando rotulo="Montando o quadro" />}
      {quadro.isError && <AvisoDeProblema problema={comoProblema(quadro.error)} />}

      {quadro.data && (
        <>
          <Atualizacao consulta={quadro} oQue="o quadro" hoje={hoje} />

          {quadro.data.categorias.length === 0 ? (
            <div className="vazio">
              <p>
                <strong>Nenhuma requisição neste período.</strong>
              </p>
              <p>Escolha outro dia ou outro turno.</p>
            </div>
          ) : (
            <div className={estilos.categorias}>
              {quadro.data.categorias.map((categoria) => (
                <CategoriaDoQuadro
                  key={categoria.categoria}
                  categoria={categoria}
                  hoje={hoje}
                  comOperador={doEscalador}
                />
              ))}
            </div>
          )}
        </>
      )}
    </section>
  );
}

function CategoriaDoQuadro({
  categoria,
  hoje,
  comOperador,
}: {
  readonly categoria: CategoriaNoQuadro;
  readonly hoje: string;
  readonly comOperador: boolean;
}) {
  const situacao = DaCategoria[categoria.situacao];
  const titulo = `quadro-${categoria.categoria}`;

  return (
    <section className={estilos.categoria} aria-labelledby={titulo}>
      <div className={estilos.cabecalho}>
        <h2 id={titulo}>{comoFrase(Categorias[categoria.categoria])}</h2>
        <Selo tom={situacao.tom}>{situacao.palavra}</Selo>
      </div>

      <p className={estilos.habilitacao}>{Habilitacao(categoria, hoje)}</p>

      <ul className={estilos.ternos}>
        {categoria.ternos.map((terno) => (
          <li key={`${terno.requisicao}-${String(terno.terno)}`}>
            <TernoDoQuadro terno={terno} comOperador={comOperador} />
          </li>
        ))}
      </ul>
    </section>
  );
}

function Habilitacao(categoria: CategoriaNoQuadro, hoje: string): string {
  const habilitados =
    categoria.habilitados === 1 ? '1 habilitado' : `${String(categoria.habilitados)} habilitados`;

  if (!categoria.abreEm || !categoria.fechaEm) {
    return 'A escalação deste período ainda não foi aberta.';
  }

  switch (categoria.situacao) {
    case 'HabilitacaoVaiAbrir':
      return `A habilitação abre ${quando(categoria.abreEm, hoje)}.`;
    case 'HabilitacaoAberta':
      return `${habilitados}. A habilitação fecha ${aHora(categoria.fechaEm)}.`;
    default:
      return `${habilitados}.`;
  }
}

function TernoDoQuadro({
  terno,
  comOperador,
}: {
  readonly terno: TernoNoQuadro;
  readonly comOperador: boolean;
}) {
  const comGente = terno.funcoes.some((funcao) => funcao.escalados.length > 0);

  return (
    <article className={estilos.terno} aria-label={`${terno.navio.nome}, terno ${String(terno.terno)}`}>
      <div className={estilos.qualTerno}>
        <h3>
          {terno.navio.nome}, terno {terno.terno}
        </h3>
        <Selo tom={DoTerno[terno.situacao]}>{palavraDoTerno(terno)}</Selo>
      </div>
      <p className={estilos.onde}>
        {terno.local.terminal}
        {terno.local.berco ? `, berço ${terno.local.berco}` : ''}
        {comOperador ? ` — ${terno.operador.nome}` : ''}
      </p>

      {/* As três colunas do quadro de parede: o que foi pedido, o que a escala preencheu, o que falta. */}
      <dl className={estilos.numeros}>
        <div>
          <dt>Pedidas</dt>
          <dd>{terno.pedidas}</dd>
        </div>
        <div>
          <dt>Escaladas</dt>
          <dd>{terno.escaladas}</dd>
        </div>
        <div>
          <dt>Em aberto</dt>
          <dd>{terno.emAberto}</dd>
        </div>
      </dl>

      {comGente && (
        <details className={estilos.quem}>
          <summary>Quem está em cada vaga</summary>
          <ul>
            {terno.funcoes.map((funcao) => (
              <li key={funcao.funcao}>
                <strong>
                  {funcao.funcao} ({funcao.escalados.length} de {funcao.pedidas}):
                </strong>{' '}
                {funcao.escalados.length > 0
                  ? funcao.escalados
                      .map((pessoa) => `${pessoa.nome}, matrícula ${String(pessoa.matricula)}`)
                      .join('; ')
                  : 'ninguém'}
              </li>
            ))}
          </ul>
        </details>
      )}
    </article>
  );
}

function comoFrase(texto: string): string {
  return `${texto.charAt(0).toUpperCase()}${texto.slice(1)}`;
}
