using System.Text.Json.Nodes;
using EscalaAvulsos.Dominio.Comum;
using EscalaAvulsos.Dominio.Testes.Escalacoes;
using static EscalaAvulsos.Dominio.Testes.Escalacoes.EscalacoesDeTeste;
using static EscalaAvulsos.Infraestrutura.Testes.Documentos.Exemplos;

namespace EscalaAvulsos.Infraestrutura.Testes.Documentos;

/*
 * A escala gravada não é feita de novo, mas o que foi gravado tem de fechar
 * (ADR-0011). Duas escalas de exemplo, nas posições da ordem das matrículas:
 *
 * A aberta, logo depois de feita — vagas: [0] Antônio, sinaleiro; [1] Carlos e
 * [2] Edson, estivadores. Situações: [0] Edson, [1] Antônio, [2] Benedito, que
 * o rodízio não alcançou, [3] Carlos, [4] Dirceu, em descanso, [5] João,
 * cadastrado. Espera de estivador: [0] Benedito, [1] João.
 *
 * A encerrada, a completa dos exemplos — situações: [0] Edson, que desistiu,
 * [1] Antônio, presente, [2] Benedito, substituto e presente, [3] Carlos, com
 * a falta justificada, [4] Dirceu, [5] Fábio, suspenso, [6] João. Uma vaga
 * vazia, de contramestre.
 */
public sealed class RegrasNaLeituraDaEscalaTests
{
    private const string DaEscala = "A escala da estiva de 05/11/2026, das 13h às 19h,";

    private static readonly Guid Desconhecido = Guid.CreateVersion7();

    private static readonly string Aberta = Json(new TardeDeQuinta().Escalar());

    private static readonly string Encerrada = Json(EscalaCompleta().Escala);

    [Fact]
    public void As_situacoes_voltam_na_ordem_das_matriculas_mesmo_gravadas_fora_dela()
    {
        var foraDeOrdem = Adulterado(Aberta, documento =>
        {
            var situacoes = documento["situacoes"]!.AsArray();
            var edson = situacoes[0]!;
            situacoes.RemoveAt(0);
            situacoes.Add(edson);
        });

        var lida = LerEscala(foraDeOrdem);

        Assert.Equal([4101, 4127, 4130, 4152, 4188, 5310], lida.Situacoes.Select(situacao => situacao.Matricula.Numero));
    }

    [Fact]
    public void A_escala_feita_depois_de_o_periodo_comecar_e_recusada()
    {
        var adulterada = Adulterado(Aberta, documento => documento["feitaEm"] = "2026-11-05T13:00:00-03:00");

        Assert.Equal($"{DaEscala} foi feita em 05/11/2026, às 13h, depois de o período começar.", Recusa(adulterada));
    }

    [Fact]
    public void A_escala_encerrada_antes_de_o_periodo_terminar_e_recusada()
    {
        var adulterada = Adulterado(Encerrada, documento => documento["encerradaEm"] = "2026-11-05T18:00:00-03:00");

        Assert.Equal($"{DaEscala} foi encerrada em 05/11/2026, às 18h, antes de o período terminar.", Recusa(adulterada));
    }

    [Fact]
    public void A_requisicao_repetida_e_recusada()
    {
        var adulterada = Adulterado(Aberta, documento => documento["requisicoes"]!.AsArray().Add(documento["requisicoes"]![0]!.DeepClone()));
        var requisicao = JsonNode.Parse(Aberta)!["requisicoes"]![0]!["id"];

        Assert.Equal($"A requisição {requisicao} aparece 2 vezes na escala.", Recusa(adulterada));
    }

    [Fact]
    public void O_habilitado_repetido_e_recusado()
    {
        var adulterada = Adulterado(Aberta, documento => documento["situacoes"]!.AsArray().Add(documento["situacoes"]![5]!.DeepClone()));

        Assert.Equal("João Batista (matrícula 5310) aparece 2 vezes entre os habilitados.", Recusa(adulterada));
    }

