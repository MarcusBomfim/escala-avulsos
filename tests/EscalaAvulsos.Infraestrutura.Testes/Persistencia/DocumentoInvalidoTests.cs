using System.Text.Json;
using EscalaAvulsos.Dominio.Comum;
using EscalaAvulsos.Dominio.Testes.Trabalhadores;
using EscalaAvulsos.Infraestrutura.Persistencia;

namespace EscalaAvulsos.Infraestrutura.Testes.Persistencia;

/*
 * O documento editado à mão no banco, ou gravado por uma versão com defeito,
 * é recusado na leitura com o id e o motivo — e não lido pela metade (ADR-0011).
 */
public sealed class DocumentoInvalidoTests(BancoDeTestes bancos) : TestesComBanco(bancos)
{
    [Fact]
    public async Task O_documento_que_fere_uma_regra_e_recusado_com_o_motivo_do_dominio()
    {
        var antonio = await Gravado();
        await Banco.ExecutarComoDono("""update escala.trabalhadores set documento = jsonb_set(documento, '{nome}', '" "')""");

        await using var sessao = Banco.NovaSessao();
        var erro = await Assert.ThrowsAsync<DocumentoInvalido>(() => sessao.Trabalhadores.Obter(antonio, Cancelamento));

        Assert.Equal($"O trabalhador {antonio} não se lê: Falta o nome do trabalhador.", erro.Message);
        Assert.IsType<ExcecaoDeDominio>(erro.InnerException);
    }

    [Fact]
    public async Task O_documento_fora_do_formato_e_recusado()
    {
        var antonio = await Gravado();
        await Banco.ExecutarComoDono("update escala.trabalhadores set documento = documento - 'aso'");

        await using var sessao = Banco.NovaSessao();
        var erro = await Assert.ThrowsAsync<DocumentoInvalido>(() => sessao.Trabalhadores.ObterPelaMatricula(Dominio.Identificacao.Matricula.De(4127), Cancelamento));

        Assert.StartsWith($"O trabalhador {antonio} não se lê: ", erro.Message, StringComparison.Ordinal);
        Assert.IsType<JsonException>(erro.InnerException);
    }

    [Fact]
    public async Task O_documento_de_outro_formato_e_recusado_em_vez_de_lido_pela_metade()
    {
        var antonio = await Gravado();
        await Banco.ExecutarComoDono("update escala.trabalhadores set formato = 2");

        await using var sessao = Banco.NovaSessao();
        var erro = await Assert.ThrowsAsync<DocumentoInvalido>(() => sessao.Trabalhadores.Obter(antonio, Cancelamento));

        Assert.Equal(
            $"O trabalhador {antonio} não se lê: O documento do trabalhador {antonio} está no formato 2, e esta versão do sistema lê o formato 1.",
            erro.Message);
    }

    private async Task<Dominio.Trabalhadores.TrabalhadorId> Gravado()
    {
        var antonio = TrabalhadoresDeTeste.Estivador();
        await using var sessao = Banco.NovaSessao();
        await sessao.Trabalhadores.Adicionar(antonio, Cancelamento);

        return antonio.Id;
    }
}
