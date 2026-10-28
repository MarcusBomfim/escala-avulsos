using EscalaAvulsos.Dominio.Identificacao;
using EscalaAvulsos.Dominio.Trabalhadores;

namespace EscalaAvulsos.Dominio.Escalacoes;

/// <summary>Um trabalhador que se apresentou para a escala de um período, nas funções que escolheu.</summary>
/// <remarks>Só nasce pela escalação, que confere a janela, a categoria e se ele pode trabalhar em cada função.</remarks>
public sealed record Habilitacao
{
    internal Habilitacao(TrabalhadorId trabalhador, Matricula matricula, IReadOnlyList<Funcao> funcoes, DateTimeOffset em)
    {
        Trabalhador = trabalhador;
        Matricula = matricula;
        Funcoes = funcoes;
        Em = em;
    }

    public TrabalhadorId Trabalhador { get; }

    public Matricula Matricula { get; }

    /// <summary>As funções em que ele concorre, sem repetição, na ordem em que escolheu.</summary>
    public IReadOnlyList<Funcao> Funcoes { get; }

    /// <summary>Quando ele se habilitou — só para o registro: a vez é pela matrícula, e não pela hora.</summary>
    public DateTimeOffset Em { get; }
}
