using EscalaAvulsos.Dominio.Comum;
using EscalaAvulsos.Dominio.Requisicoes;
using EscalaAvulsos.Dominio.Testes.Trabalhadores;
using EscalaAvulsos.Dominio.Trabalhadores;
using static EscalaAvulsos.Dominio.Testes.Requisicoes.RequisicoesDeTeste;

namespace EscalaAvulsos.Dominio.Testes.Requisicoes;

public sealed class RequisicaoTests
{
    [Fact]
    public void Nasce_aberta_com_os_ternos_numerados_na_ordem()
    {
        var requisicao = DoisTernos();

        Assert.Equal((SituacaoDaRequisicao.Aberta, Categoria.Estiva, QuartaAs10h), (requisicao.Situacao, requisicao.Categoria, requisicao.FeitaEm));
        Assert.Equal((CaisForte, AtlanticoSul, CaisLeste, QuintaDeManha), (requisicao.Operador, requisicao.Navio, requisicao.Local, requisicao.Periodo));
        Assert.Equal([1, 2], requisicao.Ternos.Select(terno => terno.Numero));
        Assert.Equal([11, 11], requisicao.Ternos.Select(terno => terno.TotalDeVagas));
        Assert.Equal(22, requisicao.TotalDeVagas);
        Assert.Null(requisicao.CanceladaEm);
        Assert.Null(requisicao.MotivoDoCancelamento);
    }

    [Fact]
    public void Soma_as_vagas_de_cada_funcao_na_ordem_em_que_a_funcao_aparece()
    {
        VagasDaFuncao[] ternoMenor = [new(Funcoes.Estivador, 6), new(Funcoes.ContramestreDeTerno, 1)];

        var requisicao = new Requisicao(Dados, [TernoDeConteiner(), ternoMenor], QuartaAs10h);

        Assert.Equal(
            [
                new VagasDaFuncao(Funcoes.ContramestreDeTerno, 2),
                new VagasDaFuncao(Funcoes.Sinaleiro, 1),
                new VagasDaFuncao(Funcoes.OperadorDeGuindaste, 1),
                new VagasDaFuncao(Funcoes.Estivador, 14),
            ],
            requisicao.VagasPorFuncao);
        Assert.Equal(18, requisicao.TotalDeVagas);
        Assert.Equal("terno 2 — estivador: 6, contramestre de terno: 1", requisicao.Ternos[1].ToString());
    }

    [Fact]
    public void Se_escreve_como_o_pedido_do_operador()
    {
        Assert.Equal(
            "Cais Forte Operações Portuárias pede 2 ternos da estiva, 22 vagas, para o navio Atlântico Sul (IMO 9876543), no Terminal Cais Leste, berço 4, em 05/11/2026, das 7h às 13h.",
            DoisTernos().ToString());

        Assert.Equal(
            "Cais Forte Operações Portuárias pede 1 terno da estiva, 1 vaga, para o navio Atlântico Sul (IMO 9876543), no Terminal Cais Leste, berço 4, em 05/11/2026, das 7h às 13h.",
            new Requisicao(Dados, [[new(Funcoes.Estivador, 1)]], QuartaAs10h).ToString());
    }

    [Fact]
    public void Diz_tudo_o_que_esta_errado_no_pedido_de_uma_vez()
    {
        var erro = Assert.Throws<ExcecaoDeDominio>(() => new Requisicao(
            Dados,
            [
                [new(Funcoes.Estivador, 0), new(new Funcao(Categoria.Estiva, "ESTIVADOR"), 2), new(Funcoes.Sinaleiro, -1)],
                [],
                [new(Funcoes.Conferente, 1)],
            ],
            QuintaDeManha.Inicio));

        Assert.Equal(
            "A requisição tem de chegar antes do período, e o de 05/11/2026, das 7h às 13h, já começou. "
            + "No terno 1, as vagas de estivador têm de ser pelo menos 1, e não 0. "
            + "No terno 1, as vagas de sinaleiro têm de ser pelo menos 1, e não -1. "
            + "O terno 1 repete estivador: junte as vagas da função numa linha só. "
            + "O terno 2 não tem vaga nenhuma. "
            + "A requisição mistura a estiva e a conferência de carga: cada categoria tem o seu rodízio, e a sua requisição.",
            erro.Message);
    }

    [Fact]
    public void Precisa_de_pelo_menos_um_terno()
    {
        var erro = Assert.Throws<ExcecaoDeDominio>(() => new Requisicao(Dados, [], QuartaAs10h));

        Assert.Equal("A requisição precisa de pelo menos um terno.", erro.Message);
    }

