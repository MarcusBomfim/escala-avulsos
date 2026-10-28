# ADR-0007 — A escala: quem vai para cada vaga, e por quê

**Status:** aceita

## Contexto

Com a janela fechada, tudo o que a escala precisa está à mão: as requisições do período, os habilitados e as funções de cada um, o rodízio de cada função, as jornadas já marcadas e a regra do descanso. Falta decidir quem vai para cada vaga — e contar a cada habilitado como ele ficou. Hoje, quem fica de fora procura o próprio nome na escala publicada e não acha; o porquê, ele descobre perguntando ([análise](../analise-do-sistema-atual.md)).

Três pessoas leem a escala: o trabalhador, que quer saber onde vai trabalhar ou por que não vai; o operador, que precisa saber antes do período se vai faltar gente; e quem confere depois, que precisa refazer a conta e chegar ao mesmo resultado.

## Decisão

- **A escala é uma conta.** `Escalador.Escalar` recebe a escalação, as requisições, os trabalhadores, as jornadas, o rodízio e o descanso, e devolve a `Escala`. As mesmas entradas dão a mesma escala, e nenhuma delas muda: o rodízio andado sai na escala, e é dele que a escala seguinte da categoria começa. A escala é feita depois de a habilitação fechar e antes de o período começar.
- **As requisições**: só as da categoria e do período da escalação, ou a conta recusa; as canceladas não entram; as que chegaram depois do prazo ficam de fora, e a escala as lista. As vagas são tomadas na ordem em que as requisições chegaram, terno a terno, na composição de cada terno.
- **As funções vão da que tem menos habilitados para a que tem mais**; no empate, a que aparece antes nas requisições. O sinaleiro que também é estivador pega primeiro a vaga que poucos podem pegar, e mais vagas são preenchidas. Quem pega uma vaga sai das outras funções.
- **Em cada função, a roda dos registrados e depois a dos cadastrados**, com as vagas que sobraram. Cada roda anda pela vez do rodízio (ADR-0006), conferindo, na hora, o descanso e a aptidão: quem não pode é pulado e perde a vez.
- **Cada habilitado tem a sua situação na escala**: a vaga que pegou ou os motivos, com o que os explica.
  - **Descanso:** dito uma vez só, porque vale para todas as funções. Vem com a jornada e a hora em que ele termina.
  - **Não pode trabalhar na função:** vem com os motivos da aptidão.
  - **O rodízio não chegou nele:** vem com quantas posições faltaram.
  - **As vagas ficaram com os registrados:** para o cadastrado.
  - **Ninguém pediu a função:** vale para o período inteiro.
- **As vagas vazias são listadas** por requisição, terno e função: a falta de mão de obra que o operador precisa saber.
- **A escala tem identidade própria**, e guarda a da escalação de onde saiu.

## Consequências

- A tela "por que não fui escalado" lê a situação do trabalhador e fala com ele em segunda pessoa, sem regra repetida no aplicativo.
- O operador fica sabendo das vagas vazias quando a escala sai, e não quando o terno chega incompleto ao navio.
- As regras são conferidas por propriedade em trezentas escalas sorteadas:
  - os escalados de cada roda são os primeiros da vez, pulando só quem não podia;
  - nenhum cadastrado pega vaga que um registrado podia pegar;
  - nenhuma vaga fica vazia com alguém habilitado que podia pegá-la;
  - a vez para no último escalado;
  - toda vaga pedida está preenchida ou listada como vazia.
- Na persistência (Parte 4), a escala e o rodízio andado são gravados na mesma transação: duas escalas da mesma categoria não podem partir do mesmo rodízio.
