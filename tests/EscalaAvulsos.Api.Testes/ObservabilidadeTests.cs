using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using EscalaAvulsos.Api.Avisos;
using EscalaAvulsos.Api.Operacao;
using EscalaAvulsos.Api.TempoReal;
using EscalaAvulsos.Infraestrutura.Avisos;
using EscalaAvulsos.Infraestrutura.Testes.Persistencia;
using EscalaAvulsos.Infraestrutura.TempoReal;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using static EscalaAvulsos.Api.Testes.Novembro;
using static EscalaAvulsos.Api.Testes.TardeDeQuintaPelaApi;

namespace EscalaAvulsos.Api.Testes;

/*
 * A API vista de fora (ADR-0022): as verificações de saúde que o orquestrador
 * consulta — o banco, a escuta do banco e os laços que rodam sozinhos —, o
 * rastro de cada pedido e de cada volta da entrega, e o que a escala mede.
 */
public sealed class ObservabilidadeTests(BancoDeTestes bancos) : TestesDaApi(bancos)
{
    private static readonly TimeSpan Prazo = TimeSpan.FromSeconds(10);

    private readonly RastrosExportados _rastros = new();

    private readonly List<Metric> _metricas = [];

    private readonly ServicoDePushDosTestes _servicoDePush = new();

    private readonly ChavesVapid _chaves = ChavesVapid.Gerar();

    private bool _semBanco;

    private bool _comLacos;

    private bool _comPush;

    private string? _coletor;

    private bool _comMetricas;

    private TardeDeQuintaPelaApi Tarde => new(Escalador, () => Operador(), Trabalhador, Relogio);

    [Fact]
    public async Task Viva_e_pronta_respondem_sem_token_com_a_situacao_de_cada_verificacao()
    {
        await EscutaDePe();

        var viva = await Anonimo().GetAsync("/saude/viva", Cancelamento);
        var pronta = await Anonimo().GetAsync("/saude/pronta", Cancelamento);

        // Nos testes, os laços não rodam sozinhos: a viva não tem o que conferir.
        Assert.Equal(
            (HttpStatusCode.OK, """{"situacao":"Healthy","verificacoes":{}}"""),
            (viva.StatusCode, await viva.Content.ReadAsStringAsync(Cancelamento)));
        Assert.Equal(
            (HttpStatusCode.OK, """{"situacao":"Healthy","verificacoes":{"banco":{"situacao":"Healthy"},"escuta":{"situacao":"Healthy"}}}"""),
            (pronta.StatusCode, await pronta.Content.ReadAsStringAsync(Cancelamento)));
    }

    [Fact]
    public async Task Sem_o_banco_a_api_continua_viva_mas_nao_esta_pronta()
    {
        _semBanco = true;

        var viva = await Anonimo().GetAsync("/saude/viva", Cancelamento);
        var pronta = await Anonimo().GetAsync("/saude/pronta", Cancelamento);

        // Reiniciar não põe o banco no ar: o orquestrador só para de mandar
        // pedidos até ele voltar. E a resposta não diz onde o banco está.
        Assert.Equal(HttpStatusCode.OK, viva.StatusCode);
        Assert.Equal(
            (HttpStatusCode.ServiceUnavailable,
                """{"situacao":"Unhealthy","verificacoes":{"banco":{"situacao":"Unhealthy","descricao":"O banco não respondeu."},"escuta":{"situacao":"Degraded","descricao":"A escuta do banco caiu: as telas não se atualizam sozinhas até ela voltar."}}}"""),
            (pronta.StatusCode, await pronta.Content.ReadAsStringAsync(Cancelamento)));
    }

