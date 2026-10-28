using EscalaAvulsos.Dominio.Escalacoes;
using EscalaAvulsos.Dominio.Identificacao;
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
 * O domínio não põe ninguém em duas jornadas que se cruzam, nem sem o
 * descanso — se receber as jornadas de agora. Quem decide com jornadas
 * velhas, lidas antes de outra escala ser gravada, esbarra no banco: a
 * restrição de exclusão recusa a vaga, e nada da gravação fica (ADR-0013).
 */
public sealed class NinguemEmDuasJornadasTests(BancoDeTestes bancos) : TestesComBanco(bancos)
{
    private static readonly Periodo NoiteDeQuinta = Periodo.De(new DateOnly(2026, 11, 5), Turno.Noite);

    /// <summary>A noite com a habilitação fechando às 11h: a escala dela sai antes de a tarde começar.</summary>
    private static readonly CalendarioDaEscalacao FechaCedo = new(TimeSpan.FromHours(8), TimeSpan.FromHours(14), TimeSpan.FromHours(8));

    private readonly TardeDeQuinta _tarde = new();

    [Fact]
    public async Task A_escala_feita_com_jornadas_velhas_e_recusada_e_nada_dela_fica_gravado()
    {
        // O Benedito pegou a tarde; a noite é escalada como se a tarde ainda não estivesse no banco.
        var noite = Noite(CalendarioDaEscalacao.Padrao, _tarde.Benedito);
        await Banco.Gravar(_tarde.Trabalhadores, [_tarde.Requisicao, RequisicaoDaNoite()], _tarde.Escalacao, noite);
        await Banco.FazerAEscala(_tarde.Escalacao.Id, Em(5, 10));

        var erro = await Assert.ThrowsAsync<JornadaSemDescanso>(() => Banco.FazerAEscala(noite.Id, Em(5, 16), jornadasVelhas: true));

        Assert.Equal(
            "Benedito Lima (matrícula 4130) já tem jornada em 05/11/2026, das 13h às 19h, e as duas não cabem com o descanso de 11 horas entre elas. "
            + "A escala não foi gravada: leia as jornadas de novo e refaça.",
            erro.Message);
        await using var sessao = Banco.NovaSessao();
        Assert.Null(await sessao.Escalas().DaEscalacao(noite.Id, Cancelamento));
        Assert.Equal(1, (await sessao.Escalas().RodizioDa(Categoria.Estiva, Cancelamento)).Versao);
    }

    [Fact]
    public async Task Com_as_jornadas_de_agora_a_escala_sai_e_diz_por_que_ele_ficou_de_fora()
    {
        var noite = Noite(CalendarioDaEscalacao.Padrao, _tarde.Benedito);
        await Banco.Gravar(_tarde.Trabalhadores, [_tarde.Requisicao, RequisicaoDaNoite()], _tarde.Escalacao, noite);
        await Banco.FazerAEscala(_tarde.Escalacao.Id, Em(5, 10));

        var escala = await Banco.FazerAEscala(noite.Id, Em(5, 16));

        Assert.Equal(
            "Benedito Lima (matrícula 4130) ficou fora da escala: tem jornada em 05/11/2026, das 13h às 19h, e o descanso de 11 horas só termina em 06/11/2026, às 6h.",
            escala.SituacaoDe(Matricula.De(4130))?.ToString());
    }

    [Fact]
    public async Task O_substituto_escolhido_com_jornadas_velhas_e_recusado_e_a_desistencia_nao_fica()
    {
        /*
         * A tarde sai às 10h, com o Carlos na espera. A noite fecha cedo e sai
         * às 11h com o Carlos. Às 11h30 o Edson desiste da tarde: com as
         * jornadas velhas, o Carlos entraria — e ficaria das 13h à 1h no cais.
         */
        var noite = Noite(FechaCedo, _tarde.Carlos);
        await Banco.Gravar(_tarde.Trabalhadores, [_tarde.Requisicao, RequisicaoDaNoite()], _tarde.Escalacao, noite);
        var daTarde = await Banco.FazerAEscala(_tarde.Escalacao.Id, Em(5, 10));
        await Banco.FazerAEscala(noite.Id, Em(5, 11));

        var erro = await Assert.ThrowsAsync<JornadaSemDescanso>(() => Banco.Desistir(daTarde.Id, _tarde.Edson.Id, Em(5, 11, 30), jornadasVelhas: true));

        Assert.StartsWith(
            "Carlos Mota (matrícula 4152) já tem jornada em 05/11/2026, das 19h à 1h, e as duas não cabem com o descanso de 11 horas entre elas.",
            erro.Message,
            StringComparison.Ordinal);
        await using var sessao = Banco.NovaSessao();
        var tarde = (await sessao.Escalas().Obter(daTarde.Id, Cancelamento))!;
        Assert.Equal(1, tarde.Versao);
        Assert.True(tarde.Agregado.SituacaoDe(Matricula.De(4101))?.Escalado);
    }

