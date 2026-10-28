using EscalaAvulsos.Dominio.Comum;
using EscalaAvulsos.Dominio.Escalacoes;
using EscalaAvulsos.Dominio.Identificacao;
using EscalaAvulsos.Dominio.Periodos;
using EscalaAvulsos.Dominio.Requisicoes;
using EscalaAvulsos.Dominio.Testes.Trabalhadores;
using EscalaAvulsos.Dominio.Trabalhadores;
using static EscalaAvulsos.Dominio.Testes.Escalacoes.EscalacoesDeTeste;
using static EscalaAvulsos.Dominio.Testes.Requisicoes.RequisicoesDeTeste;

namespace EscalaAvulsos.Dominio.Testes.Escalacoes;

public sealed class EscaladorTests
{
    private static readonly Funcao Sinaleiro = Funcoes.Sinaleiro;

    private static readonly Funcao Estivador = Funcoes.Estivador;

    private static readonly DadosDaRequisicao DaTarde = new(CaisForte, AtlanticoSul, CaisLeste, QuintaATarde);

    [Fact]
    public void Poe_cada_um_na_sua_vez_e_diz_a_quem_ficou_de_fora_por_que()
    {
        var tarde = new TardeDeQuinta();

        var escala = tarde.Escalar();

        Assert.Equal(
            [
                "Antônio Ferreira (matrícula 4127): sinaleiro no terno 1 do navio Atlântico Sul (IMO 9876543), no Terminal Cais Leste, berço 4, em 05/11/2026, das 13h às 19h",
                "Carlos Mota (matrícula 4152): estivador no terno 1 do navio Atlântico Sul (IMO 9876543), no Terminal Cais Leste, berço 4, em 05/11/2026, das 13h às 19h",
                "Edson Prado (matrícula 4101): estivador no terno 1 do navio Atlântico Sul (IMO 9876543), no Terminal Cais Leste, berço 4, em 05/11/2026, das 13h às 19h",
            ],
            escala.Engajamentos.Select(engajamento => engajamento.ToString()));

        Assert.Equal(
            [
                "Edson Prado (matrícula 4101) está na escala: estivador no terno 1 do navio Atlântico Sul (IMO 9876543), no Terminal Cais Leste, berço 4, em 05/11/2026, das 13h às 19h.",
                "Antônio Ferreira (matrícula 4127) está na escala: sinaleiro no terno 1 do navio Atlântico Sul (IMO 9876543), no Terminal Cais Leste, berço 4, em 05/11/2026, das 13h às 19h.",
                "Benedito Lima (matrícula 4130) ficou fora da escala: como estivador, o rodízio parou 2 posições antes da sua vez.",
                "Carlos Mota (matrícula 4152) está na escala: estivador no terno 1 do navio Atlântico Sul (IMO 9876543), no Terminal Cais Leste, berço 4, em 05/11/2026, das 13h às 19h.",
                "Dirceu Alves (matrícula 4188) ficou fora da escala: tem jornada em 05/11/2026, das 7h às 13h, e o descanso de 11 horas só termina em 06/11/2026, à meia-noite.",
                "João Batista (matrícula 5310) ficou fora da escala: como estivador, as vagas ficaram todas com registrados.",
            ],
            escala.Situacoes.Select(situacao => situacao.ToString()));

        Assert.Empty(escala.VagasNaoPreenchidas);
        Assert.Equal("escala da estiva de 05/11/2026, das 13h às 19h: 3 de 3 vagas preenchidas", escala.ToString());
    }

