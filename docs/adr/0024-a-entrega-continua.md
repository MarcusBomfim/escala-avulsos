# ADR-0024 — A entrega contínua: as imagens testadas são as publicadas

**Status:** aceita

## Contexto

O CI confere o código: a compilação sem aviso, a formatação, os testes do domínio, do banco e da API, e o aplicativo. O ensaio de ponta a ponta confere a tarde de quinta no Chrome, contra a API do ensaio, com o relógio acertado (ADR-0018).

Nenhum dos dois confere a imagem que vai para produção. E as imagens podem quebrar o que o código não quebra:
- uma base sem a base de fusos;
- um papel do banco sem permissão;
- o nginx comprimindo a resposta da API;
- o evento preso no proxy (ADR-0023).

## Decisão

- **O workflow `Entrega`** roda depois do CI verde de cada push na `main`, e em cada tag `v*.*.*`. A tag só sai se o commit dela passou no CI.
- **As três imagens são compiladas uma vez, como candidatas**, com a versão, o commit e a origem nas etiquetas.
- **A pilha do compose sobe com as candidatas.** A entrega espera `/saude/pronta`, que confere três coisas:
  - o banco com o papel da aplicação;
  - a escuta de pé;
  - os laços dos recados girando.
- **Um teste de fumaça, no Chrome de celular, contra as candidatas** (`npm run teste:imagens`). O relógio é o de verdade, porque a API de produção não tem o do ensaio. Por isso a fumaça confere as peças se encaixando, e não a tarde de quinta:
  - o trabalhador cadastrado pela mesma origem do aplicativo, com a ETag chegando forte, sem o `W/` do gzip;
  - a tela dele abrindo, ligada ao vivo, com o axe;
  - o quadro do escalador mudando sozinho quando uma escalação abre, com o evento atravessando o nginx;
  - o service worker sem cache e o manifesto com o tipo certo.
  - Cada execução usa uma matrícula e um dia sorteados: o banco da pilha local sobrevive de uma execução para a outra.
- **Só as imagens que passaram são publicadas**, no GitHub Container Registry, em `ghcr.io/marcusbomfim/escala-avulsos-{api,migrador,web}`:
  - da `main`, com as tags `main` e `sha-…`;
  - de uma versão, com a versão, a versão sem o último número, `latest` e o `sha-…`.
  - É a mesma imagem, com o mesmo digest, que foi testada.
- **Cada imagem leva a proveniência assinada pelo GitHub**: de que commit e de que execução ela saiu. Quem baixa confere com `gh attestation verify`. A versão vira uma release com o digest de cada imagem.
- **Com `IMAGENS=1`, a fumaça fotografa o rastro da escalação aberta** no painel do OpenTelemetry, para o README.

## Consequências

- **O que sai é o que foi testado.** Não há uma segunda compilação entre o teste e a publicação.
- **Uma imagem quebrada não sai**, mesmo com o código verde: a falha deixa o log da pilha e o relatório do Playwright como artefato.
- **Os pacotes nascem privados no GitHub.** Torná-los públicos é uma decisão de quem é dono do repositório, nas configurações de cada pacote.
- **A entrega para na publicação.** Implantar num servidor, com o domínio, o `https` e as chaves VAPID de verdade, depende de onde a escala vai rodar, e fica para quem implantar.
