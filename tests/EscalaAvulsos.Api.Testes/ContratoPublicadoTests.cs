using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using EscalaAvulsos.Infraestrutura.Testes.Persistencia;

namespace EscalaAvulsos.Api.Testes;

/*
 * O contrato da API fica escrito em docs/api/openapi.json, no repositório: é
 * dele que o aplicativo do trabalhador gera os seus tipos (Parte 6), e é nele
 * que quem revisa vê, no diff, que o contrato mudou. Se a API passar a servir
 * outra coisa, este teste falha — e o arquivo é atualizado de propósito, com
 * ESCALA_ATUALIZAR_CONTRATO=1 (ADR-0016).
 */
public sealed class ContratoPublicadoTests(BancoDeTestes bancos) : TestesDaApi(bancos)
{
    // Com os acentos como estão: é texto para gente ler no diff, e não para outro programa embutir numa página.
    private static readonly JsonSerializerOptions Indentado = new() { WriteIndented = true, IndentSize = 2, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    [Fact]
    public async Task O_contrato_publicado_e_o_que_a_api_serve()
    {
        var servido = JsonNode.Parse(await (await Anonimo().GetAsync("/openapi/v1.json", Cancelamento)).Content.ReadAsStringAsync(Cancelamento))!;
        var arquivo = Path.Combine(RaizDoRepositorio(), "docs", "api", "openapi.json");
        var escrito = servido.ToJsonString(Indentado).ReplaceLineEndings("\n") + "\n";

        if (Environment.GetEnvironmentVariable("ESCALA_ATUALIZAR_CONTRATO") == "1")
        {
            await File.WriteAllTextAsync(arquivo, escrito, Cancelamento);
        }

        var publicado = File.Exists(arquivo) ? await File.ReadAllTextAsync(arquivo, Cancelamento) : string.Empty;

        Assert.True(
            publicado.ReplaceLineEndings("\n") == escrito,
            "O contrato que a API serve não é o de docs/api/openapi.json. Se a mudança é de propósito, rode os testes com ESCALA_ATUALIZAR_CONTRATO=1 e confira o diff.");
    }

    private static string RaizDoRepositorio()
    {
        var pasta = new DirectoryInfo(AppContext.BaseDirectory);

        while (pasta is not null && !File.Exists(Path.Combine(pasta.FullName, "EscalaAvulsos.slnx")))
        {
            pasta = pasta.Parent;
        }

        return pasta?.FullName ?? throw new InvalidOperationException("O teste não achou a raiz do repositório, onde está o EscalaAvulsos.slnx.");
    }
}