    [Fact]
    public void A_matricula_de_dois_habilitados_e_recusada_e_os_problemas_vem_todos_de_uma_vez()
    {
        var adulterada = Adulterado(Aberta, documento => documento["situacoes"]![5]!["matricula"] = 4188);

        Assert.Equal(
            "A matrícula 4188 é de 2 habilitados diferentes. Na espera de estivador, João Batista (matrícula 4188) está com a matrícula 5310.",
            Recusa(adulterada));
    }

    [Fact]
    public void A_vaga_de_quem_nao_se_habilitou_e_recusada()
    {
        var adulterada = Adulterado(Aberta, documento => documento["vagas"]![2]!["trabalhador"] = Desconhecido);

        Assert.Equal($"A vaga de estivador no terno 1 é do trabalhador {Desconhecido}, que não se habilitou.", Recusa(adulterada));
    }

    [Fact]
    public void Duas_vagas_para_o_mesmo_trabalhador_sao_recusadas()
    {
        var adulterada = Adulterado(Aberta, documento => documento["vagas"]![2]!["trabalhador"] = documento["vagas"]![1]!["trabalhador"]!.DeepClone());

        Assert.Equal("Carlos Mota (matrícula 4152) tem 2 vagas na escala.", Recusa(adulterada));
    }

    [Fact]
    public void A_vaga_numa_funcao_em_que_nao_se_habilitou_e_recusada()
    {
        var adulterada = Adulterado(Aberta, documento => documento["vagas"]![2]!["funcao"] = "Sinaleiro");

        Assert.Equal("Edson Prado (matrícula 4101) está na vaga de sinaleiro, em que não se habilitou.", Recusa(adulterada));
    }

    [Fact]
    public void A_vaga_num_terno_que_nao_existe_e_recusada()
    {
        var adulterada = Adulterado(Aberta, documento => documento["vagas"]![2]!["terno"] = 0);

        Assert.Equal("A vaga de estivador está no terno 0, e os ternos contam de 1 em diante.", Recusa(adulterada));
    }

    [Fact]
    public void A_vaga_de_uma_requisicao_que_a_escala_nao_tem_e_recusada()
    {
        var adulterada = Adulterado(Aberta, documento => documento["vagas"]![2]!["requisicao"] = Desconhecido);

        Assert.Equal($"A vaga de estivador no terno 1 é da requisição {Desconhecido}, que a escala não tem.", Recusa(adulterada));
    }

    [Fact]
    public void O_habilitado_sem_funcao_e_recusado()
    {
        var adulterada = Adulterado(Aberta, documento => documento["situacoes"]![5]!["funcoes"] = new JsonArray());

        Assert.Equal(
            "João Batista (matrícula 5310) se habilitou sem função nenhuma. João Batista (matrícula 5310) está na espera de estivador, em que não se habilitou.",
            Recusa(adulterada));
    }

    [Fact]
    public void A_funcao_repetida_na_habilitacao_e_recusada()
    {
        var adulterada = Adulterado(Aberta, documento => documento["situacoes"]![5]!["funcoes"] = new JsonArray("Estivador", "estivador"));

        Assert.Equal("João Batista (matrícula 5310) se habilitou 2 vezes como estivador.", Recusa(adulterada));
    }

    [Fact]
    public void Presenca_ou_falta_de_quem_nao_esta_na_escala_e_recusada()
    {
        var adulterada = Adulterado(Aberta, documento => documento["situacoes"]![2]!["comparecimento"] = "Falta");

        Assert.Equal("Benedito Lima (matrícula 4130) tem presença ou falta sem estar na escala.", Recusa(adulterada));
    }

    [Fact]
    public void A_presenca_sem_a_hora_e_a_hora_sem_a_presenca_sao_recusadas()
    {
        var semHora = Adulterado(Aberta, documento => documento["situacoes"]![1]!["comparecimento"] = "Presente");
        var semPresenca = Adulterado(Aberta, documento => documento["situacoes"]![1]!["presenteDesde"] = "2026-11-05T12:30:00-03:00");

        Assert.Equal("Antônio Ferreira (matrícula 4127) está com a presença confirmada, sem a hora em que chegou.", Recusa(semHora));
        Assert.Equal("Antônio Ferreira (matrícula 4127) tem a hora em que chegou, sem a presença confirmada.", Recusa(semPresenca));
    }

