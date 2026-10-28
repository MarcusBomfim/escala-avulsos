using EscalaAvulsos.Dominio.Escalacoes;
using EscalaAvulsos.Dominio.Identificacao;
using EscalaAvulsos.Dominio.Periodos;
using EscalaAvulsos.Dominio.Requisicoes;
using EscalaAvulsos.Dominio.Testes.Trabalhadores;
using EscalaAvulsos.Dominio.Trabalhadores;
using static EscalaAvulsos.Dominio.Testes.Requisicoes.RequisicoesDeTeste;

namespace EscalaAvulsos.Dominio.Testes.Escalacoes;

/*
 * Trezentas escalas sorteadas, com semente fixa: habilitados com funções,
 * inscrições, suspensões e jornadas sorteadas, requisições no prazo, fora dele
 * e canceladas, e o rodízio parado em qualquer lugar. Em todas, as regras da
 * escala são conferidas de fora, sem repetir a conta do escalador.
 */
public sealed class EscalasSorteadasTests
{
    private const int Escalas = 300;

    private static readonly Funcao[] Funcoes3 = [Funcoes.Sinaleiro, Funcoes.OperadorDeGuindaste, Funcoes.Estivador];

    [Fact]
    public void As_regras_da_escala_valem_em_todas()
    {
        var sorteio = new Random(20261105);

        for (var caso = 0; caso < Escalas; caso++)
        {
            var cenario = Cenario.Sortear(sorteio);

            var escala = Escalador.Escalar(
                cenario.Escalacao, cenario.Requisicoes, cenario.Trabalhadores, cenario.Jornadas, cenario.Rodizio, Descanso.DaLei, cenario.Em);

            Conferir(cenario, escala, $"Escala sorteada {caso}");
        }
    }

    private static void Conferir(Cenario cenario, Escala escala, string qual)
    {
        var periodo = cenario.Escalacao.Periodo;
        var porId = cenario.Trabalhadores.ToDictionary(trabalhador => trabalhador.Id);
        var habilitacoes = cenario.Escalacao.Habilitacoes;
        var emDescanso = habilitacoes.ToDictionary(
            habilitacao => habilitacao.Trabalhador,
            habilitacao => Descanso.DaLei.ConflitoCom(periodo, cenario.Jornadas.Where(jornada => jornada.Trabalhador == habilitacao.Trabalhador).Select(jornada => jornada.Periodo)) is not null);
        var engajado = escala.Engajamentos.ToLookup(engajamento => engajamento.Trabalhador);

        bool Elegivel(TrabalhadorId trabalhador, Funcao funcao) => !emDescanso[trabalhador] && porId[trabalhador].AptidaoPara(funcao, periodo).Apto;
        bool SemDesculpa(Habilitacao habilitacao, Funcao funcao) => !engajado.Contains(habilitacao.Trabalhador) && Elegivel(habilitacao.Trabalhador, funcao);

        // Ninguém em duas vagas; cada um na vaga de uma função em que se habilitou e que podia exercer.
        Assert.True(escala.Engajamentos.Select(engajamento => engajamento.Trabalhador).Distinct().Count() == escala.Engajamentos.Count, qual);
        Assert.All(escala.Engajamentos, engajamento =>
        {
            Assert.Contains(engajamento.Funcao, habilitacoes.Single(habilitacao => habilitacao.Trabalhador == engajamento.Trabalhador).Funcoes);
            Assert.True(Elegivel(engajamento.Trabalhador, engajamento.Funcao), $"{qual}: {engajamento}");
        });

        // As vagas que entram são as das requisições abertas que chegaram no prazo; nenhuma some.
        var noPrazo = cenario.Requisicoes
            .Where(requisicao => requisicao.Situacao == SituacaoDaRequisicao.Aberta && requisicao.FeitaEm <= cenario.Escalacao.PrazoDasRequisicoes)
            .ToList();
        var foraDoPrazo = cenario.Requisicoes
            .Where(requisicao => requisicao.Situacao == SituacaoDaRequisicao.Aberta && requisicao.FeitaEm > cenario.Escalacao.PrazoDasRequisicoes)
            .Select(requisicao => requisicao.Id);

        Assert.Equal(foraDoPrazo.ToHashSet(), escala.RequisicoesForaDoPrazo.ToHashSet());

        foreach (var requisicao in noPrazo)
        {
            foreach (var terno in requisicao.Ternos)
            {
                foreach (var vagas in terno.Vagas)
                {
                    var preenchidas = escala.Engajamentos.Count(engajamento => engajamento.Requisicao == requisicao.Id && engajamento.Terno == terno.Numero && engajamento.Funcao == vagas.Funcao);
                    var vazias = escala.VagasNaoPreenchidas
                        .Where(vaga => vaga.Requisicao == requisicao.Id && vaga.Terno == terno.Numero && vaga.Funcao == vagas.Funcao)
                        .Sum(vaga => vaga.Quantidade);

                    Assert.True(preenchidas + vazias == vagas.Quantidade, $"{qual}: terno {terno.Numero}, {vagas.Funcao}.");
                }
            }
        }

        Assert.Equal(noPrazo.Sum(requisicao => requisicao.TotalDeVagas), escala.VagasPedidas);

        foreach (var funcao in Funcoes3)
        {
            var naFuncao = habilitacoes.Where(habilitacao => habilitacao.Funcoes.Contains(funcao)).ToList();

            // Vaga vazia numa função quer dizer que ninguém habilitado nela podia pegá-la.
            if (escala.VagasNaoPreenchidas.Any(vaga => vaga.Funcao == funcao))
            {
                Assert.DoesNotContain(naFuncao, habilitacao => SemDesculpa(habilitacao, funcao));
            }

            // Cadastrado na vaga quer dizer que nenhum registrado que podia pegá-la ficou sem vaga.
            if (escala.Engajamentos.Any(engajamento => engajamento.Funcao == funcao && porId[engajamento.Trabalhador].Inscricao == Inscricao.Cadastrado))
            {
                Assert.DoesNotContain(naFuncao, habilitacao => porId[habilitacao.Trabalhador].Inscricao == Inscricao.Registrado && SemDesculpa(habilitacao, funcao));
            }

            foreach (var inscricao in (Inscricao[])[Inscricao.Registrado, Inscricao.Cadastrado])
            {
                ConferirARoda(cenario, escala, funcao, inscricao, naFuncao.Where(habilitacao => porId[habilitacao.Trabalhador].Inscricao == inscricao).ToList(), SemDesculpa, qual);
            }
        }

        // Uma situação por habilitado, na ordem das matrículas; quem descansa não pega vaga, e só o descanso explica.
        Assert.Equal(habilitacoes.Select(habilitacao => habilitacao.Trabalhador), escala.Situacoes.Select(situacao => situacao.Trabalhador));
        Assert.All(escala.Situacoes, situacao =>
        {
            Assert.Equal(engajado.Contains(situacao.Trabalhador), situacao.Escalado);

            if (emDescanso[situacao.Trabalhador])
            {
                Assert.False(situacao.Escalado);
                Assert.Equal(TipoDeMotivoNaEscala.EmDescanso, Assert.Single(situacao.Motivos).Tipo);
            }
            else if (!situacao.Escalado)
            {
                Assert.NotEmpty(situacao.Motivos);
            }
        });
    }

