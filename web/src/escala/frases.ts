import type { Esquemas } from '../api/cliente';
import { comMaiuscula, diaParaQuemLe, quando } from '../comum/datas';
import { funcaoNaFrase, ordinal, Turnos } from '../comum/rotulos';
import type { TomDoSelo } from '../comum/Selo';

/*
 * A escala dita para o próprio trabalhador, em segunda pessoa: "Você vai
 * trabalhar", "Você ficou de fora". A API manda a frase do órgão gestor, em
 * terceira pessoa, e os dados soltos; o aplicativo monta a dele com os
 * dados, e usa as frases do domínio onde elas já falam com ele — os
 * motivos: "como sinaleiro, o rodízio parou 1 posição antes da sua vez".
 */

type MinhaEscala = Esquemas['MinhaEscalaResposta'];

type Vaga = Esquemas['VagaResposta'];

export interface SituacaoNaTela {
  readonly tom: TomDoSelo;
  readonly selo: string;
}

export function situacaoNaTela({ situacao }: MinhaEscala): SituacaoNaTela {
  if (situacao.vaga) {
    switch (situacao.comparecimento) {
      case 'Presente':
        return { tom: 'sucesso', selo: 'Presença confirmada' };
      case 'Falta':
        return { tom: 'perigo', selo: 'Falta' };
      case 'FaltaJustificada':
        return { tom: 'neutro', selo: 'Falta justificada' };
      case 'Aguardando':
        return {
          tom: 'sucesso',
          selo: situacao.substituto ? 'Você entrou na escala' : 'Você vai trabalhar',
        };
    }
  }

  return situacao.vagaDesistida
    ? { tom: 'neutro', selo: 'Você desistiu da vaga' }
    : { tom: 'atencao', selo: 'Você ficou de fora' };
}

/** "Terminal Cais Leste, berço 4" — ou só o terminal, quando a operação não é num berço. */
export function onde({ local }: Vaga): string {
  return local.berco ? `${local.terminal}, berço ${local.berco}` : local.terminal;
}

/** "Sinaleiro, no terno 1". */
export function funcaoNoTerno({ funcao, terno }: Vaga): string {
  return `${funcao}, no terno ${terno}`;
}

/** O motivo do domínio como uma frase sozinha: "Como sinaleiro, o rodízio parou…". */
export function motivoNaTela(descricao: string): string {
  return `${comMaiuscula(descricao)}.`;
}

/** "Você é o 1º da espera de sinaleiro e o 3º da espera de estivador." — vazio, se não está em nenhuma. */
export function esperaNaTela(
  espera: readonly Esquemas['PosicaoNaEspera'][],
  { falado = false } = {},
): string {
  const lugares = espera.map(
    ({ funcao, posicao }) => `o ${ordinal(posicao, { falado })} da espera de ${funcaoNaFrase(funcao)}`,
  );

  if (lugares.length === 0) {
    return '';
  }

  const juntos =
    lugares.length === 1 ? lugares[0] : `${lugares.slice(0, -1).join(', ')} e ${lugares.at(-1) ?? ''}`;

  return `Você é ${juntos ?? ''}.`;
}

/** "Você desistiu da vaga de estivador hoje, às 11h." */
export function desistenciaNaTela({ situacao }: MinhaEscala, hoje: string): string {
  const vaga = situacao.vagaDesistida;
  const em = situacao.desistiuEm ? ` ${quando(situacao.desistiuEm, hoje)}` : '';

  return vaga ? `Você desistiu da vaga de ${funcaoNaFrase(vaga.funcao)}${em}.` : '';
}

/**
 * Tudo o que o cartão diz, numa fala só, para ouvir: o dia, o horário dito
 * por extenso, e a situação. "Hoje, quinta-feira, 5 de novembro, da uma da
 * tarde às sete da noite. Você vai trabalhar como sinaleiro, no terno 1 do
 * navio Atlântico Sul. Local: Terminal Cais Leste, berço 4."
 */
export function textoParaOuvir(minha: MinhaEscala, hoje: string): string {
  const { situacao, periodo, espera } = minha;
  const partes = [`${diaParaQuemLe(periodo.dia, hoje)}, ${Turnos[periodo.turno].falado}.`];

  if (situacao.vaga) {
    const vaga = situacao.vaga;
    const trabalho = `como ${funcaoNaFrase(vaga.funcao)}, no terno ${vaga.terno} do navio ${vaga.navio.nome}. Local: ${onde(vaga)}.`;

    partes.push(
      situacao.substituto
        ? `Você entrou na escala no lugar de quem desistiu: vai trabalhar ${trabalho}`
        : `Você vai trabalhar ${trabalho}`,
    );
  } else if (situacao.vagaDesistida) {
    partes.push(desistenciaNaTela(minha, hoje));
  } else {
    partes.push('Você ficou de fora.', ...situacao.motivos.map((motivo) => motivoNaTela(motivo.descricao)));
    partes.push(esperaNaTela(espera, { falado: true }));
  }

  return partes.filter((parte) => parte !== '').join(' ');
}
