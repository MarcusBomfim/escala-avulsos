# ADR-0019 — Os avisos: escritos para o trabalhador, guardados com a escala, e um por fato

**Status:** aceita

## Contexto

No sistema de hoje, o aviso é uma notícia no site, ou um recado na primeira tela da área logada. Os dois esperam que o trabalhador vá até eles, e quem não abre o site não fica sabendo. O que mais faz falta avisar está longe de ser notícia: a escala que saiu, a vaga que passou para ele porque alguém desistiu, a habilitação que vai fechar sem ele e o exame que vai vencer. Os dois últimos custam caro, porque tiram o trabalhador da escala e contam contra a frequência mínima ([análise](../analise-do-sistema-atual.md)).

Esses avisos nascem de dois jeitos. A escala que sai e a vaga que passa para o substituto são gravações. A habilitação que vai fechar e o exame que vai vencer não são gravação nenhuma: são o tempo passando.

## Decisão

**1. O aviso é do domínio, e escrito para o trabalhador**, em segunda pessoa e com as datas por extenso: "Quinta-feira, 5 de novembro, das 13h às 19h: sinaleiro no terno 1 do navio Atlântico Sul, no Terminal Cais Leste, berço 4."

- **Por que o domínio escreve, e não o aplicativo** (ADR-0017): o aviso chega ao celular com o aplicativo fechado, e a notificação mostra o texto como veio.
- **Por que a data por extenso, e não "hoje"**: o recado pode ser lido dias depois, e o "hoje" dele já seria outro dia.

São seis tipos:

| Tipo | Quando nasce |
|---|---|
| Você vai trabalhar | A escala saiu, com vaga para ele |
| Você ficou de fora da escala | A escala saiu sem vaga para ele: os motivos e o melhor lugar dele na espera |
| Você entrou na escala | Quem estava na vaga desistiu, e ela passou para ele |
| A habilitação vai fechar | Falta menos de uma hora para fechar, e ele não se habilitou, embora possa trabalhar em alguma função |
| O exame médico vai vencer | Faltam trinta dias ou menos para vencer o ASO |
| A reciclagem vai vencer | Faltam trinta dias ou menos para vencer a reciclagem de uma função |

**2. Um aviso por fato.** Cada aviso tem uma chave que diz de que fato é:

- `escala:{id}` para a escala que saiu;
- `espera:{id}` para a vaga que passou para ele;
- `habilitacao:{id}` para a escalação;
- `aso:2026-11-30`, a validade do exame;
- `reciclagem:sinaleiro:2026-12-04`, a validade da reciclagem.

O trabalhador e a chave são a chave da linha no banco, e o aviso repetido entra com `on conflict do nothing`. Conferir de novo não avisa de novo. O exame renovado volta a ser avisado quando a validade nova estiver perto.

**3. O aviso da escala é gravado na transação da escala.** O repositório da escala guarda os avisos de cada habilitado quando ela sai. Quando ela muda, guarda o aviso de quem ganhou uma vaga que não tinha. As duas coisas acontecem no mesmo `commit` das vagas e do rodízio. Assim, não existe escala sem aviso nem aviso de escala desfeita. Quem grava a escala não precisa lembrar de avisar, e a API não muda.

**4. O aviso do prazo é do servidor, de tempos em tempos.** As regras são do domínio e não sabem que horas são: `AvisosDosPrazos` recebe a escalação, os trabalhadores e o instante, e diz quem avisar. Quem roda é o servidor da API, que confere de minuto em minuto as escalações que começam nas próximas horas e, de hora em hora, os vencimentos (Parte 7, segundo commit).

**5. Os recados ficam no banco, lidos ou não**, para a tela de recados do aplicativo: os mais novos primeiro, com quantos faltam ler. Abrir os recados marca os que estavam por ler.

**6. A entrega aos celulares também sai do banco**, como uma caixa de saída.

- **A reserva**: o servidor reserva os avisos que vai entregar (`for update skip locked`) e conta a tentativa. Outro servidor só pega o mesmo aviso depois que a reserva vence, em dois minutos, então dois servidores não mandam o mesmo aviso duas vezes. O servidor que cai no meio não perde o aviso: ele vai na próxima volta, de quem estiver de pé.
- **As tentativas**: depois de cinco, o aviso fica só no aplicativo.
- **O celular**: como o aviso chega lá é a ADR-0020.

## Consequências

- **O texto do aviso não se traduz nem muda de tom pelo aplicativo.** Mudar a frase é mudar o domínio, com teste.
- **O aviso de prazo pode chegar até um minuto atrasado**, o intervalo da conferência. Para uma janela de uma hora, não faz diferença.
- **A tabela de avisos cresce com a escala**: um aviso por habilitado a cada escala. Os recados antigos podem ser apagados por idade sem perder nada da escala, que continua no documento dela.
- **Quem precisa de outro aviso** escreve a regra no domínio e escolhe a chave. O resto já vale para ele: a gravação, a leitura, a entrega e a repetição evitada.
