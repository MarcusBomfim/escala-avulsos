namespace EscalaAvulsos.Dominio.Requisicoes;

/// <summary>
/// Um terno da requisição: a equipe que trabalha junta — num porão, num
/// guindaste —, com tantas vagas de cada função.
/// </summary>
/// <remarks>Só nasce pela requisição, que numera os ternos e confere as vagas.</remarks>
public sealed class Terno
{
    internal Terno(int numero, IReadOnlyList<VagasDaFuncao> vagas)
    {
        Numero = numero;
        Vagas = vagas;
    }

    /// <summary>De 1 em diante, na ordem da requisição: é o "terno 2" que o trabalhador lê na escala.</summary>
    public int Numero { get; }

    public IReadOnlyList<VagasDaFuncao> Vagas { get; }

    public int TotalDeVagas => Vagas.Sum(vaga => vaga.Quantidade);

    /// <summary>"terno 1 — contramestre de terno: 1, estivador: 8".</summary>
    public override string ToString() =>
        $"terno {Numero} — {string.Join(", ", Vagas.Select(vaga => $"{vaga.Funcao.NaFrase}: {vaga.Quantidade}"))}";
}
