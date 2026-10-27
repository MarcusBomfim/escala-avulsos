using EscalaAvulsos.Dominio.Comum;

namespace EscalaAvulsos.Dominio.Trabalhadores;

/// <summary>A identidade de um trabalhador.</summary>
/// <remarks>
/// Separada da matrícula: a matrícula é como as pessoas o encontram, e pode
/// ser corrigida; a identidade é como o sistema o encontra, e não muda.
/// </remarks>
public sealed record TrabalhadorId : Identificador
{
    public TrabalhadorId(Guid valor)
        : base(valor)
    {
    }

    public static TrabalhadorId Novo() => new(Guid.CreateVersion7());
}