    [Fact]
    public void Cada_motivo_traz_o_que_o_explica()
    {
        var tarde = new TardeDeQuinta();

        var escala = tarde.Escalar();

        var benedito = Assert.Single(escala.SituacaoDe(Matricula.De(4130))!.Motivos);
        Assert.Equal((TipoDeMotivoNaEscala.NaoAlcancado, Estivador, 2), (benedito.Tipo, benedito.Funcao, benedito.PosicoesAntesDaVez!.Value));

        var dirceu = Assert.Single(escala.SituacaoDe(Matricula.De(4188))!.Motivos);
        Assert.Equal((TipoDeMotivoNaEscala.EmDescanso, (Funcao?)null), (dirceu.Tipo, dirceu.Funcao));
        Assert.Equal(new DateTimeOffset(2026, 11, 6, 0, 0, 0, TimeSpan.FromHours(-3)), dirceu.Descanso?.DescansoTerminaEm);

        // O Carlos não alcançou a vaga de sinaleiro, e pegou uma de estivador.
        var carlos = escala.SituacaoDe(Matricula.De(4152))!;
        Assert.True(carlos.Escalado);
        Assert.Equal(["como sinaleiro, o rodízio parou 1 posição antes da sua vez"], carlos.Motivos.Select(motivo => motivo.Descricao));

        Assert.Equal(TipoDeMotivoNaEscala.VagasComRegistrados, Assert.Single(escala.SituacaoDe(Matricula.De(5310))!.Motivos).Tipo);
        Assert.Null(escala.SituacaoDe(Matricula.De(9999)));
    }

    [Fact]
    public void O_rodizio_para_no_ultimo_escalado_de_cada_roda()
    {
        var tarde = new TardeDeQuinta();

        var escala = tarde.Escalar();

        Assert.Equal(
            [
                new VezNoRodizio(Estivador, Inscricao.Registrado, Matricula.De(4101)),
                new VezNoRodizio(Sinaleiro, Inscricao.Registrado, Matricula.De(4127)),
            ],
            escala.RodizioDepois.Vezes);

        // Na próxima escala de estivador, o Benedito, que não foi alcançado, é o segundo.
        Assert.Equal(
            [4127, 4130, 4152, 4188, 4101],
            escala.RodizioDepois.NaOrdemDaVez(Estivador, Inscricao.Registrado, tarde.Escalacao.Habilitacoes.Select(habilitacao => habilitacao.Matricula).Where(matricula => matricula.Numero < 5000)).Select(matricula => matricula.Numero));
    }

    [Fact]
    public void A_mesma_entrada_da_a_mesma_escala_e_nada_do_que_entrou_muda()
    {
        var tarde = new TardeDeQuinta();
        var vezesAntes = tarde.Rodizio.Vezes;

        var primeira = tarde.Escalar();
        var segunda = tarde.Escalar();

        Assert.Equal(primeira.Engajamentos, segunda.Engajamentos);
        Assert.Equal(primeira.Situacoes.Select(situacao => situacao.ToString()), segunda.Situacoes.Select(situacao => situacao.ToString()));
        Assert.Equal(vezesAntes, tarde.Rodizio.Vezes);
        Assert.Equal(6, tarde.Escalacao.Habilitacoes.Count);
        Assert.NotEqual(primeira.Id, segunda.Id);
        Assert.Equal((tarde.Escalacao.Id, Categoria.Estiva, QuintaATarde, Em(5, 10)), (primeira.Escalacao, primeira.Categoria, primeira.Periodo, primeira.FeitaEm));
    }

    [Fact]
    public void So_escala_depois_de_a_habilitacao_fechar_e_antes_de_o_periodo_comecar()
    {
        var tarde = new TardeDeQuinta();

        var cedo = Assert.Throws<ExcecaoDeDominio>(() => tarde.Escalar(Em(5, 10).AddTicks(-1)));
        var tarde13h = Assert.Throws<ExcecaoDeDominio>(() => tarde.Escalar(Em(5, 13)));

        Assert.Equal("A habilitação para a escala da estiva de 05/11/2026, das 13h às 19h, só fecha em 05/11/2026, às 10h: a escala é feita depois.", cedo.Message);
        Assert.Equal("O período de 05/11/2026, das 13h às 19h, já começou: a escala é feita antes dele.", tarde13h.Message);
        Assert.Equal(3, tarde.Escalar(Em(5, 13).AddTicks(-1)).Engajamentos.Count);
    }

