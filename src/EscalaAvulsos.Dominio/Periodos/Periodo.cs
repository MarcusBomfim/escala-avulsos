using EscalaAvulsos.Dominio.Comum;

namespace EscalaAvulsos.Dominio.Periodos;

/// <summary>
/// Um período de trabalho no porto: seis horas, num dos quatro turnos de um
/// dia — da 1h às 7h, das 7h às 13h, das 13h às 19h e das 19h à 1h.
/// </summary>
/// <remarks>
/// <para>
/// O período é do dia em que começa: a noite de 05/11 vai das 19h de 05/11 à
/// 1h de 06/11, e a madrugada de 06/11 começa nessa hora (ADR-0002).
/// </para>
/// <para>
/// Início e fim são instantes no horário de Brasília, com o deslocamento do
/// dia: o mesmo período é o mesmo instante em qualquer servidor, em qualquer
/// fuso.
/// </para>
/// </remarks>
public sealed record Periodo : IComparable<Periodo>
{
    private Periodo(DateOnly dia, Turno turno)
    {
        Dia = dia;
        Turno = turno;
        Inicio = HorarioDoPorto.Em(dia, new TimeOnly(HoraDeInicio(turno), 0));
        Fim = HorarioDoPorto.Em(turno == Turno.Noite ? dia.AddDays(1) : dia, new TimeOnly(HoraDoFim(turno), 0));
    }

    /// <summary>O dia em que o período começa.</summary>
    public DateOnly Dia { get; }

    public Turno Turno { get; }

    public DateTimeOffset Inicio { get; }

    public DateTimeOffset Fim { get; }

    /// <summary>
    /// Se é trabalho noturno. No porto, a noite vai das 19h às 7h: a noite e a
    /// madrugada inteiras (OJ 60 da SDI-1 do TST).
    /// </summary>
    public bool Noturno => Turno is Turno.Noite or Turno.Madrugada;

    /// <summary>O período que começa quando este termina.</summary>
    public Periodo Seguinte => Turno == Turno.Noite ? new(Dia.AddDays(1), Turno.Madrugada) : new(Dia, Turno + 1);

    /// <summary>O período que termina quando este começa.</summary>
    public Periodo Anterior => Turno == Turno.Madrugada ? new(Dia.AddDays(-1), Turno.Noite) : new(Dia, Turno - 1);

    /// <summary>"das 7h às 13h", "da 1h às 7h", "das 19h à 1h".</summary>
    public string Horario
    {
        get
        {
            var inicio = HoraDeInicio(Turno);
            var fim = HoraDoFim(Turno);

            return $"{(inicio == 1 ? "da" : "das")} {FormatoBrasileiro.Hora(inicio)} {(fim == 1 ? "à" : "às")} {FormatoBrasileiro.Hora(fim)}";
        }
    }

    public static Periodo De(DateOnly dia, Turno turno)
    {
        if (!Enum.IsDefined(turno))
        {
            throw new ExcecaoDeDominio($"O turno {(int)turno} não existe: são a madrugada, a manhã, a tarde e a noite.");
        }

        return new Periodo(dia, turno);
    }

    /// <summary>
    /// O período em que cai um instante. Meia-noite e meia ainda é a noite do
    /// dia anterior: ela só termina à 1h.
    /// </summary>
    public static Periodo Contendo(DateTimeOffset instante)
    {
        var local = HorarioDoPorto.Local(instante);
        var dia = DateOnly.FromDateTime(local);

        return local.Hour switch
        {
            < 1 => new Periodo(dia.AddDays(-1), Turno.Noite),
            < 7 => new Periodo(dia, Turno.Madrugada),
            < 13 => new Periodo(dia, Turno.Manha),
            < 19 => new Periodo(dia, Turno.Tarde),
            _ => new Periodo(dia, Turno.Noite),
        };
    }

    /// <summary>Se o instante cai no período: do início, incluído, ao fim, que já é do período seguinte.</summary>
    public bool Contem(DateTimeOffset instante) => Inicio <= instante && instante < Fim;

    public int CompareTo(Periodo? other) => other is null ? 1 : Inicio.CompareTo(other.Inicio);

    /// <summary>"05/11/2026, das 7h às 13h".</summary>
    public override string ToString() => $"{FormatoBrasileiro.Data(Dia)}, {Horario}";

    public static bool operator <(Periodo esquerda, Periodo direita) => Comparar(esquerda, direita) < 0;

    public static bool operator >(Periodo esquerda, Periodo direita) => Comparar(esquerda, direita) > 0;

    public static bool operator <=(Periodo esquerda, Periodo direita) => Comparar(esquerda, direita) <= 0;

    public static bool operator >=(Periodo esquerda, Periodo direita) => Comparar(esquerda, direita) >= 0;

    private static int HoraDeInicio(Turno turno) => turno switch
    {
        Turno.Madrugada => 1,
        Turno.Manha => 7,
        Turno.Tarde => 13,
        _ => 19,
    };

    private static int HoraDoFim(Turno turno) => turno == Turno.Noite ? 1 : HoraDeInicio(turno) + 6;

    private static int Comparar(Periodo esquerda, Periodo direita)
    {
        ArgumentNullException.ThrowIfNull(esquerda);

        return esquerda.CompareTo(direita);
    }
}
