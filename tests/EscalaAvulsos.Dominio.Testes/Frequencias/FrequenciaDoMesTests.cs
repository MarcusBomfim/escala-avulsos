using EscalaAvulsos.Dominio.Comum;
using EscalaAvulsos.Dominio.Escalacoes;
using EscalaAvulsos.Dominio.Frequencias;
using EscalaAvulsos.Dominio.Periodos;
using EscalaAvulsos.Dominio.Requisicoes;
using EscalaAvulsos.Dominio.Testes.Trabalhadores;
using EscalaAvulsos.Dominio.Trabalhadores;
using static EscalaAvulsos.Dominio.Testes.Requisicoes.RequisicoesDeTeste;

namespace EscalaAvulsos.Dominio.Testes.Frequencias;

public sealed class FrequenciaDoMesTests
{
    private static readonly Mes Novembro = Mes.De(2026, 11);

    private static readonly DateOnly QuinzeDeNovembro = new(2026, 11, 15);

    private static readonly DateOnly PrimeiroDeDezembro = new(2026, 12, 1);

    [Fact]
    public void Conta_as_habilitacoes_o_trabalho_as_faltas_e_as_desistencias_do_mes()
    {
        var mes = new MesDoAntonio();
        mes.Habilitar(2);
        mes.Habilitar(3);
        mes.Trabalhar(4, Comparecimento.Presente);
        mes.Trabalhar(6, Comparecimento.Presente);
        mes.Trabalhar(9, Comparecimento.Falta);
        mes.Trabalhar(11, Comparecimento.FaltaJustificada);
        mes.Desistir(13);

        var frequencia = mes.Frequencia(QuinzeDeNovembro);

        Assert.Equal((7, 2, 1, 1, 1), (frequencia.Habilitacoes, frequencia.Trabalhados, frequencia.Faltas, frequencia.FaltasJustificadas, frequencia.Desistencias));
        Assert.Equal((12, 5, SituacaoDaFrequencia.EmAndamento), (frequencia.MinimoDeHabilitacoes, frequencia.HabilitacoesQueFaltam, frequencia.Situacao));
        Assert.Equal(
            "Antônio Ferreira (matrícula 4127), em 11/2026: 7 habilitações, e o mínimo é 12 — faltam 5 até 30/11/2026. "
            + "Trabalhou 2 períodos; 1 falta sem justificativa, de 2 aceitas; 1 falta justificada; 1 desistência.",
            frequencia.ToString());
    }

    [Fact]
    public void So_conta_o_mes_e_o_trabalhador_pedidos()
    {
        var mes = new MesDoAntonio();
        mes.Antonio.RegistrarAso(new Aso(new DateOnly(2026, 11, 20), new DateOnly(2027, 11, 19)));
        mes.Habilitar(5);
        mes.Habilitar(Periodo.De(new DateOnly(2026, 10, 31), Turno.Noite));
        mes.Habilitar(Periodo.De(new DateOnly(2026, 12, 1), Turno.Madrugada));
        mes.HabilitarOutro(5);
        mes.Trabalhar(Periodo.De(new DateOnly(2026, 12, 2), Turno.Manha), Comparecimento.Presente);
        mes.Trabalhar(Periodo.De(new DateOnly(2026, 12, 3), Turno.Manha), Comparecimento.Falta);

        var frequencia = mes.Frequencia(QuinzeDeNovembro);

        Assert.Equal((1, 0, 0), (frequencia.Habilitacoes, frequencia.Trabalhados, frequencia.Faltas));
    }

    [Fact]
    public void Faltas_no_limite_ainda_estao_dentro_dele()
    {
        var mes = new MesDoAntonio();
        mes.Trabalhar(2, Comparecimento.Falta);
        mes.Trabalhar(4, Comparecimento.Falta);

        var frequencia = mes.Frequencia(QuinzeDeNovembro);

        Assert.Equal((2, false, SituacaoDaFrequencia.EmAndamento), (frequencia.Faltas, frequencia.FaltasAcimaDoLimite, frequencia.Situacao));
    }

