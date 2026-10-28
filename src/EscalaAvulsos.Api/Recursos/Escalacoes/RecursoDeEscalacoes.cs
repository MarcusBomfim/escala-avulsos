using EscalaAvulsos.Api.Http;
using EscalaAvulsos.Api.Hospedagem;
using EscalaAvulsos.Api.Recursos.Requisicoes;
using EscalaAvulsos.Api.Seguranca;
using EscalaAvulsos.Dominio.Escalacoes;
using EscalaAvulsos.Dominio.Trabalhadores;
using EscalaAvulsos.Infraestrutura.Persistencia;
using EscalaAvulsos.Infraestrutura.Persistencia.Repositorios;
using Microsoft.AspNetCore.Http.HttpResults;

namespace EscalaAvulsos.Api.Recursos.Escalacoes;

/// <summary>
/// As escalações de cada categoria e período, pelo órgão gestor: abrir, ver
/// quem se habilitou, e as requisições que vão entrar na escala. O endereço é
/// a identidade: <c>/api/escalacoes/Estiva/2026-11-05/Tarde</c>.
/// </summary>
public sealed class RecursoDeEscalacoes : IRecursoDaApi
{
    public void Mapear(IEndpointRouteBuilder api)
    {
        var escalacao = api.MapGroup("/escalacoes/{categoria}/{dia}/{turno}").WithTags("Escalações").RequireAuthorization(Politicas.Escalar);

        escalacao.MapPut("/", Abrir)
            .DispensandoAVersaoLida("A categoria e o período identificam a escalação: abrir de novo devolve a que já está aberta, sem abrir outra.");
        escalacao.MapGet("/", Obter);
        escalacao.MapGet("/requisicoes", Requisicoes);
    }

    private static async Task<Results<Created<EscalacaoResposta>, Ok<EscalacaoResposta>>> Abrir(
        string categoria,
        string dia,
        string turno,
        RepositorioDeEscalacoes repositorio,
        ConfiguracaoDoPorto porto,
        HttpContext http,
        CancellationToken cancelamento)
    {
        var daCategoria = categoria.ComoOpcao<Categoria>("categoria");
        var periodo = TextosDoPedido.ComoPeriodo(dia, turno);

        if (await repositorio.Da(daCategoria, periodo, cancelamento) is { } aberta)
        {
            return JaAberta(aberta, http);
        }

        var escalacao = new Escalacao(daCategoria, periodo, porto.Calendario);

        try
        {
            Etiquetas.Escrever(http, await repositorio.Adicionar(escalacao, cancelamento));
        }
        catch (AgregadoJaExiste)
        {
            // Outro abriu a mesma no meio: vale a dele.
            return JaAberta(await LeituraDaEscalacao.Ler(repositorio, daCategoria, periodo, cancelamento), http);
        }

        return TypedResults.Created($"/api/escalacoes/{categoria}/{dia}/{turno}", EscalacaoResposta.De(escalacao));
    }

    private static async Task<Results<Ok<EscalacaoResposta>, StatusCodeHttpResult>> Obter(
        string categoria,
        string dia,
        string turno,
        RepositorioDeEscalacoes repositorio,
        HttpContext http,
        CancellationToken cancelamento)
    {
        var lida = await LeituraDaEscalacao.Ler(repositorio, categoria.ComoOpcao<Categoria>("categoria"), TextosDoPedido.ComoPeriodo(dia, turno), cancelamento);
        Etiquetas.Escrever(http, lida.Versao);

        return Etiquetas.ClienteJaTem(http, lida.Versao)
            ? TypedResults.StatusCode(StatusCodes.Status304NotModified)
            : TypedResults.Ok(EscalacaoResposta.De(lida.Agregado));
    }

    /// <summary>As requisições da categoria para o período, todas — as canceladas e as que chegaram depois do prazo também, como a escala vai vê-las.</summary>
    private static async Task<Ok<IReadOnlyList<RequisicaoResposta>>> Requisicoes(
        string categoria,
        string dia,
        string turno,
        RepositorioDeRequisicoes repositorio,
        CancellationToken cancelamento)
    {
        var requisicoes = await repositorio.DoPeriodo(categoria.ComoOpcao<Categoria>("categoria"), TextosDoPedido.ComoPeriodo(dia, turno), cancelamento);

        return TypedResults.Ok<IReadOnlyList<RequisicaoResposta>>([.. requisicoes.Select(RequisicaoResposta.De)]);
    }

    private static Ok<EscalacaoResposta> JaAberta(Versionado<Escalacao> aberta, HttpContext http)
    {
        Etiquetas.Escrever(http, aberta.Versao);

        return TypedResults.Ok(EscalacaoResposta.De(aberta.Agregado));
    }
}
