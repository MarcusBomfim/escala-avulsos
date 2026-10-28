using System.Text.Json.Nodes;
using EscalaAvulsos.Dominio.Comum;
using EscalaAvulsos.Dominio.Testes.Escalacoes;
using static EscalaAvulsos.Infraestrutura.Testes.Documentos.Exemplos;

namespace EscalaAvulsos.Infraestrutura.Testes.Documentos;

/*
 * As habilitações gravadas não passam de novo pela aptidão — valiam quando
 * foram feitas —, mas passam pela janela, pelas funções e pela regra de uma
 * por trabalhador (ADR-0011).
 */
public sealed class RegrasNaLeituraDaEscalacaoTests
{
    /// <summary>A escalação da tarde de quinta, com o Edson em primeiro, na ordem das matrículas.</summary>
    private static readonly string Tarde = Json(new TardeDeQuinta().Escalacao);

    private const string DoEdson = "A habilitação da matrícula 4101";

    [Fact]
    public void A_habilitacao_fora_da_janela_e_recusada()
    {
        var adulterada = Adulterado(Tarde, documento => documento["habilitacoes"]![0]!["em"] = "2026-11-05T10:00:00-03:00");

        Assert.Equal(
            $"{DoEdson} foi feita em 05/11/2026, às 10h, fora da janela, que abre em 05/11/2026, à 1h, e fecha em 05/11/2026, às 10h.",
            Assert.Throws<ExcecaoDeDominio>(() => LerEscalacao(adulterada)).Message);
    }

    [Fact]
    public void A_habilitacao_sem_funcao_e_recusada()
    {
        var adulterada = Adulterado(Tarde, documento => documento["habilitacoes"]![0]!["funcoes"] = new JsonArray());

        Assert.Equal($"{DoEdson} não tem função nenhuma.", Assert.Throws<ExcecaoDeDominio>(() => LerEscalacao(adulterada)).Message);
    }

    [Fact]
    public void A_funcao_repetida_e_recusada_mesmo_com_outra_grafia()
    {
        var adulterada = Adulterado(Tarde, documento => documento["habilitacoes"]![0]!["funcoes"] = new JsonArray("Estivador", "estivador"));

        Assert.Equal($"{DoEdson} repete estivador.", Assert.Throws<ExcecaoDeDominio>(() => LerEscalacao(adulterada)).Message);
    }

    [Fact]
    public void O_trabalhador_com_duas_habilitacoes_e_recusado()
    {
        var adulterada = Adulterado(Tarde, documento => documento["habilitacoes"]!.AsArray().Add(documento["habilitacoes"]![0]!.DeepClone()));

        Assert.Equal(
            "A matrícula 4101 tem 2 habilitações na escala da estiva de 05/11/2026, das 13h às 19h.",
            Assert.Throws<ExcecaoDeDominio>(() => LerEscalacao(adulterada)).Message);
    }

    [Fact]
    public void A_matricula_de_dois_trabalhadores_e_recusada()
    {
        var adulterada = Adulterado(Tarde, documento => documento["habilitacoes"]![1]!["matricula"] = 4101);

        Assert.Equal(
            "A matrícula 4101 é de 2 trabalhadores habilitados.",
            Assert.Throws<ExcecaoDeDominio>(() => LerEscalacao(adulterada)).Message);
    }

    [Fact]
    public void O_calendario_passa_pelas_regras_do_calendario()
    {
        var adulterada = Adulterado(Tarde, documento => documento["calendario"]!["antecedenciaDoFechamento"] = "00:00:00");

        Assert.Equal(
            "A habilitação tem de fechar antes de o período começar.",
            Assert.Throws<ExcecaoDeDominio>(() => LerEscalacao(adulterada)).Message);
    }

    [Fact]
    public void Os_problemas_vem_todos_de_uma_vez()
    {
        var adulterada = Adulterado(Tarde, documento =>
        {
            documento["habilitacoes"]![0]!["funcoes"] = new JsonArray();
            documento["habilitacoes"]![1]!["em"] = "2026-11-04T23:00:00-03:00";
        });

        Assert.Equal(
            $"{DoEdson} não tem função nenhuma. A habilitação da matrícula 4127 foi feita em 04/11/2026, às 23h, fora da janela, que abre em 05/11/2026, à 1h, e fecha em 05/11/2026, às 10h.",
            Assert.Throws<ExcecaoDeDominio>(() => LerEscalacao(adulterada)).Message);
    }
}
