using EscalaAvulsos.Dominio.Comum;
using EscalaAvulsos.Dominio.Escalacoes;
using EscalaAvulsos.Dominio.Extratos;
using EscalaAvulsos.Dominio.Frequencias;
using EscalaAvulsos.Dominio.Periodos;
using EscalaAvulsos.Dominio.Requisicoes;
using EscalaAvulsos.Dominio.Testes.Escalacoes;
using EscalaAvulsos.Dominio.Testes.Trabalhadores;
using EscalaAvulsos.Dominio.Trabalhadores;
using EscalaAvulsos.Infraestrutura.Persistencia;
using EscalaAvulsos.Infraestrutura.Testes.Documentos;
using Npgsql;
using static EscalaAvulsos.Dominio.Testes.Escalacoes.EscalacoesDeTeste;
using static EscalaAvulsos.Dominio.Testes.Requisicoes.RequisicoesDeTeste;

namespace EscalaAvulsos.Infraestrutura.Testes.Persistencia;

/*
 * A tarde de quinta gravada e escalada pelo banco. Sem escala anterior, o
 * rodízio começa pela menor matrícula: o Antônio fica com a vaga de
 * sinaleiro, o Edson e o Benedito com as de estivador; na espera de
 * estivador, o Carlos, o Dirceu e o João.
 */
public sealed class EscalaNoBancoTests(BancoDeTestes bancos) : TestesComBanco(bancos)
{
    private readonly TardeDeQuinta _tarde = new();

    [Fact]
    public async Task A_escala_gravada_volta_inteira_e_as_vagas_vao_para_a_tabela_que_o_banco_confere()
    {
        await GravarATarde();

        var escala = await Banco.FazerAEscala(_tarde.Escalacao.Id, Em(5, 10));

        await using var sessao = Banco.NovaSessao();
        var lida = (await sessao.Escalas().DaEscalacao(_tarde.Escalacao.Id, Cancelamento))!;
        Assert.Equal(1, lida.Versao);
        Assert.Equal(Exemplos.Json(escala), Exemplos.Json(lida.Agregado));
        Assert.Equal(["Antônio Ferreira", "Edson Prado", "Benedito Lima"], lida.Agregado.Engajamentos.Select(engajamento => engajamento.Nome));
        Assert.Equal(
            [(_tarde.Antonio.Id.Valor, Em(5, 13), Em(5, 19), Em(6, 6))],
            await Engajamentos(_tarde.Antonio.Id.Valor));
    }

    [Fact]
    public async Task Cada_escalacao_tem_uma_escala_so()
    {
        await GravarATarde();
        await Banco.FazerAEscala(_tarde.Escalacao.Id, Em(5, 10));

        var erro = await Assert.ThrowsAsync<AgregadoJaExiste>(() => Banco.FazerAEscala(_tarde.Escalacao.Id, Em(5, 10, 5)));

        Assert.Equal("A escala da estiva de 05/11/2026, das 13h às 19h, já foi feita: cada escalação tem uma escala só.", erro.Message);
    }

    [Fact]
    public async Task A_escala_sai_de_uma_escalacao_gravada()
    {
        await Banco.Gravar(_tarde.Trabalhadores, [_tarde.Requisicao]);
        var escala = _tarde.Escalar();

        await using var sessao = Banco.NovaSessao();
        var erro = await Assert.ThrowsAsync<AgregadoNaoEncontrado>(() => sessao.Escalas().Adicionar(escala, 0, Cancelamento));

        Assert.Equal($"A escalação {_tarde.Escalacao.Id} não está gravada: a escala sai de uma escalação aberta.", erro.Message);
    }

    [Fact]
    public async Task A_vaga_e_de_quem_esta_no_cadastro_e_sem_ela_nada_da_escala_fica_gravado()
    {
        await Banco.Gravar(_tarde.Trabalhadores.Where(trabalhador => trabalhador != _tarde.Antonio), [_tarde.Requisicao], _tarde.Escalacao);
        var escala = _tarde.Escalar();

        await using var sessao = Banco.NovaSessao();
        var erro = await Assert.ThrowsAsync<AgregadoNaoEncontrado>(() => sessao.Escalas().Adicionar(escala, 0, Cancelamento));

        Assert.Equal("Há vaga na escala para um trabalhador que não está gravado: a vaga é de quem está no cadastro.", erro.Message);
        Assert.Null(await sessao.Escalas().DaEscalacao(_tarde.Escalacao.Id, Cancelamento));
        Assert.Equal((Rodizio.Novo, 0), Desmontado(await sessao.Escalas().RodizioDa(Categoria.Estiva, Cancelamento)));
        Assert.Empty(await Engajamentos(_tarde.Benedito.Id.Valor));
    }

