using EscalaAvulsos.Dominio.Comum;

namespace EscalaAvulsos.Dominio.Requisicoes;

/// <summary>Onde o navio está atracado: o terminal e, quando se sabe, o berço.</summary>
/// <remarks>
/// É o que o trabalhador precisa saber para chegar: o nome pelo qual o
/// terminal é conhecido no porto, e o berço, como aparece na placa do cais.
/// </remarks>
public sealed record LocalDaOperacao
{
    public const int TamanhoMaximoDoTerminal = 80;

    public const int TamanhoMaximoDoBerco = 20;

    public LocalDaOperacao(string terminal, string? berco = null)
    {
        var problemas = new List<string>();

        if (Textos.Problema(terminal, "o terminal", TamanhoMaximoDoTerminal) is { } problemaDoTerminal)
        {
            problemas.Add(problemaDoTerminal);
        }

        if (Textos.Opcional(berco)?.Length > TamanhoMaximoDoBerco)
        {
            problemas.Add($"O berço passa de {TamanhoMaximoDoBerco} caracteres.");
        }

        if (problemas.Count > 0)
        {
            throw new ExcecaoDeDominio(string.Join(" ", problemas));
        }

        Terminal = terminal.Trim();
        Berco = Textos.Opcional(berco);
    }

    public string Terminal { get; }

    /// <summary>O berço, como na placa: "4", "35A"; nulo, se o operador não informou.</summary>
    public string? Berco { get; }

    /// <summary>"Terminal Cais Leste, berço 4".</summary>
    public override string ToString() => Berco is null ? Terminal : $"{Terminal}, berço {Berco}";
}
