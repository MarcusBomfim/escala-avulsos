using System.Net;
using System.Security.Claims;
using EscalaAvulsos.Api.Seguranca;
using EscalaAvulsos.Infraestrutura.Testes.Persistencia;
using Microsoft.IdentityModel.Tokens;

namespace EscalaAvulsos.Api.Testes;

/*
 * Quem não traz um token válido não passa da porta, e não fica sabendo o
 * que existe do lado de dentro (ADR-0014).
 */
public sealed class AutenticacaoTests(BancoDeTestes bancos) : TestesDaApi(bancos)
{
    private static readonly Claim[] DoTrabalhador = [new(Reivindicacoes.Papel, Papeis.Trabalhador), new(Reivindicacoes.Matricula, "4127")];

    [Fact]
    public async Task Sem_token_e_401_em_portugues()
    {
        var resposta = await Anonimo().Ler("/api/eu");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
        var problema = await resposta.Problema();
        Assert.Equal("É preciso entrar com a matrícula ou com o usuário.", problema.Title);
        Assert.EndsWith("#nao-autenticado", problema.Type, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Sem_token_nem_o_endereco_que_nao_existe_se_revela()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await Anonimo().Ler("/api/nada-aqui")).StatusCode);
    }

    public static TheoryData<string, string> TokensQueNaoServem => new()
    {
        { "assinado com outra chave", Tokens.Emitir(DoTrabalhador, chave: new SymmetricSecurityKey(new byte[32])) },
        { "de outro emissor", Tokens.Emitir(DoTrabalhador, emissor: "https://outro.emissor") },
        { "para outra audiência", Tokens.Emitir(DoTrabalhador, audiencia: "outro-sistema") },
        { "vencido", Tokens.Emitir(DoTrabalhador, venceEm: DateTime.UtcNow.AddMinutes(-10)) },
    };

    [Theory]
    [MemberData(nameof(TokensQueNaoServem))]
    public async Task O_token_que_nao_confere_e_401(string porque, string token)
    {
        Assert.True(HttpStatusCode.Unauthorized == (await ComToken(token).Ler("/api/eu")).StatusCode, porque);
    }

    [Fact]
    public async Task O_documento_OpenAPI_se_le_sem_token()
    {
        var resposta = await Anonimo().Ler("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }
}
