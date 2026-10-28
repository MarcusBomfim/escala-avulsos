using EscalaAvulsos.Dominio.Escalacoes;
using EscalaAvulsos.Dominio.Identificacao;
using EscalaAvulsos.Dominio.Periodos;
using EscalaAvulsos.Dominio.Requisicoes;
using EscalaAvulsos.Dominio.Testes.Escalacoes;
using EscalaAvulsos.Dominio.Testes.Trabalhadores;
using EscalaAvulsos.Dominio.Trabalhadores;
using static EscalaAvulsos.Dominio.Testes.Escalacoes.EscalacoesDeTeste;
using static EscalaAvulsos.Dominio.Testes.Requisicoes.RequisicoesDeTeste;
using static EscalaAvulsos.Infraestrutura.Testes.Documentos.Exemplos;

namespace EscalaAvulsos.Infraestrutura.Testes.Documentos;

/*
 * Gravar, ler e gravar de novo dá o mesmo documento: nada se perde no
 * caminho. E o que volta não é só parecido — responde igual às perguntas do
 * domínio, e continua de onde parou.
 */
public sealed class IdaEVoltaTests
{
    [Fact]
    public void O_trabalhador_volta_inteiro_e_com_a_mesma_identidade()
    {
        var antonio = TrabalhadorCompleto();
        var json = Json(antonio);

        var lido = LerTrabalhador(json, antonio.Id);

        Assert.Equal(json, Json(lido));
        Assert.Equal(antonio.Id, lido.Id);
        Assert.Equal(
            antonio.AptidaoPara(Funcoes.Sinaleiro, Periodo.De(new DateOnly(2026, 12, 3), Turno.Manha)).ToString(),
            lido.AptidaoPara(Funcoes.Sinaleiro, Periodo.De(new DateOnly(2026, 12, 3), Turno.Manha)).ToString());
    }

    [Fact]
    public void O_trabalhador_cancelado_volta_congelado()
    {
        var benedito = TrabalhadorCancelado();

        var lido = LerTrabalhador(Json(benedito), benedito.Id);

        Assert.Equal(Json(benedito), Json(lido));
        Assert.Equal((Inscricao.Cadastrado, "Aposentadoria"), (lido.Inscricao, lido.Cancelamento?.Motivo));
        Assert.Throws<Dominio.Comum.ExcecaoDeDominio>(() => lido.Registrar());
    }

    [Fact]
    public void A_requisicao_volta_com_os_ternos_na_ordem_e_o_cancelamento()
    {
        var requisicao = RequisicaoCancelada();

        var lida = LerRequisicao(Json(requisicao), requisicao.Id);

        Assert.Equal(Json(requisicao), Json(lida));
        Assert.Equal(requisicao.ToString(), lida.ToString());
        Assert.Equal((SituacaoDaRequisicao.Cancelada, "O navio atrasou"), (lida.Situacao, lida.MotivoDoCancelamento));
        Assert.Equal(["terno 1 — contramestre de terno: 1, sinaleiro: 1, operador de guindaste de bordo: 1, estivador: 8", "terno 2 — contramestre de terno: 1, sinaleiro: 1, operador de guindaste de bordo: 1, estivador: 8"],
            lida.Ternos.Select(terno => terno.ToString()));
    }

    [Fact]
    public void A_escalacao_volta_com_as_habilitacoes_e_o_calendario_de_quando_abriu()
    {
        var tarde = new TardeDeQuinta();
        var calendario = new CalendarioDaEscalacao(TimeSpan.FromHours(5), TimeSpan.FromHours(20), TimeSpan.FromHours(4));
        var escalacao = new Escalacao(Categoria.Estiva, QuintaATarde, calendario);
        escalacao.Habilitar(tarde.Antonio, [Funcoes.Sinaleiro, Funcoes.Estivador], Em(5, 6));
        escalacao.Habilitar(tarde.Benedito, [Funcoes.Estivador], Em(4, 18));

        var lida = LerEscalacao(Json(escalacao), escalacao.Id);

        Assert.Equal(Json(escalacao), Json(lida));
        Assert.Equal(calendario, lida.Calendario);
        Assert.Equal([Funcoes.Sinaleiro, Funcoes.Estivador], lida.HabilitacaoDe(tarde.Antonio.Id)?.Funcoes);
    }

    [Fact]
    public void A_escala_volta_com_vagas_situacoes_motivos_espera_e_rodizio()
    {
        var (escala, _) = EscalaCompleta();

        var lida = LerEscala(Json(escala), escala.Id);

        Assert.Equal(Json(escala), Json(lida));
        Assert.Equal(escala.ToString(), lida.ToString());
        Assert.Equal(escala.Situacoes.Select(situacao => situacao.ToString()), lida.Situacoes.Select(situacao => situacao.ToString()));
        Assert.Equal(escala.Engajamentos, lida.Engajamentos);
        Assert.Equal(escala.VagasNaoPreenchidas, lida.VagasNaoPreenchidas);
        Assert.Equal(escala.RequisicoesForaDoPrazo, lida.RequisicoesForaDoPrazo);
        Assert.Equal(escala.RodizioDepois.Vezes, lida.RodizioDepois.Vezes);
        Assert.Equal((escala.Escalacao, escala.EncerradaEm, escala.Descanso), (lida.Escalacao, lida.EncerradaEm, lida.Descanso));
    }

