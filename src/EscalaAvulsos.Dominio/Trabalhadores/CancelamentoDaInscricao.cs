namespace EscalaAvulsos.Dominio.Trabalhadores;

/// <summary>
/// O fim da inscrição do trabalhador no órgão gestor — aposentadoria, morte,
/// pedido dele, decisão disciplinar —, a partir de um dia.
/// </summary>
public sealed record CancelamentoDaInscricao
{
    internal CancelamentoDaInscricao(DateOnly aPartirDe, string motivo)
    {
        APartirDe = aPartirDe;
        Motivo = motivo;
    }

    /// <summary>O primeiro dia em que ele não é mais escalado.</summary>
    public DateOnly APartirDe { get; }

    public string Motivo { get; }
}
