using System.Security.Claims;
using EscalaAvulsos.Dominio.Comum;
using EscalaAvulsos.Dominio.Identificacao;
using EscalaAvulsos.Dominio.Requisicoes;

namespace EscalaAvulsos.Api.Seguranca;

/// <summary>
/// Quem fez o pedido, lido do token: a matrícula do trabalhador, o CNPJ e o
/// nome do operador. O cliente não diz em nome de quem pede (ADR-0014).
/// </summary>
/// <remarks>
/// As políticas já recusam o token sem a reivindicação válida, então dentro
/// de um endpoint ela sempre se lê.
/// </remarks>
internal static class QuemPede
{
    public static Matricula Matricula(ClaimsPrincipal usuario) =>
        LerMatricula(usuario) ?? throw new InvalidOperationException("O token passou pela política do trabalhador sem uma matrícula válida.");

    public static OperadorPortuario Operador(ClaimsPrincipal usuario)
    {
        ArgumentNullException.ThrowIfNull(usuario);

        var cnpj = LerCnpj(usuario) ?? throw new InvalidOperationException("O token passou pela política do operador sem um CNPJ válido.");

        return new OperadorPortuario(usuario.FindFirstValue(Reivindicacoes.Nome) ?? cnpj.Formatado, cnpj);
    }

    /// <summary>A matrícula do token, se houver uma válida.</summary>
    public static Matricula? LerMatricula(ClaimsPrincipal? usuario) =>
        Ler(usuario, Reivindicacoes.Matricula, texto => Dominio.Identificacao.Matricula.De(texto));

    /// <summary>O CNPJ do token, se houver um válido.</summary>
    public static Cnpj? LerCnpj(ClaimsPrincipal? usuario) => Ler(usuario, Reivindicacoes.Cnpj, Cnpj.De);

    private static T? Ler<T>(ClaimsPrincipal? usuario, string reivindicacao, Func<string, T> leitura)
        where T : class
    {
        if (usuario?.FindFirstValue(reivindicacao) is not { Length: > 0 } texto)
        {
            return null;
        }

        try
        {
            return leitura(texto);
        }
        catch (ExcecaoDeDominio)
        {
            return null;
        }
    }
}
