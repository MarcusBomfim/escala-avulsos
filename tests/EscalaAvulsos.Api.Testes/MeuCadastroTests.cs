using System.Net;
using EscalaAvulsos.Api.Recursos.Trabalhadores;
using EscalaAvulsos.Infraestrutura.Testes.Persistencia;

namespace EscalaAvulsos.Api.Testes;

/* O trabalhador entra pela matrícula, e vê o cadastro dele — só o dele. */
public sealed class MeuCadastroTests(BancoDeTestes bancos) : TestesDaApi(bancos)
{
    [Fact]
    public async Task O_trabalhador_ve_o_proprio_cadastro_pela_matricula_do_token()
    {
        var escalador = Escalador();
        await escalador.CadastrarEstivador(4127, "Antônio Ferreira");
        var versao = await escalador.CadastrarEstivador(4130, "Benedito Lima");

        var resposta = await Trabalhador(4130).Ler("/api/eu").ComSucesso();

        Assert.Equal(versao, resposta.Versao());
        Assert.Equal((4130, "Benedito Lima"), await resposta.Ler<TrabalhadorResposta>() is var benedito ? (benedito.Matricula, benedito.Nome) : default);
    }

    [Fact]
    public async Task A_matricula_que_nao_esta_no_cadastro_e_404_com_o_motivo()
    {
        var resposta = await Trabalhador(4127).Ler("/api/eu");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
        Assert.Equal("A matrícula 4127 não está no cadastro.", (await resposta.Problema()).Detail);
    }
}
