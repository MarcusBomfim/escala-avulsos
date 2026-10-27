using EscalaAvulsos.Dominio.Identificacao;
using EscalaAvulsos.Dominio.Trabalhadores;

namespace EscalaAvulsos.Dominio.Testes.Trabalhadores;

/// <summary>As funções dos exemplos: as da estiva num navio de contêineres, e uma de outra categoria.</summary>
internal static class Funcoes
{
    public static readonly Funcao Estivador = new(Categoria.Estiva, "Estivador");

    public static readonly Funcao Sinaleiro = new(Categoria.Estiva, "Sinaleiro");

    public static readonly Funcao ContramestreDeTerno = new(Categoria.Estiva, "Contramestre de terno");

    public static readonly Funcao OperadorDeGuindaste = new(Categoria.Estiva, "Operador de guindaste de bordo");

    public static readonly Funcao Conferente = new(Categoria.ConferenciaDeCarga, "Conferente");
}

internal static class TrabalhadoresDeTeste
{
    /// <summary>
    /// Um estivador qualificado como estivador desde 1994, com o exame médico
    /// válido até 30/11/2026.
    /// </summary>
    public static Trabalhador Estivador(int matricula = 4127, string nome = "Antônio Ferreira", Inscricao inscricao = Inscricao.Registrado)
    {
        var trabalhador = new Trabalhador(new DadosDoTrabalhador(
            Matricula.De(matricula), nome, Cpf.De("529.982.247-25"), Categoria.Estiva, inscricao));

        trabalhador.Qualificar(Funcoes.Estivador, new DateOnly(1994, 3, 1));
        trabalhador.RegistrarAso(new Aso(new DateOnly(2025, 12, 1), new DateOnly(2026, 11, 30)));

        return trabalhador;
    }
}
