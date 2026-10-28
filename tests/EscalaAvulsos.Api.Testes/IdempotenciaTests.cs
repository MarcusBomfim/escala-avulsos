using System.Net;
using EscalaAvulsos.Api.Recursos.Requisicoes;
using EscalaAvulsos.Dominio.Periodos;
using EscalaAvulsos.Infraestrutura.Testes.Persistencia;
using static EscalaAvulsos.Api.Testes.Novembro;

namespace EscalaAvulsos.Api.Testes;

/*
 * A requisição não tem identidade natural: o mesmo pedido mandado de novo —
 * o sinal caiu, o botão foi apertado duas vezes — seria outra requisição, e
 * o órgão gestor escalaria o dobro de gente. A chave de idempotência faz o
 * pedido repetido receber a resposta da primeira vez (ADR-0015).
 */
public sealed class IdempotenciaTests(BancoDeTestes bancos) : TestesDaApi(bancos)
{
    private static readonly NovaRequisicao DaTarde = Requisicao(5, Turno.Tarde, new VagasPedidas("Estivador", 2));

    [Fact]
    public async Task Sem_a_chave_a_criacao_e_400_e_nada_e_criado()
    {
        Relogio.Acertar(Em(4, 10));

        var resposta = await Operador().Enviar(HttpMethod.Post, "/api/requisicoes", DaTarde);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.StartsWith("Todo pedido que cria leva o cabeçalho Idempotency-Key", (await resposta.Problema()).Detail, StringComparison.Ordinal);
        Assert.Empty(await RequisicoesDaTarde());
    }

    [Fact]
    public async Task O_pedido_repetido_com_a_mesma_chave_recebe_a_resposta_da_primeira_vez_e_cria_uma_vez_so()
    {
        Relogio.Acertar(Em(4, 10));
        var operador = Operador();

        var primeira = await operador.Enviar(HttpMethod.Post, "/api/requisicoes", DaTarde, chave: "pedido-1").ComSucesso();
        var repetida = await operador.Enviar(HttpMethod.Post, "/api/requisicoes", DaTarde, chave: "pedido-1").ComSucesso();

        Assert.Equal(HttpStatusCode.Created, repetida.StatusCode);
        Assert.Equal(primeira.Headers.Location, repetida.Headers.Location);
        Assert.Equal(primeira.Headers.ETag, repetida.Headers.ETag);
        Assert.Equal(await primeira.Content.ReadAsStringAsync(Cancelamento), await repetida.Content.ReadAsStringAsync(Cancelamento));
        Assert.Equal(["true"], repetida.Headers.GetValues("Idempotent-Replayed"));
        Assert.False(primeira.Headers.Contains("Idempotent-Replayed"));
        Assert.Single(await RequisicoesDaTarde());
    }

    [Fact]
    public async Task A_mesma_chave_num_pedido_diferente_e_recusada()
    {
        Relogio.Acertar(Em(4, 10));
        var operador = Operador();
        await operador.Enviar(HttpMethod.Post, "/api/requisicoes", DaTarde, chave: "pedido-1").ComSucesso();

        var outro = await operador.Enviar(HttpMethod.Post, "/api/requisicoes", Requisicao(5, Turno.Tarde, new VagasPedidas("Estivador", 3)), chave: "pedido-1");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, outro.StatusCode);
        Assert.Equal("A chave de idempotência pedido-1 já foi usada num pedido diferente deste. Pedido novo leva chave nova.", (await outro.Problema()).Detail);
    }

    [Fact]
    public async Task A_chave_de_um_operador_nao_esbarra_na_de_outro()
    {
        Relogio.Acertar(Em(4, 10));

        await Operador().Enviar(HttpMethod.Post, "/api/requisicoes", DaTarde, chave: "pedido-1").ComSucesso();
        var deOutro = await Operador("11.444.777/0001-61", "Porto Seco Operações").Enviar(HttpMethod.Post, "/api/requisicoes", DaTarde, chave: "pedido-1").ComSucesso();

        Assert.False(deOutro.Headers.Contains("Idempotent-Replayed"));
        Assert.Equal(2, (await RequisicoesDaTarde()).Count);
    }

    [Fact]
    public async Task O_pedido_recusado_nao_gasta_a_chave()
    {
        var operador = Operador();
        Relogio.Acertar(Em(5, 13, 30));
        var tarde = await operador.Enviar(HttpMethod.Post, "/api/requisicoes", DaTarde, chave: "pedido-1");

        Relogio.Acertar(Em(4, 10));
        var aTempo = await operador.Enviar(HttpMethod.Post, "/api/requisicoes", DaTarde, chave: "pedido-1").ComSucesso();

        Assert.Equal(HttpStatusCode.UnprocessableEntity, tarde.StatusCode);
        Assert.Equal(HttpStatusCode.Created, aTempo.StatusCode);
        Assert.False(aTempo.Headers.Contains("Idempotent-Replayed"));
    }

    [Fact]
    public async Task Dez_vezes_o_mesmo_pedido_ao_mesmo_tempo_criam_uma_requisicao_so()
    {
        Relogio.Acertar(Em(4, 10));
        var token = Tokens.Operador("11.222.333/0001-81", "Cais Forte Operações Portuárias");

        var respostas = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ =>
            ComToken(token).Enviar(HttpMethod.Post, "/api/requisicoes", DaTarde, chave: "pedido-1")));

        Assert.All(respostas, resposta => Assert.Equal(HttpStatusCode.Created, resposta.StatusCode));
        Assert.Single(respostas.Select(resposta => resposta.Headers.Location).Distinct());
        Assert.Single(await RequisicoesDaTarde());
    }

    private Task<IReadOnlyList<RequisicaoResposta>> RequisicoesDaTarde() =>
        Escalador().Ler("/api/escalacoes/Estiva/2026-11-05/Tarde/requisicoes").LerComSucesso<IReadOnlyList<RequisicaoResposta>>();
}
