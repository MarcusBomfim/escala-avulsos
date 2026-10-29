/*
 * Onde a pilha dos testes de ponta a ponta roda: a que o próprio Playwright
 * sobe, em portas próprias — 5301 e 5401 —, para não esbarrar na API e no
 * aplicativo de desenvolvimento.
 */

export const Web = 'http://localhost:5401';

export const Api = 'http://localhost:5301';

/** O banco dos testes: o do CI, ou um PostgreSQL local na porta 55435. A API do ensaio o apaga e migra ao subir. */
export const Banco =
  process.env.E2E_BANCO ??
  'Host=localhost;Port=55435;Database=escala_avulsos_e2e;Username=postgres;Password=postgres';
