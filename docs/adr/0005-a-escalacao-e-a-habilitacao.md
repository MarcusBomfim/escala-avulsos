# ADR-0005 — A escalação de cada período, e a habilitação sem corrida

**Status:** aceita

## Contexto

Para cada período, o órgão gestor monta a escala de cada categoria com as requisições dos operadores e com os trabalhadores que se apresentaram para trabalhar. Hoje, o trabalhador se apresenta pelo celular em horários certos; num processo de 2025, um deles disse à Justiça do Trabalho que gastava, em média, meia hora no sistema para garantir a inclusão na escala ([análise](../analise-do-sistema-atual.md)). Para quem tem pouca prática com o celular, ou enxerga mal, esse tempo é o momento mais difícil do dia — e uma norma de frequência mínima pune quem não comparece.

A vez de cada um, porém, é pelo rodízio, na ordem das matrículas. A hora em que o trabalhador se apresenta não deveria mudar nada.

## Decisão

- **Uma escalação por categoria e período**, com o calendário dela: até quando chegam as requisições, quando a habilitação abre e quando fecha, contados para trás a partir do início do período. O padrão do sistema é requisições até 4 horas antes, e habilitação de 12 horas a 3 horas antes; cada porto configura o seu, porque os horários de verdade são combinados entre o órgão gestor, os operadores e os sindicatos. As requisições chegam até a habilitação fechar, no máximo: a escala é feita com elas.
- **Habilitar-se é escolher as funções** em que o trabalhador concorre naquele período. Dentro da janela, ele se habilita, troca as funções ou desiste quantas vezes quiser. A hora fica registrada, mas não dá preferência a ninguém: os habilitados são listados na ordem das matrículas.
- **A aptidão é conferida na habilitação.** Se o trabalhador não pode trabalhar em alguma das funções escolhidas — sem o curso, com o exame vencido, de férias —, o pedido inteiro é recusado na hora, com os motivos, e a habilitação anterior, se havia, continua valendo. O motivo que vale para todas as funções, como o exame vencido, aparece uma vez só. Na escala, a aptidão é conferida de novo: uma suspensão pode ter chegado depois.
- **Quem é de outra categoria não se habilita**: cada categoria tem o seu rodízio.

## Consequências

- Não existe corrida na abertura da janela. O aplicativo pode dizer, com calma: "você está habilitado como estivador; pode mudar até as 10h".
- O trabalhador que não pode trabalhar fica sabendo antes da escala, e por quê, em vez de procurar o nome numa lista e não achar.
- A frequência mínima, na Parte 3, vai contar as habilitações — e não os minutos em que o trabalhador ficou esperando.
