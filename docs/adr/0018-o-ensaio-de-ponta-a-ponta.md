# ADR-0018 — O ensaio de ponta a ponta: a API com o relógio do teste, e o celular de verdade

**Status:** aceita

## Contexto

A escala tem hora. A habilitação da tarde de quinta abre à 1h e fecha às 10h, e só depois a escala sai. A desistência vale até as 13h, e o extrato chega no dia seguinte. Um teste de ponta a ponta com o relógio de verdade teria de esperar horas, ou mudar o calendário do porto — e então não testaria o calendário de verdade.

O que o aplicativo promete a quem tem mais idade — o contraste, a letra que cresce sem nada vazar, os botões que o dedo acerta — só se confere num navegador que pinta a tela. O mesmo vale para abrir sem sinal: só se confere com um service worker de verdade.

## Decisão

**1. A API do ensaio** (`tests/EscalaAvulsos.Api.Ensaio`) é a mesma API — os mesmos serviços, o mesmo caminho de cada pedido —, com duas diferenças:

- **o relógio**, que o teste acerta por `PUT /ensaio/relogio`, sem token, e que anda dali em diante, como um relógio de verdade;
- **o banco**, apagado e migrado a cada subida.

Ela mora em `tests/`, não tem imagem e não vai para produção. A API de produção não ganhou nenhum gancho de teste.

**2. O navegador no mesmo relógio**: cada teste acerta a API e o navegador para o mesmo instante, pelo `clock` do Playwright. O relógio do navegador também anda: parado, ele não deixaria passar o segundo que o aplicativo espera entre uma gravação e outra da escala guardada.

**3. O exemplo do README, no celular de cada um**, com o Chrome numa tela de Android (Pixel 7, com toque):

- a preparação, pela API, como cada um faria do seu lado: o órgão gestor cadastra os seis e abre a escalação, a Cais Forte faz a requisição, e cinco se habilitam;
- o Antônio se habilita pelo celular às 8h;
- a escala sai às 10h, e ele lê onde trabalha e ouve;
- o Carlos lê por que ficou de fora;
- o Edson desiste às 11h, e no celular do Carlos a vaga já é dele;
- sem sinal, o aplicativo abre e a escala continua lá;
- no dia seguinte, o extrato.

**4. A acessibilidade conferida em cada tela**:

- o axe, no Chrome, com as WCAG 2.2 até o nível AA, as boas práticas e, do nível AAA, o contraste de 7:1, no tema claro, no escuro e no contraste alto;
- numa tela de 320px com a letra muito grande, nenhuma tela rola para o lado (WCAG 1.4.10), e nenhuma palavra de botão, do menu ou de título é cortada ao meio;
- todo botão tem pelo menos 48px de altura.

**5. As imagens do README saem dos testes**: com `IMAGENS=1`, cada tela é fotografada em `docs/imagens`. Quando a tela muda, a imagem muda junto.

## Consequências

- **O calendário do porto é o de produção** também no ensaio: o teste muda a hora, e não a regra.
- **Os testes andam em fila**, cada um de onde o anterior parou, como a tarde de quinta anda. Um que falha para os seguintes, e o relatório diz qual.
- **O CI sobe um PostgreSQL vazio**, compila a API do ensaio e o aplicativo de produção, e usa o Chrome que o runner já traz.
- **A voz não é ouvida**: o teste troca a voz do celular por uma que anota o que diria, e confere o texto.
