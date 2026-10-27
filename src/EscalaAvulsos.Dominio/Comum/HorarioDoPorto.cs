namespace EscalaAvulsos.Dominio.Comum;

/// <summary>
/// O relógio do porto: o horário de Brasília, em que os períodos começam e
/// terminam — e não o fuso da máquina que roda o sistema (ADR-0002).
/// </summary>
/// <remarks>
/// O fuso é procurado pelo nome da base IANA, <c>America/Sao_Paulo</c>, que o
/// .NET resolve no Linux e no Windows. O Brasil não tem horário de verão desde
/// 2019, mas a conversão passa pelo fuso assim mesmo: uma data anterior sai
/// com o deslocamento que valia nela. As mudanças de horário de verão eram à
/// meia-noite, e nenhum período começa nessa hora — não há hora de início que
/// caia num horário que não existiu, ou que existiu duas vezes.
/// </remarks>
public static class HorarioDoPorto
{
    public static TimeZoneInfo Fuso { get; } = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    /// <summary>O instante em que o relógio do porto marca aquela hora daquele dia.</summary>
    public static DateTimeOffset Em(DateOnly dia, TimeOnly hora)
    {
        var local = dia.ToDateTime(hora, DateTimeKind.Unspecified);

        return new DateTimeOffset(local, Fuso.GetUtcOffset(local));
    }

    /// <summary>A data e a hora que o relógio do porto marca naquele instante.</summary>
    public static DateTime Local(DateTimeOffset instante) => TimeZoneInfo.ConvertTime(instante, Fuso).DateTime;
}
