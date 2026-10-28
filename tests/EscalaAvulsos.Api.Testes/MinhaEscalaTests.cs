using System.Net;
using EscalaAvulsos.Api.Recursos.Escalas;
using EscalaAvulsos.Dominio.Escalacoes;
using EscalaAvulsos.Infraestrutura.Testes.Persistencia;
using static EscalaAvulsos.Api.Testes.Novembro;
using static EscalaAvulsos.Api.Testes.TardeDeQuintaPelaApi;

namespace EscalaAvulsos.Api.Testes;

/*
 * O trabalhador e a escala dele, no celular: onde trabalha, ou por que ficou
 * de fora e em que lugar da espera; a desistência sem versão; e o mês
 * (ADR-0016).
 */
public sealed class MinhaEscalaTests(BancoDeTestes bancos) : TestesDaApi(bancos)
{
    private TardeDeQuintaPelaApi Tarde => new(Escalador, () => Operador(), Trabalhador, Relogio);

    [Fact]
    public async Task Quem_esta_na_escala_ve_a_vaga_com_tudo_o_que_precisa_para_chegar()
    {
        await Tarde.Escalar();

        var minha = Assert.Single(await Trabalhador(Antonio).Ler("/api/eu/escalas?de=2026-11-05").LerComSucesso<IReadOnlyList<MinhaEscalaResposta>>());

        Assert.Equal("05/11/2026, das 13h às 19h", minha.Periodo.Descricao);
        Assert.Equal(
            "Antônio Ferreira (matrícula 4127) está na escala: sinaleiro no terno 1 do navio Atlântico Sul (IMO 9876543), no Terminal Cais Leste, berço 4, em 05/11/2026, das 13h às 19h.",
            minha.Situacao.Frase);
        var vaga = minha.Situacao.Vaga!;
        Assert.Equal(("Sinaleiro", 1, "Atlântico Sul", "Terminal Cais Leste", "4"), (vaga.Funcao, vaga.Terno, vaga.Navio.Nome, vaga.Local.Terminal, vaga.Local.Berco));
        Assert.Empty(minha.Espera);
    }

    [Fact]
    public async Task Quem_ficou_de_fora_ve_o_motivo_e_o_lugar_na_espera()
    {
        await Tarde.Escalar();

        var doCarlos = Assert.Single(await Trabalhador(Carlos).Ler("/api/eu/escalas?de=2026-11-05").LerComSucesso<IReadOnlyList<MinhaEscalaResposta>>());
        var doJoao = Assert.Single(await Trabalhador(Joao).Ler("/api/eu/escalas?de=2026-11-05").LerComSucesso<IReadOnlyList<MinhaEscalaResposta>>());

        Assert.Null(doCarlos.Situacao.Vaga);
        Assert.Equal([new PosicaoNaEspera("Sinaleiro", 1), new PosicaoNaEspera("Estivador", 1)], doCarlos.Espera);
        Assert.Equal(
            "Carlos Mota (matrícula 4152) ficou fora da escala: como sinaleiro, o rodízio parou 1 posição antes da sua vez; como estivador, o rodízio parou 1 posição antes da sua vez.",
            doCarlos.Situacao.Frase);
        Assert.Equal([new PosicaoNaEspera("Estivador", 3)], doJoao.Espera);
    }

    [Fact]
    public async Task Sem_o_primeiro_dia_a_tela_comeca_hoje_no_relogio_do_porto_e_mostra_uma_semana()
    {
        await Tarde.Escalar();

        Relogio.Acertar(Em(5, 2, 30));
        var hoje = await Trabalhador(Antonio).Ler("/api/eu/escalas").LerComSucesso<IReadOnlyList<MinhaEscalaResposta>>();
        Relogio.Acertar(Em(6, 2, 30));
        var amanha = await Trabalhador(Antonio).Ler("/api/eu/escalas").LerComSucesso<IReadOnlyList<MinhaEscalaResposta>>();

        // 29/10, às 22h no porto, já é 30/10 em UTC: a semana é de 29/10 a 04/11, e a tarde de 05/11 fica de fora.
        Relogio.Acertar(new DateTimeOffset(2026, 10, 29, 22, 0, 0, TimeSpan.FromHours(-3)));
        var naNoiteDe29 = await Trabalhador(Antonio).Ler("/api/eu/escalas").LerComSucesso<IReadOnlyList<MinhaEscalaResposta>>();
        var umaSemanaAntes = await Trabalhador(Antonio).Ler("/api/eu/escalas?de=2026-10-30").LerComSucesso<IReadOnlyList<MinhaEscalaResposta>>();
        var umDiaAMais = await Trabalhador(Antonio).Ler("/api/eu/escalas?de=2026-10-29").LerComSucesso<IReadOnlyList<MinhaEscalaResposta>>();

        Assert.Single(hoje);
        Assert.Empty(amanha);
        Assert.Empty(naNoiteDe29);
        Assert.Single(umaSemanaAntes);
        Assert.Empty(umDiaAMais);
    }

