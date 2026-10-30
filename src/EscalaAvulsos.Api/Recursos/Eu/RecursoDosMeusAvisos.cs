using EscalaAvulsos.Api.Hospedagem;
using EscalaAvulsos.Api.Http;
using EscalaAvulsos.Api.Seguranca;
using EscalaAvulsos.Dominio.Avisos;
using EscalaAvulsos.Infraestrutura.Avisos;
using EscalaAvulsos.Infraestrutura.Persistencia;
using EscalaAvulsos.Infraestrutura.Persistencia.Repositorios;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace EscalaAvulsos.Api.Recursos.Eu;

/// <summary>Um recado, como o trabalhador o lê.</summary>
public sealed record AvisoResposta(TipoDoAviso Tipo, string Titulo, string Texto, DateTimeOffset CriadoEm, bool Lido);

/// <summary>Os recados do trabalhador, do mais novo para o mais antigo, e quantos ele ainda não leu.</summary>
public sealed record MeusAvisosResposta(int NaoLidos, IReadOnlyList<AvisoResposta> Avisos);

/// <summary>A chave pública do servidor, que o navegador pede para se inscrever no push (<c>applicationServerKey</c>).</summary>
public sealed record ChaveDoPushResposta(string ChavePublica);

/// <summary>A inscrição do navegador, como ele a dá em <c>PushSubscription</c>: o endereço e as duas chaves, em base64url.</summary>
public sealed record InscricaoDoPushPedido(string Endereco, string ChavePublica, string Segredo);

/// <summary>O navegador que não quer mais os avisos, pelo endereço da inscrição dele.</summary>
public sealed record FimDaInscricaoPedido(string Endereco);

/// <summary>
/// Os recados do trabalhador e o celular onde eles chegam (ADR-0019,
/// ADR-0020). Como todo <c>/api/eu</c>, de quem é vem do token.
/// </summary>
public sealed class RecursoDosMeusAvisos : IRecursoDaApi
{
    /// <summary>Quantos recados a tela mostra: os mais novos.</summary>
    public const int Recados = 50;

    public void Mapear(IEndpointRouteBuilder api)
    {
        var avisos = api.MapGroup("/eu/avisos").WithTags("Eu").RequireAuthorization(Politicas.Trabalhador);

        avisos.MapGet("/", Listar);
        avisos.MapPost("/lidos", MarcarLidos).DispensandoAVersaoLida(
            "Marca como lidos os recados de quem pede: repetir o pedido não muda nada, e nenhum outro mexe nos recados dele.");

        avisos.MapGet("/push", ChaveDoPush);
        avisos.MapPut("/push", Inscrever).DispensandoAVersaoLida(
            "A inscrição é do navegador, pelo endereço dele: guardar de novo a mesma deixa tudo como estava.");
        avisos.MapDelete("/push", Desinscrever).DispensandoAVersaoLida(
            "Tirar o navegador que já saiu não muda nada: o pedido repetido é o mesmo pedido.");
    }

    private static async Task<Ok<MeusAvisosResposta>> Listar(
        RepositorioDeTrabalhadores trabalhadores,
        RepositorioDeAvisos avisos,
        HttpContext http,
        CancellationToken cancelamento)
    {
        var trabalhador = (await Leituras.Trabalhador(trabalhadores, QuemPede.Matricula(http.User), cancelamento)).Agregado;
        var guardados = await avisos.DoTrabalhador(trabalhador.Id, Recados, cancelamento);

        return TypedResults.Ok(new MeusAvisosResposta(
            await avisos.NaoLidos(trabalhador.Id, cancelamento),
            [.. guardados.Select(guardado => new AvisoResposta(
                guardado.Aviso.Tipo,
                guardado.Aviso.Titulo,
                guardado.Aviso.Texto,
                guardado.Aviso.CriadoEm,
                guardado.LidoEm is not null))]));
    }

    private static async Task<NoContent> MarcarLidos(
        RepositorioDeTrabalhadores trabalhadores,
        RepositorioDeAvisos avisos,
        HttpContext http,
        CancellationToken cancelamento)
    {
        var trabalhador = (await Leituras.Trabalhador(trabalhadores, QuemPede.Matricula(http.User), cancelamento)).Agregado;
        await avisos.MarcarLidos(trabalhador.Id, cancelamento);

        return TypedResults.NoContent();
    }

    private static Ok<ChaveDoPushResposta> ChaveDoPush(ConfiguracaoDosAvisos configuracao) =>
        TypedResults.Ok(new ChaveDoPushResposta(PushLigado(configuracao).Chaves.PublicaEmBase64Url));

    private static async Task<NoContent> Inscrever(
        InscricaoDoPushPedido pedido,
        ConfiguracaoDosAvisos configuracao,
        RepositorioDeTrabalhadores trabalhadores,
        RepositorioDeInscricoesDePush inscricoes,
        HttpContext http,
        CancellationToken cancelamento)
    {
        PushLigado(configuracao);

        var inscricao = Ler(() => InscricaoDePush.De(pedido.Endereco, pedido.ChavePublica, pedido.Segredo));
        var trabalhador = (await Leituras.Trabalhador(trabalhadores, QuemPede.Matricula(http.User), cancelamento)).Agregado;
        await inscricoes.Guardar(trabalhador.Id, inscricao, cancelamento);

        return TypedResults.NoContent();
    }

    private static async Task<NoContent> Desinscrever(
        [FromBody] FimDaInscricaoPedido pedido,
        RepositorioDeTrabalhadores trabalhadores,
        RepositorioDeInscricoesDePush inscricoes,
        HttpContext http,
        CancellationToken cancelamento)
    {
        if (!Uri.TryCreate(pedido.Endereco, UriKind.Absolute, out var endereco))
        {
            throw new PedidoInvalido("O endereço da inscrição de push tem de ser um endereço HTTPS.");
        }

        var trabalhador = (await Leituras.Trabalhador(trabalhadores, QuemPede.Matricula(http.User), cancelamento)).Agregado;
        await inscricoes.Remover(endereco, trabalhador.Id, cancelamento);

        return TypedResults.NoContent();
    }

    /// <summary>As chaves do push, ou 404: sem elas, o servidor não manda aviso ao celular, e o recado fica só no aplicativo.</summary>
    private static ConfiguracaoDoPush PushLigado(ConfiguracaoDosAvisos configuracao) =>
        configuracao.Push ?? throw new AgregadoNaoEncontrado("Este servidor não manda os avisos ao celular: os recados ficam só no aplicativo.");

    private static InscricaoDePush Ler(Func<InscricaoDePush> leitura)
    {
        try
        {
            return leitura();
        }
        catch (FormatException erro)
        {
            throw new PedidoInvalido(erro.Message, erro);
        }
    }
}
