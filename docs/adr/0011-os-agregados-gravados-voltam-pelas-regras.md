# ADR-0011 — Cada agregado é um documento JSON, e o que volta do banco passa pelas regras

**Status:** aceita

## Contexto

Até a Parte 3, o trabalhador, a requisição, a escalação e a escala viviam só em memória. Para gravá-los no PostgreSQL (Parte 4), falta o caminho de volta: ler um trabalhador gravado é criar um objeto com uma identidade e um estado que já existiam. Os construtores públicos não servem, porque dão identidade nova. E os métodos de negócio nem sempre podem ser repetidos: a habilitação foi aceita com o exame em dia, e o exame venceu desde então; a escala foi feita com o rodízio daquela hora, que já andou.

Cada agregado é uma árvore pequena, e nunca é usado pela metade. Ninguém lê uma habilitação sem a escalação, ou uma vaga sem a escala. Mapear cada nó para uma tabela daria uma dúzia delas, e o domínio teria de ceder ao ORM.

E fica a pergunta do que fazer com um dado gravado que fere uma regra: editado à mão no banco, ou gravado por uma versão com defeito.

## Decisão

**1. Cada agregado é um documento JSON**, de um tipo da infraestrutura, e não o objeto do domínio serializado. Renomear uma propriedade do domínio não muda o formato sem ninguém ver: muda o mapeamento, e um teste compara o JSON de cada agregado com o formato escrito por extenso. Cada documento tem um número de formato.

**2. A leitura é estrita.** Ela falha, e diz onde, quando falta uma propriedade, quando aparece uma que o formato não tem, ou quando há um nulo onde ele não cabe, inclusive dentro de uma lista. Enums vão pelo nome, e só pelo nome: o número deixaria entrar um valor que o enum não tem.

**3. A reconstituição é interna**, visível só para a infraestrutura. A API pública do domínio continua sendo a das regras de negócio: quem não é a persistência não tem como criar um trabalhador com o id de outro. Um teste de arquitetura confere que a lista de quem enxerga os internos tem só um nome. Os testes do domínio continuam pela porta da frente.

**4. Nada volta sem passar pelas regras.** Valem as regras de sempre, e não as que valiam no momento da escrita:

| Agregado | Como volta |
|---|---|
| Trabalhador | pelos passos de quem o cadastrou: a inscrição, cada qualificação, o exame, cada impedimento e, por último, o cancelamento; duas qualificações na mesma função são erro, e não renovação |
| Requisição | pedida de novo, com os ternos conferidos, e cancelada de novo, se foi |
| Escalação | com o calendário de quando abriu; cada habilitação dentro da janela, com função e sem repetir, uma por trabalhador |
| Escala | juntada e conferida: cada vaga com quem se habilitou na função, presença e falta só de quem está nela, a desistência antes do período, a fila de espera com quem se habilitou, cada motivo com o que o tipo pede |

O que já valia não é conferido de novo contra o mundo de hoje. A habilitação não passa outra vez pela aptidão, e a escala não é feita de novo: a aptidão e o rodízio daquela hora não voltam. Os motivos de quem ficou de fora são refeitos pelo tipo e pelos dados, e a frase sai de novo. Os motivos da aptidão voltam com a frase gravada, porque o que os explicava, o exame ou as férias, pode ter mudado.

**5. Dado gravado que fere uma regra é recusado na leitura**, com a mensagem que diz o quê. Ele não fica para ser descoberto depois, numa escala errada. A escala e a escalação dizem todos os problemas de uma vez.

**6. A escala guarda o descanso com que foi feita**, e ele vale também para quem entra no lugar de quem desistiu. A desistência recebe as jornadas que os da espera pegaram desde a escala. Quem ficaria sem o descanso é pulado: continua na fila, mas não entra naquela vaga. É o que faltava à ADR-0008 quando a escala passa a ser lida do banco horas depois de feita, ao lado de outras escalas.

## Consequências

- **O domínio continua sem saber que existe banco.** Não há atributo de mapeamento, construtor sem parâmetros nem setter público. A persistência conversa com ele pela reconstituição e pelas propriedades de leitura.
- **`InternalsVisibleTo` é um acoplamento consciente.** A infraestrutura enxerga todos os internos, e não só a reconstituição. A lista fechada, conferida por teste, limita a quem.
- **A identidade nasce no domínio**, e não no banco: um agregado tem id antes de ser gravado.
- **A evolução do formato é responsabilidade do código**, e não do banco. Quem segura são o número do formato, o teste de formato e a leitura estrita.
- **Cada documento é percorrido uma vez a mais**, para conferir as listas, antes de virar objeto. Os documentos são pequenos, e a conferência custa menos do que um nulo descoberto no meio da escala.