    [Fact]
    public async Task A_vaga_e_de_uma_requisicao_gravada()
    {
        await Banco.Gravar(_tarde.Trabalhadores, [], _tarde.Escalacao);
        var escala = _tarde.Escalar();

        await using var sessao = Banco.NovaSessao();
        var erro = await Assert.ThrowsAsync<AgregadoNaoEncontrado>(() => sessao.Escalas().Adicionar(escala, 0, Cancelamento));

        Assert.Equal("Há vaga na escala de uma requisição que não está gravada: a vaga é de um pedido do operador.", erro.Message);
    }

    [Fact]
    public async Task A_desistencia_troca_a_vaga_na_tabela_das_vagas()
    {
        await GravarATarde();
        var escala = await Banco.FazerAEscala(_tarde.Escalacao.Id, Em(5, 10));

        var substituto = await Banco.Desistir(escala.Id, _tarde.Edson.Id, Em(5, 11));

        Assert.Equal("Carlos Mota", substituto?.Nome);
        Assert.Empty(await Engajamentos(_tarde.Edson.Id.Valor));
        Assert.Single(await Engajamentos(_tarde.Carlos.Id.Valor));
    }

    [Fact]
    public async Task Quem_desistiu_ou_ficou_de_fora_continua_com_a_escala_no_mes()
    {
        // A escala é do mês de todos os habilitados, e não só dos escalados: a desistência do Edson conta na frequência dele.
        await GravarATarde();
        var escala = await Banco.FazerAEscala(_tarde.Escalacao.Id, Em(5, 10));
        await Banco.Desistir(escala.Id, _tarde.Edson.Id, Em(5, 11));

        await using var sessao = Banco.NovaSessao();
        var novembro = Mes.De(2026, 11);
        var doEdson = await sessao.Escalas().DoTrabalhadorNoMes(_tarde.Edson.Id, novembro, Cancelamento);
        var doJoao = await sessao.Escalas().DoTrabalhadorNoMes(_tarde.Joao.Id, novembro, Cancelamento);
        var escalacoesDoEdson = await sessao.Escalacoes.DoTrabalhadorNoMes(_tarde.Edson.Id, novembro, Cancelamento);

        Assert.Equal([escala.Id], doEdson.Select(doMes => doMes.Id));
        Assert.Equal([escala.Id], doJoao.Select(doMes => doMes.Id));
        Assert.Equal(
            1,
            FrequenciaDoMes.De(_tarde.Edson, novembro, escalacoesDoEdson, doEdson, RegraDeFrequencia.Padrao, new DateOnly(2026, 11, 6)).Desistencias);
    }

