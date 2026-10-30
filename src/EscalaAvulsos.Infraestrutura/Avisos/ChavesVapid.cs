using System.Buffers.Text;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace EscalaAvulsos.Infraestrutura.Avisos;

/// <summary>
/// O par de chaves com que o servidor se identifica aos serviços de push
/// (VAPID, RFC 8292): a pública vai para o navegador na inscrição, e a
/// privada assina cada envio. Quem não tem a privada não manda push para os
/// inscritos deste servidor.
/// </summary>
public sealed record ChavesVapid
{
    private ChavesVapid(byte[] publica, byte[] privada)
    {
        Publica = publica;
        Privada = privada;
    }

    public IReadOnlyList<byte> Publica { get; }

    private byte[] Privada { get; }

    /// <summary>A chave pública como o navegador a pede em <c>applicationServerKey</c>, em base64url.</summary>
    public string PublicaEmBase64Url => Base64Url.EncodeToString([.. Publica]);

    public string PrivadaEmBase64Url => Base64Url.EncodeToString(Privada);

    /// <summary>Um par novo: o comando de quem implanta gera uma vez, e a configuração guarda.</summary>
    public static ChavesVapid Gerar()
    {
        using var chave = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var parametros = chave.ExportParameters(includePrivateParameters: true);

        return new ChavesVapid(CifraDoPush.Ponto(parametros), parametros.D!);
    }

    /// <summary>Lê o par da configuração, em base64url, e confere que a privada é mesmo a da pública.</summary>
    /// <exception cref="FormatException">As chaves não são um par P-256.</exception>
    public static ChavesVapid Ler(string publica, string privada)
    {
        try
        {
            var ponto = Base64Url.DecodeFromChars(publica);
            var d = Base64Url.DecodeFromChars(privada);

            if (ponto.Length != InscricaoDePush.TamanhoDaChavePublica || ponto[0] != 0x04 || d.Length != 32)
            {
                throw new FormatException("As chaves VAPID têm de ser um par P-256: a pública sem compressão, de 65 bytes, e a privada de 32.");
            }

            var chaves = new ChavesVapid(ponto, d);

            // A privada assina, e a pública confere: um par trocado não sobe.
            using var assinador = chaves.Assinador();
            using var conferente = ECDsa.Create(CifraDoPush.ParametrosDe(ponto));
            var prova = assinador.SignData("VAPID"u8, HashAlgorithmName.SHA256);

            return conferente.VerifyData("VAPID"u8, prova, HashAlgorithmName.SHA256)
                ? chaves
                : throw new FormatException("As chaves VAPID não formam um par: a privada não é a da pública.");
        }
        catch (CryptographicException erro)
        {
            throw new FormatException("As chaves VAPID não formam um par: a privada não é a da pública.", erro);
        }
    }

    /// <summary>
    /// O cabeçalho <c>Authorization</c> de um envio: um JWT assinado em ES256 para
    /// o serviço de push do endereço, que vale por doze horas, com o contato
    /// de quem manda — e a chave pública que o confere.
    /// </summary>
    /// <param name="contato">"mailto:…" ou "https://…": quem o serviço de push procura se algo der errado.</param>
    public string Autorizacao(Uri endereco, string contato, DateTimeOffset agora)
    {
        ArgumentNullException.ThrowIfNull(endereco);

        var cabecalho = Base64Url.EncodeToString("""{"typ":"JWT","alg":"ES256"}"""u8);
        var reivindicacoes = Base64Url.EncodeToString(JsonSerializer.SerializeToUtf8Bytes(new Dictionary<string, object>
        {
            ["aud"] = endereco.GetLeftPart(UriPartial.Authority),
            ["exp"] = (agora + TimeSpan.FromHours(12)).ToUnixTimeSeconds(),
            ["sub"] = contato,
        }));

        var assinado = $"{cabecalho}.{reivindicacoes}";

        using var assinador = Assinador();
        var assinatura = Base64Url.EncodeToString(assinador.SignData(Encoding.ASCII.GetBytes(assinado), HashAlgorithmName.SHA256));

        return string.Create(CultureInfo.InvariantCulture, $"vapid t={assinado}.{assinatura}, k={PublicaEmBase64Url}");
    }

    private ECDsa Assinador()
    {
        var parametros = CifraDoPush.ParametrosDe([.. Publica]);
        parametros.D = Privada;

        return ECDsa.Create(parametros);
    }
}
