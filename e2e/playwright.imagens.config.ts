import { defineConfig, devices } from '@playwright/test';

/*
 * As imagens candidatas, na pilha do compose.yaml (ADR-0024): o nginx com o
 * aplicativo, a API de produção e o PostgreSQL com o papel da aplicação. O
 * relógio é o de verdade — a API de produção não tem o do ensaio —, e por
 * isso o teste é de fumaça: confere que as peças se encaixam, e não a tarde
 * de quinta, que os testes de ponta a ponta já conferem com o relógio acertado.
 *
 * Quem sobe a pilha é quem roda: docker compose up, e depois
 *
 *   npm run teste:imagens
 */

export default defineConfig({
  testDir: './imagens',
  timeout: 60_000,
  fullyParallel: false,
  workers: 1,
  forbidOnly: !!process.env.CI,
  reporter: process.env.CI ? [['list'], ['html', { open: 'never' }]] : 'list',
  use: {
    baseURL: process.env.E2E_WEB ?? 'http://localhost:5390',
    locale: 'pt-BR',
    timezoneId: 'America/Sao_Paulo',
    trace: 'retain-on-failure',
  },
  projects: [{ name: 'celular', use: { ...devices['Pixel 7'], channel: 'chrome' } }],
});
