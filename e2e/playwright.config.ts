import { defineConfig, devices } from '@playwright/test';
import { ConfiguracaoDoJwt } from './apoio/identidade';
import { Api, Banco, Web } from './apoio/pilha';

/*
 * A pilha inteira, de verdade: o PostgreSQL (E2E_BANCO), a API do ensaio —
 * a mesma API, com o relógio que o teste acerta (ADR-0018) — e o aplicativo
 * de produção servido pelo vite preview. Só o provedor de identidade é dos
 * testes: uma chave que a API aceita por configuração.
 *
 * O navegador é o Google Chrome instalado na máquina, num celular: a tela
 * de um Android comum, com toque. Nada a baixar, aqui ou no CI.
 */

/** O ambiente de quem roda os testes, e mais o que cada processo precisa. */
function ambiente(extras: Record<string, string>): Record<string, string> {
  const atual = Object.entries(process.env).filter(
    (entrada): entrada is [string, string] => entrada[1] !== undefined,
  );

  return { ...Object.fromEntries(atual), ...extras };
}

export default defineConfig({
  testDir: './testes',
  timeout: 90_000,
  fullyParallel: false,
  workers: 1,
  forbidOnly: !!process.env.CI,
  reporter: process.env.CI ? [['list'], ['html', { open: 'never' }]] : 'list',
  use: {
    baseURL: Web,
    locale: 'pt-BR',
    timezoneId: 'America/Sao_Paulo',
    trace: 'retain-on-failure',
  },
  projects: [
    { name: 'preparacao', testMatch: /preparacao\.setup\.ts/ },
    {
      name: 'celular',
      use: { ...devices['Pixel 7'], channel: 'chrome' },
      dependencies: ['preparacao'],
    },
  ],
  webServer: [
    {
      name: 'API do ensaio',
      // O banco nasce vazio a cada subida: a API do ensaio o apaga e migra.
      command: 'dotnet run --no-build --no-launch-profile --project ../tests/EscalaAvulsos.Api.Ensaio',
      url: `${Api}/openapi/v1.json`,
      env: ambiente({ ASPNETCORE_URLS: Api, ConnectionStrings__Escala: Banco, ...ConfiguracaoDoJwt }),
      timeout: 180_000,
    },
    {
      name: 'Aplicativo',
      command: 'npm --prefix ../web run preview -- --port 5401',
      url: Web,
      env: ambiente({ API_ESCALA: Api }),
      timeout: 60_000,
    },
  ],
});
