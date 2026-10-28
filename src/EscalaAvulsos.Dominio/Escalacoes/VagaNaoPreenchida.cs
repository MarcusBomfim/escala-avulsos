using EscalaAvulsos.Dominio.Requisicoes;
using EscalaAvulsos.Dominio.Trabalhadores;

namespace EscalaAvulsos.Dominio.Escalacoes;

/// <summary>
/// Vagas de uma função, num terno, que ninguém habilitado pôde pegar: a falta
/// de mão de obra, que o operador precisa saber antes de o período começar.
/// </summary>
public sealed record VagaNaoPreenchida
{
    internal VagaNaoPreenchida(Requisicao requisicao, int terno, Funcao funcao, int quantidade)
    {
        Requisicao = requisicao.Id;
        Navio = requisicao.Navio;
        Terno = terno;
        Funcao = funcao;
        Quantidade = quantidade;
    }

    public RequisicaoId Requisicao { get; }

    public Navio Navio { get; }

    public int Terno { get; }

    public Funcao Funcao { get; }

    public int Quantidade { get; }

    /// <summary>"estivador: 2, no terno 1 do navio Atlântico Sul (IMO 9876543)".</summary>
    public override string ToString() => $"{Funcao.NaFrase}: {Quantidade}, no terno {Terno} do navio {Navio}";
}
