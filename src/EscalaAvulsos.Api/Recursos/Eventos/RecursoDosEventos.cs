using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using EscalaAvulsos.Api.Operacao;
using EscalaAvulsos.Api.Seguranca;
using EscalaAvulsos.Api.TempoReal;
using EscalaAvulsos.Dominio.Periodos;
using EscalaAvulsos.Dominio.Trabalhadores;
using EscalaAvulsos.Infraestrutura.Persistencia.Repositorios;
using EscalaAvulsos.Infraestrutura.TempoReal;
using Microsoft.AspNetCore.Http.HttpResults;

namespace EscalaAvulsos.Api.Recursos.Eventos;

/// <summary>
/// O que mudou, para o aplicativo aberto saber que precisa ler de novo: o
/// período da categoria, ou os avisos de quem está conectado. Nada do dado
/// vai junto — quem recebe pede pelo endereço de sempre (ADR-0020).
/// </summary>
/// <param name="Origem">O que mudou; nulo no <c>pronto</c> e no <c>vivo</c>.</param>
public sealed record EventoResposta(OrigemDoAnuncio? Origem, Categoria? Categoria, DateOnly? Dia, Turno? Turno)
{
    public static EventoResposta Vazio { get; } = new(null, null, null, null);
}

/// <summary>
/// Os eventos do servidor, em Server-Sent Events: <c>pronto</c> ao conectar
/// — e de novo, se a escuta do banco recomeçar —, <c>mudou</c> a cada
/// anúncio que é de quem está conectado, e <c>vivo</c> a cada vinte
/// segundos, para os proxies não fecharem a ligação parada.
/// </summary>
public sealed class RecursoDosEventos : IRecursoDaApi
{
    /// <summary>De quanto em quanto a ligação parada recebe sinal de vida.</summary>
    public static readonly TimeSpan IntervaloDoVivo = TimeSpan.FromSeconds(20);

    public void Mapear(IEndpointRouteBuilder api)
    {
        api.MapGet("/eventos", Transmitir).WithTags("Tempo real").RequireAuthorization(Politicas.Escutar);
    }

    private static async Task<ServerSentEventsResult<EventoResposta>> Transmitir(
        CentralDeAnuncios central,
        MedidasDoTempoReal medidas,
        RepositorioDeTrabalhadores trabalhadores,
        HttpContext http,
        CancellationToken cancelamento)
    {
        var filtro = await FiltroDe(http.User, trabalhadores, cancelamento);

        // Sem buffer no caminho: o proxy que segura a resposta seguraria o evento.
        http.Response.Headers.CacheControl = "no-cache";
        http.Response.Headers["X-Accel-Buffering"] = "no";

        return TypedResults.ServerSentEvents(Fluxo(central, medidas, filtro, http.RequestAborted));
    }

    private static async IAsyncEnumerable<SseItem<EventoResposta>> Fluxo(
        CentralDeAnuncios central,
        MedidasDoTempoReal medidas,
        Func<AnuncioDoBanco, bool> eDeQuemPede,
        [EnumeratorCancellation] CancellationToken cancelamento)
    {
        using var ouvinte = central.Escutar();
        using var ligada = medidas.Ligada();

        yield return new SseItem<EventoResposta>(EventoResposta.Vazio, "pronto");

        Task<bool>? temSinal = null;

        while (!cancelamento.IsCancellationRequested)
        {
            temSinal ??= ouvinte.Sinais.WaitToReadAsync(cancelamento).AsTask();
            var primeiro = await Task.WhenAny(temSinal, Task.Delay(IntervaloDoVivo, cancelamento));

            if (cancelamento.IsCancellationRequested)
            {
                yield break;
            }

            if (primeiro != temSinal)
            {
                yield return new SseItem<EventoResposta>(EventoResposta.Vazio, "vivo");
                continue;
            }

            if (!await temSinal)
            {
                yield break;
            }

            temSinal = null;

            while (ouvinte.Sinais.TryRead(out var sinal))
            {
                if (sinal.Anuncio is null)
                {
                    yield return new SseItem<EventoResposta>(EventoResposta.Vazio, "pronto");
                }
                else if (eDeQuemPede(sinal.Anuncio))
                {
                    medidas.Mudanca(sinal.Anuncio.Origem);
                    yield return new SseItem<EventoResposta>(Evento(sinal.Anuncio), "mudou");
                }
            }
        }
    }

    /// <summary>
    /// O que interessa a quem pede. O trabalhador: as escalas da categoria
    /// dele e os avisos dele — e não a habilitação de cada colega. O escalador
    /// e o operador: as escalações, as escalas e as requisições, para o quadro.
    /// </summary>
    private static async Task<Func<AnuncioDoBanco, bool>> FiltroDe(ClaimsPrincipal usuario, RepositorioDeTrabalhadores trabalhadores, CancellationToken cancelamento)
    {
        var doQuadro = usuario.IsInRole(Papeis.Escalador) || usuario.IsInRole(Papeis.Operador);
        Trabalhador? trabalhador = null;

        if (usuario.IsInRole(Papeis.Trabalhador) && QuemPede.LerMatricula(usuario) is { } matricula)
        {
            trabalhador = (await trabalhadores.ObterPelaMatricula(matricula, cancelamento))?.Agregado;
        }

        return anuncio => anuncio.Origem switch
        {
            OrigemDoAnuncio.Avisos => trabalhador is not null && anuncio.Trabalhador == trabalhador.Id,
            OrigemDoAnuncio.Escalas => doQuadro || (trabalhador is not null && anuncio.Categoria == trabalhador.Categoria),
            _ => doQuadro,
        };
    }

    private static EventoResposta Evento(AnuncioDoBanco anuncio)
    {
        if (anuncio.Inicio is not { } inicio)
        {
            return new EventoResposta(anuncio.Origem, null, null, null);
        }

        var periodo = Periodo.Contendo(inicio);

        return new EventoResposta(anuncio.Origem, anuncio.Categoria, periodo.Dia, periodo.Turno);
    }
}
