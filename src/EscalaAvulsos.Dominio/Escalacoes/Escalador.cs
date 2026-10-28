using EscalaAvulsos.Dominio.Comum;
using EscalaAvulsos.Dominio.Requisicoes;
using EscalaAvulsos.Dominio.Trabalhadores;

namespace EscalaAvulsos.Dominio.Escalacoes;

/// <summary>
/// Faz a escala de uma escalação: põe os habilitados nas vagas das
/// requisições, cada um na sua vez do rodízio, os registrados antes dos
/// cadastrados, conferindo a aptidão e o descanso — e diz a cada habilitado
/// como ele ficou (ADR-0007).
/// </summary>
/// <remarks>
/// <para>
/// As funções são escaladas da que tem menos habilitados para a que tem mais:
/// o sinaleiro, que também é estivador, pega primeiro a vaga que poucos podem
/// pegar. Quem pega uma vaga sai das outras funções.
/// </para>
/// <para>
/// É uma conta, e não um processo: as mesmas entradas dão a mesma escala, e
/// nenhuma delas muda. O rodízio andado sai na escala.
/// </para>
/// </remarks>
public static class Escalador
{
    /// <param name="requisicoes">
    /// As requisições da categoria e do período da escalação. As canceladas não
    /// entram; as que chegaram depois do prazo ficam de fora, e a escala as lista.
    /// </param>
    /// <param name="trabalhadores">Os trabalhadores habilitados — todos eles; os outros são ignorados.</param>
    /// <param name="jornadas">Os períodos em que os habilitados já estão escalados, em outras escalas.</param>
    /// <param name="rodizio">Onde o rodízio da categoria parou na escala anterior.</param>
    /// <param name="em">Quando a escala é feita: depois de a habilitação fechar, antes de o período começar.</param>
    public static Escala Escalar(
        Escalacao escalacao,
        IReadOnlyCollection<Requisicao> requisicoes,
        IReadOnlyCollection<Trabalhador> trabalhadores,
        IReadOnlyCollection<Jornada> jornadas,
        Rodizio rodizio,
        Descanso descanso,
        DateTimeOffset em)
    {
        ArgumentNullException.ThrowIfNull(escalacao);
        ArgumentNullException.ThrowIfNull(requisicoes);
        ArgumentNullException.ThrowIfNull(trabalhadores);
        ArgumentNullException.ThrowIfNull(jornadas);
        ArgumentNullException.ThrowIfNull(rodizio);
        ArgumentNullException.ThrowIfNull(descanso);

        if (em < escalacao.FechamentoDaHabilitacao)
        {
            throw new ExcecaoDeDominio(
                $"A habilitação para a {escalacao}, só fecha em {FormatoBrasileiro.Instante(escalacao.FechamentoDaHabilitacao)}: a escala é feita depois.");
        }

        if (em >= escalacao.Periodo.Inicio)
        {
            throw new ExcecaoDeDominio($"O período de {escalacao.Periodo}, já começou: a escala é feita antes dele.");
        }

        if (requisicoes.FirstOrDefault(requisicao => requisicao.Categoria != escalacao.Categoria || requisicao.Periodo != escalacao.Periodo) is { } deOutraEscala)
        {
            throw new ExcecaoDeDominio(
                $"A requisição do navio {deOutraEscala.Navio} é {deOutraEscala.Categoria.ComDe}, para {deOutraEscala.Periodo}, e não entra na {escalacao}.");
        }

        var abertas = requisicoes
            .Where(requisicao => requisicao.Situacao == SituacaoDaRequisicao.Aberta)
            .OrderBy(requisicao => requisicao.FeitaEm)
            .ThenBy(requisicao => requisicao.Id.Valor)
            .ToList();
        var foraDoPrazo = abertas.Where(requisicao => requisicao.FeitaEm > escalacao.PrazoDasRequisicoes).ToList();
        var vagas = Vagas(abertas.Except(foraDoPrazo));

        var habilitados = Habilitados(escalacao, trabalhadores);
        var jornadasDe = jornadas.ToLookup(jornada => jornada.Trabalhador, jornada => jornada.Periodo);
        var descansoDe = habilitados.ToDictionary(
            habilitado => habilitado.Trabalhador.Id,
            habilitado => descanso.ConflitoCom(escalacao.Periodo, jornadasDe[habilitado.Trabalhador.Id]));

        var engajamentos = new Dictionary<TrabalhadorId, Engajamento>();
        var motivos = habilitados.ToDictionary(habilitado => habilitado.Trabalhador.Id, _ => new List<MotivoNaEscala>());
        var rodizioDepois = rodizio;

        // A função com menos habilitados primeiro; no empate, a que aparece antes nas requisições.
        var funcoes = vagas
            .Select(vaga => vaga.Funcao)
            .Distinct()
            .OrderBy(funcao => habilitados.Count(habilitado => habilitado.Habilitacao.Funcoes.Contains(funcao)))
            .ToList();

        foreach (var funcao in funcoes)
        {
            var fila = new Queue<Vaga>(vagas.Where(vaga => vaga.Funcao == funcao));

            foreach (var inscricao in (Inscricao[])[Inscricao.Registrado, Inscricao.Cadastrado])
            {
                var candidatos = habilitados
                    .Where(habilitado => habilitado.Trabalhador.Inscricao == inscricao && habilitado.Habilitacao.Funcoes.Contains(funcao))
                    .ToDictionary(habilitado => habilitado.Trabalhador.Matricula, habilitado => habilitado.Trabalhador);
                var semVagaNoComeco = fila.Count == 0;
                var posicao = 0;
                var posicaoDoUltimoEscalado = 0;

                foreach (var matricula in rodizio.NaOrdemDaVez(funcao, inscricao, candidatos.Keys))
                {
                    posicao++;
                    var trabalhador = candidatos[matricula];

                    // Quem já pegou vaga, ou precisa de descanso, a roda pula; o descanso é dito uma vez só, para todas as funções.
                    if (engajamentos.ContainsKey(trabalhador.Id) || descansoDe[trabalhador.Id] is not null)
                    {
                        continue;
                    }

                    var aptidao = trabalhador.AptidaoPara(funcao, escalacao.Periodo);

                    if (!aptidao.Apto)
                    {
                        motivos[trabalhador.Id].Add(MotivoNaEscala.NaoApto(funcao, aptidao.Motivos));
                    }
                    else if (fila.Count == 0)
                    {
                        motivos[trabalhador.Id].Add(
                            semVagaNoComeco ? MotivoNaEscala.VagasComRegistrados(funcao) : MotivoNaEscala.NaoAlcancado(funcao, posicao - posicaoDoUltimoEscalado));
                    }
                    else
                    {
                        var vaga = fila.Dequeue();
                        vaga.Engajamento = new Engajamento(trabalhador, funcao, vaga.Requisicao, vaga.Terno);
                        engajamentos[trabalhador.Id] = vaga.Engajamento;
                        posicaoDoUltimoEscalado = posicao;
                        rodizioDepois = rodizioDepois.ComAVezEm(funcao, inscricao, matricula);
                    }
                }
            }
        }

        foreach (var (habilitacao, trabalhador) in habilitados)
        {
            motivos[trabalhador.Id].AddRange(habilitacao.Funcoes.Where(funcao => !funcoes.Contains(funcao)).Select(MotivoNaEscala.SemVagaNaFuncao));
        }

        var situacoes = habilitados
            .Select(habilitado => new SituacaoNaEscala(
                habilitado.Trabalhador,
                habilitado.Habilitacao,
                engajamentos.GetValueOrDefault(habilitado.Trabalhador.Id),
                descansoDe[habilitado.Trabalhador.Id] is { } conflito ? [MotivoNaEscala.EmDescanso(conflito)] : motivos[habilitado.Trabalhador.Id]))
            .ToList();

        return new Escala(
            escalacao,
            em,
            [.. vagas.Select(vaga => vaga.Engajamento).OfType<Engajamento>()],
            NaoPreenchidas(vagas),
            situacoes,
            [.. foraDoPrazo.Select(requisicao => requisicao.Id)],
            rodizioDepois);
    }

