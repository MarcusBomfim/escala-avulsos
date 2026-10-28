using EscalaAvulsos.Dominio.Escalacoes;
using EscalaAvulsos.Dominio.Periodos;
using EscalaAvulsos.Dominio.Requisicoes;
using EscalaAvulsos.Dominio.Testes.Escalacoes;
using EscalaAvulsos.Dominio.Testes.Trabalhadores;
using EscalaAvulsos.Dominio.Trabalhadores;
using EscalaAvulsos.Infraestrutura.Persistencia;
using static EscalaAvulsos.Dominio.Testes.Escalacoes.EscalacoesDeTeste;
using static EscalaAvulsos.Dominio.Testes.Requisicoes.RequisicoesDeTeste;

namespace EscalaAvulsos.Infraestrutura.Testes.Persistencia;

/*
 * Cada escala da categoria parte de onde a anterior parou (ADR-0006), e duas
 * escalas não partem do mesmo ponto: o rodízio de cada categoria tem uma
 * versão, e a escala grava com a versão de onde partiu (ADR-0013).
 */
public sealed class RodizioDaCategoriaTests(BancoDeTestes bancos) : TestesComBanco(bancos)
{
    private static readonly Periodo SextaDeManha = Periodo.De(new DateOnly(2026, 11, 6), Turno.Manha);

    private readonly TardeDeQuinta _tarde = new();

    private readonly Trabalhador _fabio = TrabalhadoresDeTeste.Estivador(4199, "Fábio Rocha");

    [Fact]
    public async Task O_rodizio_da_categoria_parte_da_escala_mais_recente()
    {
        var sexta = await GravarATardeESexta();
        await using var sessao = Banco.NovaSessao();
        Assert.Equal(0, (await sessao.Escalas().RodizioDa(Categoria.Estiva, Cancelamento)).Versao);

        var daTarde = await Banco.FazerAEscala(_tarde.Escalacao.Id, Em(5, 10));
        var depoisDaTarde = await sessao.Escalas().RodizioDa(Categoria.Estiva, Cancelamento);
        var deSexta = await Banco.FazerAEscala(sexta.Id, Em(6, 4));
        var depoisDeSexta = await sessao.Escalas().RodizioDa(Categoria.Estiva, Cancelamento);

        Assert.Equal(1, depoisDaTarde.Versao);
        Assert.Equal(daTarde.RodizioDepois.Vezes, depoisDaTarde.Agregado.Vezes);
        Assert.Equal(2, depoisDeSexta.Versao);
        Assert.Equal(deSexta.RodizioDepois.Vezes, depoisDeSexta.Agregado.Vezes);
        Assert.Equal(4199, depoisDeSexta.Agregado.UltimoEscalado(Funcoes.Estivador, Inscricao.Registrado)?.Numero);
        Assert.Equal(0, (await sessao.Escalas().RodizioDa(Categoria.Capatazia, Cancelamento)).Versao);
    }

    [Fact]
    public async Task A_escala_feita_de_um_rodizio_que_ja_andou_e_recusada_e_nada_dela_fica_gravado()
    {
        var sexta = await GravarATardeESexta();
        var lida = await LerParaEscalar(_tarde.Escalacao.Id);

        await Banco.FazerAEscala(sexta.Id, Em(6, 4));

        await using var sessao = Banco.NovaSessao();
        var escala = Escalador.Escalar(lida.Escalacao, lida.Requisicoes, lida.Habilitados, [], lida.Rodizio.Agregado, Descanso.DaLei, Em(5, 10));
        var erro = await Assert.ThrowsAsync<ConflitoDeConcorrencia>(() => sessao.Escalas().Adicionar(escala, lida.Rodizio.Versao, Cancelamento));

        Assert.Equal("O rodízio da estiva mudou desde a versão 0: está na versão 1. Leia o rodízio de novo e refaça a escala.", erro.Message);
        Assert.Equal(1, erro.VersaoAtual);
        Assert.Null(await sessao.Escalas().DaEscalacao(_tarde.Escalacao.Id, Cancelamento));
    }

    [Fact]
    public async Task Duas_escalas_da_categoria_feitas_ao_mesmo_tempo_nao_partem_do_mesmo_ponto()
    {
        // As duas leem o rodízio antes de qualquer uma gravar, e gravam ao mesmo tempo, cada uma na sua conexão.
        var sexta = await GravarATardeESexta();
        var paraATarde = await LerParaEscalar(_tarde.Escalacao.Id);
        var paraASexta = await LerParaEscalar(sexta.Id);
        var daTarde = Escalador.Escalar(paraATarde.Escalacao, paraATarde.Requisicoes, paraATarde.Habilitados, [], paraATarde.Rodizio.Agregado, Descanso.DaLei, Em(5, 10));
        var deSexta = Escalador.Escalar(paraASexta.Escalacao, paraASexta.Requisicoes, paraASexta.Habilitados, [], paraASexta.Rodizio.Agregado, Descanso.DaLei, Em(6, 4));

        var tentativas = await Task.WhenAll(Tentar(daTarde, paraATarde.Rodizio.Versao), Tentar(deSexta, paraASexta.Rodizio.Versao));

        Assert.Single(tentativas, gravou => gravou);

        // Quem perdeu refaz, a partir do rodízio que o outro deixou.
        var refeita = tentativas[0] ? await Banco.FazerAEscala(sexta.Id, Em(6, 4)) : await Banco.FazerAEscala(_tarde.Escalacao.Id, Em(5, 10));
        await using var sessao = Banco.NovaSessao();
        var rodizio = await sessao.Escalas().RodizioDa(Categoria.Estiva, Cancelamento);
        Assert.Equal(2, rodizio.Versao);
        Assert.Equal(refeita.RodizioDepois.Vezes, rodizio.Agregado.Vezes);
    }

