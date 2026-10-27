using EscalaAvulsos.Dominio.Identificacao;
using EscalaAvulsos.Dominio.Periodos;
using EscalaAvulsos.Dominio.Requisicoes;
using EscalaAvulsos.Dominio.Testes.Trabalhadores;

namespace EscalaAvulsos.Dominio.Testes.Requisicoes;

/// <summary>A requisição dos exemplos: dois ternos de estiva para a manhã de quinta, 05/11/2026, pedidos na quarta às 10h.</summary>
internal static class RequisicoesDeTeste
{
    public static readonly OperadorPortuario CaisForte = new("Cais Forte Operações Portuárias", Cnpj.De("11.222.333/0001-81"));

    public static readonly Navio AtlanticoSul = new("Atlântico Sul", NumeroImo.De("IMO 9876543"));

    public static readonly LocalDaOperacao CaisLeste = new("Terminal Cais Leste", "4");

    public static readonly Periodo QuintaDeManha = Periodo.De(new DateOnly(2026, 11, 5), Turno.Manha);

    public static readonly DateTimeOffset QuartaAs10h = new(2026, 11, 4, 10, 0, 0, TimeSpan.FromHours(-3));

    public static readonly DadosDaRequisicao Dados = new(CaisForte, AtlanticoSul, CaisLeste, QuintaDeManha);

    /// <summary>Um terno de porão de navio de contêineres: contramestre, sinaleiro, operador de guindaste e oito estivadores.</summary>
    public static VagasDaFuncao[] TernoDeConteiner() =>
    [
        new(Funcoes.ContramestreDeTerno, 1),
        new(Funcoes.Sinaleiro, 1),
        new(Funcoes.OperadorDeGuindaste, 1),
        new(Funcoes.Estivador, 8),
    ];

    public static Requisicao DoisTernos() => new(Dados, [TernoDeConteiner(), TernoDeConteiner()], QuartaAs10h);
}
