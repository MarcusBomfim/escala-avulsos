using EscalaAvulsos.Dominio.Comum;

namespace EscalaAvulsos.Dominio.Frequencias;

/// <summary>
/// A frequência mínima que o órgão gestor exige no mês: quantas vezes o
/// trabalhador se habilita, no mínimo, e quantas faltas sem justificativa ele
/// pode ter, no máximo (ADR-0009).
/// </summary>
/// <remarks>
/// Os números são da norma de cada porto, e entram como configuração; o padrão
/// deste sistema — 12 habilitações e 2 faltas por mês — é só o ponto de
/// partida. O mínimo vale para o mês inteiro, e diminui na proporção dos dias
/// em que o trabalhador não podia trabalhar.
/// </remarks>
public sealed record RegraDeFrequencia
{
    public RegraDeFrequencia(int minimoDeHabilitacoes, int maximoDeFaltas)
    {
        var problemas = new List<string>();

        if (minimoDeHabilitacoes < 0)
        {
            problemas.Add($"O mínimo de habilitações no mês não pode ser negativo, e seria {minimoDeHabilitacoes}.");
        }

        if (maximoDeFaltas < 0)
        {
            problemas.Add($"O máximo de faltas no mês não pode ser negativo, e seria {maximoDeFaltas}.");
        }

        if (problemas.Count > 0)
        {
            throw new ExcecaoDeDominio(string.Join(" ", problemas));
        }

        MinimoDeHabilitacoes = minimoDeHabilitacoes;
        MaximoDeFaltas = maximoDeFaltas;
    }

    /// <summary>12 habilitações e até 2 faltas sem justificativa por mês.</summary>
    public static RegraDeFrequencia Padrao { get; } = new(12, 2);

    /// <summary>As habilitações exigidas num mês em que o trabalhador pôde trabalhar todos os dias.</summary>
    public int MinimoDeHabilitacoes { get; }

    /// <summary>As faltas sem justificativa que o mês aceita.</summary>
    public int MaximoDeFaltas { get; }
}
