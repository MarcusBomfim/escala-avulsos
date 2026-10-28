using EscalaAvulsos.Dominio.Comum;
using EscalaAvulsos.Dominio.Escalacoes;
using EscalaAvulsos.Dominio.Extratos;
using EscalaAvulsos.Dominio.Frequencias;
using EscalaAvulsos.Dominio.Identificacao;
using EscalaAvulsos.Dominio.Periodos;
using EscalaAvulsos.Dominio.Requisicoes;
using EscalaAvulsos.Dominio.Trabalhadores;

namespace EscalaAvulsos.Dominio.Testes;

/*
 * O exemplo do README, frase por frase. Se o código mudar e alguma frase de lá
 * deixar de ser verdade, este teste quebra.
 */
public sealed class ExemploDoReadmeTests
{
    private static readonly Funcao Estivador = new(Categoria.Estiva, "Estivador");

    private static readonly Funcao Sinaleiro = new(Categoria.Estiva, "Sinaleiro");

    private static readonly Funcao Contramestre = new(Categoria.Estiva, "Contramestre de terno");

    private static readonly Funcao OperadorDeGuindaste = new(Categoria.Estiva, "Operador de guindaste de bordo");

    private static readonly Funcao Conferente = new(Categoria.ConferenciaDeCarga, "Conferente");

    private static readonly DateTimeOffset QuartaAs10h = new(2026, 11, 4, 10, 0, 0, TimeSpan.FromHours(-3));

    private static readonly Periodo QuintaDeManha = Periodo.De(new DateOnly(2026, 11, 5), Turno.Manha);

    private static readonly Periodo QuintaATarde = Periodo.De(new DateOnly(2026, 11, 5), Turno.Tarde);

    private static readonly DateTimeOffset QuintaAs6h = new(2026, 11, 5, 6, 0, 0, TimeSpan.FromHours(-3));

    private static readonly DateTimeOffset QuintaAs10h = new(2026, 11, 5, 10, 0, 0, TimeSpan.FromHours(-3));

    [Fact]
    public void O_operador_pede_dois_ternos_de_estiva_para_a_manha_de_quinta()
    {
        var caisForte = new OperadorPortuario("Cais Forte Operações Portuárias", Cnpj.De("11.222.333/0001-81"));
        var atlanticoSul = new Navio("Atlântico Sul", NumeroImo.De("IMO 9876543"));

        VagasDaFuncao[] terno = [new(Contramestre, 1), new(Sinaleiro, 1), new(OperadorDeGuindaste, 1), new(Estivador, 8)];

        var requisicao = new Requisicao(
            new DadosDaRequisicao(caisForte, atlanticoSul, new LocalDaOperacao("Terminal Cais Leste", "4"), QuintaDeManha),
            [terno, terno],
            QuartaAs10h);

        Assert.Equal(
            "Cais Forte Operações Portuárias pede 2 ternos da estiva, 22 vagas, para o navio Atlântico Sul (IMO 9876543), "
            + "no Terminal Cais Leste, berço 4, em 05/11/2026, das 7h às 13h.",
            requisicao.ToString());
    }

    [Fact]
    public void O_pedido_errado_volta_com_todos_os_erros_de_uma_vez()
    {
        var dados = new DadosDaRequisicao(
            new OperadorPortuario("Cais Forte Operações Portuárias", Cnpj.De("11.222.333/0001-81")),
            new Navio("Atlântico Sul", NumeroImo.De("IMO 9876543")),
            new LocalDaOperacao("Terminal Cais Leste", "4"),
            QuintaDeManha);

        var erro = Assert.Throws<ExcecaoDeDominio>(() => new Requisicao(
            dados,
            [[new(Estivador, 8), new(new Funcao(Categoria.Estiva, "ESTIVADOR"), 2)], [new(Conferente, 1)]],
            QuartaAs10h));

        Assert.Equal(
            "O terno 1 repete estivador: junte as vagas da função numa linha só. "
            + "A requisição mistura a estiva e a conferência de carga: cada categoria tem o seu rodízio, e a sua requisição.",
            erro.Message);
    }

    [Fact]
    public void Seu_Antonio_pode_ser_sinaleiro_mas_nao_operador_de_guindaste()
    {
        var antonio = Antonio();

        Assert.Equal(
            "Antônio Ferreira (matrícula 4127) pode trabalhar como sinaleiro em 05/11/2026, das 7h às 13h.",
            antonio.AptidaoPara(Sinaleiro, QuintaDeManha).ToString());

        Assert.Equal(
            "Antônio Ferreira (matrícula 4127) não pode trabalhar como operador de guindaste de bordo em 05/11/2026, "
            + "das 7h às 13h: não tem a qualificação de operador de guindaste de bordo.",
            antonio.AptidaoPara(OperadorDeGuindaste, QuintaDeManha).ToString());
    }

