using System.Net;
using EscalaAvulsos.Api.Recursos.Escalacoes;
using EscalaAvulsos.Api.Recursos.Escalas;
using EscalaAvulsos.Api.Recursos.Requisicoes;
using EscalaAvulsos.Dominio.Escalacoes;
using EscalaAvulsos.Dominio.Periodos;
using EscalaAvulsos.Infraestrutura.Testes.Persistencia;
using static EscalaAvulsos.Api.Testes.Novembro;
using static EscalaAvulsos.Api.Testes.TardeDeQuintaPelaApi;

namespace EscalaAvulsos.Api.Testes;

/*
 * A escala pelo órgão gestor: feita uma vez por escalação, lida inteira, e
 * cada registro do período — a presença, a falta, a justificativa, o
 * encerramento — com a versão lida (ADR-0016).
 */
public sealed class EscalaPelaApiTests(BancoDeTestes bancos) : TestesDaApi(bancos)
{
    private TardeDeQuintaPelaApi Tarde => new(Escalador, () => Operador(), Trabalhador, Relogio);

    [Fact]
    public async Task A_escala_sai_do_rodizio_das_habilitacoes_e_da_requisicao()
    {
        await Tarde.Preparar();
        Relogio.Acertar(Em(5, 10));

        var resposta = await Escalador().Enviar(HttpMethod.Post, $"{DaTarde}/escala").ComSucesso();

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var escala = await resposta.Ler<EscalaResposta>();
        Assert.Equal($"/api/escalas/{escala.Id}", resposta.Headers.Location?.ToString());
        Assert.Equal("escala da estiva de 05/11/2026, das 13h às 19h: 3 de 3 vagas preenchidas", escala.Descricao);
        Assert.Equal([(Antonio, "Sinaleiro"), (Edson, "Estivador"), (Benedito, "Estivador")], escala.Vagas.Select(vaga => (vaga.Matricula, vaga.Vaga!.Funcao)));
        Assert.Equal([("Estivador", [Carlos, Dirceu, Joao]), ("Sinaleiro", [Carlos])], escala.Espera.Select(espera => (espera.Funcao, espera.Matriculas)));
        Assert.Equal(TimeSpan.FromHours(11), escala.Descanso);
    }

    [Fact]
    public async Task Quem_trabalha_a_tarde_fica_de_fora_da_noite_pelo_descanso()
    {
        // O Edson pegou a tarde; às 11h se habilita para a noite, e às 16h a escala da noite sai sem ele.
        await Tarde.Escalar();
        Relogio.Acertar(Em(5, 11));
        await Operador().Enviar(HttpMethod.Post, "/api/requisicoes", Requisicao(5, Turno.Noite, new VagasPedidas("Estivador", 1)), chave: "noite").ComSucesso();
        await Escalador().Enviar(HttpMethod.Put, "/api/escalacoes/Estiva/2026-11-05/Noite").ComSucesso();
        await Trabalhador(Edson).Enviar(HttpMethod.Put, "/api/eu/habilitacoes/2026-11-05/Noite", new PedidoDeHabilitacao(["Estivador"])).ComSucesso();

        Relogio.Acertar(Em(5, 16));
        var noite = await Escalador().Enviar(HttpMethod.Post, "/api/escalacoes/Estiva/2026-11-05/Noite/escala").LerComSucesso<EscalaResposta>();

        Assert.Empty(noite.Vagas);
        Assert.Equal(
            "Edson Prado (matrícula 4101) ficou fora da escala: tem jornada em 05/11/2026, das 13h às 19h, e o descanso de 11 horas só termina em 06/11/2026, às 6h.",
            Assert.Single(noite.Situacoes).Frase);
    }

    [Fact]
    public async Task Cada_escalacao_tem_uma_escala_so()
    {
        await Tarde.Escalar();

        var deNovo = await Escalador().Enviar(HttpMethod.Post, $"{DaTarde}/escala");

        Assert.Equal(HttpStatusCode.Conflict, deNovo.StatusCode);
        Assert.Equal("A escala da estiva de 05/11/2026, das 13h às 19h, já foi feita: cada escalação tem uma escala só.", (await deNovo.Problema()).Detail);
    }

