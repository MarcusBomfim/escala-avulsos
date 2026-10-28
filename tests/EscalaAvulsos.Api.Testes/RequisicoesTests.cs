using System.Net;
using EscalaAvulsos.Api.Recursos.Requisicoes;
using EscalaAvulsos.Dominio.Periodos;
using EscalaAvulsos.Dominio.Requisicoes;
using EscalaAvulsos.Infraestrutura.Testes.Persistencia;
using static EscalaAvulsos.Api.Testes.Novembro;

namespace EscalaAvulsos.Api.Testes;

/* O operador pede a mão de obra em nome do CNPJ do token, e só enxerga o que é dele. */
public sealed class RequisicoesTests(BancoDeTestes bancos) : TestesDaApi(bancos)
{
    private static readonly NovaRequisicao DaTarde = Requisicao(5, Turno.Tarde, new VagasPedidas("Sinaleiro", 1), new VagasPedidas("estivador", 2));

    [Fact]
    public async Task A_requisicao_e_do_operador_do_token_e_fica_no_endereco_dela()
    {
        Relogio.Acertar(Em(4, 10));

        var resposta = await Operador().Enviar(HttpMethod.Post, "/api/requisicoes", DaTarde, chave: Guid.NewGuid().ToString()).ComSucesso();

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        Assert.Equal(1, resposta.Versao());
        var requisicao = await resposta.Ler<RequisicaoResposta>();
        Assert.Equal($"/api/requisicoes/{requisicao.Id}", resposta.Headers.Location?.ToString());
        Assert.Equal(new OperadorResposta("Cais Forte Operações Portuárias", "11.222.333/0001-81"), requisicao.Operador);
        Assert.Equal(
            "Cais Forte Operações Portuárias pede 1 terno da estiva, 3 vagas, para o navio Atlântico Sul (IMO 9876543), no Terminal Cais Leste, berço 4, em 05/11/2026, das 13h às 19h.",
            requisicao.Descricao);
        Assert.Equal(["Sinaleiro", "Estivador"], requisicao.Ternos.Single().Vagas.Select(vagas => vagas.Funcao));
    }

    [Fact]
    public async Task O_operador_nao_enxerga_a_requisicao_de_outro()
    {
        Relogio.Acertar(Em(4, 10));
        var id = (await Operador().Enviar(HttpMethod.Post, "/api/requisicoes", DaTarde, chave: "a").LerComSucesso<RequisicaoResposta>()).Id;

        var deOutro = await Operador("11.444.777/0001-61", "Porto Seco Operações").Ler($"/api/requisicoes/{id}");

        Assert.Equal(HttpStatusCode.NotFound, deOutro.StatusCode);
        Assert.Equal($"Não existe a requisição {id}.", (await deOutro.Problema()).Detail);
    }

    [Fact]
    public async Task O_cancelamento_e_com_a_versao_lida_e_antes_do_periodo()
    {
        Relogio.Acertar(Em(4, 10));
        var operador = Operador();
        var criada = await operador.Enviar(HttpMethod.Post, "/api/requisicoes", DaTarde, chave: "a").ComSucesso();
        var id = (await criada.Ler<RequisicaoResposta>()).Id;

        Relogio.Acertar(Em(5, 13));
        var tarde = await operador.Enviar(HttpMethod.Post, $"/api/requisicoes/{id}/cancelamento", new CancelamentoDaRequisicao("O navio atrasou"), criada.Versao());
        Relogio.Acertar(Em(5, 8));
        var cancelada = await operador.Enviar(HttpMethod.Post, $"/api/requisicoes/{id}/cancelamento", new CancelamentoDaRequisicao("O navio atrasou"), criada.Versao()).ComSucesso();

        Assert.Equal(HttpStatusCode.UnprocessableEntity, tarde.StatusCode);
        Assert.Equal("O período de 05/11/2026, das 13h às 19h, já começou: a requisição não pode mais ser cancelada.", (await tarde.Problema()).Detail);
        Assert.Equal(2, cancelada.Versao());
        Assert.Equal((SituacaoDaRequisicao.Cancelada, "O navio atrasou"), await cancelada.Ler<RequisicaoResposta>() is var lida ? (lida.Situacao, lida.MotivoDoCancelamento) : default);
    }

    [Fact]
    public async Task A_requisicao_depois_de_o_periodo_comecar_e_recusada_pelo_dominio()
    {
        Relogio.Acertar(Em(5, 13, 30));

        var resposta = await Operador().Enviar(HttpMethod.Post, "/api/requisicoes", DaTarde, chave: "a");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
        Assert.Equal("A requisição tem de chegar antes do período, e o de 05/11/2026, das 13h às 19h, já começou.", (await resposta.Problema()).Detail);
    }

    [Fact]
    public async Task O_operador_sem_um_cnpj_valido_no_token_nao_pede_nada()
    {
        var resposta = await Operador(cnpj: "11.222.333/0001-82").Enviar(HttpMethod.Post, "/api/requisicoes", DaTarde, chave: "a");

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task O_escalador_ve_as_requisicoes_do_periodo_de_todos_os_operadores()
    {
        Relogio.Acertar(Em(4, 10));
        await Operador().Enviar(HttpMethod.Post, "/api/requisicoes", DaTarde, chave: "a").ComSucesso();
        await Operador("11.444.777/0001-61", "Porto Seco Operações").Enviar(HttpMethod.Post, "/api/requisicoes", DaTarde, chave: "a").ComSucesso();
        await Operador().Enviar(HttpMethod.Post, "/api/requisicoes", Requisicao(5, Turno.Noite, new VagasPedidas("Estivador", 1)), chave: "b").ComSucesso();

        var daTarde = await Escalador().Ler("/api/escalacoes/Estiva/2026-11-05/Tarde/requisicoes").LerComSucesso<IReadOnlyList<RequisicaoResposta>>();

        Assert.Equal(["Cais Forte Operações Portuárias", "Porto Seco Operações"], daTarde.Select(requisicao => requisicao.Operador.Nome).Order());
    }
}
