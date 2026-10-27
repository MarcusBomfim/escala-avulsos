using EscalaAvulsos.Dominio.Comum;
using EscalaAvulsos.Dominio.Identificacao;

namespace EscalaAvulsos.Dominio.Testes.Identificacao;

public sealed class MatriculaTests
{
    [Theory]
    [InlineData("4127", 4127)]
    [InlineData("004127", 4127)]
    [InlineData(" 4127 ", 4127)]
    [InlineData("0000001", 1)]
    [InlineData("9999999", 9_999_999)]
    [InlineData("00009999999", 9_999_999)]
    public void Le_o_numero_com_ou_sem_zeros_a_esquerda(string texto, int numero)
    {
        var matricula = Matricula.De(texto);

        Assert.Equal(numero, matricula.Numero);
        Assert.Equal(Matricula.De(numero), matricula);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("41a7")]
    [InlineData("4.127")]
    [InlineData("-4127")]
    [InlineData("+4127")]
    [InlineData("4127/0")]
    [InlineData("٤١٢٧")]
    public void Recusa_o_que_nao_e_numero(string texto)
    {
        var erro = Assert.Throws<ExcecaoDeDominio>(() => Matricula.De(texto));

        Assert.EndsWith("não é uma matrícula: a matrícula é um número, só com dígitos.", erro.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("0000")]
    public void A_matricula_comeca_em_1(string texto)
    {
        var erro = Assert.Throws<ExcecaoDeDominio>(() => Matricula.De(texto));

        Assert.Equal("A matrícula vai de 1 a 9999999: 0 não é matrícula.", erro.Message);
    }

    [Fact]
    public void Tem_no_maximo_sete_digitos()
    {
        var erro = Assert.Throws<ExcecaoDeDominio>(() => Matricula.De("12345678"));

        Assert.Equal("A matrícula 12345678 tem mais de 7 dígitos: confira o número.", erro.Message);
        Assert.Throws<ExcecaoDeDominio>(() => Matricula.De(10_000_000));
        Assert.Throws<ExcecaoDeDominio>(() => Matricula.De(-1));
    }

    [Fact]
    public void Compara_como_numero_e_nao_como_texto()
    {
        string[] escritas = ["100", "99", "1000", "0098", "4127"];

        var matriculas = escritas.Select(Matricula.De).Order().Select(matricula => matricula.ToString());

        Assert.Equal(["98", "99", "100", "1000", "4127"], matriculas);
        Assert.True(Matricula.De(99) < Matricula.De(100));
        Assert.True(Matricula.De(100) >= Matricula.De("0100"));
        Assert.False(Matricula.De(100) > Matricula.De(100));
        Assert.True(Matricula.De(7) <= Matricula.De(7));
    }
}
