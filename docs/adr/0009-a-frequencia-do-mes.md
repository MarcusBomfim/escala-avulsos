# ADR-0009 — A frequência do mês, contada a favor de quem trabalha

**Status:** aceita

## Contexto

O órgão gestor exige uma frequência mínima: quem não comparece às escalações, ou falta ao trabalho, está sujeito a medidas disciplinares. Em Santos, a norma de frequência mínima foi revista em junho de 2026 ([análise](../analise-do-sistema-atual.md)). O trabalhador, hoje, descobre que ficou abaixo do mínimo quando a medida chega.

As regras exatas — quantas habilitações, quantas faltas, em que janela — são da norma de cada porto, e mudam.

## Decisão

- **A regra é configuração**: o mínimo de habilitações num mês inteiro e o máximo de faltas sem justificativa. O padrão deste sistema — 12 e 2 — é só o ponto de partida.
- **A frequência é contada nos fatos da escala**, e não num cadastro à parte:
  - as escalações do mês em que o trabalhador se habilitou;
  - as escalas em que ele trabalhou, com a presença confirmada;
  - as escalas em que faltou, com ou sem justificativa;
  - as desistências.

  O que é de outro mês, ou de outro trabalhador, não entra.
- **O mínimo diminui com os dias em que ele não podia trabalhar** — férias, afastamento, suspensão, inscrição cancelada —, na proporção dos dias do mês, arredondado para baixo, a favor dele. Quem passou dez dias de férias em novembro deve 8 habilitações, e não 12.
- **A situação do mês tem três estados**:
  - **cumprida:** o mínimo foi alcançado, e as faltas estão dentro do limite;
  - **em andamento:** o mês ainda está aberto, e ainda faltam habilitações;
  - **descumprida:** o mês acabou abaixo do mínimo, ou as faltas passaram do limite, que não espera o fim do mês.
- **A frequência diz quanto falta, e até quando**: "7 habilitações, e o mínimo é 12 — faltam 5 até 30/11/2026".
- **A medida disciplinar não é decidida aqui.** O sistema aponta quem descumpriu; o processo disciplinar, com direito de defesa, é do órgão gestor.

## Consequências

- O aplicativo pode avisar no meio do mês, enquanto ainda dá tempo: "faltam 5 habilitações até o dia 30". É o contrário de hoje, em que o aviso é a punição.
- A frequência sai das mesmas escalações e escalas que o trabalhador vê, e não pode discordar delas.
- Se a norma de um porto contar de outro jeito — por semana, por escalação oferecida —, muda a conta desta classe, e não o resto do sistema.