    [Theory]
    [InlineData("2026-11-05T11:59:00-03:00", "05/11/2026, às 11h59")]
    [InlineData("2026-11-05T19:00:00-03:00", "05/11/2026, às 19h")]
    public void A_presenca_fora_da_hora_e_recusada(string instante, string escrito)
    {
        var adulterada = Adulterado(Aberta, documento =>
        {
            documento["situacoes"]![1]!["comparecimento"] = "Presente";
            documento["situacoes"]![1]!["presenteDesde"] = instante;
        });

        Assert.Equal(
            $"Antônio Ferreira (matrícula 4127) teve a presença confirmada em {escrito}, fora da hora: ela vale de 05/11/2026, ao meio-dia, ao fim do período.",
            Recusa(adulterada));
    }

    [Fact]
    public void A_falta_justificada_sem_a_justificativa_e_a_justificativa_sem_a_falta_justificada_sao_recusadas()
    {
        var semJustificativa = Adulterado(Encerrada, documento => documento["situacoes"]![3]!["justificativaDaFalta"] = null);
        var semFaltaJustificada = Adulterado(Encerrada, documento => documento["situacoes"]![3]!["comparecimento"] = "Falta");

        Assert.Equal("Carlos Mota (matrícula 4152) tem a falta justificada, sem a justificativa.", Recusa(semJustificativa));
        Assert.Equal("Carlos Mota (matrícula 4152) tem justificativa de falta, sem a falta justificada.", Recusa(semFaltaJustificada));
    }

    [Fact]
    public void A_justificativa_passa_pela_regra_do_tamanho()
    {
        var adulterada = Adulterado(Encerrada, documento => documento["situacoes"]![3]!["justificativaDaFalta"] = new string('a', 201));

        Assert.Equal("Carlos Mota (matrícula 4152): a justificativa da falta passa de 200 caracteres.", Recusa(adulterada));
    }

    [Fact]
    public void A_desistencia_depois_de_o_periodo_comecar_e_recusada()
    {
        var adulterada = Adulterado(Encerrada, documento => documento["situacoes"]![0]!["desistencia"]!["em"] = "2026-11-05T13:00:00-03:00");

        Assert.Equal("Edson Prado (matrícula 4101) desistiu em 05/11/2026, às 13h, depois de o período começar.", Recusa(adulterada));
    }

    [Fact]
    public void Quem_desistiu_e_voltou_para_a_escala_e_recusado()
    {
        var adulterada = Adulterado(Aberta, documento => documento["situacoes"]![0]!["desistencia"] = new JsonObject
        {
            ["requisicao"] = documento["requisicoes"]![0]!["id"]!.DeepClone(),
            ["terno"] = 1,
            ["funcao"] = "Estivador",
            ["em"] = "2026-11-05T11:00:00-03:00",
        });

        Assert.Equal("Edson Prado (matrícula 4101) desistiu e está de novo na escala: quem desiste não volta.", Recusa(adulterada));
    }

    [Fact]
    public void A_desistencia_de_uma_funcao_em_que_nao_se_habilitou_e_recusada()
    {
        var adulterada = Adulterado(Encerrada, documento => documento["situacoes"]![0]!["desistencia"]!["funcao"] = "Sinaleiro");

        Assert.Equal("Edson Prado (matrícula 4101) desistiu da vaga de sinaleiro, em que não se habilitou.", Recusa(adulterada));
    }

    [Fact]
    public void A_vaga_desistida_passa_pelas_regras_da_vaga()
    {
        var adulterada = Adulterado(Encerrada, documento => documento["situacoes"]![0]!["desistencia"]!["terno"] = 0);

        Assert.Equal("A vaga de estivador está no terno 0, e os ternos contam de 1 em diante.", Recusa(adulterada));
    }

    [Fact]
    public void O_substituto_sem_vaga_e_recusado()
    {
        var adulterada = Adulterado(Aberta, documento => documento["situacoes"]![2]!["substituto"] = true);

        Assert.Equal("Benedito Lima (matrícula 4130) é substituto, e não pegou vaga nenhuma.", Recusa(adulterada));
    }

