import { expect, test, type Page } from '@playwright/test';
import { acertarORelogio, doEscalador, enviar } from '../apoio/api';
import {
  alvosGrandes,
  entrar,
  fotografar,
  noRelogio,
  oQueFoiDito,
  semPalavraCortada,
  semProblemasDeAcessibilidade,
  semRolagemDeLado,
  vozQueAnota,
} from '../apoio/telas';
import { Quinta, Trabalhadores } from '../apoio/tardeDeQuinta';

/*
 * A tarde de quinta do README, no celular de cada um, contra a pilha
 * inteira: o Antônio se habilita, a escala sai, ele vê onde trabalha e
 * ouve; o Carlos vê por que ficou de fora; o Edson desiste e a vaga é do
 * Carlos; sem sinal, a escala continua lá; e, no dia seguinte, o mês.
 *
 * Os testes andam juntos, na ordem, com o relógio do porto: cada um parte de
 * onde o anterior parou. Com IMAGENS=1, cada tela é fotografada para o
 * README, em docs/imagens.
 */

test.describe.configure({ mode: 'serial' });

const { antonio, carlos, edson } = Trabalhadores;

const DaTarde = `/api/escalacoes/Estiva/${Quinta}/Tarde`;

let escala = '';

const cartaoDeHoje = (page: Page) =>
  page.getByRole('article', { name: /^Hoje, quinta-feira, 5 de novembro, Tarde/ });

test('às 8h, o Antônio se habilita pelo celular: as funções já vêm marcadas, e é um toque', async ({
  page,
}) => {
  await noRelogio(page, '2026-11-05T08:00:00-03:00');
  await entrar(page, antonio, '/habilitacao');

  const tarde = cartaoDeHoje(page);
  await expect(tarde).toContainText('A habilitação fecha hoje, às 10h.');
  await expect(tarde.getByRole('checkbox', { name: 'Sinaleiro' })).toBeChecked();
  await expect(tarde.getByRole('checkbox', { name: 'Estivador' })).toBeChecked();
  await semProblemasDeAcessibilidade(page);
  await alvosGrandes(page);
  await fotografar(page, 'habilitar');

  await tarde.getByRole('button', { name: 'Habilitar-me' }).click();

  await expect(tarde.getByRole('status')).toHaveText(/^Pronto: você está habilitado como /);
  await expect(tarde).toContainText('Você está habilitado');
});

test('às 10h a escala sai, e o Antônio lê onde trabalha — e ouve', async ({ page }) => {
  await acertarORelogio('2026-11-05T10:00:00-03:00');
  const escalador = await doEscalador();
  escala = ((await (await enviar(escalador, 'POST', `${DaTarde}/escala`)).json()) as { id: string }).id;
  await escalador.dispose();

  await vozQueAnota(page);
  await noRelogio(page, '2026-11-05T10:05:00-03:00');
  await entrar(page, antonio);

  const tarde = cartaoDeHoje(page);
  await expect(tarde).toContainText('Você vai trabalhar');
  await expect(tarde.getByRole('definition')).toHaveText([
    'Sinaleiro, no terno 1',
    'Atlântico Sul',
    'Terminal Cais Leste, berço 4',
    'Cais Forte Operações Portuárias',
  ]);
  await semProblemasDeAcessibilidade(page);
  await alvosGrandes(page);
  await fotografar(page, 'minha-escala');

  await tarde.getByRole('button', { name: 'Ouvir' }).click();

  expect(await oQueFoiDito(page)).toEqual([
    'Hoje, quinta-feira, 5 de novembro, da uma da tarde às sete da noite. Você vai trabalhar como sinaleiro, no terno 1 do navio Atlântico Sul. Local: Terminal Cais Leste, berço 4.',
  ]);
});

test('o Carlos ficou de fora: lê por quê, e que é o primeiro da espera', async ({ page }) => {
  await noRelogio(page, '2026-11-05T10:10:00-03:00');
  await entrar(page, carlos);

  const tarde = cartaoDeHoje(page);
  await expect(tarde).toContainText('Você ficou de fora');
  await expect(tarde.getByRole('listitem')).toHaveText([
    'Como sinaleiro, o rodízio parou 1 posição antes da sua vez.',
    'Como estivador, o rodízio parou 1 posição antes da sua vez.',
  ]);
  await expect(tarde).toContainText('Você é o 1º da espera de sinaleiro e o 1º da espera de estivador.');
  await semProblemasDeAcessibilidade(page);
  await fotografar(page, 'ficou-de-fora');
});

test('às 11h o Edson desiste pelo celular, e a vaga passa para o Carlos', async ({ page, browser }) => {
  await noRelogio(page, '2026-11-05T11:00:00-03:00');
  await entrar(page, edson);

  const tarde = cartaoDeHoje(page);
  await tarde.getByRole('button', { name: 'Desistir da vaga' }).click();
  // A pergunta aparece no lugar do botão, e o foco vai para ela.
  await expect(page.locator(':focus')).toHaveText(
    'Desistir da vaga de estivador de hoje, quinta-feira, 5 de novembro, das 13h às 19h?',
  );
  await semProblemasDeAcessibilidade(page);
  await page.getByRole('button', { name: 'Sim, desistir da vaga' }).click();

  await expect(tarde).toContainText('Você desistiu da vaga de estivador hoje, às 11h.');

  // No celular do Carlos, a mesma tarde agora é dele.
  const doCarlos = await browser.newPage();
  await noRelogio(doCarlos, '2026-11-05T11:05:00-03:00');
  await entrar(doCarlos, carlos);
  await expect(cartaoDeHoje(doCarlos)).toContainText('Você entrou na escala');
  await expect(cartaoDeHoje(doCarlos)).toContainText('Estivador, no terno 1');
  await doCarlos.close();
});

