using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using EscalaAvulsos.Dominio.Escalacoes;
using EscalaAvulsos.Dominio.Periodos;
using EscalaAvulsos.Dominio.Trabalhadores;

namespace EscalaAvulsos.Infraestrutura.Documentos;

/// <summary>
/// A serialização dos documentos, gerada em tempo de compilação.
/// </summary>
/// <remarks>
/// <para>
/// Geração de código em vez de reflexão: o compilador vê todos os tipos que
/// vão para o banco, e um tipo que o serializador não sabe tratar é erro de
/// build.
/// </para>
/// <para>
/// A leitura é estrita de propósito (ADR-0011): propriedade obrigatória que
/// falta, nulo onde não cabe nulo ou propriedade que o documento não conhece
/// fazem a leitura falhar. Um documento que não é exatamente o formato
/// esperado não vira, em silêncio, um trabalhador com o exame zerado.
/// </para>
/// <para>
/// Enums vão como texto — <c>"Registrado"</c>, e não <c>1</c> —: legíveis
/// numa consulta, e imunes a uma reordenação dos membros. E só como texto: o
/// número, que o conversor padrão aceitaria, deixaria entrar um valor que o
/// enum não tem.
/// </para>
/// </remarks>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    RespectNullableAnnotations = true,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    Converters =
    [
        typeof(SoTexto<Categoria>),
        typeof(SoTexto<Inscricao>),
        typeof(SoTexto<TipoDeImpedimento>),
        typeof(SoTexto<TipoDeMotivo>),
        typeof(SoTexto<Turno>),
        typeof(SoTexto<Comparecimento>),
        typeof(SoTexto<TipoDeMotivoNaEscala>),
    ])]
[JsonSerializable(typeof(DocumentoDoTrabalhador))]
[JsonSerializable(typeof(DocumentoDaRequisicao))]
[JsonSerializable(typeof(DocumentoDaEscalacao))]
[JsonSerializable(typeof(DocumentoDaEscala))]
internal sealed partial class ContextoJson : JsonSerializerContext;

/// <summary>Um enum gravado pelo nome, e lido só pelo nome — nunca pelo número.</summary>
internal sealed class SoTexto<T>() : JsonStringEnumConverter<T>(namingPolicy: null, allowIntegerValues: false)
    where T : struct, Enum;

/// <summary>Escreve e lê documentos pelo <see cref="ContextoJson"/>.</summary>
internal static class Json
{
    public static string Escrever<T>(T documento, JsonTypeInfo<T> tipo) => JsonSerializer.Serialize(documento, tipo);

    /// <remarks>
    /// A nulidade que o serializador respeita é a das propriedades, e não a dos
    /// itens de uma lista: um <c>null</c> no meio das qualificações passaria, e
    /// estouraria longe daqui. Nenhum documento tem lista com nulo, e a leitura
    /// confere antes.
    /// </remarks>
    public static T Ler<T>(string json, JsonTypeInfo<T> tipo)
    {
        using var documento = JsonDocument.Parse(json);
        ExigirListasSemNulo(documento.RootElement, "$");

        return documento.Deserialize(tipo) ?? throw new JsonException("O documento gravado é nulo.");
    }

    private static void ExigirListasSemNulo(JsonElement elemento, string caminho)
    {
        if (elemento.ValueKind == JsonValueKind.Object)
        {
            foreach (var propriedade in elemento.EnumerateObject())
            {
                ExigirListasSemNulo(propriedade.Value, $"{caminho}.{propriedade.Name}");
            }
        }
        else if (elemento.ValueKind == JsonValueKind.Array)
        {
            var posicao = 0;

            foreach (var item in elemento.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.Null)
                {
                    throw new JsonException($"A lista {caminho} tem um item nulo, na posição {posicao}.");
                }

                ExigirListasSemNulo(item, $"{caminho}[{posicao}]");
                posicao++;
            }
        }
    }
}