    [Fact]
    public async Task Sem_a_escuta_a_api_continua_pronta_mas_diz_que_as_telas_nao_se_atualizam()
    {
        var central = await EscutaDePe();
        central.Parar();

        var pronta = await Anonimo().GetAsync("/saude/pronta", Cancelamento);

        // Ler e gravar continua funcionando: tirar o servidor do ar seria pior que a tela parada.
        Assert.Equal(
            (HttpStatusCode.OK,
                """{"situacao":"Degraded","verificacoes":{"banco":{"situacao":"Healthy"},"escuta":{"situacao":"Degraded","descricao":"A escuta do banco caiu: as telas não se atualizam sozinhas até ela voltar."}}}"""),
            (pronta.StatusCode, await pronta.Content.ReadAsStringAsync(Cancelamento)));
    }

    [Fact]
    public async Task Com_os_lacos_rodando_a_viva_confere_que_eles_giram()
    {
        _comLacos = true;

        // Seis dias depois de a API subir, sem volta nenhuma nesse tempo: os dois laços estariam parados. Girando a cada 200ms,
        // eles marcam a volta com a hora nova, e a viva volta a ficar saudável.
        Relogio.Acertar(Em(5, 10));
        var limite = DateTime.UtcNow + Prazo;
        HttpResponseMessage viva;

        while ((viva = await Anonimo().GetAsync("/saude/viva", Cancelamento)).StatusCode != HttpStatusCode.OK && DateTime.UtcNow < limite)
        {
            await Task.Delay(50, Cancelamento);
        }

        Assert.Equal(
            (HttpStatusCode.OK, """{"situacao":"Healthy","verificacoes":{"lacos":{"situacao":"Healthy"}}}"""),
            (viva.StatusCode, await viva.Content.ReadAsStringAsync(Cancelamento)));

        // A pronta inclui a viva: o servidor com o laço parado também não recebe pedidos.
        Assert.Contains("\"lacos\":{\"situacao\":\"Healthy\"}", await Anonimo().GetStringAsync("/saude/pronta", Cancelamento), StringComparison.Ordinal);
    }

    [Fact]
    public async Task O_laco_que_parou_de_girar_derruba_a_viva_para_o_orquestrador_reiniciar()
    {
        var relogio = new RelogioDosTestes();
        relogio.Acertar(Em(5, 10));
        var voltas = new VoltasDosLacos(relogio);
        var lacos = new LacosGirando(voltas, new Dictionary<string, TimeSpan>
        {
            ["entrega dos recados"] = TimeSpan.FromSeconds(30),
            ["vigia dos prazos"] = TimeSpan.FromMinutes(1),
        });

        // A entrega tolera dois minutos e meio parada; a vigia, quatro. Nenhuma deu volta ainda: conta desde que o servidor subiu.
        relogio.Acertar(Em(5, 10, 2));
        voltas.Girou("entrega dos recados");
        relogio.Acertar(Em(5, 10, 4));
        Assert.Equal(HealthStatus.Healthy, (await lacos.CheckHealthAsync(new HealthCheckContext(), Cancelamento)).Status);

        relogio.Acertar(Em(5, 10, 5));
        var parados = await lacos.CheckHealthAsync(new HealthCheckContext(), Cancelamento);

        Assert.Equal((HealthStatus.Unhealthy, "Parou de girar: entrega dos recados, vigia dos prazos."), (parados.Status, parados.Description));
    }

    [Fact]
    public async Task Cada_pedido_vira_um_rastro_com_a_rota_o_banco_e_o_servico_que_atendeu()
    {
        await Tarde.Preparar();
        var (rastro, cliente) = NoRastro(Trabalhador(Antonio));

        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/api/eu/avisos", Cancelamento)).StatusCode);

        var doRastro = await _rastros.Esperar(rastro, "GET /api/eu/avisos/");
        var pedido = Assert.Single(doRastro, atividade => atividade.Kind == ActivityKind.Server);
        Assert.Equal(("/api/eu/avisos/", 200), (pedido.GetTagItem("http.route"), pedido.GetTagItem("http.response.status_code")));
        Assert.Contains(doRastro, atividade => atividade.Source.Name == "Npgsql" && atividade.ParentSpanId == pedido.SpanId);

        var servico = Servicos.GetRequiredService<TracerProvider>().GetResource().Attributes.ToDictionary();
        Assert.Equal(
            ("escala-avulsos-api", "escala-avulsos", "Testes"),
            (servico["service.name"], servico["service.namespace"], servico["deployment.environment.name"]));
    }

