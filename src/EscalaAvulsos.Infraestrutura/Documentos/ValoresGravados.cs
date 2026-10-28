using EscalaAvulsos.Dominio.Identificacao;
using EscalaAvulsos.Dominio.Periodos;
using EscalaAvulsos.Dominio.Requisicoes;

namespace EscalaAvulsos.Infraestrutura.Documentos;

/*
 * Os valores que aparecem em mais de um documento. Cada um volta pelo
 * construtor ou pela leitura do domínio, que confere: um CNPJ que não bate
 * com os dígitos não volta do banco como se batesse.
 */

/// <summary>O período, pelo dia em que começa e pelo turno: o início e o fim saem dele.</summary>
internal sealed record PeriodoGravado
{
    public required DateOnly Dia { get; init; }

    public required Turno Turno { get; init; }

    public static PeriodoGravado De(Periodo periodo) => new() { Dia = periodo.Dia, Turno = periodo.Turno };

    public Periodo ParaPeriodo() => Periodo.De(Dia, Turno);
}

internal sealed record OperadorGravado
{
    public required string Nome { get; init; }

    /// <summary>Sem máscara.</summary>
    public required string Cnpj { get; init; }

    public static OperadorGravado De(OperadorPortuario operador) => new() { Nome = operador.Nome, Cnpj = operador.Cnpj.Numero };

    public OperadorPortuario ParaOperador() => new(Nome, Dominio.Identificacao.Cnpj.De(Cnpj));
}

internal sealed record NavioGravado
{
    public required string Nome { get; init; }

    /// <summary>Os sete dígitos, sem o "IMO".</summary>
    public required string Imo { get; init; }

    public static NavioGravado De(Navio navio) => new() { Nome = navio.Nome, Imo = navio.Imo.Numero };

    public Navio ParaNavio() => new(Nome, NumeroImo.De(Imo));
}

internal sealed record LocalGravado
{
    public required string Terminal { get; init; }

    public required string? Berco { get; init; }

    public static LocalGravado De(LocalDaOperacao local) => new() { Terminal = local.Terminal, Berco = local.Berco };

    public LocalDaOperacao ParaLocal() => new(Terminal, Berco);
}
