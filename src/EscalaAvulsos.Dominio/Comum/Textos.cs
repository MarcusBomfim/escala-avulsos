namespace EscalaAvulsos.Dominio.Comum;

/// <summary>Os textos que as pessoas escrevem: nomes, motivos, observações.</summary>
internal static class Textos
{
    /// <summary>Sem espaço nas pontas; nulo se vazio.</summary>
    public static string? Opcional(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

    /// <summary>A falta ou o excesso, numa frase — ou nada, se o texto serve.</summary>
    /// <param name="oQue">Como o campo se diz numa frase: "o nome do trabalhador".</param>
    public static string? Problema(string? texto, string oQue, int maximo) =>
        string.IsNullOrWhiteSpace(texto) ? $"Falta {oQue}."
        : texto.Trim().Length > maximo ? $"{char.ToUpperInvariant(oQue[0])}{oQue[1..]} passa de {maximo} caracteres."
        : null;
}
