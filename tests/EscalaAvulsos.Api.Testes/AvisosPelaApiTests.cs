using System.Net;
using EscalaAvulsos.Api.Avisos;
using EscalaAvulsos.Api.Recursos.Eu;
using EscalaAvulsos.Dominio.Avisos;
using EscalaAvulsos.Dominio.Identificacao;
using EscalaAvulsos.Infraestrutura.Avisos;
using EscalaAvulsos.Infraestrutura.Persistencia.Repositorios;
using EscalaAvulsos.Infraestrutura.Testes.Persistencia;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using static EscalaAvulsos.Api.Testes.Novembro;
using static EscalaAvulsos.Api.Testes.TardeDeQuintaPelaApi;

namespace EscalaAvulsos.Api.Testes;

/*
 * Os recados do trabalhador pela API: os que a escala deixa, os lidos e os
 * por ler; o celular que se inscreve; a entrega, cifrada, ao serviço de push;
 * e os avisos do prazo, que o servidor confere (ADR-0019, ADR-0020).
 */
public sealed class AvisosPelaApiTests(BancoDeTestes bancos) : TestesDaApi(bancos)
{
    private readonly ServicoDePushDosTestes _servicoDePush = new();

    private readonly ChavesVapid _chaves = ChavesVapid.Gerar();

    private bool _comPush;

    private TardeDeQuintaPelaApi Tarde => new(Escalador, () => Operador(), Trabalhador, Relogio);

    [Fact]
    public async Task A_escala_que_sai_vira_recado_e_abrir_os_recados_os_marca_como_lidos()
    {
        await Tarde.Escalar();

        var recados = await Trabalhador(Antonio).Ler("/api/eu/avisos").LerComSucesso<MeusAvisosResposta>();

        Assert.Equal(1, recados.NaoLidos);
        var aviso = Assert.Single(recados.Avisos);
        Assert.Equal((TipoDoAviso.VaiTrabalhar, "Você vai trabalhar", Em(5, 10), false), (aviso.Tipo, aviso.Titulo, aviso.CriadoEm, aviso.Lido));
        Assert.Equal("Quinta-feira, 5 de novembro, das 13h às 19h: sinaleiro no terno 1 do navio Atlântico Sul, no Terminal Cais Leste, berço 4.", aviso.Texto);

        await Trabalhador(Antonio).Enviar(HttpMethod.Post, "/api/eu/avisos/lidos").ComSucesso();

        var depois = await Trabalhador(Antonio).Ler("/api/eu/avisos").LerComSucesso<MeusAvisosResposta>();
        Assert.Equal(0, depois.NaoLidos);
        Assert.True(Assert.Single(depois.Avisos).Lido);

        // Os recados do Carlos são dele: ficou de fora, e ainda não leu.
        var doCarlos = await Trabalhador(Carlos).Ler("/api/eu/avisos").LerComSucesso<MeusAvisosResposta>();
        Assert.Equal((1, TipoDoAviso.FicouDeFora), (doCarlos.NaoLidos, Assert.Single(doCarlos.Avisos).Tipo));
    }

    [Fact]
    public async Task Sem_as_chaves_do_push_o_celular_nao_se_inscreve_e_o_recado_fica_so_no_aplicativo()
    {
        await Tarde.Escalar();

        var chave = await Trabalhador(Antonio).Ler("/api/eu/avisos/push");
        var inscricao = await Trabalhador(Antonio).Enviar(HttpMethod.Put, "/api/eu/avisos/push", ServicoDePushDosTestes.Inscricao("do-antonio"));

        Assert.Equal((HttpStatusCode.NotFound, HttpStatusCode.NotFound), (chave.StatusCode, inscricao.StatusCode));
        Assert.Equal("Este servidor não manda os avisos ao celular: os recados ficam só no aplicativo.", (await chave.Problema()).Detail);

        // A entrega dá por entregue o que não tem para onde ir: ninguém tenta de novo.
        var entrega = Servicos.GetRequiredService<EntregaDosAvisos>();
        Assert.Equal(6, await entrega.Entregar(Cancelamento));
        Assert.Equal(0, await entrega.Entregar(Cancelamento));
    }

