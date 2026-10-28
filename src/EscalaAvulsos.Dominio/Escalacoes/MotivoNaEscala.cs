using EscalaAvulsos.Dominio.Trabalhadores;

namespace EscalaAvulsos.Dominio.Escalacoes;

/// <summary>Por que um habilitado não pegou vaga numa função — ou em nenhuma, no descanso.</summary>
public enum TipoDeMotivoNaEscala
{
    /// <summary>Uma jornada já marcada deixaria o trabalhador sem o descanso mínimo: não pega vaga nenhuma.</summary>
    EmDescanso,

    /// <summary>Na hora da escala, ele não podia trabalhar na função: a aptidão diz por quê.</summary>
    NaoApto,

    /// <summary>As vagas da função acabaram antes de o rodízio chegar a ele.</summary>
    NaoAlcancado,

    /// <summary>Cadastrado, e os registrados ficaram com todas as vagas da função.</summary>
    VagasComRegistrados,

    /// <summary>Nenhuma requisição do período pediu a função.</summary>
    SemVagaNaFuncao,
}

/// <summary>
/// Por que um habilitado não pegou vaga numa função: o tipo, a frase e o que
/// a explica — as posições que faltaram no rodízio, os motivos da aptidão ou
/// a jornada que pede descanso.
/// </summary>
/// <remarks>
/// A frase não tem sujeito, como os motivos da aptidão: "como estivador, o
/// rodízio parou 2 posições antes da sua vez". O aplicativo fala com o
/// trabalhador com o tipo e os dados.
/// </remarks>
public sealed record MotivoNaEscala
{
    private MotivoNaEscala(
        TipoDeMotivoNaEscala tipo,
        Funcao? funcao,
        string descricao,
        int? posicoesAntesDaVez = null,
        IReadOnlyList<Motivo>? motivosDaAptidao = null,
        ConflitoDeDescanso? descanso = null)
    {
        Tipo = tipo;
        Funcao = funcao;
        Descricao = descricao;
        PosicoesAntesDaVez = posicoesAntesDaVez;
        MotivosDaAptidao = motivosDaAptidao ?? [];
        Descanso = descanso;
    }

    public TipoDeMotivoNaEscala Tipo { get; }

    /// <summary>A função em que ele não pegou vaga; nula no descanso, que vale para todas.</summary>
    public Funcao? Funcao { get; }

    public string Descricao { get; }

    /// <summary>
    /// Quantas posições depois do último escalado estava a vez dele: quantas
    /// vagas a mais, no máximo, a função precisaria ter para chegar a ele.
    /// </summary>
    public int? PosicoesAntesDaVez { get; }

    /// <summary>Por que ele não podia trabalhar na função; vazio, se o motivo é outro.</summary>
    public IReadOnlyList<Motivo> MotivosDaAptidao { get; }

    /// <summary>A jornada que pede descanso, se o motivo é ela.</summary>
    public ConflitoDeDescanso? Descanso { get; }

    public override string ToString() => Descricao;

    internal static MotivoNaEscala EmDescanso(ConflitoDeDescanso conflito) =>
        new(TipoDeMotivoNaEscala.EmDescanso, null, conflito.Descricao, descanso: conflito);

    /// <summary>"como estivador, cumpre suspensão de 05/11/2026 a 06/11/2026 e está em afastamento desde 05/11/2026".</summary>
    internal static MotivoNaEscala NaoApto(Funcao funcao, IReadOnlyList<Motivo> motivos)
    {
        var frases = motivos.Select(motivo => motivo.Descricao).ToList();
        var juntas = frases.Count == 1 ? frases[0] : $"{string.Join(", ", frases.Take(frases.Count - 1))} e {frases[^1]}";

        return new(TipoDeMotivoNaEscala.NaoApto, funcao, $"como {funcao.NaFrase}, {juntas}", motivosDaAptidao: motivos);
    }

    internal static MotivoNaEscala NaoAlcancado(Funcao funcao, int posicoes) =>
        new(
            TipoDeMotivoNaEscala.NaoAlcancado,
            funcao,
            $"como {funcao.NaFrase}, o rodízio parou {posicoes} {(posicoes == 1 ? "posição" : "posições")} antes da sua vez",
            posicoesAntesDaVez: posicoes);

    internal static MotivoNaEscala VagasComRegistrados(Funcao funcao) =>
        new(TipoDeMotivoNaEscala.VagasComRegistrados, funcao, $"como {funcao.NaFrase}, as vagas ficaram todas com registrados");

    internal static MotivoNaEscala SemVagaNaFuncao(Funcao funcao) =>
        new(TipoDeMotivoNaEscala.SemVagaNaFuncao, funcao, $"como {funcao.NaFrase}, não houve vaga neste período");
}
