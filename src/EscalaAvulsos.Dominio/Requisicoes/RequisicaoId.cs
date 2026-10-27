using EscalaAvulsos.Dominio.Comum;

namespace EscalaAvulsos.Dominio.Requisicoes;

/// <summary>A identidade de uma requisição.</summary>
public sealed record RequisicaoId : Identificador
{
    public RequisicaoId(Guid valor)
        : base(valor)
    {
    }

    public static RequisicaoId Novo() => new(Guid.CreateVersion7());
}
