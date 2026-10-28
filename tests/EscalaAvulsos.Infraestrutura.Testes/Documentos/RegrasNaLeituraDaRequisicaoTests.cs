using System.Text.Json.Nodes;
using EscalaAvulsos.Dominio.Comum;
using static EscalaAvulsos.Infraestrutura.Testes.Documentos.Exemplos;

namespace EscalaAvulsos.Infraestrutura.Testes.Documentos;

/* A requisição gravada é pedida e cancelada de novo, pelas regras de sempre (ADR-0011). */
public sealed class RegrasNaLeituraDaRequisicaoTests
{
    private static readonly string Cancelada = Json(RequisicaoCancelada());

    [Fact]
    public void O_terno_sem_vaga_e_recusado()
    {
        var adulterada = Adulterado(Cancelada, documento => documento["ternos"]![1] = new JsonArray());

        Assert.Equal("O terno 2 não tem vaga nenhuma.", Assert.Throws<ExcecaoDeDominio>(() => LerRequisicao(adulterada)).Message);
    }

    [Fact]
    public void A_vaga_sem_quantidade_e_recusada()
    {
        var adulterada = Adulterado(Cancelada, documento => documento["ternos"]![0]![3]!["quantidade"] = 0);

        Assert.Equal(
            "No terno 1, as vagas de estivador têm de ser pelo menos 1, e não 0.",
            Assert.Throws<ExcecaoDeDominio>(() => LerRequisicao(adulterada)).Message);
    }

    [Fact]
    public void O_pedido_feito_depois_de_o_periodo_comecar_e_recusado()
    {
        var adulterada = Adulterado(Cancelada, documento =>
        {
            documento["feitaEm"] = "2026-11-05T07:00:00-03:00";
            documento["cancelamento"] = null;
        });

        Assert.Equal(
            "A requisição tem de chegar antes do período, e o de 05/11/2026, das 7h às 13h, já começou.",
            Assert.Throws<ExcecaoDeDominio>(() => LerRequisicao(adulterada)).Message);
    }

    [Fact]
    public void O_cancelamento_depois_de_o_periodo_comecar_e_recusado()
    {
        var adulterada = Adulterado(Cancelada, documento => documento["cancelamento"]!["em"] = "2026-11-05T07:30:00-03:00");

        Assert.Equal(
            "O período de 05/11/2026, das 7h às 13h, já começou: a requisição não pode mais ser cancelada.",
            Assert.Throws<ExcecaoDeDominio>(() => LerRequisicao(adulterada)).Message);
    }

    [Fact]
    public void O_cancelamento_sem_motivo_e_recusado()
    {
        var adulterada = Adulterado(Cancelada, documento => documento["cancelamento"]!["motivo"] = "  ");

        Assert.Equal("Falta o motivo do cancelamento.", Assert.Throws<ExcecaoDeDominio>(() => LerRequisicao(adulterada)).Message);
    }

    [Theory]
    [InlineData("operador", "cnpj", "11222333000182", "O CNPJ 11222333000182 não confere: os dígitos verificadores não batem com o número.")]
    [InlineData("navio", "imo", "9876542", "O número IMO 9876542 não confere: o último dígito não bate com os outros seis.")]
    public void O_operador_e_o_navio_passam_pela_leitura_dos_documentos(string parte, string propriedade, string valor, string mensagem)
    {
        var adulterada = Adulterado(Cancelada, documento => documento[parte]![propriedade] = valor);

        Assert.Equal(mensagem, Assert.Throws<ExcecaoDeDominio>(() => LerRequisicao(adulterada)).Message);
    }
}
