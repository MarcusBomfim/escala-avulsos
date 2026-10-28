using EscalaAvulsos.Dominio.Comum;
using EscalaAvulsos.Dominio.Periodos;

namespace EscalaAvulsos.Dominio.Escalacoes;

/// <summary>
/// O descanso mínimo entre duas jornadas: onze horas seguidas, pela Lei
/// 9.719/1998 (art. 8º) — ou o que a convenção coletiva disser (ADR-0006).
/// </summary>
/// <remarks>
/// Com períodos de seis horas, onze horas querem dizer que, depois de
/// trabalhar num período, o trabalhador não pega nem o seguinte nem o outro:
/// só o terceiro, doze horas depois. O descanso é contado no relógio, do fim
/// de uma jornada ao começo da outra — também para trás, quando a jornada
/// marcada é depois do período.
/// </remarks>
public sealed record Descanso
{
    public Descanso(TimeSpan minimo)
    {
        if (minimo < TimeSpan.Zero)
        {
            throw new ExcecaoDeDominio("O descanso entre jornadas não pode ser negativo.");
        }

        Minimo = minimo;
    }

    /// <summary>As onze horas seguidas da Lei 9.719/1998.</summary>
    public static Descanso DaLei { get; } = new(TimeSpan.FromHours(11));

    public TimeSpan Minimo { get; }

    /// <summary>
    /// A primeira jornada, na ordem do tempo, que não deixa o trabalhador pegar
    /// o período com o descanso em dia; nula, se nenhuma impede.
    /// </summary>
    /// <param name="jornadas">Os períodos em que ele já está escalado.</param>
    public ConflitoDeDescanso? ConflitoCom(Periodo periodo, IEnumerable<Periodo> jornadas)
    {
        ArgumentNullException.ThrowIfNull(periodo);
        ArgumentNullException.ThrowIfNull(jornadas);

        foreach (var jornada in jornadas.Order())
        {
            TimeSpan? folga = jornada.Fim <= periodo.Inicio ? periodo.Inicio - jornada.Fim
                : periodo.Fim <= jornada.Inicio ? jornada.Inicio - periodo.Fim
                : null;

            if (folga is not { } horas || horas < Minimo)
            {
                return new ConflitoDeDescanso(periodo, jornada, Minimo);
            }
        }

        return null;
    }

    /// <summary>"11 horas".</summary>
    public override string ToString() => FormatoBrasileiro.Duracao(Minimo);
}

/// <summary>Uma jornada já marcada que deixaria o trabalhador sem o descanso mínimo — e até quando.</summary>
public sealed record ConflitoDeDescanso
{
    internal ConflitoDeDescanso(Periodo periodo, Periodo jornada, TimeSpan minimo)
    {
        Periodo = periodo;
        Jornada = jornada;
        Minimo = minimo;
    }

    /// <summary>O período que o trabalhador queria pegar.</summary>
    public Periodo Periodo { get; }

    /// <summary>O período em que ele já está escalado.</summary>
    public Periodo Jornada { get; }

    public TimeSpan Minimo { get; }

    /// <summary>Quando o descanso depois da jornada termina; nulo, se a jornada é depois do período, ou é o próprio período.</summary>
    public DateTimeOffset? DescansoTerminaEm => Jornada.Fim <= Periodo.Inicio ? Jornada.Fim + Minimo : null;

    /// <summary>
    /// "tem jornada em 05/11/2026, das 7h às 13h, e o descanso de 11 horas só
    /// termina em 06/11/2026, à meia-noite".
    /// </summary>
    public string Descricao =>
        Jornada == Periodo ? $"já tem jornada em {Periodo}"
        : DescansoTerminaEm is { } termino ? $"tem jornada em {Jornada}, e o descanso de {FormatoBrasileiro.Duracao(Minimo)} só termina em {FormatoBrasileiro.Instante(termino)}"
        : $"tem jornada em {Jornada}, e não teria {FormatoBrasileiro.Duracao(Minimo)} de descanso antes dela";

    public override string ToString() => Descricao;
}
