using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace EscalaAvulsos.Api.Testes;

/// <summary>
/// O jeito de os testes falarem com a API: com os cabeçalhos dela —
/// <c>If-Match</c>, <c>Idempotency-Key</c> — e o JSON dela.
/// </summary>
internal static class Pedidos
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    private static CancellationToken Cancelamento => TestContext.Current.CancellationToken;

    /// <param name="corpo">Um objeto, serializado em JSON; ou uma string, enviada como está.</param>
    /// <param name="versao">A versão lida, para o <c>If-Match</c>.</param>
    /// <param name="chave">A chave de idempotência.</param>
    public static async Task<HttpResponseMessage> Enviar(
        this HttpClient cliente,
        HttpMethod metodo,
        string caminho,
        object? corpo = null,
        int? versao = null,
        string? chave = null)
    {
        using var pedido = new HttpRequestMessage(metodo, caminho);

        pedido.Content = corpo switch
        {
            null => null,
            string json => new StringContent(json, Encoding.UTF8, "application/json"),
            _ => JsonContent.Create(corpo, options: Json),
        };

        if (versao is not null)
        {
            pedido.Headers.IfMatch.Add(new EntityTagHeaderValue($"\"{versao}\""));
        }

        if (chave is not null)
        {
            pedido.Headers.Add("Idempotency-Key", chave);
        }

        return await cliente.SendAsync(pedido, Cancelamento);
    }

    public static Task<HttpResponseMessage> Ler(this HttpClient cliente, string caminho) => cliente.GetAsync(caminho, Cancelamento);

    public static async Task<T> Ler<T>(this HttpResponseMessage resposta) =>
        (await resposta.Content.ReadFromJsonAsync<T>(Json, Cancelamento))!;

    public static Task<ProblemDetails> Problema(this HttpResponseMessage resposta) => resposta.Ler<ProblemDetails>();

    /// <summary>A versão que veio na ETag.</summary>
    public static int Versao(this HttpResponseMessage resposta) =>
        int.Parse(resposta.Headers.ETag!.Tag.Trim('"'), CultureInfo.InvariantCulture);
}

/// <summary>
/// Os passos que um teste espera que deem certo — e que, se não derem,
/// falham dizendo o que a API respondeu, em vez de quebrar mais adiante.
/// </summary>
internal static class Conferencias
{
    private static CancellationToken Cancelamento => TestContext.Current.CancellationToken;

    /// <summary>O pedido deu certo; senão, o teste falha com o status e o corpo da resposta.</summary>
    public static async Task<HttpResponseMessage> ComSucesso(this Task<HttpResponseMessage> pedido)
    {
        var resposta = await pedido;

        Assert.True(
            resposta.IsSuccessStatusCode,
            $"{resposta.RequestMessage?.Method} {resposta.RequestMessage?.RequestUri}: {(int)resposta.StatusCode} {await resposta.Content.ReadAsStringAsync(Cancelamento)}");

        return resposta;
    }

    /// <summary>O corpo da resposta de sucesso.</summary>
    public static async Task<T> LerComSucesso<T>(this Task<HttpResponseMessage> pedido) => await (await pedido.ComSucesso()).Ler<T>();
}