    [Fact]
    public async Task A_desistencia_da_a_vaga_ao_primeiro_da_espera_sem_versao()
    {
        var escala = await Tarde.Escalar();
        Relogio.Acertar(Em(5, 11));

        var doEdson = await Trabalhador(Edson).Enviar(HttpMethod.Post, $"/api/eu/escalas/{escala.Id}/desistencia").LerComSucesso<MinhaEscalaResposta>();
        var doCarlos = Assert.Single(await Trabalhador(Carlos).Ler("/api/eu/escalas?de=2026-11-05").LerComSucesso<IReadOnlyList<MinhaEscalaResposta>>());

        Assert.Equal("Edson Prado (matrícula 4101) desistiu da vaga de estivador em 05/11/2026, às 11h.", doEdson.Situacao.Frase);
        Assert.True(doCarlos.Situacao.Substituto);
        Assert.Equal("Estivador", doCarlos.Situacao.Vaga?.Funcao);
    }

    [Fact]
    public async Task Duas_desistencias_ao_mesmo_tempo_dao_cada_vaga_a_um_da_espera()
    {
        // O Edson e o Benedito desistem no mesmo instante: o servidor relê e refaz, e entram o Carlos e o Dirceu.
        var escala = await Tarde.Escalar();
        Relogio.Acertar(Em(5, 11));

        var respostas = await Task.WhenAll(
            Trabalhador(Edson).Enviar(HttpMethod.Post, $"/api/eu/escalas/{escala.Id}/desistencia"),
            Trabalhador(Benedito).Enviar(HttpMethod.Post, $"/api/eu/escalas/{escala.Id}/desistencia"));

        Assert.All(respostas, resposta => Assert.Equal(HttpStatusCode.OK, resposta.StatusCode));
        var depois = await Escalador().Ler($"/api/escalas/{escala.Id}").LerComSucesso<EscalaResposta>();
        Assert.Equal([Antonio, Carlos, Dirceu], depois.Vagas.Select(vaga => vaga.Matricula).Order());
    }

    [Fact]
    public async Task A_escala_de_que_ele_nao_participa_e_404_e_depois_do_inicio_nao_se_desiste()
    {
        var escala = await Tarde.Escalar();
        await Escalador().CadastrarEstivador(4999, "Fábio Rocha");

        var deFora = await Trabalhador(4999).Enviar(HttpMethod.Post, $"/api/eu/escalas/{escala.Id}/desistencia");
        Relogio.Acertar(Em(5, 13));
        var tarde = await Trabalhador(Edson).Enviar(HttpMethod.Post, $"/api/eu/escalas/{escala.Id}/desistencia");

        Assert.Equal(HttpStatusCode.NotFound, deFora.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, tarde.StatusCode);
        Assert.Equal(
            "O período de 05/11/2026, das 13h às 19h, já começou: quem não vai trabalhar fica com falta, e não desiste da vaga.",
            (await tarde.Problema()).Detail);
    }

    [Fact]
    public async Task O_mes_traz_a_frequencia_e_o_extrato_pela_tabela_do_porto()
    {
        var escala = await Tarde.Escalar();
        var escalador = Escalador();
        Relogio.Acertar(Em(5, 12, 40));
        var versao = (await escalador.Enviar(HttpMethod.Post, $"/api/escalas/{escala.Id}/presencas", new TrabalhadorDaEscala(Antonio), 1).ComSucesso()).Versao();
        Relogio.Acertar(Em(5, 19));
        await escalador.Enviar(HttpMethod.Post, $"/api/escalas/{escala.Id}/encerramento", versao: versao).ComSucesso();
        Relogio.Acertar(Em(6, 9));

        var mes = await Trabalhador(Antonio).Ler("/api/eu/meses/2026-11").LerComSucesso<MeuMesResposta>();

        Assert.Equal("11/2026", mes.Mes);
        Assert.Equal((1, 12, 11, 1, 0), (mes.Frequencia.Habilitacoes, mes.Frequencia.MinimoDeHabilitacoes, mes.Frequencia.HabilitacoesQueFaltam, mes.Frequencia.Trabalhados, mes.Frequencia.Faltas));
        Assert.Equal(
            "05/11/2026, das 13h às 19h — sinaleiro no navio Atlântico Sul (IMO 9876543): R$ 260,00, pago até 07/11/2026, às 19h",
            Assert.Single(mes.Extrato.Linhas).Descricao);
        Assert.Equal((260.00m, 28.89m, 21.67m, 24.84m), (mes.Extrato.Remuneracao, mes.Extrato.Ferias, mes.Extrato.DecimoTerceiro, mes.Extrato.Fgts));
    }

    [Fact]
    public async Task O_mes_fora_do_formato_e_400()
    {
        await Escalador().CadastrarEstivador(Antonio, "Antônio Ferreira");

        var resposta = await Trabalhador(Antonio).Ler("/api/eu/meses/11-2026");

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Equal("O valor de mês é um mês, como 2026-11; veio \"11-2026\".", (await resposta.Problema()).Detail);
    }
}
