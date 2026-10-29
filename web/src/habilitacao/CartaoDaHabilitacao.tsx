import { useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { api, type Esquemas } from '../api/cliente';
import { comoProblema } from '../api/problemas';
import { AvisoDeProblema } from '../comum/AvisoDeProblema';
import { CartaoDoPeriodo } from '../comum/CartaoDoPeriodo';
import { Selo } from '../comum/Selo';
import estilos from './CartaoDaHabilitacao.module.css';
import { funcoesNaFrase, janela, motivosNaFrase, situacaoDaHabilitacao } from './frases';

type Minha = Esquemas['MinhaHabilitacaoResposta'];

/**
 * Uma escalação em que ele pode se habilitar: o dia e o horário, a janela,
 * e as funções dele — as que pode escolher já marcadas, as que não pode com
 * o motivo, antes de ele tentar. Habilitar, trocar e desistir vão sem
 * versão e sem "tente de novo" (ADR-0015); o que o domínio recusar volta
 * com a frase dele.
 */
export function CartaoDaHabilitacao({
  minha,
  hoje,
  agora,
}: {
  readonly minha: Minha;
  readonly hoje: string;
  readonly agora: Date;
}) {
  const consultas = useQueryClient();
  const [escolhidas, definirEscolhidas] = useState<readonly string[]>(() =>
    minha.habilitado
      ? minha.funcoes
      : minha.opcoes.filter((opcao) => opcao.pode).map((opcao) => opcao.funcao),
  );
  const [feito, definirFeito] = useState<string | null>(null);
  const atualizar = () => consultas.invalidateQueries({ queryKey: ['get', '/api/eu/habilitacoes'] });
  const habilitar = api.useMutation('put', '/api/eu/habilitacoes/{dia}/{turno}', {
    onSuccess: async (resposta) => {
      definirFeito(`Pronto: você está habilitado como ${funcoesNaFrase(resposta.funcoes)}.`);
      await atualizar();
    },
  });
  const desistir = api.useMutation('delete', '/api/eu/habilitacoes/{dia}/{turno}', {
    onSuccess: async () => {
      definirFeito('Pronto: você desistiu da habilitação para este período.');
      await atualizar();
    },
  });

  const { periodo } = minha;
  const caminho = { path: { dia: periodo.dia, turno: periodo.turno } };
  const { tom, selo, frase } = situacaoDaHabilitacao(minha, agora, hoje);
  const aberta = janela(minha, agora) === 'aberta';
  const enviando = habilitar.isPending || desistir.isPending;
  const problema = habilitar.isError
    ? comoProblema(habilitar.error)
    : desistir.isError
      ? comoProblema(desistir.error)
      : null;
  const mesmasFuncoes =
    minha.habilitado &&
    escolhidas.length === minha.funcoes.length &&
    escolhidas.every((funcao) => minha.funcoes.includes(funcao));
  const titulo = `habilitacao-${periodo.dia}-${periodo.turno}`;

  function alternar(funcao: string, marcada: boolean) {
    definirFeito(null);
    // Na ordem das opções, e não na ordem dos toques.
    definirEscolhidas(
      minha.opcoes
        .map((opcao) => opcao.funcao)
        .filter((candidata) => (candidata === funcao ? marcada : escolhidas.includes(candidata))),
    );
  }

  return (
    <CartaoDoPeriodo periodo={periodo} hoje={hoje} tom={tom} chave={titulo}>
      <Selo tom={tom}>{selo}</Selo>
      <p>{frase}</p>

      {aberta && minha.opcoes.length > 0 && (
        <fieldset className={estilos.funcoes} disabled={enviando}>
          <legend>Em que funções você quer trabalhar?</legend>
          {minha.opcoes.map((opcao) => {
            const motivo = `motivo-${titulo}-${opcao.funcao}`;

            return (
              <div key={opcao.funcao} className={estilos.opcao}>
                <label>
                  <input
                    type="checkbox"
                    checked={escolhidas.includes(opcao.funcao)}
                    disabled={!opcao.pode}
                    aria-describedby={opcao.pode ? undefined : motivo}
                    onChange={(evento) => {
                      alternar(opcao.funcao, evento.target.checked);
                    }}
                  />
                  {opcao.funcao}
                </label>
                {!opcao.pode && (
                  <p id={motivo} className={estilos.motivo}>
                    {motivosNaFrase(opcao)}
                  </p>
                )}
              </div>
            );
          })}
        </fieldset>
      )}

      {!aberta && minha.opcoes.some((opcao) => !opcao.pode) && (
        <ul className={estilos.motivos}>
          {minha.opcoes
            .filter((opcao) => !opcao.pode)
            .map((opcao) => (
              <li key={opcao.funcao}>
                {opcao.funcao} — {motivosNaFrase(opcao)}
              </li>
            ))}
        </ul>
      )}

      {minha.opcoes.length === 0 && (
        <p>Você não tem qualificação em nenhuma função desta categoria. Procure o órgão gestor.</p>
      )}

      {problema && <AvisoDeProblema problema={problema} />}
      {feito && (
        <p className={estilos.feito} role="status">
          {feito}
        </p>
      )}

      {aberta && minha.opcoes.length > 0 && (
        <div className="acoes">
          <button
            type="button"
            disabled={enviando || escolhidas.length === 0 || mesmasFuncoes}
            onClick={() => {
              habilitar.mutate({ params: caminho, body: { funcoes: [...escolhidas] } });
            }}
          >
            {minha.habilitado ? 'Trocar as funções' : 'Habilitar-me'}
          </button>
          {minha.habilitado && (
            <button
              type="button"
              className="secundario"
              disabled={enviando}
              onClick={() => {
                desistir.mutate({ params: caminho });
              }}
            >
              Desistir da habilitação
            </button>
          )}
        </div>
      )}
    </CartaoDoPeriodo>
  );
}
