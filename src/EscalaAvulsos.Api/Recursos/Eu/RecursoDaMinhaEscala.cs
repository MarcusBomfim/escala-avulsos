using EscalaAvulsos.Api.Hospedagem;
using EscalaAvulsos.Api.Http;
using EscalaAvulsos.Api.Recursos.Escalas;
using EscalaAvulsos.Api.Seguranca;
using EscalaAvulsos.Dominio.Comum;
using EscalaAvulsos.Dominio.Escalacoes;
using EscalaAvulsos.Dominio.Extratos;
using EscalaAvulsos.Dominio.Frequencias;
using EscalaAvulsos.Dominio.Trabalhadores;
using EscalaAvulsos.Infraestrutura.Persistencia;
using EscalaAvulsos.Infraestrutura.Persistencia.Repositorios;
using Microsoft.AspNetCore.Http.HttpResults;

namespace EscalaAvulsos.Api.Recursos.Eu;

/// <summary>
/// O trabalhador e a escala dele: a tela "minha escala", a desistência — sem
/// versão, e sem "tente de novo" — e o mês, com a frequência e o extrato
/// (ADR-0016).
/// </summary>
public sealed class RecursoDaMinhaEscala : IRecursoDaApi
{
    /// <summary>Quantos dias a tela "minha escala" mostra, a partir do primeiro.</summary>
    public const int DiasMostrados = 7;

    public void Mapear(IEndpointRouteBuilder api)
    {
        var eu = api.MapGroup("/eu").WithTags("Eu").RequireAuthorization(Politicas.Trabalhador);

        eu.MapGet("/escalas", Escalas);
        eu.MapPost("/escalas/{id:guid}/desistencia", Desistir).DispensandoAVersaoLida(
            "A desistência é só de quem pede: se outro gravou na escala no meio, o servidor lê de novo — com as jornadas de agora de quem espera — e refaz.");
        eu.MapGet("/meses/{mes}", Mes);
    }

    /// <param name="de">O primeiro dia, como <c>2026-11-05</c>; sem ele, hoje, no relógio do porto.</param>
    private static async Task<Ok<IReadOnlyList<MinhaEscalaResposta>>> Escalas(
        string? de,
        RepositorioDeTrabalhadores trabalhadores,
        RepositorioDeEscalas escalas,
        TimeProvider relogio,
        HttpContext http,
        CancellationToken cancelamento)
    {
        var trabalhador = await Quem(trabalhadores, http, cancelamento);
        var primeiro = de?.ComoDia("de") ?? Hoje(relogio);
        var minhas = await escalas.DoTrabalhadorEntre(trabalhador.Id, primeiro, primeiro.AddDays(DiasMostrados - 1), cancelamento);

        return TypedResults.Ok<IReadOnlyList<MinhaEscalaResposta>>([.. minhas.Select(escala => MinhaEscalaResposta.De(escala, escala.SituacaoDe(trabalhador.Matricula)!))]);
    }

    private static async Task<Ok<MinhaEscalaResposta>> Desistir(
        Guid id,
        RepositorioDeTrabalhadores trabalhadores,
        RepositorioDeEscalas escalas,
        TimeProvider relogio,
        HttpContext http,
        CancellationToken cancelamento)
    {
        var trabalhador = await Quem(trabalhadores, http, cancelamento);

        var escala = await Repeticao.AteGravar(async () =>
        {
            var lida = await escalas.Obter(new EscalaId(id), cancelamento);

            // A escala de que ele não participa é, para ele, uma escala que não existe.
            if (lida?.Agregado.SituacaoDe(trabalhador.Matricula) is null)
            {
                throw new AgregadoNaoEncontrado($"Não existe a escala {id}.");
            }

            var atual = lida.Agregado;
            var jornadas = await escalas.JornadasPerto(
                atual.Situacoes.Where(situacao => !situacao.Escalado).Select(situacao => situacao.Trabalhador),
                atual.Periodo,
                atual.Descanso,
                cancelamento);

            atual.Desistir(trabalhador.Id, relogio.GetUtcNow(), jornadas);
            await escalas.Atualizar(atual, lida.Versao, cancelamento);

            return atual;
        });

        return TypedResults.Ok(MinhaEscalaResposta.De(escala, escala.SituacaoDe(trabalhador.Matricula)!));
    }

    /// <param name="mes">O mês, como <c>2026-11</c>.</param>
    private static async Task<Ok<MeuMesResposta>> Mes(
        string mes,
        RepositorioDeTrabalhadores trabalhadores,
        RepositorioDeEscalacoes escalacoes,
        RepositorioDeEscalas escalas,
        ConfiguracaoDoPorto porto,
        TimeProvider relogio,
        HttpContext http,
        CancellationToken cancelamento)
    {
        var doMes = mes.ComoMes("mês");
        var trabalhador = await Quem(trabalhadores, http, cancelamento);
        var escalacoesDoMes = await escalacoes.DoTrabalhadorNoMes(trabalhador.Id, doMes, cancelamento);
        var escalasDoMes = await escalas.DoTrabalhadorNoMes(trabalhador.Id, doMes, cancelamento);

        var frequencia = FrequenciaDoMes.De(trabalhador, doMes, escalacoesDoMes, escalasDoMes, porto.Frequencia, Hoje(relogio));
        var extrato = Extrato.De(trabalhador, doMes, escalasDoMes, porto.Remuneracao);

        return TypedResults.Ok(new MeuMesResposta(doMes.ToString(), FrequenciaResposta.De(frequencia), ExtratoResposta.De(extrato)));
    }

    private static DateOnly Hoje(TimeProvider relogio) => DateOnly.FromDateTime(HorarioDoPorto.Local(relogio.GetUtcNow()));

    private static async Task<Trabalhador> Quem(RepositorioDeTrabalhadores trabalhadores, HttpContext http, CancellationToken cancelamento) =>
        (await Leituras.Trabalhador(trabalhadores, QuemPede.Matricula(http.User), cancelamento)).Agregado;
}
