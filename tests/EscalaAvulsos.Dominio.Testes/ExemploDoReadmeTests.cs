using EscalaAvulsos.Dominio.Comum;
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
