# ADR-0014 — O contrato HTTP: entrada pela matrícula, três papéis, versão na ETag, erro em ProblemDetails

**Status:** aceita

## Contexto

A Parte 5 abre a escala pela rede. Quem chama precisa saber, sem ler o código, como se identifica, o que pode fazer, como sabe que o que leu ainda vale e o que deu errado. São três clientes: o aplicativo do trabalhador, no celular (Parte 6), o painel do órgão gestor e o sistema do operador portuário.

O trabalhador é o caso que mais pesa. Ele se identifica pela matrícula, como faz hoje no sistema do órgão gestor. Muitas vezes é alguém de mais idade, no cais, com sinal fraco. Uma tela que diz "a versão mudou, tente de novo" para ele é uma tela que falhou.

## Decisão

**1. Autenticação por JWT, emitido por um provedor de identidade.** A API não guarda senha nem emite token. Ela confere assinatura, emissor, audiência e validade, configurados em `Authentication:Schemes:Bearer`: em produção, um provedor OIDC; em desenvolvimento, o `dotnet user-jwts`. É o provedor que confere a matrícula e a senha do trabalhador, e o CNPJ e a senha do operador.

**2. Três papéis, na reivindicação `role`:**

| Papel | Quem | Política | O token traz também |
|---|---|---|---|
| `trabalhador` | o avulso | `trabalhador` | `matricula` |
| `escalador` | quem trabalha no órgão gestor | `escalar` | — |
| `operador` | o operador portuário | `requisitar` | `cnpj` e `name` |

A política de reserva exige um dos três papéis. O endpoint que esquecer de declarar a sua, ou a rota que nem existe, não fica aberta. Sem token, a resposta é 401, que não revela o que existe. Token sem a matrícula ou o CNPJ válido que o papel pede recebe 403.

**3. Quem pede vem do token, e de nenhum outro lugar.** O trabalhador usa `/api/eu`: não há matrícula no caminho nem no corpo, e ele não alcança os dados de outro. A matrícula fora do cadastro é 404, com a frase que diz isso. O operador faz requisições em nome do CNPJ do token, e só enxerga as dele.

**4. A matrícula é o endereço do trabalhador**: `/api/trabalhadores/4127`. É como ele é conhecido no porto, e é única no banco (ADR-0012). O id interno não sai da API.

**5. A versão do registro é a ETag**, forte: `"3"`, em toda leitura e toda alteração do escalador e do operador. Alterar exige `If-Match` com a versão lida. Sem ele, a resposta é 428. Com uma versão que já não é a atual, é 412, com `versaoAtual` no corpo. A versão é conferida antes de aplicar a alteração, e de novo pelo banco, ao gravar. Na leitura, `If-None-Match` com a versão atual dá 304.

**6. Todo endpoint que grava declara uma coisa só**, nos metadados:

- **exige a versão lida**, quando altera um registro que alguém leu e editou;
- **exige `Idempotency-Key`**, quando cria um registro sem identidade natural, como a requisição (ADR-0015);
- **dispensa as duas, e diz por quê.** É a criação pela identidade natural: a matrícula, a escalação da categoria no período, a escala da escalação, em que a repetição é recusada ou devolve o que já existe. E é a alteração só da parte de quem pede, que o servidor relê e refaz sozinho: a habilitação e a desistência do trabalhador (ADR-0015, ADR-0016).

Um teste percorre os endpoints mapeados e falha se algum gravar sem declarar, ou declarar mais de uma. O documento OpenAPI descreve os cabeçalhos e o motivo de quem dispensa.

**7. Uma alteração por pedido, pelo nome do que se faz**: `POST /api/trabalhadores/4127/qualificacoes`, `PUT /api/trabalhadores/4127/aso`, `POST /api/trabalhadores/4127/registro`. É o domínio que confere cada passo, e o erro fala do passo que falhou. Toda alteração devolve o registro como ficou.

**8. Todo erro sai em ProblemDetails** (RFC 9457), de um catálogo em [`docs/api/problemas.md`](../api/problemas.md). Os endpoints não tratam erro: lançam, e a tradução é uma só.

| Status | Quando |
|---|---|
| 400 | o pedido não é o combinado: JSON malformado, campo que falta ou sobra, enum em número, cabeçalho inválido |
| 401 | sem token válido |
| 403 | o token não satisfaz a política do endpoint |
| 404 | não existe |
| 409 | repete o que identifica outro registro, ou a vaga cruzaria com outra jornada sem o descanso |
| 412 | a versão informada já não é a atual |
| 415 | o corpo não é JSON |
| 422 | regra do trabalho portuário, com a frase do domínio, todos os problemas de uma vez |
| 428 | alteração sem a versão lida |
| 500 | erro interno: o detalhe fica nos registros, e quem pediu recebe só o `traceId` |

**9. Entrada estrita**, como na leitura dos documentos (ADR-0011). Dão 400, com a mensagem dizendo qual:

- o campo obrigatório que falta;
- o nulo onde não cabe;
- o campo que o contrato não tem;
- o enum em número;
- o número em texto.

Opções no caminho, como `Tarde` ou `Estiva`, também vão só pelo nome. CPF, CNPJ e número IMO chegam como texto, do jeito que as pessoas escrevem, e o domínio os lê. Pedidos e respostas são tipos próprios da API, separados do domínio.

**10. A configuração do porto é da instalação**, na seção `Porto`: os horários da escalação, o descanso entre jornadas, a frequência mínima e a tabela de remuneração. O que falta fica com o padrão do domínio, menos a tabela, que é da convenção de cada porto. Configuração que o domínio recusa impede a API de subir.

## Consequências

- **O cliente sabe o que fazer com cada resposta** sem ler o código: o `type` do problema é estável, a ETag diz a versão, e a dispensa diz por que a repetição é segura.
- **Trocar o provedor de identidade é configuração.** A API depende do formato do token, e não de quem o emite.
- **O aplicativo do trabalhador não lida com versão.** Tudo o que ele grava é só dele, e o servidor relê e refaz quando outro gravou no meio.
- **Cada teste da API sobe a API inteira, com um banco só dele.** Custa um pouco mais de tempo, e nenhum teste escala no rodízio de outro.
