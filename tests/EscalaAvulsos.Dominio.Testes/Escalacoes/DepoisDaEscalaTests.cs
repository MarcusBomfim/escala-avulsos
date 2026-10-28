using EscalaAvulsos.Dominio.Comum;
using EscalaAvulsos.Dominio.Escalacoes;
using EscalaAvulsos.Dominio.Identificacao;
using EscalaAvulsos.Dominio.Periodos;
using EscalaAvulsos.Dominio.Requisicoes;
using EscalaAvulsos.Dominio.Testes.Trabalhadores;
using EscalaAvulsos.Dominio.Trabalhadores;
using static EscalaAvulsos.Dominio.Testes.Escalacoes.EscalacoesDeTeste;

namespace EscalaAvulsos.Dominio.Testes.Escalacoes;

public sealed class DepoisDaEscalaTests
{
    private const string Vaga = "estivador no terno 1 do navio Atlântico Sul (IMO 9876543), no Terminal Cais Leste, berço 4, em 05/11/2026, das 13h às 19h";

    [Fact]
    public void A_escala_guarda_a_fila_de_espera_de_cada_funcao()
    {
        var escala = new TardeDeQuinta().Escalar();

        // O Benedito, registrado que o rodízio não alcançou, e depois o João, cadastrado.
        Assert.Equal([4130, 5310], escala.FilaDeEsperaDe(Funcoes.Estivador).Select(matricula => matricula.Numero));

        // O Carlos não alcançou a vaga de sinaleiro, mas pegou uma de estivador: não espera nada.
        Assert.Empty(escala.FilaDeEsperaDe(Funcoes.Sinaleiro));
        Assert.Empty(escala.FilaDeEsperaDe(Funcoes.OperadorDeGuindaste));
    }

    [Fact]
    public void Quem_desiste_da_a_vaga_ao_primeiro_da_espera_e_a_vez_anda_ate_ele()
    {
        var tarde = new TardeDeQuinta();
        var escala = tarde.Escalar();

        var substituto = escala.Desistir(tarde.Edson.Id, Em(5, 11), []);

        Assert.Equal($"Benedito Lima (matrícula 4130) está na escala, no lugar de quem desistiu: {Vaga}.", substituto?.ToString());
        Assert.Equal("Edson Prado (matrícula 4101) desistiu da vaga de estivador em 05/11/2026, às 11h.", escala.SituacaoDe(Matricula.De(4101))?.ToString());
        Assert.Equal(["Antônio Ferreira", "Carlos Mota", "Benedito Lima"], escala.Engajamentos.Select(engajamento => engajamento.Nome));
        Assert.Equal(4130, escala.RodizioDepois.UltimoEscalado(Funcoes.Estivador, Inscricao.Registrado)?.Numero);
        Assert.Equal([5310], escala.FilaDeEsperaDe(Funcoes.Estivador).Select(matricula => matricula.Numero));

        var edson = escala.SituacaoDe(Matricula.De(4101))!;
        Assert.Equal((false, Em(5, 11)), (edson.Escalado, edson.DesistiuEm!.Value));
        Assert.Equal(Funcoes.Estivador, edson.VagaDesistida?.Funcao);
        Assert.True(substituto!.Substituto);
    }

    [Fact]
    public void Acabados_os_registrados_entra_o_cadastrado_e_anda_a_vez_dos_cadastrados()
    {
        var tarde = new TardeDeQuinta();
        var escala = tarde.Escalar();
        escala.Desistir(tarde.Edson.Id, Em(5, 11), []);

        var joao = escala.Desistir(tarde.Carlos.Id, Em(5, 11, 30), []);

        Assert.Equal($"João Batista (matrícula 5310) está na escala, no lugar de quem desistiu: {Vaga}.", joao?.ToString());
        Assert.Equal(5310, escala.RodizioDepois.UltimoEscalado(Funcoes.Estivador, Inscricao.Cadastrado)?.Numero);
        Assert.Equal(["Antônio Ferreira", "João Batista", "Benedito Lima"], escala.Engajamentos.Select(engajamento => engajamento.Nome));
        Assert.Empty(escala.FilaDeEsperaDe(Funcoes.Estivador));
    }

