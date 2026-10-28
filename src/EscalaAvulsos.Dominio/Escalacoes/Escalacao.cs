using EscalaAvulsos.Dominio.Comum;
using EscalaAvulsos.Dominio.Periodos;
using EscalaAvulsos.Dominio.Trabalhadores;

namespace EscalaAvulsos.Dominio.Escalacoes;

/// <summary>
/// A escalação de uma categoria para um período: o prazo das requisições, a
/// janela de habilitação e quem se habilitou, em que funções.
/// </summary>
/// <remarks>
/// <para>
/// Dentro da janela, o trabalhador se habilita, troca as funções ou desiste,
/// quantas vezes quiser. A hora em que ele se habilita não muda nada: a vez é
/// pela matrícula, no rodízio (ADR-0005).
/// </para>
/// <para>
/// Quem não pode trabalhar numa função fica sabendo na hora, com o motivo, e
/// não depois de a escala sair sem o nome dele.
/// </para>
/// </remarks>
public sealed class Escalacao
{
    private readonly List<Habilitacao> _habilitacoes = [];

    public Escalacao(Categoria categoria, Periodo periodo, CalendarioDaEscalacao calendario)
    {
        ArgumentNullException.ThrowIfNull(periodo);
        ArgumentNullException.ThrowIfNull(calendario);

        if (!Enum.IsDefined(categoria))
        {
            throw new ExcecaoDeDominio($"A categoria {(int)categoria} não existe.");
        }

        Id = EscalacaoId.Novo();
        Categoria = categoria;
        Periodo = periodo;
        Calendario = calendario;
    }

    private Escalacao(EscalacaoId id, Categoria categoria, Periodo periodo, CalendarioDaEscalacao calendario)
        : this(categoria, periodo, calendario)
    {
        ArgumentNullException.ThrowIfNull(id);

        Id = id;
    }

    public EscalacaoId Id { get; }

    public Categoria Categoria { get; }

    public Periodo Periodo { get; }

    public CalendarioDaEscalacao Calendario { get; }

    /// <summary>O último instante em que uma requisição entra nesta escala.</summary>
    public DateTimeOffset PrazoDasRequisicoes => Periodo.Inicio - Calendario.AntecedenciaDasRequisicoes;

    public DateTimeOffset AberturaDaHabilitacao => Periodo.Inicio - Calendario.AntecedenciaDaAbertura;

    /// <summary>Quando a habilitação fecha: dali em diante, a escala pode ser feita.</summary>
    public DateTimeOffset FechamentoDaHabilitacao => Periodo.Inicio - Calendario.AntecedenciaDoFechamento;

    /// <summary>Quem se habilitou, na ordem das matrículas: a ordem em que chegaram não muda nada.</summary>
    public IReadOnlyList<Habilitacao> Habilitacoes => [.. _habilitacoes.OrderBy(habilitacao => habilitacao.Matricula)];

    /// <summary>Se a habilitação está aberta no instante: da abertura, incluída, ao fechamento.</summary>
    public bool HabilitacaoAbertaEm(DateTimeOffset instante) => AberturaDaHabilitacao <= instante && instante < FechamentoDaHabilitacao;

    public Habilitacao? HabilitacaoDe(TrabalhadorId trabalhador) =>
        _habilitacoes.SingleOrDefault(habilitacao => habilitacao.Trabalhador == trabalhador);

    /// <summary>
    /// Habilita o trabalhador nas funções escolhidas — ou troca as funções de
    /// quem já estava habilitado. Ele tem de poder trabalhar em todas, no
    /// período; se não pode em alguma, a habilitação é recusada com os motivos.
    /// </summary>
    public Habilitacao Habilitar(Trabalhador trabalhador, IReadOnlyCollection<Funcao> funcoes, DateTimeOffset em)
    {
        ArgumentNullException.ThrowIfNull(trabalhador);
        ArgumentNullException.ThrowIfNull(funcoes);
        ExigirHabilitacaoAberta(em);

        if (trabalhador.Categoria != Categoria)
        {
            throw new ExcecaoDeDominio($"{trabalhador} é {trabalhador.Categoria.ComDe}, e esta é a {this}.");
        }

        var escolhidas = funcoes.Distinct().ToList();

        if (escolhidas.Count == 0)
        {
            throw new ExcecaoDeDominio("Escolha pelo menos uma função para se habilitar.");
        }

        var recusadas = escolhidas.Select(funcao => trabalhador.AptidaoPara(funcao, Periodo)).Where(aptidao => !aptidao.Apto).ToList();

        if (recusadas.Count > 0)
        {
            var motivos = recusadas.SelectMany(aptidao => aptidao.Motivos).Distinct().Select(motivo => motivo.Descricao);

            throw new ExcecaoDeDominio(
                $"{trabalhador} não pode se habilitar {Como(recusadas.Select(aptidao => aptidao.Funcao))}: {string.Join("; ", motivos)}.");
        }

        var habilitacao = new Habilitacao(trabalhador.Id, trabalhador.Matricula, escolhidas, em);

        _habilitacoes.RemoveAll(existente => existente.Trabalhador == trabalhador.Id);
        _habilitacoes.Add(habilitacao);

        return habilitacao;
    }

