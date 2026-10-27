namespace EscalaAvulsos.Dominio.Trabalhadores;

/// <summary>
/// As atividades do trabalho portuário avulso (Lei 12.815/2013, art. 40).
/// Cada uma tem o seu rodízio: o estivador não entra na escala da capatazia.
/// </summary>
public enum Categoria
{
    /// <summary>A movimentação da carga no cais e nos armazéns, com os equipamentos do porto.</summary>
    Capatazia,

    /// <summary>A movimentação da carga a bordo — nos porões e no convés —, com os equipamentos do navio.</summary>
    Estiva,

    /// <summary>A contagem e a conferência da carga que entra no navio e sai dele.</summary>
    ConferenciaDeCarga,

    /// <summary>O reparo e a remarcação das embalagens da carga.</summary>
    ConsertoDeCarga,

    /// <summary>A vigilância da entrada e da saída de pessoas e de carga a bordo.</summary>
    VigilanciaDeEmbarcacoes,

    /// <summary>A limpeza e a conservação dos navios e dos tanques.</summary>
    Bloco,
}

/// <summary>A categoria escrita numa frase.</summary>
public static class Categorias
{
    extension(Categoria categoria)
    {
        /// <summary>"a estiva", "o bloco".</summary>
        public string ComArtigo => categoria switch
        {
            Categoria.Capatazia => "a capatazia",
            Categoria.Estiva => "a estiva",
            Categoria.ConferenciaDeCarga => "a conferência de carga",
            Categoria.ConsertoDeCarga => "o conserto de carga",
            Categoria.VigilanciaDeEmbarcacoes => "a vigilância de embarcações",
            Categoria.Bloco => "o bloco",
            _ => $"a categoria {(int)categoria}",
        };

        /// <summary>"da estiva", "do bloco".</summary>
        public string ComDe => $"d{categoria.ComArtigo}";
    }
}
