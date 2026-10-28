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
        : this(RequisicaoNaEscala.De(requisicao), terno, funcao, quantidade)
    {
    }

    /// <summary>A vaga de quem desistiu, quando não havia ninguém na espera.</summary>
    internal VagaNaoPreenchida(Engajamento vaga, int quantidade)
        : this(vaga.DaRequisicao, vaga.Terno, vaga.Funcao, quantidade)
    {
    }

    internal VagaNaoPreenchida(RequisicaoNaEscala requisicao, int terno, Funcao funcao, int quantidade)
    {
        Requisicao = requisicao.Id;
        Operador = requisicao.Operador;
        Navio = requisicao.Navio;
        Local = requisicao.Local;
        Terno = terno;
        Funcao = funcao;
        Quantidade = quantidade;
    }

    public RequisicaoId Requisicao { get; }

    /// <summary>Quem pediu a vaga, e precisa saber que ela ficou vazia.</summary>
    public OperadorPortuario Operador { get; }

    public Navio Navio { get; }

    public LocalDaOperacao Local { get; }

    public int Terno { get; }

    public Funcao Funcao { get; }

    public int Quantidade { get; }

    /// <summary>"estivador: 2, no terno 1 do navio Atlântico Sul (IMO 9876543)".</summary>
    public override string ToString() => $"{Funcao.NaFrase}: {Quantidade}, no terno {Terno} do navio {Navio}";

    internal VagasVazias Gravadas => new(new VagaGravada(Requisicao, Terno, Funcao), Quantidade);

    internal RequisicaoNaEscala DaRequisicao => new(Requisicao, Operador, Navio, Local);
}
