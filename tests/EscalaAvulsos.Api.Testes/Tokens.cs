using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using EscalaAvulsos.Api.Seguranca;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace EscalaAvulsos.Api.Testes;

/// <summary>
/// Os tokens dos testes, emitidos aqui mesmo com uma chave gerada a cada
/// execução — no lugar do provedor de identidade, que é quem confere a
/// matrícula e a senha.
/// </summary>
internal static class Tokens
{
    public const string Emissor = "https://identidade.testes";

    public const string Audiencia = "escala-avulsos";

    public static readonly SymmetricSecurityKey Chave = new(RandomNumberGenerator.GetBytes(32));

    /// <summary>O token do trabalhador da matrícula.</summary>
    public static string Trabalhador(int matricula) =>
        Emitir([new(Reivindicacoes.Papel, Papeis.Trabalhador), new(Reivindicacoes.Matricula, matricula.ToString(CultureInfo.InvariantCulture))]);

    /// <summary>O token de alguém do órgão gestor.</summary>
    public static string Escalador() => Emitir([new(Reivindicacoes.Papel, Papeis.Escalador), new(Reivindicacoes.Nome, "Escalador dos testes")]);

    /// <summary>O token do operador portuário do CNPJ.</summary>
    public static string Operador(string cnpj, string nome) =>
        Emitir([new(Reivindicacoes.Papel, Papeis.Operador), new(Reivindicacoes.Cnpj, cnpj), new(Reivindicacoes.Nome, nome)]);

    /// <summary>Um token com as reivindicações dadas, válido por uma hora — ou como o teste quiser.</summary>
    public static string Emitir(
        IEnumerable<Claim> reivindicacoes,
        SecurityKey? chave = null,
        string emissor = Emissor,
        string audiencia = Audiencia,
        DateTime? venceEm = null)
    {
        var vencimento = venceEm ?? DateTime.UtcNow.AddHours(1);

        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = emissor,
            Audience = audiencia,
            Subject = new ClaimsIdentity([new Claim("sub", Guid.NewGuid().ToString()), .. reivindicacoes]),
            IssuedAt = vencimento.AddHours(-2),
            NotBefore = vencimento.AddHours(-2),
            Expires = vencimento,
            SigningCredentials = new SigningCredentials(chave ?? Chave, SecurityAlgorithms.HmacSha256),
        });
    }
}
