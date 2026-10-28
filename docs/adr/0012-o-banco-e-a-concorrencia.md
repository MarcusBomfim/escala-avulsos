# ADR-0012 — O PostgreSQL guarda os documentos, e ninguém grava por cima do que não viu

**Status:** aceita

## Contexto

Os agregados viram documentos (ADR-0011) e vão para o PostgreSQL. Algumas perguntas se fazem sem abrir documento nenhum: o trabalhador da matrícula 4127, as requisições da estiva para a tarde de quinta, a escalação daquele período, as escalações de um trabalhador no mês.

E a escalação é a gravação mais disputada do sistema. Quando a janela de habilitação abre, centenas de trabalhadores se habilitam na mesma escalação ao mesmo tempo, cada um pelo celular. Sem controle, quem grava por último apaga, sem saber, a habilitação de quem gravou no meio, e alguém fica fora da escala achando que estava nela.

Trancar a escalação enquanto alguém se habilita não serve. A habilitação atravessa pedidos HTTP (Parte 5), e o celular pode perder o sinal no cais com a tranca na mão.

## Decisão

**1. Uma tabela por agregado, uma linha por agregado, o agregado inteiro numa coluna `jsonb`**, no esquema `escala`, com nomes em snake_case. Ao lado do documento, só o que o banco precisa enxergar: identidade, versão, formato, quando foi gravado e a categoria. Também vai em coluna o que se procura sem abrir o documento: a matrícula e o CPF do trabalhador, o início e o dia do período, quem se habilitou na escalação e quem aparece na escala. As colunas são cópias do documento, gravadas junto com ele.

**2. O banco garante o que consegue sem repetir regra de negócio:**

- versão e formato a partir de 1, e o documento é um objeto JSON;
- a categoria é uma das que o domínio conhece;
- a matrícula vai de 1 a 9999999, o CPF tem 11 dígitos, e nenhum dos dois se repete;
- cada categoria tem uma escalação por período, e cada escalação tem uma escala;
- a escala aponta para uma escalação que existe.

As restrições têm nome, e o erro diz qual foi violada. O resto das regras fica no domínio, e vale também na leitura.

**3. Concorrência otimista, com a versão conferida num comando só.** Cada linha tem uma versão inteira, que começa em 1 e sobe a cada gravação. Atualizar pede a versão que foi lida: `update … where id = @id and versao = @lida`. Se outra gravação passou no meio, nenhuma linha muda, e só então uma consulta separa "mudou" de "não existe". Não há janela entre conferir e gravar. A versão fica fora do domínio: o repositório devolve um `Versionado<T>`, com o agregado e a versão lida.

**4. Na habilitação, quem perde a corrida lê de novo e refaz.** Como a vez é pela matrícula, e não pela ordem de chegada (ADR-0005), refazer não custa nada a ninguém. Um teste habilita vinte trabalhadores no mesmo instante, cada um na sua conexão: no fim estão os vinte, e a escalação andou vinte versões.

**5. O que o PostgreSQL responde vira exceção com nome, e mensagem para gente:**

| O banco responde | A persistência lança |
|---|---|
| nenhuma linha mudou, e a linha existe | `ConflitoDeConcorrencia`, com a versão atual: "O trabalhador … mudou desde a versão 1: está na versão 2. Leia de novo e refaça a alteração." |
| nenhuma linha mudou, e a linha não existe | `AgregadoNaoEncontrado` |
| chave primária repetida | `AgregadoJaExiste`: "Já existe o trabalhador …" |
| matrícula ou CPF repetido | `AgregadoJaExiste`: "Já existe trabalhador com a matrícula 4127." |
| outra escalação da categoria no período | `AgregadoJaExiste`: "A escala da estiva de 05/11/2026, das 13h às 19h, já está aberta…" |
| documento que não se lê (ADR-0011) | `DocumentoInvalido`, com o id e o motivo |

A restrição que ninguém previu não vira mensagem: é defeito, e sobe como tal.

**6. Nada fica no rastreador de mudanças do Entity Framework.** Lê-se sem rastreio, grava-se direto no banco, e o contexto não guarda estado entre uma operação e outra. O relógio é injetado, e os instantes vão em UTC, que é o que o `timestamptz` recebe. O fuso do porto está no domínio (ADR-0002).

**7. A aplicação conecta com um papel comum**, que lê e grava as tabelas e não muda o esquema. As migrações rodam com o dono do banco. Os testes fazem igual, num PostgreSQL de verdade, num contêiner. Cada teste ganha um banco só seu, copiado de um banco-modelo já migrado, porque a matrícula, o CPF e a escalação de cada período não se repetem. Sem Docker, os testes de banco são pulados com o motivo; no CI, Docker indisponível é falha.

## Consequências

- **Nenhuma habilitação apaga outra em silêncio.** O conflito chega a quem gravou por último, com a versão atual, e refazer é ler de novo.
- **Ler e gravar um agregado é uma linha**, sem junção e sem N+1.
- **Consultar dentro dos documentos é mais caro.** O que se pergunta com frequência vira coluna, numa migração; o resto, os operadores do `jsonb` respondem.
- **Uma escalação muito disputada custa releituras.** Cada conflito é uma leitura e uma gravação a mais, só quando acontece. Se a abertura da janela virar gargalo, a habilitação pode ir para uma tabela própria, uma linha por trabalhador, sem mudar o domínio.