    [Fact]
    public void Sem_ninguem_na_espera_a_vaga_fica_vazia_e_o_operador_fica_sabendo()
    {
        var tarde = new TardeDeQuinta();
        var escala = tarde.Escalar();
        escala.Desistir(tarde.Edson.Id, Em(5, 11), []);
        escala.Desistir(tarde.Carlos.Id, Em(5, 11), []);

        Assert.Null(escala.Desistir(tarde.Benedito.Id, Em(5, 12), []));
        Assert.Null(escala.Desistir(tarde.Joao.Id, Em(5, 12), []));
        Assert.Null(escala.Desistir(tarde.Antonio.Id, Em(5, 12), []));

        Assert.Empty(escala.Engajamentos);
        Assert.Equal(
            ["estivador: 2, no terno 1 do navio Atlântico Sul (IMO 9876543)", "sinaleiro: 1, no terno 1 do navio Atlântico Sul (IMO 9876543)"],
            escala.VagasNaoPreenchidas.Select(vaga => vaga.ToString()));
        Assert.Equal("escala da estiva de 05/11/2026, das 13h às 19h: 0 de 3 vagas preenchidas", escala.ToString());
    }

    [Fact]
    public void Vagas_vazias_de_ternos_diferentes_nao_se_juntam()
    {
        var escalacao = DaEstiva();
        var antonio = TrabalhadoresDeTeste.Estivador();
        var benedito = TrabalhadoresDeTeste.Estivador(4130, "Benedito Lima");
        escalacao.Habilitar(antonio, [Funcoes.Estivador], QuintaAs6h);
        escalacao.Habilitar(benedito, [Funcoes.Estivador], QuintaAs6h);
        var requisicao = new Requisicao(TardeDeQuinta.DaTarde, [[new(Funcoes.Estivador, 1)], [new(Funcoes.Estivador, 1)]], Testes.Requisicoes.RequisicoesDeTeste.QuartaAs10h);
        var escala = Escalador.Escalar(escalacao, [requisicao], [antonio, benedito], [], Rodizio.Novo, Descanso.DaLei, Em(5, 10));

        escala.Desistir(antonio.Id, Em(5, 11), []);
        escala.Desistir(benedito.Id, Em(5, 11), []);

        Assert.Equal(
            ["estivador: 1, no terno 1 do navio Atlântico Sul (IMO 9876543)", "estivador: 1, no terno 2 do navio Atlântico Sul (IMO 9876543)"],
            escala.VagasNaoPreenchidas.Select(vaga => vaga.ToString()));
    }

    [Fact]
    public void O_substituto_que_desiste_passa_a_vaga_adiante_e_nao_volta_para_a_espera()
    {
        var tarde = new TardeDeQuinta();
        var escala = tarde.Escalar();
        escala.Desistir(tarde.Edson.Id, Em(5, 11), []);

        var joao = escala.Desistir(tarde.Benedito.Id, Em(5, 11, 30), []);

        Assert.Equal("João Batista", joao?.Nome);
        Assert.Equal("Benedito Lima (matrícula 4130) desistiu da vaga de estivador em 05/11/2026, às 11h30.", escala.SituacaoDe(Matricula.De(4130))?.ToString());
        Assert.Empty(escala.FilaDeEsperaDe(Funcoes.Estivador));
    }

