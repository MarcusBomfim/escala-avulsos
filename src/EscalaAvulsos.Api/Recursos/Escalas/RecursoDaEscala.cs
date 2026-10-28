using EscalaAvulsos.Api.Http;
using EscalaAvulsos.Api.Hospedagem;
using EscalaAvulsos.Api.Recursos.Escalacoes;
using EscalaAvulsos.Api.Seguranca;
using EscalaAvulsos.Dominio.Comum;
using EscalaAvulsos.Dominio.Escalacoes;
using EscalaAvulsos.Dominio.Identificacao;
using EscalaAvulsos.Dominio.Trabalhadores;
using EscalaAvulsos.Infraestrutura.Persistencia;
using EscalaAvulsos.Infraestrutura.Persistencia.Repositorios;
using Microsoft.AspNetCore.Http.HttpResults;

namespace EscalaAvulsos.Api.Recursos.Escalas;

/// <summary>
/// A escala pelo órgão gestor: fazer a escala da escalação, ler, e registrar
/// o que acontece no período — a presença, a falta, a justificativa, o
/// encerramento.
/// </summary>
public sealed class RecursoDaEscala : IRecursoDaApi
{
    public void Mapear(IEndpointRouteBuilder api)
    {
        var daEscalacao = api.MapGroup("/escalacoes/{categoria}/{dia}/{turno}/escala").WithTags("Escalas").RequireAuthorization(Politicas.Escalar);

        daEscalacao.MapPost("/", Fazer).DispensandoAVersaoLida(
            "A escalação identifica a escala: fazer de novo é recusado com 409. Se outra escala da categoria andou o rodízio no meio, " +
            "ou um habilitado pegou outra jornada, o servidor lê de novo e refaz.");
        daEscalacao.MapGet("/", DaEscalacao);

        var escalas = api.MapGroup("/escalas").WithTags("Escalas").RequireAuthorization(Politicas.Escalar);

        escalas.MapGet("/{id:guid}", Obter);
        escalas.MapPost("/{id:guid}/presencas", ConfirmarPresenca);
        escalas.MapPost("/{id:guid}/faltas", RegistrarFalta);
        escalas.MapPost("/{id:guid}/justificativas", JustificarFalta);
        escalas.MapPost("/{id:guid}/encerramento", Encerrar);
    }

    /// <summary>
    /// Lê o que a escala precisa — a escalação, as requisições do período, os
    /// habilitados, o rodízio da categoria e as jornadas deles —, escala pelo
    /// domínio e grava com a versão do rodízio que leu (ADR-0013).
    /// </summary>
    private static async Task<Created<EscalaResposta>> Fazer(
        string categoria,
        string dia,
        string turno,
        RepositorioDeEscalacoes escalacoes,
        RepositorioDeRequisicoes requisicoes,
        RepositorioDeTrabalhadores trabalhadores,
        RepositorioDeEscalas escalas,
        ConfiguracaoDoPorto porto,
        TimeProvider relogio,
        HttpContext http,
        CancellationToken cancelamento)
    {
        var daCategoria = categoria.ComoOpcao<Categoria>("categoria");
        var periodo = TextosDoPedido.ComoPeriodo(dia, turno);

        var escala = await Repeticao.AteGravar(async () =>
        {
            var escalacao = (await LeituraDaEscalacao.Ler(escalacoes, daCategoria, periodo, cancelamento)).Agregado;
            var doPeriodo = await requisicoes.DoPeriodo(daCategoria, periodo, cancelamento);
            var habilitados = await trabalhadores.Obter(escalacao.Habilitacoes.Select(habilitacao => habilitacao.Trabalhador), cancelamento);
            var rodizio = await escalas.RodizioDa(daCategoria, cancelamento);
            var jornadas = await escalas.JornadasPerto(habilitados.Select(trabalhador => trabalhador.Id), periodo, porto.Descanso, cancelamento);

            var feita = Escalador.Escalar(escalacao, doPeriodo, habilitados, jornadas, rodizio.Agregado, porto.Descanso, relogio.GetUtcNow());
            Etiquetas.Escrever(http, await escalas.Adicionar(feita, rodizio.Versao, cancelamento));

            return feita;
        });

        return TypedResults.Created($"/api/escalas/{escala.Id}", EscalaResposta.De(escala));
    }

