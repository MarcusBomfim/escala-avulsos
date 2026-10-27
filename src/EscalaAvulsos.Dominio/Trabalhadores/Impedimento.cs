using EscalaAvulsos.Dominio.Comum;

namespace EscalaAvulsos.Dominio.Trabalhadores;

/// <summary>Por que um trabalhador ativo fica um tempo sem poder ser escalado.</summary>
public enum TipoDeImpedimento
{
    /// <summary>As férias, com começo e fim.</summary>
    Ferias,

    /// <summary>O afastamento por doença ou acidente; pode começar sem data para acabar.</summary>
    Afastamento,

    /// <summary>A suspensão disciplinar, com começo e fim.</summary>
    Suspensao,
}

/// <summary>Um tempo em que o trabalhador não pode ser escalado: férias, afastamento ou suspensão.</summary>
/// <remarks>As datas são dias inteiros, incluídos o primeiro e o último.</remarks>
public sealed record Impedimento
{
    public const int TamanhoMaximoDaObservacao = 200;

    public Impedimento(TipoDeImpedimento tipo, DateOnly de, DateOnly? ate = null, string? observacao = null)
    {
        var problemas = new List<string>();

        if (!Enum.IsDefined(tipo))
        {
            problemas.Add($"O impedimento {(int)tipo} não existe: são as férias, o afastamento e a suspensão.");
        }
        else if (ate is null && tipo != TipoDeImpedimento.Afastamento)
        {
            problemas.Add(tipo == TipoDeImpedimento.Ferias ? "As férias precisam do último dia." : "A suspensão precisa do último dia.");
        }

        if (ate is { } ultimo && ultimo < de)
        {
            problemas.Add($"O último dia, {FormatoBrasileiro.Data(ultimo)}, é antes do primeiro, {FormatoBrasileiro.Data(de)}.");
        }

        if (Textos.Opcional(observacao)?.Length > TamanhoMaximoDaObservacao)
        {
            problemas.Add($"A observação passa de {TamanhoMaximoDaObservacao} caracteres.");
        }

        if (problemas.Count > 0)
        {
            throw new ExcecaoDeDominio(string.Join(" ", problemas));
        }

        Tipo = tipo;
        De = de;
        Ate = ate;
        Observacao = Textos.Opcional(observacao);
    }

    public TipoDeImpedimento Tipo { get; }

    /// <summary>O primeiro dia sem escala.</summary>
    public DateOnly De { get; }

    /// <summary>O último dia sem escala; nulo, no afastamento que ainda não tem data para acabar.</summary>
    public DateOnly? Ate { get; }

    public string? Observacao { get; }

    /// <summary>"as férias de 04/01/2027 a 02/02/2027", "o afastamento desde 10/03/2026".</summary>
    public string ComArtigo => $"{(Tipo == TipoDeImpedimento.Ferias ? "as" : Tipo == TipoDeImpedimento.Suspensao ? "a" : "o")} {this}";

    public bool VigenteEm(DateOnly dia) => De <= dia && (Ate is null || dia <= Ate);

    /// <summary>Se os dois impedimentos têm algum dia em comum.</summary>
    public bool Cruza(Impedimento outro)
    {
        ArgumentNullException.ThrowIfNull(outro);

        return De <= (outro.Ate ?? DateOnly.MaxValue) && outro.De <= (Ate ?? DateOnly.MaxValue);
    }

    /// <summary>"férias de 04/01/2027 a 02/02/2027", "afastamento desde 10/03/2026", "suspensão de 01/12/2026 a 15/12/2026".</summary>
    public override string ToString()
    {
        var nome = Tipo switch
        {
            TipoDeImpedimento.Ferias => "férias",
            TipoDeImpedimento.Afastamento => "afastamento",
            _ => "suspensão",
        };

        return Ate is { } ultimo
            ? $"{nome} de {FormatoBrasileiro.Data(De)} a {FormatoBrasileiro.Data(ultimo)}"
            : $"{nome} desde {FormatoBrasileiro.Data(De)}";
    }

    internal Impedimento EncerradoEm(DateOnly ultimoDia) => new(Tipo, De, ultimoDia, Observacao);
}
