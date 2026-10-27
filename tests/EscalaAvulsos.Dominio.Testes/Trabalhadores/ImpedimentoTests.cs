using EscalaAvulsos.Dominio.Comum;
using EscalaAvulsos.Dominio.Trabalhadores;

namespace EscalaAvulsos.Dominio.Testes.Trabalhadores;

public sealed class ImpedimentoTests
{
    private static readonly DateOnly Quatro = new(2027, 1, 4);

    private static readonly DateOnly Dois = new(2027, 2, 2);

    [Theory]
    [InlineData(TipoDeImpedimento.Ferias, "As férias precisam do último dia.")]
    [InlineData(TipoDeImpedimento.Suspensao, "A suspensão precisa do último dia.")]
    public void Ferias_e_suspensao_tem_dia_para_acabar(TipoDeImpedimento tipo, string mensagem)
    {
        var erro = Assert.Throws<ExcecaoDeDominio>(() => new Impedimento(tipo, Quatro));

        Assert.Equal(mensagem, erro.Message);
    }

    [Fact]
    public void O_afastamento_pode_comecar_sem_dia_para_acabar()
    {
        var afastamento = new Impedimento(TipoDeImpedimento.Afastamento, new DateOnly(2026, 3, 10), observacao: "  Cirurgia no joelho ");

        Assert.Null(afastamento.Ate);
        Assert.Equal("Cirurgia no joelho", afastamento.Observacao);
        Assert.Equal("afastamento desde 10/03/2026", afastamento.ToString());
        Assert.Equal("o afastamento desde 10/03/2026", afastamento.ComArtigo);
    }

    [Fact]
    public void Diz_tudo_o_que_esta_errado_de_uma_vez()
    {
        var erro = Assert.Throws<ExcecaoDeDominio>(() => new Impedimento((TipoDeImpedimento)5, Quatro, Quatro.AddDays(-1), new string('a', 201)));

        Assert.Equal(
            "O impedimento 5 não existe: são as férias, o afastamento e a suspensão. O último dia, 03/01/2027, é antes do primeiro, 04/01/2027. A observação passa de 200 caracteres.",
            erro.Message);
    }

    [Fact]
    public void Um_dia_so_serve()
    {
        var umDia = new Impedimento(TipoDeImpedimento.Suspensao, Quatro, Quatro, "   ");

        Assert.True(umDia.VigenteEm(Quatro));
        Assert.Null(umDia.Observacao);
        Assert.Equal("a suspensão de 04/01/2027 a 04/01/2027", umDia.ComArtigo);
    }

    [Theory]
    [InlineData("2027-01-03", false)]
    [InlineData("2027-01-04", true)]
    [InlineData("2027-01-20", true)]
    [InlineData("2027-02-02", true)]
    [InlineData("2027-02-03", false)]
    public void Vale_do_primeiro_ao_ultimo_dia(string dia, bool vigente)
    {
        var ferias = new Impedimento(TipoDeImpedimento.Ferias, Quatro, Dois);

        Assert.Equal(vigente, ferias.VigenteEm(DateOnly.Parse(dia, System.Globalization.CultureInfo.InvariantCulture)));
    }

    [Fact]
    public void Dois_impedimentos_cruzam_se_tem_um_dia_em_comum()
    {
        var ferias = new Impedimento(TipoDeImpedimento.Ferias, Quatro, Dois);

        Assert.True(ferias.Cruza(new Impedimento(TipoDeImpedimento.Ferias, Dois, Dois.AddDays(29))));
        Assert.True(ferias.Cruza(new Impedimento(TipoDeImpedimento.Ferias, Quatro.AddDays(-30), Quatro)));
        Assert.True(ferias.Cruza(new Impedimento(TipoDeImpedimento.Ferias, Quatro.AddDays(5), Quatro.AddDays(6))));
        Assert.False(ferias.Cruza(new Impedimento(TipoDeImpedimento.Ferias, Dois.AddDays(1), Dois.AddDays(30))));
        Assert.False(ferias.Cruza(new Impedimento(TipoDeImpedimento.Ferias, Quatro.AddDays(-30), Quatro.AddDays(-1))));
        Assert.True(ferias.Cruza(new Impedimento(TipoDeImpedimento.Afastamento, Quatro.AddDays(-300))));
        Assert.False(ferias.Cruza(new Impedimento(TipoDeImpedimento.Afastamento, Dois.AddDays(1))));
    }

