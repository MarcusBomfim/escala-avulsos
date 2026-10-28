using EscalaAvulsos.Dominio.Comum;
using EscalaAvulsos.Dominio.Identificacao;
using EscalaAvulsos.Dominio.Trabalhadores;

namespace EscalaAvulsos.Dominio.Escalacoes;

/// <summary>
/// Como ficou, na escala, quem se habilitou: a vaga que pegou, ou por que
/// ficou de fora — a resposta da tela "por que não fui escalado". Depois da
/// escala, ela acompanha o que acontece: a desistência, a vaga do substituto,
/// a presença e a falta (ADR-0008).
/// </summary>
/// <remarks>Só muda pela escala, que confere os prazos.</remarks>
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

    /// <summary>A situação gravada, com a vaga que ela tem agora e a de que desistiu (ADR-0011).</summary>
    internal SituacaoNaEscala(SituacaoGravada gravada, Engajamento? engajamento, Engajamento? vagaDesistida)
    {
        Trabalhador = gravada.Trabalhador;
        Matricula = gravada.Matricula;
        Nome = gravada.Nome;
        Funcoes = gravada.Funcoes;
        Motivos = gravada.Motivos;
        Engajamento = engajamento;
        Substituto = gravada.Substituto;
        VagaDesistida = vagaDesistida;
        DesistiuEm = gravada.Desistencia?.Em;
        Comparecimento = gravada.Comparecimento;
        PresenteDesde = gravada.PresenteDesde;
        JustificativaDaFalta = gravada.JustificativaDaFalta;
    }

    public TrabalhadorId Trabalhador { get; }

    public Matricula Matricula { get; }

    public string Nome { get; }

    /// <summary>As funções em que ele se habilitou.</summary>
    public IReadOnlyList<Funcao> Funcoes { get; }

    /// <summary>A vaga que ele tem agora; nula, se ficou de fora ou desistiu.</summary>
    public Engajamento? Engajamento { get; private set; }

    public bool Escalado => Engajamento is not null;

    /// <summary>Se ele entrou na escala depois, no lugar de quem desistiu.</summary>
    public bool Substituto { get; private set; }

    /// <summary>A vaga de que ele desistiu; nula, se não desistiu.</summary>
    public Engajamento? VagaDesistida { get; private set; }

    public DateTimeOffset? DesistiuEm { get; private set; }

    /// <summary>Se ele apareceu para trabalhar; só conta para quem está na escala.</summary>
    public Comparecimento Comparecimento { get; private set; }

    public DateTimeOffset? PresenteDesde { get; private set; }

    public string? JustificativaDaFalta { get; private set; }

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
    public override string ToString()
    {
        var quem = $"{Nome} (matrícula {Matricula})";

        if (Engajamento is { } engajamento)
        {
            var naEscala = Substituto
                ? $"{quem} está na escala, no lugar de quem desistiu: {engajamento.Vaga}."
                : $"{quem} está na escala: {engajamento.Vaga}.";

            return Comparecimento switch
            {
                Comparecimento.Presente => $"{naEscala} Presença confirmada em {FormatoBrasileiro.Instante(PresenteDesde!.Value)}.",
                Comparecimento.Falta => $"{naEscala} Faltou.",
                Comparecimento.FaltaJustificada => $"{naEscala} Faltou, com justificativa: {JustificativaDaFalta}.",
                _ => naEscala,
            };
        }

        return VagaDesistida is { } desistida
            ? $"{quem} desistiu da vaga de {desistida.Funcao.NaFrase} em {FormatoBrasileiro.Instante(DesistiuEm!.Value)}."
            : $"{quem} ficou fora da escala: {string.Join("; ", Motivos.Select(motivo => motivo.Descricao))}.";
    }

    /// <summary>A situação como se grava: sem a vaga de agora, que a escala guarda.</summary>
    internal SituacaoGravada Gravada =>
        new(
            Trabalhador,
            Matricula,
            Nome,
            Funcoes,
            Motivos,
            Substituto,
            VagaDesistida is { } desistida ? new DesistenciaGravada(desistida.Gravada, DesistiuEm!.Value) : null,
            Comparecimento,
            PresenteDesde,
            JustificativaDaFalta);

    internal void Desistir(DateTimeOffset em)
    {
        VagaDesistida = Engajamento;
        DesistiuEm = em;
        Engajamento = null;
    }

    internal void Substituir(Engajamento vaga)
    {
        Engajamento = vaga.Para(this);
        Substituto = true;
    }

    internal void ConfirmarPresenca(DateTimeOffset em)
    {
        Comparecimento = Comparecimento.Presente;
        PresenteDesde = em;
    }

    internal void Faltar() => Comparecimento = Comparecimento.Falta;

    internal void JustificarFalta(string justificativa)
    {
        Comparecimento = Comparecimento.FaltaJustificada;
        JustificativaDaFalta = justificativa;
    }
}