    [Fact]
    public async Task O_aviso_vai_cifrado_e_assinado_aos_celulares_inscritos_e_o_que_venceu_sai()
    {
        ComPush();
        await Tarde.Preparar();

        var chave = await Trabalhador(Antonio).Ler("/api/eu/avisos/push").LerComSucesso<ChaveDoPushResposta>();
        Assert.Equal(_chaves.PublicaEmBase64Url, chave.ChavePublica);

        await Trabalhador(Antonio).Enviar(HttpMethod.Put, "/api/eu/avisos/push", ServicoDePushDosTestes.Inscricao("do-antonio")).ComSucesso();
        await Trabalhador(Antonio).Enviar(HttpMethod.Put, "/api/eu/avisos/push", ServicoDePushDosTestes.Inscricao("antigo-do-antonio")).ComSucesso();
        _servicoDePush.Responder("antigo-do-antonio", HttpStatusCode.Gone);

        Relogio.Acertar(Em(5, 10));
        await Escalador().Enviar(HttpMethod.Post, $"{DaTarde}/escala").ComSucesso();
        await Servicos.GetRequiredService<EntregaDosAvisos>().Entregar(Cancelamento);

        var paraOAntonio = _servicoDePush.Recebidas.Where(recebida => recebida.Endereco.AbsolutePath.EndsWith("do-antonio", StringComparison.Ordinal)).ToList();
        Assert.Equal(2, paraOAntonio.Count);
        var (_, conteudo, cabecalhos) = paraOAntonio[0];
        Assert.Equal(("Você vai trabalhar", "/"), (conteudo.Titulo, conteudo.Endereco));
        Assert.StartsWith("Quinta-feira, 5 de novembro, das 13h às 19h: sinaleiro", conteudo.Texto, StringComparison.Ordinal);
        Assert.Equal(("43200", "high", "aes128gcm"), (cabecalhos["TTL"], cabecalhos["Urgency"], cabecalhos["Content-Encoding"]));
        Assert.StartsWith("vapid t=", cabecalhos["Authorization"], StringComparison.Ordinal);

        // A inscrição que o serviço deu por vencida saiu; o celular que está de pé continua.
        await using var escopo = Servicos.CreateAsyncScope();
        var antonio = (await escopo.ServiceProvider.GetRequiredService<RepositorioDeTrabalhadores>().ObterPelaMatricula(Matricula.De(Antonio), Cancelamento))!.Agregado;
        var inscritos = await escopo.ServiceProvider.GetRequiredService<RepositorioDeInscricoesDePush>().DoTrabalhador(antonio.Id, Cancelamento);
        Assert.Equal(["https://push.example.net/push/do-antonio"], inscritos.Select(inscricao => inscricao.Endereco.AbsoluteUri));

        // E o aviso entregue não vai de novo.
        _servicoDePush.Recebidas.Clear();
        await Servicos.GetRequiredService<EntregaDosAvisos>().Entregar(Cancelamento);
        Assert.Empty(_servicoDePush.Recebidas);
    }

    [Fact]
    public async Task A_inscricao_que_nao_e_de_push_e_recusada_com_o_motivo()
    {
        ComPush();
        await Tarde.Preparar();

        var resposta = await Trabalhador(Antonio).Enviar(
            HttpMethod.Put,
            "/api/eu/avisos/push",
            new InscricaoDoPushPedido("http://push.example.net/x", ServicoDePushDosTestes.ChavePublica, ServicoDePushDosTestes.Segredo));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Equal("O endereço da inscrição de push tem de ser um endereço HTTPS.", (await resposta.Problema()).Detail);
    }

