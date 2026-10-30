using System.Buffers.Binary;
using System.Security.Cryptography;

namespace EscalaAvulsos.Infraestrutura.Avisos;

/// <summary>
/// A mensagem de push cifrada para um navegador só, como a RFC 8291 manda:
/// uma chave de curva elíptica nova a cada mensagem, o segredo combinado por
/// ECDH com a chave do navegador, e o conteúdo em AES-128-GCM, num registro
/// só do formato <c>aes128gcm</c> (RFC 8188). O serviço de push do caminho
/// carrega a mensagem, mas não a lê.
/// </summary>
/// <remarks>
/// Feita com a criptografia do próprio .NET, sem biblioteca de fora: são
/// poucas linhas, e o teste as confere byte a byte com o exemplo da RFC.
/// </remarks>
public static class CifraDoPush
{
    /// <summary>O tamanho do registro: a mensagem inteira cabe num só.</summary>
    public const int TamanhoDoRegistro = 4096;

    /// <summary>
    /// O maior conteúdo que cabe: o registro, menos a etiqueta do AES-GCM e o
    /// delimitador. Os serviços de push aceitam corpos de até 4 KB.
    /// </summary>
    public const int TamanhoMaximoDoConteudo = 3993;

    private const int TamanhoDoSal = 16;

    private const int TamanhoDaEtiqueta = 16;

    /// <summary>Cifra o conteúdo para o navegador da inscrição.</summary>
    public static byte[] Cifrar(InscricaoDePush inscricao, ReadOnlySpan<byte> conteudo)
    {
        ArgumentNullException.ThrowIfNull(inscricao);

        using var doServidor = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);

        return Cifrar([.. inscricao.ChavePublica], [.. inscricao.Segredo], conteudo, doServidor, RandomNumberGenerator.GetBytes(TamanhoDoSal));
    }

    /// <summary>A cifra com a chave do servidor e o sal dados: é como o teste refaz o exemplo da RFC.</summary>
    internal static byte[] Cifrar(byte[] chaveDoNavegador, byte[] segredo, ReadOnlySpan<byte> conteudo, ECDiffieHellman doServidor, byte[] sal)
    {
        if (conteudo.Length > TamanhoMaximoDoConteudo)
        {
            throw new ArgumentException($"A mensagem de push passa de {TamanhoMaximoDoConteudo} bytes.", nameof(conteudo));
        }

        var publicaDoServidor = Ponto(doServidor.ExportParameters(includePrivateParameters: false));

        using var doNavegador = ECDiffieHellman.Create(ParametrosDe(chaveDoNavegador));
        var compartilhado = doServidor.DeriveRawSecretAgreement(doNavegador.PublicKey);

        // O segredo do navegador entra na combinação: sem ele, a chave do ECDH sozinha não decifra.
        byte[] infoDaChave = [.. "WebPush: info\0"u8, .. chaveDoNavegador, .. publicaDoServidor];
        var materialDaChave = HKDF.DeriveKey(HashAlgorithmName.SHA256, compartilhado, 32, segredo, infoDaChave);

        var pseudoaleatoria = HKDF.Extract(HashAlgorithmName.SHA256, materialDaChave, sal);
        var chaveDoConteudo = HKDF.Expand(HashAlgorithmName.SHA256, pseudoaleatoria, 16, "Content-Encoding: aes128gcm\0"u8.ToArray());
        var nonce = HKDF.Expand(HashAlgorithmName.SHA256, pseudoaleatoria, 12, "Content-Encoding: nonce\0"u8.ToArray());

        // O delimitador 0x02 diz que este é o último registro — e o único.
        byte[] claro = [.. conteudo, 0x02];
        var cifrado = new byte[claro.Length];
        var etiqueta = new byte[TamanhoDaEtiqueta];

        using (var aes = new AesGcm(chaveDoConteudo, TamanhoDaEtiqueta))
        {
            aes.Encrypt(nonce, claro, cifrado, etiqueta);
        }

        // O cabeçalho do aes128gcm: o sal, o tamanho do registro, e a chave do servidor, com que o navegador refaz o ECDH.
        var tamanho = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(tamanho, TamanhoDoRegistro);

        return [.. sal, .. tamanho, (byte)publicaDoServidor.Length, .. publicaDoServidor, .. cifrado, .. etiqueta];
    }

    /// <summary>A chave pública como ponto sem compressão: 0x04, X e Y.</summary>
    internal static byte[] Ponto(ECParameters parametros) => [0x04, .. parametros.Q.X!, .. parametros.Q.Y!];

    internal static ECParameters ParametrosDe(byte[] ponto) => new()
    {
        Curve = ECCurve.NamedCurves.nistP256,
        Q = new ECPoint { X = ponto[1..33], Y = ponto[33..65] },
    };
}
