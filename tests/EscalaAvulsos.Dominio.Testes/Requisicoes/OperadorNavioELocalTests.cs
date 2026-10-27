using EscalaAvulsos.Dominio.Comum;
using EscalaAvulsos.Dominio.Identificacao;
using EscalaAvulsos.Dominio.Requisicoes;

namespace EscalaAvulsos.Dominio.Testes.Requisicoes;

public sealed class OperadorNavioELocalTests
{
    [Fact]
    public void O_operador_e_o_cnpj_dele_seja_qual_for_o_nome_escrito()
    {
        var caisForte = new OperadorPortuario(" Cais Forte Operações Portuárias ", Cnpj.De("11.222.333/0001-81"));
        var comLtda = new OperadorPortuario("Cais Forte Operações Portuárias Ltda.", Cnpj.De("11222333000181"));

        Assert.Equal("Cais Forte Operações Portuárias (11.222.333/0001-81)", caisForte.ToString());
        Assert.Equal(caisForte, comLtda);
        Assert.Equal(caisForte.GetHashCode(), comLtda.GetHashCode());
        Assert.NotEqual(caisForte, new OperadorPortuario("Cais Forte Operações Portuárias", Cnpj.De("11.222.333/0002-62")));
    }

    [Theory]
    [InlineData(null, "Falta o nome do operador portuário.")]
    [InlineData(" ", "Falta o nome do operador portuário.")]
    public void O_operador_precisa_de_nome(string? nome, string mensagem)
    {
        var erro = Assert.Throws<ExcecaoDeDominio>(() => new OperadorPortuario(nome!, Cnpj.De("11.222.333/0001-81")));

        Assert.Equal(mensagem, erro.Message);
    }

    [Fact]
    public void O_nome_do_operador_tem_no_maximo_cento_e_vinte_caracteres()
    {
        var erro = Assert.Throws<ExcecaoDeDominio>(() => new OperadorPortuario(new string('a', 121), Cnpj.De("11.222.333/0001-81")));

        Assert.Equal("O nome do operador portuário passa de 120 caracteres.", erro.Message);
    }

    [Fact]
    public void O_navio_e_o_numero_imo_mesmo_trocando_de_nome()
    {
        var atlanticoSul = new Navio(" Atlântico Sul ", NumeroImo.De("IMO 9876543"));
        var renomeado = new Navio("Southern Atlantic", NumeroImo.De("9876543"));

        Assert.Equal("Atlântico Sul (IMO 9876543)", atlanticoSul.ToString());
        Assert.Equal(atlanticoSul, renomeado);
        Assert.Equal(atlanticoSul.GetHashCode(), renomeado.GetHashCode());
        Assert.NotEqual(atlanticoSul, new Navio("Atlântico Sul", NumeroImo.De("IMO 9074729")));
    }

    [Fact]
    public void O_navio_precisa_de_nome_de_ate_oitenta_caracteres()
    {
        Assert.Equal("Falta o nome do navio.", Assert.Throws<ExcecaoDeDominio>(() => new Navio("", NumeroImo.De("9876543"))).Message);
        Assert.Equal(
            "O nome do navio passa de 80 caracteres.",
            Assert.Throws<ExcecaoDeDominio>(() => new Navio(new string('a', 81), NumeroImo.De("9876543"))).Message);
    }

    [Fact]
    public void O_local_e_o_terminal_e_o_berco_quando_se_sabe()
    {
        Assert.Equal("Terminal Cais Leste, berço 4", new LocalDaOperacao(" Terminal Cais Leste ", " 4 ").ToString());
        Assert.Equal("Terminal Cais Leste", new LocalDaOperacao("Terminal Cais Leste", "  ").ToString());
        Assert.Null(new LocalDaOperacao("Terminal Cais Leste").Berco);
    }

    [Fact]
    public void O_local_diz_tudo_o_que_falta_de_uma_vez()
    {
        var erro = Assert.Throws<ExcecaoDeDominio>(() => new LocalDaOperacao(" ", new string('9', 21)));

        Assert.Equal("Falta o terminal. O berço passa de 20 caracteres.", erro.Message);
        Assert.Equal(
            "O terminal passa de 80 caracteres.",
            Assert.Throws<ExcecaoDeDominio>(() => new LocalDaOperacao(new string('a', 81))).Message);
    }
}
