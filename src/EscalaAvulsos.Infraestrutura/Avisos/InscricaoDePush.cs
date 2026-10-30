using System.Buffers.Text;

namespace EscalaAvulsos.Infraestrutura.Avisos;

/// <summary>
/// Um navegador que aceitou receber os avisos: o endereço do serviço de push
/// dele — do Google, da Mozilla, da Apple —, a chave pública com que a
/// mensagem é cifrada e o segredo que só ele e o servidor conhecem (RFC 8030,
/// RFC 8291). É o que o navegador entrega em <c>PushSubscription</c>.
/// </summary>
public sealed record InscricaoDePush
{
    /// <summary>A chave pública P-256 do navegador, sem compressão: 0x04 e as duas coordenadas.</summary>
    public const int TamanhoDaChavePublica = 65;

    public const int TamanhoDoSegredo = 16;

    private InscricaoDePush(Uri endereco, byte[] chavePublica, byte[] segredo)
    {
        Endereco = endereco;
        ChavePublica = chavePublica;
        Segredo = segredo;
    }

    public Uri Endereco { get; }

    public IReadOnlyList<byte> ChavePublica { get; }

    public IReadOnlyList<byte> Segredo { get; }

    /// <summary>"BCVxsr7N…": a chave pública como o navegador a dá, em base64url.</summary>
    public string ChavePublicaEmBase64Url => Base64Url.EncodeToString([.. ChavePublica]);

    public string SegredoEmBase64Url => Base64Url.EncodeToString([.. Segredo]);

    /// <summary>Lê a inscrição como o navegador a manda: o endereço e as chaves em base64url.</summary>
    /// <exception cref="FormatException">O endereço não é HTTPS, ou as chaves não têm o tamanho e a forma de uma inscrição de push.</exception>
    public static InscricaoDePush De(string endereco, string chavePublica, string segredo)
    {
        if (!Uri.TryCreate(endereco, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new FormatException("O endereço da inscrição de push tem de ser um endereço HTTPS.");
        }

        var chave = Decodificar(chavePublica, "A chave pública (p256dh)");
        var auth = Decodificar(segredo, "O segredo (auth)");

        if (chave.Length != TamanhoDaChavePublica || chave[0] != 0x04)
        {
            throw new FormatException($"A chave pública (p256dh) tem de ser um ponto P-256 sem compressão, de {TamanhoDaChavePublica} bytes.");
        }

        if (auth.Length != TamanhoDoSegredo)
        {
            throw new FormatException($"O segredo (auth) tem de ter {TamanhoDoSegredo} bytes.");
        }

        return new InscricaoDePush(uri, chave, auth);
    }

    private static byte[] Decodificar(string texto, string oQue)
    {
        try
        {
            return Base64Url.DecodeFromChars(texto);
        }
        catch (FormatException erro)
        {
            throw new FormatException($"{oQue} não está em base64url.", erro);
        }
    }
}
