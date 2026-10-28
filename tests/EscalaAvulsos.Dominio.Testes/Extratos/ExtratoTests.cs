using EscalaAvulsos.Dominio.Comum;
using EscalaAvulsos.Dominio.Escalacoes;
using EscalaAvulsos.Dominio.Extratos;
using EscalaAvulsos.Dominio.Periodos;
using EscalaAvulsos.Dominio.Requisicoes;
using EscalaAvulsos.Dominio.Testes.Trabalhadores;
using EscalaAvulsos.Dominio.Trabalhadores;
using static EscalaAvulsos.Dominio.Testes.Requisicoes.RequisicoesDeTeste;

namespace EscalaAvulsos.Dominio.Testes.Extratos;

public sealed class ExtratoTests
{
    private static readonly Mes Novembro = Mes.De(2026, 11);

    private static readonly TabelaDeRemuneracao Tabela = new(
        [new(Funcoes.Estivador, Dinheiro.Reais(220m)), new(Funcoes.Sinaleiro, Dinheiro.Reais(260m))], adicionalNoturno: 0.20m);

    [Fact]
    public void So_entra_o_periodo_trabalhado_com_a_presenca_confirmada()
    {
        var mes = new MesDoAntonio();
        mes.Escalar(Periodo.De(new DateOnly(2026, 11, 5), Turno.Tarde), Funcoes.Sinaleiro, Comparecimento.Presente);
        mes.Escalar(Periodo.De(new DateOnly(2026, 11, 7), Turno.Noite), Funcoes.Estivador, Comparecimento.Presente);
        mes.Escalar(Periodo.De(new DateOnly(2026, 11, 9), Turno.Manha), Funcoes.Estivador, Comparecimento.Falta);
        mes.Escalar(Periodo.De(new DateOnly(2026, 11, 11), Turno.Manha), Funcoes.Estivador, Comparecimento.FaltaJustificada);
        mes.Desistir(Periodo.De(new DateOnly(2026, 11, 13), Turno.Manha));

        var extrato = Extrato.De(mes.Antonio, Novembro, mes.Escalas, Tabela);

        Assert.Equal(
            [
                "05/11/2026, das 13h às 19h — sinaleiro no navio Atlântico Sul (IMO 9876543): R$ 260,00, pago até 07/11/2026, às 19h",
                "07/11/2026, das 19h à 1h — estivador no navio Atlântico Sul (IMO 9876543): R$ 220,00 + R$ 44,00 de adicional noturno = R$ 264,00, pago até 10/11/2026, à 1h",
            ],
            extrato.Linhas.Select(linha => linha.ToString()));
    }

    [Fact]
    public void Soma_o_mes_com_as_provisoes_e_o_fgts()
    {
        var mes = new MesDoAntonio();
        mes.Escalar(Periodo.De(new DateOnly(2026, 11, 5), Turno.Tarde), Funcoes.Sinaleiro, Comparecimento.Presente);
        mes.Escalar(Periodo.De(new DateOnly(2026, 11, 7), Turno.Noite), Funcoes.Estivador, Comparecimento.Presente);
        mes.Escalar(Periodo.De(new DateOnly(2026, 11, 30), Turno.Noite), Funcoes.Estivador, Comparecimento.Presente);

        var extrato = Extrato.De(mes.Antonio, Novembro, mes.Escalas, Tabela);

        // 260 + 264 + 264 = 788; férias 788 / 9 = 87,555…; 13º 788 / 12 = 65,666…; FGTS 8% de 941,23 = 75,2984.
        Assert.Equal(
            (Dinheiro.Reais(788m), Dinheiro.Reais(87.56m), Dinheiro.Reais(65.67m), Dinheiro.Reais(75.30m)),
            (extrato.Remuneracao, extrato.Ferias, extrato.DecimoTerceiro, extrato.Fgts));
        Assert.Equal(
            "Antônio Ferreira (matrícula 4127), em 11/2026: 3 períodos, R$ 788,00; férias com 1/3, R$ 87,56; 13º, R$ 65,67; FGTS depositado, R$ 75,30.",
            extrato.ToString());
    }

    [Fact]
    public void As_linhas_seguem_a_ordem_dos_periodos_e_nao_a_das_escalas()
    {
        var mes = new MesDoAntonio();
        mes.Escalar(Periodo.De(new DateOnly(2026, 11, 20), Turno.Manha), Funcoes.Estivador, Comparecimento.Presente);
        mes.Escalar(Periodo.De(new DateOnly(2026, 11, 3), Turno.Tarde), Funcoes.Estivador, Comparecimento.Presente);
        mes.Escalar(Periodo.De(new DateOnly(2026, 11, 3), Turno.Manha), Funcoes.Estivador, Comparecimento.Presente);

        var extrato = Extrato.De(mes.Antonio, Novembro, mes.Escalas, Tabela);

        Assert.Equal(
            ["03/11/2026, das 7h às 13h", "03/11/2026, das 13h às 19h", "20/11/2026, das 7h às 13h"],
            extrato.Linhas.Select(linha => linha.Periodo.ToString()));
    }

