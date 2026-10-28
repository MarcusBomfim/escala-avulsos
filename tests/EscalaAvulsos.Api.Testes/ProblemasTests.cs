using System.Net;
using EscalaAvulsos.Api.Recursos.Trabalhadores;
using EscalaAvulsos.Dominio.Trabalhadores;
using EscalaAvulsos.Infraestrutura.Testes.Persistencia;

namespace EscalaAvulsos.Api.Testes;

/*
 * Todo erro sai em ProblemDetails, do catálogo (ADR-0014): o status, o tipo
 * que aponta para a explicação, o título fixo, e o detalhe com o que houve —
 * a frase do domínio, quando é ele que recusa.
 */
public sealed class ProblemasTests(BancoDeTestes bancos) : TestesDaApi(bancos)
{
    private const string Antonio = """{ "matricula": 4127, "nome": "Antônio Ferreira", "cpf": "529.984.127-26", "categoria": "Estiva", "inscricao": "Registrado" }""";

    [Fact]
    public async Task A_regra_do_dominio_e_422_com_a_frase_dele()
    {
        var resposta = await Escalador().Enviar(HttpMethod.Post, "/api/trabalhadores", new NovoTrabalhador(4127, "Antônio Ferreira", "529.984.127-25", Categoria.Estiva, Inscricao.Registrado));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
        var problema = await resposta.Problema();
        Assert.Equal("O CPF 529.984.127-25 não confere: os dígitos verificadores não batem com o número.", problema.Detail);
        Assert.Equal("O pedido fere uma regra do trabalho portuário.", problema.Title);
        Assert.Equal("POST /api/trabalhadores", problema.Instance);
        Assert.True(problema.Extensions.ContainsKey("traceId"));
    }

    [Theory]
    [InlineData("{ \"matricula\": 4127", "O corpo não é o JSON esperado")]
    [InlineData("""{ "matricula": 4127, "nome": "Antônio Ferreira", "cpf": "529.984.127-26", "categoria": "Estiva", "inscricao": "Registrado", "apelido": "Toninho" }""", "apelido")]
    [InlineData("""{ "matricula": 4127, "nome": "Antônio Ferreira", "cpf": "529.984.127-26", "categoria": 1, "inscricao": "Registrado" }""", "O corpo não é o JSON esperado")]
    [InlineData("""{ "matricula": "4127", "nome": "Antônio Ferreira", "cpf": "529.984.127-26", "categoria": "Estiva", "inscricao": "Registrado" }""", "O corpo não é o JSON esperado")]
    [InlineData("""{ "matricula": 4127, "nome": null, "cpf": "529.984.127-26", "categoria": "Estiva", "inscricao": "Registrado" }""", "O corpo não é o JSON esperado")]
    public async Task O_corpo_fora_do_contrato_e_400_dizendo_o_que(string corpo, string trecho)
    {
        var resposta = await Escalador().Enviar(HttpMethod.Post, "/api/trabalhadores", corpo);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Contains(trecho, (await resposta.Problema()).Detail, StringComparison.Ordinal);
    }

    [Fact]
    public async Task O_corpo_que_nao_e_json_e_415()
    {
        using var conteudo = new StringContent(Antonio, System.Text.Encoding.UTF8, "text/plain");
        var resposta = await Escalador().PostAsync("/api/trabalhadores", conteudo, Cancelamento);

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, resposta.StatusCode);
        Assert.Equal("O corpo do pedido precisa ser JSON.", (await resposta.Problema()).Title);
    }

    [Fact]
    public async Task O_metodo_que_o_endereco_nao_aceita_e_405()
    {
        var resposta = await Escalador().Enviar(HttpMethod.Delete, "/api/trabalhadores/4127");

        Assert.Equal(HttpStatusCode.MethodNotAllowed, resposta.StatusCode);
        Assert.Equal("Este endereço não aceita este método.", (await resposta.Problema()).Title);
    }

    [Fact]
    public async Task A_matricula_repetida_e_409()
    {
        var escalador = Escalador();
        await escalador.Enviar(HttpMethod.Post, "/api/trabalhadores", Antonio).ComSucesso();

        // Outra pessoa, outro CPF, a mesma matrícula.
        var resposta = await escalador.Enviar(HttpMethod.Post, "/api/trabalhadores", new NovoTrabalhador(4127, "Outro Antônio", "111.444.777-35", Categoria.Estiva, Inscricao.Registrado));

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        Assert.Equal("Já existe trabalhador com a matrícula 4127.", (await resposta.Problema()).Detail);
    }

    [Fact]
    public async Task A_alteracao_sem_a_versao_lida_e_428_e_com_a_versao_malformada_e_400()
    {
        var escalador = Escalador();
        await escalador.CadastrarEstivador(4127, "Antônio Ferreira");

        var semVersao = await escalador.Enviar(HttpMethod.Post, "/api/trabalhadores/4127/registro");
        using var qualquer = new HttpRequestMessage(HttpMethod.Post, "/api/trabalhadores/4127/registro");
        qualquer.Headers.TryAddWithoutValidation("If-Match", "*");
        var comQualquerVersao = await escalador.SendAsync(qualquer, Cancelamento);
        using var malformado = new HttpRequestMessage(HttpMethod.Post, "/api/trabalhadores/4127/registro");
        malformado.Headers.TryAddWithoutValidation("If-Match", "W/\"3\"");
        var comVersaoFraca = await escalador.SendAsync(malformado, Cancelamento);

        Assert.Equal(HttpStatusCode.PreconditionRequired, semVersao.StatusCode);
        Assert.Equal("Falta a versão lida, no cabeçalho If-Match.", (await semVersao.Problema()).Title);

        // "*" é "qualquer versão" — o mesmo que não dizer qual leu.
        Assert.Equal(HttpStatusCode.PreconditionRequired, comQualquerVersao.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, comVersaoFraca.StatusCode);
    }

    [Fact]
    public async Task A_alteracao_a_partir_de_uma_versao_velha_e_412_com_a_versao_atual()
    {
        var escalador = Escalador();
        var versao = await escalador.CadastrarEstivador(4127, "Antônio Ferreira", Inscricao.Cadastrado);

        var resposta = await escalador.Enviar(HttpMethod.Post, "/api/trabalhadores/4127/registro", versao: versao - 1);

        Assert.Equal(HttpStatusCode.PreconditionFailed, resposta.StatusCode);
        var problema = await resposta.Problema();
        Assert.Equal($"O trabalhador da matrícula 4127 mudou desde a versão {versao - 1}: está na versão {versao}. Leia de novo e refaça a alteração.", problema.Detail);
        Assert.Equal(versao, ((System.Text.Json.JsonElement)problema.Extensions["versaoAtual"]!).GetInt32());
    }
}
