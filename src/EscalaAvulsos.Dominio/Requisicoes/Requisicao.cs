using EscalaAvulsos.Dominio.Comum;
using EscalaAvulsos.Dominio.Periodos;
using EscalaAvulsos.Dominio.Trabalhadores;

namespace EscalaAvulsos.Dominio.Requisicoes;

/// <summary>Em que pé está a requisição.</summary>
public enum SituacaoDaRequisicao
{
    /// <summary>Esperando a escala do período.</summary>
    Aberta,

    /// <summary>Cancelada pelo operador antes de o período começar.</summary>
    Cancelada,
}

/// <summary>O que o operador informa ao pedir a mão de obra, além dos ternos.</summary>
/// <param name="Operador">Quem pede, e paga.</param>
/// <param name="Navio">Em que navio.</param>
/// <param name="Local">Onde o navio está atracado.</param>
/// <param name="Periodo">Em que período de seis horas.</param>
public sealed record DadosDaRequisicao(OperadorPortuario Operador, Navio Navio, LocalDaOperacao Local, Periodo Periodo);

/// <summary>
/// O pedido de mão de obra do operador portuário ao órgão gestor: para um
/// navio, num local, num período — e quantos ternos, com quantas vagas de cada
/// função.
/// </summary>
/// <remarks>
/// <para>
/// A requisição é de uma categoria só: cada categoria tem o seu rodízio, e a
/// escala de cada uma é feita à parte. O operador que precisa de estiva e de
/// conferência faz duas (ADR-0004).
/// </para>
/// <para>
/// A composição dos ternos vem da convenção coletiva e da operação; aqui ela
/// é informada terno a terno e conferida — terno sem vaga, quantidade zero,
/// função repetida —, sem que o sistema decida quantos estivadores um porão
/// pede.
/// </para>
/// </remarks>
public sealed class Requisicao
{
    public const int TamanhoMaximoDoMotivo = 200;

    private readonly List<Terno> _ternos;

    /// <param name="ternos">As vagas de cada terno, na ordem: o primeiro da lista é o terno 1.</param>
    /// <param name="feitaEm">Quando o operador fez o pedido; tem de ser antes de o período começar.</param>
    public Requisicao(DadosDaRequisicao dados, IReadOnlyList<IReadOnlyList<VagasDaFuncao>> ternos, DateTimeOffset feitaEm)
    {
        ArgumentNullException.ThrowIfNull(dados);
        ArgumentNullException.ThrowIfNull(dados.Operador);
        ArgumentNullException.ThrowIfNull(dados.Navio);
        ArgumentNullException.ThrowIfNull(dados.Local);
        ArgumentNullException.ThrowIfNull(dados.Periodo);
        ArgumentNullException.ThrowIfNull(ternos);

        var problemas = new List<string>();

        if (feitaEm >= dados.Periodo.Inicio)
        {
            problemas.Add($"A requisição tem de chegar antes do período, e o de {dados.Periodo}, já começou.");
        }

        if (ternos.Count == 0)
        {
            problemas.Add("A requisição precisa de pelo menos um terno.");
        }

        for (var indice = 0; indice < ternos.Count; indice++)
        {
            problemas.AddRange(ProblemasDoTerno(indice + 1, ternos[indice]));
        }

        var categorias = ternos.SelectMany(vagas => vagas).Select(vaga => vaga.Funcao.Categoria).Distinct().ToList();

        if (categorias.Count > 1)
        {
            problemas.Add(
                $"A requisição mistura {Enumerar(categorias.Select(categoria => categoria.ComArtigo))}: cada categoria tem o seu rodízio, e a sua requisição.");
        }

        if (problemas.Count > 0)
        {
            throw new ExcecaoDeDominio(string.Join(" ", problemas));
        }

        Id = RequisicaoId.Novo();
        Operador = dados.Operador;
        Navio = dados.Navio;
        Local = dados.Local;
        Periodo = dados.Periodo;
        Categoria = categorias[0];
        FeitaEm = feitaEm;
        _ternos = [.. ternos.Select((vagas, indice) => new Terno(indice + 1, [.. vagas]))];
    }