    [Fact]
    public void Quem_espera_e_pegou_jornada_em_outra_escala_sem_o_descanso_e_pulado()
    {
        /*
         * Depois desta escala, o Benedito pegou a noite de quinta em outra, e
         * ficaria sem as 11 horas de descanso: a vaga do Edson vai para o João.
         * O Benedito continua na fila — a jornada dele pode ser desfeita.
         */
        var tarde = new TardeDeQuinta();
        var escala = tarde.Escalar();
        var noiteDeQuinta = Periodo.De(new DateOnly(2026, 11, 5), Turno.Noite);

        var substituto = escala.Desistir(tarde.Edson.Id, Em(5, 11), [new Jornada(tarde.Benedito.Id, noiteDeQuinta)]);

        Assert.Equal("João Batista", substituto?.Nome);
        Assert.Equal(5310, escala.RodizioDepois.UltimoEscalado(Funcoes.Estivador, Inscricao.Cadastrado)?.Numero);
        Assert.Equal([4130], escala.FilaDeEsperaDe(Funcoes.Estivador).Select(matricula => matricula.Numero));
    }

    [Fact]
    public void A_jornada_que_deixa_o_descanso_em_dia_nao_pula_ninguem()
    {
        var tarde = new TardeDeQuinta();
        var escala = tarde.Escalar();
        var manhaDeSexta = Periodo.De(new DateOnly(2026, 11, 6), Turno.Manha);
        var noiteDeQuinta = Periodo.De(new DateOnly(2026, 11, 5), Turno.Noite);

        // Das 19h de quinta às 7h de sexta são 12 horas; a noite de quinta é do João, que não é o primeiro da fila.
        var substituto = escala.Desistir(
            tarde.Edson.Id,
            Em(5, 11),
            [new Jornada(tarde.Benedito.Id, manhaDeSexta), new Jornada(tarde.Joao.Id, noiteDeQuinta)]);

        Assert.Equal("Benedito Lima", substituto?.Nome);
    }

    [Fact]
    public void O_descanso_do_substituto_e_o_da_escala()
    {
        // Feita com 13 horas de descanso, a escala não dá a vaga a quem tem só 12 até a manhã de sexta.
        var tarde = new TardeDeQuinta();
        var treze = new Descanso(TimeSpan.FromHours(13));
        var escala = Escalador.Escalar(tarde.Escalacao, [tarde.Requisicao], tarde.Trabalhadores, tarde.Jornadas, tarde.Rodizio, treze, Em(5, 10));
        var manhaDeSexta = Periodo.De(new DateOnly(2026, 11, 6), Turno.Manha);

        var substituto = escala.Desistir(tarde.Edson.Id, Em(5, 11), [new Jornada(tarde.Benedito.Id, manhaDeSexta)]);

        Assert.Equal(treze, escala.Descanso);
        Assert.Equal("João Batista", substituto?.Nome);
    }

    [Fact]
    public void Depois_que_o_periodo_comeca_nao_se_desiste_falta_se()
    {
        var tarde = new TardeDeQuinta();
        var escala = tarde.Escalar();

        var erro = Assert.Throws<ExcecaoDeDominio>(() => escala.Desistir(tarde.Edson.Id, Em(5, 13), []));

        Assert.Equal("O período de 05/11/2026, das 13h às 19h, já começou: quem não vai trabalhar fica com falta, e não desiste da vaga.", erro.Message);
        Assert.True(escala.SituacaoDe(Matricula.De(4101))!.Escalado);
    }

    [Fact]
    public void So_desiste_quem_tem_vaga()
    {
        var tarde = new TardeDeQuinta();
        var escala = tarde.Escalar();

        Assert.Equal(
            "Benedito Lima (matrícula 4130) não tem vaga na escala da estiva de 05/11/2026, das 13h às 19h.",
            Assert.Throws<ExcecaoDeDominio>(() => escala.Desistir(tarde.Benedito.Id, Em(5, 11), [])).Message);
        Assert.Equal(
            "O trabalhador não se habilitou para a escala da estiva de 05/11/2026, das 13h às 19h.",
            Assert.Throws<ExcecaoDeDominio>(() => escala.Desistir(TrabalhadorId.Novo(), Em(5, 11), [])).Message);
    }