    /// <summary>Tira a habilitação do trabalhador, enquanto a janela está aberta.</summary>
    public void Desistir(Trabalhador trabalhador, DateTimeOffset em)
    {
        ArgumentNullException.ThrowIfNull(trabalhador);

        if (em >= FechamentoDaHabilitacao)
        {
            throw new ExcecaoDeDominio(
                $"A habilitação para a {this}, fechou em {FormatoBrasileiro.Instante(FechamentoDaHabilitacao)}: não dá mais para desistir.");
        }

        if (_habilitacoes.RemoveAll(habilitacao => habilitacao.Trabalhador == trabalhador.Id) == 0)
        {
            throw new ExcecaoDeDominio($"{trabalhador} não tem habilitação para a {this}.");
        }
    }

    /// <summary>"escala da estiva de 05/11/2026, das 13h às 19h".</summary>
    public override string ToString() => $"escala {Categoria.ComDe} de {Periodo}";

    /// <summary>
    /// Refaz a escalação gravada. As habilitações não passam de novo pela
    /// aptidão — valiam quando foram feitas, e o trabalhador de hoje pode não
    /// ser o daquele dia —, mas passam pelo resto: a janela, ao menos uma
    /// função, sem repetir, e uma habilitação por trabalhador (ADR-0011).
    /// </summary>
    /// <remarks>As funções chegam da categoria da escalação: é com ela que a gravação as refaz.</remarks>
    internal static Escalacao Reconstituir(
        EscalacaoId id,
        Categoria categoria,
        Periodo periodo,
        CalendarioDaEscalacao calendario,
        IReadOnlyList<Habilitacao> habilitacoes)
    {
        ArgumentNullException.ThrowIfNull(habilitacoes);

        var escalacao = new Escalacao(id, categoria, periodo, calendario);
        var problemas = new List<string>();

        foreach (var repetido in habilitacoes.GroupBy(habilitacao => habilitacao.Trabalhador).Where(grupo => grupo.Count() > 1))
        {
            problemas.Add($"A matrícula {repetido.First().Matricula} tem {repetido.Count()} habilitações na {escalacao}.");
        }

        foreach (var repetida in habilitacoes.DistinctBy(habilitacao => habilitacao.Trabalhador).GroupBy(habilitacao => habilitacao.Matricula).Where(grupo => grupo.Count() > 1))
        {
            problemas.Add($"A matrícula {repetida.Key} é de {repetida.Count()} trabalhadores habilitados.");
        }

        foreach (var habilitacao in habilitacoes)
        {
            var daMatricula = $"A habilitação da matrícula {habilitacao.Matricula}";

            if (!escalacao.HabilitacaoAbertaEm(habilitacao.Em))
            {
                problemas.Add(
                    $"{daMatricula} foi feita em {FormatoBrasileiro.Instante(habilitacao.Em)}, fora da janela, que abre em {FormatoBrasileiro.Instante(escalacao.AberturaDaHabilitacao)}, e fecha em {FormatoBrasileiro.Instante(escalacao.FechamentoDaHabilitacao)}.");
            }

            if (habilitacao.Funcoes.Count == 0)
            {
                problemas.Add($"{daMatricula} não tem função nenhuma.");
            }

            foreach (var repetida in habilitacao.Funcoes.GroupBy(funcao => funcao).Where(grupo => grupo.Count() > 1))
            {
                problemas.Add($"{daMatricula} repete {repetida.Key.NaFrase}.");
            }
        }

        if (problemas.Count > 0)
        {
            throw new ExcecaoDeDominio(string.Join(" ", problemas));
        }

        escalacao._habilitacoes.AddRange(habilitacoes);

        return escalacao;
    }

    private void ExigirHabilitacaoAberta(DateTimeOffset em)
    {
        if (em < AberturaDaHabilitacao)
        {
            throw new ExcecaoDeDominio($"A habilitação para a {this}, abre em {FormatoBrasileiro.Instante(AberturaDaHabilitacao)}.");
        }

        if (em >= FechamentoDaHabilitacao)
        {
            throw new ExcecaoDeDominio($"A habilitação para a {this}, fechou em {FormatoBrasileiro.Instante(FechamentoDaHabilitacao)}.");
        }
    }

    /// <summary>"como sinaleiro", "como sinaleiro nem como estivador", "como sinaleiro, estivador nem contramestre de terno".</summary>
    private static string Como(IEnumerable<Funcao> funcoes)
    {
        var nomes = funcoes.Select(funcao => funcao.NaFrase).ToList();

        return nomes.Count switch
        {
            1 => $"como {nomes[0]}",
            2 => $"como {nomes[0]} nem como {nomes[1]}",
            _ => $"como {string.Join(", ", nomes.Take(nomes.Count - 1))} nem {nomes[^1]}",
        };
    }
}
