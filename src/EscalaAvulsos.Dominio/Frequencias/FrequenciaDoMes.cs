using EscalaAvulsos.Dominio.Comum;
using EscalaAvulsos.Dominio.Escalacoes;
using EscalaAvulsos.Dominio.Identificacao;
using EscalaAvulsos.Dominio.Trabalhadores;

namespace EscalaAvulsos.Dominio.Frequencias;

/// <summary>Como está a frequência do mês.</summary>
public enum SituacaoDaFrequencia
{
    /// <summary>O mínimo de habilitações foi cumprido, e as faltas estão dentro do limite.</summary>
    Cumprida,

    /// <summary>O mês ainda está aberto, faltam habilitações, e ainda dá tempo.</summary>
    EmAndamento,

    /// <summary>O mês acabou abaixo do mínimo, ou as faltas passaram do limite.</summary>
    Descumprida,
}

/// <summary>
/// A frequência de um trabalhador num mês: quantas vezes ele se habilitou,
/// trabalhou, faltou e desistiu — e quanto falta para o mínimo, a tempo de ele
/// não ser punido (ADR-0009).
/// </summary>
/// <remarks>
/// O mínimo diminui na proporção dos dias em que o trabalhador não podia
/// trabalhar — férias, afastamento, suspensão, inscrição cancelada —, e é
/// arredondado para baixo, a favor dele.
/// </remarks>
public sealed class FrequenciaDoMes
{
    private FrequenciaDoMes(Trabalhador trabalhador, Mes mes)
    {
        Trabalhador = trabalhador.Id;
        Matricula = trabalhador.Matricula;
        Nome = trabalhador.Nome;
        Mes = mes;
    }

    public TrabalhadorId Trabalhador { get; }

    public Matricula Matricula { get; }

    public string Nome { get; }

    public Mes Mes { get; }

    /// <summary>Em quantas escalações do mês ele se habilitou.</summary>
    public int Habilitacoes { get; private init; }

    /// <summary>Quantos períodos ele trabalhou: na escala, com a presença confirmada.</summary>
    public int Trabalhados { get; private init; }

    /// <summary>As faltas sem justificativa.</summary>
    public int Faltas { get; private init; }

    public int FaltasJustificadas { get; private init; }

    public int Desistencias { get; private init; }

    /// <summary>Os dias do mês em que ele não podia trabalhar.</summary>
    public int DiasImpedidos { get; private init; }

    /// <summary>As habilitações exigidas no mês, já descontados os dias impedidos.</summary>
    public int MinimoDeHabilitacoes { get; private init; }

    public int MaximoDeFaltas { get; private init; }

    /// <summary>Se o mês já acabou.</summary>
    public bool MesEncerrado { get; private init; }

    public int HabilitacoesQueFaltam => Math.Max(0, MinimoDeHabilitacoes - Habilitacoes);

    public bool FaltasAcimaDoLimite => Faltas > MaximoDeFaltas;

    public SituacaoDaFrequencia Situacao =>
        FaltasAcimaDoLimite || (MesEncerrado && HabilitacoesQueFaltam > 0) ? SituacaoDaFrequencia.Descumprida
        : HabilitacoesQueFaltam > 0 ? SituacaoDaFrequencia.EmAndamento
        : SituacaoDaFrequencia.Cumprida;

