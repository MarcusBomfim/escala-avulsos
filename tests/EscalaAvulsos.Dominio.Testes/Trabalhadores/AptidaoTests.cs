using EscalaAvulsos.Dominio.Identificacao;
using EscalaAvulsos.Dominio.Periodos;
using EscalaAvulsos.Dominio.Trabalhadores;

namespace EscalaAvulsos.Dominio.Testes.Trabalhadores;

public sealed class AptidaoTests
{
    private static readonly Periodo QuintaDeManha = Periodo.De(new DateOnly(2026, 11, 5), Turno.Manha);

    [Fact]
    public void O_estivador_qualificado_e_com_o_exame_em_dia_pode_trabalhar()
    {
        var antonio = TrabalhadoresDeTeste.Estivador();

        var aptidao = antonio.AptidaoPara(Funcoes.Estivador, QuintaDeManha);

        Assert.True(aptidao.Apto);
        Assert.Empty(aptidao.Motivos);
        Assert.Equal((antonio.Id, antonio.Matricula, Funcoes.Estivador, QuintaDeManha), (aptidao.Trabalhador, aptidao.Matricula, aptidao.Funcao, aptidao.Periodo));
        Assert.Equal("Antônio Ferreira (matrícula 4127) pode trabalhar como estivador em 05/11/2026, das 7h às 13h.", aptidao.ToString());
    }

    [Fact]
    public void Estar_no_cadastro_nao_impede_ninguem()
    {
        var joao = TrabalhadoresDeTeste.Estivador(5310, "João Batista", Inscricao.Cadastrado);

        Assert.True(joao.AptidaoPara(Funcoes.Estivador, QuintaDeManha).Apto);
    }

    [Fact]
    public void Sem_a_qualificacao_nao_trabalha_na_funcao()
    {
        var aptidao = TrabalhadoresDeTeste.Estivador().AptidaoPara(Funcoes.OperadorDeGuindaste, QuintaDeManha);

        var motivo = Assert.Single(aptidao.Motivos);
        Assert.Equal((TipoDeMotivo.SemQualificacao, (DateOnly?)null, (DateOnly?)null), (motivo.Tipo, motivo.De, motivo.Ate));
        Assert.Equal(
            "Antônio Ferreira (matrícula 4127) não pode trabalhar como operador de guindaste de bordo em 05/11/2026, das 7h às 13h: não tem a qualificação de operador de guindaste de bordo.",
            aptidao.ToString());
    }

    [Fact]
    public void A_funcao_de_outra_categoria_e_dita_como_tal()
    {
        var motivo = Assert.Single(TrabalhadoresDeTeste.Estivador().AptidaoPara(Funcoes.Conferente, QuintaDeManha).Motivos);

        Assert.Equal((TipoDeMotivo.FuncaoDeOutraCategoria, "conferente é função da conferência de carga, e não da estiva"), (motivo.Tipo, motivo.Descricao));
    }

    [Fact]
    public void A_qualificacao_que_ainda_nao_comecou_diz_quando_comeca()
    {
        var antonio = TrabalhadoresDeTeste.Estivador();
        antonio.Qualificar(Funcoes.Sinaleiro, new DateOnly(2026, 11, 10));

        var motivo = Assert.Single(antonio.AptidaoPara(Funcoes.Sinaleiro, QuintaDeManha).Motivos);

        Assert.Equal((TipoDeMotivo.SemQualificacao, (DateOnly?)null, (DateOnly?)new DateOnly(2026, 11, 9)), (motivo.Tipo, motivo.De, motivo.Ate));
        Assert.Equal("a qualificação de sinaleiro só vale a partir de 10/11/2026", motivo.Descricao);

        // A noite da véspera ainda não: ela é do dia 9, mesmo terminando à 1h do dia 10.
        Assert.False(antonio.AptidaoPara(Funcoes.Sinaleiro, Periodo.De(new DateOnly(2026, 11, 9), Turno.Noite)).Apto);
        Assert.True(antonio.AptidaoPara(Funcoes.Sinaleiro, Periodo.De(new DateOnly(2026, 11, 10), Turno.Madrugada)).Apto);
    }

