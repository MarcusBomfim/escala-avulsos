using EscalaAvulsos.Api.Http;
using EscalaAvulsos.Api.Recursos.Trabalhadores;
using EscalaAvulsos.Api.Seguranca;
using EscalaAvulsos.Infraestrutura.Persistencia.Repositorios;
using Microsoft.AspNetCore.Http.HttpResults;

namespace EscalaAvulsos.Api.Recursos.Eu;

/// <summary>
/// O trabalhador vendo o próprio cadastro: as funções, o exame, os
/// impedimentos. Em <c>/api/eu</c> ninguém diz de quem é o pedido — a
/// matrícula vem do token, e um trabalhador não alcança os dados de outro
/// (ADR-0014).
/// </summary>
public sealed class RecursoDoMeuCadastro : IRecursoDaApi
{
    public void Mapear(IEndpointRouteBuilder api)
    {
        api.MapGet("/eu", Obter).WithTags("Eu").RequireAuthorization(Politicas.Trabalhador);
    }

    private static async Task<Results<Ok<TrabalhadorResposta>, StatusCodeHttpResult>> Obter(
        RepositorioDeTrabalhadores repositorio,
        HttpContext http,
        CancellationToken cancelamento)
    {
        var lido = await Leituras.Trabalhador(repositorio, QuemPede.Matricula(http.User), cancelamento);
        Etiquetas.Escrever(http, lido.Versao);

        return Etiquetas.ClienteJaTem(http, lido.Versao)
            ? TypedResults.StatusCode(StatusCodes.Status304NotModified)
            : TypedResults.Ok(TrabalhadorResposta.De(lido.Agregado));
    }
}
