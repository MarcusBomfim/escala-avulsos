# ADR-0017 — O aplicativo do trabalhador: no celular, para quem tem mais idade, e sem sinal no cais

**Status:** aceita

## Contexto

O sistema de hoje não cabe no celular, tem letra e botões pequenos, e a escala, a tela mais usada, é uma tabela cheia de códigos em que o trabalhador procura a própria matrícula ([análise](../analise-do-sistema-atual.md)). Quem mais usa a escala são trabalhadores com décadas de cais, que aprenderam a profissão na escala presencial, e que muitas vezes estão no cais, onde o sinal falha.

A API já entrega o que o trabalhador precisa (ADR-0016). Faltam a tela, e uma coisa na API: para se habilitar, o trabalhador teria de saber de cor o dia e o turno de cada escalação aberta.

## Decisão

**1. Um aplicativo web instalável (PWA)**, em React e TypeScript, no mesmo desenho do front-end do CRM de obras: os tipos gerados do contrato publicado (`docs/api/openapi.json`, pelo `openapi-typescript`), o cache das leituras no TanStack Query, as áreas encontradas pela compilação (cada pasta com um `modulo.tsx`), e a API de mentira do MSW nos testes. Sem loja de aplicativos: abre-se pelo navegador e instala-se na tela inicial. O aplicativo do OGMO Santos saiu da Play Store, e um PWA não depende de nenhuma loja.

**2. Quatro telas, e a primeira é a escala.**

- **Minha escala**: hoje e os próximos seis dias, um cartão por período, com o dia e o horário escritos por extenso ("Hoje, quinta-feira, 5 de novembro", "Tarde, das 13h às 19h"). Em cada cartão, a situação num selo com a palavra, e:
  - se ele vai trabalhar, a função, o terno, o navio, o local e quem pediu;
  - se ficou de fora, os motivos e o lugar na espera.
- **Habilitar**: as escalações abertas da categoria dele.
- **Meu mês**: a frequência e o extrato.
- **Meus dados**: o cadastro, com o aviso do exame ou da reciclagem que vai vencer.

**3. A API diz onde ele pode se habilitar, e com que funções.**

- `GET /api/eu/habilitacoes` traz as escalações abertas da categoria dele cujo período ainda não começou, na ordem dos períodos.
- Cada habilitação passa a trazer as opções: as funções em que ele é qualificado, se pode exercê-las no período e, se não pode, os motivos, como o domínio os diz ("o exame médico (ASO) vale só até 30/11/2026").

O "não" aparece antes de ele tentar. As funções que ele pode escolher já vêm marcadas: habilitar-se é um toque.

**4. O aplicativo fala com o trabalhador em segunda pessoa.** A API manda a frase do órgão gestor ("Antônio Ferreira (matrícula 4127) está na escala…") e os dados. O aplicativo monta a frase dele: "Você vai trabalhar", "Você é o 1º da espera de sinaleiro". "Hoje" e "amanhã" são do aplicativo, porque dependem de quando se lê, e são contados no relógio do porto, e não no do celular. Os motivos do domínio já falam com ele ("o rodízio parou 1 posição antes da sua vez") e vão como estão.

**5. Feito para quem tem mais idade:**

- **A letra**:
  - parte de 18px e aumenta com um toque — normal, grande, muito grande —, num botão em cima de toda tela;
  - tudo é medido em rem, então os botões e os espaços crescem junto, e o tamanho de letra do próprio celular também vale;
  - com a letra maior, o menu passa de quatro colunas para duas, para nenhuma palavra ser cortada.
- **As cores**:
  - o texto tem contraste de pelo menos 7:1 (WCAG AAA) no tema claro, no escuro e no contraste alto (branco no preto, com amarelo no que se toca), que liga com um toque e segue o celular que já pede mais contraste.
- **Os botões**:
  - todo botão tem pelo menos 48px de altura e uma palavra;
  - o ícone, quando há, ajuda a achar, mas é a palavra que diz;
  - a situação tem cor, mas nunca só cor.