    [Fact]
    public void A_qualificacao_vale_ate_o_ultimo_dia_inclusive()
    {
        var antonio = TrabalhadoresDeTeste.Estivador();
        antonio.Qualificar(Funcoes.OperadorDeGuindaste, new DateOnly(2021, 7, 1), new DateOnly(2026, 6, 30));

        Assert.True(antonio.AptidaoPara(Funcoes.OperadorDeGuindaste, Periodo.De(new DateOnly(2026, 6, 30), Turno.Noite)).Apto);

        var motivo = Assert.Single(antonio.AptidaoPara(Funcoes.OperadorDeGuindaste, Periodo.De(new DateOnly(2026, 7, 1), Turno.Madrugada)).Motivos);

        Assert.Equal((TipoDeMotivo.QualificacaoVencida, (DateOnly?)new DateOnly(2026, 7, 1), (DateOnly?)null), (motivo.Tipo, motivo.De, motivo.Ate));
        Assert.Equal("a qualificação de operador de guindaste de bordo vale só até 30/06/2026", motivo.Descricao);
    }

    [Fact]
    public void O_exame_vale_ate_o_ultimo_dia_e_a_noite_desse_dia()
    {
        var antonio = TrabalhadoresDeTeste.Estivador();

        // A noite de 30/11 termina à 1h de 01/12, mas é do dia 30 (ADR-0002).
        Assert.True(antonio.AptidaoPara(Funcoes.Estivador, Periodo.De(new DateOnly(2026, 11, 30), Turno.Noite)).Apto);

        var motivo = Assert.Single(antonio.AptidaoPara(Funcoes.Estivador, Periodo.De(new DateOnly(2026, 12, 1), Turno.Madrugada)).Motivos);

        Assert.Equal((TipoDeMotivo.AsoVencido, (DateOnly?)new DateOnly(2026, 12, 1), (DateOnly?)null), (motivo.Tipo, motivo.De, motivo.Ate));
        Assert.Equal("o exame médico (ASO) vale só até 30/11/2026", motivo.Descricao);
    }

    [Fact]
    public void Sem_exame_nao_ha_escala()
    {
        var novato = new Trabalhador(new DadosDoTrabalhador(
            Matricula.De(6001), "Rafael Souza", Cpf.De("111.444.777-35"), Categoria.Estiva, Inscricao.Cadastrado));
        novato.Qualificar(Funcoes.Estivador, new DateOnly(2026, 10, 1));

        var motivo = Assert.Single(novato.AptidaoPara(Funcoes.Estivador, QuintaDeManha).Motivos);

        Assert.Equal((TipoDeMotivo.SemAso, "não tem exame médico (ASO) registrado"), (motivo.Tipo, motivo.Descricao));
    }

    [Fact]
    public void As_ferias_impedem_do_primeiro_ao_ultimo_dia()
    {
        var antonio = TrabalhadoresDeTeste.Estivador();
        antonio.RegistrarAso(new Aso(new DateOnly(2026, 11, 20), new DateOnly(2027, 11, 19)));
        antonio.Impedir(new Impedimento(TipoDeImpedimento.Ferias, new DateOnly(2027, 1, 4), new DateOnly(2027, 2, 2)));

        Assert.True(antonio.AptidaoPara(Funcoes.Estivador, Periodo.De(new DateOnly(2027, 1, 3), Turno.Noite)).Apto);
        Assert.False(antonio.AptidaoPara(Funcoes.Estivador, Periodo.De(new DateOnly(2027, 1, 4), Turno.Madrugada)).Apto);
        Assert.True(antonio.AptidaoPara(Funcoes.Estivador, Periodo.De(new DateOnly(2027, 2, 3), Turno.Madrugada)).Apto);

        var motivo = Assert.Single(antonio.AptidaoPara(Funcoes.Estivador, Periodo.De(new DateOnly(2027, 2, 2), Turno.Noite)).Motivos);

        Assert.Equal((TipoDeMotivo.Ferias, new DateOnly(2027, 1, 4), new DateOnly(2027, 2, 2)), (motivo.Tipo, motivo.De!.Value, motivo.Ate!.Value));
        Assert.Equal("está de férias de 04/01/2027 a 02/02/2027", motivo.Descricao);
    }

