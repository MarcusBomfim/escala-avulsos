# ADR-0016 — A escala pela API: o órgão gestor com versão, o trabalhador numa semana e sem versão

**Status:** aceita

## Contexto

A escala tem dois públicos com necessidades opostas.

- **O órgão gestor** faz a escala e registra o que acontece no período. A escala de um período é mexida por mais de uma pessoa ao mesmo tempo, no cais e no escritório. Quem registra a presença precisa saber se a escala mudou desde que a leu: alguém pode ter desistido, e outro entrado no lugar.
- **O trabalhador** precisa de duas coisas: saber onde trabalha, ou por que ficou de fora, e poder desistir sem burocracia. Para ele, "a escala mudou, leia de novo" não quer dizer nada: a desistência dele é dele, aconteça o que acontecer com as outras vagas.

## Decisão

**1. A escala é feita pelo endereço da escalação**: `POST /api/escalacoes/Estiva/2026-11-05/Tarde/escala`. O servidor lê o que a escala precisa (a escalação, as requisições do período, os habilitados, o rodízio da categoria e as jornadas deles), escala pelo domínio e grava com a versão do rodízio que leu (ADR-0013). Há duas situações em que ele lê de novo e refaz: outra escala da categoria andou o rodízio no meio, ou um habilitado pegou outra jornada. Fazer de novo a escala da mesma escalação é 409.

**2. O que o órgão gestor registra vai com a versão lida**: a presença, a falta, a justificativa e o encerramento, cada um em `POST /api/escalas/{id}/…`, com `If-Match`, e o trabalhador pela matrícula. A matrícula que não se habilitou é regra do domínio (422), e não endereço.

**3. O trabalhador vê a escala dele, e não a dos outros.** `GET /api/eu/escalas` traz uma semana, a partir de hoje no relógio do porto, ou do dia em `?de=`. Para cada escala em que ele aparece, vêm:

- a frase do domínio, que o aplicativo pode mostrar como está;
- a vaga, com a função, o terno, o navio, o terminal, o berço e quem pediu;
- ou os motivos de ter ficado de fora;
- o lugar dele na espera de cada função: "1" é o primeiro a entrar se alguém desistir.

A escala inteira, com todos os nomes, é só do órgão gestor.

**4. A desistência do trabalhador não tem versão**, como a habilitação (ADR-0015): `POST /api/eu/escalas/{id}/desistencia`. O servidor lê a escala e as jornadas de agora dos que esperam, decide pelo domínio e grava. Se outro gravou no meio, ou se o banco recusou o substituto por jornada sem descanso, ele lê de novo e refaz. A escala de que o trabalhador não participa é, para ele, 404.

**5. O mês do trabalhador sai pronto**: `GET /api/eu/meses/2026-11` traz a frequência e o extrato. A regra de frequência e a tabela de remuneração são as da configuração do porto (ADR-0014), e a conta é do domínio (ADR-0009, ADR-0010).

**6. O contrato fica escrito no repositório**, em [`docs/api/openapi.json`](../api/openapi.json). É dele que o aplicativo do trabalhador vai gerar os tipos (Parte 6), e é no diff dele que quem revisa vê o contrato mudar. Um teste falha se a API servir outra coisa; atualizar é um ato de propósito, com `ESCALA_ATUALIZAR_CONTRATO=1`.

## Consequências

- **O aplicativo do trabalhador só lê, e manda pedidos simples**: habilitar-se com as funções, desistir. Não guarda versão nem chave, e não mostra "tente de novo".
- **A frase vem pronta**, em português, sem gênero. O aplicativo ainda decide a forma: "Amanhã, quinta, das 13h às 19h…" é dele, porque "amanhã" depende de quando se lê.
- **A tela do operador**, com as vagas preenchidas e as vazias das requisições dele, fica para a Parte 7, com os avisos e o tempo real.
- **Mudar o contrato é mudar um arquivo do repositório**, que aparece na revisão, e não um efeito colateral que ninguém viu.