    /// <summary>As vagas, uma a uma, na ordem das requisições, dos ternos e da composição de cada terno.</summary>
    private static List<Vaga> Vagas(IEnumerable<Requisicao> requisicoes) =>
    [
        .. from requisicao in requisicoes
           from terno in requisicao.Ternos
           from vagasDaFuncao in terno.Vagas
           from _ in Enumerable.Range(0, vagasDaFuncao.Quantidade)
           select new Vaga(requisicao, terno.Numero, vagasDaFuncao.Funcao),
    ];

    /// <summary>Os habilitados, na ordem das matrículas, com o trabalhador de cada um.</summary>
    private static List<(Habilitacao Habilitacao, Trabalhador Trabalhador)> Habilitados(Escalacao escalacao, IReadOnlyCollection<Trabalhador> trabalhadores)
    {
        var porId = trabalhadores.DistinctBy(trabalhador => trabalhador.Id).ToDictionary(trabalhador => trabalhador.Id);

        return
        [
            .. escalacao.Habilitacoes.Select(habilitacao => porId.TryGetValue(habilitacao.Trabalhador, out var trabalhador)
                ? (habilitacao, trabalhador)
                : throw new ExcecaoDeDominio($"Falta o trabalhador da matrícula {habilitacao.Matricula}, que se habilitou para a {escalacao}.")),
        ];
    }

    /// <summary>As vagas vazias, somadas por requisição, terno e função, na ordem em que aparecem.</summary>
    private static List<VagaNaoPreenchida> NaoPreenchidas(List<Vaga> vagas) =>
    [
        .. vagas
            .Where(vaga => vaga.Engajamento is null)
            .GroupBy(vaga => (vaga.Requisicao, vaga.Terno, vaga.Funcao))
            .Select(grupo => new VagaNaoPreenchida(grupo.Key.Requisicao, grupo.Key.Terno, grupo.Key.Funcao, grupo.Count())),
    ];

    /// <summary>Uma vaga de uma função num terno, e quem a pegou.</summary>
    private sealed class Vaga(Requisicao requisicao, int terno, Funcao funcao)
    {
        public Requisicao Requisicao { get; } = requisicao;

        public int Terno { get; } = terno;

        public Funcao Funcao { get; } = funcao;

        public Engajamento? Engajamento { get; set; }
    }
}
