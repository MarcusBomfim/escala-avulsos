# ADR-0022 — A observabilidade: o OpenTelemetry, a saúde que olha o que é só da escala, e o que a escala mede

**Status:** aceita

## Contexto

Desde a Parte 7, a API não só responde a pedidos. Três coisas rodam sozinhas dentro dela:

- a escuta do banco, de que depende a tela que se atualiza sozinha (ADR-0020);
- a entrega dos recados aos celulares;
- a vigia dos prazos (ADR-0019).

Quando uma delas para, nada dá erro na tela. A escala que sai simplesmente não aparece no celular aberto, a notificação não chega, e o aviso de que a habilitação vai fechar não sai. É justamente o que o trabalhador mais precisa (ADR-0019), e ninguém percebe.

Quem opera precisa de três respostas:

- se o processo está de pé e girando;
- se ele alcança o banco;
- o que aconteceu com cada recado.

O crm-obras e o planejamento-obras já respondem às duas primeiras do mesmo jeito.

## Decisão

- **OpenTelemetry, num projeto próprio**, `EscalaAvulsos.Observabilidade`, igual ao dos outros dois sistemas:
  - os pedidos HTTP, os comandos no PostgreSQL, os pedidos aos serviços de push e o runtime do .NET;
  - os logs no mesmo rastro;
  - o destino é configurado pelas variáveis padrão (`OTEL_EXPORTER_OTLP_ENDPOINT`). Sem elas, nada sai do processo.
  - O nome do serviço é `escala-avulsos-api`, com a versão e o commit da compilação.
- **O que começa com `EscalaAvulsos.` entra sozinho.** A escala tem uma fonte de rastro e dois medidores:
  - `EscalaAvulsos.Avisos`, com um rastro por volta da entrega e por conferência da vigia;
  - o medidor `escala.avisos.entregas`, que conta cada celular chamado pelo resultado: entregue, recusado ou inscrição vencida;
  - o medidor `escala.avisos.prazos`, que conta os recados que a vigia deixou;
  - `EscalaAvulsos.TempoReal`, com as telas ligadas agora (`escala.eventos.ligacoes`) e as mudanças mandadas a elas, pela origem.
  - Nenhum atributo leva matrícula, nome ou endereço de celular. São dados de gente, e multiplicariam as séries.
- **Os medidores nascem da fábrica de medidores (`IMeterFactory`).** O teste confere as medidas da API dele, e só as dele, mesmo com os outros testes rodando ao mesmo tempo.
- **A saúde, em `/saude/viva` e `/saude/pronta`, sem token e fora dos rastros:**
  - **Viva** confere que os laços da entrega e da vigia deram uma volta há pouco: no máximo três intervalos e mais um minuto. O laço preso não volta sozinho, e reiniciar o processo o põe para girar de novo. Quando os laços não rodam, como nos testes, não há o que conferir.
  - **Pronta** é o banco respondendo a um `select 1` em até cinco segundos. Sem o banco, o servidor sai da fila de pedidos até ele voltar, porque reiniciar não resolve.
  - **A escuta do banco caída deixa a API pronta, mas "Degraded".** Ler e gravar continuam funcionando, e a escuta volta sozinha, com espera crescente. Tirar o servidor do ar por isso deixaria todos sem a escala, e não só sem a atualização sozinha. A resposta diz o porquê em português: "as telas não se atualizam sozinhas até ela voltar".
  - A resposta diz a situação de cada verificação, e nada do erro por trás dela: o endereço do banco vai para o log, e não para quem pergunta.

## Consequências

- **A escala que não chega ao celular aparece em algum lugar.** Os recados recusados crescem no medidor, o laço parado derruba a `viva`, e a escuta caída aparece na `pronta`.
- **O painel mostra a mesma coisa que o do crm-obras e o do planejamento-obras.** Quem opera os três lê um só jeito.
- **A observabilidade custa um pouco a cada pedido**, uma atividade e algumas medidas. Sem destino configurado, o custo é só esse, e nada sai do processo.
- **Os nomes das medidas são contrato com os painéis.** Mudar um é mudar o painel junto.
