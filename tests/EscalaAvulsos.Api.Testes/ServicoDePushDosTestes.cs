using System.Buffers.Text;
using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using EscalaAvulsos.Api.Avisos;
using EscalaAvulsos.Api.Recursos.Eu;

namespace EscalaAvulsos.Api.Testes;

/// <summary>
/// Um serviço de push de mentira, no lugar do Google e da Mozilla: guarda o
/// que recebeu, responde o que o teste mandar para cada endereço, e decifra a
/// mensagem como o navegador do exemplo da RFC 8291 faria — é com as chaves
/// dele que o navegador do teste se inscreve.
/// </summary>
internal sealed class ServicoDePushDosTestes : HttpMessageHandler
{
    public const string ChavePublica = "BCVxsr7N_eNgVRqvHtD0zTZsEc6-VV-JvLexhqUzORcxaOzi6-AYWXvTBHm4bjyPjs7Vd8pZGH6SRpkNtoIAiw4";

    public const string Segredo = "BTBZMqHH6r4Tts7J_aSIgg";

    private const string ChavePrivada = "q1dXpw3UpT5VOmu_cf_v6ih07Aems3njxI-JWgLcM94";

    private readonly ConcurrentDictionary<Uri, HttpStatusCode> _respostas = new();

    public ConcurrentQueue<(Uri Endereco, ConteudoDoPush Conteudo, IReadOnlyDictionary<string, string> Cabecalhos)> Recebidas { get; } = new();

    /// <summary>A inscrição de um navegador com as chaves do exemplo, num endereço só dele.</summary>
    public static InscricaoDoPushPedido Inscricao(string celular) => new($"https://push.example.net/push/{celular}", ChavePublica, Segredo);

    /// <summary>O que o serviço responde a esse endereço daqui em diante: 410 é a inscrição que venceu.</summary>
    public void Responder(string celular, HttpStatusCode status) => _respostas[new Uri($"https://push.example.net/push/{celular}")] = status;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var corpo = await request.Content!.ReadAsByteArrayAsync(cancellationToken);
        var cabecalhos = request.Headers.Concat(request.Content.Headers).ToDictionary(cabecalho => cabecalho.Key, cabecalho => string.Join(",", cabecalho.Value));

        Recebidas.Enqueue((request.RequestUri!, JsonSerializer.Deserialize<ConteudoDoPush>(Decifrar(corpo), JsonSerializerOptions.Web)!, cabecalhos));

        return new HttpResponseMessage(_respostas.GetValueOrDefault(request.RequestUri!, HttpStatusCode.Created));
    }

    private static byte[] Decifrar(byte[] mensagem)
    {
        var sal = mensagem[..16];
        var publicaDoServidor = mensagem[21..86];
        var chaveDoNavegador = Base64Url.DecodeFromChars(ChavePublica);

        using var doNavegador = ECDiffieHellman.Create(new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            D = Base64Url.DecodeFromChars(ChavePrivada),
            Q = new ECPoint { X = chaveDoNavegador[1..33], Y = chaveDoNavegador[33..65] },
        });
        using var doServidor = ECDiffieHellman.Create(new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            Q = new ECPoint { X = publicaDoServidor[1..33], Y = publicaDoServidor[33..65] },
        });

        var compartilhado = doNavegador.DeriveRawSecretAgreement(doServidor.PublicKey);
        byte[] info = [.. "WebPush: info\0"u8, .. chaveDoNavegador, .. publicaDoServidor];
        var ikm = HKDF.DeriveKey(HashAlgorithmName.SHA256, compartilhado, 32, Base64Url.DecodeFromChars(Segredo), info);
        var prk = HKDF.Extract(HashAlgorithmName.SHA256, ikm, sal);
        var chave = HKDF.Expand(HashAlgorithmName.SHA256, prk, 16, "Content-Encoding: aes128gcm\0"u8.ToArray());
        var nonce = HKDF.Expand(HashAlgorithmName.SHA256, prk, 12, "Content-Encoding: nonce\0"u8.ToArray());

        var claro = new byte[mensagem.Length - 86 - 16];
        using var aes = new AesGcm(chave, 16);
        aes.Decrypt(nonce, mensagem[86..^16], mensagem[^16..], claro);

        return claro[..^1];
    }
}
