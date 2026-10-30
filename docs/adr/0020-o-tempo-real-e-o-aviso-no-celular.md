# ADR-0020 — O tempo real e o aviso no celular: o banco anuncia, o servidor transmite, e o push vai cifrado

**Status:** aceita

## Contexto

Há dois jeitos de alguém ficar sabendo do que mudou.

- **Com o aplicativo aberto**, a escala que sai precisa aparecer sozinha na tela do trabalhador. O quadro do escalador também precisa se mexer a cada habilitação, sem ninguém apertar "atualizar".
- **Com o aplicativo fechado**, que é o caso mais comum, o aviso precisa chegar ao celular como notificação (ADR-0019).

Há mais de um servidor da API, e o que um grava os outros precisam ficar sabendo. Um canal em memória não serve; um serviço de mensagens a mais, só para isso, também não.

## Decisão

**1. O banco anuncia o que mudou**, com `pg_notify`, em gatilhos nas tabelas das escalações, das escalas, das requisições e dos avisos. O anúncio diz só onde olhar: a tabela, a categoria e o início do período, ou o trabalhador com aviso novo. Nada da escala vai junto.

- **O anúncio sai na transação de quem gravou**: só sai se ela for confirmada, e sai uma vez só para o mesmo período, mesmo que ela grave várias linhas dele.
- **Ninguém precisa lembrar de avisar.** O repositório que grava não sabe do anúncio, e o gatilho vale também para quem gravar por outro caminho.

**2. Cada servidor escuta numa conexão só sua** (`LISTEN escala_mudou`) e repassa o anúncio a quem está conectado nele. Se a conexão cai, ele abre de novo, e cada conectado recebe `pronto` outra vez: o que mudou no meio não volta, então quem está do outro lado lê de novo.

**3. O aplicativo aberto recebe os anúncios por Server-Sent Events**, em `GET /api/eventos`.

- **Com o token no cabeçalho**, como todo pedido. O `EventSource` do navegador não manda cabeçalho, então o aplicativo lê o fluxo pelo `fetch`. Token no endereço iria parar nos registros de acesso.
- **Ao conectar**, chega `event: pronto`. Depois, um `event: mudou` por anúncio: `{"origem":"escalas","categoria":"Estiva","dia":"2026-11-05","turno":"Tarde"}`, ou `{"origem":"avisos"}`. A cada vinte segundos, um comentário mantém a ligação de pé nos proxies.
- **Cada um recebe o que é dele.** O trabalhador recebe as escalas da categoria dele e os avisos dele. O escalador e o operador recebem as escalações, as escalas e as requisições de todas as categorias, para o quadro (ADR-0021).
- **O anúncio não traz o dado**: quem recebe pede de novo, pelo endereço de sempre, e a autorização de cada leitura continua a mesma. Sem a ligação aberta, o aplicativo funciona como antes: lê ao abrir e quando se pede.

**4. O aviso vai ao celular por Web Push**, o padrão dos navegadores, que funciona no Android e no iPhone com o aplicativo instalado na tela inicial.

- **O servidor se identifica com chaves VAPID** (RFC 8292), da configuração (`Avisos:Push`). Cada envio leva um JWT assinado em ES256, que vale por doze horas. Sem as chaves, o push fica desligado, e o resto continua.
- **A mensagem vai cifrada para aquele navegador só** (RFC 8291): uma chave de curva elíptica nova a cada mensagem, o segredo combinado por ECDH e o conteúdo em AES-128-GCM. A cifra é feita com a criptografia do próprio .NET, sem biblioteca de fora, e o teste a confere byte a byte com o exemplo da RFC.
- **O navegador inscrito** guarda o endereço do serviço de push e as duas chaves dele, por trabalhador, em `PUT /api/eu/avisos/inscricao`. O endereço é a chave da linha: o celular que passa a ser usado com outra matrícula leva os avisos de quem entrou por último.
- **Quem entrega é o servidor, pela caixa de saída da ADR-0019.** Cada aviso novo acorda a entrega, e a cada trinta segundos ela olha de novo o que ficou. A resposta do serviço de push decide o resto:
  - "201" entregou;
  - "404" ou "410" quer dizer que a inscrição venceu, e ela é apagada;
  - qualquer outra resposta faz tentar de novo depois.
- **Prazo, pressa e substituição** dependem do aviso:
  - a habilitação que vai fechar vale por uma hora e vai com pressa;
  - a escala e a vaga valem por doze horas e também vão com pressa;
  - o exame e a reciclagem valem por três dias;
  - o tópico (`Topic`) é tirado da chave do aviso, e o aviso novo do mesmo fato substitui o que ainda não chegou.

## Consequências

- **Nenhum serviço novo para implantar.** O PostgreSQL que já existe faz o papel do barramento, e o que muda na implantação são as chaves VAPID.
- **O anúncio não tem fila**: perdido com a conexão, ele não volta. Quem depende dele lê de novo no `pronto`, e o push, que não pode se perder, sai do banco, e não do anúncio.
- **O push depende de serviços de fora** (Google, Mozilla, Apple), que podem atrasar a entrega. O recado continua no aplicativo de qualquer jeito.
- **A cifra é código nosso**, conferido com os exemplos das RFCs. Se um serviço de push mudar o que aceita, o teste do carteiro é o lugar de reproduzir.
