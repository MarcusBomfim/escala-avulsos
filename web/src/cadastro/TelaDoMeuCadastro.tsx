import { api, type Esquemas } from '../api/cliente';
import { comoProblema } from '../api/problemas';
import { AvisoDeProblema } from '../comum/AvisoDeProblema';
import { Carregando } from '../comum/Carregando';
import { comMaiuscula, formatarData, hojeNoPorto } from '../comum/datas';
import { Categorias, Inscricoes } from '../comum/rotulos';
import { Selo } from '../comum/Selo';
import { validadeDaQualificacao, validadeDoExame } from './avisos';
import estilos from './TelaDoMeuCadastro.module.css';

/**
 * O que o órgão gestor sabe dele: a matrícula, as funções, o exame médico e
 * as férias e afastamentos — com o aviso do que vai vencer, antes de virar
 * motivo para ficar de fora da escala. Só leitura: quem muda o cadastro é o
 * órgão gestor (ADR-0014).
 */
export function TelaDoMeuCadastro() {
  const cadastro = api.useQuery('get', '/api/eu');

  return (
    <section aria-labelledby="titulo-do-cadastro">
      <h1 id="titulo-do-cadastro">Meus dados</h1>
      <p className="subtitulo">Para mudar algum dado, procure o órgão gestor.</p>

      {cadastro.isPending && <Carregando rotulo="Buscando o seu cadastro" />}
      {cadastro.isError && <AvisoDeProblema problema={comoProblema(cadastro.error)} />}
      {cadastro.data && <Cadastro trabalhador={cadastro.data} hoje={hojeNoPorto()} />}
    </section>
  );
}

function Cadastro({
  trabalhador,
  hoje,
}: {
  readonly trabalhador: Esquemas['TrabalhadorResposta'];
  readonly hoje: string;
}) {
  const exame = validadeDoExame(trabalhador.aso, hoje);

  return (
    <>
      {trabalhador.cancelamento && (
        <AvisoDeProblema
          problema={{
            titulo: `A sua inscrição foi cancelada a partir de ${formatarData(trabalhador.cancelamento.aPartirDe)}.`,
            detalhe: trabalhador.cancelamento.motivo,
          }}
        />
      )}

      <section className={estilos.parte} aria-labelledby="titulo-de-voce">
        <h2 id="titulo-de-voce">Você</h2>
        <dl className={estilos.dados}>
          <div>
            <dt>Nome</dt>
            <dd>{trabalhador.nome}</dd>
          </div>
          <div>
            <dt>Matrícula</dt>
            <dd>{trabalhador.matricula}</dd>
          </div>
          <div>
            <dt>CPF</dt>
            <dd>{trabalhador.cpf}</dd>
          </div>
          <div>
            <dt>Categoria</dt>
            <dd>{comMaiuscula(Categorias[trabalhador.categoria])}</dd>
          </div>
          <div>
            <dt>Inscrição</dt>
            <dd>
              {comMaiuscula(Inscricoes[trabalhador.inscricao])}
              <span className={estilos.explicacao}>
                {trabalhador.inscricao === 'Registrado'
                  ? 'Na escala, os registrados vêm antes dos cadastrados.'
                  : 'Na escala, você entra nas vagas que os registrados não pegaram.'}
              </span>
            </dd>
          </div>
        </dl>
      </section>

      <section className={estilos.parte} aria-labelledby="titulo-do-exame">
        <h2 id="titulo-do-exame">Exame médico (ASO)</h2>
        <Selo tom={exame.tom}>{exame.selo}</Selo>
        <p>{exame.frase}</p>
      </section>

      <section className={estilos.parte} aria-labelledby="titulo-das-funcoes">
        <h2 id="titulo-das-funcoes">Funções</h2>
        {trabalhador.qualificacoes.length === 0 ? (
          <p>Nenhuma função registrada.</p>
        ) : (
          <ul className={estilos.lista}>
            {trabalhador.qualificacoes.map((qualificacao) => {
              const validade = validadeDaQualificacao(qualificacao, hoje);

              return (
                <li key={qualificacao.funcao}>
                  <p className={estilos.nome}>
                    <strong>{qualificacao.funcao}</strong>
                    <Selo tom={validade.tom}>{validade.selo}</Selo>
                  </p>
                  <p>{validade.frase}</p>
                </li>
              );
            })}
          </ul>
        )}
      </section>

      <section className={estilos.parte} aria-labelledby="titulo-dos-impedimentos">
        <h2 id="titulo-dos-impedimentos">Férias, afastamentos e suspensões</h2>
        {trabalhador.impedimentos.length === 0 ? (
          <p>Nenhum registrado.</p>
        ) : (
          <ul className={estilos.lista}>
            {trabalhador.impedimentos.map((impedimento) => (
              <li key={`${impedimento.tipo}-${impedimento.de}`}>
                <p>
                  <strong>{comMaiuscula(impedimento.descricao)}.</strong>
                </p>
                {impedimento.observacao && <p>{impedimento.observacao}</p>}
                <p className={estilos.explicacao}>Nesses dias, você não entra na escala.</p>
              </li>
            ))}
          </ul>
        )}
      </section>
    </>
  );
}
