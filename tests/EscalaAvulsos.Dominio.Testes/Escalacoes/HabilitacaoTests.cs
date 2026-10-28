using EscalaAvulsos.Dominio.Comum;
using EscalaAvulsos.Dominio.Escalacoes;
using EscalaAvulsos.Dominio.Identificacao;
using EscalaAvulsos.Dominio.Periodos;
using EscalaAvulsos.Dominio.Testes.Trabalhadores;
using EscalaAvulsos.Dominio.Trabalhadores;
using static EscalaAvulsos.Dominio.Testes.Escalacoes.EscalacoesDeTeste;

namespace EscalaAvulsos.Dominio.Testes.Escalacoes;

public sealed class HabilitacaoTests
{
    [Fact]
    public void Habilita_nas_funcoes_escolhidas_sem_repetir_nenhuma()
    {
        var escalacao = DaEstiva();
        var antonio = Sinaleiro();

        var habilitacao = escalacao.Habilitar(antonio, [Funcoes.Sinaleiro, Funcoes.Estivador, new Funcao(Categoria.Estiva, "SINALEIRO")], QuintaAs6h);

        Assert.Equal((antonio.Id, antonio.Matricula, QuintaAs6h), (habilitacao.Trabalhador, habilitacao.Matricula, habilitacao.Em));
        Assert.Equal([Funcoes.Sinaleiro, Funcoes.Estivador], habilitacao.Funcoes);
        Assert.Equal(habilitacao, Assert.Single(escalacao.Habilitacoes));
        Assert.Equal(habilitacao, escalacao.HabilitacaoDe(antonio.Id));
    }

    [Fact]
    public void A_janela_vai_da_abertura_incluida_ate_o_fechamento()
    {
        var escalacao = DaEstiva();

        Assert.False(escalacao.HabilitacaoAbertaEm(Em(5, 1).AddTicks(-1)));
        Assert.True(escalacao.HabilitacaoAbertaEm(Em(5, 1)));
        Assert.True(escalacao.HabilitacaoAbertaEm(Em(5, 10).AddTicks(-1)));
        Assert.False(escalacao.HabilitacaoAbertaEm(Em(5, 10)));

        escalacao.Habilitar(TrabalhadoresDeTeste.Estivador(), [Funcoes.Estivador], Em(5, 1));
        escalacao.Habilitar(TrabalhadoresDeTeste.Estivador(4130, "Benedito Lima"), [Funcoes.Estivador], Em(5, 10).AddTicks(-1));

        Assert.Equal(2, escalacao.Habilitacoes.Count);
    }

    [Fact]
    public void Antes_de_abrir_diz_quando_abre()
    {
        var erro = Assert.Throws<ExcecaoDeDominio>(() => DaEstiva().Habilitar(TrabalhadoresDeTeste.Estivador(), [Funcoes.Estivador], Em(5, 0, 59)));

        Assert.Equal("A habilitação para a escala da estiva de 05/11/2026, das 13h às 19h, abre em 05/11/2026, à 1h.", erro.Message);
    }

    [Fact]
    public void Depois_de_fechar_diz_quando_fechou()
    {
        var erro = Assert.Throws<ExcecaoDeDominio>(() => DaEstiva().Habilitar(TrabalhadoresDeTeste.Estivador(), [Funcoes.Estivador], Em(5, 10)));

        Assert.Equal("A habilitação para a escala da estiva de 05/11/2026, das 13h às 19h, fechou em 05/11/2026, às 10h.", erro.Message);
    }

