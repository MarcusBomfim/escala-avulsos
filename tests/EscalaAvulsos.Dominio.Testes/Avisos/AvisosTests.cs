using EscalaAvulsos.Dominio.Avisos;
using EscalaAvulsos.Dominio.Escalacoes;
using EscalaAvulsos.Dominio.Requisicoes;
using EscalaAvulsos.Dominio.Testes.Escalacoes;
using EscalaAvulsos.Dominio.Testes.Requisicoes;
using EscalaAvulsos.Dominio.Testes.Trabalhadores;
using EscalaAvulsos.Dominio.Trabalhadores;
using static EscalaAvulsos.Dominio.Testes.Escalacoes.EscalacoesDeTeste;

namespace EscalaAvulsos.Dominio.Testes.Avisos;

public sealed class AvisosTests
{
    private const string AQuinta = "Quinta-feira, 5 de novembro, das 13h às 19h";

    [Fact]
    public void A_escala_que_sai_avisa_cada_habilitado_onde_vai_trabalhar_ou_por_que_ficou_de_fora()
    {
        var tarde = new TardeDeQuinta();
        var escala = tarde.Escalar();

        var avisos = AvisosDaEscala.DaEscalaQueSaiu(escala).ToDictionary(aviso => aviso.Trabalhador);

        Assert.Equal(6, avisos.Count);
        Assert.All(avisos.Values, aviso => Assert.Equal(($"escala:{escala.Id}", Em(5, 10)), (aviso.Chave, aviso.CriadoEm)));

        var antonio = avisos[tarde.Antonio.Id];
        Assert.Equal(TipoDoAviso.VaiTrabalhar, antonio.Tipo);
        Assert.Equal("Você vai trabalhar", antonio.Titulo);
        Assert.Equal($"{AQuinta}: sinaleiro no terno 1 do navio Atlântico Sul, no Terminal Cais Leste, berço 4.", antonio.Texto);

        var benedito = avisos[tarde.Benedito.Id];
        Assert.Equal(TipoDoAviso.FicouDeFora, benedito.Tipo);
        Assert.Equal("Você ficou de fora da escala", benedito.Titulo);
        Assert.StartsWith($"{AQuinta}: ", benedito.Texto, StringComparison.Ordinal);
        Assert.EndsWith(" Você é o 1º da espera de estivador: se alguém desistir, a vaga pode ser sua.", benedito.Texto, StringComparison.Ordinal);

        // O João é cadastrado: espera depois do Benedito, que é registrado.
        Assert.EndsWith(" Você é o 2º da espera de estivador: se alguém desistir, a vaga pode ser sua.", avisos[tarde.Joao.Id].Texto, StringComparison.Ordinal);

        // O Dirceu trabalhou de manhã: o descanso o tira da escala, e ele não está em espera nenhuma.
        var dirceu = avisos[tarde.Dirceu.Id];
        Assert.Equal(TipoDoAviso.FicouDeFora, dirceu.Tipo);
        Assert.DoesNotContain("espera", dirceu.Texto, StringComparison.Ordinal);
        Assert.Contains("descanso", dirceu.Texto, StringComparison.Ordinal);
    }

    [Fact]
    public void Quem_espera_em_duas_funcoes_le_o_lugar_mais_perto_da_vaga()
    {
        var tarde = new TardeDeQuinta();
        var gilberto = TrabalhadoresDeTeste.Estivador(4140, "Gilberto Souza");
        gilberto.Qualificar(Funcoes.Sinaleiro, new DateOnly(2011, 1, 1));
        tarde.Escalacao.Habilitar(gilberto, [Funcoes.Sinaleiro, Funcoes.Estivador], Em(5, 8));
        var helio = TrabalhadoresDeTeste.Estivador(4145, "Hélio Ramos");
        helio.Qualificar(Funcoes.Sinaleiro, new DateOnly(2011, 1, 1));
        tarde.Escalacao.Habilitar(helio, [Funcoes.Sinaleiro], Em(5, 8));
        var umDeCada = new Requisicao(TardeDeQuinta.DaTarde, [[new(Funcoes.Sinaleiro, 1), new(Funcoes.Estivador, 1)]], RequisicoesDeTeste.QuartaAs10h);

        var escala = Escalador.Escalar(tarde.Escalacao, [umDeCada], [.. tarde.Trabalhadores, gilberto, helio], tarde.Jornadas, tarde.Rodizio, Descanso.DaLei, Em(5, 10));

        // O Carlos é o 2º da espera de sinaleiro, que vem primeiro nas funções dele, e o 1º da de estivador.
        Assert.Equal([helio.Matricula, tarde.Carlos.Matricula], escala.FilaDeEsperaDe(Funcoes.Sinaleiro));
        Assert.Equal(tarde.Carlos.Matricula, escala.FilaDeEsperaDe(Funcoes.Estivador)[0]);
        var carlos = AvisosDaEscala.DaEscalaQueSaiu(escala).Single(aviso => aviso.Trabalhador == tarde.Carlos.Id);
        Assert.EndsWith(" Você é o 1º da espera de estivador: se alguém desistir, a vaga pode ser sua.", carlos.Texto, StringComparison.Ordinal);
    }

