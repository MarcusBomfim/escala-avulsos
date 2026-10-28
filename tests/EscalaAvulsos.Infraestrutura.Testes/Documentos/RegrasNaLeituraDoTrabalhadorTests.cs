using System.Text.Json.Nodes;
using EscalaAvulsos.Dominio.Comum;
using static EscalaAvulsos.Infraestrutura.Testes.Documentos.Exemplos;

namespace EscalaAvulsos.Infraestrutura.Testes.Documentos;

/*
 * O trabalhador gravado volta pelos passos de quem o cadastrou, e as regras
 * de cada passo valem na leitura (ADR-0011): o documento editado à mão no
 * banco é recusado com o motivo, e não descoberto depois, numa escala errada.
 */
public sealed class RegrasNaLeituraDoTrabalhadorTests
{
    private static readonly string Antonio = Json(TrabalhadorCompleto());

    [Theory]
    [InlineData("cpf", "52998224724", "O CPF 52998224724 não confere: os dígitos verificadores não batem com o número.")]
    [InlineData("nome", " ", "Falta o nome do trabalhador.")]
    public void Os_dados_da_inscricao_passam_pelas_regras(string propriedade, string valor, string mensagem)
    {
        var adulterado = Adulterado(Antonio, documento => documento[propriedade] = valor);

        Assert.Equal(mensagem, Assert.Throws<ExcecaoDeDominio>(() => LerTrabalhador(adulterado)).Message);
    }

    [Fact]
    public void A_matricula_passa_pela_leitura_da_matricula()
    {
        var adulterado = Adulterado(Antonio, documento => documento["matricula"] = 0);

        Assert.Equal("A matrícula vai de 1 a 9999999: 0 não é matrícula.", Assert.Throws<ExcecaoDeDominio>(() => LerTrabalhador(adulterado)).Message);
    }

    [Fact]
    public void Duas_qualificacoes_na_mesma_funcao_nao_sao_uma_renovacao()
    {
        var adulterado = Adulterado(Antonio, documento => documento["qualificacoes"]!.AsArray().Add(documento["qualificacoes"]![1]!.DeepClone()));

        Assert.Equal(
            "Antônio Ferreira (matrícula 4127) tem 2 qualificações de sinaleiro.",
            Assert.Throws<ExcecaoDeDominio>(() => LerTrabalhador(adulterado)).Message);
    }

    [Fact]
    public void A_qualificacao_passa_pela_regra_das_datas()
    {
        var adulterado = Adulterado(Antonio, documento => documento["qualificacoes"]![1]!["validaAte"] = "2009-05-09");

        Assert.Equal(
            "A qualificação de sinaleiro não pode valer só até 09/05/2009, antes de começar, em 10/05/2010.",
            Assert.Throws<ExcecaoDeDominio>(() => LerTrabalhador(adulterado)).Message);
    }

    [Fact]
    public void O_exame_passa_pela_regra_da_validade()
    {
        var adulterado = Adulterado(Antonio, documento => documento["aso"]!["validoAte"] = "2025-11-30");

        Assert.Equal(
            "O exame médico emitido em 01/12/2025 não pode valer só até 30/11/2025, antes de ser emitido.",
            Assert.Throws<ExcecaoDeDominio>(() => LerTrabalhador(adulterado)).Message);
    }

    [Fact]
    public void Impedimentos_do_mesmo_tipo_que_cruzam_sao_recusados()
    {
        var adulterado = Adulterado(Antonio, documento => documento["impedimentos"]!.AsArray().Add(documento["impedimentos"]![0]!.DeepClone()));

        Assert.Equal(
            "Não dá para registrar as férias de 04/01/2027 a 02/02/2027: as datas cruzam com as férias de 04/01/2027 a 02/02/2027.",
            Assert.Throws<ExcecaoDeDominio>(() => LerTrabalhador(adulterado)).Message);
    }

    [Fact]
    public void A_suspensao_sem_fim_e_recusada()
    {
        var adulterado = Adulterado(Antonio, documento =>
        {
            documento["impedimentos"]![0]!["tipo"] = "Suspensao";
            documento["impedimentos"]![0]!["ate"] = null;
        });

        Assert.Equal("A suspensão precisa do último dia.", Assert.Throws<ExcecaoDeDominio>(() => LerTrabalhador(adulterado)).Message);
    }

    [Fact]
    public void O_cancelamento_sem_motivo_e_recusado()
    {
        var adulterado = Adulterado(
            Json(TrabalhadorCancelado()),
            documento => documento["cancelamento"]!["motivo"] = JsonValue.Create(string.Empty));

        Assert.Equal("Falta o motivo do cancelamento.", Assert.Throws<ExcecaoDeDominio>(() => LerTrabalhador(adulterado)).Message);
    }
}
