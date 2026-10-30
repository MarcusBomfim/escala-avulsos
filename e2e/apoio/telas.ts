import { AxeBuilder } from '@axe-core/playwright';
import { expect, type Page } from '@playwright/test';
import { acertarORelogio } from './api';
import { tokenDoEscalador, tokenDoTrabalhador } from './identidade';

/*
 * O celular nas mãos do trabalhador: entrar, acertar a hora, conferir a
 * acessibilidade da tela e, com IMAGENS=1, fotografá-la para o README.
 */

export interface Quem {
  readonly matricula: number;
  readonly nome: string;
}

const comRelogio = new WeakSet<Page>();

/**
 * A hora do porto, na API do ensaio e no navegador ao mesmo tempo: as duas
 * pontas veem o mesmo "agora", e o relógio das duas anda dali em diante —
 * parado, ele não deixaria passar o segundo que o aplicativo espera entre
 * uma gravação e outra da escala guardada.
 */
export async function noRelogio(page: Page, agora: string): Promise<void> {
  await acertarORelogio(agora);

  if (comRelogio.has(page)) {
    await page.clock.setSystemTime(new Date(agora));
  } else {
    await page.clock.install({ time: new Date(agora) });
    comRelogio.add(page);
  }
}

/** Entra pela tela de entrada, como uma pessoa entraria, e vai para o endereço. */
export async function entrar(page: Page, quem: Quem, endereco = '/'): Promise<void> {
  await page.goto(endereco);
  await expect(page).toHaveURL(/\/entrar/);

  await page.getByLabel('Token de acesso').fill(tokenDoTrabalhador(quem.matricula, quem.nome));
  await page.getByRole('button', { name: 'Entrar' }).click();
  await expect(page.getByText(`Matrícula ${quem.matricula}`)).toBeVisible();
}

/** Entra como o órgão gestor: a tela é o quadro (ADR-0021). */
export async function entrarNoQuadro(page: Page): Promise<void> {
  await page.goto('/');
  await expect(page).toHaveURL(/\/entrar/);

  await page.getByLabel('Token de acesso').fill(tokenDoEscalador());
  await page.getByRole('button', { name: 'Entrar' }).click();
  await expect(page).toHaveURL(/\/quadro$/);
}

/**
 * A tela pelo axe, no navegador de verdade: as WCAG 2.2 até o nível AA e,
 * do AAA, o contraste de 7:1 — o que o aplicativo promete para quem tem a
 * vista cansada (ADR-0017). Também as boas práticas.
 */
export async function semProblemasDeAcessibilidade(page: Page): Promise<void> {
  const resultado = await new AxeBuilder({ page })
    .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa', 'best-practice'])
    .analyze();
  const aaa = await new AxeBuilder({ page }).withRules(['color-contrast-enhanced']).analyze();
  const problemas = [...resultado.violations, ...aaa.violations].map(
    (violacao) =>
      `${violacao.id}: ${violacao.help} — ${violacao.nodes.map((no) => no.target.join(' ')).join(', ')}`,
  );

  expect(problemas).toEqual([]);
}

/** Nada vaza para os lados — sem rolar de lado, mesmo com a letra muito grande (WCAG 1.4.10). */
export async function semRolagemDeLado(page: Page): Promise<void> {
  const larguras = await page.evaluate(() => ({
    pagina: document.documentElement.scrollWidth,
    tela: document.documentElement.clientWidth,
  }));

  expect(larguras.pagina).toBeLessThanOrEqual(larguras.tela);
}

/**
 * Nenhuma palavra de botão, do menu ou de título cortada ao meio: com a
 * letra grande, a tela pode quebrar a linha entre as palavras — e no hífen
 * de "quinta-feira" —, mas "Habili-tar" não se lê.
 */
export async function semPalavraCortada(page: Page): Promise<void> {
  const cortadas = await page.evaluate(() => {
    const encontradas: string[] = [];

    for (const elemento of document.querySelectorAll('nav a, button, h1, h2')) {
      const textos = document.createTreeWalker(elemento, NodeFilter.SHOW_TEXT);

      for (let no = textos.nextNode(); no; no = textos.nextNode()) {
        for (const palavra of (no.textContent ?? '').matchAll(/[^\s-]+/g)) {
          const faixa = document.createRange();
          faixa.setStart(no, palavra.index);
          faixa.setEnd(no, palavra.index + palavra[0].length);

          if (faixa.getClientRects().length > 1) {
            encontradas.push(palavra[0]);
          }
        }
      }
    }

    return encontradas;
  });

  expect(cortadas).toEqual([]);
}

/** Todo botão e todo link que faz papel de botão tem pelo menos 48px de altura: o dedo acerta. */
export async function alvosGrandes(page: Page): Promise<void> {
  const pequenos = await page.evaluate(() =>
    [...document.querySelectorAll<HTMLElement>('button, a.botao, nav a, label:has(input[type="checkbox"])')]
      .filter((elemento) => elemento.getBoundingClientRect().height < 48)
      .map((elemento) => `${elemento.textContent.trim()}: ${elemento.getBoundingClientRect().height}px`),
  );

  expect(pequenos).toEqual([]);
}

export async function fotografar(page: Page, nome: string): Promise<void> {
  if (process.env.IMAGENS === '1') {
    await page.screenshot({ path: `../docs/imagens/${nome}.png`, fullPage: true, scale: 'css' });
  }
}

/** Uma voz no celular que não fala, só anota o que diria — para o teste ouvir sem som. */
export async function vozQueAnota(page: Page): Promise<void> {
  await page.addInitScript(() => {
    const ditas: string[] = [];

    class Fala {
      lang = '';
      rate = 1;
      voice: unknown = null;
      onend: (() => void) | null = null;
      onerror: (() => void) | null = null;

      constructor(readonly text: string) {}
    }

    Object.assign(window, { ditas, SpeechSynthesisUtterance: Fala });
    Object.defineProperty(window, 'speechSynthesis', {
      value: { speak: (fala: Fala) => ditas.push(fala.text), cancel: () => undefined, getVoices: () => [] },
    });
  });
}

export async function oQueFoiDito(page: Page): Promise<string[]> {
  return page.evaluate(() => (window as unknown as { ditas: string[] }).ditas);
}