    [Fact]
    public void Com_o_mes_encerrado_abaixo_do_minimo_ficou_faltando()
    {
        var mes = new MesDoAntonio();
        mes.Trabalhar(4, Comparecimento.Presente);

        var frequencia = mes.Frequencia(PrimeiroDeDezembro);

        Assert.Equal((true, SituacaoDaFrequencia.Descumprida), (frequencia.MesEncerrado, frequencia.Situacao));
        Assert.Equal(
            "Antônio Ferreira (matrícula 4127), em 11/2026: 1 habilitação, e o mínimo era 12 — ficaram faltando 11. "
            + "Trabalhou 1 período; 0 faltas sem justificativa, de 2 aceitas.",
            frequencia.ToString());
        Assert.Equal(SituacaoDaFrequencia.EmAndamento, mes.Frequencia(new DateOnly(2026, 11, 30)).Situacao);
    }

    [Fact]
    public void Com_o_minimo_cumprido_a_frequencia_esta_cumprida_mesmo_antes_do_fim_do_mes()
    {
        var mes = new MesDoAntonio();

        for (var dia = 1; dia <= 12; dia++)
        {
            mes.Habilitar(dia);
        }

        var frequencia = mes.Frequencia(QuinzeDeNovembro);

        Assert.Equal((0, SituacaoDaFrequencia.Cumprida), (frequencia.HabilitacoesQueFaltam, frequencia.Situacao));
        Assert.StartsWith("Antônio Ferreira (matrícula 4127), em 11/2026: 12 habilitações, e o mínimo é 12. Trabalhou 0 períodos", frequencia.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Faltas_demais_descumprem_o_mes_mesmo_com_ele_aberto()
    {
        var mes = new MesDoAntonio();
        mes.Trabalhar(2, Comparecimento.Falta);
        mes.Trabalhar(4, Comparecimento.Falta);
        mes.Trabalhar(6, Comparecimento.Falta);

        var frequencia = mes.Frequencia(QuinzeDeNovembro);

        Assert.Equal((true, SituacaoDaFrequencia.Descumprida), (frequencia.FaltasAcimaDoLimite, frequencia.Situacao));
        Assert.EndsWith("Trabalhou 0 períodos; 3 faltas sem justificativa, de 2 aceitas — acima do limite.", frequencia.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void O_minimo_diminui_com_os_dias_em_que_nao_podia_trabalhar()
    {
        var mes = new MesDoAntonio();
        mes.Antonio.Impedir(new Impedimento(TipoDeImpedimento.Ferias, new DateOnly(2026, 11, 16), new DateOnly(2026, 11, 25)));

        var frequencia = mes.Frequencia(QuinzeDeNovembro);

        // 12 habilitações em 30 dias; ele podia trabalhar em 20: 12 × 20 / 30 = 8.
        Assert.Equal((10, 8), (frequencia.DiasImpedidos, frequencia.MinimoDeHabilitacoes));
        Assert.Contains("0 habilitações, e o mínimo é 8, pelos 20 dias em que podia trabalhar — faltam 8 até 30/11/2026.", frequencia.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void O_minimo_proporcional_e_arredondado_para_baixo_a_favor_do_trabalhador()
    {
        var mes = new MesDoAntonio();
        mes.Antonio.Impedir(new Impedimento(TipoDeImpedimento.Suspensao, new DateOnly(2026, 11, 20), new DateOnly(2026, 11, 20)));

        // 12 × 29 / 30 = 11,6: o mínimo é 11.
        Assert.Equal(11, mes.Frequencia(QuinzeDeNovembro).MinimoDeHabilitacoes);
    }

    [Fact]
    public void Afastamento_sem_fim_e_inscricao_cancelada_contam_como_dias_impedidos()
    {
        var afastado = new MesDoAntonio();
        afastado.Antonio.Impedir(new Impedimento(TipoDeImpedimento.Afastamento, new DateOnly(2026, 10, 20)));

        var aposentado = new MesDoAntonio();
        aposentado.Antonio.CancelarInscricao(new DateOnly(2026, 11, 16), "Aposentadoria");

        Assert.Equal((30, 0), (afastado.Frequencia(QuinzeDeNovembro).DiasImpedidos, afastado.Frequencia(QuinzeDeNovembro).MinimoDeHabilitacoes));
        Assert.Equal((15, 6), (aposentado.Frequencia(QuinzeDeNovembro).DiasImpedidos, aposentado.Frequencia(QuinzeDeNovembro).MinimoDeHabilitacoes));
        Assert.Equal(SituacaoDaFrequencia.Cumprida, afastado.Frequencia(PrimeiroDeDezembro).Situacao);
    }

    [Fact]
    public void A_regra_e_configuravel_e_nao_aceita_numero_negativo()
    {
        Assert.Equal((12, 2), (RegraDeFrequencia.Padrao.MinimoDeHabilitacoes, RegraDeFrequencia.Padrao.MaximoDeFaltas));

        var mes = new MesDoAntonio();
        mes.Trabalhar(2, Comparecimento.Falta);
        var semFalta = mes.Frequencia(QuinzeDeNovembro, new RegraDeFrequencia(1, 0));

        Assert.Equal((1, 0, SituacaoDaFrequencia.Descumprida), (semFalta.MinimoDeHabilitacoes, semFalta.HabilitacoesQueFaltam, semFalta.Situacao));

        var erro = Assert.Throws<ExcecaoDeDominio>(() => new RegraDeFrequencia(-1, -2));

        Assert.Equal(
            "O mínimo de habilitações no mês não pode ser negativo, e seria -1. O máximo de faltas no mês não pode ser negativo, e seria -2.",
            erro.Message);
    }

    /// <summary>Novembro de 2026 do Antônio: escalações em que ele se habilita, e escalas em que trabalha, falta ou desiste.</summary>
    private sealed class MesDoAntonio
    {
        private readonly List<Escalacao> _escalacoes = [];

        private readonly List<Escala> _escalas = [];

        public Trabalhador Antonio { get; } = TrabalhadoresDeTeste.Estivador();

        public void Habilitar(int dia) => Habilitar(Periodo.De(new DateOnly(2026, 11, dia), Turno.Manha));

        public Escalacao Habilitar(Periodo periodo)
        {
            var escalacao = new Escalacao(Categoria.Estiva, periodo, CalendarioDaEscalacao.Padrao);
            escalacao.Habilitar(Antonio, [Funcoes.Estivador], escalacao.AberturaDaHabilitacao);
            _escalacoes.Add(escalacao);

            return escalacao;
        }

        public void HabilitarOutro(int dia)
        {
            var escalacao = new Escalacao(Categoria.Estiva, Periodo.De(new DateOnly(2026, 11, dia), Turno.Tarde), CalendarioDaEscalacao.Padrao);
            escalacao.Habilitar(TrabalhadoresDeTeste.Estivador(4130, "Benedito Lima"), [Funcoes.Estivador], escalacao.AberturaDaHabilitacao);
            _escalacoes.Add(escalacao);
        }

        public void Trabalhar(int dia, Comparecimento comparecimento) => Trabalhar(Periodo.De(new DateOnly(2026, 11, dia), Turno.Manha), comparecimento);

        public void Trabalhar(Periodo periodo, Comparecimento comparecimento)
        {
            var escala = Escalar(periodo);

            switch (comparecimento)
            {
                case Comparecimento.Presente:
                    escala.ConfirmarPresenca(Antonio.Id, escala.Periodo.Inicio);
                    break;
                case Comparecimento.FaltaJustificada:
                    escala.RegistrarFalta(Antonio.Id, escala.Periodo.Inicio.AddHours(1));
                    escala.JustificarFalta(Antonio.Id, "Atestado médico");
                    break;
            }

            escala.Encerrar(escala.Periodo.Fim);
        }

        public void Desistir(int dia)
        {
            var escala = Escalar(Periodo.De(new DateOnly(2026, 11, dia), Turno.Manha));
            escala.Desistir(Antonio.Id, escala.Periodo.Inicio.AddHours(-1), []);
        }

        public FrequenciaDoMes Frequencia(DateOnly hoje, RegraDeFrequencia? regra = null) =>
            FrequenciaDoMes.De(Antonio, Novembro, _escalacoes, _escalas, regra ?? RegraDeFrequencia.Padrao, hoje);

        private Escala Escalar(Periodo periodo)
        {
            var escalacao = Habilitar(periodo);
            var requisicao = new Requisicao(
                new DadosDaRequisicao(CaisForte, AtlanticoSul, CaisLeste, escalacao.Periodo), [[new(Funcoes.Estivador, 1)]], escalacao.PrazoDasRequisicoes);
            var escala = Escalador.Escalar(escalacao, [requisicao], [Antonio], [], Rodizio.Novo, Descanso.DaLei, escalacao.FechamentoDaHabilitacao);
            _escalas.Add(escala);

            return escala;
        }
    }
}
