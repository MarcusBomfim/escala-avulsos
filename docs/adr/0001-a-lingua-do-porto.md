# ADR-0001 — A língua do porto, em português

**Status:** aceita

## Contexto

O sistema é a escala do trabalhador portuário avulso. Quem vive nela fala em *matrícula*, *estiva*, *capatazia*, *terno*, *requisição*, *período*, *escalação*, *rodízio*, *ASO*. Boa parte desses termos não tem tradução que preste — *terno* não é *gang*, e *avulso* não é *casual worker* —, e as regras que os acompanham estão em leis brasileiras (a Lei dos Portos, 12.815/2013, e a 9.719/1998), nas convenções coletivas e nas normas do órgão gestor de mão de obra de cada porto.

A convenção do ecossistema .NET é nomear tudo em inglês.

## Decisão

O código do domínio usa os termos do porto, em português: `Trabalhador`, `Matricula`, `Categoria.Estiva`, `Funcao`, `Requisicao`, `Terno`, `Periodo`. O que é mecanismo do framework continua com o nome do framework — `IReadOnlyList`, `DateTimeOffset`, `ArgumentNullException`.

O vocabulário, para quem chega:

| Termo | O que é |
|---|---|
| Trabalhador portuário avulso | Quem trabalha no porto sem vínculo com uma empresa, escalado a cada período pelo órgão gestor de mão de obra (o OGMO). |
| Matrícula | O número do trabalhador no órgão gestor; é na ordem delas que o rodízio anda. |
| Cadastro e registro | As duas inscrições do avulso no órgão gestor. O registrado tem a preferência na escala; o cadastrado completa as vagas que sobram. |
| Categoria | A atividade: capatazia, estiva, conferência de carga, conserto de carga, vigilância de embarcações e bloco. Cada uma tem o seu rodízio. |
| Função | O posto dentro da categoria: estivador, sinaleiro, contramestre de terno, operador de guindaste de bordo. |
| Qualificação | Uma função que o trabalhador pode exercer, porque fez o curso; algumas vencem e pedem reciclagem. |
| ASO | O atestado de saúde ocupacional: o exame médico que diz que o trabalhador pode trabalhar no porto, até uma data. |
| Operador portuário | A empresa que movimenta a carga e pede a mão de obra ao órgão gestor. |
| Requisição | O pedido do operador para um período: o navio, o local e os ternos. |
| Terno | A equipe que trabalha junta — num porão, num guindaste —, com tantas vagas de cada função. |
| Período | As seis horas de trabalho; são quatro por dia (ADR-0002). |
| Escalação e rodízio | A escolha de quem trabalha em cada vaga, na ordem do rodízio, para que o trabalho se reparta entre todos (a partir da Parte 2). |

A regra de análise **CA1710**, que exige o sufixo `Exception` em exceções, foi desligada no `.editorconfig`: ela pressupõe nomes em inglês, e `ExcecaoDeDominio` já diz o que é.

## Consequências

- Uma regra lida em voz alta para um estivador, um escalador ou um operador é reconhecível no código.
- Quem não lê português terá mais dificuldade com o domínio. Para um sistema cujos usuários são trabalhadores do porto de Santos, e cujas regras são brasileiras, é a troca certa.
- Mistura de idiomas na mesma linha (`Ternos.Sum(...)`) é inevitável e aceita: se o termo vem do porto, como o porto fala; se vem da biblioteca, como ela nomeou.
- Acentos ficam fora dos identificadores (`Funcao`, não `Função`): o C# os aceita, mas eles quebram busca, ferramenta de linha de comando e teclado de quem não está no padrão ABNT.