    public RequisicaoId Id { get; }

    public OperadorPortuario Operador { get; }

    public Navio Navio { get; }

    public LocalDaOperacao Local { get; }

    public Periodo Periodo { get; }

    /// <summary>A categoria de todas as funções pedidas: é o rodízio de onde a escala vai tirar os trabalhadores.</summary>
    public Categoria Categoria { get; }

    public IReadOnlyList<Terno> Ternos => _ternos.AsReadOnly();

    public DateTimeOffset FeitaEm { get; }

    public SituacaoDaRequisicao Situacao { get; private set; }

    public DateTimeOffset? CanceladaEm { get; private set; }

    public string? MotivoDoCancelamento { get; private set; }

    public int TotalDeVagas => _ternos.Sum(terno => terno.TotalDeVagas);

    /// <summary>
    /// As vagas de cada função, somadas em todos os ternos, na ordem em que a
    /// função aparece primeiro: é o que a escala do período vai preencher.
    /// </summary>
    public IReadOnlyList<VagasDaFuncao> VagasPorFuncao =>
    [
        .. _ternos
            .SelectMany(terno => terno.Vagas)
            .GroupBy(vaga => vaga.Funcao)
            .Select(funcao => new VagasDaFuncao(funcao.Key, funcao.Sum(vaga => vaga.Quantidade))),
    ];

    /// <summary>Cancela a requisição, com o motivo, antes de o período começar.</summary>
    public void Cancelar(string motivo, DateTimeOffset em)
    {
        if (Situacao == SituacaoDaRequisicao.Cancelada)
        {
            throw new ExcecaoDeDominio($"A requisição já foi cancelada: {MotivoDoCancelamento}.");
        }

        if (em >= Periodo.Inicio)
        {
            throw new ExcecaoDeDominio($"O período de {Periodo}, já começou: a requisição não pode mais ser cancelada.");
        }

        if (Textos.Problema(motivo, "o motivo do cancelamento", TamanhoMaximoDoMotivo) is { } problema)
        {
            throw new ExcecaoDeDominio(problema);
        }

        Situacao = SituacaoDaRequisicao.Cancelada;
        CanceladaEm = em;
        MotivoDoCancelamento = motivo.Trim();
    }

    /// <summary>
    /// "Cais Forte Operações Portuárias pede 2 ternos da estiva, 22 vagas, para
    /// o navio Atlântico Sul (IMO 9876543), no Terminal Cais Leste, berço 4, em
    /// 05/11/2026, das 7h às 13h."
    /// </summary>
    public override string ToString() =>
        $"{Operador.Nome} pede {Contar(_ternos.Count, "terno", "ternos")} {Categoria.ComDe}, {Contar(TotalDeVagas, "vaga", "vagas")}, "
        + $"para o navio {Navio}, no {Local}, em {Periodo}.";

    private static IEnumerable<string> ProblemasDoTerno(int numero, IReadOnlyList<VagasDaFuncao> vagas)
    {
        if (vagas.Count == 0)
        {
            yield return $"O terno {numero} não tem vaga nenhuma.";
            yield break;
        }

        foreach (var vaga in vagas.Where(vaga => vaga.Quantidade < 1))
        {
            yield return $"No terno {numero}, as vagas de {vaga.Funcao.NaFrase} têm de ser pelo menos 1, e não {vaga.Quantidade}.";
        }

        foreach (var repetida in vagas.GroupBy(vaga => vaga.Funcao).Where(funcao => funcao.Count() > 1))
        {
            yield return $"O terno {numero} repete {repetida.Key.NaFrase}: junte as vagas da função numa linha só.";
        }
    }

    /// <summary>"a estiva e o bloco", "a estiva, a capatazia e o bloco".</summary>
    private static string Enumerar(IEnumerable<string> itens)
    {
        var lista = itens.ToList();

        return lista.Count == 1 ? lista[0] : $"{string.Join(", ", lista.Take(lista.Count - 1))} e {lista[^1]}";
    }

    private static string Contar(int quantidade, string singular, string plural) =>
        $"{quantidade} {(quantidade == 1 ? singular : plural)}";
}
