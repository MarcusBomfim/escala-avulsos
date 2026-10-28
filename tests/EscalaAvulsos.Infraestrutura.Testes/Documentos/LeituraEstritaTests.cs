using System.Text.Json;
using System.Text.Json.Nodes;
using EscalaAvulsos.Dominio.Testes.Escalacoes;
using EscalaAvulsos.Dominio.Testes.Trabalhadores;
using static EscalaAvulsos.Infraestrutura.Testes.Documentos.Exemplos;

namespace EscalaAvulsos.Infraestrutura.Testes.Documentos;

/*
 * Um documento que não é exatamente o formato esperado não vira, em
 * silêncio, um agregado com um campo zerado (ADR-0011): a leitura falha, e
 * diz onde.
 */
public sealed class LeituraEstritaTests
{
    private static readonly string Antonio = Json(TrabalhadoresDeTeste.Estivador());

    [Fact]
    public void Propriedade_que_falta_e_recusada()
    {
        var semAso = Adulterado(Antonio, documento => documento.Remove("aso"));

        var erro = Assert.Throws<JsonException>(() => LerTrabalhador(semAso));

        Assert.Contains("aso", erro.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Propriedade_que_o_formato_nao_tem_e_recusada()
    {
        var comApelido = Adulterado(Antonio, documento => documento["apelido"] = "Toninho");

        var erro = Assert.Throws<JsonException>(() => LerTrabalhador(comApelido));

        Assert.Contains("apelido", erro.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Nulo_onde_nao_cabe_nulo_e_recusado()
    {
        var semNome = Adulterado(Antonio, documento => documento["nome"] = null);

        Assert.Throws<JsonException>(() => LerTrabalhador(semNome));
    }

    [Fact]
    public void Nulo_dentro_de_uma_lista_e_recusado()
    {
        var comQualificacaoNula = Adulterado(Antonio, documento => documento["qualificacoes"]!.AsArray().Add(null));
        var comTernoNulo = Adulterado(Json(Exemplos.RequisicaoCancelada()), documento => documento["ternos"]![1]!.AsArray().Insert(0, null));

        Assert.Equal("A lista $.qualificacoes tem um item nulo, na posição 1.", Assert.Throws<JsonException>(() => LerTrabalhador(comQualificacaoNula)).Message);
        Assert.Equal("A lista $.ternos[1] tem um item nulo, na posição 0.", Assert.Throws<JsonException>(() => LerRequisicao(comTernoNulo)).Message);
    }

    [Fact]
    public void Enum_pelo_numero_e_recusado_mesmo_que_o_numero_exista()
    {
        var registradoPeloNumero = Adulterado(Antonio, documento => documento["inscricao"] = 1);

        Assert.Throws<JsonException>(() => LerTrabalhador(registradoPeloNumero));
    }

    [Fact]
    public void Enum_com_nome_que_nao_existe_e_recusado()
    {
        var deOutraCategoria = Adulterado(Antonio, documento => documento["categoria"] = "Pesca");

        Assert.Throws<JsonException>(() => LerTrabalhador(deOutraCategoria));
    }

    [Fact]
    public void Documento_nulo_e_recusado()
    {
        var erro = Assert.Throws<JsonException>(() => LerTrabalhador("null"));

        Assert.Equal("O documento gravado é nulo.", erro.Message);
    }

    [Fact]
    public void A_escala_tambem_e_lida_estrita_por_dentro()
    {
        var escala = Json(new TardeDeQuinta().Escalar());
        var vagaComLugar = Adulterado(escala, documento => documento["vagas"]![0]!.AsObject()["lugar"] = JsonValue.Create("porão 2"));

        var erro = Assert.Throws<JsonException>(() => LerEscala(vagaComLugar));

        Assert.Contains("lugar", erro.Message, StringComparison.Ordinal);
    }
}
