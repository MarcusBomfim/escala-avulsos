using System.Net;
using EscalaAvulsos.Api.Recursos.Escalacoes;
using EscalaAvulsos.Infraestrutura.Testes.Persistencia;
using static EscalaAvulsos.Api.Testes.Novembro;

namespace EscalaAvulsos.Api.Testes;

/*
 * O trabalhador se habilita pelo celular, na categoria dele, sem versão e
 * sem "tente de novo": se outro se habilitou no meio, o servidor relê e
 * refaz (ADR-0015). A janela, as funções e a aptidão são do domínio.
 */
public sealed class HabilitacaoTests(BancoDeTestes bancos) : TestesDaApi(bancos)
{
    private const string DaTarde = "/api/eu/habilitacoes/2026-11-05/Tarde";

    [Fact]
    public async Task Antes_de_se_habilitar_o_trabalhador_ve_a_janela()
    {
        await Preparar(4127);
        Relogio.Acertar(Em(5, 0, 30));

        var minha = await Trabalhador(4127).Ler(DaTarde).LerComSucesso<MinhaHabilitacaoResposta>();

        Assert.Equal((Em(5, 1), Em(5, 10), false, false), (minha.AbreEm, minha.FechaEm, minha.JanelaAberta, minha.Habilitado));
        Assert.Empty(minha.Funcoes);
        Assert.Equal([("Estivador", true)], minha.Opcoes.Select(opcao => (opcao.Funcao, opcao.Pode)));
    }

    [Fact]
    public async Task O_trabalhador_se_habilita_troca_as_funcoes_e_desiste()
    {
        await Preparar(4127, "Sinaleiro");
        var antonio = Trabalhador(4127);
        Relogio.Acertar(Em(5, 6));

        var habilitado = await antonio.Enviar(HttpMethod.Put, DaTarde, new PedidoDeHabilitacao(["Sinaleiro", "estivador"])).LerComSucesso<MinhaHabilitacaoResposta>();
        var trocado = await antonio.Enviar(HttpMethod.Put, DaTarde, new PedidoDeHabilitacao(["Estivador"])).LerComSucesso<MinhaHabilitacaoResposta>();
        var desistiu = await antonio.Enviar(HttpMethod.Delete, DaTarde).LerComSucesso<MinhaHabilitacaoResposta>();

        Assert.Equal((true, true, Em(5, 6)), (habilitado.JanelaAberta, habilitado.Habilitado, habilitado.Em!.Value));
        Assert.Equal(["Sinaleiro", "Estivador"], habilitado.Funcoes);
        Assert.Equal(["Estivador"], trocado.Funcoes);
        Assert.False(desistiu.Habilitado);
    }

    [Fact]
    public async Task A_lista_traz_as_escalacoes_da_categoria_dele_que_nao_comecaram_com_as_funcoes_que_ele_pode_escolher()
    {
        // A tarde de quinta, e a manhã de 1º de dezembro, quando o exame do Antônio já venceu; a capatazia não é a categoria dele.
        await Preparar(4127, "Sinaleiro");
        var escalador = Escalador();
        await escalador.Enviar(HttpMethod.Put, "/api/escalacoes/Estiva/2026-12-01/Manha").ComSucesso();
        await escalador.Enviar(HttpMethod.Put, "/api/escalacoes/Capatazia/2026-11-05/Noite").ComSucesso();
        Relogio.Acertar(Em(5, 6));

        var lista = await Trabalhador(4127).Ler("/api/eu/habilitacoes").LerComSucesso<IReadOnlyList<MinhaHabilitacaoResposta>>();
        Relogio.Acertar(Em(5, 13));
        var depoisDaTarde = await Trabalhador(4127).Ler("/api/eu/habilitacoes").LerComSucesso<IReadOnlyList<MinhaHabilitacaoResposta>>();

        Assert.Equal([new DateOnly(2026, 11, 5), new DateOnly(2026, 12, 1)], lista.Select(escalacao => escalacao.Periodo.Dia));
        var (tarde, dezembro) = (lista[0], lista[1]);
        Assert.Equal((true, false), (tarde.JanelaAberta, dezembro.JanelaAberta));
        Assert.Equal([("Estivador", true), ("Sinaleiro", true)], tarde.Opcoes.Select(opcao => (opcao.Funcao, opcao.Pode)));
        Assert.All(dezembro.Opcoes, opcao => Assert.Equal((false, "o exame médico (ASO) vale só até 30/11/2026"), (opcao.Pode, Assert.Single(opcao.Motivos))));
        Assert.Equal([new DateOnly(2026, 12, 1)], depoisDaTarde.Select(escalacao => escalacao.Periodo.Dia));
    }