    [Fact]
    public void O_afastamento_impede_ate_a_volta()
    {
        var antonio = TrabalhadoresDeTeste.Estivador();
        antonio.Impedir(new Impedimento(TipoDeImpedimento.Afastamento, new DateOnly(2026, 3, 10)));
        var vinteDeAbril = Periodo.De(new DateOnly(2026, 4, 20), Turno.Tarde);

        var aberto = Assert.Single(antonio.AptidaoPara(Funcoes.Estivador, vinteDeAbril).Motivos);

        Assert.Equal((TipoDeMotivo.Afastamento, "está em afastamento desde 10/03/2026", (DateOnly?)null), (aberto.Tipo, aberto.Descricao, aberto.Ate));

        antonio.EncerrarAfastamento(new DateOnly(2026, 4, 21));

        Assert.Equal("está em afastamento de 10/03/2026 a 20/04/2026", Assert.Single(antonio.AptidaoPara(Funcoes.Estivador, vinteDeAbril).Motivos).Descricao);
        Assert.True(antonio.AptidaoPara(Funcoes.Estivador, Periodo.De(new DateOnly(2026, 4, 21), Turno.Madrugada)).Apto);
    }

    [Fact]
    public void A_suspensao_impede_os_dias_dela()
    {
        var antonio = TrabalhadoresDeTeste.Estivador();
        antonio.Impedir(new Impedimento(TipoDeImpedimento.Suspensao, new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 15)));

        var motivo = Assert.Single(antonio.AptidaoPara(Funcoes.Estivador, QuintaDeManha).Motivos);

        Assert.Equal((TipoDeMotivo.Suspensao, "cumpre suspensão de 01/11/2026 a 15/11/2026"), (motivo.Tipo, motivo.Descricao));
    }

    [Fact]
    public void Diz_todos_os_motivos_da_qualificacao_ao_impedimento_mais_recente()
    {
        var antonio = TrabalhadoresDeTeste.Estivador();
        antonio.Qualificar(Funcoes.Sinaleiro, new DateOnly(2010, 5, 10), new DateOnly(2026, 12, 31));
        antonio.Impedir(new Impedimento(TipoDeImpedimento.Ferias, new DateOnly(2027, 1, 4), new DateOnly(2027, 2, 2)));
        antonio.Impedir(new Impedimento(TipoDeImpedimento.Suspensao, new DateOnly(2027, 1, 2), new DateOnly(2027, 1, 10)));

        var aptidao = antonio.AptidaoPara(Funcoes.Sinaleiro, Periodo.De(new DateOnly(2027, 1, 5), Turno.Tarde));

        Assert.Equal(
            [TipoDeMotivo.QualificacaoVencida, TipoDeMotivo.AsoVencido, TipoDeMotivo.Suspensao, TipoDeMotivo.Ferias],
            aptidao.Motivos.Select(motivo => motivo.Tipo));
        Assert.Equal(
            "Antônio Ferreira (matrícula 4127) não pode trabalhar como sinaleiro em 05/01/2027, das 13h às 19h: "
            + "a qualificação de sinaleiro vale só até 31/12/2026; o exame médico (ASO) vale só até 30/11/2026; "
            + "cumpre suspensão de 02/01/2027 a 10/01/2027; está de férias de 04/01/2027 a 02/02/2027.",
            aptidao.ToString());
    }

    [Fact]
    public void Com_a_inscricao_cancelada_o_cancelamento_e_o_unico_motivo()
    {
        var antonio = TrabalhadoresDeTeste.Estivador();
        antonio.Impedir(new Impedimento(TipoDeImpedimento.Afastamento, new DateOnly(2026, 10, 1)));
        antonio.CancelarInscricao(new DateOnly(2026, 11, 5), "Aposentadoria");

        Assert.Equal(TipoDeMotivo.Afastamento, Assert.Single(antonio.AptidaoPara(Funcoes.Estivador, Periodo.De(new DateOnly(2026, 11, 4), Turno.Noite)).Motivos).Tipo);

        var motivo = Assert.Single(antonio.AptidaoPara(Funcoes.OperadorDeGuindaste, QuintaDeManha).Motivos);

        Assert.Equal((TipoDeMotivo.InscricaoCancelada, new DateOnly(2026, 11, 5)), (motivo.Tipo, motivo.De!.Value));
        Assert.Equal("a inscrição foi cancelada a partir de 05/11/2026 (Aposentadoria)", motivo.ToString());
    }
}
