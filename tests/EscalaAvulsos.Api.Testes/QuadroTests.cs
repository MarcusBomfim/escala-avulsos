using System.Net;
using EscalaAvulsos.Api.Recursos.Quadro;
using EscalaAvulsos.Dominio.Trabalhadores;
using EscalaAvulsos.Infraestrutura.Testes.Persistencia;
using static EscalaAvulsos.Api.Testes.Novembro;
using static EscalaAvulsos.Api.Testes.TardeDeQuintaPelaApi;

namespace EscalaAvulsos.Api.Testes;

/*
 * O quadro do período, como o de parede (ADR-0021): a escalação de cada
 * categoria, e em cada terno pedido as vagas, as escaladas e as em aberto.
 * O escalador vê tudo; o operador, só as requisições dele.
 */
public sealed class QuadroTests(BancoDeTestes bancos) : TestesDaApi(bancos)
{
    private const string DaQuinta = "/api/quadro/2026-11-05/Tarde";

    private TardeDeQuintaPelaApi Tarde => new(Escalador, () => Operador(), Trabalhador, Relogio);

    [Fact]
    public async Task O_quadro_acompanha_a_escalacao_da_habilitacao_aberta_ate_a_escala()
    {
        await Tarde.Preparar();

        var aberta = await Escalador().Ler(DaQuinta).LerComSucesso<QuadroResposta>();

        var estiva = Assert.Single(aberta.Categorias);
        Assert.Equal((Categoria.Estiva, SituacaoDaCategoriaNoQuadro.HabilitacaoAberta, 6), (estiva.Categoria, estiva.Situacao, estiva.Habilitados));
        Assert.Equal((Em(5, 1), Em(5, 10)), (estiva.AbreEm, estiva.FechaEm));
        var terno = Assert.Single(estiva.Ternos);
        Assert.Equal(("Atlântico Sul", 1, 3, 0, 3, SituacaoDoTernoNoQuadro.AguardandoAEscala), (terno.Navio.Nome, terno.Terno, terno.Pedidas, terno.Escaladas, terno.EmAberto, terno.Situacao));

        // Às 10h a habilitação fechou: travada, até a escala sair.
        Relogio.Acertar(Em(5, 10));
        Assert.Equal(SituacaoDaCategoriaNoQuadro.Travada, Assert.Single((await Escalador().Ler(DaQuinta).LerComSucesso<QuadroResposta>()).Categorias).Situacao);

        await Escalador().Enviar(HttpMethod.Post, $"{DaTarde}/escala").ComSucesso();
        var escalada = Assert.Single((await Escalador().Ler(DaQuinta).LerComSucesso<QuadroResposta>()).Categorias);

        Assert.Equal(SituacaoDaCategoriaNoQuadro.Escalada, escalada.Situacao);
        var completo = Assert.Single(escalada.Ternos);
        Assert.Equal((3, 0, SituacaoDoTernoNoQuadro.Completo), (completo.Escaladas, completo.EmAberto, completo.Situacao));
        Assert.Equal(
            [("Sinaleiro", 1, "Antônio Ferreira"), ("Estivador", 2, "Edson Prado, Benedito Lima")],
            completo.Funcoes.Select(funcao => (funcao.Funcao, funcao.Pedidas, string.Join(", ", funcao.Escalados.Select(pessoa => pessoa.Nome)))));
    }

    [Fact]
    public async Task Antes_de_abrir_a_habilitacao_vai_abrir_e_depois_do_periodo_a_escala_encerrada_fica_encerrada()
    {
        var escala = await Tarde.Escalar();

        // A escalação da sexta à tarde, aberta na quinta às 10h05: a habilitação dela só abre na sexta, à 1h.
        Relogio.Acertar(Em(5, 10, 5));
        await Escalador().Enviar(HttpMethod.Put, "/api/escalacoes/Estiva/2026-11-06/Tarde").ComSucesso();
        Assert.Equal(SituacaoDaCategoriaNoQuadro.HabilitacaoVaiAbrir, Assert.Single((await Escalador().Ler("/api/quadro/2026-11-06/Tarde").LerComSucesso<QuadroResposta>()).Categorias).Situacao);

        Relogio.Acertar(Em(5, 19));
        await Escalador().Enviar(HttpMethod.Post, $"/api/escalas/{escala.Id}/encerramento", versao: 1).ComSucesso();

        Assert.Equal(SituacaoDaCategoriaNoQuadro.Encerrada, Assert.Single((await Escalador().Ler(DaQuinta).LerComSucesso<QuadroResposta>()).Categorias).Situacao);
    }

    [Fact]
    public async Task O_operador_ve_so_os_ternos_dele_e_o_trabalhador_nao_ve_o_quadro()
    {
        await Tarde.Escalar();

        var daCaisForte = await Operador().Ler(DaQuinta).LerComSucesso<QuadroResposta>();
        var deOutro = await Operador("11.444.777/0001-61", "Porto Novo Logística").Ler(DaQuinta).LerComSucesso<QuadroResposta>();
        var doTrabalhador = await Trabalhador(Antonio).Ler(DaQuinta);

        Assert.Equal("Cais Forte Operações Portuárias", Assert.Single(Assert.Single(daCaisForte.Categorias).Ternos).Operador.Nome);
        Assert.Empty(deOutro.Categorias);
        Assert.Equal(HttpStatusCode.Forbidden, doTrabalhador.StatusCode);
    }

    [Fact]
    public async Task A_requisicao_que_chega_depois_da_escala_fica_fora_do_prazo_sem_vaga_em_aberto()
    {
        await Tarde.Escalar();
        Relogio.Acertar(Em(5, 10, 30));
        await Operador()
            .Enviar(HttpMethod.Post, "/api/requisicoes", Requisicao(5, Dominio.Periodos.Turno.Tarde, new Recursos.Requisicoes.VagasPedidas("Estivador", 1)), chave: "atrasada")
            .ComSucesso();

        var ternos = Assert.Single((await Escalador().Ler(DaQuinta).LerComSucesso<QuadroResposta>()).Categorias).Ternos;

        Assert.Equal(
            [(3, 0, SituacaoDoTernoNoQuadro.Completo), (1, 0, SituacaoDoTernoNoQuadro.ForaDoPrazo)],
            ternos.Select(terno => (terno.Pedidas, terno.EmAberto, terno.Situacao)));
    }

    [Fact]
    public async Task O_terno_que_perdeu_quem_desistiu_sem_ninguem_na_espera_fica_com_a_vaga_em_aberto()
    {
        await Tarde.Escalar();
        var escala = (await Trabalhador(Edson).Ler("/api/eu/escalas?de=2026-11-05").LerComSucesso<IReadOnlyList<Recursos.Escalas.MinhaEscalaResposta>>())[0].Escala;

        // Saem da vaga, um a um, o Edson e os três da espera: a última desistência deixa a vaga vazia.
        Relogio.Acertar(Em(5, 11));
        foreach (var quem in (int[])[Edson, Carlos, Dirceu, Joao])
        {
            await Trabalhador(quem).Enviar(HttpMethod.Post, $"/api/eu/escalas/{escala}/desistencia").ComSucesso();
        }

        var terno = Assert.Single(Assert.Single((await Escalador().Ler(DaQuinta).LerComSucesso<QuadroResposta>()).Categorias).Ternos);

        Assert.Equal((2, 1, SituacaoDoTernoNoQuadro.Incompleto), (terno.Escaladas, terno.EmAberto, terno.Situacao));
    }
}
