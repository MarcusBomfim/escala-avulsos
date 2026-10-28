using EscalaAvulsos.Dominio.Trabalhadores;

namespace EscalaAvulsos.Api.Recursos.Trabalhadores;

/// <summary>Um trabalhador novo, como o órgão gestor o inscreve: a matrícula, o nome, o CPF, a categoria e a inscrição.</summary>
public sealed record NovoTrabalhador(int Matricula, string Nome, string Cpf, Categoria Categoria, Inscricao Inscricao);

/// <summary>Uma função que o trabalhador passa a poder exercer, ou a renovação dela: o nome da função, desde quando e até quando.</summary>
public sealed record NovaQualificacao(string Funcao, DateOnly Desde, DateOnly? ValidaAte = null);

/// <summary>O exame médico mais recente.</summary>
public sealed record NovoAso(DateOnly EmitidoEm, DateOnly ValidoAte);

/// <summary>Férias, afastamento ou suspensão. O afastamento pode vir sem o último dia.</summary>
public sealed record NovoImpedimento(TipoDeImpedimento Tipo, DateOnly De, DateOnly? Ate = null, string? Observacao = null);

/// <summary>O dia em que o trabalhador afastado volta a poder ser escalado.</summary>
public sealed record FimDoAfastamento(DateOnly Volta);

/// <summary>O cancelamento da inscrição, a partir de um dia, com o motivo.</summary>
public sealed record NovoCancelamento(DateOnly APartirDe, string Motivo);

/// <summary>O trabalhador como a API o mostra — ao escalador, e a ele mesmo.</summary>
public sealed record TrabalhadorResposta(
    int Matricula,
    string Nome,
    string Cpf,
    Categoria Categoria,
    Inscricao Inscricao,
    IReadOnlyList<QualificacaoResposta> Qualificacoes,
    AsoResposta? Aso,
    IReadOnlyList<ImpedimentoResposta> Impedimentos,
    CancelamentoResposta? Cancelamento)
{
    public static TrabalhadorResposta De(Trabalhador trabalhador)
    {
        ArgumentNullException.ThrowIfNull(trabalhador);

        return new(
            trabalhador.Matricula.Numero,
            trabalhador.Nome,
            trabalhador.Cpf.Formatado,
            trabalhador.Categoria,
            trabalhador.Inscricao,
            [.. trabalhador.Qualificacoes.Select(qualificacao => new QualificacaoResposta(qualificacao.Funcao.Nome, qualificacao.Desde, qualificacao.ValidaAte))],
            trabalhador.Aso is { } aso ? new AsoResposta(aso.EmitidoEm, aso.ValidoAte) : null,
            [.. trabalhador.Impedimentos.Select(impedimento => new ImpedimentoResposta(
                impedimento.Tipo,
                impedimento.De,
                impedimento.Ate,
                impedimento.Observacao,
                impedimento.ToString()))],
            trabalhador.Cancelamento is { } cancelamento
                ? new CancelamentoResposta(cancelamento.APartirDe, cancelamento.Motivo)
                : null);
    }
}

public sealed record QualificacaoResposta(string Funcao, DateOnly Desde, DateOnly? ValidaAte);

public sealed record AsoResposta(DateOnly EmitidoEm, DateOnly ValidoAte);

/// <param name="Descricao">"férias de 04/01/2027 a 02/02/2027".</param>
public sealed record ImpedimentoResposta(TipoDeImpedimento Tipo, DateOnly De, DateOnly? Ate, string? Observacao, string Descricao);

public sealed record CancelamentoResposta(DateOnly APartirDe, string Motivo);
