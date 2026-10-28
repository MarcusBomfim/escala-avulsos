using EscalaAvulsos.Api.Http;
using EscalaAvulsos.Api.Seguranca;
using EscalaAvulsos.Dominio.Identificacao;
using EscalaAvulsos.Dominio.Trabalhadores;
using EscalaAvulsos.Infraestrutura.Persistencia.Repositorios;
using Microsoft.AspNetCore.Http.HttpResults;

namespace EscalaAvulsos.Api.Recursos.Trabalhadores;

/// <summary>
/// O cadastro dos trabalhadores, pelo órgão gestor: a inscrição, as
/// qualificações, o exame, os impedimentos, o registro e o cancelamento —
/// sempre pela matrícula, que é como o trabalhador é conhecido.
/// </summary>
public sealed class RecursoDeTrabalhadores : IRecursoDaApi
{
    public void Mapear(IEndpointRouteBuilder api)
    {
        var trabalhadores = api.MapGroup("/trabalhadores").WithTags("Trabalhadores").RequireAuthorization(Politicas.Escalar);

        trabalhadores.MapPost("/", Cadastrar)
            .DispensandoAVersaoLida("A matrícula identifica o trabalhador: cadastrar de novo a mesma matrícula é recusado com 409, e não cria outro.");
        trabalhadores.MapGet("/{matricula:int}", Obter);
        trabalhadores.MapPost("/{matricula:int}/qualificacoes", Qualificar);
        trabalhadores.MapPut("/{matricula:int}/aso", RegistrarAso);
        trabalhadores.MapPost("/{matricula:int}/impedimentos", Impedir);
        trabalhadores.MapPost("/{matricula:int}/fim-do-afastamento", EncerrarAfastamento);
        trabalhadores.MapPost("/{matricula:int}/registro", Registrar);
        trabalhadores.MapPost("/{matricula:int}/cancelamento", CancelarInscricao);
    }

    private static async Task<Created<TrabalhadorResposta>> Cadastrar(
        NovoTrabalhador novo,
        RepositorioDeTrabalhadores repositorio,
        HttpContext http,
        CancellationToken cancelamento)
    {
        var trabalhador = new Trabalhador(new DadosDoTrabalhador(Matricula.De(novo.Matricula), novo.Nome, Cpf.De(novo.Cpf), novo.Categoria, novo.Inscricao));

        Etiquetas.Escrever(http, await repositorio.Adicionar(trabalhador, cancelamento));

        return TypedResults.Created($"/api/trabalhadores/{trabalhador.Matricula}", TrabalhadorResposta.De(trabalhador));
    }

    private static async Task<Results<Ok<TrabalhadorResposta>, StatusCodeHttpResult>> Obter(
        int matricula,
        RepositorioDeTrabalhadores repositorio,
        HttpContext http,
        CancellationToken cancelamento)
    {
        var lido = await Leituras.Trabalhador(repositorio, Matricula.De(matricula), cancelamento);
        Etiquetas.Escrever(http, lido.Versao);

        return Etiquetas.ClienteJaTem(http, lido.Versao)
            ? TypedResults.StatusCode(StatusCodes.Status304NotModified)
            : TypedResults.Ok(TrabalhadorResposta.De(lido.Agregado));
    }

    private static Task<Ok<TrabalhadorResposta>> Qualificar(
        int matricula,
        NovaQualificacao nova,
        VersaoLida versao,
        RepositorioDeTrabalhadores repositorio,
        HttpContext http,
        CancellationToken cancelamento) =>
        Alterar(
            matricula,
            versao,
            repositorio,
            http,
            trabalhador => trabalhador.Qualificar(new Funcao(trabalhador.Categoria, nova.Funcao), nova.Desde, nova.ValidaAte),
            cancelamento);

    private static Task<Ok<TrabalhadorResposta>> RegistrarAso(
        int matricula,
        NovoAso novo,
        VersaoLida versao,
        RepositorioDeTrabalhadores repositorio,
        HttpContext http,
        CancellationToken cancelamento) =>
        Alterar(matricula, versao, repositorio, http, trabalhador => trabalhador.RegistrarAso(new Aso(novo.EmitidoEm, novo.ValidoAte)), cancelamento);

    private static Task<Ok<TrabalhadorResposta>> Impedir(
        int matricula,
        NovoImpedimento novo,
        VersaoLida versao,
        RepositorioDeTrabalhadores repositorio,
        HttpContext http,
        CancellationToken cancelamento) =>
        Alterar(
            matricula,
            versao,
            repositorio,
            http,
            trabalhador => trabalhador.Impedir(new Impedimento(novo.Tipo, novo.De, novo.Ate, novo.Observacao)),
            cancelamento);

    private static Task<Ok<TrabalhadorResposta>> EncerrarAfastamento(
        int matricula,
        FimDoAfastamento fim,
        VersaoLida versao,
        RepositorioDeTrabalhadores repositorio,
        HttpContext http,
        CancellationToken cancelamento) =>
        Alterar(matricula, versao, repositorio, http, trabalhador => trabalhador.EncerrarAfastamento(fim.Volta), cancelamento);

    private static Task<Ok<TrabalhadorResposta>> Registrar(
        int matricula,
        VersaoLida versao,
        RepositorioDeTrabalhadores repositorio,
        HttpContext http,
        CancellationToken cancelamento) =>
        Alterar(matricula, versao, repositorio, http, trabalhador => trabalhador.Registrar(), cancelamento);

    private static Task<Ok<TrabalhadorResposta>> CancelarInscricao(
        int matricula,
        NovoCancelamento novo,
        VersaoLida versao,
        RepositorioDeTrabalhadores repositorio,
        HttpContext http,
        CancellationToken cancelamento) =>
        Alterar(matricula, versao, repositorio, http, trabalhador => trabalhador.CancelarInscricao(novo.APartirDe, novo.Motivo), cancelamento);

    /// <summary>Lê, confere a versão, aplica a alteração pelo domínio e grava com a versão lida.</summary>
    private static async Task<Ok<TrabalhadorResposta>> Alterar(
        int matricula,
        VersaoLida versao,
        RepositorioDeTrabalhadores repositorio,
        HttpContext http,
        Action<Trabalhador> alteracao,
        CancellationToken cancelamento)
    {
        var lido = await Leituras.Trabalhador(repositorio, Matricula.De(matricula), cancelamento);
        Lidos.ConferirVersao(lido, versao, $"O trabalhador da matrícula {matricula}");

        alteracao(lido.Agregado);
        Etiquetas.Escrever(http, await repositorio.Atualizar(lido.Agregado, lido.Versao, cancelamento));

        return TypedResults.Ok(TrabalhadorResposta.De(lido.Agregado));
    }
}
