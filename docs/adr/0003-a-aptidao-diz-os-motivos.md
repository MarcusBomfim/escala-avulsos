# ADR-0003 — A aptidão diz todos os motivos, e não só sim ou não

**Status:** aceita

## Contexto

Antes de entrar no rodízio de uma vaga, o trabalhador tem de poder trabalhar nela: ter a qualificação da função, o exame médico (ASO) em dia, e não estar de férias, afastado, suspenso ou com a inscrição cancelada. Cada uma dessas coisas tem a sua regra — a NR-29 e a NR-7 para o exame, os cursos para as qualificações, a Lei 12.815/2013 para o cadastro e o registro.

Para escalar, um sim ou um não bastaria. Mas quem ficou de fora quer saber por quê — e, sobretudo, até quando. Hoje, o trabalhador descobre que o exame venceu quando percebe que não está sendo chamado, e vai ao órgão gestor perguntar ([análise do sistema atual](../analise-do-sistema-atual.md)). É a pergunta que o aplicativo mais precisa responder, e ela nasce aqui.

## Decisão

- **`Trabalhador.AptidaoPara(funcao, periodo)` devolve uma `Aptidao`**, com todos os motivos contra, e não um `bool`. Apto é não ter motivo nenhum.
- **Cada motivo tem tipo, frase e os dias em que impede.** O exame que valeu até 30/11 impede de 01/12 em diante; as férias, do primeiro ao último dia; a qualificação que ainda não começou, até a véspera. O dia seguinte ao último é quando o trabalhador volta a poder: é isso que o aplicativo vai dizer a ele.
- **Os motivos vêm numa ordem fixa**: a função e a qualificação, o exame, e os impedimentos pela data de início. A inscrição cancelada, quando há, é o único motivo: nenhum exame novo traz de volta quem saiu do órgão gestor.
- **A frase não tem sujeito** — "o exame médico (ASO) vale só até 30/11/2026" —, para ser juntada depois do nome de quem não pode, e não fala de gênero: "está em afastamento", "cumpre suspensão". O aplicativo fala com o trabalhador em segunda pessoa, montando a frase dele com o tipo e as datas.
- **As regras de cada parte ficam com ela.** A qualificação é só em função da categoria do trabalhador, e renovar substitui a anterior. O exame só anda para a frente. Impedimentos do mesmo tipo não cruzam; férias e suspensão têm último dia, e o afastamento pode começar sem ele, até a volta.

## Consequências

- A escala da Parte 2 é a aptidão somada ao rodízio e ao descanso entre jornadas. A tela "por que não fui escalado" sai dos mesmos motivos, sem regra repetida no front-end.
- As frases fazem parte do domínio, e os testes conferem cada uma: mudar uma vírgula de mensagem quebra teste. É de propósito — é o que o trabalhador lê.
- Estar no cadastro, e não no registro, não é motivo: o cadastrado pode trabalhar. A preferência dos registrados é do rodízio, e não da aptidão.
