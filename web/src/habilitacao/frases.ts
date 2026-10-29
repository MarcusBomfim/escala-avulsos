import type { Esquemas } from '../api/cliente';
import { quando } from '../comum/datas';
import { funcaoNaFrase } from '../comum/rotulos';
import type { TomDoSelo } from '../comum/Selo';

type Minha = Esquemas['MinhaHabilitacaoResposta'];

/** "sinaleiro", "sinaleiro e estivador", "sinaleiro, estivador e contramestre de terno". */
export function funcoesNaFrase(funcoes: readonly string[]): string {
  const nomes = funcoes.map(funcaoNaFrase);

  return nomes.length <= 1 ? (nomes[0] ?? '') : `${nomes.slice(0, -1).join(', ')} e ${nomes.at(-1) ?? ''}`;
}

export type Janela = 'ainda-nao-abriu' | 'aberta' | 'fechada';

/**
 * A janela da habilitação, agora. Aberta é a API quem diz, pelo relógio do
 * porto; fechada ou por abrir, o relógio do celular desempata.
 */
export function janela(minha: Minha, agora: Date): Janela {
  if (minha.janelaAberta) {
    return 'aberta';
  }

  return agora.getTime() < new Date(minha.abreEm).getTime() ? 'ainda-nao-abriu' : 'fechada';
}

export interface SituacaoDaHabilitacao {
  readonly tom: TomDoSelo;
  readonly selo: string;
  readonly frase: string;
}

/** O selo e a frase de cada momento: antes de abrir, aberta, habilitado, fechada. */
export function situacaoDaHabilitacao(minha: Minha, agora: Date, hoje: string): SituacaoDaHabilitacao {
  const abre = quando(minha.abreEm, hoje);
  const fecha = quando(minha.fechaEm, hoje);

  switch (janela(minha, agora)) {
    case 'ainda-nao-abriu':
      return {
        tom: 'neutro',
        selo: 'Ainda não abriu',
        frase: `A habilitação abre ${abre}, e fecha ${fecha}.`,
      };
    case 'aberta':
      return minha.habilitado
        ? {
            tom: 'sucesso',
            selo: 'Você está habilitado',
            frase: `Como ${funcoesNaFrase(minha.funcoes)}. Pode trocar as funções ou desistir até ${fecha}.`,
          }
        : { tom: 'atencao', selo: 'Habilitação aberta', frase: `A habilitação fecha ${fecha}.` };
    case 'fechada':
      return minha.habilitado
        ? {
            tom: 'sucesso',
            selo: 'Você está habilitado',
            frase: `Como ${funcoesNaFrase(minha.funcoes)}. A habilitação fechou ${fecha}: a escala sai em seguida, na tela Minha escala.`,
          }
        : {
            tom: 'neutro',
            selo: 'Habilitação fechada',
            frase: `A habilitação fechou ${fecha}, e você não se habilitou para este período.`,
          };
  }
}

/** "Não pode: o exame médico (ASO) vale só até 30/11/2026." */
export function motivosNaFrase(opcao: Esquemas['OpcaoDeFuncao']): string {
  return `Não pode: ${opcao.motivos.join('; ')}.`;
}
