using System.Text.Json.Serialization;
using EscalaAvulsos.Api.Http;
using EscalaAvulsos.Api.Seguranca;
using EscalaAvulsos.Infraestrutura.Persistencia;

namespace EscalaAvulsos.Api.Hospedagem;

/// <summary>Os serviços da API: persistência, segurança, erros, JSON, documentação e a configuração do porto.</summary>
internal static class ServicosDaApi
{
    public static WebApplicationBuilder AdicionarApi(this WebApplicationBuilder builder)
    {
        var conexao = builder.Configuration.GetConnectionString("Escala")
            ?? throw new InvalidOperationException("Falta a cadeia de conexão ConnectionStrings:Escala.");

        builder.Services.AdicionarPersistencia(conexao);
        builder.Services.AdicionarSeguranca();
        builder.Services.AddSingleton(ConfiguracaoDoPorto.Ler(builder.Configuration.GetSection("Porto")));

        builder.Services.AddProblemDetails(opcoes => opcoes.CustomizeProblemDetails = Problemas.Completar);
        builder.Services.AddExceptionHandler<TradutorDeExcecoes>();

        // JSON estrito, como a leitura dos documentos (ADR-0011): campo
        // obrigatório que falta, nulo onde não cabe, campo que o contrato não
        // tem, enum em número e número em texto são recusados — e não viram
        // zero em silêncio.
        builder.Services.ConfigureHttpJsonOptions(opcoes =>
        {
            var json = opcoes.SerializerOptions;
            json.RespectNullableAnnotations = true;
            json.RespectRequiredConstructorParameters = true;
            json.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
            json.NumberHandling = JsonNumberHandling.Strict;
            json.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
        });

        // Pedido malformado lança, e o tradutor de exceções explica o que
        // estava errado. Sem isso, seria um 400 mudo.
        builder.Services.Configure<RouteHandlerOptions>(opcoes => opcoes.ThrowOnBadRequest = true);

        builder.Services.AddOpenApi(opcoes =>
        {
            opcoes.AddDocumentTransformer<DocumentacaoDaApi>();
            opcoes.AddOperationTransformer<DocumentacaoDaApi>();
        });

        return builder;
    }
}
