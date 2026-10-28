using System.Net;
using EscalaAvulsos.Api.Recursos.Escalacoes;
using EscalaAvulsos.Api.Recursos.Escalas;
using EscalaAvulsos.Api.Recursos.Requisicoes;
using EscalaAvulsos.Dominio.Periodos;
using EscalaAvulsos.Infraestrutura.Testes.Persistencia;
using Microsoft.AspNetCore.Hosting;
using static EscalaAvulsos.Api.Testes.Novembro;

namespace EscalaAvulsos.Api.Testes;

/*
 * Um porto em que a habilitação fecha 8 horas antes do período. A escala da
 * tarde pode sair das 5h às 13h, e a da noite das 11h às 19h: as duas ao
 * mesmo tempo, e a da noite antes de a tarde começar. É onde as corridas que
 * o calendário padrão não deixa acontecer acontecem — e o servidor lê de
 * novo e refaz (ADR-0013, ADR-0016).
 */
public sealed class PortoQueFechaCedoTests(BancoDeTestes bancos) : TestesDaApi(bancos)
{
    private const string Tarde = "/api/escalacoes/Estiva/2026-11-05/Tarde";

    private const string Noite = "/api/escalacoes/Estiva/2026-11-05/Noite";

    protected override void Configurar(IWebHostBuilder api)
    {
        api.UseSetting("Porto:Escalacao:AntecedenciaDasRequisicoes", "08:00:00");
        api.UseSetting("Porto:Escalacao:AntecedenciaDaAbertura", "14:00:00");
        api.UseSetting("Porto:Escalacao:AntecedenciaDoFechamento", "08:00:00");
    }

    [Fact]
    public async Task Duas_escalas_da_categoria_no_mesmo_instante_saem_as_duas_de_primeira()
    {
        // As duas partem do mesmo rodízio, e o banco grava uma; o servidor lê o rodízio que ela deixou e refaz a outra.
        await Preparar();
        await Habilitar(4127, Tarde, Em(5, 4));
        await Habilitar(4130, Noite, Em(5, 6));

        Relogio.Acertar(Em(5, 11));
        var token = Tokens.Escalador();
        var respostas = await Task.WhenAll(
            ComToken(token).Enviar(HttpMethod.Post, $"{Tarde}/escala"),
            ComToken(token).Enviar(HttpMethod.Post, $"{Noite}/escala"));

        Assert.All(respostas, resposta => Assert.Equal(HttpStatusCode.Created, resposta.StatusCode));
        var escalas = await Task.WhenAll(respostas.Select(resposta => resposta.Ler<EscalaResposta>()));
        Assert.Equal([4127, 4130], escalas.SelectMany(escala => escala.Vagas).Select(vaga => vaga.Matricula).Order());
    }

    [Fact]
    public async Task Quem_espera_e_ja_pegou_a_noite_nao_entra_na_tarde_de_quem_desiste()
    {
        /*
         * A tarde sai às 5h: o Antônio na vaga, o Benedito na espera. A noite
         * sai às 11h com o Benedito. Às 11h30 o Antônio desiste da tarde: o
         * Benedito ficaria das 13h à 1h no cais, e a vaga fica vazia.
         */
        await Preparar();
        await Habilitar(4127, Tarde, Em(5, 4));
        await Habilitar(4130, Tarde, Em(5, 4));
        await Habilitar(4130, Noite, Em(5, 6));

        Relogio.Acertar(Em(5, 5));
        var daTarde = await Escalador().Enviar(HttpMethod.Post, $"{Tarde}/escala").LerComSucesso<EscalaResposta>();
        Relogio.Acertar(Em(5, 11));
        await Escalador().Enviar(HttpMethod.Post, $"{Noite}/escala").ComSucesso();

        Relogio.Acertar(Em(5, 11, 30));
        var resposta = await Trabalhador(4127).Enviar(HttpMethod.Post, $"/api/eu/escalas/{daTarde.Id}/desistencia").ComSucesso();

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var depois = await Escalador().Ler($"/api/escalas/{daTarde.Id}").LerComSucesso<EscalaResposta>();
        Assert.Empty(depois.Vagas);
        Assert.Equal("estivador: 1, no terno 1 do navio Atlântico Sul (IMO 9876543)", Assert.Single(depois.VagasNaoPreenchidas).Descricao);
    }

    /// <summary>O Antônio e o Benedito cadastrados; uma vaga de estivador na tarde e uma na noite; as duas escalações abertas.</summary>
    private async Task Preparar()
    {
        var escalador = Escalador();
        await escalador.CadastrarEstivador(4127, "Antônio Ferreira");
        await escalador.CadastrarEstivador(4130, "Benedito Lima");

        Relogio.Acertar(Em(4, 10));
        var operador = Operador();
        await operador.Enviar(HttpMethod.Post, "/api/requisicoes", Requisicao(5, Turno.Tarde, new VagasPedidas("Estivador", 1)), chave: "tarde").ComSucesso();
        await operador.Enviar(HttpMethod.Post, "/api/requisicoes", Requisicao(5, Turno.Noite, new VagasPedidas("Estivador", 1)), chave: "noite").ComSucesso();
        await escalador.Enviar(HttpMethod.Put, Tarde).ComSucesso();
        await escalador.Enviar(HttpMethod.Put, Noite).ComSucesso();
    }

    private async Task Habilitar(int matricula, string escalacao, DateTimeOffset em)
    {
        Relogio.Acertar(em);
        await Trabalhador(matricula)
            .Enviar(HttpMethod.Put, escalacao.Replace("/api/escalacoes/Estiva/", "/api/eu/habilitacoes/", StringComparison.Ordinal), new PedidoDeHabilitacao(["Estivador"]))
            .ComSucesso();
    }
}