    /// <summary>
    /// Na roda da função e da inscrição: os escalados são os primeiros da vez,
    /// pulando só quem não podia ou já tinha vaga; e a vez para no último deles.
    /// </summary>
    private static void ConferirARoda(
        Cenario cenario,
        Escala escala,
        Funcao funcao,
        Inscricao inscricao,
        List<Habilitacao> naRoda,
        Func<Habilitacao, Funcao, bool> semDesculpa,
        string qual)
    {
        var ordem = cenario.Rodizio.NaOrdemDaVez(funcao, inscricao, naRoda.Select(habilitacao => habilitacao.Matricula)).ToList();
        var porMatricula = naRoda.ToDictionary(habilitacao => habilitacao.Matricula);
        var escaladosNaRoda = escala.Engajamentos
            .Where(engajamento => engajamento.Funcao == funcao && porMatricula.ContainsKey(engajamento.Matricula))
            .Select(engajamento => ordem.IndexOf(engajamento.Matricula))
            .ToList();

        if (escaladosNaRoda.Count == 0)
        {
            Assert.Equal(cenario.Rodizio.UltimoEscalado(funcao, inscricao), escala.RodizioDepois.UltimoEscalado(funcao, inscricao));
            return;
        }

        var ultimo = escaladosNaRoda.Max();

        Assert.DoesNotContain(ordem.Take(ultimo), matricula => semDesculpa(porMatricula[matricula], funcao));
        Assert.True(ordem[ultimo] == escala.RodizioDepois.UltimoEscalado(funcao, inscricao), $"{qual}: a vez de {funcao} entre os {inscricao}.");
    }

