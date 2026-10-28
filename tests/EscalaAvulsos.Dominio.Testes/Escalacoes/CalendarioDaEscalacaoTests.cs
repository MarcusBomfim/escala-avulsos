using EscalaAvulsos.Dominio.Comum;
using EscalaAvulsos.Dominio.Escalacoes;
using EscalaAvulsos.Dominio.Periodos;
using EscalaAvulsos.Dominio.Trabalhadores;
using static EscalaAvulsos.Dominio.Testes.Escalacoes.EscalacoesDeTeste;

namespace EscalaAvulsos.Dominio.Testes.Escalacoes;

public sealed class CalendarioDaEscalacaoTests
{
    [Fact]
    public void O_padrao_recebe_requisicoes_ate_quatro_horas_antes_e_habilita_de_doze_a_tres_horas_antes()
    {
        var padrao = CalendarioDaEscalacao.Padrao;

        Assert.Equal(
            (TimeSpan.FromHours(4), TimeSpan.FromHours(12), TimeSpan.FromHours(3)),
            (padrao.AntecedenciaDasRequisicoes, padrao.AntecedenciaDaAbertura, padrao.AntecedenciaDoFechamento));
    }

    [Fact]
    public void Os_horarios_da_escalacao_saem_do_inicio_do_periodo()
    {
        var tarde = DaEstiva();

        Assert.Equal((Em(5, 9), Em(5, 1), Em(5, 10)), (tarde.PrazoDasRequisicoes, tarde.AberturaDaHabilitacao, tarde.FechamentoDaHabilitacao));
    }

    [Fact]
    public void Para_a_madrugada_tudo_acontece_na_vespera()
    {
        var madrugada = new Escalacao(Categoria.Estiva, Periodo.De(new DateOnly(2026, 11, 6), Turno.Madrugada), CalendarioDaEscalacao.Padrao);

        Assert.Equal((Em(5, 21), Em(5, 13), Em(5, 22)), (madrugada.PrazoDasRequisicoes, madrugada.AberturaDaHabilitacao, madrugada.FechamentoDaHabilitacao));
    }

    [Fact]
    public void As_requisicoes_podem_chegar_ate_a_hora_em_que_a_habilitacao_fecha()
    {
        var justo = new CalendarioDaEscalacao(TimeSpan.FromHours(3), TimeSpan.FromHours(12), TimeSpan.FromHours(3));

        Assert.Equal(justo.AntecedenciaDoFechamento, justo.AntecedenciaDasRequisicoes);
    }

    [Fact]
    public void Diz_tudo_o_que_esta_errado_de_uma_vez()
    {
        var erro = Assert.Throws<ExcecaoDeDominio>(() => new CalendarioDaEscalacao(TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero));

        Assert.Equal(
            "A habilitação tem de fechar antes de o período começar. "
            + "A habilitação tem de abrir antes de fechar, e abriria 0 minutos antes do período, fechando 0 minutos antes.",
            erro.Message);
    }

    [Fact]
    public void A_habilitacao_abre_antes_de_fechar()
    {
        var erro = Assert.Throws<ExcecaoDeDominio>(() => new CalendarioDaEscalacao(TimeSpan.FromHours(4), new TimeSpan(2, 30, 0), TimeSpan.FromHours(3)));

        Assert.Equal("A habilitação tem de abrir antes de fechar, e abriria 2 horas e 30 minutos antes do período, fechando 3 horas antes.", erro.Message);
    }

    [Fact]
    public void As_requisicoes_chegam_antes_de_a_habilitacao_fechar()
    {
        var erro = Assert.Throws<ExcecaoDeDominio>(() => new CalendarioDaEscalacao(TimeSpan.FromMinutes(45), TimeSpan.FromHours(12), TimeSpan.FromHours(1)));

        Assert.Equal(
            "As requisições têm de chegar até a habilitação fechar, porque a escala é feita com elas: chegariam até 45 minutos antes do período, e a habilitação fecha 1 hora antes.",
            erro.Message);

        var porUmMinuto = Assert.Throws<ExcecaoDeDominio>(() => new CalendarioDaEscalacao(new TimeSpan(0, 59, 0), TimeSpan.FromHours(12), new TimeSpan(1, 1, 0)));

        Assert.Contains("chegariam até 59 minutos antes do período, e a habilitação fecha 1 hora e 1 minuto antes.", porUmMinuto.Message, StringComparison.Ordinal);
    }
}
