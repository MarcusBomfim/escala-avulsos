# ADR-0008 — Depois da escala: a desistência com substituto, a presença e a falta

**Status:** aceita

## Contexto

A escala sai algumas horas antes do período, e a vida continua até lá. Alguém desiste — ficou doente, tem um compromisso —, e a vaga não pode ficar vazia se havia quem quisesse trabalhar. No período, o órgão gestor e o operador conferem quem está no local de trabalho (Lei 9.719/1998, art. 6º); quem não aparece fica com falta, que conta na frequência e pode ser justificada com um atestado.

Para o trabalhador, duas perguntas importam: "se alguém desistir, eu entro?" e "minha presença foi registrada?".

## Decisão

- **A escala guarda a fila de espera de cada função**: os habilitados que podiam pegar a vaga e que o rodízio não alcançou — os registrados primeiro, cada um na sua vez, e depois os cadastrados. É a mesma ordem em que a roda continuaria girando.
- **Quem desiste dá a vaga ao primeiro da espera**, e a vez da roda anda até ele, como se o rodízio tivesse alcançado o substituto na escala. A vaga é a mesma — navio, terno, função —, e a posição dela na escala também. Sem ninguém na espera, a vaga entra nas vazias, somada às que já havia, para o operador saber.
- **A desistência é antes de o período começar.** Depois, quem não vai trabalhar fica com falta. Quem desiste não volta para a fila, e quem já confirmou presença não desiste mais.
- **A presença é confirmada de uma hora antes do período até o fim dele**: quem chega cedo ao cais já pode ser registrado.
- **A falta é registrada depois de o período começar**, e pode ser justificada a qualquer momento, com o motivo — o atestado costuma chegar depois. Presença e falta não se sobrepõem: a primeira registrada vale.
- **A escala é encerrada depois do fim do período.** Quem estava nela e não teve a presença confirmada fica com falta. Dali em diante, só a justificativa de falta muda alguma coisa.
- **A situação de cada habilitado acompanha tudo isso**: "está na escala, no lugar de quem desistiu", "desistiu da vaga de estivador em 05/11/2026, às 11h", "Presença confirmada em 05/11/2026, ao meio-dia", "Faltou, com justificativa: atestado médico".

## Consequências

- O aplicativo pode dizer ao trabalhador que ficou de fora: "você é o primeiro da espera de estivador; se alguém desistir, a vaga é sua" — e avisá-lo quando entrar (Parte 7).
- A substituição usa o que se sabia na hora da escala: quem estava na espera podia trabalhar quando a escala saiu. Uma suspensão que chegue depois disso e antes da desistência não é conferida de novo; o caso é raro, e a conferência no local de trabalho o pega.
- A frequência do mês (ADR-0009) e o extrato (ADR-0010) partem das escalas encerradas: quem trabalhou, quem faltou, quem desistiu.
