using EscalaAvulsos.Dominio.Comum;
using EscalaAvulsos.Dominio.Identificacao;

namespace EscalaAvulsos.Dominio.Requisicoes;

/// <summary>
/// A empresa que opera a carga no porto e pede a mão de obra ao órgão gestor,
/// identificada pelo CNPJ.
/// </summary>
/// <remarks>
/// Dois operadores são o mesmo se o CNPJ é o mesmo: o nome que vem escrito
/// numa requisição e noutra pode variar — "Cais Forte", "Cais Forte Operações
/// Portuárias Ltda." —, e a empresa continua a mesma.
/// </remarks>
public sealed record OperadorPortuario
{
    public const int TamanhoMaximoDoNome = 120;

    public OperadorPortuario(string nome, Cnpj cnpj)
    {
        ArgumentNullException.ThrowIfNull(cnpj);

        if (Textos.Problema(nome, "o nome do operador portuário", TamanhoMaximoDoNome) is { } problema)
        {
            throw new ExcecaoDeDominio(problema);
        }

        Nome = nome.Trim();
        Cnpj = cnpj;
    }

    public string Nome { get; }

    public Cnpj Cnpj { get; }

    public bool Equals(OperadorPortuario? other) => other is not null && Cnpj == other.Cnpj;

    public override int GetHashCode() => Cnpj.GetHashCode();

    /// <summary>"Cais Forte Operações Portuárias (11.222.333/0001-81)".</summary>
    public override string ToString() => $"{Nome} ({Cnpj})";
}
