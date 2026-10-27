using System.Globalization;

namespace EscalaAvulsos.Dominio.Comum;

/// <summary>
/// Datas e horas escritas como se lê no Brasil — "05/11/2026", "7h" —, para
/// as mensagens e o texto do domínio.
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
}