- **Ouvir**: cada cartão tem o botão "Ouvir", que lê a escala em voz alta pela voz do próprio celular, com o horário por extenso ("da uma da tarde às sete da noite"), porque "13h" lido por máquina vira "treze agá".
- **Nada sem explicação**: nenhuma sigla aparece sozinha — "Exame médico (ASO)", "FGTS — o Fundo de Garantia" —, e o que pode tirar o trabalhador da escala é avisado com o prazo, antes de acontecer.
- **Desistir pergunta antes**: a pergunta, escrita por inteiro, aparece no lugar do botão, sem janela por cima. O "sim" e o "não" têm o mesmo tamanho, e o leitor de tela vai direto para ela.

**6. Sem sinal no cais, o aplicativo abre, e a escala está lá.**

- **A casca**: um service worker escrito à mão (`web/public/sw.js`, sem Workbox) guarda a página e os arquivos dela, lidos da própria página, e os renova a cada vez que o aplicativo abre com sinal.
- **A escala**: a última escala lida fica guardada no aparelho, pelo persistidor do TanStack Query, com a hora em que foi lida. Só a escala — o que ele precisa para chegar ao trabalho —, e não o cadastro, que tem o CPF.
- **O aviso**: sem internet, a tela diz que está sem internet e que o que aparece é o que ficou guardado.
- **O que ele manda**: a habilitação e a desistência não esperam o sinal voltar. Falham na hora, dizendo por quê, em vez de chegarem horas depois, quando já não valem.

**7. O token fica no aparelho**, e não só na aba: o celular é de uma pessoa, e quem tem mais idade não deveria entrar de novo a cada vez que abre a escala. Sai-se pelo botão vermelho do canto de cima, onde ele fica no sistema de hoje, ou quando o token vence. Sair apaga também a escala guardada. Em produção, a entrada é a do provedor de identidade, com a matrícula e a senha; em desenvolvimento, cola-se o token do `dotnet user-jwts`.

**8. A cara é a do sistema que o trabalhador já usa** ([análise](../analise-do-sistema-atual.md#a-área-logada-por-dentro)). Quem tem décadas de cais não precisa aprender uma tela nova para ler a escala: o que muda é o tamanho, e não o lugar das coisas.

- **O que vem de lá**:
  - a faixa azul no alto, com quem entrou e o "Sair" vermelho no canto;
  - as abas cinza sobre o azul, com a aberta em azul-escuro;
  - a barra cinza embaixo delas, com o relógio;
  - o título em azul-marinho com a linha grossa embaixo, e a linha no pé;
  - os rótulos em letra de forma no cinza-azulado, como nos engajamentos, e a situação escrita com a bolinha da cor, como o "Travada" do quadro;
  - a entrada num cartão branco no meio da tela, com o botão azul da largura toda;
  - a letra Helvetica, ou Arial.
- **O que não vem**:
  - a marca e o nome do órgão gestor: o projeto é independente;
  - a letra de 13px, os botões de 27px e a tabela larga que pede rolagem para o lado;
  - as cores que não chegam a 7:1. A faixa fica um tom mais clara, para o azul-marinho ficar legível em cima dela, e o vermelho do "Sair" fica um tom mais escuro.
- **O relógio marca a hora do porto**, e escrita como o resto do aplicativo — "Agora: 9h40" —, para comparar de olho com "a habilitação fecha às 10h".

## Consequências

- **A acessibilidade é conferida por teste**: o axe roda em cada tela nos testes de unidade, com as WCAG 2.2 até o nível AA e as boas práticas. As cores, que o jsdom não pinta, são conferidas no Chrome, nos testes de ponta a ponta (ADR-0018).
- **O contrato manda nas telas**: os exemplos dos testes são tipados pelo contrato, e o CI regera os tipos e falha se eles mudarem sem commit.
- **O service worker é pouco código, e todo nosso**: não há atualização em segundo plano nem aviso de versão nova. A casca renovada vale a partir da próxima abertura.
- **"Hoje" no aparelho e na API podem divergir** se o relógio do celular estiver errado. A semana mostrada é a da API, e a tela não escreve as datas dela no subtítulo.
- **Os avisos — a escala que saiu, a habilitação que vai fechar — ficam para a Parte 7**, com o tempo real.