    [Fact]
    public void Em_janeiro_o_sistema_diz_por_que_ele_nao_pode_e_ate_quando()
    {
        var antonio = Antonio();
        antonio.Impedir(new Impedimento(TipoDeImpedimento.Ferias, new DateOnly(2027, 1, 4), new DateOnly(2027, 2, 2)));

        var aptidao = antonio.AptidaoPara(Estivador, Periodo.De(new DateOnly(2027, 1, 5), Turno.Noite));

        Assert.Equal(
            "Antônio Ferreira (matrícula 4127) não pode trabalhar como estivador em 05/01/2027, das 19h à 1h: "
            + "o exame médico (ASO) vale só até 30/11/2026; está de férias de 04/01/2027 a 02/02/2027.",
            aptidao.ToString());

        // O dia em que cada motivo deixa de impedir: o exame, só com exame novo; as férias, em 03/02.
        Assert.Equal([null, new DateOnly(2027, 2, 2)], aptidao.Motivos.Select(motivo => motivo.Ate));
    }

    [Fact]
    public void Na_tarde_de_quinta_a_habilitacao_vai_da_1h_as_10h_e_quem_nao_pode_fica_sabendo_na_hora()
    {
        var escalacao = new Escalacao(Categoria.Estiva, QuintaATarde, CalendarioDaEscalacao.Padrao);

        escalacao.Habilitar(Antonio(), [Sinaleiro, Estivador], QuintaAs6h);

        var erro = Assert.Throws<ExcecaoDeDominio>(() => escalacao.Habilitar(Registrado(4188, "Dirceu Alves"), [Sinaleiro], QuintaAs6h));

        Assert.Equal("Dirceu Alves (matrícula 4188) não pode se habilitar como sinaleiro: não tem a qualificação de sinaleiro.", erro.Message);
        Assert.Equal((QuintaAs6h.AddHours(-5), QuintaAs10h), (escalacao.AberturaDaHabilitacao, escalacao.FechamentoDaHabilitacao));
    }

    [Fact]
    public void A_escala_da_tarde_poe_cada_um_na_sua_vez()
    {
        var escala = TardeDeQuinta().Escala;

        Assert.Equal(
            [
                "Antônio Ferreira (matrícula 4127): sinaleiro no terno 1 do navio Atlântico Sul (IMO 9876543), no Terminal Cais Leste, berço 4, em 05/11/2026, das 13h às 19h",
                "Carlos Mota (matrícula 4152): estivador no terno 1 do navio Atlântico Sul (IMO 9876543), no Terminal Cais Leste, berço 4, em 05/11/2026, das 13h às 19h",
                "Edson Prado (matrícula 4101): estivador no terno 1 do navio Atlântico Sul (IMO 9876543), no Terminal Cais Leste, berço 4, em 05/11/2026, das 13h às 19h",
            ],
            escala.Engajamentos.Select(engajamento => engajamento.ToString()));
    }

    [Fact]
    public void Quem_ficou_de_fora_sabe_por_que()
    {
        var escala = TardeDeQuinta().Escala;

        Assert.Equal(
            "Benedito Lima (matrícula 4130) ficou fora da escala: como estivador, o rodízio parou 2 posições antes da sua vez.",
            escala.SituacaoDe(Matricula.De(4130))?.ToString());
        Assert.Equal(
            "Dirceu Alves (matrícula 4188) ficou fora da escala: tem jornada em 05/11/2026, das 7h às 13h, e o descanso de 11 horas "
            + "só termina em 06/11/2026, à meia-noite.",
            escala.SituacaoDe(Matricula.De(4188))?.ToString());
        Assert.Equal(
            "João Batista (matrícula 5310) ficou fora da escala: como estivador, as vagas ficaram todas com registrados.",
            escala.SituacaoDe(Matricula.De(5310))?.ToString());
    }

    [Fact]
    public void A_proxima_escala_de_estivador_comeca_depois_do_Edson_e_o_Benedito_e_o_segundo()
    {
        var quinta = TardeDeQuinta();

        Assert.Equal(
            [4127, 4130, 4152, 4188, 4101],
            quinta.Escala.RodizioDepois.NaOrdemDaVez(Estivador, Inscricao.Registrado, quinta.Registrados).Select(matricula => matricula.Numero));
    }