    [Fact]
    public void A_noite_que_termina_no_mes_seguinte_e_do_mes_em_que_comeca()
    {
        var mes = new MesDoAntonio();
        mes.Antonio.RegistrarAso(new Aso(new DateOnly(2026, 11, 20), new DateOnly(2027, 11, 19)));
        mes.Escalar(Periodo.De(new DateOnly(2026, 11, 30), Turno.Noite), Funcoes.Estivador, Comparecimento.Presente);
        mes.Escalar(Periodo.De(new DateOnly(2026, 12, 1), Turno.Madrugada), Funcoes.Estivador, Comparecimento.Presente);

        var novembro = Extrato.De(mes.Antonio, Novembro, mes.Escalas, Tabela);
        var dezembro = Extrato.De(mes.Antonio, Mes.De(2026, 12), mes.Escalas, Tabela);

        Assert.Equal("30/11/2026, das 19h à 1h", Assert.Single(novembro.Linhas).Periodo.ToString());
        Assert.Equal(new DateTimeOffset(2026, 12, 3, 1, 0, 0, TimeSpan.FromHours(-3)), novembro.Linhas[0].PagoAte);
        Assert.Equal("01/12/2026, da 1h às 7h", Assert.Single(dezembro.Linhas).Periodo.ToString());
    }

    [Fact]
    public void O_extrato_e_so_do_trabalhador_pedido()
    {
        var mes = new MesDoAntonio();
        var benedito = TrabalhadoresDeTeste.Estivador(4130, "Benedito Lima");
        mes.Escalar(Periodo.De(new DateOnly(2026, 11, 5), Turno.Manha), Funcoes.Estivador, Comparecimento.Presente, benedito);

        var extrato = Extrato.De(mes.Antonio, Novembro, mes.Escalas, Tabela);

        Assert.Single(extrato.Linhas);
        Assert.Equal("Benedito Lima (matrícula 4130), em 11/2026: 1 período, R$ 220,00; férias com 1/3, R$ 24,44; 13º, R$ 18,33; FGTS depositado, R$ 21,02.", Extrato.De(benedito, Novembro, mes.Escalas, Tabela).ToString());
    }

    [Fact]
    public void Sem_trabalho_o_extrato_diz_que_nao_ha_periodo()
    {
        var extrato = Extrato.De(TrabalhadoresDeTeste.Estivador(), Novembro, [], Tabela);

        Assert.Empty(extrato.Linhas);
        Assert.Equal((Dinheiro.Zero, Dinheiro.Zero, Dinheiro.Zero, Dinheiro.Zero), (extrato.Remuneracao, extrato.Ferias, extrato.DecimoTerceiro, extrato.Fgts));
        Assert.Equal("Antônio Ferreira (matrícula 4127), em 11/2026: nenhum período trabalhado.", extrato.ToString());
    }

    [Fact]
    public void A_funcao_sem_valor_na_tabela_e_recusada_e_nao_paga_como_zero()
    {
        var mes = new MesDoAntonio();
        mes.Antonio.Qualificar(Funcoes.OperadorDeGuindaste, new DateOnly(2021, 7, 1));
        mes.Escalar(Periodo.De(new DateOnly(2026, 11, 5), Turno.Tarde), Funcoes.OperadorDeGuindaste, Comparecimento.Presente);

        var erro = Assert.Throws<ExcecaoDeDominio>(() => Extrato.De(mes.Antonio, Novembro, mes.Escalas, Tabela));

        Assert.Equal("A tabela de remuneração não tem o valor de operador de guindaste de bordo.", erro.Message);
    }

    /// <summary>Novembro de 2026 do Antônio: escalas em que ele trabalha, falta ou desiste.</summary>
    private sealed class MesDoAntonio
    {
        private readonly List<Escala> _escalas = [];

        public MesDoAntonio() => Antonio.Qualificar(Funcoes.Sinaleiro, new DateOnly(2010, 5, 10));

        public Trabalhador Antonio { get; } = TrabalhadoresDeTeste.Estivador();

        public IReadOnlyList<Escala> Escalas => _escalas;

        public void Escalar(Periodo periodo, Funcao funcao, Comparecimento comparecimento, Trabalhador? mais = null)
        {
            var escala = Escalada(periodo, funcao, mais);

            switch (comparecimento)
            {
                case Comparecimento.Presente:
                    escala.ConfirmarPresenca(Antonio.Id, periodo.Inicio);
                    break;
                case Comparecimento.FaltaJustificada:
                    escala.RegistrarFalta(Antonio.Id, periodo.Inicio);
                    escala.JustificarFalta(Antonio.Id, "Atestado médico");
                    break;
            }

            if (mais is not null)
            {
                escala.ConfirmarPresenca(mais.Id, periodo.Inicio);
            }

            escala.Encerrar(periodo.Fim);
        }

        public void Desistir(Periodo periodo) => Escalada(periodo, Funcoes.Estivador).Desistir(Antonio.Id, periodo.Inicio.AddHours(-1));

        private Escala Escalada(Periodo periodo, Funcao funcao, Trabalhador? mais = null)
        {
            var escalacao = new Escalacao(Categoria.Estiva, periodo, CalendarioDaEscalacao.Padrao);
            escalacao.Habilitar(Antonio, [funcao], escalacao.AberturaDaHabilitacao);

            if (mais is not null)
            {
                escalacao.Habilitar(mais, [funcao], escalacao.AberturaDaHabilitacao);
            }

            var requisicao = new Requisicao(
                new DadosDaRequisicao(CaisForte, AtlanticoSul, CaisLeste, periodo), [[new(funcao, mais is null ? 1 : 2)]], escalacao.PrazoDasRequisicoes);
            Trabalhador[] trabalhadores = mais is null ? [Antonio] : [Antonio, mais];
            var escala = Escalador.Escalar(escalacao, [requisicao], trabalhadores, [], Rodizio.Novo, Descanso.DaLei, escalacao.FechamentoDaHabilitacao);
            _escalas.Add(escala);

            return escala;
        }
    }
}
