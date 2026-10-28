using EscalaAvulsos.Dominio.Comum;

namespace EscalaAvulsos.Dominio.Escalacoes;

/// <summary>A identidade de uma escalação.</summary>
public sealed record EscalacaoId : Identificador
{
    public EscalacaoId(Guid valor)
        : base(valor)
    {
    }

    public static EscalacaoId Novo() => new(Guid.CreateVersion7());
}