    /// <summary>
    /// A frequência do trabalhador no mês, contada nas escalações e nas escalas
    /// dele. As de outros meses e de outros trabalhadores são ignoradas.
    /// </summary>
    /// <param name="hoje">O dia de hoje, para saber se o mês já acabou.</param>
    public static FrequenciaDoMes De(
        Trabalhador trabalhador,
        Mes mes,
        IEnumerable<Escalacao> escalacoes,
        IEnumerable<Escala> escalas,
        RegraDeFrequencia regra,
        DateOnly hoje)
    {
        ArgumentNullException.ThrowIfNull(trabalhador);
        ArgumentNullException.ThrowIfNull(mes);
        ArgumentNullException.ThrowIfNull(escalacoes);
        ArgumentNullException.ThrowIfNull(escalas);
        ArgumentNullException.ThrowIfNull(regra);

        var situacoes = escalas
            .Where(escala => Mes.De(escala.Periodo.Dia) == mes)
            .Select(escala => escala.Situacoes.SingleOrDefault(situacao => situacao.Trabalhador == trabalhador.Id))
            .OfType<SituacaoNaEscala>()
            .ToList();

        var diasDoMes = mes.UltimoDia.Day;
        var diasImpedidos = Enumerable.Range(0, diasDoMes).Select(mes.PrimeiroDia.AddDays).Count(dia => Impedido(trabalhador, dia));

        return new FrequenciaDoMes(trabalhador, mes)
        {
            Habilitacoes = escalacoes.Count(escalacao => Mes.De(escalacao.Periodo.Dia) == mes && escalacao.HabilitacaoDe(trabalhador.Id) is not null),
            Trabalhados = situacoes.Count(situacao => situacao.Escalado && situacao.Comparecimento == Comparecimento.Presente),
            Faltas = situacoes.Count(situacao => situacao.Escalado && situacao.Comparecimento == Comparecimento.Falta),
            FaltasJustificadas = situacoes.Count(situacao => situacao.Escalado && situacao.Comparecimento == Comparecimento.FaltaJustificada),
            Desistencias = situacoes.Count(situacao => situacao.VagaDesistida is not null),
            DiasImpedidos = diasImpedidos,
            MinimoDeHabilitacoes = regra.MinimoDeHabilitacoes * (diasDoMes - diasImpedidos) / diasDoMes,
            MaximoDeFaltas = regra.MaximoDeFaltas,
            MesEncerrado = hoje > mes.UltimoDia,
        };
    }

    /// <summary>
    /// "Antônio Ferreira (matrícula 4127), em 11/2026: 9 habilitações, e o
    /// mínimo é 12 — faltam 3 até 30/11/2026. Trabalhou 6 períodos; 1 falta sem
    /// justificativa, de 2 aceitas."
    /// </summary>
    public override string ToString()
    {
        var verbo = MesEncerrado ? "era" : "é";
        var diasPossiveis = Mes.UltimoDia.Day - DiasImpedidos;
        var minimo = DiasImpedidos > 0
            ? $"o mínimo {verbo} {MinimoDeHabilitacoes}, pelos {Contar(diasPossiveis, "dia", "dias")} em que podia trabalhar"
            : $"o mínimo {verbo} {MinimoDeHabilitacoes}";
        var falta = HabilitacoesQueFaltam == 0 ? string.Empty
            : MesEncerrado ? $" — ficaram faltando {HabilitacoesQueFaltam}"
            : $" — faltam {HabilitacoesQueFaltam} até {FormatoBrasileiro.Data(Mes.UltimoDia)}";

        var trabalho = new List<string>
        {
            $"Trabalhou {Contar(Trabalhados, "período", "períodos")}",
            $"{Contar(Faltas, "falta", "faltas")} sem justificativa, de {Contar(MaximoDeFaltas, "aceita", "aceitas")}{(FaltasAcimaDoLimite ? " — acima do limite" : string.Empty)}",
        };

        if (FaltasJustificadas > 0)
        {
            trabalho.Add(Contar(FaltasJustificadas, "falta justificada", "faltas justificadas"));
        }

        if (Desistencias > 0)
        {
            trabalho.Add(Contar(Desistencias, "desistência", "desistências"));
        }

        return $"{Nome} (matrícula {Matricula}), em {Mes}: {Contar(Habilitacoes, "habilitação", "habilitações")}, e {minimo}{falta}. {string.Join("; ", trabalho)}.";
    }

    private static bool Impedido(Trabalhador trabalhador, DateOnly dia) =>
        trabalhador.Impedimentos.Any(impedimento => impedimento.VigenteEm(dia))
        || trabalhador.Cancelamento is { } cancelamento && cancelamento.APartirDe <= dia;

    private static string Contar(int quantidade, string singular, string plural) => $"{quantidade} {(quantidade == 1 ? singular : plural)}";
}
