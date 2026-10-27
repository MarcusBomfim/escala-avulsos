# ADR-0004 — A requisição: uma categoria, e os ternos como o operador pede

**Status:** aceita

## Contexto

O trabalho do avulso começa num pedido: o operador portuário que vai movimentar a carga de um navio pede ao órgão gestor a mão de obra de um período — tantos ternos, cada um com tantas vagas de cada função. É desse pedido que a escala sai, e é dele que o trabalhador escalado lê onde vai trabalhar: o navio, o terminal, o berço, o terno.

Três coisas pesam no desenho:

- **Cada categoria tem o seu rodízio**, o seu sindicato e a sua escala. Em Santos, a Escala Digital foi implantada sindicato por sindicato.
- **A composição dos ternos vem da convenção coletiva**, e muda com a operação — contêiner, granel, carga geral — e com cada negociação. Quantos estivadores um porão pede não é regra que o sistema deva inventar.
- **O operador e o navio têm números que não mudam**: o CNPJ da empresa e o número IMO do casco. O nome escrito no pedido varia; o número, não.

## Decisão

- **Uma requisição é de uma categoria só**, a das funções pedidas. Misturar estiva e conferência no mesmo pedido é recusado, dizendo as categorias misturadas: o operador faz uma requisição para cada uma.
- **Os ternos são informados um a um, e numerados na ordem**: o primeiro é o terno 1. A composição é dado — função e quantidade —, conferido: terno sem vaga, quantidade menor que 1 e função repetida no mesmo terno (mesmo escrita de outro jeito) são recusados. O sistema não calcula a composição; um catálogo de composições por tipo de operação, quando vier, vai preencher o mesmo pedido.
- **O operador é o CNPJ, o navio é o número IMO.** Dois operadores com o mesmo CNPJ são o mesmo, com o nome que for; dois navios com o mesmo IMO também — o navio que troca de nome continua sendo o mesmo casco. O local é o terminal e o berço, como se escreve no porto.
- **A requisição chega antes de o período começar, e só se cancela antes dele**, com motivo. O prazo de verdade — antes da escalação do período, e não do período — vem com o calendário das escalações, na Parte 2.
- **As vagas a preencher são `VagasPorFuncao`**: cada função somada em todos os ternos, na ordem em que aparece primeiro.
- **Os erros do pedido vêm todos de uma vez**, como nos outros pontos do domínio: o operador corrige tudo numa volta só.

## Consequências

- A escala da Parte 2 preenche `VagasPorFuncao` pelo rodízio da categoria da requisição, e distribui os escalados pelos ternos.
- O que o trabalhador lê na escala — "navio Atlântico Sul, Terminal Cais Leste, berço 4, terno 2" — já está na requisição, sem código para decifrar.
- Validar a composição contra a convenção (um terno de contêiner sem contramestre, por exemplo) fica para quando houver o catálogo de composições.
