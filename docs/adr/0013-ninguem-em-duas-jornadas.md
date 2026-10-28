# ADR-0013 — Ninguém em duas jornadas que se cruzam, e o rodízio sem corrida: o banco confere

**Status:** aceita

## Contexto

O domínio não põe ninguém em duas vagas, nem sem as onze horas de descanso (ADR-0006, ADR-0007), desde que receba as jornadas de agora. Gravada a escala no banco (ADR-0012), entre ler as jornadas e gravar passa um tempo, e nesse tempo outra escala pode ser gravada.

- **A escala da noite sai com jornadas lidas antes de a da tarde ser gravada.** O Benedito, que pegou a tarde, pega também a noite, e trabalha das 13h à 1h.
- **Duas desistências chegam no mesmo instante**, uma na tarde e uma na noite. O Fábio espera nas duas, e cada desistência, olhando as jornadas, o vê livre: ele entra nas duas.
- **Duas escalas da mesma categoria saem ao mesmo tempo**, de períodos diferentes, a partir do mesmo ponto do rodízio. Os mesmos trabalhadores ganham a vez duas vezes, e os seguintes esperam o dobro.

São corridas raras, mas o porto tem muitas escalas por dia, e cada uma, quando acontece, é um trabalhador sem descanso ou uma vez roubada de alguém. O domínio não as vê: ele decide com o que recebe.

## Decisão

**1. As vagas de cada escala também vão para uma tabela própria**, `engajamentos`. É uma linha por vaga ocupada agora, com o trabalhador, a requisição, o início, o fim e o fim do descanso, calculado com o descanso com que a escala foi feita. As linhas são cópias das vagas do documento, gravadas na mesma transação que ele: a escala nova insere as suas, e a escala alterada troca as dela.

**2. Uma restrição de exclusão confere as jornadas**:

```sql
exclude using gist (trabalhador_id with =, tstzrange(inicio, descansado_em) with &&)
```

O mesmo trabalhador não tem dois intervalos que se cruzam, cada um do início da jornada ao fim do descanso depois dela. Isso cobre as duas regras de uma vez. Duas jornadas ao mesmo tempo se cruzam. E se uma termina antes de a outra começar, os intervalos só se cruzam quando a segunda começa antes de terminar o descanso da primeira. O PostgreSQL confere na hora de gravar, também entre duas gravações ao mesmo tempo: a segunda espera a primeira e é recusada.

**3. A gravação recusada não deixa nada**, nem o documento, nem as vagas, nem o rodízio. A persistência lança `JornadaSemDescanso`, dizendo quem e onde: "Benedito Lima (matrícula 4130) já tem jornada em 05/11/2026, das 13h às 19h, e as duas não cabem com o descanso de 11 horas entre elas. A escala não foi gravada: leia as jornadas de novo e refaça."

**4. O caminho certo continua no domínio.** As jornadas perto do período, as que terminam menos de um descanso antes dele ou começam menos de um descanso depois, são lidas da mesma tabela. O `Escalador` as usa para dizer "em descanso até…", e a desistência para pular o substituto que já não cabe (ADR-0011). O banco é a segunda barreira, para a decisão tomada com dados velhos, e não o lugar da regra.

**5. O rodízio de cada categoria tem uma versão.** Uma linha por categoria, em `rodizios`, aponta para a escala mais recente, e o rodízio em si é o que está no documento dela. A escala nova grava com a versão do rodízio que leu: `update … where versao = @lida`, ou, na primeira escala da categoria, `insert … on conflict do nothing`. Se outra escala da categoria passou no meio, nada muda, e a gravação é recusada com `ConflitoDeConcorrencia`: "O rodízio da estiva mudou desde a versão 0: está na versão 1. Leia o rodízio de novo e refaça a escala." Refeita, a escala parte de onde a outra parou.

**6. A desistência que anda a vez sobe a versão do rodízio**, se a escala ainda é a mais recente da categoria. Quem leu o rodízio antes refaz, e a vez do substituto conta. A presença, a falta e a desistência sem substituto não mexem na roda nem na versão: a comparação é entre o rodízio do documento de antes e o de depois.

## Consequências

- **São duas barreiras, e nenhuma basta sozinha.** O domínio decide com as jornadas de agora e explica cada "não". O banco recusa o que foi decidido com as de antes.
- **O descanso de cada vaga é o da escala dela.** Se a convenção mudar o mínimo, as jornadas já marcadas continuam com o de quando foram marcadas.
- **A desistência numa escala que já não é a mais recente não anda o rodízio da categoria**: a próxima escala já partiu dele. Com o calendário padrão isso não acontece, porque a escala seguinte só sai depois de o período começar, e aí não se desiste mais. Com um calendário que feche a habilitação muito cedo, o substituto ganha uma vez que a roda não conta.
- **Cada escala gravada custa o documento, uma linha por vaga e a linha do rodízio**, numa transação só. As vagas de um período são dezenas, e o índice GiST da restrição é pequeno.
