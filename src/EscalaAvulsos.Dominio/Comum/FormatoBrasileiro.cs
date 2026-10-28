using System.Globalization;

namespace EscalaAvulsos.Dominio.Comum;

/// <summary>
/// Datas, horas e durações escritas como se lê no Brasil — "05/11/2026",
/// "7h", "às 10h", "11 horas" —, para as mensagens e o texto do domínio.
/// </summary>
/// <remarks>
/// Montado à mão, e não com <c>CultureInfo("pt-BR")</c>: o texto não pode
/// depender dos dados de cultura instalados na máquina que roda, nem da
/// cultura do processo.
/// </remarks>
internal static class FormatoBrasileiro
{
    /// <summary>"05/11/2026".</summary>
    public static string Data(DateOnly data) => data.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    /// <summary>"7h", "13h": a hora cheia, como se fala no porto.</summary>
    public static string Hora(int hora) => $"{hora.ToString(CultureInfo.InvariantCulture)}h";

    /// <summary>
    /// O instante no relógio do porto: "05/11/2026, às 10h", "05/11/2026, à 1h",
    /// "05/11/2026, às 4h30", "06/11/2026, à meia-noite", "05/11/2026, ao meio-dia".
    /// </summary>
    public static string Instante(DateTimeOffset instante)
    {
        var local = HorarioDoPorto.Local(instante);

        var hora = (local.Hour, local.Minute) switch
        {
            (0, 0) => "à meia-noite",
            (12, 0) => "ao meio-dia",
            var (horas, minutos) => $"{(horas <= 1 ? "à" : "às")} {Hora(horas)}{(minutos == 0 ? string.Empty : minutos.ToString("00", CultureInfo.InvariantCulture))}",
        };

        return $"{Data(DateOnly.FromDateTime(local))}, {hora}";
    }

    /// <summary>"11 horas", "1 hora", "2 horas e 30 minutos", "45 minutos".</summary>
    public static string Duracao(TimeSpan duracao)
    {
        var horas = (int)duracao.TotalHours;
        var minutos = duracao.Minutes;
        var emHoras = horas == 1 ? "1 hora" : $"{horas.ToString(CultureInfo.InvariantCulture)} horas";
        var emMinutos = minutos == 1 ? "1 minuto" : $"{minutos.ToString(CultureInfo.InvariantCulture)} minutos";

        return (horas, minutos) switch
        {
            (0, _) => emMinutos,
            (_, 0) => emHoras,
            _ => $"{emHoras} e {emMinutos}",
        };
    }
}