    [Fact]
    public void A_requisicao_de_outra_escala_e_recusada()
    {
        var tarde = new TardeDeQuinta();
        var daManha = DoisTernos();

        var erro = Assert.Throws<ExcecaoDeDominio>(() => Escalador.Escalar(
            tarde.Escalacao, [tarde.Requisicao, daManha], tarde.Trabalhadores, [], Rodizio.Novo, Descanso.DaLei, Em(5, 10)));

        Assert.Equal(
            "A requisição do navio Atlântico Sul (IMO 9876543) é da estiva, para 05/11/2026, das 7h às 13h, e não entra na escala da estiva de 05/11/2026, das 13h às 19h.",
            erro.Message);
    }

    [Fact]
    public void A_requisicao_cancelada_nao_entra_e_a_que_chegou_depois_do_prazo_fica_de_fora_listada()
    {
        var tarde = new TardeDeQuinta();
        var cancelada = new Requisicao(DaTarde, [[new(Estivador, 3)]], QuartaAs10h);
        cancelada.Cancelar("Navio atrasou", QuartaAs10h.AddHours(1));
        var atrasada = new Requisicao(DaTarde, [[new(Estivador, 5)]], Em(5, 9).AddMinutes(1));
        var noLimite = new Requisicao(DaTarde, [[new(Estivador, 1)]], Em(5, 9));

        var escala = Escalador.Escalar(
            tarde.Escalacao, [atrasada, cancelada, tarde.Requisicao, noLimite], tarde.Trabalhadores, tarde.Jornadas, tarde.Rodizio, Descanso.DaLei, Em(5, 10));

        Assert.Equal([atrasada.Id], escala.RequisicoesForaDoPrazo);
        Assert.Equal(4, escala.VagasPedidas);
        Assert.Equal(noLimite.Id, escala.Engajamentos[^1].Requisicao);
    }

    [Fact]
    public void Falta_um_trabalhador_que_se_habilitou()
    {
        var tarde = new TardeDeQuinta();

        var erro = Assert.Throws<ExcecaoDeDominio>(() => Escalador.Escalar(
            tarde.Escalacao, [tarde.Requisicao], tarde.Trabalhadores[..^1], tarde.Jornadas, tarde.Rodizio, Descanso.DaLei, Em(5, 10)));

        Assert.Equal("Falta o trabalhador da matrícula 5310, que se habilitou para a escala da estiva de 05/11/2026, das 13h às 19h.", erro.Message);
    }

    [Fact]
    public void Quem_deixou_de_poder_trabalhar_depois_de_se_habilitar_perde_a_vez()
    {
        var tarde = new TardeDeQuinta();
        tarde.Carlos.Impedir(new Impedimento(TipoDeImpedimento.Suspensao, new DateOnly(2026, 11, 5), new DateOnly(2026, 11, 6)));

        var escala = tarde.Escalar();

        Assert.Equal(
            "Carlos Mota (matrícula 4152) ficou fora da escala: como sinaleiro, cumpre suspensão de 05/11/2026 a 06/11/2026; como estivador, cumpre suspensão de 05/11/2026 a 06/11/2026.",
            escala.SituacaoDe(Matricula.De(4152))!.ToString());
        Assert.Equal(TipoDeMotivo.Suspensao, escala.SituacaoDe(Matricula.De(4152))!.Motivos[0].MotivosDaAptidao.Single().Tipo);

        // Sem o Carlos, a roda de estivador alcança o Benedito.
        Assert.Equal(["Antônio Ferreira", "Edson Prado", "Benedito Lima"], escala.Engajamentos.Select(engajamento => engajamento.Nome));
    }

