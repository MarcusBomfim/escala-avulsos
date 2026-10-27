# ADR-0002 — Os períodos de seis horas, no horário de Brasília

**Status:** aceita

## Contexto

No porto de Santos, o avulso trabalha em períodos de seis horas: da 1h às 7h, das 7h às 13h, das 13h às 19h e das 19h à 1h do dia seguinte, como dizem as convenções coletivas. Quase tudo na escala depende do início e do fim exatos de cada período:

- o descanso de onze horas seguidas entre duas jornadas, que a Lei 9.719/1998 exige na escalação (art. 8º), salvo as exceções da convenção;
- o trabalho noturno, que no porto vai das 19h às 7h (OJ 60 da SDI-1 do TST);
- a requisição, que tem de chegar antes de o período começar;
- e a validade do exame médico, das qualificações, das férias e das suspensões, contadas em dias.

O período mais difícil é o da noite: começa num dia e termina no outro. E o sistema vai rodar num servidor que pode estar em UTC, ou em qualquer fuso.

## Decisão

- **O período é um dia e um turno**: madrugada (1h–7h), manhã (7h–13h), tarde (13h–19h) e noite (19h–1h). `Periodo.De(dia, turno)` e mais nada: não existe período das 8h às 14h.
- **O período é do dia em que começa.** A noite de 05/11 vai das 19h de 05/11 à 1h de 06/11; a madrugada de 06/11 começa à 1h de 06/11. Meia-noite e meia de 06/11 ainda é a noite de 05/11 — é o que `Periodo.Contendo` responde.
- **Início e fim são instantes no horário de Brasília**, com deslocamento, calculados pelo fuso `America/Sao_Paulo` e não pelo da máquina. O Brasil não tem horário de verão desde 2019, mas a conversão passa pelo fuso: um período de dezembro de 2018 sai com `-02:00`, e as noites em que o horário mudava tiveram cinco e sete horas de relógio, sem buraco nem sobreposição.
- **Noturno é a noite e a madrugada**, inteiras: das 19h às 7h.
- **O que se conta em dias usa o dia do período.** Um exame médico válido até 05/11 vale para a noite de 05/11, que termina à 1h de 06/11.

## Consequências

- Um período escrito para gente — "05/11/2026, das 19h à 1h" — e um instante para máquina — `2026-11-05T19:00-03:00` — são a mesma coisa, sem conta de fuso espalhada pelo código.
- As horas dos turnos estão no código. Outro porto com outra grade de horários pediria a grade como configuração; até lá, é a de Santos.
- O "dia do período" é uma convenção do sistema: se a norma de algum prazo disser o contrário, a regra daquele prazo é que muda, e não o período.
