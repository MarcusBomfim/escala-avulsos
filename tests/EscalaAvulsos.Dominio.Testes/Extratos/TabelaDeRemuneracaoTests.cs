using EscalaAvulsos.Dominio.Comum;
using EscalaAvulsos.Dominio.Extratos;
using EscalaAvulsos.Dominio.Testes.Trabalhadores;
using EscalaAvulsos.Dominio.Trabalhadores;

namespace EscalaAvulsos.Dominio.Testes.Extratos;

public sealed class TabelaDeRemuneracaoTests
{
    [Fact]
    public void Da_o_valor_do_periodo_de_cada_funcao()
    {
        var tabela = new TabelaDeRemuneracao([new(Funcoes.Estivador, Dinheiro.Reais(220m))], 0.20m);

        Assert.Equal(Dinheiro.Reais(220m), tabela.ValorDe(new Funcao(Categoria.Estiva, "ESTIVADOR")));
        Assert.Equal(0.20m, tabela.AdicionalNoturno);
        Assert.Equal([new ValorDaFuncao(Funcoes.Estivador, Dinheiro.Reais(220m))], tabela.Valores);
    }

    [Fact]
    public void A_funcao_que_nao_esta_na_tabela_e_recusada()
    {
        var tabela = new TabelaDeRemuneracao([new(Funcoes.Estivador, Dinheiro.Reais(220m))], 0.20m);

        var erro = Assert.Throws<ExcecaoDeDominio>(() => tabela.ValorDe(Funcoes.Sinaleiro));

        Assert.Equal("A tabela de remuneração não tem o valor de sinaleiro.", erro.Message);
    }

    [Fact]
    public void Diz_tudo_o_que_esta_errado_de_uma_vez()
    {
        var erro = Assert.Throws<ExcecaoDeDominio>(() => new TabelaDeRemuneracao(
            [
                new(Funcoes.Estivador, Dinheiro.Zero),
                new(Funcoes.Sinaleiro, Dinheiro.Reais(250.005m)),
                new(Funcoes.ContramestreDeTerno, Dinheiro.Reais(-10m)),
                new(new Funcao(Categoria.Estiva, "sinaleiro"), Dinheiro.Reais(260m)),
            ],
            1.5m));

        Assert.Equal(
            "O valor do período de estivador tem de ser positivo e em centavos, e seria R$ 0,00. "
            + "O valor do período de sinaleiro tem de ser positivo e em centavos, e seria R$ 250,005. "
            + "O valor do período de contramestre de terno tem de ser positivo e em centavos, e seria -R$ 10,00. "
            + "A tabela repete o valor de sinaleiro. "
            + "O adicional noturno vai de 0% a 100%, e seria 150%.",
            erro.Message);
    }

    [Fact]
    public void Precisa_de_pelo_menos_uma_funcao_e_aceita_os_limites_do_adicional()
    {
        Assert.Equal(
            "A tabela precisa do valor de pelo menos uma função. O adicional noturno vai de 0% a 100%, e seria -5%.",
            Assert.Throws<ExcecaoDeDominio>(() => new TabelaDeRemuneracao([], -0.05m)).Message);

        _ = new TabelaDeRemuneracao([new(Funcoes.Estivador, Dinheiro.Reais(0.01m))], 0m);
        _ = new TabelaDeRemuneracao([new(Funcoes.Estivador, Dinheiro.Reais(0.01m))], 1m);
    }
}
