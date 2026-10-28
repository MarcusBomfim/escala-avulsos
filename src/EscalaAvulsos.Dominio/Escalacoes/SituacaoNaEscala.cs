using EscalaAvulsos.Dominio.Identificacao;
using EscalaAvulsos.Dominio.Trabalhadores;

namespace EscalaAvulsos.Dominio.Escalacoes;

/// <summary>
/// Como ficou, na escala, quem se habilitou: a vaga que pegou, ou por que
/// ficou de fora — a resposta da tela "por que não fui escalado".
/// </summary>
public sealed class SituacaoNaEscala
{
    internal SituacaoNaEscala(Trabalhador trabalhador, Habilitacao habilitacao, Engajamento? engajamento, IReadOnlyList<MotivoNaEscala> motivos)
    {
        Trabalhador = trabalhador.Id;
        Matricula = trabalhador.Matricula;
        Nome = trabalhador.Nome;
        Funcoes = habilitacao.Funcoes;
        Engajamento = engajamento;
        Motivos = motivos;
    }

    public TrabalhadorId Trabalhador { get; }

    public Matricula Matricula { get; }

    public string Nome { get; }

    /// <summary>As funções em que ele se habilitou.</summary>
    public IReadOnlyList<Funcao> Funcoes { get; }

    /// <summary>A vaga que ele pegou; nula, se ficou de fora.</summary>
    public Engajamento? Engajamento { get; }

    public bool Escalado => Engajamento is not null;

    /// <summary>
    /// Por que ele não pegou vaga em cada função, na ordem em que as funções
    /// foram escaladas. Quem está na escala pode ter motivos também: o que não
    /// pegou como sinaleiro e pegou como estivador.
    /// </summary>
    public IReadOnlyList<MotivoNaEscala> Motivos { get; }

    /// <summary>
    /// "Antônio Ferreira (matrícula 4127) está na escala: sinaleiro no terno 1 …";
    /// "Benedito Lima (matrícula 4130) ficou fora da escala: como estivador, o
    /// rodízio parou 2 posições antes da sua vez."
    /// </summary>
    public override string ToString() =>
        Engajamento is { } engajamento
            ? $"{Nome} (matrícula {Matricula}) está na escala: {engajamento.Vaga}."
            : $"{Nome} (matrícula {Matricula}) ficou fora da escala: {string.Join("; ", Motivos.Select(motivo => motivo.Descricao))}.";
}
