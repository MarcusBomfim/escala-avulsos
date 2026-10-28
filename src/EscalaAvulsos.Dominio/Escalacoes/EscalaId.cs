using EscalaAvulsos.Dominio.Comum;

namespace EscalaAvulsos.Dominio.Escalacoes;

/// <summary>A identidade de uma escala.</summary>
public sealed record EscalaId : Identificador
{
    public EscalaId(Guid valor)
        : base(valor)
    {
    }

    public static EscalaId Novo() => new(Guid.CreateVersion7());
}
