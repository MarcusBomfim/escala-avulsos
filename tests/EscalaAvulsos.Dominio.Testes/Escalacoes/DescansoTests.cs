using EscalaAvulsos.Dominio.Comum;
using EscalaAvulsos.Dominio.Escalacoes;
using EscalaAvulsos.Dominio.Periodos;
using static EscalaAvulsos.Dominio.Testes.Escalacoes.EscalacoesDeTeste;

namespace EscalaAvulsos.Dominio.Testes.Escalacoes;

public sealed class DescansoTests
{
    private static readonly DateOnly Quinta = new(2026, 11, 5);

    [Fact]
    public void A_lei_pede_onze_horas()
    {
        Assert.Equal(TimeSpan.FromHours(11), Descanso.DaLei.Minimo);
        Assert.Equal("11 horas", Descanso.DaLei.ToString());
    }

    [Fact]
    public void O_descanso_nao_e_negativo()
    {
        var erro = Assert.Throws<ExcecaoDeDominio>(() => new Descanso(TimeSpan.FromMinutes(-1)));

        Assert.Equal("O descanso entre jornadas não pode ser negativo.", erro.Message);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(6, 1)]
    [InlineData(11, 2)]
    [InlineData(12, 2)]
    [InlineData(13, 3)]
    public void Com_periodos_de_seis_horas_impede_tantos_periodos_antes_e_depois_da_jornada(int horas, int impedidos)
    {
        /*
         * Entre uma jornada e o período que está d períodos depois (ou antes),
         * a folga é de (|d| - 1) × 6 horas. Com 11 horas de descanso, o
         * seguinte e o outro ficam impedidos; o terceiro, com 12 horas de folga,
         * não. A jornada no próprio período impede sempre.
         */
        var descanso = new Descanso(TimeSpan.FromHours(horas));

        foreach (var turno in Enum.GetValues<Turno>())
        {
            var jornada = Periodo.De(Quinta, turno);

            for (var distancia = -5; distancia <= 5; distancia++)
            {
                var periodo = Deslocado(jornada, distancia);
                var conflito = descanso.ConflitoCom(periodo, [jornada]);

                Assert.True(
                    (conflito is not null) == (Math.Abs(distancia) <= impedidos || distancia == 0),
                    $"Jornada em {jornada}, período em {periodo}, descanso de {horas} horas.");
            }
        }
    }

    [Theory]
    [InlineData(Turno.Madrugada, "06/11/2026, às 18h")]
    [InlineData(Turno.Manha, "07/11/2026, à meia-noite")]
    [InlineData(Turno.Tarde, "07/11/2026, às 6h")]
    [InlineData(Turno.Noite, "07/11/2026, ao meio-dia")]
    public void Diz_quando_o_descanso_termina(Turno turno, string termino)
    {
        var jornada = Periodo.De(Quinta.AddDays(1), turno);

        var conflito = Descanso.DaLei.ConflitoCom(jornada.Seguinte, [jornada]);

        Assert.NotNull(conflito);
        Assert.Equal(jornada.Fim.AddHours(11), conflito.DescansoTerminaEm);
        Assert.Equal($"tem jornada em {jornada}, e o descanso de 11 horas só termina em {termino}", conflito.Descricao);
    }

    [Fact]
    public void A_manha_de_quinta_impede_a_noite_de_quinta_e_libera_a_madrugada_de_sexta()
    {
        var manha = Periodo.De(Quinta, Turno.Manha);

        var conflito = Descanso.DaLei.ConflitoCom(Periodo.De(Quinta, Turno.Noite), [manha]);

        Assert.Equal(
            "tem jornada em 05/11/2026, das 7h às 13h, e o descanso de 11 horas só termina em 06/11/2026, à meia-noite",
            conflito?.ToString());
        Assert.Null(Descanso.DaLei.ConflitoCom(Periodo.De(Quinta.AddDays(1), Turno.Madrugada), [manha]));
    }

    [Fact]
    public void A_jornada_marcada_depois_tambem_pede_descanso_antes_dela()
    {
        var conflito = Descanso.DaLei.ConflitoCom(Periodo.De(Quinta, Turno.Manha), [Periodo.De(Quinta, Turno.Noite)]);

        Assert.NotNull(conflito);
        Assert.Null(conflito.DescansoTerminaEm);
        Assert.Equal("tem jornada em 05/11/2026, das 19h à 1h, e não teria 11 horas de descanso antes dela", conflito.Descricao);
    }

    [Fact]
    public void A_jornada_no_mesmo_periodo_impede_mesmo_sem_descanso_nenhum()
    {
        var conflito = new Descanso(TimeSpan.Zero).ConflitoCom(QuintaATarde, [QuintaATarde]);

        Assert.Equal("já tem jornada em 05/11/2026, das 13h às 19h", conflito?.Descricao);
        Assert.Null(conflito?.DescansoTerminaEm);
    }

    [Fact]
    public void Diz_a_primeira_jornada_que_impede_e_ignora_as_que_nao_impedem()
    {
        // Fora de ordem de propósito: a manhã, que também impede, vem antes da madrugada na lista.
        Periodo[] jornadas =
        [
            Periodo.De(Quinta.AddDays(1), Turno.Manha),
            Periodo.De(Quinta, Turno.Manha),
            Periodo.De(Quinta, Turno.Madrugada),
            Periodo.De(Quinta.AddDays(-2), Turno.Tarde),
        ];

        var conflito = Descanso.DaLei.ConflitoCom(QuintaATarde, jornadas);

        Assert.Equal(Periodo.De(Quinta, Turno.Madrugada), conflito?.Jornada);
        Assert.Null(Descanso.DaLei.ConflitoCom(QuintaATarde, [Periodo.De(Quinta.AddDays(-2), Turno.Tarde), Periodo.De(Quinta.AddDays(1), Turno.Manha)]));
        Assert.Null(Descanso.DaLei.ConflitoCom(QuintaATarde, []));
    }

    private static Periodo Deslocado(Periodo periodo, int distancia)
    {
        for (var passo = 0; passo < Math.Abs(distancia); passo++)
        {
            periodo = distancia > 0 ? periodo.Seguinte : periodo.Anterior;
        }

        return periodo;
    }
}
