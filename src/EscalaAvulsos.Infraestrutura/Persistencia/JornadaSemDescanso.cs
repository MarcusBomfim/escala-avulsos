namespace EscalaAvulsos.Infraestrutura.Persistencia;

/// <summary>
/// Alguém da escala já tem, em outra escala, uma jornada que cruza com esta,
/// contado o descanso: o banco não grava a segunda (ADR-0013).
/// </summary>
/// <remarks>
/// Só chega aqui a escala feita com jornadas velhas — lidas antes de a outra
/// ser gravada. Quem leu as jornadas de novo e refez não a vê.
/// </remarks>
public sealed class JornadaSemDescanso : Exception
{
    public JornadaSemDescanso()
    {
    }

    public JornadaSemDescanso(string message)
        : base(message)
    {
    }

    public JornadaSemDescanso(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