    [Fact]
    public async Task Com_o_endereco_do_coletor_rastros_metricas_e_logs_saem_por_otlp()
    {
        await using var coletor = await ColetorDeTeste.Subir();
        _coletor = coletor.Endereco;
        await Tarde.Preparar();

        Assert.Equal(HttpStatusCode.OK, (await Trabalhador(Antonio).GetAsync("/api/eu/avisos", Cancelamento)).StatusCode);

        // O SDK manda em lotes, de tempos em tempos; aqui, na hora.
        Servicos.GetRequiredService<TracerProvider>().ForceFlush();
        Servicos.GetRequiredService<MeterProvider>().ForceFlush();
        Servicos.GetRequiredService<LoggerProvider>().ForceFlush();

        Assert.Equal(["/v1/logs", "/v1/metrics", "/v1/traces"], coletor.Recebidos.Distinct().Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task As_medidas_da_escala_entram_na_telemetria_com_as_dos_pedidos()
    {
        _comMetricas = true;
        await Tarde.Preparar();
        await EscutaDePe();

        using (var resposta = await Trabalhador(Antonio).SendAsync(new HttpRequestMessage(HttpMethod.Get, "/api/eventos"), HttpCompletionOption.ResponseHeadersRead, Cancelamento))
        using (var leitor = new StreamReader(await resposta.Content.ReadAsStreamAsync(Cancelamento)))
        {
            await AteOEvento(leitor, "pronto");
            Servicos.GetRequiredService<MeterProvider>().ForceFlush();
        }

        // O medidor da escala vai pelo mesmo caminho que o dos pedidos: o que o painel mostra, e não só o que o teste lê.
        Assert.Contains(MedidasDoTempoReal.Ligacoes, _metricas.Select(metrica => metrica.Name));
        Assert.Contains("http.server.request.duration", _metricas.Select(metrica => metrica.Name));
    }

    [Fact]
    public async Task A_verificacao_de_saude_nao_vira_rastro()
    {
        await Tarde.Preparar();
        var (rastro, cliente) = NoRastro(Trabalhador(Antonio));

        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/saude/viva", Cancelamento)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/saude/pronta", Cancelamento)).StatusCode);

        // Um pedido de verdade depois, no mesmo rastro: quando ele chega, o que as verificações tivessem deixado já teria chegado.
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/api/eu/avisos", Cancelamento)).StatusCode);
        var doRastro = await _rastros.Esperar(rastro, "GET /api/eu/avisos/");

