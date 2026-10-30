using System.Text.Json.Serialization;
using EscalaAvulsos.Api.Avisos;
using EscalaAvulsos.Api.Http;
using EscalaAvulsos.Api.Operacao;
using EscalaAvulsos.Api.Seguranca;
using EscalaAvulsos.Api.TempoReal;
using EscalaAvulsos.Infraestrutura.Persistencia;
using EscalaAvulsos.Infraestrutura.TempoReal;
using EscalaAvulsos.Observabilidade;

namespace EscalaAvulsos.Api.Hospedagem;

/// <summary>Os serviços da API: persistência, segurança, erros, JSON, documentação, a configuração do porto, os avisos, o tempo real e a observabilidade.</summary>
internal static class ServicosDaApi
{
    public static WebApplicationBuilder AdicionarApi(this WebApplicationBuilder builder)
    {
        var conexao = builder.Configuration.GetConnectionString("Escala")
            ?? throw new InvalidOperationException("Falta a cadeia de conexão ConnectionStrings:Escala.");

        builder.AdicionarObservabilidade("escala-avulsos-api");
        builder.Services.AdicionarPersistencia(conexao);
        builder.Services.AdicionarSeguranca();
        builder.Services.AddSingleton(ConfiguracaoDoPorto.Ler(builder.Configuration.GetSection("Porto")));
        AdicionarAvisosETempoReal(builder, conexao);

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

    /// <summary>
    /// A escuta do banco e a central que repassa os anúncios (ADR-0020), e os
    /// avisos: a entrega aos celulares e a vigia dos prazos (ADR-0019). As duas
    /// últimas rodam sozinhas, a não ser que a configuração diga que não — nos
    /// testes, quem as chama é o teste, na hora que ele escolhe.
    /// </summary>
    private static void AdicionarAvisosETempoReal(WebApplicationBuilder builder, string conexao)
    {
        var avisos = ConfiguracaoDosAvisos.Ler(builder.Configuration.GetSection("Avisos"));

        builder.Services.AddSingleton(avisos);
        builder.Services.AddSingleton(new OuvinteDoBanco(conexao));
        builder.Services.AddSingleton<MedidasDosAvisos>();
        builder.Services.AddSingleton<MedidasDoTempoReal>();
        builder.Services.AddSingleton<VoltasDosLacos>();
        builder.Services.AddSingleton<CentralDeAnuncios>();
        builder.Services.AddHostedService<EscutaDoBanco>();

        // O serviço de push que não responde em dez segundos fica para a próxima volta.
        builder.Services.AddHttpClient(nameof(EntregaDosAvisos), cliente => cliente.Timeout = TimeSpan.FromSeconds(10));
        builder.Services.AddSingleton<EntregaDosAvisos>();
        builder.Services.AddSingleton<VigiaDosPrazos>();

        // Pronta é o banco no ar e a escuta de pé; viva, os laços girando, quando eles rodam (ADR-0022).
        var saude = builder.Services.AddHealthChecks()
            .VerificarBanco(conexao)
            .AddCheck<EscutaDePe>("escuta", tags: [Saude.Pronta]);

        if (avisos.ConferirEEntregarSozinho)
        {
            builder.Services.AddHostedService<EntregaDosAvisosSozinha>();
            builder.Services.AddHostedService<VigiaDosPrazosSozinha>();

            var intervalos = new Dictionary<string, TimeSpan>
            {
                [EntregaDosAvisosSozinha.Laco] = avisos.IntervaloDaEntrega,
                [VigiaDosPrazosSozinha.Laco] = avisos.IntervaloDaConferencia,
            };
            saude.Add(new("lacos", servicos => new LacosGirando(servicos.GetRequiredService<VoltasDosLacos>(), intervalos), failureStatus: null, tags: [Saude.Viva]));
        }
    }
}