    private static async Task<Ok<EscalaResposta>> DaEscalacao(
        string categoria,
        string dia,
        string turno,
        RepositorioDeEscalacoes escalacoes,
        RepositorioDeEscalas escalas,
        HttpContext http,
        CancellationToken cancelamento)
    {
        var daCategoria = categoria.ComoOpcao<Categoria>("categoria");
        var periodo = TextosDoPedido.ComoPeriodo(dia, turno);
        var escalacao = await LeituraDaEscalacao.Ler(escalacoes, daCategoria, periodo, cancelamento);
        var lida = await escalas.DaEscalacao(escalacao.Agregado.Id, cancelamento)
            ?? throw new AgregadoNaoEncontrado($"A escala {daCategoria.ComDe} de {periodo}, ainda não foi feita.");

        Etiquetas.Escrever(http, lida.Versao);

        return TypedResults.Ok(EscalaResposta.De(lida.Agregado));
    }

    private static async Task<Results<Ok<EscalaResposta>, StatusCodeHttpResult>> Obter(
        Guid id,
        RepositorioDeEscalas escalas,
        HttpContext http,
        CancellationToken cancelamento)
    {
        var lida = await Ler(escalas, id, cancelamento);
        Etiquetas.Escrever(http, lida.Versao);

        return Etiquetas.ClienteJaTem(http, lida.Versao)
            ? TypedResults.StatusCode(StatusCodes.Status304NotModified)
            : TypedResults.Ok(EscalaResposta.De(lida.Agregado));
    }

    private static Task<Ok<EscalaResposta>> ConfirmarPresenca(
        Guid id,
        TrabalhadorDaEscala quem,
        VersaoLida versao,
        RepositorioDeEscalas escalas,
        TimeProvider relogio,
        HttpContext http,
        CancellationToken cancelamento) =>
        Alterar(id, versao, escalas, http, escala => escala.ConfirmarPresenca(NaEscala(escala, quem.Matricula), relogio.GetUtcNow()), cancelamento);

    private static Task<Ok<EscalaResposta>> RegistrarFalta(
        Guid id,
        TrabalhadorDaEscala quem,
        VersaoLida versao,
        RepositorioDeEscalas escalas,
        TimeProvider relogio,
        HttpContext http,
        CancellationToken cancelamento) =>
        Alterar(id, versao, escalas, http, escala => escala.RegistrarFalta(NaEscala(escala, quem.Matricula), relogio.GetUtcNow()), cancelamento);

    private static Task<Ok<EscalaResposta>> JustificarFalta(
        Guid id,
        JustificativaDaFalta justificativa,
        VersaoLida versao,
        RepositorioDeEscalas escalas,
        HttpContext http,
        CancellationToken cancelamento) =>
        Alterar(id, versao, escalas, http, escala => escala.JustificarFalta(NaEscala(escala, justificativa.Matricula), justificativa.Justificativa), cancelamento);

    private static Task<Ok<EscalaResposta>> Encerrar(
        Guid id,
        VersaoLida versao,
        RepositorioDeEscalas escalas,
        TimeProvider relogio,
        HttpContext http,
        CancellationToken cancelamento) =>
        Alterar(id, versao, escalas, http, escala => escala.Encerrar(relogio.GetUtcNow()), cancelamento);

    private static async Task<Ok<EscalaResposta>> Alterar(
        Guid id,
        VersaoLida versao,
        RepositorioDeEscalas escalas,
        HttpContext http,
        Action<Escala> alteracao,
        CancellationToken cancelamento)
    {
        var lida = await Ler(escalas, id, cancelamento);
        Lidos.ConferirVersao(lida, versao, $"A escala {id}");

        alteracao(lida.Agregado);
        Etiquetas.Escrever(http, await escalas.Atualizar(lida.Agregado, lida.Versao, cancelamento));

        return TypedResults.Ok(EscalaResposta.De(lida.Agregado));
    }

    private static async Task<Versionado<Escala>> Ler(RepositorioDeEscalas escalas, Guid id, CancellationToken cancelamento) =>
        await escalas.Obter(new EscalaId(id), cancelamento) ?? throw new AgregadoNaoEncontrado($"Não existe a escala {id}.");

    /// <summary>Quem, entre os habilitados da escala, tem a matrícula. A matrícula que não se habilitou é regra do domínio, e não endereço: 422.</summary>
    private static TrabalhadorId NaEscala(Escala escala, int matricula) =>
        escala.SituacaoDe(Matricula.De(matricula))?.Trabalhador
        ?? throw new ExcecaoDeDominio($"A matrícula {matricula} não se habilitou para a escala {escala.Categoria.ComDe} de {escala.Periodo}.");
}
