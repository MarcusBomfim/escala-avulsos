# Análise do sistema atual

Feita em 27/10/2026, com o que é público e com o relato de quem usa o sistema todo dia — o pai de quem propôs este projeto é estivador no porto de Santos.

Este projeto não tem ligação com o OGMO/Santos. O nome do órgão aparece aqui só para descrever o sistema que os trabalhadores usam hoje.

## O que foi possível ver, e o que não foi

- **O site institucional** ([www97.ogmo-santos.com.br](https://www97.ogmo-santos.com.br/)) foi visto por inteiro na página inicial: os menus, as notícias "Atenção TPA", os acessos "ACESSO TPA" e "OGMO ON" (as duas entradas com login), a Escala Digital, o PAD Digital, o PPP eletrônico e um PDF sobre limpeza de cache no Android e no iOS.
- **A página da Escala Digital** pediu uma verificação anti-robô, e o site passou a recusar os acessos seguintes. A verificação não foi contornada: o que se diz da Escala Digital aqui vem das notícias sobre ela.
- **A área logada** — a escala, a habilitação, o extrato — fica em [www50.ogmo-santos.com.br](https://www50.ogmo-santos.com.br/), que leva à entrada em `/erp/Login/`, com matrícula e senha. Em 28/10/2026 ela parou numa verificação de segurança ("confirme que é humano"), que não foi contornada, e por trás dela não foi acessada. O que se diz dela vem do relato de quem a usa.
- **O aplicativo "Ogmo Santos Digital"**, publicado pela Container Solution, não está mais na Google Play: a página dele responde "não encontrado".

## Como a escala funciona hoje

Pelas leis e pelas fontes públicas:

- **O rodízio é do órgão gestor.** A escalação do avulso, em sistema de rodízio, é feita pelo órgão gestor de mão de obra ([Lei 9.719/1998](https://www.planalto.gov.br/ccivil_03/leis/l9719.htm), art. 5º). O órgão mantém o cadastro e o registro dos trabalhadores ([Lei 12.815/2013](https://www.planalto.gov.br/ccivil_03/_ato2011-2014/2013/lei/l12815.htm), art. 41), e, pelas convenções, o registrado tem a preferência na escala.
- **O dia tem quatro períodos de seis horas**: das 7h às 13h, das 13h às 19h, das 19h à 1h e da 1h às 7h, como dizem as convenções coletivas de Santos.
- **Entre duas jornadas, onze horas de descanso**, salvo as exceções da convenção (Lei 9.719/1998, art. 8º). Numa ação do sindicato dos operadores de guindastes e empilhadeiras, a 7ª Vara do Trabalho de Santos decidiu que o órgão gestor não pode mudar essas exceções por conta própria ([Diário do Litoral](https://www.diariodolitoral.com.br/cotidiano/operadores-de-guindastes-e-empilhadeiras-obtem-vitoria-na-justica/)).
- **A Escala Digital começou em julho de 2019.** Os cerca de seis mil avulsos passaram a poder disputar as vagas pelo celular, pelo tablet ou pelo computador, na mesma ordem do rodízio presencial, com o sistema implantado sindicato por sindicato ([Datamar News](https://www.datamarnews.com/noticias/scale-of-mobile-workers-is-implemented-in-the-port-of-santos/); [OGMO/Santos](https://www97.ogmo-santos.com.br/escaladigital/)).
- **Em 2020, a escalação passou a ser só digital**, para atender à Medida Provisória 945, da pandemia ([Sopesp](https://sopesp.com.br/2025/03/20/ogmo-completa-30-anos-e-destaca-digitalizacao-como-marco-da-decada/); [Portos e Navios](https://www.portosenavios.com.br/noticias/portos-e-logistica/ogmo-santos-completa-30-anos-com-foco-na-digitalizacao-e-qualificacao-da-mao-de-obra)).
- **Há uma frequência mínima.** A norma revista em junho de 2026 exige que o trabalhador compareça às escalações, que acontecem várias vezes por dia pelo sistema informatizado, e prevê medidas disciplinares para quem não cumpre ([norma de frequência mínima](https://www97.ogmo-santos.com.br/norma-de-frequencia-minima/)).
- **Os avisos saem como notícia no site**: recadastramento biométrico, vale-transporte, mudança no horário da escala, cadastro de uma categoria — todos com o título "Atenção TPA", na página inicial.

## O que quem usa relata

- **O site e o aplicativo não se ajustam ao celular.** A tela é de computador: é preciso aumentar com os dedos e arrastar para os lados para ler.
- **Não foram feitos para quem tem mais idade.** A letra é pequena, os botões são pequenos, há siglas que ninguém explica, e o caminho até a informação é longo.
- **A escala tem um layout ruim.** É a tela mais usada — o trabalhador abre para saber se vai trabalhar, onde e quando —, e é a mais difícil de ler.

## O que as fontes reforçam

- **O tempo aperta.** Num processo julgado em 2025 pelo TRT da 2ª Região, que abrange Santos, um trabalhador disse que precisava entrar no sistema de escalação digital em horários certos, e que gastava, em média, meia hora para garantir a inclusão na escala ([COAD](https://coad.com.br/home/noticias-detalhe/130356/escalacao-digital-de-trabalhador-portuario-avulso-nao-configura-tempo-a-disposicao)). Com a frequência mínima, ficar de fora da escalação também tem custo.
- **O aviso não chega à pessoa.** Uma notícia no site espera que o trabalhador vá até ela; quem não abre o site não fica sabendo do recadastramento, do vale-transporte ou da mudança de horário.
- **O celular dá trabalho.** O órgão gestor publicou um PDF sobre limpeza de cache para Android e iOS ([PDF](https://www97.ogmo-santos.com.br/wp-content/uploads/2024/05/Limpeza-de-cache-Android-e-IOS.pdf)), e o aplicativo publicado em 2018 já não está na loja.

## O que este projeto faz diferente

| O problema | O que muda | Parte |
|---|---|:-:|
| A tela não cabe no celular | Feito para o celular primeiro: uma coluna, sem tabela larga, conferido por teste numa tela de 360 pixels | 6 |
| Letra e botões pequenos, siglas | Letra grande que aumenta com um toque, contraste alto, botões de pelo menos 48 pixels, palavra junto de todo ícone, sigla sempre explicada — "ASO (exame médico)" | 6 |
| A escala é difícil de ler | "Minha escala" primeiro, numa frase — "Amanhã, quinta, das 7h às 13h: navio Atlântico Sul, Terminal Cais Leste, berço 4 — estivador, terno 2". A escala completa continua lá, com a linha de quem lê destacada | 6 |
| Quem fica de fora não sabe por quê | Cada "não" com o motivo e os dias em que ele impede: o exame, a reciclagem, as férias, o descanso de onze horas, a posição no rodízio | 1, 2 e 6 |
| A pressa na hora da habilitação | Habilitar-se com um toque, com a confirmação na tela e o horário de fechar à vista | 2 e 6 |
| Ler cansa a vista | A escala lida em voz alta pelo próprio celular | 6 |
| O aviso não chega | Notificação quando sai a escala, quando a habilitação vai fechar e quando o exame está para vencer | 7 |
| Sinal fraco no cais | A última escala fica guardada no celular, para ler sem internet | 6 |
| A família ajuda a acompanhar | Mandar a escala por WhatsApp com um toque | 6 |

A acessibilidade não fica na intenção: a Parte 6 confere por teste o contraste, os tamanhos e as regras das WCAG 2.2, nível AA, em celular.

## Fontes

- OGMO/Santos — [página inicial](https://www97.ogmo-santos.com.br/), [entrada da área logada](https://www50.ogmo-santos.com.br/), [Escala Digital](https://www97.ogmo-santos.com.br/escaladigital/), [norma de frequência mínima](https://www97.ogmo-santos.com.br/norma-de-frequencia-minima/) e [limpeza de cache](https://www97.ogmo-santos.com.br/wp-content/uploads/2024/05/Limpeza-de-cache-Android-e-IOS.pdf).
- [Lei 9.719/1998](https://www.planalto.gov.br/ccivil_03/leis/l9719.htm) e [Lei 12.815/2013](https://www.planalto.gov.br/ccivil_03/_ato2011-2014/2013/lei/l12815.htm).
- [Datamar News, 2019 — a escala pelo celular no porto de Santos](https://www.datamarnews.com/noticias/scale-of-mobile-workers-is-implemented-in-the-port-of-santos/).
- [Sopesp, 2025 — os 30 anos do OGMO e a digitalização](https://sopesp.com.br/2025/03/20/ogmo-completa-30-anos-e-destaca-digitalizacao-como-marco-da-decada/) e [Portos e Navios, 2025](https://www.portosenavios.com.br/noticias/portos-e-logistica/ogmo-santos-completa-30-anos-com-foco-na-digitalizacao-e-qualificacao-da-mao-de-obra).
- [COAD, 2025 — escalação digital não é tempo à disposição](https://coad.com.br/home/noticias-detalhe/130356/escalacao-digital-de-trabalhador-portuario-avulso-nao-configura-tempo-a-disposicao).
- [Diário do Litoral — as exceções ao descanso de onze horas](https://www.diariodolitoral.com.br/cotidiano/operadores-de-guindastes-e-empilhadeiras-obtem-vitoria-na-justica/).
- [Google Play — Ogmo Santos Digital](https://play.google.com/store/apps/details?id=br.com.containersol.ogmosantostpa) (não encontrado em 27/10/2026).