    [Fact]
    public void Quem_desiste_da_a_vaga_ao_primeiro_da_espera_e_quem_nao_aparece_fica_com_falta()
    {
        var quinta = TardeDeQuinta();
        var escala = quinta.Escala;

        Assert.Equal(
            "Benedito Lima (matrícula 4130) está na escala, no lugar de quem desistiu: estivador no terno 1 do navio Atlântico Sul (IMO 9876543), "
            + "no Terminal Cais Leste, berço 4, em 05/11/2026, das 13h às 19h.",
            escala.Desistir(quinta.Edson.Id, QuintaAs10h.AddHours(1), [])?.ToString());

        escala.ConfirmarPresenca(quinta.Antonio.Id, QuintaAs10h.AddHours(2));
        escala.ConfirmarPresenca(quinta.Benedito.Id, QuintaAs10h.AddHours(2.5));
        escala.Encerrar(QuintaATarde.Fim);

        Assert.Equal(
            "Carlos Mota (matrícula 4152) está na escala: estivador no terno 1 do navio Atlântico Sul (IMO 9876543), "
            + "no Terminal Cais Leste, berço 4, em 05/11/2026, das 13h às 19h. Faltou.",
            escala.SituacaoDe(Matricula.De(4152))?.ToString());
    }

    [Fact]
    public void O_extrato_de_novembro_mostra_cada_periodo_e_ate_quando_e_pago()
    {
        var (antonio, escalas, _) = NovembroDoAntonio();
        var tabela = new TabelaDeRemuneracao([new(Estivador, Dinheiro.Reais(220m)), new(Sinaleiro, Dinheiro.Reais(260m))], adicionalNoturno: 0.20m);

        var extrato = Extrato.De(antonio, Mes.De(2026, 11), escalas, tabela);

        Assert.Equal(
            [
                "05/11/2026, das 13h às 19h — sinaleiro no navio Atlântico Sul (IMO 9876543): R$ 260,00, pago até 07/11/2026, às 19h",
                "07/11/2026, das 19h à 1h — estivador no navio Atlântico Sul (IMO 9876543): R$ 220,00 + R$ 44,00 de adicional noturno = R$ 264,00, pago até 10/11/2026, à 1h",
            ],
            extrato.Linhas.Select(linha => linha.ToString()));
        Assert.Equal(
            "Antônio Ferreira (matrícula 4127), em 11/2026: 2 períodos, R$ 524,00; férias com 1/3, R$ 58,22; 13º, R$ 43,67; FGTS depositado, R$ 50,07.",
            extrato.ToString());
    }

    [Fact]
    public void A_frequencia_avisa_enquanto_ainda_da_tempo()
    {
        var (antonio, escalas, escalacoes) = NovembroDoAntonio();

        var frequencia = FrequenciaDoMes.De(antonio, Mes.De(2026, 11), escalacoes, escalas, RegraDeFrequencia.Padrao, hoje: new DateOnly(2026, 11, 20));

        Assert.Equal(
            "Antônio Ferreira (matrícula 4127), em 11/2026: 9 habilitações, e o mínimo é 12 — faltam 3 até 30/11/2026. "
            + "Trabalhou 2 períodos; 0 faltas sem justificativa, de 2 aceitas.",
            frequencia.ToString());
    }

    /// <summary>
    /// O novembro do Antônio: a tarde de quinta, como sinaleiro; a noite de
    /// sábado, como estivador; e sete manhãs em que ele se habilitou e não foi escalado.
    /// </summary>
    private static (Trabalhador Antonio, IReadOnlyList<Escala> Escalas, IReadOnlyList<Escalacao> Escalacoes) NovembroDoAntonio()
    {
        var quinta = TardeDeQuinta();
        var antonio = quinta.Antonio;
        quinta.Escala.ConfirmarPresenca(antonio.Id, QuintaAs10h.AddHours(2));
        quinta.Escala.Encerrar(QuintaATarde.Fim);

        var sabadoANoite = new Escalacao(Categoria.Estiva, Periodo.De(new DateOnly(2026, 11, 7), Turno.Noite), CalendarioDaEscalacao.Padrao);
        sabadoANoite.Habilitar(antonio, [Estivador], sabadoANoite.AberturaDaHabilitacao);
        var requisicao = new Requisicao(
            new DadosDaRequisicao(
                new OperadorPortuario("Cais Forte Operações Portuárias", Cnpj.De("11.222.333/0001-81")),
                new Navio("Atlântico Sul", NumeroImo.De("IMO 9876543")),
                new LocalDaOperacao("Terminal Cais Leste", "4"),
                sabadoANoite.Periodo),
            [[new(Estivador, 1)]],
            sabadoANoite.PrazoDasRequisicoes);
        var noite = Escalador.Escalar(sabadoANoite, [requisicao], [antonio], [], Rodizio.Novo, Descanso.DaLei, sabadoANoite.FechamentoDaHabilitacao);
        noite.ConfirmarPresenca(antonio.Id, noite.Periodo.Inicio);
        noite.Encerrar(noite.Periodo.Fim);

        var escalacoes = new List<Escalacao> { quinta.Escalacao, sabadoANoite };

        for (var dia = 9; dia <= 15; dia++)
        {
            var manha = new Escalacao(Categoria.Estiva, Periodo.De(new DateOnly(2026, 11, dia), Turno.Manha), CalendarioDaEscalacao.Padrao);
            manha.Habilitar(antonio, [Estivador], manha.AberturaDaHabilitacao);
            escalacoes.Add(manha);
        }

        return (antonio, [quinta.Escala, noite], escalacoes);
    }