    [Fact]
    public void Os_cadastrados_completam_as_vagas_na_roda_deles()
    {
        var tarde = new TardeDeQuinta();
        var maria = TrabalhadoresDeTeste.Estivador(5402, "Maria Santos", Inscricao.Cadastrado);
        var paulo = TrabalhadoresDeTeste.Estivador(5288, "Paulo Reis", Inscricao.Cadastrado);
        tarde.Escalacao.Habilitar(maria, [Estivador], Em(5, 8));
        tarde.Escalacao.Habilitar(paulo, [Estivador], Em(5, 8));
        var quatro = new Requisicao(DaTarde, [[new(Sinaleiro, 1), new(Estivador, 4)]], QuartaAs10h);
        var rodizio = tarde.Rodizio.ComAVezEm(Estivador, Inscricao.Cadastrado, Matricula.De(5310));

        var escala = Escalador.Escalar(
            tarde.Escalacao, [quatro], [.. tarde.Trabalhadores, maria, paulo], tarde.Jornadas, rodizio, Descanso.DaLei, Em(5, 10));

        // O Antônio é o sinaleiro, e o Dirceu descansa: três registrados pegam três vagas de estivador, e a
        // quarta é do primeiro cadastrado depois da matrícula 5310 — a roda dos cadastrados dá a volta dela.
        Assert.Equal(
            ["Antônio Ferreira", "Carlos Mota", "Edson Prado", "Benedito Lima", "Maria Santos"],
            escala.Engajamentos.Select(engajamento => engajamento.Nome));
        Assert.Equal(5402, escala.RodizioDepois.UltimoEscalado(Estivador, Inscricao.Cadastrado)?.Numero);
        Assert.Equal(
            ["como estivador, o rodízio parou 1 posição antes da sua vez", "como estivador, o rodízio parou 2 posições antes da sua vez"],
            escala.Situacoes.Where(situacao => situacao.Matricula.Numero is 5288 or 5310).Select(situacao => situacao.Motivos.Single().Descricao));
    }

    [Fact]
    public void Sem_gente_bastante_as_vagas_ficam_vazias_e_o_operador_fica_sabendo()
    {
        var escalacao = DaEstiva();
        var antonio = TrabalhadoresDeTeste.Estivador();
        escalacao.Habilitar(antonio, [Estivador], QuintaAs6h);
        var requisicao = new Requisicao(DaTarde, [[new(Funcoes.ContramestreDeTerno, 1), new(Estivador, 3)], [new(Estivador, 2)]], QuartaAs10h);

        var escala = Escalador.Escalar(escalacao, [requisicao], [antonio], [], Rodizio.Novo, Descanso.DaLei, Em(5, 10));

        Assert.Equal(
            [
                "contramestre de terno: 1, no terno 1 do navio Atlântico Sul (IMO 9876543)",
                "estivador: 2, no terno 1 do navio Atlântico Sul (IMO 9876543)",
                "estivador: 2, no terno 2 do navio Atlântico Sul (IMO 9876543)",
            ],
            escala.VagasNaoPreenchidas.Select(vaga => vaga.ToString()));
        Assert.Equal((requisicao.Id, 1), (escala.VagasNaoPreenchidas[0].Requisicao, escala.VagasNaoPreenchidas[0].Terno));
        Assert.Equal("escala da estiva de 05/11/2026, das 13h às 19h: 1 de 6 vagas preenchidas", escala.ToString());
    }

    [Fact]
    public void A_funcao_com_menos_habilitados_e_escalada_primeiro()
    {
        var escalacao = DaEstiva();
        var antonio = TrabalhadoresDeTeste.Estivador();
        antonio.Qualificar(Sinaleiro, new DateOnly(2010, 5, 10));
        var benedito = TrabalhadoresDeTeste.Estivador(4130, "Benedito Lima");
        escalacao.Habilitar(antonio, [Estivador, Sinaleiro], QuintaAs6h);
        escalacao.Habilitar(benedito, [Estivador], QuintaAs6h);

        // O estivador vem primeiro na requisição, mas o sinaleiro tem menos habilitados.
        var requisicao = new Requisicao(DaTarde, [[new(Estivador, 1), new(Sinaleiro, 1)]], QuartaAs10h);

        var escala = Escalador.Escalar(escalacao, [requisicao], [antonio, benedito], [], Rodizio.Novo, Descanso.DaLei, Em(5, 10));

        Assert.Equal(
            ["Benedito Lima (matrícula 4130): estivador", "Antônio Ferreira (matrícula 4127): sinaleiro"],
            escala.Engajamentos.Select(engajamento => $"{engajamento.Nome} (matrícula {engajamento.Matricula}): {engajamento.Funcao.NaFrase}"));
    }