        // Nem o pedido, nem o select 1: o orquestrador pergunta a cada poucos segundos, e encheria o painel.
        var pedido = Assert.Single(doRastro, atividade => atividade.Kind == ActivityKind.Server);
        Assert.All(doRastro.Where(atividade => atividade != pedido), atividade => Assert.Equal(pedido.SpanId, atividade.ParentSpanId));
    }

    [Fact]
    public async Task A_volta_da_entrega_vira_um_rastro_com_cada_celular_e_cada_resultado_vira_medida()
    {
        _comPush = true;
        using var medicoes = new Medicoes(Servicos.GetRequiredService<IMeterFactory>());
        await Tarde.Preparar();
        await Trabalhador(Antonio).Enviar(HttpMethod.Put, "/api/eu/avisos/push", ServicoDePushDosTestes.Inscricao("do-antonio")).ComSucesso();
        await Trabalhador(Antonio).Enviar(HttpMethod.Put, "/api/eu/avisos/push", ServicoDePushDosTestes.Inscricao("antigo-do-antonio")).ComSucesso();
        _servicoDePush.Responder("antigo-do-antonio", HttpStatusCode.Gone);
        Relogio.Acertar(Em(5, 10));
        await Escalador().Enviar(HttpMethod.Post, $"{DaTarde}/escala").ComSucesso();

        // A volta roda dentro de um rastro do teste, gravado, para ele achar o que é seu entre os dos outros testes.
        ActivityTraceId rastro;
        using (var teste = new Activity("o teste") { ActivityTraceFlags = ActivityTraceFlags.Recorded }.Start())
        {
            rastro = teste.TraceId;
            await Servicos.GetRequiredService<EntregaDosAvisos>().Entregar(Cancelamento);
        }

        var doRastro = await _rastros.Esperar(rastro, "entregar os recados");
        var volta = Assert.Single(doRastro, atividade => atividade.DisplayName == "entregar os recados");

        // Os seis recados da escala foram dados por entregues, e o que a volta fez no banco está dentro dela.
        Assert.Equal((TelemetriaDaEscala.Avisos, 6), (volta.Source.Name, volta.GetTagItem("escala.avisos.entregues")));
        Assert.Contains(doRastro, atividade => atividade.Source.Name == "Npgsql" && atividade.ParentSpanId == volta.SpanId);

        // Cada celular do Antônio é uma medida: o de pé recebeu, o abandonado saiu. Os outros cinco não têm celular inscrito.
        Assert.Equal(
            (1, 1, 0),
            (medicoes.Soma(MedidasDosAvisos.Entregas, "escala.entrega.resultado", "entregue"),
                medicoes.Soma(MedidasDosAvisos.Entregas, "escala.entrega.resultado", "inscricao_vencida"),
                medicoes.Soma(MedidasDosAvisos.Entregas, "escala.entrega.resultado", "recusado")));
    }

    [Fact]
    public async Task A_vigia_mede_os_recados_que_deixou()
    {
        using var medicoes = new Medicoes(Servicos.GetRequiredService<IMeterFactory>());
        await Tarde.Preparar();
        await Escalador().CadastrarEstivador(4140, "Gilberto Souza");
        var vigia = Servicos.GetRequiredService<VigiaDosPrazos>();

        // Às 9h05, o Gilberto não se habilitou; e o exame dos sete vence em 30/11.
        Relogio.Acertar(Em(5, 9, 5));
        await vigia.ConferirAsHabilitacoes(Cancelamento);
        await vigia.ConferirOsVencimentos(Cancelamento);
        await vigia.ConferirOsVencimentos(Cancelamento);

        Assert.Equal(
            (1, 7),
            (medicoes.Soma(MedidasDosAvisos.Prazos, "escala.prazo", "habilitacao"), medicoes.Soma(MedidasDosAvisos.Prazos, "escala.prazo", "vencimento")));
    }

    [Fact]
    public async Task A_tela_ligada_e_cada_mudanca_mandada_entram_nas_medidas()
    {
        using var medicoes = new Medicoes(Servicos.GetRequiredService<IMeterFactory>());
        await Tarde.Preparar();
        var central = await EscutaDePe();

        using (var resposta = await Trabalhador(Antonio).SendAsync(new HttpRequestMessage(HttpMethod.Get, "/api/eventos"), HttpCompletionOption.ResponseHeadersRead, Cancelamento))
        using (var leitor = new StreamReader(await resposta.Content.ReadAsStreamAsync(Cancelamento)))
        {
            await AteOEvento(leitor, "pronto");
            Assert.Equal(1, medicoes.Soma(MedidasDoTempoReal.Ligacoes));

            central.Publicar(new AnuncioDoBanco(OrigemDoAnuncio.Escalas, Dominio.Trabalhadores.Categoria.Estiva, Em(5, 13), null));
            await AteOEvento(leitor, "mudou");
            Assert.Equal(1, medicoes.Soma(MedidasDoTempoReal.Mudancas, "escala.anuncio.origem", "Escalas"));
        }

        // Fechada a tela, a ligação sai da conta, quando o servidor percebe que o celular foi embora.
        var limite = DateTime.UtcNow + Prazo;

        while (medicoes.Soma(MedidasDoTempoReal.Ligacoes) != 0 && DateTime.UtcNow < limite)
        {
            await Task.Delay(20, Cancelamento);
        }

        Assert.Equal(0, medicoes.Soma(MedidasDoTempoReal.Ligacoes));
    }

    protected override void Configurar(IWebHostBuilder api)
    {
        api.ConfigureTestServices(servicos =>
            servicos.ConfigureOpenTelemetryTracerProvider(rastros => rastros.AddInMemoryExporter(_rastros)));

        if (_comMetricas)
        {
            api.ConfigureTestServices(servicos =>
                servicos.ConfigureOpenTelemetryMeterProvider(medidores => medidores.AddInMemoryExporter(_metricas)));
        }

        if (_coletor is not null)
        {
            api.UseSetting("OTEL_EXPORTER_OTLP_ENDPOINT", _coletor);
            api.UseSetting("OTEL_EXPORTER_OTLP_PROTOCOL", "http/protobuf");
        }

        if (_semBanco)
        {
            api.UseSetting("ConnectionStrings:Escala", "Host=127.0.0.1;Port=1;Database=nenhum;Username=ninguem;Password=nada;Timeout=2");
        }

        if (_comLacos)
        {
            api.UseSetting("Avisos:ConferirEEntregarSozinho", "true");
            api.UseSetting("Avisos:IntervaloDaEntrega", "00:00:00.200");
            api.UseSetting("Avisos:IntervaloDaConferencia", "00:00:00.200");
        }

        if (_comPush)
        {
            api.UseSetting("Avisos:Push:ChavePublica", _chaves.PublicaEmBase64Url);
            api.UseSetting("Avisos:Push:ChavePrivada", _chaves.PrivadaEmBase64Url);
            api.UseSetting("Avisos:Push:Contato", "mailto:escala@example.com");
            api.ConfigureTestServices(servicos =>
                servicos.AddHttpClient(nameof(EntregaDosAvisos)).ConfigurePrimaryHttpMessageHandler(() => _servicoDePush));
        }
    }

    /// <summary>
    /// Um cliente cujos pedidos vêm todos de um rastro só, como de um
    /// aplicativo instrumentado. Os testes rodam em paralelo, e o SDK escuta o
    /// processo inteiro: é o rastro que separa o que é deste teste.
    /// </summary>
    private static (ActivityTraceId Rastro, HttpClient Cliente) NoRastro(HttpClient cliente)
    {
        var rastro = ActivityTraceId.CreateRandom();
        cliente.DefaultRequestHeaders.Add("traceparent", $"00-{rastro}-{ActivitySpanId.CreateRandom()}-01");

        return (rastro, cliente);
    }

    /// <summary>Lê o fluxo de eventos até a linha do evento pedido, no prazo.</summary>
    private static async Task AteOEvento(StreamReader leitor, string tipo)
    {
        using var limite = CancellationTokenSource.CreateLinkedTokenSource(Cancelamento);
        limite.CancelAfter(Prazo);

        while (await leitor.ReadLineAsync(limite.Token) is { } linha)
        {
            if (linha == $"event: {tipo}")
            {
                return;
            }
        }

        Assert.Fail($"O fluxo acabou sem o evento {tipo}.");
    }

    /// <summary>Espera a escuta do banco desta API ficar de pé.</summary>
    private async Task<CentralDeAnuncios> EscutaDePe()
    {
        var central = Servicos.GetRequiredService<CentralDeAnuncios>();
        var limite = DateTime.UtcNow + Prazo;

        while (!central.Escutando && DateTime.UtcNow < limite)
        {
            await Task.Delay(20, Cancelamento);
        }

        Assert.True(central.Escutando, "A escuta do banco não ficou de pé.");

        return central;
    }
}
