using EscalaAvulsos.Dominio.Identificacao;
using EscalaAvulsos.Dominio.Periodos;
using EscalaAvulsos.Dominio.Requisicoes;
using EscalaAvulsos.Dominio.Trabalhadores;

namespace EscalaAvulsos.Dominio.Escalacoes;

/// <summary>Um trabalhador numa vaga da escala: tudo o que ele precisa saber para chegar ao trabalho.</summary>
public sealed record Engajamento
{
    internal Engajamento(Trabalhador trabalhador, Funcao funcao, Requisicao requisicao, int terno)
    {
        Trabalhador = trabalhador.Id;
        Matricula = trabalhador.Matricula;
        Nome = trabalhador.Nome;
        Funcao = funcao;
        Requisicao = requisicao.Id;
        Operador = requisicao.Operador;
        Navio = requisicao.Navio;
        Local = requisicao.Local;
        Periodo = requisicao.Periodo;
        Terno = terno;
    }

    /// <summary>A mesma vaga, com outro trabalhador: o substituto de quem desistiu.</summary>
    private Engajamento(Engajamento vaga, SituacaoNaEscala substituto)
    {
        Trabalhador = substituto.Trabalhador;
        Matricula = substituto.Matricula;
        Nome = substituto.Nome;
        Funcao = vaga.Funcao;
        Requisicao = vaga.Requisicao;
        Operador = vaga.Operador;
        Navio = vaga.Navio;
        Local = vaga.Local;
        Periodo = vaga.Periodo;
        Terno = vaga.Terno;
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

    internal Engajamento Para(SituacaoNaEscala substituto) => new(this, substituto);
}
