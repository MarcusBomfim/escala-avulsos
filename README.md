# Escala dos Avulsos

[![CI](https://github.com/MarcusBomfim/escala-avulsos/actions/workflows/ci.yml/badge.svg)](https://github.com/MarcusBomfim/escala-avulsos/actions/workflows/ci.yml)
[![Licença: MIT](https://img.shields.io/badge/licen%C3%A7a-MIT-blue.svg)](LICENSE)

A escala do trabalhador portuário avulso, feita para o celular e para quem já não enxerga tão bem: o estivador abre o aplicativo e lê, numa frase, onde trabalha amanhã — ou por que ficou de fora, e até quando. Por trás da tela, o que a escala precisa saber: quem pode trabalhar em cada função, o pedido do operador portuário, o rodízio, o descanso entre jornadas — e, nas próximas partes, o extrato, os avisos e o aplicativo.

C# 14 · .NET 10 · xUnit v3 · GitHub Actions

> Projeto independente, de estudo e portfólio. Não tem ligação com o OGMO/Santos nem com nenhum órgão gestor de mão de obra; os nomes de pessoas, empresas, navios e terminais dos exemplos são inventados.

## O problema

No porto de Santos, mais de seis mil trabalhadores portuários avulsos — estivadores, conferentes, vigias, trabalhadores da capatazia — não têm patrão fixo. A cada período de seis horas, o órgão gestor de mão de obra reparte as vagas que os operadores portuários pediram, em rodízio, para que o trabalho se divida entre todos. Desde 2019 a disputa pelas vagas pode ser feita pelo celular, e em 2020, com a pandemia, passou a ser só digital.

Entre esses trabalhadores há muitos com décadas de cais, que aprenderam a profissão na escala presencial, e não no celular. Quem usa o sistema atual todo dia conta que:

- **ele não cabe no celular** — o layout é de computador, e é preciso dar zoom e arrastar para ler;
- **ele não foi feito para quem tem mais idade** — letra pequena, botões pequenos, siglas, caminhos longos;
- **a escala, a tela mais usada, é a mais difícil de ler** — o trabalhador procura a própria matrícula no meio de uma tabela cheia de códigos.

As fontes públicas mostram o resto: o tempo aperta (um trabalhador disse à Justiça do Trabalho que gastava, em média, meia hora no sistema para garantir a inclusão na escala), os avisos importantes saem como notícia no site, e o próprio órgão gestor publicou um PDF sobre limpeza de cache no Android e no iOS. A análise inteira, com as fontes, o que foi possível ver e o que não foi, está em [`docs/analise-do-sistema-atual.md`](docs/analise-do-sistema-atual.md).

Este projeto refaz o sistema com quatro ideias:

- **"Minha escala" primeiro**, numa frase: "Amanhã, quinta, das 7h às 13h: navio Atlântico Sul, Terminal Cais Leste, berço 4 — estivador, terno 2." A escala completa continua lá, com a linha de quem lê destacada.
- **Todo "não" vem com o motivo e com o prazo**: o exame que venceu, o curso de reciclagem, as férias, o descanso de onze horas — e a partir de quando ele volta a poder.
- **Feito para o celular e para os olhos de quem tem mais idade**: letra grande que aumenta com um toque, alto contraste, botões grandes, palavra junto de todo ícone, nenhuma sigla sem explicação — e a escala lida em voz alta.
- **Os avisos chegam ao trabalhador**: quando sai a escala, quando a habilitação vai fechar, quando o exame está para vencer.

## Exemplo

O exemplo inteiro está em [`ExemploDoReadmeTests`](tests/EscalaAvulsos.Dominio.Testes/ExemploDoReadmeTests.cs). É um teste: se o código mudar e alguma frase desta página deixar de ser verdade, o build quebra.

Quarta-feira, 4 de novembro de 2026, 10h. A Cais Forte vai trabalhar o navio Atlântico Sul no Terminal Cais Leste na manhã de quinta, e pede ao órgão gestor dois ternos de estiva — em cada um, um contramestre, um sinaleiro, um operador de guindaste de bordo e oito estivadores:

```csharp
var caisForte = new OperadorPortuario("Cais Forte Operações Portuárias", Cnpj.De("11.222.333/0001-81"));
var atlanticoSul = new Navio("Atlântico Sul", NumeroImo.De("IMO 9876543"));
var quintaDeManha = Periodo.De(new DateOnly(2026, 11, 5), Turno.Manha);

VagasDaFuncao[] terno = [new(contramestre, 1), new(sinaleiro, 1), new(operadorDeGuindaste, 1), new(estivador, 8)];

var requisicao = new Requisicao(
    new DadosDaRequisicao(caisForte, atlanticoSul, new LocalDaOperacao("Terminal Cais Leste", "4"), quintaDeManha),
    [terno, terno], quartaAs10h);
// Cais Forte Operações Portuárias pede 2 ternos da estiva, 22 vagas, para o navio Atlântico Sul (IMO 9876543),
// no Terminal Cais Leste, berço 4, em 05/11/2026, das 7h às 13h.
```

O que o operador pedir errado volta todo de uma vez, dito como se diz no porto — a mesma função escrita de dois jeitos é a mesma função, e cada categoria tem o seu rodízio:

```csharp
new Requisicao(dados, [[new(estivador, 8), new(new Funcao(Categoria.Estiva, "ESTIVADOR"), 2)], [new(conferente, 1)]], quartaAs10h);
// O terno 1 repete estivador: junte as vagas da função numa linha só. A requisição mistura a estiva e a
// conferência de carga: cada categoria tem o seu rodízio, e a sua requisição.
```

Seu Antônio Ferreira, matrícula 4127, é estivador registrado desde 1994. Tem também o curso de sinaleiro, que vale até maio de 2027, e o exame médico vale até 30 de novembro:

```csharp
var antonio = new Trabalhador(new DadosDoTrabalhador(
    Matricula.De("4127"), "Antônio Ferreira", Cpf.De("529.982.247-25"), Categoria.Estiva, Inscricao.Registrado));

antonio.Qualificar(estivador, new DateOnly(1994, 3, 1));
antonio.Qualificar(sinaleiro, new DateOnly(2010, 5, 10), validaAte: new DateOnly(2027, 5, 9));
antonio.RegistrarAso(new Aso(new DateOnly(2025, 12, 1), validoAte: new DateOnly(2026, 11, 30)));
```

Na quinta, ele pode pegar uma vaga de sinaleiro — mas não a de guindaste, que não é a dele:

```csharp
antonio.AptidaoPara(sinaleiro, quintaDeManha);
// Antônio Ferreira (matrícula 4127) pode trabalhar como sinaleiro em 05/11/2026, das 7h às 13h.

antonio.AptidaoPara(operadorDeGuindaste, quintaDeManha);
// Antônio Ferreira (matrícula 4127) não pode trabalhar como operador de guindaste de bordo em 05/11/2026,
// das 7h às 13h: não tem a qualificação de operador de guindaste de bordo.
```

As férias de janeiro já estão marcadas, e ninguém renovou o exame. Para a noite de 5 de janeiro — que começa às 19h e termina à 1h do dia 6 —, o sistema não diz só "não": diz por quê, e cada motivo traz os dias em que impede. É com isso que o aplicativo vai avisar o Seu Antônio, antes de ele ficar de fora da escala sem entender:

```csharp
antonio.Impedir(new Impedimento(TipoDeImpedimento.Ferias, new DateOnly(2027, 1, 4), new DateOnly(2027, 2, 2)));

antonio.AptidaoPara(estivador, Periodo.De(new DateOnly(2027, 1, 5), Turno.Noite));
// Antônio Ferreira (matrícula 4127) não pode trabalhar como estivador em 05/01/2027, das 19h à 1h:
// o exame médico (ASO) vale só até 30/11/2026; está de férias de 04/01/2027 a 02/02/2027.
```

Para a tarde de quinta, das 13h às 19h, a Cais Forte pediu um terno menor: um sinaleiro e dois estivadores. A habilitação abre à 1h e fecha às 10h, e não há corrida: quem se habilita às 9h59 tem a mesma vez de quem se habilitou à 1h, porque a vez é pela matrícula. Quem não pode trabalhar numa função fica sabendo na hora:

```csharp
var escalacao = new Escalacao(Categoria.Estiva, quintaATarde, CalendarioDaEscalacao.Padrao);

escalacao.Habilitar(antonio, [sinaleiro, estivador], quintaAs6h);

escalacao.Habilitar(dirceu, [sinaleiro], quintaAs6h);
// Dirceu Alves (matrícula 4188) não pode se habilitar como sinaleiro: não tem a qualificação de sinaleiro.
```

Seis estivadores se habilitam; o Antônio e o Carlos também como sinaleiros, e o João é cadastrado. O rodízio de sinaleiro tinha parado no Carlos, o de estivador no Benedito, e o Dirceu trabalhou de manhã. Às 10h, a escala sai: a função com menos habilitados primeiro, os registrados antes dos cadastrados, cada um na sua vez — e quem não pode é pulado:

```csharp
var escala = Escalador.Escalar(escalacao, [requisicao], habilitados, [manhaDoDirceu], rodizio, Descanso.DaLei, quintaAs10h);

escala.Engajamentos;
// Antônio Ferreira (matrícula 4127): sinaleiro no terno 1 do navio Atlântico Sul (IMO 9876543), no Terminal Cais Leste, berço 4, …
// Carlos Mota (matrícula 4152): estivador no terno 1 do navio Atlântico Sul (IMO 9876543), …
// Edson Prado (matrícula 4101): estivador no terno 1 do navio Atlântico Sul (IMO 9876543), …
```

E quem ficou de fora sabe por quê, sem procurar o nome numa lista:

```csharp
escala.SituacaoDe(Matricula.De(4130));
// Benedito Lima (matrícula 4130) ficou fora da escala: como estivador, o rodízio parou 2 posições antes da sua vez.

escala.SituacaoDe(Matricula.De(4188));
// Dirceu Alves (matrícula 4188) ficou fora da escala: tem jornada em 05/11/2026, das 7h às 13h, e o descanso de 11 horas
// só termina em 06/11/2026, à meia-noite.

escala.SituacaoDe(Matricula.De(5310));
// João Batista (matrícula 5310) ficou fora da escala: como estivador, as vagas ficaram todas com registrados.
```

A próxima escala de estivador começa depois da matrícula 4101, a do Edson — e o Benedito, que desta vez não foi alcançado, é o segundo da vez.

## Roteiro

O projeto é construído em partes, cada uma entregue funcionando, testada e documentada.

| Parte | Conteúdo | |
|--:|---|---|
| 1 | Trabalhadores e requisições: matrícula, qualificações, exame médico e impedimentos, com o motivo de cada "não"; o pedido do operador com o navio, o local, o período e os ternos | ✅ |
| 2 | A escala: a habilitação para o período, o rodízio pela ordem das matrículas, os registrados antes dos cadastrados, o descanso de onze horas e a explicação de cada escolha | ✅ |
| 3 | Depois da escala: presença e falta, troca e desistência, a frequência mínima e o extrato do trabalhador, com o noturno e as provisões de férias, 13º e FGTS | |
| 4 | Persistência com EF Core e PostgreSQL, testes com Testcontainers, e a escala que não põe ninguém em duas vagas ao mesmo tempo | |
| 5 | API REST: entrada pela matrícula, papéis de trabalhador, escalador e operador, ProblemDetails, concorrência otimista e idempotência | |
| 6 | O aplicativo do trabalhador, para o celular e para quem tem mais idade: "Minha escala" numa tela, letra grande, alto contraste, botões grandes, ouvir a escala, linguagem simples — com a acessibilidade conferida por teste | |
| 7 | Avisos e tempo real: notificação quando sai a escala e antes de fechar a habilitação, a escala ao vivo e o painel do escalador e do operador | |
| 8 | Observabilidade, Docker e entrega contínua | |

## O que já está pronto

**Parte 1 — trabalhadores e requisições**

- **Matrícula, CPF, CNPJ e número IMO conferidos.** A matrícula é um número, comparado como número — a 99 vem antes da 100 —, porque é nessa ordem que o rodízio anda. O CPF e o CNPJ, também o alfanumérico, são conferidos pelos dígitos verificadores; o número IMO do navio, pelo dígito da soma ponderada.
- **Os períodos de seis horas, no horário de Brasília.** Da 1h às 7h, das 7h às 13h, das 13h às 19h e das 19h à 1h; o período é do dia em que começa, a meia-noite e meia ainda é a noite do dia anterior, e a noite e a madrugada são trabalho noturno. Início e fim são instantes com fuso, os mesmos em qualquer servidor — até nas noites em que o horário de verão começava e terminava, que tiveram cinco e sete horas.
- **O trabalhador, como o órgão gestor o conhece.** A matrícula, a categoria — capatazia, estiva, conferência de carga, conserto de carga, vigilância de embarcações ou bloco —, o cadastro ou o registro; as funções em que é qualificado, com a validade da reciclagem; o exame médico, que só anda para a frente; as férias, os afastamentos e as suspensões; e o cancelamento da inscrição, que congela tudo.
- **A aptidão, com todos os motivos.** Pode ou não pode trabalhar numa função, num período — e, se não pode, cada motivo com o tipo, a frase e os dias em que impede: sem a qualificação, qualificação vencida ou que ainda não começou, sem exame ou com exame vencido, férias, afastamento, suspensão, inscrição cancelada.
- **A requisição do operador portuário.** O operador pelo CNPJ, o navio pelo número IMO, o terminal e o berço, o período, e os ternos com as vagas de cada função; uma categoria por requisição, os ternos numerados na ordem, as vagas somadas por função, e o cancelamento com motivo antes de o período começar.

**Parte 2 — a escala**

- **A escalação de cada período**, por categoria, com o calendário dela: as requisições chegam até 4 horas antes, e a habilitação vai de 12 a 3 horas antes do período — o padrão do sistema, configurável por porto. Os horários saem no relógio do porto: "abre em 05/11/2026, à 1h", "fechou em 05/11/2026, às 10h".
- **A habilitação sem corrida.** O trabalhador escolhe as funções em que concorre, troca ou desiste enquanto a janela está aberta, e a hora em que se habilitou não dá preferência: os habilitados ficam na ordem das matrículas. Quem não pode trabalhar numa função é recusado na hora, com os motivos — o que vale para todas, como o exame vencido, dito uma vez só.
- **O rodízio pela ordem das matrículas.** Uma roda por função, separada para registrados e cadastrados, que começa depois do último escalado e dá a volta. Quem a roda alcança e não pode trabalhar perde a vez; quem ela não alcança fica mais perto na próxima. A justiça é conferida: com os mesmos habilitados, ninguém fica mais de uma escala na frente de ninguém.
- **O descanso de onze horas**, contado no relógio, antes e depois de cada jornada marcada, com a hora em que ele termina: "o descanso de 11 horas só termina em 06/11/2026, à meia-noite". O mínimo é configurável, para a convenção.
- **A escala, e o porquê de cada um.** A função com menos habilitados primeiro; em cada uma, os registrados e depois os cadastrados, com as vagas que sobraram. As requisições canceladas não entram, e as que chegaram depois do prazo ficam de fora, listadas. Cada habilitado sai com a vaga — navio, terminal, berço, terno, função — ou com os motivos: o descanso, a aptidão, as posições que faltaram no rodízio, as vagas que ficaram com os registrados, a função que ninguém pediu. As vagas vazias saem listadas para o operador, e a mesma entrada dá sempre a mesma escala.

## Decisões

As decisões que mudam o desenho do sistema estão registradas em [`docs/adr`](docs/adr):

- [ADR-0001](docs/adr/0001-a-lingua-do-porto.md) — o código fala a língua do porto: `Trabalhador`, `Matricula`, `Terno`, `Requisicao`, com um vocabulário para quem chega.
- [ADR-0002](docs/adr/0002-os-periodos-de-seis-horas.md) — os períodos de seis horas, do dia em que começam, em instantes no horário de Brasília.
- [ADR-0003](docs/adr/0003-a-aptidao-diz-os-motivos.md) — a aptidão diz todos os motivos, e os dias em que cada um impede, e não só sim ou não.
- [ADR-0004](docs/adr/0004-a-requisicao-e-os-ternos.md) — a requisição é de uma categoria, com os ternos como o operador pede; o operador é o CNPJ, e o navio, o número IMO.
- [ADR-0005](docs/adr/0005-a-escalacao-e-a-habilitacao.md) — uma escalação por categoria e período, com o calendário dela; a habilitação sem corrida, e a aptidão conferida na hora.
- [ADR-0006](docs/adr/0006-o-rodizio-e-o-descanso.md) — o rodízio pela ordem das matrículas, uma roda por função e inscrição; quem não pode perde a vez; o descanso contado no relógio, para os dois lados.
- [ADR-0007](docs/adr/0007-a-escala.md) — a escala é uma conta: a função com menos habilitados primeiro, registrados antes de cadastrados, e a situação de cada habilitado, com o porquê.

E algumas que não precisaram de ADR:

- **O domínio não depende de nada** além da biblioteca padrão do .NET. Um teste de arquitetura falha se ele passar a referenciar qualquer outro assembly.
- **Valores só nascem pela leitura que valida.** Matrícula, CPF, CNPJ, número IMO e período não têm construtor público: uma matrícula zero não existe.
- **Erros de quem usa vêm todos de uma vez**, em frases para quem lê — o trabalhador, o escalador, o operador —, e sem gênero: "está em afastamento", "cumpre suspensão".
- **Aviso é erro.** Analisadores no nível `latest-recommended`, estilo verificado no build e `dotnet format` conferido no CI.

## Testes

- **Exemplos conferidos à mão**: os CPFs e CNPJs de exemplo, os números IMO com a soma ponderada feita no papel, as matrículas com zeros à esquerda e as que só parecem número.
- **Propriedades em 400 casos sorteados**, com semente fixa: CPFs, CNPJs e números IMO com os verificadores calculados pelo próprio teste, por uma conta escrita à parte, são aceitos, voltam iguais da máscara, e com um verificador trocado são recusados.
- **Seis meses de períodos, de outubro de 2018 a março de 2019**, um atrás do outro: cada um começa onde o anterior termina, e cada instante cai no período certo, também nas noites em que o horário de verão mudava.
- **A aptidão no limite de cada data**: o último dia do exame e da qualificação, o primeiro e o último das férias, a véspera da volta do afastamento, a noite que é de um dia e termina no outro.
- **A janela da habilitação no limite**: da abertura, incluída, ao último instante antes do fechamento; os horários "à 1h", "ao meio-dia", "à meia-noite", "às 12h30".
- **A roda girando setenta vezes**, com os mesmos sete habilitados e três vagas: depois de cada escala, quem trabalhou mais tem no máximo uma a mais que quem trabalhou menos.
- **O descanso em tabela**: para cada turno e cada distância entre a jornada e o período, com 0, 6, 11, 12 e 13 horas de mínimo, quais períodos ficam impedidos.
- **Trezentas escalas sorteadas**, com semente fixa — habilitados, funções, inscrições, suspensões, jornadas, requisições no prazo, fora dele e canceladas, e o rodízio parado em qualquer lugar. Em todas, conferido de fora: ninguém em duas vagas; cada um numa função em que se habilitou e que podia exercer; os escalados de cada roda são os primeiros da vez, pulando só quem não podia; nenhum cadastrado pega vaga que um registrado podia pegar; nenhuma vaga fica vazia com alguém que podia pegá-la; a vez para no último escalado; e toda vaga pedida está preenchida ou listada.
- **O exemplo deste README**, de ponta a ponta.
- **Arquitetura.** O domínio só referencia a biblioteca padrão; toda classe pública é selada, estática ou abstrata; nenhuma propriedade tem `set` público; os valores não têm construtor público; os internos não são visíveis a ninguém; e todo agregado tem identidade própria.

## Como rodar

Pré-requisito: o [SDK do .NET 10](https://dotnet.microsoft.com/download).

```bash
dotnet test --solution EscalaAvulsos.slnx
```

## Estrutura

```text
src/EscalaAvulsos.Dominio/               regras do negócio, sem dependências
  Comum/                                 exceção de domínio, identidade, relógio do porto e textos
  Identificacao/                         matrícula, CPF, CNPJ e número IMO
  Periodos/                              os quatro turnos e os períodos de seis horas
  Trabalhadores/                         trabalhador, funções, qualificações, exame, impedimentos e aptidão
  Requisicoes/                           requisição, ternos, operador portuário, navio e local
  Escalacoes/                            escalação e habilitação, rodízio, descanso, o escalador e a escala
tests/EscalaAvulsos.Dominio.Testes/      exemplos, propriedades e o exemplo deste README
tests/EscalaAvulsos.Arquitetura.Testes/  regras que o domínio não pode quebrar
docs/adr/                                decisões de arquitetura
docs/analise-do-sistema-atual.md         o sistema de hoje, o que os usuários relatam e o que muda aqui
```

## Licença

[MIT](LICENSE): use, copie, modifique e distribua à vontade, mantendo o aviso de copyright.
