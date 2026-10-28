using EscalaAvulsos.Dominio.Comum;
using EscalaAvulsos.Dominio.Escalacoes;
using EscalaAvulsos.Dominio.Periodos;
using EscalaAvulsos.Dominio.Testes.Escalacoes;
using EscalaAvulsos.Dominio.Testes.Trabalhadores;
using EscalaAvulsos.Dominio.Trabalhadores;
using EscalaAvulsos.Infraestrutura.Persistencia;
using EscalaAvulsos.Infraestrutura.Testes.Documentos;
using static EscalaAvulsos.Dominio.Testes.Escalacoes.EscalacoesDeTeste;

namespace EscalaAvulsos.Infraestrutura.Testes.Persistencia;

public sealed class RepositorioDeEscalacoesTests(BancoDeTestes bancos) : TestesComBanco(bancos)
{
    [Fact]
    public async Task A_escalacao_e_achada_pela_categoria_e_pelo_periodo()
    {
        await using var sessao = Banco.NovaSessao();
        var tarde = new TardeDeQuinta();
        await sessao.Escalacoes.Adicionar(tarde.Escalacao, Cancelamento);

        var achada = await sessao.Escalacoes.Da(Categoria.Estiva, QuintaATarde, Cancelamento);

        Assert.Equal(Exemplos.Json(tarde.Escalacao), Exemplos.Json(achada!.Agregado));
        Assert.Null(await sessao.Escalacoes.Da(Categoria.Capatazia, QuintaATarde, Cancelamento));
        Assert.Null(await sessao.Escalacoes.Da(Categoria.Estiva, QuintaATarde.Seguinte, Cancelamento));
    }

    [Fact]
    public async Task Cada_categoria_tem_uma_escalacao_por_periodo()
    {
        await using var sessao = Banco.NovaSessao();
        await sessao.Escalacoes.Adicionar(DaEstiva(), Cancelamento);
        await sessao.Escalacoes.Adicionar(new Escalacao(Categoria.Capatazia, QuintaATarde, CalendarioDaEscalacao.Padrao), Cancelamento);

        var erro = await Assert.ThrowsAsync<AgregadoJaExiste>(() => sessao.Escalacoes.Adicionar(DaEstiva(), Cancelamento));

        Assert.Equal(
            "A escala da estiva de 05/11/2026, das 13h às 19h, já está aberta: cada categoria tem uma escalação por período.",
            erro.Message);
    }

    [Fact]
    public async Task A_habilitacao_grava_a_versao_seguinte()
    {
        await using var sessao = Banco.NovaSessao();
        var antonio = TrabalhadoresDeTeste.Estivador();
        await sessao.Escalacoes.Adicionar(DaEstiva(), Cancelamento);

        var lida = (await sessao.Escalacoes.Da(Categoria.Estiva, QuintaATarde, Cancelamento))!;
        lida.Agregado.Habilitar(antonio, [Funcoes.Estivador], QuintaAs6h);
        await sessao.Escalacoes.Atualizar(lida.Agregado, lida.Versao, Cancelamento);

        var relida = (await sessao.Escalacoes.Obter(lida.Agregado.Id, Cancelamento))!;
        Assert.Equal(2, relida.Versao);
        Assert.Equal([antonio.Id], relida.Agregado.Habilitacoes.Select(habilitacao => habilitacao.Trabalhador));

        // A coluna dos habilitados anda com o documento: a escalação passa a ser do mês do Antônio.
        Assert.Single(await sessao.Escalacoes.DoTrabalhadorNoMes(antonio.Id, Mes.De(2026, 11), Cancelamento));
    }

    [Fact]
    public async Task As_escalacoes_do_mes_sao_as_em_que_o_trabalhador_se_habilitou_na_ordem_dos_periodos()
    {
        await using var sessao = Banco.NovaSessao();
        var antonio = TrabalhadoresDeTeste.Estivador();
        antonio.RegistrarAso(new Aso(new DateOnly(2026, 11, 20), new DateOnly(2027, 11, 19)));
        var benedito = TrabalhadoresDeTeste.Estivador(4130, "Benedito Lima");

        var dia30 = Habilitada(Periodo.De(new DateOnly(2026, 11, 30), Turno.Noite), antonio);
        var dia2 = Habilitada(Periodo.De(new DateOnly(2026, 11, 2), Turno.Madrugada), antonio, benedito);
        var semEle = Habilitada(Periodo.De(new DateOnly(2026, 11, 10), Turno.Manha), benedito);
        var dezembro = Habilitada(Periodo.De(new DateOnly(2026, 12, 1), Turno.Madrugada), antonio);
        var outubro = Habilitada(Periodo.De(new DateOnly(2026, 10, 31), Turno.Noite), antonio);

        foreach (var escalacao in (Escalacao[])[dia30, dia2, semEle, dezembro, outubro])
        {
            await sessao.Escalacoes.Adicionar(escalacao, Cancelamento);
        }

        var doMes = await sessao.Escalacoes.DoTrabalhadorNoMes(antonio.Id, Mes.De(2026, 11), Cancelamento);

        Assert.Equal([dia2.Id, dia30.Id], doMes.Select(escalacao => escalacao.Id));
    }

    private static Escalacao Habilitada(Periodo periodo, params Trabalhador[] trabalhadores)
    {
        var escalacao = new Escalacao(Categoria.Estiva, periodo, CalendarioDaEscalacao.Padrao);

        foreach (var trabalhador in trabalhadores)
        {
            escalacao.Habilitar(trabalhador, [Funcoes.Estivador], escalacao.AberturaDaHabilitacao);
        }

        return escalacao;
    }
}