    [Fact]
    public async Task O_escalador_ve_os_habilitados_na_ordem_das_matriculas()
    {
        await Preparar(4130);
        await Escalador().CadastrarEstivador(4127, "Antônio Ferreira");
        Relogio.Acertar(Em(5, 6));
        await Trabalhador(4130).Enviar(HttpMethod.Put, DaTarde, new PedidoDeHabilitacao(["Estivador"])).ComSucesso();
        await Trabalhador(4127).Enviar(HttpMethod.Put, DaTarde, new PedidoDeHabilitacao(["Estivador"])).ComSucesso();

        var escalacao = await Escalador().Ler("/api/escalacoes/Estiva/2026-11-05/Tarde").LerComSucesso<EscalacaoResposta>();

        Assert.Equal([4127, 4130], escalacao.Habilitacoes.Select(habilitacao => habilitacao.Matricula));
    }

    [Fact]
    public async Task Fora_da_janela_e_numa_funcao_sem_qualificacao_o_dominio_recusa_com_o_motivo()
    {
        await Preparar(4127);
        var antonio = Trabalhador(4127);

        Relogio.Acertar(Em(5, 10));
        var fechada = await antonio.Enviar(HttpMethod.Put, DaTarde, new PedidoDeHabilitacao(["Estivador"]));
        Relogio.Acertar(Em(5, 6));
        var semQualificacao = await antonio.Enviar(HttpMethod.Put, DaTarde, new PedidoDeHabilitacao(["Sinaleiro"]));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, fechada.StatusCode);
        Assert.Equal("A habilitação para a escala da estiva de 05/11/2026, das 13h às 19h, fechou em 05/11/2026, às 10h.", (await fechada.Problema()).Detail);
        Assert.Equal(
            "Antônio Ferreira (matrícula 4127) não pode se habilitar como sinaleiro: não tem a qualificação de sinaleiro.",
            (await semQualificacao.Problema()).Detail);
    }

    [Fact]
    public async Task Sem_a_escalacao_aberta_e_404()
    {
        await Escalador().CadastrarEstivador(4127, "Antônio Ferreira");

        var resposta = await Trabalhador(4127).Ler(DaTarde);

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
        Assert.Equal("A escala da estiva de 05/11/2026, das 13h às 19h, ainda não foi aberta.", (await resposta.Problema()).Detail);
    }

    [Fact]
    public async Task Na_abertura_da_janela_vinte_trabalhadores_se_habilitam_juntos_e_ninguem_precisa_tentar_de_novo()
    {
        /*
         * Cada um no seu celular, no mesmo instante. Cada um recebe a resposta
         * de primeira — o servidor relê e refaz quando perde a corrida —, e no
         * fim a escalação tem os vinte, na ordem das matrículas.
         */
        var escalador = Escalador();
        await escalador.Enviar(HttpMethod.Put, "/api/escalacoes/Estiva/2026-11-05/Tarde").ComSucesso();
        var matriculas = Enumerable.Range(5001, 20).ToList();

        foreach (var matricula in matriculas)
        {
            await escalador.CadastrarEstivador(matricula, $"Estivador {matricula}");
        }

        Relogio.Acertar(Em(5, 1));
        var respostas = await Task.WhenAll(matriculas.Select(matricula =>
            Trabalhador(matricula).Enviar(HttpMethod.Put, DaTarde, new PedidoDeHabilitacao(["Estivador"]))));

        Assert.All(respostas, resposta => Assert.Equal(HttpStatusCode.OK, resposta.StatusCode));
        var escalacao = await escalador.Ler("/api/escalacoes/Estiva/2026-11-05/Tarde").LerComSucesso<EscalacaoResposta>();
        Assert.Equal(matriculas, escalacao.Habilitacoes.Select(habilitacao => habilitacao.Matricula));
    }

    /// <summary>O trabalhador cadastrado, e a escalação da tarde de quinta aberta.</summary>
    private async Task Preparar(int matricula, params string[] outrasFuncoes)
    {
        var escalador = Escalador();
        await escalador.CadastrarEstivador(matricula, "Antônio Ferreira", outrasFuncoes: outrasFuncoes);
        await escalador.Enviar(HttpMethod.Put, "/api/escalacoes/Estiva/2026-11-05/Tarde").ComSucesso();
    }
}
