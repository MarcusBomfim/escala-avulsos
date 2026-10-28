using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using EscalaAvulsos.Api.Http;
using EscalaAvulsos.Infraestrutura.Testes.Persistencia;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace EscalaAvulsos.Api.Testes;

/*
 * As regras que valem para todo endpoint, conferidas percorrendo os
 * endpoints que a API mapeou — os de hoje e os que vierem. Um recurso novo
 * que esqueça a política, ou que grave sem dizer o que exige, quebra aqui, e
 * não em produção (ADR-0014).
 */
public sealed partial class EndpointsTests(BancoDeTestes bancos) : TestesDaApi(bancos)
{
    private IReadOnlyList<RouteEndpoint> DaApi() =>
        [.. Servicos.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.RoutePattern.RawText?.StartsWith("/api/", StringComparison.Ordinal) == true)];

    private static string Metodo(RouteEndpoint endpoint) =>
        endpoint.Metadata.GetRequiredMetadata<HttpMethodMetadata>().HttpMethods.Single();

    private static string Descrever(RouteEndpoint endpoint) => $"{Metodo(endpoint)} {endpoint.RoutePattern.RawText}";

    [GeneratedRegex(@"\{(\w+):[^}]+\}")]
    private static partial Regex Restricao();

    [Fact]
    public void A_api_tem_endpoints()
    {
        Assert.NotEmpty(DaApi());
    }

    [Fact]
    public void Todo_endpoint_exige_uma_politica_com_nome_e_nenhum_e_anonimo()
    {
        Assert.All(DaApi(), endpoint =>
        {
            Assert.Null(endpoint.Metadata.GetMetadata<IAllowAnonymous>());
            Assert.Contains(endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>(), dado => !string.IsNullOrEmpty(dado.Policy));
        });
    }

    [Fact]
    public void Todo_endpoint_que_grava_declara_uma_coisa_so_a_versao_lida_a_chave_ou_por_que_nao_precisa()
    {
        foreach (var endpoint in DaApi().Where(endpoint => Metodo(endpoint) != "GET"))
        {
            var declarou = new object?[]
            {
                endpoint.Metadata.GetMetadata<ExigeVersaoLida>(),
                endpoint.Metadata.GetMetadata<ExigeChaveDeIdempotencia>(),
                endpoint.Metadata.GetMetadata<DispensaAVersaoLida>(),
            }.Count(declaracao => declaracao is not null);

            Assert.True(declarou == 1, $"{Descrever(endpoint)} declarou {declarou} coisas, e devia declarar uma.");
        }
    }

    [Fact]
    public void A_leitura_nao_exige_versao_nem_chave()
    {
        Assert.All(
            DaApi().Where(endpoint => Metodo(endpoint) == "GET"),
            endpoint => Assert.True(
                endpoint.Metadata.GetMetadata<ExigeVersaoLida>() is null && endpoint.Metadata.GetMetadata<ExigeChaveDeIdempotencia>() is null,
                $"{Descrever(endpoint)} é leitura e exige o que é de quem grava."));
    }

    [Fact]
    public async Task O_documento_OpenAPI_descreve_os_cabecalhos_exigidos_e_o_motivo_de_quem_dispensa()
    {
        var documento = JsonNode.Parse(await (await Anonimo().GetAsync("/openapi/v1.json", Cancelamento)).Content.ReadAsStringAsync(Cancelamento))!;

        foreach (var endpoint in DaApi())
        {
            // No documento, os parâmetros vêm sem a restrição de rota: {matricula}, e não {matricula:int}.
            var caminho = Restricao().Replace(endpoint.RoutePattern.RawText!, "{$1}").TrimEnd('/');
            var operacao = documento["paths"]?[caminho]?[Metodo(endpoint).ToLowerInvariant()];
            var cabecalhos = (operacao?["parameters"]?.AsArray() ?? [])
                .Where(parametro => parametro?["in"]?.GetValue<string>() == "header")
                .Select(parametro => parametro!["name"]!.GetValue<string>())
                .ToList();

            Assert.True(operacao is not null, $"{Descrever(endpoint)} não está no documento OpenAPI.");
            Assert.Equal(endpoint.Metadata.GetMetadata<ExigeVersaoLida>() is not null, cabecalhos.Contains("If-Match"));
            Assert.Equal(endpoint.Metadata.GetMetadata<ExigeChaveDeIdempotencia>() is not null, cabecalhos.Contains("Idempotency-Key"));

            if (endpoint.Metadata.GetMetadata<DispensaAVersaoLida>() is { } dispensa)
            {
                Assert.Contains(dispensa.Motivo, operacao!["description"]?.GetValue<string>() ?? string.Empty, StringComparison.Ordinal);
            }
        }
    }
}
