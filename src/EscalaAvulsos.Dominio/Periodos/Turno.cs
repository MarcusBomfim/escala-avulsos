namespace EscalaAvulsos.Dominio.Periodos;

/// <summary>Os quatro turnos de seis horas de cada dia, na ordem em que começam (ADR-0002).</summary>
public enum Turno
{
    /// <summary>Da 1h às 7h.</summary>
    Madrugada,

    /// <summary>Das 7h às 13h.</summary>
    Manha,

    /// <summary>Das 13h às 19h.</summary>
    Tarde,

    /// <summary>Das 19h à 1h do dia seguinte.</summary>
    Noite,
}