    [Theory]
    [InlineData(TipoDeImpedimento.Ferias, "férias de 04/01/2027 a 02/02/2027", "as férias de 04/01/2027 a 02/02/2027")]
    [InlineData(TipoDeImpedimento.Afastamento, "afastamento de 04/01/2027 a 02/02/2027", "o afastamento de 04/01/2027 a 02/02/2027")]
    [InlineData(TipoDeImpedimento.Suspensao, "suspensão de 04/01/2027 a 02/02/2027", "a suspensão de 04/01/2027 a 02/02/2027")]
    public void Se_escreve_como_se_fala(TipoDeImpedimento tipo, string escrito, string comArtigo)
    {
        var impedimento = new Impedimento(tipo, Quatro, Dois);

        Assert.Equal((escrito, comArtigo), (impedimento.ToString(), impedimento.ComArtigo));
    }

    [Fact]
    public void Dois_impedimentos_do_mesmo_tipo_nao_cruzam_no_mesmo_trabalhador()
    {
        var antonio = TrabalhadoresDeTeste.Estivador();
        antonio.Impedir(new Impedimento(TipoDeImpedimento.Ferias, Quatro, Dois));

        var erro = Assert.Throws<ExcecaoDeDominio>(() => antonio.Impedir(new Impedimento(TipoDeImpedimento.Ferias, new DateOnly(2027, 1, 20), new DateOnly(2027, 2, 18))));

        Assert.Equal(
            "Não dá para registrar as férias de 20/01/2027 a 18/02/2027: as datas cruzam com as férias de 04/01/2027 a 02/02/2027.",
            erro.Message);
        Assert.Single(antonio.Impedimentos);
    }

    [Fact]
    public void Impedimentos_de_tipos_diferentes_podem_cruzar()
    {
        var antonio = TrabalhadoresDeTeste.Estivador();
        antonio.Impedir(new Impedimento(TipoDeImpedimento.Ferias, Quatro, Dois));

        antonio.Impedir(new Impedimento(TipoDeImpedimento.Afastamento, new DateOnly(2027, 1, 20)));
        antonio.Impedir(new Impedimento(TipoDeImpedimento.Ferias, Dois.AddDays(1), Dois.AddDays(30)));

        Assert.Equal(3, antonio.Impedimentos.Count);
    }

    [Fact]
    public void O_afastamento_sem_dia_para_acabar_termina_na_vespera_da_volta()
    {
        var antonio = TrabalhadoresDeTeste.Estivador();
        antonio.Impedir(new Impedimento(TipoDeImpedimento.Afastamento, new DateOnly(2026, 3, 10), observacao: "Cirurgia no joelho"));

        antonio.EncerrarAfastamento(new DateOnly(2026, 4, 21));

        var afastamento = Assert.Single(antonio.Impedimentos);
        Assert.Equal(new Impedimento(TipoDeImpedimento.Afastamento, new DateOnly(2026, 3, 10), new DateOnly(2026, 4, 20), "Cirurgia no joelho"), afastamento);

        var erro = Assert.Throws<ExcecaoDeDominio>(() => antonio.EncerrarAfastamento(new DateOnly(2026, 4, 22)));

        Assert.Equal("Antônio Ferreira não tem afastamento sem data para acabar.", erro.Message);
    }

    [Fact]
    public void A_volta_e_depois_do_primeiro_dia_do_afastamento()
    {
        var antonio = TrabalhadoresDeTeste.Estivador();
        antonio.Impedir(new Impedimento(TipoDeImpedimento.Afastamento, new DateOnly(2026, 3, 10)));

        var erro = Assert.Throws<ExcecaoDeDominio>(() => antonio.EncerrarAfastamento(new DateOnly(2026, 3, 10)));

        Assert.Equal("A volta, em 10/03/2026, tem de ser depois do primeiro dia do afastamento, 10/03/2026.", erro.Message);

        antonio.EncerrarAfastamento(new DateOnly(2026, 3, 11));

        Assert.Equal((DateOnly?)new DateOnly(2026, 3, 10), antonio.Impedimentos[0].Ate);
    }
}