    [Fact]
    public void A_presenca_e_confirmada_de_uma_hora_antes_ate_o_fim_do_periodo()
    {
        var tarde = new TardeDeQuinta();
        var escala = tarde.Escalar();

        var cedo = Assert.Throws<ExcecaoDeDominio>(() => escala.ConfirmarPresenca(tarde.Edson.Id, Em(5, 11, 59)));
        var tarde19h = Assert.Throws<ExcecaoDeDominio>(() => escala.ConfirmarPresenca(tarde.Edson.Id, Em(5, 19)));

        Assert.Equal("A presença na escala da estiva de 05/11/2026, das 13h às 19h só pode ser confirmada a partir de 05/11/2026, ao meio-dia.", cedo.Message);
        Assert.Equal("O período de 05/11/2026, das 13h às 19h, já terminou: a presença não pode mais ser confirmada.", tarde19h.Message);

        escala.ConfirmarPresenca(tarde.Edson.Id, Em(5, 12));
        escala.ConfirmarPresenca(tarde.Carlos.Id, Em(5, 18, 59));

        var edson = escala.SituacaoDe(Matricula.De(4101))!;
        Assert.Equal((Comparecimento.Presente, Em(5, 12)), (edson.Comparecimento, edson.PresenteDesde!.Value));
        Assert.Equal($"Edson Prado (matrícula 4101) está na escala: {Vaga}. Presença confirmada em 05/11/2026, ao meio-dia.", edson.ToString());
        Assert.Equal(
            "Edson Prado (matrícula 4101) já está com a presença confirmada.",
            Assert.Throws<ExcecaoDeDominio>(() => escala.ConfirmarPresenca(tarde.Edson.Id, Em(5, 13))).Message);
        Assert.Equal(
            "Edson Prado (matrícula 4101) já está com a presença confirmada: não dá mais para desistir.",
            Assert.Throws<ExcecaoDeDominio>(() => escala.Desistir(tarde.Edson.Id, Em(5, 12, 30), [])).Message);
    }

    [Fact]
    public void A_falta_e_registrada_depois_de_o_periodo_comecar_e_pode_ser_justificada()
    {
        var tarde = new TardeDeQuinta();
        var escala = tarde.Escalar();

        Assert.Equal(
            "O período de 05/11/2026, das 13h às 19h, ainda não começou: não há falta antes dele.",
            Assert.Throws<ExcecaoDeDominio>(() => escala.RegistrarFalta(tarde.Carlos.Id, Em(5, 12, 59))).Message);

        escala.RegistrarFalta(tarde.Carlos.Id, Em(5, 13, 30));

        Assert.Equal($"Carlos Mota (matrícula 4152) está na escala: {Vaga}. Faltou.", escala.SituacaoDe(Matricula.De(4152))?.ToString());
        Assert.Equal(
            "Carlos Mota (matrícula 4152) já tem falta registrada.",
            Assert.Throws<ExcecaoDeDominio>(() => escala.ConfirmarPresenca(tarde.Carlos.Id, Em(5, 14))).Message);

        escala.JustificarFalta(tarde.Carlos.Id, "  Atestado médico de 05/11 ");

        var carlos = escala.SituacaoDe(Matricula.De(4152))!;
        Assert.Equal((Comparecimento.FaltaJustificada, "Atestado médico de 05/11"), (carlos.Comparecimento, carlos.JustificativaDaFalta));
        Assert.EndsWith("Faltou, com justificativa: Atestado médico de 05/11.", carlos.ToString(), StringComparison.Ordinal);
        Assert.Equal(
            "Carlos Mota (matrícula 4152) já tem falta registrada.",
            Assert.Throws<ExcecaoDeDominio>(() => escala.RegistrarFalta(tarde.Carlos.Id, Em(5, 15))).Message);
    }

