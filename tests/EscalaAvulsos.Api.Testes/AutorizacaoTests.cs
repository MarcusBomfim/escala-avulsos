using System.Net;
using System.Security.Claims;
using EscalaAvulsos.Api.Seguranca;
using EscalaAvulsos.Infraestrutura.Testes.Persistencia;

namespace EscalaAvulsos.Api.Testes;

/*
 * Cada papel no que é dele (ADR-0014): o trabalhador no cadastro e na escala
 * dele, pela matrícula do token; o escalador no órgão gestor; o operador nas
 * requisições do CNPJ dele.
 */
public sealed class AutorizacaoTests(BancoDeTestes bancos) : TestesDaApi(bancos)
{
    [Fact]
    public async Task O_trabalhador_nao_mexe_no_cadastro_dos_outros()
    {
        var resposta = await Trabalhador(4127).Ler("/api/trabalhadores/4130");

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
        Assert.Equal("Sem permissão para este pedido.", (await resposta.Problema()).Title);
    }

    [Fact]
    public async Task O_escalador_e_o_operador_nao_tem_um_eu_de_trabalhador()
    {
        Assert.Equal(HttpStatusCode.Forbidden, (await Escalador().Ler("/api/eu")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Operador().Ler("/api/eu")).StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("abc")]
    [InlineData("0")]
    public async Task O_trabalhador_sem_uma_matricula_valida_no_token_nao_entra(string? matricula)
    {
        Claim[] reivindicacoes = matricula is null
            ? [new(Reivindicacoes.Papel, Papeis.Trabalhador)]
            : [new(Reivindicacoes.Papel, Papeis.Trabalhador), new(Reivindicacoes.Matricula, matricula)];

        Assert.Equal(HttpStatusCode.Forbidden, (await ComToken(Tokens.Emitir(reivindicacoes)).Ler("/api/eu")).StatusCode);
    }

    [Fact]
    public async Task Quem_nao_tem_um_dos_papeis_nao_passa_nem_onde_a_rota_nao_existe()
    {
        var visitante = ComToken(Tokens.Emitir([new(Reivindicacoes.Papel, "visitante")]));

        Assert.Equal(HttpStatusCode.Forbidden, (await visitante.Ler("/api/eu")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await visitante.Ler("/api/nada-aqui")).StatusCode);
    }

    [Fact]
    public async Task Com_papel_a_rota_que_nao_existe_e_404()
    {
        var resposta = await Escalador().Ler("/api/nada-aqui");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
        Assert.Equal("Não encontrado.", (await resposta.Problema()).Title);
    }
}
