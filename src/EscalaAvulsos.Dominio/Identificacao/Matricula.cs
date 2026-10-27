using System.Globalization;
using EscalaAvulsos.Dominio.Comum;

namespace EscalaAvulsos.Dominio.Identificacao;

/// <summary>A matrícula do trabalhador no órgão gestor de mão de obra: um número.</summary>
/// <remarks>
/// É pela matrícula que o trabalhador se identifica — no aplicativo, na escala
/// publicada, no extrato —, e é na ordem das matrículas que o rodízio anda.
/// Por isso ela é comparada como número: a 99 vem antes da 100, e zeros à
/// esquerda não mudam nada — "004127" é a matrícula 4127.
/// </remarks>
public sealed record Matricula : IComparable<Matricula>
{
    /// <summary>A maior matrícula: sete dígitos.</summary>
    public const int Maior = 9_999_999;

    private Matricula(int numero) => Numero = numero;

    public int Numero { get; }

    /// <summary>Lê a matrícula como o trabalhador digita: só os dígitos, com ou sem zeros à esquerda.</summary>
    public static Matricula De(string texto)
    {
        ArgumentNullException.ThrowIfNull(texto);

        var digitos = texto.Trim();

        if (digitos.Length == 0 || !digitos.All(char.IsAsciiDigit))
        {
            throw new ExcecaoDeDominio($"\"{digitos}\" não é uma matrícula: a matrícula é um número, só com dígitos.");
        }

        var semZeros = digitos.TrimStart('0');

        if (semZeros.Length > 7)
        {
            throw new ExcecaoDeDominio($"A matrícula {digitos} tem mais de 7 dígitos: confira o número.");
        }

        return De(semZeros.Length == 0 ? 0 : int.Parse(semZeros, NumberStyles.None, CultureInfo.InvariantCulture));
    }

    public static Matricula De(int numero)
    {
        if (numero is < 1 or > Maior)
        {
            throw new ExcecaoDeDominio($"A matrícula vai de 1 a {Maior.ToString(CultureInfo.InvariantCulture)}: {numero.ToString(CultureInfo.InvariantCulture)} não é matrícula.");
        }

        return new Matricula(numero);
    }

    public int CompareTo(Matricula? other) => other is null ? 1 : Numero.CompareTo(other.Numero);

    public override string ToString() => Numero.ToString(CultureInfo.InvariantCulture);

    public static bool operator <(Matricula esquerda, Matricula direita) => Comparar(esquerda, direita) < 0;

    public static bool operator >(Matricula esquerda, Matricula direita) => Comparar(esquerda, direita) > 0;

    public static bool operator <=(Matricula esquerda, Matricula direita) => Comparar(esquerda, direita) <= 0;

    public static bool operator >=(Matricula esquerda, Matricula direita) => Comparar(esquerda, direita) >= 0;

    private static int Comparar(Matricula esquerda, Matricula direita)
    {
        ArgumentNullException.ThrowIfNull(esquerda);

        return esquerda.CompareTo(direita);
    }
}
