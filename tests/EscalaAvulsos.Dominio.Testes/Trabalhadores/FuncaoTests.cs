using EscalaAvulsos.Dominio.Comum;
using EscalaAvulsos.Dominio.Trabalhadores;

namespace EscalaAvulsos.Dominio.Testes.Trabalhadores;

public sealed class FuncaoTests
{
    [Fact]
    public void O_nome_fica_com_a_primeira_letra_maiuscula_e_um_espaco_entre_as_palavras()
    {
        var funcao = new Funcao(Categoria.Estiva, "  operador   de guindaste\tde bordo ");

        Assert.Equal(("Operador de guindaste de bordo", "operador de guindaste de bordo"), (funcao.Nome, funcao.NaFrase));
        Assert.Equal("Operador de guindaste de bordo", funcao.ToString());
    }

    [Fact]
    public void A_mesma_funcao_escrita_de_outro_jeito_e_a_mesma()
    {
        var escrita = new Funcao(Categoria.Estiva, "Operador de guindaste de bordo");
        var gritada = new Funcao(Categoria.Estiva, "OPERADOR DE GUINDASTE DE BORDO");

        Assert.Equal(escrita, gritada);
        Assert.Equal(escrita.GetHashCode(), gritada.GetHashCode());
        Assert.Single(new HashSet<Funcao> { escrita, gritada });
    }

    [Fact]
    public void O_mesmo_nome_em_outra_categoria_e_outra_funcao()
    {
        Assert.NotEqual(new Funcao(Categoria.Capatazia, "Operador de empilhadeira"), new Funcao(Categoria.Estiva, "Operador de empilhadeira"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Precisa_de_nome(string? nome)
    {
        var erro = Assert.Throws<ExcecaoDeDominio>(() => new Funcao(Categoria.Estiva, nome!));

        Assert.Equal("Falta o nome da função.", erro.Message);
    }

    [Fact]
    public void O_nome_tem_no_maximo_sessenta_caracteres()
    {
        _ = new Funcao(Categoria.Estiva, new string('a', 60));

        var erro = Assert.Throws<ExcecaoDeDominio>(() => new Funcao(Categoria.Estiva, new string('a', 61)));

        Assert.Equal("O nome da função passa de 60 caracteres.", erro.Message);
    }

    [Fact]
    public void A_categoria_tem_de_existir()
    {
        var erro = Assert.Throws<ExcecaoDeDominio>(() => new Funcao((Categoria)9, "Estivador"));

        Assert.Equal("A categoria 9 não existe.", erro.Message);
    }

    [Theory]
    [InlineData(Categoria.Capatazia, "a capatazia", "da capatazia")]
    [InlineData(Categoria.Estiva, "a estiva", "da estiva")]
    [InlineData(Categoria.ConferenciaDeCarga, "a conferência de carga", "da conferência de carga")]
    [InlineData(Categoria.ConsertoDeCarga, "o conserto de carga", "do conserto de carga")]
    [InlineData(Categoria.VigilanciaDeEmbarcacoes, "a vigilância de embarcações", "da vigilância de embarcações")]
    [InlineData(Categoria.Bloco, "o bloco", "do bloco")]
    public void A_categoria_se_escreve_na_frase_com_o_artigo_certo(Categoria categoria, string comArtigo, string comDe)
    {
        Assert.Equal((comArtigo, comDe), (categoria.ComArtigo, categoria.ComDe));
    }
}
