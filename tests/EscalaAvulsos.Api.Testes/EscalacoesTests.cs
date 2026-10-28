using System.Net;
using EscalaAvulsos.Api.Recursos.Escalacoes;
using EscalaAvulsos.Infraestrutura.Testes.Persistencia;

namespace EscalaAvulsos.Api.Testes;

/* O órgão gestor abre a escalação de cada categoria e período; o endereço é a identidade dela. */
public sealed class EscalacoesTests(BancoDeTestes bancos) : TestesDaApi(bancos)
{
    private const string DaTarde = "/api/escalacoes/Estiva/2026-11-05/Tarde";

    [Fact]
    public async Task A_escalacao_abre_com_o_calendario_do_porto()
    {
        var resposta = await Escalador().Enviar(HttpMethod.Put, DaTarde).ComSucesso();

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        Assert.Equal(DaTarde, resposta.Headers.Location?.ToString());
        var escalacao = await resposta.Ler<EscalacaoResposta>();
        Assert.Equal("05/11/2026, das 13h às 19h", escalacao.Periodo.Descricao);
        Assert.Equal(
            (Novembro.Em(5, 9), Novembro.Em(5, 1), Novembro.Em(5, 10)),
            (escalacao.PrazoDasRequisicoes, escalacao.AberturaDaHabilitacao, escalacao.FechamentoDaHabilitacao));
    }

    [Fact]
    public async Task Abrir_de_novo_devolve_a_que_ja_esta_aberta()
    {
        var escalador = Escalador();
        var primeira = await escalador.Enviar(HttpMethod.Put, DaTarde).ComSucesso();

        var segunda = await escalador.Enviar(HttpMethod.Put, DaTarde).ComSucesso();

        Assert.Equal(HttpStatusCode.OK, segunda.StatusCode);
        Assert.Equal(primeira.Headers.ETag, segunda.Headers.ETag);
    }

    [Fact]
    public async Task Abrir_ao_mesmo_tempo_abre_uma_so()
    {
        var token = Tokens.Escalador();

        var respostas = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => ComToken(token).Enviar(HttpMethod.Put, DaTarde)));

        Assert.Single(respostas, resposta => resposta.StatusCode == HttpStatusCode.Created);
        Assert.All(respostas, resposta => Assert.True(resposta.IsSuccessStatusCode));
    }

    [Fact]
    public async Task A_escalacao_que_nao_foi_aberta_e_404()
    {
        var resposta = await Escalador().Ler(DaTarde);

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
        Assert.Equal("A escala da estiva de 05/11/2026, das 13h às 19h, ainda não foi aberta.", (await resposta.Problema()).Detail);
    }

    [Theory]
    [InlineData("/api/escalacoes/Pesca/2026-11-05/Tarde", "O valor de categoria é um destes: Capatazia, Estiva, ConferenciaDeCarga, ConsertoDeCarga, VigilanciaDeEmbarcacoes, Bloco; veio \"Pesca\".")]
    [InlineData("/api/escalacoes/Estiva/05-11-2026/Tarde", "O valor de dia é um dia, como 2026-11-05; veio \"05-11-2026\".")]
    [InlineData("/api/escalacoes/Estiva/2026-11-05/2", "O valor de turno é um destes: Madrugada, Manha, Tarde, Noite; veio \"2\".")]
    public async Task O_endereco_fora_do_formato_e_400_dizendo_o_formato(string endereco, string detalhe)
    {
        var resposta = await Escalador().Enviar(HttpMethod.Put, endereco);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Equal(detalhe, (await resposta.Problema()).Detail);
    }
}
