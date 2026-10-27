using System.Globalization;
using EscalaAvulsos.Dominio.Comum;
using EscalaAvulsos.Dominio.Periodos;

namespace EscalaAvulsos.Dominio.Testes.Periodos;

public sealed class PeriodoTests
{
    private static readonly DateOnly Quinta = new(2026, 11, 5);

    [Theory]
    [InlineData(Turno.Madrugada, "2026-11-05T01:00:00-03:00", "2026-11-05T07:00:00-03:00")]
    [InlineData(Turno.Manha, "2026-11-05T07:00:00-03:00", "2026-11-05T13:00:00-03:00")]
    [InlineData(Turno.Tarde, "2026-11-05T13:00:00-03:00", "2026-11-05T19:00:00-03:00")]
    [InlineData(Turno.Noite, "2026-11-05T19:00:00-03:00", "2026-11-06T01:00:00-03:00")]
    public void Cada_turno_tem_seis_horas_no_horario_de_Brasilia(Turno turno, string inicio, string fim)
    {
        var periodo = Periodo.De(Quinta, turno);

        Assert.Equal((Instante(inicio), Instante(fim)), (periodo.Inicio, periodo.Fim));
        Assert.Equal((TimeSpan.FromHours(-3), TimeSpan.FromHours(-3)), (periodo.Inicio.Offset, periodo.Fim.Offset));
        Assert.Equal(TimeSpan.FromHours(6), periodo.Fim - periodo.Inicio);
    }

    [Theory]
    [InlineData("2026-11-06T00:30:00-03:00", "2026-11-05", Turno.Noite)]
    [InlineData("2026-11-06T00:59:59-03:00", "2026-11-05", Turno.Noite)]
    [InlineData("2026-11-06T01:00:00-03:00", "2026-11-06", Turno.Madrugada)]
    [InlineData("2026-11-06T06:59:59-03:00", "2026-11-06", Turno.Madrugada)]
    [InlineData("2026-11-06T07:00:00-03:00", "2026-11-06", Turno.Manha)]
    [InlineData("2026-11-06T12:59:59-03:00", "2026-11-06", Turno.Manha)]
    [InlineData("2026-11-06T13:00:00-03:00", "2026-11-06", Turno.Tarde)]
    [InlineData("2026-11-06T18:59:59-03:00", "2026-11-06", Turno.Tarde)]
    [InlineData("2026-11-06T19:00:00-03:00", "2026-11-06", Turno.Noite)]
    [InlineData("2026-11-06T23:59:59-03:00", "2026-11-06", Turno.Noite)]
    [InlineData("2026-11-06T10:00:00+00:00", "2026-11-06", Turno.Manha)]
    [InlineData("2026-11-06T03:30:00+00:00", "2026-11-05", Turno.Noite)]
    public void Acha_o_periodo_de_um_instante_pelo_relogio_do_porto(string instante, string dia, Turno turno)
    {
        var periodo = Periodo.Contendo(Instante(instante));

        Assert.Equal(Periodo.De(DateOnly.Parse(dia, CultureInfo.InvariantCulture), turno), periodo);
        Assert.True(periodo.Contem(Instante(instante)));
    }

    [Fact]
    public void O_inicio_e_do_periodo_e_o_fim_ja_e_do_seguinte()
    {
        var manha = Periodo.De(Quinta, Turno.Manha);

        Assert.True(manha.Contem(manha.Inicio));
        Assert.False(manha.Contem(manha.Fim));
        Assert.False(manha.Contem(manha.Inicio.AddTicks(-1)));
        Assert.True(manha.Seguinte.Contem(manha.Fim));
    }

    [Fact]
    public void A_noite_passa_para_a_madrugada_do_dia_seguinte_e_volta()
    {
        var noite = Periodo.De(Quinta, Turno.Noite);

        Assert.Equal(Periodo.De(Quinta.AddDays(1), Turno.Madrugada), noite.Seguinte);
        Assert.Equal(noite, noite.Seguinte.Anterior);
        Assert.Equal(Periodo.De(Quinta, Turno.Tarde), noite.Anterior);
        Assert.Equal(Periodo.De(Quinta.AddDays(-1), Turno.Noite), Periodo.De(Quinta, Turno.Madrugada).Anterior);
    }

