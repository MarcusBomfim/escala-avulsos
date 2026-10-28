namespace EscalaAvulsos.Dominio.Escalacoes;

/// <summary>Se quem está na escala apareceu para trabalhar (Lei 9.719/1998, art. 6º).</summary>
public enum Comparecimento
{
    /// <summary>O período ainda não chegou, ou a presença ainda não foi conferida.</summary>
    Aguardando,

    /// <summary>A presença foi confirmada no local de trabalho.</summary>
    Presente,

    /// <summary>Não apareceu, sem justificativa.</summary>
    Falta,

    /// <summary>Não apareceu, e justificou — um atestado, por exemplo.</summary>
    FaltaJustificada,
}