    [Fact]
    public void Chega_ate_o_instante_antes_de_o_periodo_comecar()
    {
        var noUltimoInstante = new Requisicao(Dados, [TernoDeConteiner()], QuintaDeManha.Inicio.AddTicks(-1));

        Assert.Equal(SituacaoDaRequisicao.Aberta, noUltimoInstante.Situacao);
        Assert.Throws<ExcecaoDeDominio>(() => new Requisicao(Dados, [TernoDeConteiner()], QuintaDeManha.Inicio));
        Assert.Throws<ExcecaoDeDominio>(() => new Requisicao(Dados, [TernoDeConteiner()], QuintaDeManha.Fim));
    }

    [Fact]
    public void Mistura_de_tres_categorias_e_dita_por_inteiro()
    {
        var erro = Assert.Throws<ExcecaoDeDominio>(() => new Requisicao(
            Dados,
            [[new(Funcoes.Estivador, 8), new(Funcoes.Conferente, 1)], [new(new Funcao(Categoria.Bloco, "Trabalhador de bloco"), 4)]],
            QuartaAs10h));

        Assert.Equal(
            "A requisição mistura a estiva, a conferência de carga e o bloco: cada categoria tem o seu rodízio, e a sua requisição.",
            erro.Message);
    }

    [Fact]
    public void A_categoria_e_a_das_funcoes_pedidas()
    {
        var conferencia = new Requisicao(Dados, [[new(Funcoes.Conferente, 2)]], QuartaAs10h);

        Assert.Equal(Categoria.ConferenciaDeCarga, conferencia.Categoria);
    }

    [Fact]
    public void Os_ternos_nao_mudam_se_a_lista_do_operador_mudar()
    {
        var terno = TernoDeConteiner();
        var requisicao = new Requisicao(Dados, [terno], QuartaAs10h);

        terno[3] = new VagasDaFuncao(Funcoes.Estivador, 80);

        Assert.Equal(11, requisicao.TotalDeVagas);
    }

    [Fact]
    public void Cancela_com_motivo_antes_de_o_periodo_comecar()
    {
        var requisicao = DoisTernos();
        var quartaAs22h = QuartaAs10h.AddHours(12);

        requisicao.Cancelar("  Navio atrasou a atracação ", quartaAs22h);

        Assert.Equal(
            (SituacaoDaRequisicao.Cancelada, quartaAs22h, "Navio atrasou a atracação"),
            (requisicao.Situacao, requisicao.CanceladaEm!.Value, requisicao.MotivoDoCancelamento!));

        var erro = Assert.Throws<ExcecaoDeDominio>(() => requisicao.Cancelar("De novo", quartaAs22h));

        Assert.Equal("A requisição já foi cancelada: Navio atrasou a atracação.", erro.Message);
    }

    [Fact]
    public void Depois_que_o_periodo_comeca_nao_cancela_mais()
    {
        var requisicao = DoisTernos();

        var erro = Assert.Throws<ExcecaoDeDominio>(() => requisicao.Cancelar("Chuva", QuintaDeManha.Inicio));

        Assert.Equal("O período de 05/11/2026, das 7h às 13h, já começou: a requisição não pode mais ser cancelada.", erro.Message);
        Assert.Equal(SituacaoDaRequisicao.Aberta, requisicao.Situacao);
    }

    [Theory]
    [InlineData(null, "Falta o motivo do cancelamento.")]
    [InlineData(" ", "Falta o motivo do cancelamento.")]
    public void O_cancelamento_precisa_de_motivo(string? motivo, string mensagem)
    {
        var requisicao = DoisTernos();

        var erro = Assert.Throws<ExcecaoDeDominio>(() => requisicao.Cancelar(motivo!, QuartaAs10h));

        Assert.Equal(mensagem, erro.Message);
        Assert.Equal(SituacaoDaRequisicao.Aberta, requisicao.Situacao);
    }

    [Fact]
    public void O_motivo_do_cancelamento_tem_no_maximo_duzentos_caracteres()
    {
        var erro = Assert.Throws<ExcecaoDeDominio>(() => DoisTernos().Cancelar(new string('a', 201), QuartaAs10h));

        Assert.Equal("O motivo do cancelamento passa de 200 caracteres.", erro.Message);
    }

    [Fact]
    public void Cada_requisicao_tem_a_sua_identidade()
    {
        Assert.NotEqual(DoisTernos().Id, DoisTernos().Id);
    }

    [Fact]
    public void Ids_de_tipos_diferentes_nunca_sao_iguais_nem_com_o_mesmo_guid()
    {
        var valor = Guid.CreateVersion7();

        Assert.Equal(new RequisicaoId(valor), new RequisicaoId(valor));
        Assert.NotEqual<object>(new RequisicaoId(valor), new TrabalhadorId(valor));
        Assert.Throws<ExcecaoDeDominio>(() => new RequisicaoId(Guid.Empty));
    }
}
