import type { Esquemas } from '../api/cliente';
import { comMaiuscula, diasEntre, formatarData } from '../comum/datas';
import type { TomDoSelo } from '../comum/Selo';

/*
 * O que no cadastro pode tirar o trabalhador da escala, dito antes de
 * tirar: o exame médico que vence, a reciclagem que vence. Todo "não" com o
 * prazo (ADR-0003) — e, aqui, antes de virar "não".
 */

/** Com quantos dias de antecedência o aplicativo avisa que o exame ou a reciclagem vão vencer. */
export const DiasDeAviso = 30;

export interface Validade {
  readonly tom: TomDoSelo;
  readonly selo: string;
  readonly frase: string;
}

/** "Vale até 30/11/2026"; perto de vencer, quantos dias faltam; vencido, desde quando. */
function validade(ate: string, hoje: string, oQue: string): Validade {
  const dias = diasEntre(hoje, ate);
  const data = formatarData(ate);

  if (dias < 0) {
    return {
      tom: 'perigo',
      selo: 'Vencido',
      frase: `${comMaiuscula(oQue)} venceu em ${data}: sem renovar, você não entra na escala.`,
    };
  }

  if (dias <= DiasDeAviso) {
    const falta = dias === 0 ? 'vence hoje' : `vence em ${dias === 1 ? '1 dia' : `${dias} dias`}`;

    return {
      tom: 'atencao',
      selo: 'Vence logo',
      frase: `${comMaiuscula(oQue)} vale até ${data} — ${falta}. Renove antes, para não ficar de fora da escala.`,
    };
  }

  return { tom: 'sucesso', selo: 'Em dia', frase: `${comMaiuscula(oQue)} vale até ${data}.` };
}

export function validadeDoExame(aso: Esquemas['AsoResposta'] | null, hoje: string): Validade {
  if (!aso) {
    return {
      tom: 'perigo',
      selo: 'Sem exame',
      frase: 'Não há exame médico registrado: sem ele, você não entra na escala. Procure o órgão gestor.',
    };
  }

  return validade(aso.validoAte, hoje, 'o exame');
}

export function validadeDaQualificacao(
  qualificacao: Esquemas['QualificacaoResposta'],
  hoje: string,
): Validade {
  const desde = `Desde ${formatarData(qualificacao.desde)}`;

  if (!qualificacao.validaAte) {
    return { tom: 'sucesso', selo: 'Em dia', frase: `${desde}, sem prazo para renovar.` };
  }

  const { tom, selo, frase } = validade(qualificacao.validaAte, hoje, 'a reciclagem');

  return { tom, selo, frase: `${desde}. ${frase}` };
}
