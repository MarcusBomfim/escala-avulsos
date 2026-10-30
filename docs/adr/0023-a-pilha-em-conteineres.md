# ADR-0023 — A pilha em contêineres: três imagens de um Dockerfile só, e o compose como produção em miniatura

**Status:** aceita

## Contexto

Até a Parte 7, rodar a escala pedia o SDK do .NET, o Node e um PostgreSQL à mão, cada um na sua porta. Para quem desenvolve, serve. Para implantar, não: produção não compila código, e precisa de três coisas.

- **As imagens que vão rodar**, as mesmas em qualquer lugar.
- **O banco com o papel da aplicação**, que lê e grava as tabelas mas não muda o esquema (ADR-0012), e a migração feita por outro papel.
- **O aplicativo e a API na mesma origem**, como em desenvolvimento, em que o Vite repassa `/api` para a API, sem CORS. Duas coisas que só existem em produção podem quebrar o que já funciona:
  - o proxy na frente da API pode enfraquecer a ETag de que o órgão gestor depende para alterar com a versão lida (ADR-0014);
  - o proxy pode segurar os eventos do servidor de que depende a tela que se atualiza sozinha (ADR-0020).

## Decisão

- **Um Dockerfile, três alvos**, nas mesmas bases do crm-obras e do planejamento-obras, fixadas pelo digest:
  - `api`: a API publicada, sobre o ASP.NET sem shell e sem root. É a versão `-extra`, com a base de fusos: a escala inteira é no relógio do porto, e sem `America/Sao_Paulo` a API não sobe;
  - `migrador`: o pacote de migrações do EF Core, um executável que roda e sai, sem SDK;
  - `web`: o aplicativo compilado pelo Vite, servido pelo nginx sem root.
  - O .NET restaura e compila uma vez. A versão e o commit entram na compilação e aparecem no rastro (ADR-0022).
- **O compose sobe a escala inteira**:
  - o PostgreSQL;
  - o script que cria o papel `escala_aplicacao`, com os privilégios padrão do esquema;
  - o migrador, como dono das tabelas;
  - a API, como `escala_aplicacao`, com a entrega e a vigia rodando dentro dela;
  - o aplicativo;
  - o painel do .NET Aspire, que recebe a telemetria.
  - As portas são 5390 (aplicativo), 5391 (API) e 18890 (painel), longe das do crm-obras e do planejamento-obras, para as três pilhas rodarem juntas.
- **O nginx repassa `/api` sem gzip**, para a ETag chegar forte como a API mandou. O resto continua comprimido, e o que o Vite gera com nome que muda vai com cache de um ano.
- **Os eventos do servidor passam pelo mesmo caminho**, na hora. A API responde com `X-Accel-Buffering: no` para nenhum proxy guardar o evento, e o sinal de vida a cada vinte segundos mantém a ligação aberta.
- **O service worker e o manifesto**:
  - o `sw.js` vai sem cache, senão o celular ficaria com a casca velha depois de uma versão nova;
  - o manifesto sai com o tipo `application/manifest+json`.
- **A política de conteúdo só deixa rodar o que é desta origem.**
- **O push no compose é opcional**: as chaves VAPID ficam num `push.env` ao lado do `compose.yaml`, fora do Git, gerado pela própria API (`chaves-vapid`). Sem ele, os recados ficam só no aplicativo. O navegador aceita o push em `http://localhost`, e em qualquer outro lugar só em `https`.
- **O provedor de identidade do compose é o dos testes de ponta a ponta**: uma chave pública no repositório, que só vale aqui. Em produção, o do órgão gestor, pela mesma configuração.

## Consequências

- **O que vai para produção é o que roda na máquina de quem desenvolve**, com os mesmos papéis no banco e o mesmo proxy.
- **A ETag e os eventos atravessando o proxy viram teste** (ADR-0024). As duas promessas que um proxy mal configurado quebraria sem ninguém ver são conferidas nas imagens, antes de elas saírem.
- **A base sem shell não tem `curl`**: a saúde da API é consultada de fora, pelo orquestrador ou pelo `curl` de quem sobe a pilha, e não por um `HEALTHCHECK` dentro da imagem.
- **Atualizar uma base é trocar o digest dela**, nos três projetos ao mesmo tempo.