    [Fact]
    public void Os_motivos_de_quem_ficou_de_fora_voltam_com_a_mesma_frase()
    {
        var (escala, _) = EscalaCompleta();

        var lida = LerEscala(Json(escala), escala.Id);

        var tipos = lida.Situacoes.SelectMany(situacao => situacao.Motivos).Select(motivo => motivo.Tipo).Distinct().Order();
        Assert.Equal(Enum.GetValues<TipoDeMotivoNaEscala>().Order(), tipos);
        Assert.Equal(
            escala.SituacaoDe(Matricula.De(4199))?.Motivos.Single(motivo => motivo.Tipo == TipoDeMotivoNaEscala.NaoApto).MotivosDaAptidao,
            lida.SituacaoDe(Matricula.De(4199))?.Motivos.Single(motivo => motivo.Tipo == TipoDeMotivoNaEscala.NaoApto).MotivosDaAptidao);
    }

    [Fact]
    public void A_escala_lida_continua_de_onde_parou()
    {
        // Lida logo depois de feita, a escala ainda sabe quem espera: a desistência do Edson vai para o Benedito.
        var tarde = new TardeDeQuinta();
        var escala = tarde.Escalar();
        var lida = LerEscala(Json(escala), escala.Id);

        var substituto = lida.Desistir(tarde.Edson.Id, Em(5, 11), []);

        Assert.Equal("Benedito Lima", substituto?.Nome);
        Assert.Equal(4130, lida.RodizioDepois.UltimoEscalado(Funcoes.Estivador, Inscricao.Registrado)?.Numero);
        Assert.Equal([5310], lida.FilaDeEsperaDe(Funcoes.Estivador).Select(matricula => matricula.Numero));
    }

    [Fact]
    public void Cada_vaga_volta_no_seu_terno()
    {
        // Comparar o JSON de ida com o de volta não basta: um terno gravado errado volta errado igual.
        var antonio = TrabalhadoresDeTeste.Estivador();
        var benedito = TrabalhadoresDeTeste.Estivador(4130, "Benedito Lima");
        var escalacao = DaEstiva();
        escalacao.Habilitar(antonio, [Funcoes.Estivador], QuintaAs6h);
        escalacao.Habilitar(benedito, [Funcoes.Estivador], QuintaAs6h);
        var requisicao = new Requisicao(TardeDeQuinta.DaTarde, [[new(Funcoes.Estivador, 1)], [new(Funcoes.Estivador, 1)]], QuartaAs10h);
        var escala = Escalador.Escalar(escalacao, [requisicao], [antonio, benedito], [], Rodizio.Novo, Descanso.DaLei, Em(5, 10));
        var escalada = LerEscala(Json(escala), escala.Id);

        Assert.Equal([1, 2], escalada.Engajamentos.Select(engajamento => engajamento.Terno));
        Assert.Equal(escala.Engajamentos, escalada.Engajamentos);

        // O Benedito, no terno 2, desiste sem ninguém na espera: a vaga dele fica vazia no terno 2.
        escala.Desistir(benedito.Id, Em(5, 11), []);
        var lida = LerEscala(Json(escala), escala.Id);

        Assert.Equal(escala.Engajamentos, lida.Engajamentos);
        Assert.Equal(2, Assert.Single(lida.VagasNaoPreenchidas).Terno);
        Assert.Equal(escala.VagasNaoPreenchidas, lida.VagasNaoPreenchidas);
        Assert.Equal(2, lida.SituacaoDe(Matricula.De(4130))?.VagaDesistida?.Terno);
    }

    [Fact]
    public void O_descanso_volta_com_a_escala_e_vale_para_o_substituto()
    {
        // Feita com 13 horas de descanso, a escala lida não dá a vaga a quem tem só 12 até a manhã de sexta.
        var tarde = new TardeDeQuinta();
        var treze = new Descanso(TimeSpan.FromHours(13));
        var escala = Escalador.Escalar(tarde.Escalacao, [tarde.Requisicao], tarde.Trabalhadores, tarde.Jornadas, tarde.Rodizio, treze, Em(5, 10));

        var lida = LerEscala(Json(escala), escala.Id);
        var substituto = lida.Desistir(tarde.Edson.Id, Em(5, 11), [new Jornada(tarde.Benedito.Id, Periodo.De(new DateOnly(2026, 11, 6), Turno.Manha))]);

        Assert.Equal(treze, lida.Descanso);
        Assert.Equal("João Batista", substituto?.Nome);
    }

    [Fact]
    public void A_escala_encerrada_continua_encerrada()
    {
        var (escala, tarde) = EscalaCompleta();

        var lida = LerEscala(Json(escala), escala.Id);

        var erro = Assert.Throws<Dominio.Comum.ExcecaoDeDominio>(() => lida.ConfirmarPresenca(tarde.Carlos.Id, Em(5, 18)));
        Assert.Equal("A escala da estiva de 05/11/2026, das 13h às 19h já foi encerrada.", erro.Message);
    }
}