    [Fact]
    public void Quem_esta_presente_nao_tem_falta_e_so_se_justifica_falta_que_existe()
    {
        var tarde = new TardeDeQuinta();
        var escala = tarde.Escalar();
        escala.ConfirmarPresenca(tarde.Edson.Id, Em(5, 12, 50));

        Assert.Equal(
            "Edson Prado (matrícula 4101) já está com a presença confirmada.",
            Assert.Throws<ExcecaoDeDominio>(() => escala.RegistrarFalta(tarde.Edson.Id, Em(5, 14))).Message);
        Assert.Equal(
            "Edson Prado (matrícula 4101) não tem falta para justificar na escala da estiva de 05/11/2026, das 13h às 19h.",
            Assert.Throws<ExcecaoDeDominio>(() => escala.JustificarFalta(tarde.Edson.Id, "Atestado")).Message);

        Assert.Equal(
            "Carlos Mota (matrícula 4152) não tem falta para justificar na escala da estiva de 05/11/2026, das 13h às 19h.",
            Assert.Throws<ExcecaoDeDominio>(() => escala.JustificarFalta(tarde.Carlos.Id, "Atestado")).Message);

        escala.RegistrarFalta(tarde.Carlos.Id, Em(5, 14));

        Assert.Equal("Falta a justificativa da falta.", Assert.Throws<ExcecaoDeDominio>(() => escala.JustificarFalta(tarde.Carlos.Id, " ")).Message);
        Assert.Equal(
            "A justificativa da falta passa de 200 caracteres.",
            Assert.Throws<ExcecaoDeDominio>(() => escala.JustificarFalta(tarde.Carlos.Id, new string('a', 201))).Message);

        escala.JustificarFalta(tarde.Carlos.Id, "Atestado");

        // A falta já justificada não se justifica de novo.
        Assert.Equal(
            "Carlos Mota (matrícula 4152) não tem falta para justificar na escala da estiva de 05/11/2026, das 13h às 19h.",
            Assert.Throws<ExcecaoDeDominio>(() => escala.JustificarFalta(tarde.Carlos.Id, "Outro atestado")).Message);
    }

    [Fact]
    public void Ao_encerrar_quem_nao_confirmou_presenca_fica_com_falta_e_a_escala_para_de_mudar()
    {
        var tarde = new TardeDeQuinta();
        var escala = tarde.Escalar();
        escala.ConfirmarPresenca(tarde.Antonio.Id, Em(5, 12, 40));
        escala.ConfirmarPresenca(tarde.Edson.Id, Em(5, 12, 55));

        Assert.Equal(
            "O período de 05/11/2026, das 13h às 19h, só termina em 05/11/2026, às 19h: a escala é encerrada depois.",
            Assert.Throws<ExcecaoDeDominio>(() => escala.Encerrar(Em(5, 18, 59))).Message);

        escala.Encerrar(Em(5, 19));

        Assert.Equal(Em(5, 19), escala.EncerradaEm);
        // Antônio e Edson confirmaram; o Carlos, não.
        Assert.Equal(
            [Comparecimento.Presente, Comparecimento.Falta, Comparecimento.Presente],
            escala.Situacoes.Where(situacao => situacao.Escalado).OrderBy(situacao => situacao.Nome, StringComparer.Ordinal).Select(situacao => situacao.Comparecimento));
        Assert.Equal(Comparecimento.Aguardando, escala.SituacaoDe(Matricula.De(4130))!.Comparecimento);

        Action[] depois =
        [
            () => escala.Encerrar(Em(5, 20)),
            () => escala.ConfirmarPresenca(tarde.Carlos.Id, Em(5, 18)),
            () => escala.RegistrarFalta(tarde.Antonio.Id, Em(5, 18)),
        ];

        Assert.All(depois, mudanca => Assert.Equal(
            "A escala da estiva de 05/11/2026, das 13h às 19h já foi encerrada.",
            Assert.Throws<ExcecaoDeDominio>(mudanca).Message));

        // A falta ainda se justifica: o atestado costuma chegar depois.
        escala.JustificarFalta(tarde.Carlos.Id, "Atestado médico");

        Assert.Equal(Comparecimento.FaltaJustificada, escala.SituacaoDe(Matricula.De(4152))!.Comparecimento);
    }
}
