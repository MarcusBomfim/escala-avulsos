using EscalaAvulsos.Dominio.Escalacoes;
using EscalaAvulsos.Dominio.Identificacao;
using EscalaAvulsos.Dominio.Trabalhadores;

namespace EscalaAvulsos.Infraestrutura.Documentos;

/// <summary>
/// Como uma escalação é gravada: a categoria, o período, o calendário com que
/// ela foi aberta, e quem se habilitou, em que funções e quando.
/// </summary>
/// <remarks>
/// O calendário vai junto, e não só o nome de um padrão: se o porto mudar os
/// horários, as escalações já abertas continuam com os de quando abriram.
/// </remarks>
internal sealed record DocumentoDaEscalacao
{
    public const short Formato = 1;

    public required Categoria Categoria { get; init; }

    public required PeriodoGravado Periodo { get; init; }

    public required CalendarioGravado Calendario { get; init; }

    /// <summary>Na ordem das matrículas.</summary>
    public required IReadOnlyList<HabilitacaoGravada> Habilitacoes { get; init; }

    public static DocumentoDaEscalacao De(Escalacao escalacao) => new()
    {
        Categoria = escalacao.Categoria,
        Periodo = PeriodoGravado.De(escalacao.Periodo),
        Calendario = new CalendarioGravado
        {
            AntecedenciaDasRequisicoes = escalacao.Calendario.AntecedenciaDasRequisicoes,
            AntecedenciaDaAbertura = escalacao.Calendario.AntecedenciaDaAbertura,
            AntecedenciaDoFechamento = escalacao.Calendario.AntecedenciaDoFechamento,
        },
        Habilitacoes = [.. escalacao.Habilitacoes.Select(HabilitacaoGravada.De)],
    };

    /// <remarks>As habilitações não passam de novo pela aptidão; o resto das regras, sim (ADR-0011).</remarks>
    public Escalacao ParaEscalacao(EscalacaoId id) =>
        Escalacao.Reconstituir(
            id,
            Categoria,
            Periodo.ParaPeriodo(),
            new CalendarioDaEscalacao(Calendario.AntecedenciaDasRequisicoes, Calendario.AntecedenciaDaAbertura, Calendario.AntecedenciaDoFechamento),
            [.. Habilitacoes.Select(habilitacao => habilitacao.ParaHabilitacao(Categoria))]);
}

/// <summary>As antecedências, contadas para trás a partir do início do período, como <c>"04:00:00"</c>.</summary>
internal sealed record CalendarioGravado
{
    public required TimeSpan AntecedenciaDasRequisicoes { get; init; }

    public required TimeSpan AntecedenciaDaAbertura { get; init; }

    public required TimeSpan AntecedenciaDoFechamento { get; init; }
}

internal sealed record HabilitacaoGravada
{
    public required Guid Trabalhador { get; init; }

    public required int Matricula { get; init; }

    /// <summary>Na ordem em que o trabalhador escolheu.</summary>
    public required IReadOnlyList<string> Funcoes { get; init; }

    public required DateTimeOffset Em { get; init; }

    public static HabilitacaoGravada De(Habilitacao habilitacao) => new()
    {
        Trabalhador = habilitacao.Trabalhador.Valor,
        Matricula = habilitacao.Matricula.Numero,
        Funcoes = [.. habilitacao.Funcoes.Select(funcao => funcao.Nome)],
        Em = habilitacao.Em,
    };

    public Habilitacao ParaHabilitacao(Categoria categoria) =>
        new(new TrabalhadorId(Trabalhador), Dominio.Identificacao.Matricula.De(Matricula), [.. Funcoes.Select(funcao => new Funcao(categoria, funcao))], Em);
}