    [Fact]
    public void Quatro_periodos_depois_e_o_mesmo_turno_no_dia_seguinte()
    {
        foreach (var turno in Enum.GetValues<Turno>())
        {
            var periodo = Periodo.De(Quinta, turno);

            Assert.Equal(Periodo.De(Quinta.AddDays(1), turno), periodo.Seguinte.Seguinte.Seguinte.Seguinte);
        }
    }

    [Fact]
    public void Os_periodos_se_emendam_sem_buraco_nem_sobreposicao_mesmo_quando_havia_horario_de_verao()
    {
        /*
         * De outubro de 2018 a março de 2019 o horário de verão começou e
         * terminou. O deslocamento muda à meia-noite, no meio da noite — que
         * nessas duas datas teve cinco e sete horas de relógio —, e ainda
         * assim cada período começa onde o anterior termina.
         */
        var periodo = Periodo.De(new DateOnly(2018, 10, 1), Turno.Madrugada);
        var duracoes = new HashSet<TimeSpan>();

        while (periodo.Dia < new DateOnly(2019, 3, 31))
        {
            Assert.Equal(periodo.Fim, periodo.Seguinte.Inicio);
            Assert.Equal(periodo, Periodo.Contendo(periodo.Inicio));
            Assert.Equal(periodo, Periodo.Contendo(periodo.Fim.AddTicks(-1)));
            duracoes.Add(periodo.Fim - periodo.Inicio);
            periodo = periodo.Seguinte;
        }

        Assert.Equal([TimeSpan.FromHours(5), TimeSpan.FromHours(6), TimeSpan.FromHours(7)], duracoes.Order());
    }

    [Fact]
    public void No_horario_de_verao_o_periodo_sai_com_o_deslocamento_daquele_dia()
    {
        var manhaDeVerao = Periodo.De(new DateOnly(2018, 12, 3), Turno.Manha);

        Assert.Equal(TimeSpan.FromHours(-2), manhaDeVerao.Inicio.Offset);
        Assert.Equal(Instante("2018-12-03T09:00:00+00:00"), manhaDeVerao.Inicio);
    }

    [Theory]
    [InlineData(Turno.Madrugada, true)]
    [InlineData(Turno.Manha, false)]
    [InlineData(Turno.Tarde, false)]
    [InlineData(Turno.Noite, true)]
    public void A_noite_e_a_madrugada_sao_trabalho_noturno(Turno turno, bool noturno)
    {
        Assert.Equal(noturno, Periodo.De(Quinta, turno).Noturno);
    }

    [Theory]
    [InlineData(Turno.Madrugada, "05/11/2026, da 1h às 7h")]
    [InlineData(Turno.Manha, "05/11/2026, das 7h às 13h")]
    [InlineData(Turno.Tarde, "05/11/2026, das 13h às 19h")]
    [InlineData(Turno.Noite, "05/11/2026, das 19h à 1h")]
    public void Se_escreve_como_se_fala(Turno turno, string escrito)
    {
        Assert.Equal(escrito, Periodo.De(Quinta, turno).ToString());
    }

    [Fact]
    public void Turno_que_nao_existe_e_recusado()
    {
        var erro = Assert.Throws<ExcecaoDeDominio>(() => Periodo.De(Quinta, (Turno)4));

        Assert.Equal("O turno 4 não existe: são a madrugada, a manhã, a tarde e a noite.", erro.Message);
    }

    [Fact]
    public void Ordena_pelo_inicio()
    {
        var noiteDeQuarta = Periodo.De(Quinta.AddDays(-1), Turno.Noite);
        var madrugadaDeQuinta = Periodo.De(Quinta, Turno.Madrugada);
        var manhaDeQuinta = Periodo.De(Quinta, Turno.Manha);

        Assert.Equal([noiteDeQuarta, madrugadaDeQuinta, manhaDeQuinta], new[] { manhaDeQuinta, noiteDeQuarta, madrugadaDeQuinta }.Order());
        Assert.True(noiteDeQuarta < madrugadaDeQuinta);
        Assert.True(manhaDeQuinta > madrugadaDeQuinta);
        Assert.True(manhaDeQuinta >= Periodo.De(Quinta, Turno.Manha));
        Assert.True(noiteDeQuarta <= noiteDeQuarta.Seguinte.Anterior);
    }

    private static DateTimeOffset Instante(string texto) => DateTimeOffset.Parse(texto, CultureInfo.InvariantCulture);
}
