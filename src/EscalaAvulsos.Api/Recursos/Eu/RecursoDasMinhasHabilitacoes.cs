using EscalaAvulsos.Api.Http;
using EscalaAvulsos.Api.Recursos.Escalacoes;
using EscalaAvulsos.Api.Seguranca;
using EscalaAvulsos.Dominio.Trabalhadores;
using EscalaAvulsos.Infraestrutura.Persistencia.Repositorios;
using Microsoft.AspNetCore.Http.HttpResults;

namespace EscalaAvulsos.Api.Recursos.Eu;

/// <summary>
/// O trabalhador se habilitando para a escala de um período, na categoria
/// dele: ver a janela, escolher as funções, trocar, desistir — sem versão, e
/// sem "tente de novo" (ADR-0015).
/// </summary>
public sealed class RecursoDasMinhasHabilitacoes : IRecursoDaApi
{
    private const string SoDeQuemPede =
        "A habilitação é só de quem pede: se outro trabalhador se habilitou no meio, o servidor lê a escalação de novo e refaz — a vez é pela matrícula, e não pela ordem de chegada.";

    public void Mapear(IEndpointRouteBuilder api)
    {
        var minha = api.MapGroup("/eu/habilitacoes/{dia}/{turno}").WithTags("Eu").RequireAuthorization(Politicas.Trabalhador);

        minha.MapGet("/", Obter);
        minha.MapPut("/", Habilitar).DispensandoAVersaoLida(SoDeQuemPede);
        minha.MapDelete("/", Desistir).DispensandoAVersaoLida(SoDeQuemPede);
    }

    private static async Task<Ok<MinhaHabilitacaoResposta>> Obter(
        string dia,
        string turno,
        RepositorioDeTrabalhadores trabalhadores,
        RepositorioDeEscalacoes escalacoes,
        TimeProvider relogio,
        HttpContext http,
        CancellationToken cancelamento)
    {
        var trabalhador = await Quem(trabalhadores, http, cancelamento);
        var lida = await LeituraDaEscalacao.Ler(escalacoes, trabalhador.Categoria, TextosDoPedido.ComoPeriodo(dia, turno), cancelamento);

        return TypedResults.Ok(MinhaHabilitacaoResposta.De(lida.Agregado, trabalhador.Id, relogio.GetUtcNow()));
    }

    private static async Task<Ok<MinhaHabilitacaoResposta>> Habilitar(
        string dia,
        string turno,
        PedidoDeHabilitacao pedido,
        RepositorioDeTrabalhadores trabalhadores,
        RepositorioDeEscalacoes escalacoes,
        TimeProvider relogio,
        HttpContext http,
        CancellationToken cancelamento)
    {
        var trabalhador = await Quem(trabalhadores, http, cancelamento);
        var periodo = TextosDoPedido.ComoPeriodo(dia, turno);
        var funcoes = pedido.Funcoes.Select(funcao => new Funcao(trabalhador.Categoria, funcao)).ToList();

        var escalacao = await Repeticao.AteGravar(async () =>
        {
            var lida = await LeituraDaEscalacao.Ler(escalacoes, trabalhador.Categoria, periodo, cancelamento);
            lida.Agregado.Habilitar(trabalhador, funcoes, relogio.GetUtcNow());
            await escalacoes.Atualizar(lida.Agregado, lida.Versao, cancelamento);

            return lida.Agregado;
        });

        return TypedResults.Ok(MinhaHabilitacaoResposta.De(escalacao, trabalhador.Id, relogio.GetUtcNow()));
    }

    private static async Task<Ok<MinhaHabilitacaoResposta>> Desistir(
        string dia,
        string turno,
        RepositorioDeTrabalhadores trabalhadores,
        RepositorioDeEscalacoes escalacoes,
        TimeProvider relogio,
        HttpContext http,
        CancellationToken cancelamento)
    {
        var trabalhador = await Quem(trabalhadores, http, cancelamento);
        var periodo = TextosDoPedido.ComoPeriodo(dia, turno);

        var escalacao = await Repeticao.AteGravar(async () =>
        {
            var lida = await LeituraDaEscalacao.Ler(escalacoes, trabalhador.Categoria, periodo, cancelamento);
            lida.Agregado.Desistir(trabalhador, relogio.GetUtcNow());
            await escalacoes.Atualizar(lida.Agregado, lida.Versao, cancelamento);

            return lida.Agregado;
        });

        return TypedResults.Ok(MinhaHabilitacaoResposta.De(escalacao, trabalhador.Id, relogio.GetUtcNow()));
    }

    private static async Task<Trabalhador> Quem(RepositorioDeTrabalhadores trabalhadores, HttpContext http, CancellationToken cancelamento) =>
        (await Leituras.Trabalhador(trabalhadores, QuemPede.Matricula(http.User), cancelamento)).Agregado;
}