    [Fact]
    public void Os_horarios_se_escrevem_como_se_fala()
    {
        var aoMeioDia = new Escalacao(Categoria.Estiva, QuintaATarde, new CalendarioDaEscalacao(TimeSpan.FromHours(3), TimeSpan.FromHours(1), TimeSpan.FromMinutes(30)));
        var antonio = TrabalhadoresDeTeste.Estivador();

        Assert.EndsWith("abre em 05/11/2026, ao meio-dia.", Assert.Throws<ExcecaoDeDominio>(() => aoMeioDia.Habilitar(antonio, [Funcoes.Estivador], Em(5, 11))).Message, StringComparison.Ordinal);
        Assert.EndsWith("fechou em 05/11/2026, às 12h30.", Assert.Throws<ExcecaoDeDominio>(() => aoMeioDia.Habilitar(antonio, [Funcoes.Estivador], Em(5, 13))).Message, StringComparison.Ordinal);

        var aMeiaNoite = new Escalacao(
            Categoria.Estiva, Periodo.De(new DateOnly(2026, 11, 5), Turno.Noite), new CalendarioDaEscalacao(TimeSpan.FromHours(19), TimeSpan.FromHours(19), new TimeSpan(18, 30, 0)));

        Assert.EndsWith("abre em 05/11/2026, à meia-noite.", Assert.Throws<ExcecaoDeDominio>(() => aMeiaNoite.Habilitar(antonio, [Funcoes.Estivador], Em(4, 23))).Message, StringComparison.Ordinal);
        Assert.EndsWith("fechou em 05/11/2026, à 0h30.", Assert.Throws<ExcecaoDeDominio>(() => aMeiaNoite.Habilitar(antonio, [Funcoes.Estivador], Em(5, 2))).Message, StringComparison.Ordinal);

        var cincoMinutosDepois = new Escalacao(Categoria.Estiva, QuintaATarde, new CalendarioDaEscalacao(TimeSpan.FromHours(3), TimeSpan.FromHours(12), new TimeSpan(2, 55, 0)));

        Assert.EndsWith("fechou em 05/11/2026, às 10h05.", Assert.Throws<ExcecaoDeDominio>(() => cincoMinutosDepois.Habilitar(antonio, [Funcoes.Estivador], Em(5, 11))).Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Quem_e_de_outra_categoria_nao_entra_nesta_escala()
    {
        var marta = new Trabalhador(new DadosDoTrabalhador(
            Matricula.De(7002), "Marta Rocha", Cpf.De("111.444.777-35"), Categoria.ConferenciaDeCarga, Inscricao.Registrado));

        var erro = Assert.Throws<ExcecaoDeDominio>(() => DaEstiva().Habilitar(marta, [Funcoes.Conferente], QuintaAs6h));

        Assert.Equal("Marta Rocha (matrícula 7002) é da conferência de carga, e esta é a escala da estiva de 05/11/2026, das 13h às 19h.", erro.Message);
    }

    [Fact]
    public void Precisa_de_pelo_menos_uma_funcao()
    {
        var erro = Assert.Throws<ExcecaoDeDominio>(() => DaEstiva().Habilitar(TrabalhadoresDeTeste.Estivador(), [], QuintaAs6h));

        Assert.Equal("Escolha pelo menos uma função para se habilitar.", erro.Message);
    }

    [Fact]
    public void Na_funcao_que_nao_pode_exercer_e_recusado_na_hora_com_o_motivo()
    {
        var escalacao = DaEstiva();

        var erro = Assert.Throws<ExcecaoDeDominio>(() => escalacao.Habilitar(TrabalhadoresDeTeste.Estivador(), [Funcoes.OperadorDeGuindaste], QuintaAs6h));

        Assert.Equal(
            "Antônio Ferreira (matrícula 4127) não pode se habilitar como operador de guindaste de bordo: não tem a qualificação de operador de guindaste de bordo.",
            erro.Message);
        Assert.Empty(escalacao.Habilitacoes);
    }

    [Fact]
    public void O_motivo_que_vale_para_todas_as_funcoes_aparece_uma_vez_so()
    {
        var dezembro = new Escalacao(Categoria.Estiva, Periodo.De(new DateOnly(2026, 12, 3), Turno.Tarde), CalendarioDaEscalacao.Padrao);
        var antonio = Sinaleiro();

        var erro = Assert.Throws<ExcecaoDeDominio>(() => dezembro.Habilitar(
            antonio, [Funcoes.Estivador, Funcoes.Sinaleiro], new DateTimeOffset(2026, 12, 3, 6, 0, 0, TimeSpan.FromHours(-3))));

        Assert.Equal(
            "Antônio Ferreira (matrícula 4127) não pode se habilitar como estivador nem como sinaleiro: o exame médico (ASO) vale só até 30/11/2026.",
            erro.Message);
    }

    [Fact]
    public void Tres_funcoes_recusadas_sao_ditas_numa_frase()
    {
        var erro = Assert.Throws<ExcecaoDeDominio>(() => DaEstiva().Habilitar(
            TrabalhadoresDeTeste.Estivador(), [Funcoes.Sinaleiro, Funcoes.OperadorDeGuindaste, Funcoes.ContramestreDeTerno], QuintaAs6h));

        Assert.Equal(
            "Antônio Ferreira (matrícula 4127) não pode se habilitar como sinaleiro, operador de guindaste de bordo nem contramestre de terno: "
            + "não tem a qualificação de sinaleiro; não tem a qualificação de operador de guindaste de bordo; não tem a qualificação de contramestre de terno.",
            erro.Message);
    }

    [Fact]
    public void Pedido_recusado_nao_mexe_na_habilitacao_que_ja_havia()
    {
        var escalacao = DaEstiva();
        var antonio = TrabalhadoresDeTeste.Estivador();
        escalacao.Habilitar(antonio, [Funcoes.Estivador], QuintaAs6h);

        Assert.Throws<ExcecaoDeDominio>(() => escalacao.Habilitar(antonio, [Funcoes.Estivador, Funcoes.OperadorDeGuindaste], QuintaAs6h.AddHours(1)));

        Assert.Equal([Funcoes.Estivador], Assert.Single(escalacao.Habilitacoes).Funcoes);
    }

    [Fact]
    public void Habilitar_de_novo_troca_as_funcoes()
    {
        var escalacao = DaEstiva();
        var antonio = Sinaleiro();
        escalacao.Habilitar(antonio, [Funcoes.Estivador], QuintaAs6h);

        escalacao.Habilitar(antonio, [Funcoes.Sinaleiro], QuintaAs6h.AddHours(2));

        var habilitacao = Assert.Single(escalacao.Habilitacoes);
        Assert.Equal([Funcoes.Sinaleiro], habilitacao.Funcoes);
        Assert.Equal(QuintaAs6h.AddHours(2), habilitacao.Em);
    }

    [Fact]
    public void Os_habilitados_ficam_na_ordem_das_matriculas_e_nao_na_da_chegada()
    {
        var escalacao = DaEstiva();

        escalacao.Habilitar(TrabalhadoresDeTeste.Estivador(5310, "João Batista", Inscricao.Cadastrado), [Funcoes.Estivador], Em(5, 6));
        escalacao.Habilitar(TrabalhadoresDeTeste.Estivador(4127), [Funcoes.Estivador], Em(5, 7));
        escalacao.Habilitar(TrabalhadoresDeTeste.Estivador(4130, "Benedito Lima"), [Funcoes.Estivador], Em(5, 5));

        Assert.Equal([4127, 4130, 5310], escalacao.Habilitacoes.Select(habilitacao => habilitacao.Matricula.Numero));
    }

    [Fact]
    public void Desiste_enquanto_a_janela_esta_aberta()
    {
        var escalacao = DaEstiva();
        var antonio = TrabalhadoresDeTeste.Estivador();
        escalacao.Habilitar(antonio, [Funcoes.Estivador], QuintaAs6h);

        escalacao.Desistir(antonio, Em(5, 10).AddTicks(-1));

        Assert.Empty(escalacao.Habilitacoes);
        Assert.Null(escalacao.HabilitacaoDe(antonio.Id));
    }

    [Fact]
    public void Depois_de_fechar_nao_desiste_mais()
    {
        var escalacao = DaEstiva();
        var antonio = TrabalhadoresDeTeste.Estivador();
        escalacao.Habilitar(antonio, [Funcoes.Estivador], QuintaAs6h);

        var erro = Assert.Throws<ExcecaoDeDominio>(() => escalacao.Desistir(antonio, Em(5, 10)));

        Assert.Equal(
            "A habilitação para a escala da estiva de 05/11/2026, das 13h às 19h, fechou em 05/11/2026, às 10h: não dá mais para desistir.",
            erro.Message);
        Assert.Single(escalacao.Habilitacoes);
    }

    [Fact]
    public void Quem_nao_se_habilitou_nao_tem_do_que_desistir()
    {
        var erro = Assert.Throws<ExcecaoDeDominio>(() => DaEstiva().Desistir(TrabalhadoresDeTeste.Estivador(), QuintaAs6h));

        Assert.Equal("Antônio Ferreira (matrícula 4127) não tem habilitação para a escala da estiva de 05/11/2026, das 13h às 19h.", erro.Message);
    }

    [Fact]
    public void Com_a_inscricao_cancelada_nao_se_habilita()
    {
        var antonio = TrabalhadoresDeTeste.Estivador();
        antonio.CancelarInscricao(new DateOnly(2026, 11, 1), "Aposentadoria");

        var erro = Assert.Throws<ExcecaoDeDominio>(() => DaEstiva().Habilitar(antonio, [Funcoes.Estivador], QuintaAs6h));

        Assert.Equal(
            "Antônio Ferreira (matrícula 4127) não pode se habilitar como estivador: a inscrição foi cancelada a partir de 01/11/2026 (Aposentadoria).",
            erro.Message);
    }

    [Fact]
    public void A_escalacao_e_de_uma_categoria_que_existe()
    {
        Assert.Equal("escala da estiva de 05/11/2026, das 13h às 19h", DaEstiva().ToString());
        Assert.NotEqual(DaEstiva().Id, DaEstiva().Id);

        var erro = Assert.Throws<ExcecaoDeDominio>(() => new Escalacao((Categoria)9, QuintaATarde, CalendarioDaEscalacao.Padrao));

        Assert.Equal("A categoria 9 não existe.", erro.Message);
    }

    /// <summary>O estivador dos exemplos, também qualificado como sinaleiro até maio de 2027.</summary>
    private static Trabalhador Sinaleiro()
    {
        var antonio = TrabalhadoresDeTeste.Estivador();
        antonio.Qualificar(Funcoes.Sinaleiro, new DateOnly(2010, 5, 10), new DateOnly(2027, 5, 9));

        return antonio;
    }
}