    [Fact]
    public async Task Uma_hora_antes_de_fechar_quem_pode_trabalhar_e_nao_se_habilitou_e_avisado_uma_vez()
    {
        await Tarde.Preparar();
        await Escalador().CadastrarEstivador(4140, "Gilberto Souza");
        var vigia = Servicos.GetRequiredService<VigiaDosPrazos>();

        Relogio.Acertar(Em(5, 8, 30));
        Assert.Equal(0, await vigia.ConferirAsHabilitacoes(Cancelamento));

        Relogio.Acertar(Em(5, 9, 5));
        Assert.Equal(1, await vigia.ConferirAsHabilitacoes(Cancelamento));
        Assert.Equal(0, await vigia.ConferirAsHabilitacoes(Cancelamento));

        var recados = await Trabalhador(4140).Ler("/api/eu/avisos").LerComSucesso<MeusAvisosResposta>();
        var aviso = Assert.Single(recados.Avisos, recado => recado.Tipo == TipoDoAviso.HabilitacaoVaiFechar);
        Assert.Equal("A habilitação para quinta-feira, 5 de novembro, das 13h às 19h fecha às 10h, e você ainda não se habilitou.", aviso.Texto);
        Assert.Empty((await Trabalhador(Antonio).Ler("/api/eu/avisos").LerComSucesso<MeusAvisosResposta>()).Avisos);
    }

    [Fact]
    public async Task O_recado_que_o_servico_recusou_vai_de_novo_e_cada_tipo_abre_a_sua_tela_e_vale_o_seu_tempo()
    {
        ComPush();
        await Tarde.Preparar();
        await Escalador().CadastrarEstivador(4140, "Gilberto Souza");
        await Trabalhador(4140).Enviar(HttpMethod.Put, "/api/eu/avisos/push", ServicoDePushDosTestes.Inscricao("do-gilberto")).ComSucesso();
        var vigia = Servicos.GetRequiredService<VigiaDosPrazos>();
        var entrega = Servicos.GetRequiredService<EntregaDosAvisos>();

        // Às 9h05, o Gilberto não se habilitou, e o exame dele vence em 30/11: dois recados. O serviço de push está fora do ar.
        Relogio.Acertar(Em(5, 9, 5));
        await vigia.ConferirAsHabilitacoes(Cancelamento);
        await vigia.ConferirOsVencimentos(Cancelamento);
        _servicoDePush.Responder("do-gilberto", HttpStatusCode.InternalServerError);
        await entrega.Entregar(Cancelamento);
        Assert.Equal(2, _servicoDePush.Recebidas.Count(recebida => recebida.Endereco.AbsolutePath.EndsWith("do-gilberto", StringComparison.Ordinal)));

        // O serviço voltou e a reserva venceu: os dois vão de novo, porque a recusa não conta como entrega.
        _servicoDePush.Recebidas.Clear();
        _servicoDePush.Responder("do-gilberto", HttpStatusCode.Created);
        Relogio.Acertar(Em(5, 9, 8));
        await entrega.Entregar(Cancelamento);

        var recebidas = _servicoDePush.Recebidas.ToList();
        var habilitacao = Assert.Single(recebidas, recebida => recebida.Conteudo.Titulo == "A habilitação vai fechar");
        var exame = Assert.Single(recebidas, recebida => recebida.Conteudo.Titulo == "O exame médico vai vencer");

        // A habilitação abre a tela de se habilitar, é urgente, e não vale depois que fechar; o exame pode esperar uns dias.
        Assert.Equal(("/habilitacao", "3600", "high"), (habilitacao.Conteudo.Endereco, habilitacao.Cabecalhos["TTL"], habilitacao.Cabecalhos["Urgency"]));
        Assert.Equal(("/meus-dados", "259200", "normal"), (exame.Conteudo.Endereco, exame.Cabecalhos["TTL"], exame.Cabecalhos["Urgency"]));

        // O serviço guarda um recado por tópico: com o mesmo tópico, o do exame apagaria o da habilitação.
        Assert.NotEqual(habilitacao.Cabecalhos["Topic"], exame.Cabecalhos["Topic"]);
    }

    protected override void Configurar(IWebHostBuilder api)
    {
        if (!_comPush)
        {
            return;
        }

        api.UseSetting("Avisos:Push:ChavePublica", _chaves.PublicaEmBase64Url);
        api.UseSetting("Avisos:Push:ChavePrivada", _chaves.PrivadaEmBase64Url);
        api.UseSetting("Avisos:Push:Contato", "mailto:escala@example.com");
        api.ConfigureTestServices(servicos =>
            servicos.AddHttpClient(nameof(EntregaDosAvisos)).ConfigurePrimaryHttpMessageHandler(() => _servicoDePush));
    }

    /// <summary>Liga o push nesta API: as chaves VAPID e o serviço de push de mentira.</summary>
    private void ComPush() => _comPush = true;
}
