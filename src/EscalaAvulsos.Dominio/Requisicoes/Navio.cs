using EscalaAvulsos.Dominio.Comum;
using EscalaAvulsos.Dominio.Identificacao;

namespace EscalaAvulsos.Dominio.Requisicoes;

/// <summary>O navio em que se trabalha: o nome, como está no costado, e o número IMO.</summary>
/// <remarks>
/// Dois navios são o mesmo se o número IMO é o mesmo: o navio que troca de
/// nome continua sendo o mesmo casco, e dois navios podem ter o mesmo nome.
/// </remarks>
public sealed record Navio
{
    public const int TamanhoMaximoDoNome = 80;

    public Navio(string nome, NumeroImo imo)
    {
        ArgumentNullException.ThrowIfNull(imo);

        if (Textos.Problema(nome, "o nome do navio", TamanhoMaximoDoNome) is { } problema)
        {
            throw new ExcecaoDeDominio(problema);
        }

        Nome = nome.Trim();
        Imo = imo;
    }

    public string Nome { get; }

    public NumeroImo Imo { get; }

    public bool Equals(Navio? other) => other is not null && Imo == other.Imo;

    public override int GetHashCode() => Imo.GetHashCode();

    /// <summary>"Atlântico Sul (IMO 9876543)".</summary>
    public override string ToString() => $"{Nome} ({Imo})";
}
