# ADR-0015 — A criação repetida não cria duas vezes, e o que é só de quem pede o servidor refaz sozinho

**Status:** aceita

## Contexto

Pela rede, o mesmo pedido chega duas vezes. O sinal cai depois de o servidor gravar e antes de a resposta voltar, o cliente tenta de novo, ou o botão é apertado duas vezes. A requisição do operador é o caso que mais pesa: a mesma requisição duas vezes faz o órgão gestor escalar o dobro de gente, e ninguém vê.

A habilitação tem o problema contrário. Na abertura da janela, centenas de trabalhadores se habilitam na mesma escalação, e a concorrência otimista (ADR-0012) recusa todos menos um a cada rodada. Devolver 412 ao trabalhador, e pedir que ele "leia de novo e refaça", é empurrar para quem tem menos prática com o celular um problema que é do servidor.

## Decisão

**1. A criação sem identidade natural exige `Idempotency-Key`.** É o caso da requisição: duas requisições iguais podem ser legítimas, então só o cliente sabe se é a mesma. Sem a chave, a resposta é 400, e nada é criado.

**2. A chave é de quem pede**, o `sub` do token. A chave de um operador não esbarra na de outro.

**3. O pedido repetido recebe a resposta da primeira vez**, igual: o status, o corpo, o `Location` e a ETag, com `Idempotent-Replayed: true`. A mesma chave num pedido diferente, com outro caminho ou outro corpo, é 422.

**4. A chave se reserva na mesma transação da criação.** Ou ficam as duas, ou nenhuma: o pedido recusado pelo domínio não gasta a chave, e pode ser corrigido e mandado de novo com ela. Dois pedidos iguais ao mesmo tempo não criam duas vezes: o segundo espera o primeiro na chave primária, e recebe a resposta guardada.

**5. A criação pela identidade natural dispensa a chave.**

| Pedido | Repetido |
|---|---|
| cadastrar a matrícula | 409: a matrícula já existe |
| abrir a escalação da categoria no período | devolve a que já está aberta, e cinco ao mesmo tempo abrem uma |
| fazer a escala da escalação | 409: a escala já foi feita (ADR-0016) |

**6. O que é só de quem pede o servidor relê e refaz** quando outro gravou no meio: a habilitação, a troca de funções e a desistência da habilitação. A escalação é lida, a habilitação passa de novo pelo domínio e a gravação usa a versão recém-lida, até 50 vezes. Não há `If-Match`: o trabalhador não está editando a escalação inteira, só a parte dele. A vez é pela matrícula (ADR-0005), e a ordem em que as tentativas gravam não muda nada.

## Consequências

- **O aplicativo do trabalhador não conhece versão nem chave.** Habilitar-se é um `PUT` com as funções, e a resposta vem de primeira. Um teste habilita vinte trabalhadores no mesmo instante, e todos recebem 200.
- **Cada chave fica guardada com a resposta.** A limpeza das antigas entra com a operação do sistema (Parte 8).
- **Uma disputa maior que 50 tentativas chega como 412.** É o sinal de que a habilitação precisa de uma tabela própria, uma linha por trabalhador (ADR-0012).