    [Fact]
    public void A_funcao_que_nenhuma_requisicao_pediu_e_dita()
    {
        var escalacao = DaEstiva();
        var antonio = TrabalhadoresDeTeste.Estivador();
        antonio.Qualificar(Sinaleiro, new DateOnly(2010, 5, 10));
        escalacao.Habilitar(antonio, [Sinaleiro], QuintaAs6h);

        var escala = Escalador.Escalar(escalacao, [new Requisicao(DaTarde, [[new(Estivador, 1)]], QuartaAs10h)], [antonio], [], Rodizio.Novo, Descanso.DaLei, Em(5, 10));

        Assert.Equal(
            "Antônio Ferreira (matrícula 4127) ficou fora da escala: como sinaleiro, não houve vaga neste período.",
            Assert.Single(escala.Situacoes).ToString());
        Assert.Equal(TipoDeMotivoNaEscala.SemVagaNaFuncao, escala.Situacoes[0].Motivos[0].Tipo);
        Assert.Equal("escala da estiva de 05/11/2026, das 13h às 19h: 0 de 1 vaga preenchida", escala.ToString());
    }

    [Fact]
    public void Os_motivos_da_aptidao_na_mesma_funcao_vem_juntos_numa_frase()
    {
        var escalacao = DaEstiva();
        var antonio = TrabalhadoresDeTeste.Estivador();
        escalacao.Habilitar(antonio, [Estivador], QuintaAs6h);
        antonio.Impedir(new Impedimento(TipoDeImpedimento.Suspensao, new DateOnly(2026, 11, 5), new DateOnly(2026, 11, 6)));
        antonio.Impedir(new Impedimento(TipoDeImpedimento.Afastamento, new DateOnly(2026, 11, 5)));

        var escala = Escalador.Escalar(escalacao, [new Requisicao(DaTarde, [[new(Estivador, 1)]], QuartaAs10h)], [antonio], [], Rodizio.Novo, Descanso.DaLei, Em(5, 10));

        var motivo = Assert.Single(Assert.Single(escala.Situacoes).Motivos);
        Assert.Equal(2, motivo.MotivosDaAptidao.Count);
        Assert.Equal("como estivador, cumpre suspensão de 05/11/2026 a 06/11/2026 e está em afastamento desde 05/11/2026", motivo.Descricao);
    }

    [Fact]
    public void As_vagas_seguem_a_ordem_em_que_as_requisicoes_chegaram()
    {
        var escalacao = DaEstiva();
        var antonio = TrabalhadoresDeTeste.Estivador();
        var benedito = TrabalhadoresDeTeste.Estivador(4130, "Benedito Lima");
        escalacao.Habilitar(antonio, [Estivador], QuintaAs6h);
        escalacao.Habilitar(benedito, [Estivador], QuintaAs6h);
        var mareAlta = new Navio("Maré Alta", NumeroImo.De("IMO 9074729"));
        var depois = new Requisicao(new DadosDaRequisicao(CaisForte, mareAlta, new LocalDaOperacao("Terminal Cais Oeste"), QuintaATarde), [[new(Estivador, 1)]], QuartaAs10h.AddHours(1));
        var antes = new Requisicao(DaTarde, [[new(Estivador, 1)]], QuartaAs10h);

        var escala = Escalador.Escalar(escalacao, [depois, antes], [antonio, benedito], [], Rodizio.Novo, Descanso.DaLei, Em(5, 10));

        Assert.Equal(
            ["Antônio Ferreira no navio Atlântico Sul", "Benedito Lima no navio Maré Alta"],
            escala.Engajamentos.Select(engajamento => $"{engajamento.Nome} no navio {engajamento.Navio.Nome}"));
        Assert.Equal(("Terminal Cais Oeste", CaisForte), (escala.Engajamentos[1].Local.Terminal, escala.Engajamentos[1].Operador));
    }

    [Fact]
    public void Sem_ninguem_habilitado_todas_as_vagas_ficam_vazias()
    {
        var escala = Escalador.Escalar(DaEstiva(), [new Requisicao(DaTarde, [[new(Estivador, 4)]], QuartaAs10h)], [], [], Rodizio.Novo, Descanso.DaLei, Em(5, 10));

        Assert.Empty(escala.Engajamentos);
        Assert.Empty(escala.Situacoes);
        Assert.Equal(4, Assert.Single(escala.VagasNaoPreenchidas).Quantidade);
        Assert.Empty(escala.RodizioDepois.Vezes);
    }

