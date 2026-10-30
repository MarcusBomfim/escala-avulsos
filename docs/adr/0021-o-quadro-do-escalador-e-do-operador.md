# ADR-0021 — O quadro do escalador e do operador: o quadro de parede, com quem está em cada vaga

**Status:** aceita

## Contexto

Na Escala Digital de hoje, o que o órgão gestor e o cais olham é um "quadro de parede". É uma tabela por categoria, com cada posição de trabalho, a turma, as vagas pedidas, as escaladas, as em aberto, e a situação escrita com uma bolinha colorida, como "Travada" ([análise](../analise-do-sistema-atual.md#a-área-logada-por-dentro)).

Até a Parte 6, este projeto mostrava só a escala de quem lê. Faltava o que esse quadro mostra:

- **para o escalador**: em que pé está cada escalação e onde faltou gente;
- **para o operador portuário**: as vagas das requisições dele, as preenchidas e as vazias (ADR-0016).

## Decisão

**1. Um quadro por período**, em `GET /api/quadro/2026-11-05/Tarde`, montado na hora a partir da escalação, das requisições e da escala.

- **Uma linha por terno pedido**: o operador, o navio, o local, as vagas pedidas, as escaladas e as em aberto.
- **Dentro da linha, as vagas de cada função**, com quem está em cada uma, pela matrícula e pelo nome.

**2. A situação da categoria tem os nomes do quadro de hoje**, mais os de antes e depois:

| Situação | Quando |
|---|---|
| Sem escalação | Há requisição, mas a escalação do período não foi aberta |
| A habilitação vai abrir | Antes da abertura |
| Habilitação aberta | Da abertura ao fechamento |
| Travada | A habilitação fechou, e a escala ainda não saiu |
| Escalada | A escala saiu |
| Encerrada | O período acabou, e a escala foi encerrada |

Cada terno também tem a dele: aguardando a escala, completo, incompleto, cancelado ou fora do prazo.

**3. Cada um vê o que é seu.**

- **O escalador** vê todas as categorias com requisição ou escalação no período.
- **O operador** vê só as requisições do CNPJ dele, e só as categorias em que pediu.
- **O trabalhador** não vê o quadro: os nomes da escala dos outros são do órgão gestor e de quem pediu.

**4. O quadro se mexe sozinho**: cada escalação, escala e requisição que muda chega como `mudou` aos eventos do servidor (ADR-0020), e a tela lê o quadro de novo.

**5. A tela é a do quadro de parede**, no aplicativo, com a cara do sistema de hoje (ADR-0017):

- a tabela por categoria, com a situação escrita e a bolinha da cor;
- no celular, cada linha vira um bloco com os mesmos números, sem rolagem para o lado.

## Consequências

- **O quadro é lido, e não guardado**: sai sempre igual ao que está gravado, ao custo de algumas leituras por período. Se o porto crescer a ponto de pesar, o caminho é um quadro guardado e atualizado pelos anúncios do banco.
- **O operador vê os nomes de quem vai trabalhar nos ternos dele**: é o que ele precisa para receber a equipe, e já é dele pela requisição.
- **"Travada" continua significando o que significa hoje**: quem conhece o quadro de parede não precisa aprender outra palavra.