    [Fact]
    public void Quem_entra_no_lugar_de_quem_desistiu_e_avisado_e_so_ele()
    {
        var tarde = new TardeDeQuinta();
        var antes = tarde.Escalar();
        var depois = tarde.Escalar();
        depois.Desistir(tarde.Edson.Id, Em(5, 11), []);

        var aviso = Assert.Single(AvisosDaEscala.DaMudanca(antes, depois, Em(5, 11)));

        Assert.Equal((tarde.Benedito.Id, TipoDoAviso.EntrouPelaEspera, $"espera:{depois.Id}"), (aviso.Trabalhador, aviso.Tipo, aviso.Chave));
        Assert.Equal("Você entrou na escala", aviso.Titulo);
        Assert.Equal(
            $"Quem estava na vaga desistiu, e ela passou para você. {AQuinta}: estivador no terno 1 do navio Atlântico Sul, no Terminal Cais Leste, berço 4.",
            aviso.Texto);
        Assert.Empty(AvisosDaEscala.DaMudanca(depois, depois, Em(5, 11)));
    }

    [Fact]
    public void A_habilitacao_que_vai_fechar_chama_quem_pode_trabalhar_e_ainda_nao_se_habilitou()
    {
        var tarde = new TardeDeQuinta();
        var faltou = TrabalhadoresDeTeste.Estivador(4140, "Gilberto Souza");
        var semExame = new Trabalhador(new DadosDoTrabalhador(
            Dominio.Identificacao.Matricula.De(4141), "Hélio Ramos", TrabalhadoresDeTeste.CpfDaMatricula(4141), Categoria.Estiva, Inscricao.Registrado));
        semExame.Qualificar(Funcoes.Estivador, new DateOnly(1994, 3, 1));
        semExame.RegistrarAso(new Aso(new DateOnly(2025, 11, 1), new DateOnly(2026, 11, 4)));
        var conferente = new Trabalhador(new DadosDoTrabalhador(
            Dominio.Identificacao.Matricula.De(4142), "Ivo Nunes", TrabalhadoresDeTeste.CpfDaMatricula(4142), Categoria.ConferenciaDeCarga, Inscricao.Registrado));
        conferente.Qualificar(Funcoes.Conferente, new DateOnly(2001, 1, 1));
        conferente.RegistrarAso(new Aso(new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1)));
        Trabalhador[] todos = [.. tarde.Trabalhadores, faltou, semExame, conferente];

        // A habilitação fecha às 10h: às 8h59 ainda falta mais de uma hora.
        Assert.Empty(AvisosDosPrazos.DaHabilitacaoQueVaiFechar(tarde.Escalacao, todos, Em(5, 8, 59), AvisosDosPrazos.AntecedenciaDoFechamento));
        Assert.Empty(AvisosDosPrazos.DaHabilitacaoQueVaiFechar(tarde.Escalacao, todos, Em(5, 10), AvisosDosPrazos.AntecedenciaDoFechamento));

        var aviso = Assert.Single(AvisosDosPrazos.DaHabilitacaoQueVaiFechar(tarde.Escalacao, todos, Em(5, 9), AvisosDosPrazos.AntecedenciaDoFechamento));

        Assert.Equal((faltou.Id, TipoDoAviso.HabilitacaoVaiFechar, $"habilitacao:{tarde.Escalacao.Id}"), (aviso.Trabalhador, aviso.Tipo, aviso.Chave));
        Assert.Equal("A habilitação vai fechar", aviso.Titulo);
        Assert.Equal("A habilitação para quinta-feira, 5 de novembro, das 13h às 19h fecha às 10h, e você ainda não se habilitou.", aviso.Texto);
    }

    [Fact]
    public void O_exame_e_a_reciclagem_sao_avisados_trinta_dias_antes_e_cada_validade_uma_vez()
    {
        var antonio = TrabalhadoresDeTeste.Estivador();
        antonio.Qualificar(Funcoes.Sinaleiro, new DateOnly(2010, 5, 10), validaAte: new DateOnly(2026, 12, 4));

        // O exame vale até 30/11: em 30/10 faltam 31 dias, e ainda não é hora; em 31/10 faltam 30, e é.
        Assert.Empty(AvisosDosPrazos.DosVencimentos(antonio, Em(1, 10).AddDays(-2)));
        Assert.Equal(["aso:2026-11-30"], AvisosDosPrazos.DosVencimentos(antonio, Em(1, 10).AddDays(-1)).Select(aviso => aviso.Chave));

        var avisos = AvisosDosPrazos.DosVencimentos(antonio, Em(5, 10));

        Assert.Equal(["aso:2026-11-30", "reciclagem:sinaleiro:2026-12-04"], avisos.Select(aviso => aviso.Chave));
        Assert.Equal(
            "O seu exame médico (ASO) vale até 30/11/2026. Sem ele em dia, você não entra na escala.",
            avisos[0].Texto);
        Assert.Equal(
            "A sua reciclagem de sinaleiro vale até 04/12/2026. Depois disso, você não entra na escala nessa função.",
            avisos[1].Texto);

        // Vencido, não é mais aviso do que vai vencer: é a aptidão que diz o porquê.
        Assert.Equal(["reciclagem:sinaleiro:2026-12-04"], AvisosDosPrazos.DosVencimentos(antonio, Em(1, 10).AddDays(30)).Select(aviso => aviso.Chave));

        // Com a inscrição cancelada, ele não volta à escala: nada vai vencer que importe.
        antonio.CancelarInscricao(new DateOnly(2026, 11, 1), "Aposentadoria.");
        Assert.Empty(AvisosDosPrazos.DosVencimentos(antonio, Em(5, 10)));
    }
}