    [Fact]
    public void A_jornada_marcada_depois_tambem_impede()
    {
        var escalacao = DaEstiva();
        var antonio = TrabalhadoresDeTeste.Estivador();
        escalacao.Habilitar(antonio, [Estivador], QuintaAs6h);

        var escala = Escalador.Escalar(
            escalacao,
            [new Requisicao(DaTarde, [[new(Estivador, 1)]], QuartaAs10h)],
            [antonio],
            [new Jornada(antonio.Id, Periodo.De(new DateOnly(2026, 11, 5), Turno.Noite)), new Jornada(TrabalhadorId.Novo(), QuintaATarde)],
            Rodizio.Novo,
            Descanso.DaLei,
            Em(5, 10));

        Assert.Equal(
            "Antônio Ferreira (matrícula 4127) ficou fora da escala: tem jornada em 05/11/2026, das 19h à 1h, e não teria 11 horas de descanso antes dela.",
            Assert.Single(escala.Situacoes).ToString());
        Assert.Empty(escala.Engajamentos);
    }

    /// <summary>
    /// A tarde de quinta do README: um terno com um sinaleiro e dois
    /// estivadores, seis habilitados — o Dirceu trabalhou de manhã —, e o
    /// rodízio parado no Carlos (sinaleiro) e no Benedito (estivador).
    /// </summary>
    private sealed class TardeDeQuinta
    {
        public TardeDeQuinta()
        {
            Antonio = TrabalhadoresDeTeste.Estivador(4127, "Antônio Ferreira");
            Antonio.Qualificar(Sinaleiro, new DateOnly(2010, 5, 10));
            Carlos = TrabalhadoresDeTeste.Estivador(4152, "Carlos Mota");
            Carlos.Qualificar(Sinaleiro, new DateOnly(2012, 8, 1));
            var benedito = TrabalhadoresDeTeste.Estivador(4130, "Benedito Lima");
            var dirceu = TrabalhadoresDeTeste.Estivador(4188, "Dirceu Alves");
            var edson = TrabalhadoresDeTeste.Estivador(4101, "Edson Prado");
            var joao = TrabalhadoresDeTeste.Estivador(5310, "João Batista", Inscricao.Cadastrado);

            Escalacao.Habilitar(Antonio, [Sinaleiro, Estivador], Em(5, 6));
            Escalacao.Habilitar(benedito, [Estivador], Em(5, 2));
            Escalacao.Habilitar(Carlos, [Sinaleiro, Estivador], Em(5, 9, 59));
            Escalacao.Habilitar(dirceu, [Estivador], Em(5, 1));
            Escalacao.Habilitar(edson, [Estivador], Em(5, 7));
            Escalacao.Habilitar(joao, [Estivador], Em(5, 3));

            Trabalhadores = [Antonio, benedito, Carlos, dirceu, edson, joao];
            Jornadas = [new Jornada(dirceu.Id, Periodo.De(new DateOnly(2026, 11, 5), Turno.Manha))];
        }

        public Escalacao Escalacao { get; } = DaEstiva();

        public Trabalhador Antonio { get; }

        public Trabalhador Carlos { get; }

        public Trabalhador[] Trabalhadores { get; }

        public Jornada[] Jornadas { get; }

        public Requisicao Requisicao { get; } = new(DaTarde, [[new(Sinaleiro, 1), new(Estivador, 2)]], QuartaAs10h);

        public Rodizio Rodizio { get; } = Rodizio.Novo
            .ComAVezEm(Sinaleiro, Inscricao.Registrado, Matricula.De(4152))
            .ComAVezEm(Estivador, Inscricao.Registrado, Matricula.De(4130));

        public Escala Escalar(DateTimeOffset? em = null) =>
            Escalador.Escalar(Escalacao, [Requisicao], Trabalhadores, Jornadas, Rodizio, Descanso.DaLei, em ?? Em(5, 10));
    }
}