    [Fact]
    public async Task Com_as_jornadas_de_agora_quem_ja_esta_na_noite_e_pulado()
    {
        var noite = Noite(FechaCedo, _tarde.Carlos);
        await Banco.Gravar(_tarde.Trabalhadores, [_tarde.Requisicao, RequisicaoDaNoite()], _tarde.Escalacao, noite);
        var daTarde = await Banco.FazerAEscala(_tarde.Escalacao.Id, Em(5, 10));
        await Banco.FazerAEscala(noite.Id, Em(5, 11));

        var substituto = await Banco.Desistir(daTarde.Id, _tarde.Edson.Id, Em(5, 11, 30));

        Assert.Equal("Dirceu Alves", substituto?.Nome);
    }

    [Fact]
    public async Task Duas_desistencias_ao_mesmo_tempo_nao_dao_as_duas_vagas_ao_mesmo_substituto()
    {
        /*
         * O Fábio espera na tarde e na noite. O Antônio desiste da tarde e o
         * Carlos da noite no mesmo instante, e as duas desistências leram as
         * jornadas antes de qualquer uma gravar: as duas escolhem o Fábio. O
         * banco grava uma só; a outra refaz, e o Fábio já não cabe nela.
         */
        var fabio = TrabalhadoresDeTeste.Estivador(4199, "Fábio Rocha");
        var tarde = new Escalacao(Categoria.Estiva, QuintaATarde, CalendarioDaEscalacao.Padrao);
        tarde.Habilitar(_tarde.Antonio, [Funcoes.Estivador], tarde.AberturaDaHabilitacao);
        tarde.Habilitar(fabio, [Funcoes.Estivador], tarde.AberturaDaHabilitacao);
        var noite = Noite(FechaCedo, _tarde.Carlos, fabio);
        var daTarde = new Requisicao(TardeDeQuinta.DaTarde, [[new(Funcoes.Estivador, 1)]], QuartaAs10h);
        await Banco.Gravar([_tarde.Antonio, _tarde.Carlos, fabio], [daTarde, RequisicaoDaNoite()], tarde, noite);
        var escalaDaTarde = await Banco.FazerAEscala(tarde.Id, Em(5, 10));
        var escalaDaNoite = await Banco.FazerAEscala(noite.Id, Em(5, 11));

        var naTarde = await DesistirSemGravar(escalaDaTarde.Id, _tarde.Antonio.Id);
        var naNoite = await DesistirSemGravar(escalaDaNoite.Id, _tarde.Carlos.Id);
        Assert.Equal(["Fábio Rocha", "Fábio Rocha"], [naTarde.Substituto?.Nome, naNoite.Substituto?.Nome]);

        var gravaram = await Task.WhenAll(Gravar(naTarde.Escala), Gravar(naNoite.Escala));

        Assert.Single(gravaram, gravou => gravou);

        // Quem perdeu refaz com as jornadas de agora: o Fábio não cabe, e a vaga fica vazia.
        var refeita = gravaram[0] ? escalaDaNoite : escalaDaTarde;
        var quemDesiste = gravaram[0] ? _tarde.Carlos : _tarde.Antonio;
        Assert.Null(await Banco.Desistir(refeita.Id, quemDesiste.Id, Em(5, 11, 30)));
    }

    private static Escalacao Noite(CalendarioDaEscalacao calendario, params Trabalhador[] habilitados)
    {
        var noite = new Escalacao(Categoria.Estiva, NoiteDeQuinta, calendario);

        foreach (var trabalhador in habilitados)
        {
            noite.Habilitar(trabalhador, [Funcoes.Estivador], noite.AberturaDaHabilitacao);
        }

        return noite;
    }

    private static Requisicao RequisicaoDaNoite() =>
        new(new DadosDaRequisicao(CaisForte, AtlanticoSul, CaisLeste, NoiteDeQuinta), [[new(Funcoes.Estivador, 1)]], QuartaAs10h);

    /// <summary>A desistência decidida com as jornadas lidas agora, e gravada depois.</summary>
    private async Task<(Versionado<Escala> Escala, SituacaoNaEscala? Substituto)> DesistirSemGravar(EscalaId id, TrabalhadorId quem)
    {
        await using var sessao = Banco.NovaSessao();
        var lida = (await sessao.Escalas().Obter(id, Cancelamento))!;
        var jornadas = await sessao.Escalas().JornadasPerto(
            lida.Agregado.Situacoes.Where(situacao => !situacao.Escalado).Select(situacao => situacao.Trabalhador),
            lida.Agregado.Periodo,
            lida.Agregado.Descanso,
            Cancelamento);

        return (lida, lida.Agregado.Desistir(quem, Em(5, 11, 30), jornadas));
    }

    private async Task<bool> Gravar(Versionado<Escala> escala)
    {
        try
        {
            await using var sessao = Banco.NovaSessao();
            await sessao.Escalas().Atualizar(escala.Agregado, escala.Versao, Cancelamento);

            return true;
        }
        catch (JornadaSemDescanso)
        {
            return false;
        }
    }
}
