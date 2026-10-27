using EscalaAvulsos.Dominio.Comum;

namespace EscalaAvulsos.Dominio.Identificacao;

/// <summary>
/// O número IMO do navio: sete dígitos, o último deles verificador.
/// </summary>
/// <remarks>
/// <para>
/// O número é do casco, e não muda enquanto o navio existir — nem quando ele
/// troca de nome, de dono ou de bandeira. É o que distingue dois navios de
/// mesmo nome, e o que se confere no AIS e nos documentos de bordo.
/// </para>
/// <para>
/// O verificador é o último algarismo da soma dos seis primeiros dígitos
/// multiplicados por 7, 6, 5, 4, 3 e 2: no IMO 9074729, 9×7 + 0×6 + 7×5 +
/// 4×4 + 7×3 + 2×2 = 139, e o verificador é 9.
/// </para>
/// </remarks>
public sealed record NumeroImo
{
    private NumeroImo(string numero) => Numero = numero;

    /// <summary>Os sete dígitos, sem o prefixo: <c>9074729</c>.</summary>
    public string Numero { get; }

    /// <summary>Lê o número com ou sem o prefixo: "IMO 9074729", "imo9074729", "9074729".</summary>
    public static NumeroImo De(string texto)
    {
        ArgumentNullException.ThrowIfNull(texto);

        var escrito = texto.Trim();
        var numero = escrito.StartsWith("IMO", StringComparison.OrdinalIgnoreCase) ? escrito[3..].TrimStart(' ', ':', '-') : escrito;

        if (numero.Length != 7 || !numero.All(char.IsAsciiDigit))
        {
            throw new ExcecaoDeDominio($"\"{escrito}\" não é um número IMO: são 7 dígitos, como IMO 9074729.");
        }

        if (numero.All(digito => digito == '0') || Verificador(numero) != numero[6])
        {
            throw new ExcecaoDeDominio($"O número IMO {escrito} não confere: o último dígito não bate com os outros seis.");
        }

        return new NumeroImo(numero);
    }

    /// <summary>"IMO 9074729".</summary>
    public override string ToString() => $"IMO {Numero}";

    private static char Verificador(string numero)
    {
        var soma = 0;

        for (var posicao = 0; posicao < 6; posicao++)
        {
            soma += (numero[posicao] - '0') * (7 - posicao);
        }

        return (char)('0' + (soma % 10));
    }
}
