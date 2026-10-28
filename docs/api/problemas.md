# Os problemas que a API devolve

Todo erro da API sai em ProblemDetails (RFC 9457), com estes campos:

- `type` aponta para uma seção desta página;
- `title` é fixo por tipo;
- `detail` diz o que houve naquele pedido. Quando é uma regra do trabalho portuário que recusa, ele traz a frase do domínio, escrita para quem usa, e o aplicativo pode mostrá-la como está;
- `instance` traz o método e o caminho do pedido;
- `traceId` acha o pedido nos registros.

## pedido-invalido

**400 — O pedido não está no formato esperado.**

O corpo não é o JSON combinado. Pode ser:

- JSON malformado;
- um campo que falta ou sobra;
- nulo onde não cabe;
- enum em número ou número em texto;
- um dia que não está no formato `2026-11-05`;
- um cabeçalho malformado;
- a chave de idempotência que falta.

O `detail` diz qual e onde.

## nao-autenticado

**401 — É preciso entrar com a matrícula ou com o usuário.**

Não veio token, ou o que veio não serve: vencido, de outro emissor, para outro sistema, assinado com outra chave. Sem token, nem o endereço que não existe se revela: a resposta é a mesma.

## sem-permissao

**403 — Sem permissão para este pedido.**

O token é válido, mas o papel dele não é o deste pedido. Isso também vale quando o token não traz o que o papel exige: a matrícula do trabalhador ou o CNPJ do operador.

## nao-encontrado

**404 — Não encontrado.**

O que o endereço aponta não existe: uma matrícula fora do cadastro, uma requisição, uma escalação ainda não aberta, uma escala ainda não feita. Também vale para o endereço que a API não tem.

## metodo-nao-permitido

**405 — Este endereço não aceita este método.**

## ja-existe

**409 — Já existe.**

O pedido repete o que identifica outro registro:

- a matrícula ou o CPF de um trabalhador;
- a escala de uma escalação que já foi feita.

## jornada-sem-descanso

**409 — Alguém ficaria em duas jornadas sem o descanso entre elas.**

O banco recusou a vaga porque, no meio do caminho, o trabalhador pegou outra jornada que não cabe com o descanso (ADR-0013). O `detail` diz quem e onde. Basta repetir o pedido: a API lê as jornadas de novo.

## versao-desatualizada

**412 — A versão informada não é mais a atual.**

Alguém gravou depois da leitura de quem pede. O corpo traz `versaoAtual`. Leia de novo, confira o que mudou e refaça a alteração.

## tipo-nao-suportado

**415 — O corpo do pedido precisa ser JSON.**

## regra-do-negocio

**422 — O pedido fere uma regra do trabalho portuário.**

O domínio recusou, e o `detail` traz a frase dele, com todos os problemas de uma vez. Exemplos:

- "O CPF 529.984.127-25 não confere: os dígitos verificadores não batem com o número."
- "A habilitação para a escala da estiva de 05/11/2026, das 13h às 19h, fechou em 05/11/2026, às 10h."

## chave-reutilizada

**422 — A chave de idempotência já foi usada em outro pedido.**

A mesma chave veio num pedido diferente do primeiro: outro caminho ou outro corpo. Pedido novo leva chave nova.

## versao-ausente

**428 — Falta a versão lida, no cabeçalho If-Match.**

Toda alteração de um registro diz a versão que leu: a ETag da leitura, como `"3"`.

## erro-interno

**500 — Erro interno.**

O detalhe fica nos registros, com o `traceId`; quem pediu recebe só o `traceId`.
