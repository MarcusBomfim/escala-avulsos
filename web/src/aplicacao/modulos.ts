import type { ComponentType } from 'react';
import type { RouteObject } from 'react-router';
import type { NomeDoIcone } from '../comum/Icone';
import type { Sessao } from '../sessao/token';

export type Papel = Sessao['papel'];

export interface EntradaDoMenu {
  /** A palavra do menu, sempre escrita: o ícone só ajuda a achar. */
  readonly rotulo: string;
  readonly icone: NomeDoIcone;
  readonly caminho: string;
  /** A posição no menu: menor vem antes. */
  readonly ordem: number;
  /** Para quem a área aparece no menu: o trabalhador, se a área não disser. */
  readonly papeis?: readonly Papel[] | undefined;
  /** O que aparece ao lado da palavra — quantos recados novos —, se a área tiver o que contar. */
  readonly Contagem?: ComponentType | undefined;
}

/** Uma área do aplicativo: as rotas dela, dentro da moldura, e a entrada no menu, se tiver uma. */
export interface Modulo {
  readonly rotas: readonly RouteObject[];
  readonly menu?: EntradaDoMenu | undefined;
}

/**
 * As áreas do aplicativo, encontradas na compilação: cada pasta com um
 * modulo.tsx entra sozinha — rotas e menu —, como os recursos da API se
 * encontram no assembly (ADR-0014). Área nova não mexe aqui nem nas rotas.
 */
const encontrados = import.meta.glob<Modulo>('../*/modulo.tsx', { eager: true, import: 'modulo' });

export const modulos: readonly Modulo[] = Object.values(encontrados);

/** O menu de quem entrou: as áreas do papel dele, na ordem pedida por cada uma. */
export function entradasDoMenu(
  papel: Papel = 'trabalhador',
  lista: readonly Modulo[] = modulos,
): EntradaDoMenu[] {
  return lista
    .flatMap((modulo) =>
      modulo.menu && (modulo.menu.papeis ?? ['trabalhador']).includes(papel) ? [modulo.menu] : [],
    )
    .sort((a, b) => a.ordem - b.ordem);
}
