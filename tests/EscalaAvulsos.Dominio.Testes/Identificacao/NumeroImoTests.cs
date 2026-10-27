using EscalaAvulsos.Dominio.Comum;
using EscalaAvulsos.Dominio.Identificacao;

namespace EscalaAvulsos.Dominio.Testes.Identificacao;

public sealed class NumeroImoTests
{
    [Theory]
    [InlineData("IMO 9074729")]
    [InlineData("imo9074729")]
    [InlineData("9074729")]
    [InlineData(" IMO: 9074729 ")]
    [InlineData("IMO-9074729")]
    public void Le_com_ou_sem_o_prefixo(string texto)
    {
        var imo = NumeroImo.De(texto);

        Assert.Equal(("9074729", "IMO 9074729"), (imo.Numero, imo.ToString()));
    }

    [Fact]
    public void O_verificador_e_o_ultimo_algarismo_da_soma_ponderada()
    {
        // 9×7 + 8×6 + 7×5 + 6×4 + 5×3 + 4×2 = 193: o verificador é 3.
        Assert.Equal("9876543", NumeroImo.De("9876543").Numero);
        // 1×7 + 0×6 + 0×5 + 0×4 + 0×3 + 1×2 = 9.
        Assert.Equal("1000019", NumeroImo.De("1000019").Numero);
    }

    [Theory]
    [InlineData("IMO 9074728")]
    [InlineData("9074720")]
    [InlineData("9876534")]
    [InlineData("0000000")]
    public void Recusa_o_que_nao_confere(string texto)
    {
        var erro = Assert.Throws<ExcecaoDeDominio>(() => NumeroImo.De(texto));

        Assert.Equal($"O número IMO {texto} não confere: o último dígito não bate com os outros seis.", erro.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("907472")]
    [InlineData("90747290")]
    [InlineData("IMO 90747A9")]
    [InlineData("IMO")]
    [InlineData("MMSI 710000000")]
    public void Recusa_o_que_nao_tem_forma_de_numero_imo(string texto)
    {
        var erro = Assert.Throws<ExcecaoDeDominio>(() => NumeroImo.De(texto));

        Assert.Equal($"\"{texto.Trim()}\" não é um número IMO: são 7 dígitos, como IMO 9074729.", erro.Message);
    }

    [Fact]
    public void Numeros_iguais_sao_iguais_escritos_de_qualquer_jeito()
    {
        Assert.Equal(NumeroImo.De("IMO 9074729"), NumeroImo.De("9074729"));
        Assert.NotEqual(NumeroImo.De("9074729"), NumeroImo.De("9876543"));
    }
}