test('sem sinal no cais, o aplicativo abre, e a escala que o Antônio viu continua lá', async ({
  page,
  context,
}) => {
  await noRelogio(page, '2026-11-05T12:00:00-03:00');
  await entrar(page, antonio);
  await expect(cartaoDeHoje(page)).toContainText('Você vai trabalhar');

  // O service worker guardou a casca, e a escala ficou guardada no aparelho.
  await page.evaluate(async () => {
    await navigator.serviceWorker.ready;
  });
  await expect
    .poll(() => page.evaluate(() => localStorage.getItem('escala-avulsos.consultas')))
    .toContain('Atlântico Sul');

  await context.setOffline(true);
  await page.reload();

  await expect(page.getByText('Sem internet.')).toBeVisible();
  await expect(cartaoDeHoje(page)).toContainText('Você vai trabalhar');
  await expect(cartaoDeHoje(page)).toContainText('Sinaleiro, no terno 1');
  await context.setOffline(false);
});

test('letra muito grande e contraste alto, numa tela de 320px: nada vaza, nenhuma palavra se corta, e o contraste passa de 7:1', async ({
  page,
}) => {
  await page.setViewportSize({ width: 320, height: 720 });
  await noRelogio(page, '2026-11-05T12:00:00-03:00');
  await entrar(page, antonio);

  await page.getByRole('button', { name: 'Letra: normal' }).click();
  await page.getByRole('button', { name: 'Letra: grande' }).click();
  await page.getByRole('button', { name: 'Contraste alto' }).click();

  await expect(page.locator('html')).toHaveAttribute('data-letra', 'muito-grande');
  await expect(page.locator('html')).toHaveAttribute('data-contraste', 'alto');
  await expect(cartaoDeHoje(page)).toContainText('Você vai trabalhar');
  await semRolagemDeLado(page);
  await semPalavraCortada(page);
  await semProblemasDeAcessibilidade(page);
  await alvosGrandes(page);
  await fotografar(page, 'contraste-alto');

  for (const tela of ['Habilitar', 'Meu mês', 'Meus dados']) {
    await page.getByRole('link', { name: tela }).click();
    await expect(page.getByRole('heading', { level: 1 })).toBeVisible();
    await semRolagemDeLado(page);
    await semPalavraCortada(page);
  }
});

test('no tema escuro do celular, o contraste também passa', async ({ page }) => {
  await page.emulateMedia({ colorScheme: 'dark' });
  await noRelogio(page, '2026-11-05T12:00:00-03:00');
  await entrar(page, carlos);

  await expect(cartaoDeHoje(page)).toContainText('Você entrou na escala');
  await semProblemasDeAcessibilidade(page);
});

test('no dia seguinte, o mês: a tarde trabalhada no extrato, com o prazo do pagamento', async ({ page }) => {
  // O órgão gestor confirma a presença do Antônio às 12h40, e encerra a escala às 19h.
  const escalador = await doEscalador();
  await acertarORelogio('2026-11-05T12:40:00-03:00');
  const lida = await enviar(escalador, 'GET', `/api/escalas/${escala}`);
  const presente = await enviar(escalador, 'POST', `/api/escalas/${escala}/presencas`, {
    corpo: { matricula: antonio.matricula },
    versao: lida.headers().etag ?? '',
  });
  await acertarORelogio('2026-11-05T19:00:00-03:00');
  await enviar(escalador, 'POST', `/api/escalas/${escala}/encerramento`, {
    versao: presente.headers().etag ?? '',
  });
  await escalador.dispose();

  await noRelogio(page, '2026-11-06T09:00:00-03:00');
  await entrar(page, antonio, '/mes');

  const extrato = page.getByRole('region', { name: 'Extrato' });
  await expect(extrato.getByRole('listitem')).toContainText('Quinta-feira, 5 de novembro');
  await expect(extrato.getByRole('listitem')).toContainText('R$ 260,00');
  await expect(extrato.getByRole('listitem')).toContainText('Pago até amanhã, às 19h.');
  await expect(page.getByRole('region', { name: 'Frequência' })).toContainText(
    'Você se habilitou 1 vez neste mês.',
  );
  await semProblemasDeAcessibilidade(page);
  await fotografar(page, 'meu-mes');

  await page.getByRole('link', { name: 'Meus dados' }).click();
  await expect(page.getByRole('region', { name: 'Exame médico (ASO)' })).toContainText(
    'O exame vale até 30/11/2026 — vence em 24 dias.',
  );
  await semProblemasDeAcessibilidade(page);
  await fotografar(page, 'meus-dados');
});
