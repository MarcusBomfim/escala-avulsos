using EscalaAvulsos.Dominio.Comum;

namespace EscalaAvulsos.Dominio.Trabalhadores;

/// <summary>
/// Uma função de uma categoria: estivador, sinaleiro, contramestre de terno,
/// operador de guindaste de bordo.
/// </summary>
/// <remarks>
/// <para>
/// As funções de cada categoria vêm das convenções coletivas e mudam com
/// elas: são dados, e não um enum.
/// </para>
/// <para>
/// Duas funções são a mesma se são da mesma categoria e têm o mesmo nome,
/// sem diferença de maiúsculas nem de espaços: "operador de  guindaste" é
/// "Operador de guindaste".
/// </para>
/// </remarks>
public sealed record Funcao
{
    public const int TamanhoMaximoDoNome = 60;

    public Funcao(Categoria categoria, string nome)
    {
        if (!Enum.IsDefined(categoria))
        {
            throw new ExcecaoDeDominio($"A categoria {(int)categoria} não existe.");
        }

        if (Textos.Problema(nome, "o nome da função", TamanhoMaximoDoNome) is { } problema)
        {
            throw new ExcecaoDeDominio(problema);
        }

        var palavras = string.Join(' ', nome.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        Categoria = categoria;
        Nome = $"{char.ToUpperInvariant(palavras[0])}{palavras[1..]}";
    }

    public Categoria Categoria { get; }

    /// <summary>Como se escreve sozinho: "Operador de guindaste de bordo".</summary>
    public string Nome { get; }

    /// <summary>Como se escreve no meio da frase: "operador de guindaste de bordo".</summary>
    public string NaFrase => $"{char.ToLowerInvariant(Nome[0])}{Nome[1..]}";

    public bool Equals(Funcao? other) =>
        other is not null && Categoria == other.Categoria && string.Equals(Nome, other.Nome, StringComparison.OrdinalIgnoreCase);

    public override int GetHashCode() => HashCode.Combine(Categoria, StringComparer.OrdinalIgnoreCase.GetHashCode(Nome));

    public override string ToString() => Nome;
}
