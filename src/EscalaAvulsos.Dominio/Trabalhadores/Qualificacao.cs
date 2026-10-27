namespace EscalaAvulsos.Dominio.Trabalhadores;

/// <summary>
/// Uma função que o trabalhador pode exercer porque fez o curso: desde quando,
/// e até quando, se ela pede reciclagem.
/// </summary>
/// <remarks>Só nasce pelo trabalhador, que confere se a função é da categoria dele.</remarks>
public sealed record Qualificacao
{
    internal Qualificacao(Funcao funcao, DateOnly desde, DateOnly? validaAte)
    {
        Funcao = funcao;
        Desde = desde;
        ValidaAte = validaAte;
    }

    public Funcao Funcao { get; }

    /// <summary>O primeiro dia em que a qualificação vale.</summary>
    public DateOnly Desde { get; }

    /// <summary>O último dia em que a qualificação vale; nulo, se ela não vence.</summary>
    public DateOnly? ValidaAte { get; }
}
