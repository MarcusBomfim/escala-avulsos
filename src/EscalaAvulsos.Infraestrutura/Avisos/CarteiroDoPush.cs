using System.Globalization;
using System.Net;
using System.Net.Http.Headers;

namespace EscalaAvulsos.Infraestrutura.Avisos;

/// <summary>Com que pressa o serviço de push entrega: o celular em economia de bateria segura o que não é urgente (RFC 8030).</summary>
public enum UrgenciaDoPush
{
    Normal,
    Alta,
}

/// <summary>
/// O que vai para um navegador: o conteúdo, por quanto tempo ainda vale a
/// pena entregá-lo, e o tópico — o aviso novo do mesmo tópico substitui o
/// antigo que ainda não chegou.
/// </summary>
public sealed record MensagemDoPush(byte[] Conteudo, TimeSpan Validade, UrgenciaDoPush Urgencia, string? Topico = null);

/// <summary>O que o serviço de push respondeu.</summary>
public enum ResultadoDoPush
{
    /// <summary>Recebeu, e vai entregar quando o celular estiver ao alcance.</summary>
    Entregue,

    /// <summary>A inscrição não existe mais — o trabalhador desinstalou, trocou de celular, tirou a permissão: apaga-se.</summary>
    InscricaoVencida,

    /// <summary>Recusou desta vez: tenta-se de novo depois.</summary>
    Recusado,
}

/// <summary>O que o servidor precisa para mandar push: as chaves VAPID e o contato de quem manda (RFC 8292).</summary>
public sealed record ConfiguracaoDoPush(ChavesVapid Chaves, string Contato);

/// <summary>
/// Leva a mensagem cifrada ao serviço de push de cada navegador, assinada
/// com as chaves do servidor. Não guarda nada: quem decide o que mandar, e o
/// que fazer com a resposta, é quem chama.
/// </summary>
public sealed class CarteiroDoPush(HttpClient http, ConfiguracaoDoPush configuracao, TimeProvider relogio)
{
    public async Task<ResultadoDoPush> Entregar(InscricaoDePush inscricao, MensagemDoPush mensagem, CancellationToken cancelamento = default)
    {
        ArgumentNullException.ThrowIfNull(inscricao);
        ArgumentNullException.ThrowIfNull(mensagem);

        using var pedido = new HttpRequestMessage(HttpMethod.Post, inscricao.Endereco)
        {
            Content = new ByteArrayContent(CifraDoPush.Cifrar(inscricao, mensagem.Conteudo)),
        };

        pedido.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        pedido.Content.Headers.ContentEncoding.Add("aes128gcm");
        pedido.Headers.TryAddWithoutValidation("Authorization", configuracao.Chaves.Autorizacao(inscricao.Endereco, configuracao.Contato, relogio.GetUtcNow()));
        pedido.Headers.Add("TTL", ((long)Math.Max(0, mensagem.Validade.TotalSeconds)).ToString(CultureInfo.InvariantCulture));
        pedido.Headers.Add("Urgency", mensagem.Urgencia == UrgenciaDoPush.Alta ? "high" : "normal");

        if (mensagem.Topico is { } topico)
        {
            pedido.Headers.Add("Topic", topico);
        }

        using var resposta = await http.SendAsync(pedido, cancelamento);

        return resposta.StatusCode switch
        {
            HttpStatusCode.Created or HttpStatusCode.OK or HttpStatusCode.Accepted => ResultadoDoPush.Entregue,
            HttpStatusCode.NotFound or HttpStatusCode.Gone => ResultadoDoPush.InscricaoVencida,
            _ => ResultadoDoPush.Recusado,
        };
    }
}
