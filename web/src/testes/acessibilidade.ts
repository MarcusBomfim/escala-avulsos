import axe from 'axe-core';

/**
 * O que o axe acha de errado na tela, pelas regras das WCAG 2.2, níveis A e
 * AA, e mais as boas práticas — uma linha por problema, com onde ele está,
 * para o teste dizer o que quebrou.
 *
 * O contraste das cores fica de fora aqui: o jsdom não pinta a tela. Ele é
 * conferido no navegador de verdade, nos testes de ponta a ponta.
 */
export async function problemasDeAcessibilidade(elemento: Element = document.body): Promise<string[]> {
  const resultado = await axe.run(elemento, {
    runOnly: {
      type: 'tag',
      values: ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa', 'best-practice'],
    },
    rules: { 'color-contrast': { enabled: false } },
  });

  return resultado.violations.map(
    (violacao) =>
      `${violacao.id}: ${violacao.help} — ${violacao.nodes.map((no) => no.target.join(' ')).join(', ')}`,
  );
}
