using EscalaAvulsos.Dominio.Identificacao;
using EscalaAvulsos.Dominio.Trabalhadores;

namespace EscalaAvulsos.Infraestrutura.Documentos;

/// <summary>
/// Como um trabalhador é gravado: o que se informou na inscrição, e o que veio
/// depois — as qualificações, o exame, os impedimentos, o cancelamento.
/// </summary>
/// <remarks>
/// As funções vão só pelo nome: a categoria é a do trabalhador, e uma
/// qualificação de outra categoria nem chega a ser gravada.
/// </remarks>
internal sealed record DocumentoDoTrabalhador
{
    public const short Formato = 1;

    public required int Matricula { get; init; }

    public required string Nome { get; init; }

    /// <summary>Só os dígitos.</summary>
    public required string Cpf { get; init; }

    public required Categoria Categoria { get; init; }

    public required Inscricao Inscricao { get; init; }

    public required IReadOnlyList<QualificacaoGravada> Qualificacoes { get; init; }

    public required AsoGravado? Aso { get; init; }

    public required IReadOnlyList<ImpedimentoGravado> Impedimentos { get; init; }

    public required CancelamentoGravado? Cancelamento { get; init; }

    public static DocumentoDoTrabalhador De(Trabalhador trabalhador) => new()
    {
        Matricula = trabalhador.Matricula.Numero,
        Nome = trabalhador.Nome,
        Cpf = trabalhador.Cpf.Numero,
        Categoria = trabalhador.Categoria,
        Inscricao = trabalhador.Inscricao,
        Qualificacoes = [.. trabalhador.Qualificacoes.Select(QualificacaoGravada.De)],
        Aso = trabalhador.Aso is { } aso ? new AsoGravado { EmitidoEm = aso.EmitidoEm, ValidoAte = aso.ValidoAte } : null,
        Impedimentos = [.. trabalhador.Impedimentos.Select(ImpedimentoGravado.Do)],
        Cancelamento = trabalhador.Cancelamento is { } cancelamento
            ? new CancelamentoGravado { APartirDe = cancelamento.APartirDe, Motivo = cancelamento.Motivo }
            : null,
    };

    /// <remarks>Os passos são dados de novo pelas regras de sempre (ADR-0011).</remarks>
    public Trabalhador ParaTrabalhador(TrabalhadorId id) =>
        Trabalhador.Reconstituir(
            id,
            new DadosDoTrabalhador(Dominio.Identificacao.Matricula.De(Matricula), Nome, Dominio.Identificacao.Cpf.De(Cpf), Categoria, Inscricao),
            [.. Qualificacoes.Select(qualificacao => qualificacao.ParaQualificacao(Categoria))],
            Aso is { } aso ? new Aso(aso.EmitidoEm, aso.ValidoAte) : null,
            [.. Impedimentos.Select(impedimento => impedimento.ParaImpedimento())],
            Cancelamento is { } cancelamento ? new CancelamentoDaInscricao(cancelamento.APartirDe, cancelamento.Motivo) : null);
}

internal sealed record QualificacaoGravada
{
    public required string Funcao { get; init; }

    public required DateOnly Desde { get; init; }

    public required DateOnly? ValidaAte { get; init; }

    public static QualificacaoGravada De(Qualificacao qualificacao) => new()
    {
        Funcao = qualificacao.Funcao.Nome,
        Desde = qualificacao.Desde,
        ValidaAte = qualificacao.ValidaAte,
    };

    public Qualificacao ParaQualificacao(Categoria categoria) => new(new Funcao(categoria, Funcao), Desde, ValidaAte);
}

internal sealed record AsoGravado
{
    public required DateOnly EmitidoEm { get; init; }

    public required DateOnly ValidoAte { get; init; }
}

internal sealed record ImpedimentoGravado
{
    public required TipoDeImpedimento Tipo { get; init; }

    public required DateOnly De { get; init; }

    public required DateOnly? Ate { get; init; }

    public required string? Observacao { get; init; }

    public static ImpedimentoGravado Do(Impedimento impedimento) => new()
    {
        Tipo = impedimento.Tipo,
        De = impedimento.De,
        Ate = impedimento.Ate,
        Observacao = impedimento.Observacao,
    };

    public Impedimento ParaImpedimento() => new(Tipo, De, Ate, Observacao);
}

internal sealed record CancelamentoGravado
{
    public required DateOnly APartirDe { get; init; }

    public required string Motivo { get; init; }
}