    /// <summary>
    /// A tarde de quinta: um terno com um sinaleiro e dois estivadores, seis
    /// habilitados, o rodízio de sinaleiro parado no Carlos, o de estivador no
    /// Benedito, e o Dirceu, que trabalhou de manhã.
    /// </summary>
    private static Quinta TardeDeQuinta()
    {
        var antonio = Antonio();
        var benedito = Registrado(4130, "Benedito Lima");
        var carlos = Registrado(4152, "Carlos Mota");
        carlos.Qualificar(Sinaleiro, new DateOnly(2012, 8, 1));
        var dirceu = Registrado(4188, "Dirceu Alves");
        var edson = Registrado(4101, "Edson Prado");
        var joao = new Trabalhador(new DadosDoTrabalhador(
            Matricula.De("5310"), "João Batista", Cpf.De("111.444.777-35"), Categoria.Estiva, Inscricao.Cadastrado));
        joao.Qualificar(Estivador, new DateOnly(2024, 2, 1));
        joao.RegistrarAso(new Aso(new DateOnly(2026, 2, 1), new DateOnly(2027, 1, 31)));

        var escalacao = new Escalacao(Categoria.Estiva, QuintaATarde, CalendarioDaEscalacao.Padrao);
        escalacao.Habilitar(antonio, [Sinaleiro, Estivador], QuintaAs6h);
        escalacao.Habilitar(benedito, [Estivador], QuintaAs6h.AddHours(-4));
        escalacao.Habilitar(carlos, [Sinaleiro, Estivador], QuintaAs10h.AddMinutes(-1));
        escalacao.Habilitar(dirceu, [Estivador], QuintaAs6h);
        escalacao.Habilitar(edson, [Estivador], QuintaAs6h);
        escalacao.Habilitar(joao, [Estivador], QuintaAs6h);

        var requisicao = new Requisicao(
            new DadosDaRequisicao(
                new OperadorPortuario("Cais Forte Operações Portuárias", Cnpj.De("11.222.333/0001-81")),
                new Navio("Atlântico Sul", NumeroImo.De("IMO 9876543")),
                new LocalDaOperacao("Terminal Cais Leste", "4"),
                QuintaATarde),
            [[new(Sinaleiro, 1), new(Estivador, 2)]],
            QuartaAs10h);

        var rodizio = Rodizio.Novo
            .ComAVezEm(Sinaleiro, Inscricao.Registrado, Matricula.De(4152))
            .ComAVezEm(Estivador, Inscricao.Registrado, Matricula.De(4130));

        var escala = Escalador.Escalar(
            escalacao, [requisicao], [antonio, benedito, carlos, dirceu, edson, joao], [new Jornada(dirceu.Id, QuintaDeManha)], rodizio, Descanso.DaLei, QuintaAs10h);

        return new Quinta(escalacao, escala, antonio, benedito, carlos, edson, [antonio.Matricula, benedito.Matricula, carlos.Matricula, dirceu.Matricula, edson.Matricula]);
    }

    /// <summary>A escalação e a escala da tarde de quinta, e quem aparece no exemplo depois dela.</summary>
    private sealed record Quinta(
        Escalacao Escalacao,
        Escala Escala,
        Trabalhador Antonio,
        Trabalhador Benedito,
        Trabalhador Carlos,
        Trabalhador Edson,
        IReadOnlyList<Matricula> Registrados);

    private static Trabalhador Registrado(int matricula, string nome)
    {
        var trabalhador = new Trabalhador(new DadosDoTrabalhador(
            Matricula.De(matricula), nome, Cpf.De("529.982.247-25"), Categoria.Estiva, Inscricao.Registrado));

        trabalhador.Qualificar(Estivador, new DateOnly(2001, 6, 1));
        trabalhador.RegistrarAso(new Aso(new DateOnly(2026, 3, 1), new DateOnly(2027, 2, 28)));

        return trabalhador;
    }

    private static Trabalhador Antonio()
    {
        var antonio = new Trabalhador(new DadosDoTrabalhador(
            Matricula.De("4127"), "Antônio Ferreira", Cpf.De("529.982.247-25"), Categoria.Estiva, Inscricao.Registrado));

        antonio.Qualificar(Estivador, new DateOnly(1994, 3, 1));
        antonio.Qualificar(Sinaleiro, new DateOnly(2010, 5, 10), validaAte: new DateOnly(2027, 5, 9));
        antonio.RegistrarAso(new Aso(new DateOnly(2025, 12, 1), validoAte: new DateOnly(2026, 11, 30)));

        return antonio;
    }
}
