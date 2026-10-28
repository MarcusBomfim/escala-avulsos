namespace EscalaAvulsos.Api.Http;

/*
 * O que cada endpoint que grava declara sobre si (ADR-0014): ou exige a
 * versão lida, ou exige a chave de idempotência, ou diz por que não precisa
 * de nenhuma das duas. Os testes da API percorrem os endpoints mapeados e
 * conferem que cada um declarou uma coisa só; a documentação OpenAPI
 * descreve os cabeçalhos pelo que eles declararam.
 */

/// <summary>Os cabeçalhos do contrato, pelo nome.</summary>
internal static class Cabecalhos
{
    public const string VersaoLida = "If-Match";

    public const string ChaveDeIdempotencia = "Idempotency-Key";

    /// <summary>Na resposta repetida a uma chave já atendida: <c>true</c>.</summary>
    public const string Repetido = "Idempotent-Replayed";
}

/// <summary>Marca, nos metadados do endpoint, que ele exige <c>If-Match</c>.</summary>
public sealed class ExigeVersaoLida
{
    private ExigeVersaoLida()
    {
    }

    public static ExigeVersaoLida Instancia { get; } = new();
}

/// <summary>Marca, nos metadados do endpoint, que ele cria agregado sem identidade natural, e exige <c>Idempotency-Key</c>.</summary>
public sealed class ExigeChaveDeIdempotencia
{
    private ExigeChaveDeIdempotencia()
    {
    }

    public static ExigeChaveDeIdempotencia Instancia { get; } = new();
}

/// <summary>
/// Marca o endpoint que grava sem <c>If-Match</c> nem <c>Idempotency-Key</c>,
/// com o motivo — uma criação pela identidade natural, ou uma alteração só da
/// parte de quem pede, que o servidor relê e refaz.
/// </summary>
public sealed record DispensaAVersaoLida(string Motivo);

internal static class ConvencoesDosEndpoints
{
    /// <summary>O endpoint grava sem versão lida, pelo motivo dado — que fica nos metadados e na documentação.</summary>
    public static RouteHandlerBuilder DispensandoAVersaoLida(this RouteHandlerBuilder endpoint, string motivo) =>
        endpoint.WithMetadata(new DispensaAVersaoLida(motivo));
}
