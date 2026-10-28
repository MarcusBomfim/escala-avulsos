using EscalaAvulsos.Dominio.Identificacao;
using EscalaAvulsos.Dominio.Periodos;
using EscalaAvulsos.Dominio.Requisicoes;
using EscalaAvulsos.Dominio.Trabalhadores;

namespace EscalaAvulsos.Dominio.Escalacoes;

/// <summary>Um trabalhador numa vaga da escala: tudo o que ele precisa saber para chegar ao trabalho.</summary>
public sealed record Engajamento
{
    internal Engajamento(Trabalhador trabalhador, Funcao funcao, Requisicao requisicao, int terno)
        : this(trabalhador.Id, trabalhador.Matricula, trabalhador.Nome, funcao, RequisicaoNaEscala.De(requisicao), requisicao.Periodo, terno)
    {
    }

    /// <summary>A vaga, com quem está nela: a da escala, a do substituto, a que a escala gravada refaz.</summary>
    internal Engajamento(TrabalhadorId trabalhador, Matricula matricula, string nome, Funcao funcao, RequisicaoNaEscala requisicao, Periodo periodo, int terno)
    {
        Trabalhador = trabalhador;
        Matricula = matricula;
        Nome = nome;
        Funcao = funcao;
        Requisicao = requisicao.Id;
        Operador = requisicao.Operador;
        Navio = requisicao.Navio;
        Local = requisicao.Local;
        Periodo = periodo;
        Terno = terno;
    }

    public TrabalhadorId Trabalhador { get; }

    public Matricula Matricula { get; }

    public string Nome { get; }

    public Funcao Funcao { get; }

    public RequisicaoId Requisicao { get; }

    public OperadorPortuario Operador { get; }

    public Navio Navio { get; }

    public LocalDaOperacao Local { get; }

    public Periodo Periodo { get; }

    /// <summary>O número do terno na requisição: o "terno 2" que o trabalhador lê.</summary>
    public int Terno { get; }

    /// <summary>
    /// "sinaleiro no terno 1 do navio Atlântico Sul (IMO 9876543), no Terminal
    /// Cais Leste, berço 4, em 05/11/2026, das 13h às 19h".
    /// </summary>
    public string Vaga => $"{Funcao.NaFrase} no terno {Terno} do navio {Navio}, no {Local}, em {Periodo}";

    /// <summary>"Antônio Ferreira (matrícula 4127): sinaleiro no terno 1 do navio Atlântico Sul (IMO 9876543), …".</summary>
    public override string ToString() => $"{Nome} (matrícula {Matricula}): {Vaga}";

    /// <summary>A requisição, o terno e a função, sem quem está na vaga.</summary>
    internal VagaGravada Gravada => new(Requisicao, Terno, Funcao);

    internal RequisicaoNaEscala DaRequisicao => new(Requisicao, Operador, Navio, Local);

    /// <summary>A mesma vaga, com outro trabalhador: o substituto de quem desistiu.</summary>
    internal Engajamento Para(SituacaoNaEscala substituto) =>
        new(substituto.Trabalhador, substituto.Matricula, substituto.Nome, Funcao, DaRequisicao, Periodo, Terno);
}