    [Fact]
    public async Task A_frequencia_e_o_extrato_do_mes_saem_do_banco()
    {
        /*
         * O caminho da tela "meu mês": as escalações e as escalas de novembro
         * em que o Antônio aparece, e o domínio conta (ADR-0009, ADR-0010).
         * Fora do mês dele ficam a manhã de sexta, só do Fábio, e a madrugada
         * de 1º de dezembro, que é dele, mas de outro mês.
         */
        var fabio = TrabalhadoresDeTeste.Estivador(4199, "Fábio Rocha");
        _tarde.Antonio.RegistrarAso(new Aso(new DateOnly(2026, 11, 20), new DateOnly(2027, 11, 19)));
        var sexta = Habilitada(Periodo.De(new DateOnly(2026, 11, 6), Turno.Manha), fabio);
        var dezembro = Habilitada(Periodo.De(new DateOnly(2026, 12, 1), Turno.Madrugada), _tarde.Antonio);
        await Banco.Gravar(
            [.. _tarde.Trabalhadores, fabio],
            [_tarde.Requisicao, UmEstivadorPara(sexta.Periodo), UmEstivadorPara(dezembro.Periodo)],
            _tarde.Escalacao,
            sexta,
            dezembro);
        var feita = await Banco.FazerAEscala(_tarde.Escalacao.Id, Em(5, 10));
        await Banco.FazerAEscala(sexta.Id, Em(6, 4));
        var deDezembro = await Banco.FazerAEscala(dezembro.Id, Em(30, 22));

        await using (var sessao = Banco.NovaSessao())
        {
            var lida = (await sessao.Escalas().Obter(feita.Id, Cancelamento))!;
            lida.Agregado.ConfirmarPresenca(_tarde.Antonio.Id, Em(5, 12, 40));
            lida.Agregado.Encerrar(Em(5, 19));
            await sessao.Escalas().Atualizar(lida.Agregado, lida.Versao, Cancelamento);
        }

        await using var doMes = Banco.NovaSessao();
        var novembro = Mes.De(2026, 11);
        var escalacoes = await doMes.Escalacoes.DoTrabalhadorNoMes(_tarde.Antonio.Id, novembro, Cancelamento);
        var escalas = await doMes.Escalas().DoTrabalhadorNoMes(_tarde.Antonio.Id, novembro, Cancelamento);
        var tabela = new TabelaDeRemuneracao([new(Funcoes.Estivador, Dinheiro.Reais(220m)), new(Funcoes.Sinaleiro, Dinheiro.Reais(260m))], adicionalNoturno: 0.20m);

        var frequencia = FrequenciaDoMes.De(_tarde.Antonio, novembro, escalacoes, escalas, RegraDeFrequencia.Padrao, new DateOnly(2026, 11, 6));
        var extrato = Extrato.De(_tarde.Antonio, novembro, escalas, tabela);

        Assert.Equal([_tarde.Escalacao.Id], escalacoes.Select(escalacao => escalacao.Id));
        Assert.Equal([feita.Id], escalas.Select(escala => escala.Id));
        Assert.Equal((1, 1, 0), (frequencia.Habilitacoes, frequencia.Trabalhados, frequencia.Faltas));
        Assert.Equal(
            "05/11/2026, das 13h às 19h — sinaleiro no navio Atlântico Sul (IMO 9876543): R$ 260,00, pago até 07/11/2026, às 19h",
            Assert.Single(extrato.Linhas).ToString());
        Assert.Equal(
            [deDezembro.Id],
            (await doMes.Escalas().DoTrabalhadorNoMes(_tarde.Antonio.Id, Mes.De(2026, 12), Cancelamento)).Select(escala => escala.Id));
    }

    private Task GravarATarde() => Banco.Gravar(_tarde.Trabalhadores, [_tarde.Requisicao], _tarde.Escalacao);

    private static Escalacao Habilitada(Periodo periodo, Trabalhador trabalhador)
    {
        var escalacao = new Escalacao(Categoria.Estiva, periodo, CalendarioDaEscalacao.Padrao);
        escalacao.Habilitar(trabalhador, [Funcoes.Estivador], escalacao.AberturaDaHabilitacao);

        return escalacao;
    }

    private static Requisicao UmEstivadorPara(Periodo periodo) =>
        new(new DadosDaRequisicao(CaisForte, AtlanticoSul, CaisLeste, periodo), [[new(Funcoes.Estivador, 1)]], QuartaAs10h);

    private static (Rodizio, int) Desmontado(Versionado<Rodizio> rodizio) => (rodizio.Agregado, rodizio.Versao);

    /// <summary>As linhas de vaga do trabalhador, como o banco as guarda: o início, o fim e o fim do descanso, no horário do porto.</summary>
    private async Task<List<(Guid, DateTimeOffset, DateTimeOffset, DateTimeOffset)>> Engajamentos(Guid trabalhador)
    {
        await using var conexao = new NpgsqlConnection(Banco.ConexaoDoDono);
        await conexao.OpenAsync(Cancelamento);
        await using var comando = new NpgsqlCommand(
            "select trabalhador_id, inicio, fim, descansado_em from escala.engajamentos where trabalhador_id = $1 order by inicio",
            conexao);
        comando.Parameters.Add(new NpgsqlParameter { Value = trabalhador });
        await using var leitor = await comando.ExecuteReaderAsync(Cancelamento);

        var linhas = new List<(Guid, DateTimeOffset, DateTimeOffset, DateTimeOffset)>();

        while (await leitor.ReadAsync(Cancelamento))
        {
            linhas.Add((
                leitor.GetGuid(0),
                leitor.GetFieldValue<DateTimeOffset>(1).ToOffset(TimeSpan.FromHours(-3)),
                leitor.GetFieldValue<DateTimeOffset>(2).ToOffset(TimeSpan.FromHours(-3)),
                leitor.GetFieldValue<DateTimeOffset>(3).ToOffset(TimeSpan.FromHours(-3))));
        }

        return linhas;
    }
}
