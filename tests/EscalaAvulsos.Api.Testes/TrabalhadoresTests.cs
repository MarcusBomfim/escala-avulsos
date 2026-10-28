using System.Net;
using EscalaAvulsos.Api.Recursos.Trabalhadores;
using EscalaAvulsos.Dominio.Trabalhadores;
using EscalaAvulsos.Infraestrutura.Testes.Persistencia;

namespace EscalaAvulsos.Api.Testes;

/* O cadastro pelo órgão gestor, sempre pela matrícula, e cada alteração com a versão lida. */
public sealed class TrabalhadoresTests(BancoDeTestes bancos) : TestesDaApi(bancos)
{
    [Fact]
    public async Task O_trabalhador_cadastrado_fica_no_endereco_da_matricula_na_versao_1()
    {
        var resposta = await Escalador()
            .Enviar(HttpMethod.Post, "/api/trabalhadores", new NovoTrabalhador(4127, "  Antônio Ferreira ", "52998412726", Categoria.Estiva, Inscricao.Cadastrado))
            .ComSucesso();

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        Assert.Equal("/api/trabalhadores/4127", resposta.Headers.Location?.ToString());
        Assert.Equal(1, resposta.Versao());
        var antonio = await resposta.Ler<TrabalhadorResposta>();
        Assert.Equal((4127, "Antônio Ferreira", "529.984.127-26", Inscricao.Cadastrado), (antonio.Matricula, antonio.Nome, antonio.Cpf, antonio.Inscricao));
    }

    [Fact]
    public async Task O_cadastro_anda_versao_a_versao_e_a_leitura_devolve_a_ultima()
    {
        var escalador = Escalador();
        var versao = await escalador.CadastrarEstivador(4127, "Antônio Ferreira", Inscricao.Cadastrado, "Sinaleiro");

        versao = (await escalador.Enviar(HttpMethod.Post, "/api/trabalhadores/4127/impedimentos", new NovoImpedimento(TipoDeImpedimento.Afastamento, new DateOnly(2026, 11, 10)), versao).ComSucesso()).Versao();
        versao = (await escalador.Enviar(HttpMethod.Post, "/api/trabalhadores/4127/fim-do-afastamento", new FimDoAfastamento(new DateOnly(2026, 11, 20)), versao).ComSucesso()).Versao();
        versao = (await escalador.Enviar(HttpMethod.Post, "/api/trabalhadores/4127/registro", versao: versao).ComSucesso()).Versao();
        var lido = await escalador.Ler("/api/trabalhadores/4127").ComSucesso();

        Assert.Equal(versao, lido.Versao());
        var antonio = await lido.Ler<TrabalhadorResposta>();
        Assert.Equal(Inscricao.Registrado, antonio.Inscricao);
        Assert.Equal(["Estivador", "Sinaleiro"], antonio.Qualificacoes.Select(qualificacao => qualificacao.Funcao));
        Assert.Equal(new AsoResposta(new DateOnly(2025, 12, 1), new DateOnly(2026, 11, 30)), antonio.Aso);
        Assert.Equal("afastamento de 10/11/2026 a 19/11/2026", Assert.Single(antonio.Impedimentos).Descricao);
    }

    [Fact]
    public async Task O_cancelamento_congela_o_cadastro()
    {
        var escalador = Escalador();
        var versao = await escalador.CadastrarEstivador(4127, "Antônio Ferreira");
        versao = (await escalador.Enviar(HttpMethod.Post, "/api/trabalhadores/4127/cancelamento", new NovoCancelamento(new DateOnly(2026, 12, 1), "Aposentadoria"), versao).ComSucesso()).Versao();

        var resposta = await escalador.Enviar(HttpMethod.Put, "/api/trabalhadores/4127/aso", new NovoAso(new DateOnly(2026, 11, 20), new DateOnly(2027, 11, 19)), versao);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
        Assert.Equal(
            "A inscrição de Antônio Ferreira (matrícula 4127) foi cancelada a partir de 01/12/2026: nada mais muda nela.",
            (await resposta.Problema()).Detail);
    }

    [Fact]
    public async Task A_qualificacao_e_numa_funcao_da_categoria_do_trabalhador()
    {
        var escalador = Escalador();
        var versao = await escalador.CadastrarEstivador(4127, "Antônio Ferreira");

        var resposta = await escalador.Enviar(HttpMethod.Post, "/api/trabalhadores/4127/qualificacoes", new NovaQualificacao("sinaleiro", new DateOnly(2010, 5, 10), new DateOnly(2009, 5, 9)), versao);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
        Assert.Equal("A qualificação de sinaleiro não pode valer só até 09/05/2009, antes de começar, em 10/05/2010.", (await resposta.Problema()).Detail);
    }

    [Fact]
    public async Task Quem_ja_tem_a_versao_recebe_304()
    {
        var escalador = Escalador();
        var versao = await escalador.CadastrarEstivador(4127, "Antônio Ferreira");
        using var pedido = new HttpRequestMessage(HttpMethod.Get, "/api/trabalhadores/4127");
        pedido.Headers.TryAddWithoutValidation("If-None-Match", $"\"{versao}\"");

        var resposta = await escalador.SendAsync(pedido, Cancelamento);

        Assert.Equal(HttpStatusCode.NotModified, resposta.StatusCode);
    }

    [Fact]
    public async Task A_matricula_fora_do_cadastro_e_404()
    {
        var resposta = await Escalador().Ler("/api/trabalhadores/4127");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
        Assert.Equal("A matrícula 4127 não está no cadastro.", (await resposta.Problema()).Detail);
    }
}
