import { useQueryClient } from '@tanstack/react-query';
import { useEffect, useRef, useState } from 'react';
import { api, type Esquemas } from '../api/cliente';
import { comoProblema } from '../api/problemas';
import { AvisoDeProblema } from '../comum/AvisoDeProblema';
import { funcaoNaFrase, Turnos } from '../comum/rotulos';

type MinhaEscala = Esquemas['MinhaEscalaResposta'];

/**
 * Desistir da vaga: um botão, e depois a pergunta, escrita por inteiro —
 * que vaga, de que dia, e o que acontece —, com o "sim" e o "não" do mesmo
 * tamanho. Nada de janela por cima da tela: a pergunta aparece no lugar do
 * botão, e o leitor de tela vai direto para ela.
 *
 * Não há versão a mandar (ADR-0016): se outro desistiu no meio, o servidor
 * relê e refaz. O que o domínio recusar — o período já começou — volta com
 * a frase dele.
 */
export function Desistencia({ minha, dia }: { readonly minha: MinhaEscala; readonly dia: string }) {
  const consultas = useQueryClient();
  const [perguntando, definirPerguntando] = useState(false);
  const pergunta = useRef<HTMLParagraphElement>(null);
  const desistencia = api.useMutation('post', '/api/eu/escalas/{id}/desistencia', {
    onSuccess: () => consultas.invalidateQueries({ queryKey: ['get', '/api/eu/escalas'] }),
  });
  const funcao = funcaoNaFrase(minha.situacao.vaga?.funcao ?? '');
  const turno = Turnos[minha.periodo.turno];

  useEffect(() => {
    if (perguntando) {
      pergunta.current?.focus();
    }
  }, [perguntando]);

  if (!perguntando) {
    return (
      <button
        type="button"
        className="secundario"
        onClick={() => {
          definirPerguntando(true);
        }}
      >
        Desistir da vaga
      </button>
    );
  }

  return (
    <div className="confirmacao" role="group" aria-labelledby={`pergunta-${minha.escala}`}>
      <p id={`pergunta-${minha.escala}`} ref={pergunta} tabIndex={-1}>
        <strong>
          Desistir da vaga de {funcao} de {dia.toLowerCase()}, {turno.horario}?
        </strong>
      </p>
      <p>A vaga passa para o primeiro da espera, e você não volta para a espera desta escala.</p>

      {desistencia.isError && <AvisoDeProblema problema={comoProblema(desistencia.error)} />}

      <div className="acoes">
        <button
          type="button"
          className="perigo"
          disabled={desistencia.isPending}
          onClick={() => {
            desistencia.mutate({ params: { path: { id: minha.escala } } });
          }}
        >
          {desistencia.isPending ? 'Desistindo…' : 'Sim, desistir da vaga'}
        </button>
        <button
          type="button"
          className="secundario"
          disabled={desistencia.isPending}
          onClick={() => {
            desistencia.reset();
            definirPerguntando(false);
          }}
        >
          Não, vou trabalhar
        </button>
      </div>
    </div>
  );
}
