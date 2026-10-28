using EscalaAvulsos.Dominio.Comum;
using EscalaAvulsos.Dominio.Identificacao;
using EscalaAvulsos.Dominio.Periodos;

namespace EscalaAvulsos.Dominio.Trabalhadores;

/// <summary>
/// Se o trabalhador pode trabalhar numa função, num período — e, se não pode,
/// todos os motivos, do mais definitivo ao mais passageiro (ADR-0003).
/// </summary>
public sealed class Aptidao
{
    internal Aptidao(Trabalhador trabalhador, Funcao funcao, Periodo periodo, IReadOnlyList<Motivo> motivos)
    {
        Trabalhador = trabalhador.Id;
        Matricula = trabalhador.Matricula;
        Nome = trabalhador.Nome;
        Funcao = funcao;
        Periodo = periodo;
        Motivos = motivos;
    }

    public TrabalhadorId Trabalhador { get; }

    public Matricula Matricula { get; }

    public string Nome { get; }

    public Funcao Funcao { get; }

    public Periodo Periodo { get; }

    /// <summary>Por que ele não pode; vazio, se pode.</summary>
    public IReadOnlyList<Motivo> Motivos { get; }

    public bool Apto => Motivos.Count == 0;

    /// <summary>
    /// "Antônio Ferreira (matrícula 4127) não pode trabalhar como estivador em
    /// 03/12/2026, das 7h às 13h: o exame médico (ASO) vale só até 30/11/2026."
    /// </summary>
    public override string ToString() =>
        Apto
            ? $"{Nome} (matrícula {Matricula}) pode trabalhar como {Funcao.NaFrase} em {Periodo}."
            : $"{Nome} (matrícula {Matricula}) não pode trabalhar como {Funcao.NaFrase} em {Periodo}: {string.Join("; ", Motivos.Select(motivo => motivo.Descricao))}.";
}

/// <summary>Os motivos por que um trabalhador não pode ser escalado.</summary>
public enum TipoDeMotivo
{
    InscricaoCancelada,
    FuncaoDeOutraCategoria,
    SemQualificacao,
    QualificacaoVencida,
    SemAso,
    AsoVencido,
    Ferias,
    Afastamento,
    Suspensao,
}

/// <summary>
/// Um motivo por que o trabalhador não pode trabalhar naquela função, naquele
/// período: o tipo, a frase e os dias em que ele impede.
/// </summary>
/// <remarks>
/// <para>
/// A frase não tem sujeito — "o exame médico (ASO) vale só até 30/11/2026",
/// "está de férias de 04/01/2027 a 02/02/2027" —, para ser juntada a outras
/// depois do nome de quem não pode. Quem fala com o próprio trabalhador, em
/// segunda pessoa, monta a frase dele com o tipo e as datas.
/// </para>
/// <para>
/// <see cref="De"/> e <see cref="Ate"/> são os dias em que o motivo impede,
/// quando se sabe: o exame que valeu até 30/11 impede de 01/12 em diante, sem
/// fim; as férias impedem do primeiro ao último dia. O dia seguinte ao
/// <see cref="Ate"/> é quando o motivo deixa de impedir.
/// </para>
/// </remarks>
public sealed record Motivo
{
    private Motivo(TipoDeMotivo tipo, string descricao, DateOnly? de = null, DateOnly? ate = null)
    {
        Tipo = tipo;
        Descricao = descricao;
        De = de;
        Ate = ate;
    }

    public TipoDeMotivo Tipo { get; }

    public string Descricao { get; }

    /// <summary>O primeiro dia em que o motivo impede, se se sabe.</summary>
    public DateOnly? De { get; }

    /// <summary>O último dia em que o motivo impede, se se sabe e se ele acaba.</summary>
    public DateOnly? Ate { get; }

    public override string ToString() => Descricao;

    internal static Motivo InscricaoCancelada(CancelamentoDaInscricao cancelamento) =>
        new(TipoDeMotivo.InscricaoCancelada,
            $"a inscrição foi cancelada a partir de {FormatoBrasileiro.Data(cancelamento.APartirDe)} ({cancelamento.Motivo})",
            de: cancelamento.APartirDe);

    internal static Motivo FuncaoDeOutraCategoria(Funcao funcao, Categoria categoriaDoTrabalhador) =>
        new(TipoDeMotivo.FuncaoDeOutraCategoria,
            $"{funcao.NaFrase} é função {funcao.Categoria.ComDe}, e não {categoriaDoTrabalhador.ComDe}");

    internal static Motivo SemQualificacao(Funcao funcao) =>
        new(TipoDeMotivo.SemQualificacao, $"não tem a qualificação de {funcao.NaFrase}");

    internal static Motivo QualificacaoAindaNaoVale(Qualificacao qualificacao) =>
        new(TipoDeMotivo.SemQualificacao,
            $"a qualificação de {qualificacao.Funcao.NaFrase} só vale a partir de {FormatoBrasileiro.Data(qualificacao.Desde)}",
            ate: qualificacao.Desde.AddDays(-1));

    internal static Motivo QualificacaoVencida(Qualificacao qualificacao, DateOnly validaAte) =>
        new(TipoDeMotivo.QualificacaoVencida,
            $"a qualificação de {qualificacao.Funcao.NaFrase} vale só até {FormatoBrasileiro.Data(validaAte)}",
            de: validaAte.AddDays(1));

    internal static Motivo SemAso() =>
        new(TipoDeMotivo.SemAso, "não tem exame médico (ASO) registrado");

    internal static Motivo AsoVencido(Aso aso) =>
        new(TipoDeMotivo.AsoVencido,
            $"o exame médico (ASO) vale só até {FormatoBrasileiro.Data(aso.ValidoAte)}",
            de: aso.ValidoAte.AddDays(1));

    internal static Motivo Impedido(Impedimento impedimento) =>
        impedimento.Tipo switch
        {
            TipoDeImpedimento.Ferias => new(TipoDeMotivo.Ferias, $"está de {impedimento}", impedimento.De, impedimento.Ate),
            TipoDeImpedimento.Afastamento => new(TipoDeMotivo.Afastamento, $"está em afastamento {Dias(impedimento)}", impedimento.De, impedimento.Ate),
            _ => new(TipoDeMotivo.Suspensao, $"cumpre suspensão {Dias(impedimento)}", impedimento.De, impedimento.Ate),
        };

    /// <summary>
    /// O motivo gravado com a escala, como foi dito naquela hora: a frase não é
    /// refeita, porque o que a explicava — o exame, as férias — pode ter mudado
    /// desde então (ADR-0011).
    /// </summary>
    internal static Motivo Reconstituir(TipoDeMotivo tipo, string descricao, DateOnly? de, DateOnly? ate)
    {
        if (string.IsNullOrWhiteSpace(descricao))
        {
            throw new ExcecaoDeDominio($"O motivo {tipo} não tem a frase que o explica.");
        }

        if (de is { } primeiro && ate is { } ultimo && ultimo < primeiro)
        {
            throw new ExcecaoDeDominio(
                $"O motivo {tipo} impede de {FormatoBrasileiro.Data(primeiro)} a {FormatoBrasileiro.Data(ultimo)}: o último dia é antes do primeiro.");
        }

        return new(tipo, descricao, de, ate);
    }

    private static string Dias(Impedimento impedimento) =>
        impedimento.Ate is { } ultimo
            ? $"de {FormatoBrasileiro.Data(impedimento.De)} a {FormatoBrasileiro.Data(ultimo)}"
            : $"desde {FormatoBrasileiro.Data(impedimento.De)}";
}
