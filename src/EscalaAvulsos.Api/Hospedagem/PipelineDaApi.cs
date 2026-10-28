using EscalaAvulsos.Api.Http;
using EscalaAvulsos.Api.Recursos;

namespace EscalaAvulsos.Api.Hospedagem;

/// <summary>O caminho de cada pedido, na ordem.</summary>
internal static class PipelineDaApi
{
    public static WebApplication UsarApi(this WebApplication app)
    {
        // Primeiro, quem transforma erro em ProblemDetails: exceção de
        // qualquer etapa adiante, e resposta de erro sem corpo — o 401 da
        // autenticação, o 403 da autorização, o 404 de rota.
        app.UseExceptionHandler();
        app.UseStatusCodePages();

        app.UseAuthentication();
        app.UseAuthorization();
        app.Use(GuardarCorpoDasCriacoes);

        app.MapOpenApi().AllowAnonymous();
        MapaDosRecursos.MapearTodos(app.MapGroup("/api"));

        return app;
    }

    /// <summary>
    /// Deixa o corpo dos pedidos com chave de idempotência pronto para ser
    /// lido duas vezes: uma pelo leitor de JSON, outra para reconhecer o mesmo
    /// pedido quando ele chegar de novo (ADR-0015).
    /// </summary>
    private static async Task GuardarCorpoDasCriacoes(HttpContext contexto, RequestDelegate proximo)
    {
        if (contexto.GetEndpoint()?.Metadata.GetMetadata<ExigeChaveDeIdempotencia>() is not null)
        {
            contexto.Request.EnableBuffering();
        }

        await proximo(contexto);
    }
}
