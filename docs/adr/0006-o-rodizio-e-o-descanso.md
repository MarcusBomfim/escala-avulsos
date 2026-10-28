# ADR-0006 — O rodízio pela ordem das matrículas, e o descanso de onze horas

**Status:** aceita

## Contexto

A escalação do avulso é feita em rodízio pelo órgão gestor (Lei 9.719/1998, art. 5º), para que o trabalho se reparta entre todos. Em Santos, a Escala Digital manteve a ordem da escala presencial: sequencial e numérica, pela matrícula. As convenções dão a preferência aos registrados; os cadastrados completam as vagas que sobram. E entre duas jornadas o trabalhador tem de ter onze horas seguidas de descanso (art. 8º), salvo as exceções da convenção.

O rodízio é a regra que o trabalhador mais precisa entender — é ela que diz se a vez dele está perto —, e a que menos pode ter surpresa.

## Decisão

- **Uma roda por função, e separada para registrados e cadastrados.** Cada roda guarda só a matrícula do último escalado. A escala seguinte começa na matrícula logo depois dela, sobe até a maior e volta à menor. O último escalado pode nem estar entre os habilitados: a roda continua do número, e não da pessoa.
- **Quem não pode pegar a vez perde a vez.** O trabalhador que a roda alcançou e que não pode trabalhar — sem a qualificação, em descanso — é pulado, e a roda passa por ele. Quem a roda não alcançou porque as vagas acabaram não perde nada: a próxima escala começa onde esta parou, e ele está mais perto.
- **O rodízio não muda; anda.** Andar a vez devolve outro rodízio, e o de antes fica como estava: a escala é refeita com as mesmas entradas e dá o mesmo resultado, o que deixa qualquer escala ser conferida depois. O rodízio é gravado como a lista das vezes de cada roda, e remontado dela.
- **O descanso é contado no relógio**, do fim de uma jornada ao começo da outra, para os dois lados — a jornada marcada depois do período também pede descanso antes dela. O mínimo é configurável, para a convenção; o padrão é o da lei. Com períodos de seis horas, onze horas querem dizer que, depois de um período, o trabalhador não pega nem o seguinte nem o outro: só o terceiro, doze horas depois.
- **A dobra — dois períodos seguidos — não entra.** É uma exceção da convenção, discutida na Justiça; se entrar, será uma regra à parte, que só age depois de a roda inteira ser percorrida.

## Consequências

- O aplicativo pode dizer "o rodízio parou 3 posições antes da sua vez", e o trabalhador entende o que falta.
- A justiça do rodízio é verificável: com os mesmos habilitados, quem trabalhou mais tem, a qualquer momento, no máximo uma escala a mais que quem trabalhou menos. Há um teste que gira a roda setenta vezes e confere isso a cada volta.
- O descanso explica até quando o trabalhador está impedido: "o descanso de 11 horas só termina em 06/11/2026, à meia-noite".
