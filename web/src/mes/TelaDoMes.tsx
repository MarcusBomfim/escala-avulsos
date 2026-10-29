import { Link, useParams } from 'react-router';
import { api, type Esquemas } from '../api/cliente';
import { comoProblema } from '../api/problemas';
import { AvisoDeProblema } from '../comum/AvisoDeProblema';
import { Carregando } from '../comum/Carregando';
import {
  comMaiuscula,
  diaPorExtenso,
  formatarDinheiro,
  hojeNoPorto,
  mesPorExtenso,
  quando,
  somarMeses,
} from '../comum/datas';
import { SituacoesDaFrequencia, Turnos } from '../comum/rotulos';
import { Selo } from '../comum/Selo';
import { faltasNaFrase, frequenciaNaFrase, tomDaFrequencia } from './frases';
import estilos from './TelaDoMes.module.css';

/**
 * O mês do trabalhador: se ele está cumprindo a frequência, e quanto
 * recebe por cada período trabalhado — com o prazo do pagamento. Mês
 * atual, no relógio do porto, ou o do endereço, para voltar aos anteriores.
 */
export function TelaDoMes() {
  const hoje = hojeNoPorto();
  const atual = hoje.slice(0, 7);
  const mes = useParams().mes ?? atual;
  const consulta = api.useQuery('get', '/api/eu/meses/{mes}', { params: { path: { mes } } });
  const valido = /^\d{4}-\d{2}$/.test(mes);

  return (
    <section aria-labelledby="titulo-do-mes">
      <h1 id="titulo-do-mes">Meu mês</h1>
      <p className="subtitulo">{valido ? comMaiuscula(mesPorExtenso(mes)) : mes}</p>

      {valido && (
        <nav className={estilos.meses} aria-label="Outros meses">
          <Link to={`/mes/${somarMeses(mes, -1)}`} className="botao secundario">
            Mês anterior
          </Link>
          {mes < atual && (
            <Link to={`/mes/${somarMeses(mes, 1)}`} className="botao secundario">
              Próximo mês
            </Link>
          )}
        </nav>
      )}

      {consulta.isPending && <Carregando rotulo="Buscando o seu mês" />}
      {consulta.isError && <AvisoDeProblema problema={comoProblema(consulta.error)} />}
      {consulta.data && <Mes mes={consulta.data} hoje={hoje} />}
    </section>
  );
}

function Mes({ mes, hoje }: { readonly mes: Esquemas['MeuMesResposta']; readonly hoje: string }) {
  const { frequencia, extrato } = mes;

  return (
    <>
      <section className={estilos.parte} aria-labelledby="titulo-da-frequencia">
        <h2 id="titulo-da-frequencia">Frequência</h2>
        <Selo tom={tomDaFrequencia(frequencia)}>{SituacoesDaFrequencia[frequencia.situacao]}</Selo>
        <p>{frequenciaNaFrase(frequencia)}</p>
        <dl className={estilos.numeros}>
          <div>
            <dt>Habilitações</dt>
            <dd>
              {frequencia.habilitacoes} de {frequencia.minimoDeHabilitacoes}
            </dd>
          </div>
          <div>
            <dt>Períodos trabalhados</dt>
            <dd>{frequencia.trabalhados}</dd>
          </div>
          <div>
            <dt>Faltas sem justificativa</dt>
            <dd>{faltasNaFrase(frequencia)}</dd>
          </div>
          <div>
            <dt>Faltas justificadas</dt>
            <dd>{frequencia.faltasJustificadas}</dd>
          </div>
          <div>
            <dt>Desistências</dt>
            <dd>{frequencia.desistencias}</dd>
          </div>
        </dl>
      </section>

      <section className={estilos.parte} aria-labelledby="titulo-do-extrato">
        <h2 id="titulo-do-extrato">Extrato</h2>

        {extrato.linhas.length === 0 ? (
          <p>Nenhum período trabalhado neste mês.</p>
        ) : (
          <ul className={estilos.linhas}>
            {extrato.linhas.map((linha) => (
              <li key={`${linha.periodo.dia}-${linha.periodo.turno}`} className={estilos.linha}>
                <p className={estilos.dia}>
                  <strong>{comMaiuscula(diaPorExtenso(linha.periodo.dia))}</strong>
                  <span>
                    {Turnos[linha.periodo.turno].nome}, {Turnos[linha.periodo.turno].horario}
                  </span>
                </p>
                <p>
                  {linha.funcao}, no terno {linha.terno} do navio {linha.navio.nome}
                </p>
                {linha.adicionalNoturno > 0 && (
                  <p>
                    {formatarDinheiro(linha.valor)} e mais {formatarDinheiro(linha.adicionalNoturno)} do
                    adicional noturno
                  </p>
                )}
                <p className={estilos.valor}>{formatarDinheiro(linha.total)}</p>
                <p className={estilos.prazo}>Pago até {quando(linha.pagoAte, hoje)}.</p>
              </li>
            ))}
          </ul>
        )}

        <dl className={estilos.totais}>
          <div>
            <dt>Remuneração dos períodos</dt>
            <dd>{formatarDinheiro(extrato.remuneracao)}</dd>
          </div>
          <div>
            <dt>Férias, com o terço a mais</dt>
            <dd>{formatarDinheiro(extrato.ferias)}</dd>
          </div>
          <div>
            <dt>13º salário</dt>
            <dd>{formatarDinheiro(extrato.decimoTerceiro)}</dd>
          </div>
          <div>
            <dt>FGTS — o Fundo de Garantia, depositado na sua conta</dt>
            <dd>{formatarDinheiro(extrato.fgts)}</dd>
          </div>
        </dl>
      </section>
    </>
  );
}
