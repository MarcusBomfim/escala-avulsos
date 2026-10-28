using EscalaAvulsos.Dominio.Periodos;
using EscalaAvulsos.Dominio.Requisicoes;
using EscalaAvulsos.Dominio.Testes.Escalacoes;
using EscalaAvulsos.Dominio.Testes.Trabalhadores;
using EscalaAvulsos.Dominio.Trabalhadores;
using static EscalaAvulsos.Dominio.Testes.Escalacoes.EscalacoesDeTeste;
using static EscalaAvulsos.Dominio.Testes.Requisicoes.RequisicoesDeTeste;

namespace EscalaAvulsos.Infraestrutura.Testes.Persistencia;

public sealed class RepositorioDeRequisicoesTests(BancoDeTestes bancos) : TestesComBanco(bancos)
{
    [Fact]
    public async Task A_requisicao_volta_inteira_e_o_cancelamento_grava_a_versao_seguinte()
    {
        await using var sessao = Banco.NovaSessao();
        var requisicao = DoisTernos();
        await sessao.Requisicoes.Adicionar(requisicao, Cancelamento);

        var lida = (await sessao.Requisicoes.Obter(requisicao.Id, Cancelamento))!;
        lida.Agregado.Cancelar("O navio atrasou", QuartaAs10h.AddHours(2));
        await sessao.Requisicoes.Atualizar(lida.Agregado, lida.Versao, Cancelamento);

        var cancelada = (await sessao.Requisicoes.Obter(requisicao.Id, Cancelamento))!;
        Assert.Equal(2, cancelada.Versao);
        Assert.Equal((SituacaoDaRequisicao.Cancelada, "O navio atrasou"), (cancelada.Agregado.Situacao, cancelada.Agregado.MotivoDoCancelamento));
    }

    [Fact]
    public async Task As_requisicoes_do_periodo_sao_so_as_da_categoria_e_do_periodo_inclusive_as_canceladas()
    {
        await using var sessao = Banco.NovaSessao();
        var daTarde = new Requisicao(TardeDeQuinta.DaTarde, [[new(Funcoes.Estivador, 2)]], QuartaAs10h);
        var daManha = DoisTernos();
        var daConferencia = new Requisicao(TardeDeQuinta.DaTarde, [[new(Funcoes.Conferente, 1)]], QuartaAs10h);
        var outraDaTarde = new Requisicao(TardeDeQuinta.DaTarde, [[new(Funcoes.Sinaleiro, 1)]], QuartaAs10h);
        outraDaTarde.Cancelar("O navio atrasou", Em(5, 8));

        foreach (var requisicao in (Requisicao[])[daTarde, outraDaTarde, daManha, daConferencia])
        {
            await sessao.Requisicoes.Adicionar(requisicao, Cancelamento);
        }

        var doPeriodo = await sessao.Requisicoes.DoPeriodo(Categoria.Estiva, QuintaATarde, Cancelamento);

        Assert.Equal([daTarde.Id, outraDaTarde.Id], doPeriodo.Select(requisicao => requisicao.Id));
        Assert.Empty(await sessao.Requisicoes.DoPeriodo(Categoria.Estiva, Periodo.De(new DateOnly(2026, 11, 5), Turno.Noite), Cancelamento));
    }
}
