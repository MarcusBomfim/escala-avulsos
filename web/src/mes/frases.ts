import type { Esquemas } from '../api/cliente';
import type { TomDoSelo } from '../comum/Selo';

/*
 * A frequência e o extrato ditos para o trabalhador. A API manda a frase do
 * órgão gestor, em terceira pessoa, e os números; o aplicativo diz com os
 * números, em segunda pessoa (ADR-0009, ADR-0010).
 */

type Frequencia = Esquemas['FrequenciaResposta'];

function contar(quantidade: number, singular: string, plural: string): string {
  return `${quantidade} ${quantidade === 1 ? singular : plural}`;
}

const Tons: Readonly<Record<Esquemas['SituacaoDaFrequencia'], TomDoSelo>> = {
  Cumprida: 'sucesso',
  EmAndamento: 'neutro',
  Descumprida: 'perigo',
};

export function tomDaFrequencia(frequencia: Frequencia): TomDoSelo {
  return Tons[frequencia.situacao];
}

/**
 * "Você se habilitou 1 vez neste mês. O mínimo é 12: faltam 11." — e, com
 * dias de férias ou afastamento, que o mínimo já desconta esses dias.
 */
export function frequenciaNaFrase(frequencia: Frequencia): string {
  const vezes = `Você se habilitou ${contar(frequencia.habilitacoes, 'vez', 'vezes')} neste mês.`;
  const minimo =
    frequencia.diasImpedidos > 0
      ? `O mínimo é ${frequencia.minimoDeHabilitacoes}, contados só os dias em que você podia trabalhar`
      : `O mínimo é ${frequencia.minimoDeHabilitacoes}`;
  const falta =
    frequencia.habilitacoesQueFaltam > 0
      ? `: ${frequencia.habilitacoesQueFaltam === 1 ? 'falta' : 'faltam'} ${frequencia.habilitacoesQueFaltam}.`
      : ', e você já chegou lá.';

  return `${vezes} ${minimo}${falta}`;
}

/** "0 de 2 aceitas" — e o aviso, quando passou do limite. */
export function faltasNaFrase(frequencia: Frequencia): string {
  const faltas = `${frequencia.faltas} de ${contar(frequencia.maximoDeFaltas, 'aceita', 'aceitas')}`;

  return frequencia.faltas > frequencia.maximoDeFaltas ? `${faltas} — acima do limite` : faltas;
}
