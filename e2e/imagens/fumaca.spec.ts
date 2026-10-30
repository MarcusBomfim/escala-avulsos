import { randomBytes } from 'node:crypto';
import { expect, request, test, type Page } from '@playwright/test';
import { cpfDaMatricula } from '../apoio/api';
import { tokenDoEscalador, tokenDoTrabalhador } from '../apoio/identidade';
import { semProblemasDeAcessibilidade } from '../apoio/telas';

/*
 * A fumaça das imagens (ADR-0024): o que só a pilha de produção pode
 * quebrar. O nginx repassa a API sem enfraquecer a ETag, e deixa passar
 * cada evento do servidor na hora, sem buffer; o service worker e o
 * manifesto saem com o tipo certo; e a tela do trabalhador e o quadro do
 * escalador abrem e se ligam ao vivo.
 *
 * A pilha guarda o banco entre uma execução e outra: cada execução usa uma
 * matrícula e um dia que as outras não usaram. Com IMAGENS=1, o rastro da
 * escalação aberta é fotografado no painel do OpenTelemetry, para o README.
 */

const Painel = process.env.E2E_PAINEL ?? 'http://localhost:18890';

const sorteio = (de: number, ate: number) => de + Math.floor(Math.random() * (ate - de));

/** O órgão gestor, pela mesma origem do aplicativo: o pedido passa pelo nginx. */
async function doEscalador() {
  return request.newContext({
    baseURL: test.info().project.use.baseURL ?? '',
    extraHTTPHeaders: { Authorization: `Bearer ${tokenDoEscalador()}` },
  });
}

async function entrar(page: Page, token: string): Promise<void> {
  await page.goto('/');
  await expect(page).toHaveURL(/\/entrar/);
  await page.getByLabel('Token de acesso').fill(token);
  await page.getByRole('button', { name: 'Entrar' }).click();
}

test('o trabalhador entra pelo nginx, a versão passa inteira, e a tela dele fica ao vivo', async ({
  page,
}) => {
  const matricula = sorteio(100_000, 9_000_000);
  const escalador = await doEscalador();

  const criado = await escalador.post('/api/trabalhadores', {
    data: {
      matricula,
      nome: 'Teste de Fumaça',
      cpf: cpfDaMatricula(matricula),
      categoria: 'Estiva',
      inscricao: 'Registrado',
    },
  });
  const lido = await escalador.get(`/api/trabalhadores/${matricula}`);

  // A ETag chega forte: com o gzip do nginx, viraria W/"1", e o If-Match seguinte seria recusado.
  expect([criado.status(), criado.headers().etag]).toEqual([201, '"1"']);
  expect([lido.status(), lido.headers().etag]).toEqual([200, '"1"']);

  await entrar(page, tokenDoTrabalhador(matricula, 'Teste de Fumaça'));

  await expect(page.getByRole('heading', { name: 'Minha escala', level: 1 })).toBeVisible();
  await expect(page.getByText(`Matrícula ${matricula}`)).toBeVisible();
  await expect(page.getByText('Atualiza sozinho')).toBeVisible();
  await semProblemasDeAcessibilidade(page);

  // A casca que abre sem sinal: o service worker e o manifesto, com o tipo que o navegador exige.
  const casca = await page.request.get('/sw.js');
  const manifesto = await page.request.get('/manifest.webmanifest');
  expect([casca.status(), casca.headers()['content-type'], casca.headers()['cache-control']]).toEqual([
    200,
    'application/javascript',
    'no-cache',
  ]);
  expect([manifesto.status(), manifesto.headers()['content-type']]).toEqual([
    200,
    'application/manifest+json',
  ]);
  await escalador.dispose();
});

test('o quadro do escalador muda sozinho quando a escalação abre: o evento passa pelo nginx na hora', async ({
  page,
  browser,
}) => {
  const dia = new Date(Date.now() + sorteio(30, 3_000) * 86_400_000).toISOString().slice(0, 10);

  await entrar(page, tokenDoEscalador());
  await expect(page).toHaveURL(/\/quadro$/);
  await page.getByLabel('Dia').fill(dia);
  await page.getByLabel('Turno').selectOption('Tarde');
  await expect(page.getByText('Nenhuma requisição neste período.')).toBeVisible();
  await expect(page.getByText('Atualiza sozinho')).toBeVisible();

  const escalador = await doEscalador();
  // O pedido abre um rastro conhecido, como um aplicativo instrumentado abriria: é ele que a foto do painel mostra.
  const rastro = randomBytes(16).toString('hex');
  const aberta = await escalador.put(`/api/escalacoes/Estiva/${dia}/Tarde`, {
    headers: { traceparent: `00-${rastro}-${randomBytes(8).toString('hex')}-01` },
  });
  expect(aberta.status()).toBe(201);

  // Sem recarregar nada: o anúncio do banco vira evento, o evento atravessa o nginx, e a estiva aparece.
  await expect(page.getByRole('region', { name: 'Estiva' })).toBeVisible({ timeout: 10_000 });
  await semProblemasDeAcessibilidade(page);
  await escalador.dispose();

  if (process.env.IMAGENS === '1') {
    // O painel é para a tela do computador; a API manda a telemetria em lotes, e a foto espera o rastro chegar.
    const computador = await browser.newPage({
      viewport: { width: 1400, height: 260 },
      deviceScaleFactor: 1,
      isMobile: false,
      hasTouch: false,
    });
    await expect(async () => {
      await computador.goto(`${Painel}/traces/detail/${rastro}`);
      await expect(
        computador.getByText('PUT /api/escalacoes/{categoria}/{dia}/{turno}/', { exact: true }).first(),
      ).toBeVisible({
        timeout: 2_000,
      });
    }).toPass({ timeout: 30_000 });
    // O aviso de que o painel não tem login é verdade no compose local, mas não é o assunto da foto.
    const fechar = computador.locator('[aria-label="Dismiss notification"]');
    if (await fechar.isVisible()) {
      await fechar.click();
    }
    await computador.mouse.move(0, 0);
    await computador.screenshot({ path: '../docs/imagens/rastro.png' });
    await computador.close();
  }
});