    [Fact]
    public async Task Antes_de_a_habilitacao_fechar_o_dominio_nao_deixa_escalar()
    {
        await Tarde.Preparar();
        Relogio.Acertar(Em(5, 9, 59));

        var cedo = await Escalador().Enviar(HttpMethod.Post, $"{DaTarde}/escala");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, cedo.StatusCode);
        Assert.Equal(
            "A habilitação para a escala da estiva de 05/11/2026, das 13h às 19h, só fecha em 05/11/2026, às 10h: a escala é feita depois.",
            (await cedo.Problema()).Detail);
    }

    [Fact]
    public async Task A_escala_e_lida_pela_escalacao_e_pelo_endereco_dela()
    {
        var feita = await Tarde.Escalar();

        var pelaEscalacao = await Escalador().Ler($"{DaTarde}/escala").LerComSucesso<EscalaResposta>();
        var peloEndereco = await Escalador().Ler($"/api/escalas/{feita.Id}").ComSucesso();

        Assert.Equal(feita.Id, pelaEscalacao.Id);
        Assert.Equal(1, peloEndereco.Versao());
        Assert.Equal(HttpStatusCode.NotFound, (await Escalador().Ler("/api/escalacoes/Estiva/2026-11-05/Noite/escala")).StatusCode);
    }

    [Fact]
    public async Task A_presenca_a_falta_a_justificativa_e_o_encerramento_andam_versao_a_versao()
    {
        var escala = await Tarde.Escalar();
        var escalador = Escalador();
        var endereco = $"/api/escalas/{escala.Id}";

        Relogio.Acertar(Em(5, 12, 40));
        var versao = (await escalador.Enviar(HttpMethod.Post, $"{endereco}/presencas", new TrabalhadorDaEscala(Antonio), 1).ComSucesso()).Versao();
        Relogio.Acertar(Em(5, 14));
        versao = (await escalador.Enviar(HttpMethod.Post, $"{endereco}/faltas", new TrabalhadorDaEscala(Edson), versao).ComSucesso()).Versao();
        versao = (await escalador.Enviar(HttpMethod.Post, $"{endereco}/justificativas", new JustificativaDaFalta(Edson, "Atestado médico"), versao).ComSucesso()).Versao();
        Relogio.Acertar(Em(5, 19));
        var encerrada = await escalador.Enviar(HttpMethod.Post, $"{endereco}/encerramento", versao: versao).LerComSucesso<EscalaResposta>();

        Assert.NotNull(encerrada.EncerradaEm);
        Assert.Equal(
            [Comparecimento.Presente, Comparecimento.FaltaJustificada, Comparecimento.Falta],
            encerrada.Vagas.Select(vaga => vaga.Comparecimento));
        Assert.Equal(
            "Edson Prado (matrícula 4101) está na escala: estivador no terno 1 do navio Atlântico Sul (IMO 9876543), no Terminal Cais Leste, berço 4, em 05/11/2026, das 13h às 19h. Faltou, com justificativa: Atestado médico.",
            encerrada.Vagas[1].Frase);
    }

    [Fact]
    public async Task O_registro_a_partir_de_uma_versao_velha_e_412_e_a_matricula_de_fora_e_422()
    {
        var escala = await Tarde.Escalar();
        var escalador = Escalador();
        Relogio.Acertar(Em(5, 12, 40));
        await escalador.Enviar(HttpMethod.Post, $"/api/escalas/{escala.Id}/presencas", new TrabalhadorDaEscala(Antonio), 1).ComSucesso();

        var velha = await escalador.Enviar(HttpMethod.Post, $"/api/escalas/{escala.Id}/presencas", new TrabalhadorDaEscala(Edson), 1);
        var deFora = await escalador.Enviar(HttpMethod.Post, $"/api/escalas/{escala.Id}/presencas", new TrabalhadorDaEscala(4999), 2);

        Assert.Equal(HttpStatusCode.PreconditionFailed, velha.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, deFora.StatusCode);
        Assert.Equal("A matrícula 4999 não se habilitou para a escala da estiva de 05/11/2026, das 13h às 19h.", (await deFora.Problema()).Detail);
    }

    [Fact]
    public async Task O_trabalhador_e_o_operador_nao_leem_a_escala_inteira()
    {
        var escala = await Tarde.Escalar();

        Assert.Equal(HttpStatusCode.Forbidden, (await Trabalhador(Antonio).Ler($"/api/escalas/{escala.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Operador().Ler($"/api/escalas/{escala.Id}")).StatusCode);
    }
}
