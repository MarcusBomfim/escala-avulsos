using EscalaAvulsos.Api.Http;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace EscalaAvulsos.Api.Hospedagem;

/// <summary>
/// O que o documento OpenAPI não descobre sozinho: o esquema de
/// autenticação, os cabeçalhos que os endpoints exigem — <c>If-Match</c> e
/// <c>Idempotency-Key</c> — e por que alguns não exigem nenhum.
/// </summary>
internal sealed class DocumentacaoDaApi : IOpenApiDocumentTransformer, IOpenApiOperationTransformer
{
    private const string Esquema = "Bearer";

    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(document);

        document.Info.Title = "Escala dos Avulsos";
        document.Info.Description =
            "A escala do trabalhador portuário avulso: o cadastro, as requisições dos operadores, a habilitação, a escala, " +
            "a presença e a falta, a frequência e o extrato. Toda chamada leva um JWT com o papel de quem pede — e a matrícula " +
            "do trabalhador, ou o CNPJ do operador; os erros vêm em ProblemDetails (RFC 9457).";

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[Esquema] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Token do provedor de identidade, com as reivindicações role e, conforme o papel, matricula ou cnpj.",
        };

        document.Security ??= [];
        document.Security.Add(new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference(Esquema, document)] = [] });

        return Task.CompletedTask;
    }

    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(context);

        var metadados = context.Description.ActionDescriptor.EndpointMetadata;

        if (metadados.OfType<ExigeVersaoLida>().Any())
        {
            Cabecalho(operation, Cabecalhos.VersaoLida, "A versão lida — a ETag da leitura, como \"3\". Sem ela, 428; desatualizada, 412.");
        }

        if (metadados.OfType<ExigeChaveDeIdempotencia>().Any())
        {
            Cabecalho(
                operation,
                Cabecalhos.ChaveDeIdempotencia,
                "Um valor único por operação — um UUID —, repetido em cada nova tentativa. A repetição recebe a resposta da primeira vez.");
        }

        if (metadados.OfType<DispensaAVersaoLida>().FirstOrDefault() is { } dispensa)
        {
            operation.Description = string.IsNullOrEmpty(operation.Description) ? dispensa.Motivo : $"{operation.Description} {dispensa.Motivo}";
        }

        return Task.CompletedTask;
    }

    private static void Cabecalho(OpenApiOperation operacao, string nome, string descricao)
    {
        operacao.Parameters ??= [];
        operacao.Parameters.Add(new OpenApiParameter
        {
            Name = nome,
            In = ParameterLocation.Header,
            Required = true,
            Description = descricao,
            Schema = new OpenApiSchema { Type = JsonSchemaType.String },
        });
    }
}