    [Fact]
    public void O_substituto_que_desistiu_volta_sem_vaga()
    {
        // O Benedito entra no lugar do Edson e desiste também: substituto, sem vaga, e com a desistência.
        var tarde = new TardeDeQuinta();
        var escala = tarde.Escalar();
        escala.Desistir(tarde.Edson.Id, Em(5, 11), []);
        escala.Desistir(tarde.Benedito.Id, Em(5, 11, 30), []);

        var lida = LerEscala(Json(escala));

        Assert.Equal(
            "Benedito Lima (matrícula 4130) desistiu da vaga de estivador em 05/11/2026, às 11h30.",
            lida.SituacaoDe(Dominio.Identificacao.Matricula.De(4130))?.ToString());
    }

    [Fact]
    public void A_escala_encerrada_com_alguem_aguardando_e_recusada()
    {
        var adulterada = Adulterado(Encerrada, documento =>
        {
            documento["situacoes"]![3]!["comparecimento"] = "Aguardando";
            documento["situacoes"]![3]!["justificativaDaFalta"] = null;
        });

        Assert.Equal(
            $"{DaEscala} foi encerrada com Carlos Mota (matrícula 4152) aguardando: quem não teve a presença confirmada fica com falta.",
            Recusa(adulterada));
    }

    [Fact]
    public void As_vagas_vazias_sao_pelo_menos_uma_e_de_uma_requisicao_da_escala()
    {
        var nenhuma = Adulterado(Encerrada, documento => documento["vagasNaoPreenchidas"]![0]!["quantidade"] = 0);
        var deOutraRequisicao = Adulterado(Encerrada, documento => documento["vagasNaoPreenchidas"]![0]!["requisicao"] = Desconhecido);

        Assert.Equal("As vagas vazias de contramestre de terno, no terno 1, são 0, e têm de ser pelo menos 1.", Recusa(nenhuma));
        Assert.Equal($"A vaga de contramestre de terno no terno 1 é da requisição {Desconhecido}, que a escala não tem.", Recusa(deOutraRequisicao));
    }

    [Fact]
    public void A_espera_de_quem_nao_se_habilitou_e_recusada()
    {
        var adulterada = Adulterado(Aberta, documento => documento["espera"]![0]!["trabalhador"] = Desconhecido);

        Assert.Equal("Na espera de estivador, a matrícula 4130 não se habilitou.", Recusa(adulterada));
    }

    [Fact]
    public void A_espera_repetida_e_recusada()
    {
        var adulterada = Adulterado(Aberta, documento => documento["espera"]!.AsArray().Add(documento["espera"]![0]!.DeepClone()));

        Assert.Equal("Benedito Lima (matrícula 4130) está 2 vezes na espera de estivador.", Recusa(adulterada));
    }

    [Fact]
    public void A_espera_com_outra_matricula_e_recusada()
    {
        var adulterada = Adulterado(Aberta, documento => documento["espera"]![0]!["matricula"] = 4131);

        Assert.Equal("Na espera de estivador, Benedito Lima (matrícula 4130) está com a matrícula 4131.", Recusa(adulterada));
    }

    [Fact]
    public void A_espera_numa_funcao_em_que_nao_se_habilitou_e_recusada()
    {
        var adulterada = Adulterado(Aberta, documento => documento["espera"]![0]!["funcao"] = "Sinaleiro");

        Assert.Equal("Benedito Lima (matrícula 4130) está na espera de sinaleiro, em que não se habilitou.", Recusa(adulterada));
    }

    [Fact]
    public void O_rodizio_passa_pela_regra_de_uma_vez_por_funcao()
    {
        var adulterada = Adulterado(Aberta, documento => documento["rodizioDepois"]!.AsArray().Add(documento["rodizioDepois"]![0]!.DeepClone()));

        Assert.Equal("O rodízio de estivador tem duas vezes para os registrados.", Recusa(adulterada));
    }

