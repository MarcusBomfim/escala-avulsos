using System.Buffers.Text;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EscalaAvulsos.Infraestrutura.Avisos;

namespace EscalaAvulsos.Infraestrutura.Testes.Avisos;

/*
 * O push do jeito que a RFC 8291 manda, conferido com o exemplo dela, byte a
 * byte; a assinatura VAPID da RFC 8292, conferida com a chave pública; e o
 * envio, com o que cada resposta do serviço de push quer dizer.
 */
public sealed class PushTests
{
    private const string ChaveDoNavegador = "BCVxsr7N_eNgVRqvHtD0zTZsEc6-VV-JvLexhqUzORcxaOzi6-AYWXvTBHm4bjyPjs7Vd8pZGH6SRpkNtoIAiw4";

    private const string SegredoDoNavegador = "BTBZMqHH6r4Tts7J_aSIgg";

    [Fact]
    public void A_mensagem_cifrada_e_a_do_exemplo_da_rfc_8291()
    {
        using var doServidor = ECDiffieHellman.Create(new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            D = Base64Url.DecodeFromChars("yfWPiYE-n46HLnH0KqZOF1fJJU3MYrct3AELtAQ-oRw"),
            Q = Ponto("BP4z9KsN6nGRTbVYI_c7VJSPQTBtkgcy27mlmlMoZIIgDll6e3vCYLocInmYWAmS6TlzAC8wEqKK6PBru3jl7A8"),
        });

        var cifrada = CifraDoPush.Cifrar(
            Base64Url.DecodeFromChars(ChaveDoNavegador),
            Base64Url.DecodeFromChars(SegredoDoNavegador),
            "When I grow up, I want to be a watermelon"u8,
            doServidor,
            Base64Url.DecodeFromChars("DGv6ra1nlYgDCS1FRnbzlw"));

