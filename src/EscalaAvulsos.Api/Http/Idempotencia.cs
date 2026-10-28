using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using EscalaAvulsos.Infraestrutura.Persistencia.Idempotencia;
using Microsoft.Net.Http.Headers;

namespace EscalaAvulsos.Api.Http;

/// <summary>
/// Os pedidos que criam um registro sem identidade natural, atendidos uma
/// vez só por chave de idempotência (ADR-0015).
/// </summary>
/// <remarks>
/// <para>
/// O endpoint que cria declara <see cref="ComIdempotencia"/>. O filtro tira
/// a impressão do pedido — método, caminho e corpo —, e entrega a execução
/// do endpoint às chaves de idempotência, que a fazem na mesma transação da
/// chave. A resposta é escrita num buffer, guardada e devolvida; o pedido
/// repetido recebe a guardada, igual, com <c>Idempotent-Replayed: true</c>.
/// </para>
/// <para>
/// A chave é obrigatória: sem ela, 400. Uma repetição sem chave seria uma
/// criação nova — a mesma requisição de mão de obra duas vezes —, e quem a
/// mandou não teria como saber.
/// </para>
/// </remarks>
internal static class Idempotencia
{
    /// <summary>Os cabeçalhos que fazem parte do resultado, e voltam iguais na repetição.</summary>
    private static readonly string[] CabecalhosGuardados = [HeaderNames.Location, HeaderNames.ETag, HeaderNames.ContentType];

    public static RouteHandlerBuilder ComIdempotencia(this RouteHandlerBuilder endpoint) =>
        endpoint.WithMetadata(ExigeChaveDeIdempotencia.Instancia).AddEndpointFilter(Atender);

    private static async ValueTask<object?> Atender(EndpointFilterInvocationContext invocacao, EndpointFilterDelegate proximo)
    {
        var http = invocacao.HttpContext;
        var chave = LerChave(http);
        var impressao = await Impressao(http);
        var dono = http.User.FindFirstValue("sub") ?? throw new PedidoInvalido("O token não traz quem pede, na reivindicação sub.");
        var chaves = http.RequestServices.GetRequiredService<ChavesDeIdempotencia>();

        var atendido = await chaves.Atender(
            dono,
            chave,
            impressao,
            async _ => await Executar(await proximo(invocacao), http),
            http.RequestAborted);

        return new RespostaDoPedido(atendido);
    }

    private static string LerChave(HttpContext http)
    {
        var valores = http.Request.Headers[Cabecalhos.ChaveDeIdempotencia];

        if (valores.Count != 1 || string.IsNullOrWhiteSpace(valores[0]))
        {
            throw new PedidoInvalido(
                $"Todo pedido que cria leva o cabeçalho {Cabecalhos.ChaveDeIdempotencia}: um valor único por operação — um UUID, por exemplo —, repetido em cada nova tentativa dela.");
        }

        var chave = valores[0]!;

        return chave.Length <= ChavesDeIdempotencia.TamanhoMaximoDaChave
            ? chave
            : throw new PedidoInvalido($"O {Cabecalhos.ChaveDeIdempotencia} tem mais de {ChavesDeIdempotencia.TamanhoMaximoDaChave} caracteres.");
    }

    /// <summary>O que identifica o pedido: o método, o caminho e o corpo, byte a byte, num SHA-256.</summary>
    private static async Task<string> Impressao(HttpContext http)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        hash.AppendData(Encoding.UTF8.GetBytes($"{http.Request.Method} {http.Request.Path}{http.Request.QueryString}\n"));

        var corpo = http.Request.Body;
        corpo.Position = 0;
        var pedaco = new byte[8192];
        int lidos;

        while ((lidos = await corpo.ReadAsync(pedaco, http.RequestAborted)) > 0)
        {
            hash.AppendData(pedaco, 0, lidos);
        }

        corpo.Position = 0;

        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }

    /// <summary>Executa o resultado do endpoint num buffer, para guardar exatamente o que seria enviado.</summary>
    private static async Task<RespostaGuardada> Executar(object? resultado, HttpContext http)
    {
        var original = http.Response.Body;
        using var buffer = new MemoryStream();
        http.Response.Body = buffer;

        try
        {
            var comoResultado = resultado as IResult ?? Results.Ok(resultado);
            await comoResultado.ExecuteAsync(http);
        }
        finally
        {
            http.Response.Body = original;
        }

        var cabecalhos = CabecalhosGuardados
            .Where(nome => http.Response.Headers.ContainsKey(nome))
            .ToDictionary(nome => nome, nome => http.Response.Headers[nome].ToString());

        return new RespostaGuardada(http.Response.StatusCode, cabecalhos, Encoding.UTF8.GetString(buffer.ToArray()));
    }

    /// <summary>Escreve a resposta atendida — a da operação, ou a guardada da primeira vez.</summary>
    private sealed class RespostaDoPedido(PedidoAtendido atendido) : IResult, IStatusCodeHttpResult
    {
        public int? StatusCode => atendido.Resposta.Status;

        public async Task ExecuteAsync(HttpContext httpContext)
        {
            var resposta = atendido.Resposta;
            httpContext.Response.StatusCode = resposta.Status;

            foreach (var (nome, valor) in resposta.Cabecalhos)
            {
                httpContext.Response.Headers[nome] = valor;
            }

            if (atendido.Repetido)
            {
                httpContext.Response.Headers[Cabecalhos.Repetido] = "true";
            }

            if (resposta.Corpo.Length > 0)
            {
                await httpContext.Response.WriteAsync(resposta.Corpo, Encoding.UTF8);
            }
        }
    }
}