    private sealed record Cenario(
        Escalacao Escalacao,
        IReadOnlyList<Requisicao> Requisicoes,
        IReadOnlyList<Trabalhador> Trabalhadores,
        IReadOnlyList<Jornada> Jornadas,
        Rodizio Rodizio,
        DateTimeOffset Em)
    {
        public static Cenario Sortear(Random sorteio)
        {
            var periodo = Periodo.De(new DateOnly(2026, 11, 5), (Turno)sorteio.Next(4));
            var escalacao = new Escalacao(Categoria.Estiva, periodo, CalendarioDaEscalacao.Padrao);

            // Matrículas próximas umas das outras, para a roda dar a volta com frequência.
            var trabalhadores = Enumerable.Range(1000, 60)
                .OrderBy(_ => sorteio.Next())
                .Take(sorteio.Next(3, 25))
                .Select(numero => Trabalhador(numero, sorteio))
                .ToList();

            foreach (var trabalhador in trabalhadores.Where(_ => sorteio.Next(10) < 8))
            {
                var qualificadas = trabalhador.Qualificacoes.Select(qualificacao => qualificacao.Funcao).ToList();
                var escolhidas = qualificadas.Where(_ => sorteio.Next(3) > 0).DefaultIfEmpty(qualificadas[sorteio.Next(qualificadas.Count)]).ToList();
                var quando = escalacao.AberturaDaHabilitacao.AddMinutes(sorteio.Next(9 * 60));

                escalacao.Habilitar(trabalhador, escolhidas, quando);

                // Depois de habilitado, às vezes chega uma suspensão para o dia.
                if (sorteio.Next(10) == 0)
                {
                    trabalhador.Impedir(new Impedimento(TipoDeImpedimento.Suspensao, periodo.Dia, periodo.Dia));
                }
            }

            var jornadas = trabalhadores
                .Where(_ => sorteio.Next(4) == 0)
                .Select(trabalhador => new Jornada(trabalhador.Id, Deslocado(periodo, sorteio.Next(-4, 5))))
                .ToList();

            var requisicoes = Enumerable.Range(0, sorteio.Next(1, 4)).Select(_ => Requisicao(escalacao, sorteio)).ToList();

            var rodizio = Rodizio.Novo;

            foreach (var funcao in Funcoes3)
            {
                foreach (var inscricao in (Inscricao[])[Inscricao.Registrado, Inscricao.Cadastrado])
                {
                    if (sorteio.Next(2) == 0)
                    {
                        rodizio = rodizio.ComAVezEm(funcao, inscricao, Matricula.De(sorteio.Next(990, 1070)));
                    }
                }
            }

            var em = escalacao.FechamentoDaHabilitacao.AddMinutes(sorteio.Next(3 * 60));

            return new Cenario(escalacao, requisicoes, trabalhadores, jornadas, rodizio, em);
        }

        private static Trabalhador Trabalhador(int numero, Random sorteio)
        {
            var trabalhador = TrabalhadoresDeTeste.Estivador(
                numero, $"Trabalhador {numero}", sorteio.Next(10) < 7 ? Inscricao.Registrado : Inscricao.Cadastrado);

            if (sorteio.Next(10) < 4)
            {
                trabalhador.Qualificar(Funcoes.Sinaleiro, new DateOnly(2010, 1, 1));
            }

            if (sorteio.Next(10) < 3)
            {
                trabalhador.Qualificar(Funcoes.OperadorDeGuindaste, new DateOnly(2015, 1, 1));
            }

            return trabalhador;
        }

        private static Requisicao Requisicao(Escalacao escalacao, Random sorteio)
        {
            var ternos = Enumerable.Range(0, sorteio.Next(1, 4))
                .Select(_ => (IReadOnlyList<VagasDaFuncao>)[.. Funcoes3.Where(_ => sorteio.Next(2) == 0).DefaultIfEmpty(Funcoes.Estivador).Select(funcao => new VagasDaFuncao(funcao, sorteio.Next(1, 5)))])
                .ToList();

            // A maior parte chega no prazo; algumas, depois dele, ainda antes de o período começar.
            var feitaEm = sorteio.Next(5) == 0
                ? escalacao.PrazoDasRequisicoes.AddMinutes(sorteio.Next(1, 4 * 60))
                : escalacao.PrazoDasRequisicoes.AddMinutes(-sorteio.Next(0, 20 * 60));

            var requisicao = new Requisicao(
                new DadosDaRequisicao(CaisForte, AtlanticoSul, CaisLeste, escalacao.Periodo),
                ternos,
                feitaEm);

            if (sorteio.Next(10) == 0)
            {
                requisicao.Cancelar("Sorteada para cancelar", feitaEm);
            }

            return requisicao;
        }

        private static Periodo Deslocado(Periodo periodo, int distancia)
        {
            for (var passo = 0; passo < Math.Abs(distancia); passo++)
            {
                periodo = distancia > 0 ? periodo.Seguinte : periodo.Anterior;
            }

            return periodo;
        }
    }
}