        Assert.Equal(
            "DGv6ra1nlYgDCS1FRnbzlwAAEABBBP4z9KsN6nGRTbVYI_c7VJSPQTBtkgcy27ml" +
            "mlMoZIIgDll6e3vCYLocInmYWAmS6TlzAC8wEqKK6PBru3jl7A_yl95bQpu6cVPT" +
            "pK4Mqgkf1CXztLVBSt2Ks3oZwbuwXPXLWyouBWLVWGNWQexSgSxsj_Qulcy4a-fN",
            Base64Url.EncodeToString(cifrada));
    }

    [Fact]
    public void Cada_mensagem_sai_com_uma_chave_e_um_sal_novos_e_o_navegador_a_decifra()
    {
        var inscricao = InscricaoDePush.De("https://push.example.net/push/x", ChaveDoNavegador, SegredoDoNavegador);
        var conteudo = """{"titulo":"Você vai trabalhar"}"""u8.ToArray();

        var uma = CifraDoPush.Cifrar(inscricao, conteudo);
        var outra = CifraDoPush.Cifrar(inscricao, conteudo);

        Assert.NotEqual(uma, outra);
        Assert.Equal(conteudo, Decifrar(uma));
        Assert.Throws<ArgumentException>(() => CifraDoPush.Cifrar(inscricao, new byte[CifraDoPush.TamanhoMaximoDoConteudo + 1]));
    }

    [Fact]
    public void A_assinatura_vapid_vale_para_o_servico_de_push_do_endereco_por_doze_horas_e_a_chave_publica_confere()
    {
        var chaves = ChavesVapid.Gerar();
        var agora = new DateTimeOffset(2026, 11, 5, 10, 0, 0, TimeSpan.FromHours(-3));

        var autorizacao = chaves.Autorizacao(new Uri("https://fcm.googleapis.com/fcm/send/abc"), "mailto:escala@example.com", agora);

        var partes = autorizacao.Split(", ");
        Assert.StartsWith("vapid t=", partes[0], StringComparison.Ordinal);
        Assert.Equal($"k={chaves.PublicaEmBase64Url}", partes[1]);

        var token = partes[0]["vapid t=".Length..].Split('.');
        using var reivindicacoes = JsonDocument.Parse(Base64Url.DecodeFromChars(token[1]));
        Assert.Equal("https://fcm.googleapis.com", reivindicacoes.RootElement.GetProperty("aud").GetString());
        Assert.Equal(agora.AddHours(12).ToUnixTimeSeconds(), reivindicacoes.RootElement.GetProperty("exp").GetInt64());
        Assert.Equal("mailto:escala@example.com", reivindicacoes.RootElement.GetProperty("sub").GetString());

        using var conferente = ECDsa.Create(new ECParameters { Curve = ECCurve.NamedCurves.nistP256, Q = Ponto(chaves.PublicaEmBase64Url) });
        Assert.True(conferente.VerifyData(Encoding.ASCII.GetBytes($"{token[0]}.{token[1]}"), Base64Url.DecodeFromChars(token[2]), HashAlgorithmName.SHA256));
    }

    [Fact]
    public void As_chaves_da_configuracao_sao_conferidas_como_par()
    {
        var chaves = ChavesVapid.Gerar();
        var outras = ChavesVapid.Gerar();

        Assert.Equal(chaves.PublicaEmBase64Url, ChavesVapid.Ler(chaves.PublicaEmBase64Url, chaves.PrivadaEmBase64Url).PublicaEmBase64Url);
        Assert.Throws<FormatException>(() => ChavesVapid.Ler(chaves.PublicaEmBase64Url, outras.PrivadaEmBase64Url));
        Assert.Throws<FormatException>(() => ChavesVapid.Ler("BCVx", chaves.PrivadaEmBase64Url));
    }

    [Theory]
    [InlineData(HttpStatusCode.Created, ResultadoDoPush.Entregue)]
    [InlineData(HttpStatusCode.Gone, ResultadoDoPush.InscricaoVencida)]
    [InlineData(HttpStatusCode.NotFound, ResultadoDoPush.InscricaoVencida)]
    [InlineData(HttpStatusCode.TooManyRequests, ResultadoDoPush.Recusado)]
    public async Task O_carteiro_manda_cifrado_e_assinado_e_diz_o_que_o_servico_respondeu(HttpStatusCode status, ResultadoDoPush resultado)
    {
        var servico = new ServicoDePushDeMentira(status);
        var configuracao = new ConfiguracaoDoPush(ChavesVapid.Gerar(), "mailto:escala@example.com");
        var carteiro = new CarteiroDoPush(new HttpClient(servico), configuracao, TimeProvider.System);
        var inscricao = InscricaoDePush.De("https://push.example.net/push/x", ChaveDoNavegador, SegredoDoNavegador);
        var conteudo = """{"titulo":"A habilitação vai fechar"}"""u8.ToArray();

        var respondido = await carteiro.Entregar(
            inscricao,
            new MensagemDoPush(conteudo, TimeSpan.FromMinutes(50), UrgenciaDoPush.Alta, "habilitacao"),
            TestContext.Current.CancellationToken);

        Assert.Equal(resultado, respondido);
        var pedido = servico.Recebido!;
        Assert.Equal(inscricao.Endereco, pedido.Endereco);
        Assert.Equal(("3000", "high", "habilitacao", "aes128gcm"), (pedido.Ttl, pedido.Urgencia, pedido.Topico, pedido.Codificacao));
        Assert.StartsWith("vapid t=", pedido.Autorizacao, StringComparison.Ordinal);
        Assert.Equal(conteudo, Decifrar(pedido.Corpo));
    }

    /// <summary>O que o navegador do exemplo da RFC faz com a mensagem: refaz a chave pela dele e decifra.</summary>
    private static byte[] Decifrar(byte[] mensagem)
    {
        var sal = mensagem[..16];
        var publicaDoServidor = mensagem[21..86];
        var cifrado = mensagem[86..^16];
        var etiqueta = mensagem[^16..];
        var chaveDoNavegador = Base64Url.DecodeFromChars(ChaveDoNavegador);

        using var doNavegador = ECDiffieHellman.Create(new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            D = Base64Url.DecodeFromChars("q1dXpw3UpT5VOmu_cf_v6ih07Aems3njxI-JWgLcM94"),
            Q = Ponto(ChaveDoNavegador),
        });
        using var doServidor = ECDiffieHellman.Create(new ECParameters { Curve = ECCurve.NamedCurves.nistP256, Q = Ponto(Base64Url.EncodeToString(publicaDoServidor)) });

        var compartilhado = doNavegador.DeriveRawSecretAgreement(doServidor.PublicKey);
        byte[] info = [.. "WebPush: info\0"u8, .. chaveDoNavegador, .. publicaDoServidor];
        var ikm = HKDF.DeriveKey(HashAlgorithmName.SHA256, compartilhado, 32, Base64Url.DecodeFromChars(SegredoDoNavegador), info);
        var prk = HKDF.Extract(HashAlgorithmName.SHA256, ikm, sal);
        var chave = HKDF.Expand(HashAlgorithmName.SHA256, prk, 16, "Content-Encoding: aes128gcm\0"u8.ToArray());
        var nonce = HKDF.Expand(HashAlgorithmName.SHA256, prk, 12, "Content-Encoding: nonce\0"u8.ToArray());

        var claro = new byte[cifrado.Length];
        using var aes = new AesGcm(chave, 16);
        aes.Decrypt(nonce, cifrado, etiqueta, claro);

        Assert.Equal(0x02, claro[^1]);

        return claro[..^1];
    }

    private static ECPoint Ponto(string base64Url)
    {
        var ponto = Base64Url.DecodeFromChars(base64Url);

        return new ECPoint { X = ponto[1..33], Y = ponto[33..65] };
    }

    private sealed record PedidoRecebido(Uri Endereco, string? Ttl, string? Urgencia, string? Topico, string? Codificacao, string? Autorizacao, byte[] Corpo);

    /// <summary>Um serviço de push que guarda o que recebeu e responde o que o teste manda.</summary>
    private sealed class ServicoDePushDeMentira(HttpStatusCode status) : HttpMessageHandler
    {
        public PedidoRecebido? Recebido { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            static string? Um(IEnumerable<string>? valores) => valores?.SingleOrDefault();

            Recebido = new PedidoRecebido(
                request.RequestUri!,
                Um(request.Headers.TryGetValues("TTL", out var ttl) ? ttl : null),
                Um(request.Headers.TryGetValues("Urgency", out var urgencia) ? urgencia : null),
                Um(request.Headers.TryGetValues("Topic", out var topico) ? topico : null),
                Um(request.Content!.Headers.ContentEncoding),
                Um(request.Headers.TryGetValues("Authorization", out var autorizacao) ? autorizacao : null),
                await request.Content.ReadAsByteArrayAsync(cancellationToken));

            return new HttpResponseMessage(status);
        }
    }
}
