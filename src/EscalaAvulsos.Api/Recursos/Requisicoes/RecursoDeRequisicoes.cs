using EscalaAvulsos.Api.Http;
using EscalaAvulsos.Api.Seguranca;
using EscalaAvulsos.Dominio.Identificacao;
using EscalaAvulsos.Dominio.Periodos;
using EscalaAvulsos.Dominio.Requisicoes;
using EscalaAvulsos.Dominio.Trabalhadores;
using EscalaAvulsos.Infraestrutura.Persistencia;
using EscalaAvulsos.Infraestrutura.Persistencia.Repositorios;
using Microsoft.AspNetCore.Http.HttpResults;

namespace EscalaAvulsos.Api.Recursos.Requisicoes;

/// <summary>
/// As requisições do operador portuário: o pedido de mão de obra, a leitura e
/// o cancelamento. Cada operador só enxerga as do CNPJ dele.
/// </summary>
public sealed class RecursoDeRequisicoes : IRecursoDaApi
{
    public void Mapear(IEndpointRouteBuilder api)
    {
        var requisicoes = api.MapGroup("/requisicoes").WithTags("Requisições").RequireAuthorization(Politicas.Requisitar);

        requisicoes.MapPost("/", Criar).ComIdempotencia();
        requisicoes.MapGet("/{id:guid}", Obter);
        requisicoes.MapPost("/{id:guid}/cancelamento", Cancelar);
    }

    private static async Task<Created<RequisicaoResposta>> Criar(
        NovaRequisicao nova,
        RepositorioDeRequisicoes repositorio,
        TimeProvider relogio,
        HttpContext http,
        CancellationToken cancelamento)
    {
        var dados = new DadosDaRequisicao(
            QuemPede.Operador(http.User),
            new Navio(nova.Navio.Nome, NumeroImo.De(nova.Navio.Imo)),
            new LocalDaOperacao(nova.Local.Terminal, nova.Local.Berco),
            Periodo.De(nova.Dia, nova.Turno));
        var ternos = nova.Ternos
            .Select(terno => (IReadOnlyList<VagasDaFuncao>)[.. terno.Select(vagas => new VagasDaFuncao(new Funcao(nova.Categoria, vagas.Funcao), vagas.Quantidade))])
            .ToList();
        var requisicao = new Requisicao(dados, ternos, relogio.GetUtcNow());

        Etiquetas.Escrever(http, await repositorio.Adicionar(requisicao, cancelamento));

        return TypedResults.Created($"/api/requisicoes/{requisicao.Id}", RequisicaoResposta.De(requisicao));
    }

    private static async Task<Results<Ok<RequisicaoResposta>, StatusCodeHttpResult>> Obter(
        Guid id,
        RepositorioDeRequisicoes repositorio,
        HttpContext http,
        CancellationToken cancelamento)
    {
        var lida = await DoOperador(repositorio, id, http, cancelamento);
        Etiquetas.Escrever(http, lida.Versao);

        return Etiquetas.ClienteJaTem(http, lida.Versao)
            ? TypedResults.StatusCode(StatusCodes.Status304NotModified)
            : TypedResults.Ok(RequisicaoResposta.De(lida.Agregado));
    }

    private static async Task<Ok<RequisicaoResposta>> Cancelar(
        Guid id,
        CancelamentoDaRequisicao cancelamentoPedido,
        VersaoLida versao,
        RepositorioDeRequisicoes repositorio,
        TimeProvider relogio,
        HttpContext http,
        CancellationToken cancelamento)
    {
        var lida = await DoOperador(repositorio, id, http, cancelamento);
        Lidos.ConferirVersao(lida, versao, $"A requisição {id}");

        lida.Agregado.Cancelar(cancelamentoPedido.Motivo, relogio.GetUtcNow());
        Etiquetas.Escrever(http, await repositorio.Atualizar(lida.Agregado, lida.Versao, cancelamento));

        return TypedResults.Ok(RequisicaoResposta.De(lida.Agregado));
    }

    /// <summary>A requisição, se é do operador do token. A de outro operador é 404, como a que não existe: não se revela que ela existe.</summary>
    private static async Task<Versionado<Requisicao>> DoOperador(RepositorioDeRequisicoes repositorio, Guid id, HttpContext http, CancellationToken cancelamento)
    {
        var lida = await repositorio.Obter(new RequisicaoId(id), cancelamento);

        return lida is not null && lida.Agregado.Operador.Cnpj == QuemPede.Operador(http.User).Cnpj
            ? lida
            : throw new AgregadoNaoEncontrado($"Não existe a requisição {id}.");
    }
}
