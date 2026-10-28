using EscalaAvulsos.Dominio.Comum;
using EscalaAvulsos.Dominio.Trabalhadores;

namespace EscalaAvulsos.Dominio.Extratos;

/// <summary>Quanto vale um período de trabalho numa função.</summary>
public sealed record ValorDaFuncao(Funcao Funcao, Dinheiro PorPeriodo);

/// <summary>
/// A tabela de remuneração da convenção: o valor do período em cada função, e
/// o adicional do trabalho noturno (ADR-0010).
/// </summary>
/// <remarks>
/// O valor por período é a taxa da convenção. O pagamento por produção —
/// por contêiner, por tonelada — não entra aqui: ele depende da medição da
/// operação, que o sistema não recebe.
/// </remarks>
public sealed class TabelaDeRemuneracao
{
    private readonly Dictionary<Funcao, Dinheiro> _valores;

    /// <param name="adicionalNoturno">O adicional sobre o valor do período, na noite e na madrugada: 0,20 é 20%.</param>
    public TabelaDeRemuneracao(IReadOnlyCollection<ValorDaFuncao> valores, decimal adicionalNoturno)
    {
        ArgumentNullException.ThrowIfNull(valores);

        var problemas = new List<string>();

        if (valores.Count == 0)
        {
            problemas.Add("A tabela precisa do valor de pelo menos uma função.");
        }

        foreach (var valor in valores.Where(valor => valor.PorPeriodo <= Dinheiro.Zero || !valor.PorPeriodo.EstaEmCentavos))
        {
            problemas.Add($"O valor do período de {valor.Funcao.NaFrase} tem de ser positivo e em centavos, e seria {valor.PorPeriodo}.");
        }

        foreach (var repetida in valores.GroupBy(valor => valor.Funcao).Where(funcao => funcao.Count() > 1))
        {
            problemas.Add($"A tabela repete o valor de {repetida.Key.NaFrase}.");
        }

        if (adicionalNoturno is < 0 or > 1)
        {
            problemas.Add($"O adicional noturno vai de 0% a 100%, e seria {FormatoBrasileiro.Porcentagem(adicionalNoturno)}.");
        }

        if (problemas.Count > 0)
        {
            throw new ExcecaoDeDominio(string.Join(" ", problemas));
        }

        _valores = valores.ToDictionary(valor => valor.Funcao, valor => valor.PorPeriodo);
        AdicionalNoturno = adicionalNoturno;
    }

    public decimal AdicionalNoturno { get; }

    public IReadOnlyList<ValorDaFuncao> Valores => [.. _valores.Select(par => new ValorDaFuncao(par.Key, par.Value))];

    /// <summary>O valor de um período na função; a função que a tabela não tem é recusada, e não paga como zero.</summary>
    public Dinheiro ValorDe(Funcao funcao)
    {
        ArgumentNullException.ThrowIfNull(funcao);

        return _valores.TryGetValue(funcao, out var valor)
            ? valor
            : throw new ExcecaoDeDominio($"A tabela de remuneração não tem o valor de {funcao.NaFrase}.");
    }
}