    [Fact]
    public async Task O_substituto_anda_a_vez_e_o_rodizio_muda_de_versao_a_presenca_nao()
    {
        await GravarATardeESexta();
        var escala = await Banco.FazerAEscala(_tarde.Escalacao.Id, Em(5, 10));

        await Banco.Desistir(escala.Id, _tarde.Edson.Id, Em(5, 11));

        await using var sessao = Banco.NovaSessao();
        var depoisDaDesistencia = await sessao.Escalas().RodizioDa(Categoria.Estiva, Cancelamento);
        Assert.Equal(2, depoisDaDesistencia.Versao);
        Assert.Equal(4152, depoisDaDesistencia.Agregado.UltimoEscalado(Funcoes.Estivador, Inscricao.Registrado)?.Numero);

        var lida = (await sessao.Escalas().Obter(escala.Id, Cancelamento))!;
        lida.Agregado.ConfirmarPresenca(_tarde.Antonio.Id, Em(5, 12));
        await sessao.Escalas().Atualizar(lida.Agregado, lida.Versao, Cancelamento);

        Assert.Equal(2, (await sessao.Escalas().RodizioDa(Categoria.Estiva, Cancelamento)).Versao);
    }

    [Fact]
    public async Task Quem_leu_o_rodizio_antes_de_a_desistencia_andar_a_vez_refaz_a_escala()
    {
        // A sexta lê o rodízio na versão 1; o Edson desiste da tarde, o Carlos entra, e a vez anda: versão 2.
        var sexta = await GravarATardeESexta();
        var daTarde = await Banco.FazerAEscala(_tarde.Escalacao.Id, Em(5, 10));
        var lida = await LerParaEscalar(sexta.Id);

        await Banco.Desistir(daTarde.Id, _tarde.Edson.Id, Em(5, 11));

        await using var sessao = Banco.NovaSessao();
        var escala = Escalador.Escalar(lida.Escalacao, lida.Requisicoes, lida.Habilitados, [], lida.Rodizio.Agregado, Descanso.DaLei, Em(6, 4));
        var erro = await Assert.ThrowsAsync<ConflitoDeConcorrencia>(() => sessao.Escalas().Adicionar(escala, lida.Rodizio.Versao, Cancelamento));

        Assert.Equal("O rodízio da estiva mudou desde a versão 1: está na versão 2. Leia o rodízio de novo e refaça a escala.", erro.Message);
        Assert.Null(await sessao.Escalas().DaEscalacao(sexta.Id, Cancelamento));
    }

    [Fact]
    public async Task A_desistencia_numa_escala_que_nao_e_a_mais_recente_nao_mexe_no_rodizio()
    {
        var sexta = await GravarATardeESexta();
        var daTarde = await Banco.FazerAEscala(_tarde.Escalacao.Id, Em(5, 10));
        var deSexta = await Banco.FazerAEscala(sexta.Id, Em(6, 4));

        await Banco.Desistir(daTarde.Id, _tarde.Edson.Id, Em(5, 11));

        await using var sessao = Banco.NovaSessao();
        var rodizio = await sessao.Escalas().RodizioDa(Categoria.Estiva, Cancelamento);
        Assert.Equal(2, rodizio.Versao);
        Assert.Equal(deSexta.RodizioDepois.Vezes, rodizio.Agregado.Vezes);
    }

    /// <summary>A tarde de quinta, e a manhã de sexta, com uma vaga de estivador e o Fábio, que não trabalha na quinta.</summary>
    private async Task<Escalacao> GravarATardeESexta()
    {
        var sexta = new Escalacao(Categoria.Estiva, SextaDeManha, CalendarioDaEscalacao.Padrao);
        sexta.Habilitar(_fabio, [Funcoes.Estivador], sexta.AberturaDaHabilitacao);
        var requisicao = new Requisicao(new DadosDaRequisicao(CaisForte, AtlanticoSul, CaisLeste, SextaDeManha), [[new(Funcoes.Estivador, 1)]], QuartaAs10h);

        await Banco.Gravar([.. _tarde.Trabalhadores, _fabio], [_tarde.Requisicao, requisicao], _tarde.Escalacao, sexta);

        return sexta;
    }

    private async Task<(Escalacao Escalacao, IReadOnlyList<Requisicao> Requisicoes, IReadOnlyList<Trabalhador> Habilitados, Versionado<Rodizio> Rodizio)> LerParaEscalar(EscalacaoId id)
    {
        await using var sessao = Banco.NovaSessao();
        var escalacao = (await sessao.Escalacoes.Obter(id, Cancelamento))!.Agregado;

        return (
            escalacao,
            await sessao.Requisicoes.DoPeriodo(escalacao.Categoria, escalacao.Periodo, Cancelamento),
            await sessao.Trabalhadores.Obter(escalacao.Habilitacoes.Select(habilitacao => habilitacao.Trabalhador), Cancelamento),
            await sessao.Escalas().RodizioDa(escalacao.Categoria, Cancelamento));
    }

    private async Task<bool> Tentar(Escala escala, int versaoDoRodizio)
    {
        try
        {
            await using var sessao = Banco.NovaSessao();
            await sessao.Escalas().Adicionar(escala, versaoDoRodizio, Cancelamento);

            return true;
        }
        catch (ConflitoDeConcorrencia)
        {
            return false;
        }
    }
}