    [Theory]
    [InlineData(2, "posicoesAntesDaVez", null)]
    [InlineData(2, "posicoesAntesDaVez", 0)]
    [InlineData(4, "funcao", "Estivador")]
    [InlineData(5, "posicoesAntesDaVez", 3)]
    public void O_motivo_de_quem_ficou_de_fora_traz_o_que_o_tipo_pede(int situacao, string propriedade, object? valor)
    {
        var adulterada = Adulterado(Aberta, documento => documento["situacoes"]![situacao]!["motivos"]![0]![propriedade] = JsonValue.Create(valor));

        var esperado = situacao switch
        {
            2 => "O motivo NaoAlcancado, de estivador, não traz o que o tipo pede: a função e as posições antes da vez, a partir de 1.",
            4 => "O motivo EmDescanso, de estivador, não traz o que o tipo pede: a jornada que pede descanso, sem função.",
            _ => "O motivo VagasComRegistrados, de estivador, não traz o que o tipo pede: a função, e só ela.",
        };
        Assert.Equal(esperado, Recusa(adulterada));
    }

    [Fact]
    public void O_descanso_sem_a_jornada_e_recusado()
    {
        var adulterada = Adulterado(Aberta, documento => documento["situacoes"]![4]!["motivos"]![0]!["jornada"] = null);

        Assert.Equal("O motivo EmDescanso não traz o que o tipo pede: a jornada que pede descanso, sem função.", Recusa(adulterada));
    }

    [Fact]
    public void Os_motivos_trazem_so_o_que_e_do_tipo()
    {
        var jornada = new JsonObject { ["dia"] = "2026-11-05", ["turno"] = "Manha" };
        var alcancadoComJornada = Adulterado(Aberta, documento => documento["situacoes"]![2]!["motivos"]![0]!["jornada"] = jornada.DeepClone());
        var semVagaComJornada = Adulterado(Encerrada, documento => documento["situacoes"]![5]!["motivos"]![1]!["jornada"] = jornada.DeepClone());
        var naoAptoComPosicoes = Adulterado(Encerrada, documento => documento["situacoes"]![5]!["motivos"]![0]!["posicoesAntesDaVez"] = 1);

        Assert.Equal("O motivo NaoAlcancado, de estivador, não traz o que o tipo pede: a função e as posições antes da vez, a partir de 1.", Recusa(alcancadoComJornada));
        Assert.Equal("O motivo SemVagaNaFuncao, de operador de guindaste de bordo, não traz o que o tipo pede: a função, e só ela.", Recusa(semVagaComJornada));
        Assert.Equal("O motivo NaoApto, de estivador, não traz o que o tipo pede: a função e os motivos da aptidão.", Recusa(naoAptoComPosicoes));
    }

    [Fact]
    public void Quem_nao_podia_trabalhar_tem_os_motivos_da_aptidao()
    {
        var semMotivos = Adulterado(Encerrada, documento => documento["situacoes"]![5]!["motivos"]![0]!["motivosDaAptidao"] = new JsonArray());

        Assert.Equal("O motivo NaoApto, de estivador, não traz o que o tipo pede: a função e os motivos da aptidão.", Recusa(semMotivos));
    }

    [Fact]
    public void O_motivo_da_aptidao_tem_a_frase_e_os_dias_em_ordem()
    {
        var semFrase = Adulterado(Encerrada, documento => documento["situacoes"]![5]!["motivos"]![0]!["motivosDaAptidao"]![0]!["descricao"] = " ");
        var diasTrocados = Adulterado(Encerrada, documento =>
        {
            documento["situacoes"]![5]!["motivos"]![0]!["motivosDaAptidao"]![0]!["de"] = "2026-11-06";
            documento["situacoes"]![5]!["motivos"]![0]!["motivosDaAptidao"]![0]!["ate"] = "2026-11-05";
        });

        Assert.Equal("O motivo Suspensao não tem a frase que o explica.", Recusa(semFrase));
        Assert.Equal("O motivo Suspensao impede de 06/11/2026 a 05/11/2026: o último dia é antes do primeiro.", Recusa(diasTrocados));
    }

    private static string Recusa(string json) => Assert.Throws<ExcecaoDeDominio>(() => LerEscala(json)).Message;
}
