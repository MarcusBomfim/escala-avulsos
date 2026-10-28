using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace EscalaAvulsos.Api.Seguranca;

/// <summary>Autenticação por JWT e as políticas de autorização (ADR-0014).</summary>
internal static class ServicosDeSeguranca
{
    /// <remarks>
    /// <para>
    /// Quem emite o token não é a API: um provedor de identidade, configurado
    /// em <c>Authentication:Schemes:Bearer</c> — em desenvolvimento, o
    /// <c>dotnet user-jwts</c>. É ele que confere a matrícula e a senha do
    /// trabalhador; a API não guarda senha, e só confere assinatura, emissor,
    /// audiência e validade.
    /// </para>
    /// <para>
    /// A política de reserva exige um dos três papéis: endpoint que esquecer
    /// de declarar a sua ainda exige ao menos isso. Anônimo, só o documento
    /// OpenAPI.
    /// </para>
    /// </remarks>
    public static IServiceCollection AdicionarSeguranca(this IServiceCollection servicos)
    {
        servicos
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opcoes =>
            {
                // As reivindicações com os nomes do token — role, matricula —,
                // sem a tradução para os URIs longos do WS-Federation.
                opcoes.MapInboundClaims = false;
                opcoes.TokenValidationParameters.NameClaimType = Reivindicacoes.Nome;
                opcoes.TokenValidationParameters.RoleClaimType = Reivindicacoes.Papel;
            });

        var comPapel = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
            .RequireAuthenticatedUser()
            .RequireRole(Papeis.Trabalhador, Papeis.Escalador, Papeis.Operador)
            .Build();

        servicos.AddAuthorizationBuilder()
            .SetDefaultPolicy(comPapel)
            .SetFallbackPolicy(comPapel)
            .AddPolicy(Politicas.Trabalhador, politica => politica
                .Combine(comPapel)
                .RequireRole(Papeis.Trabalhador)
                .RequireAssertion(contexto => QuemPede.LerMatricula(contexto.User) is not null))
            .AddPolicy(Politicas.Escalar, politica => politica.Combine(comPapel).RequireRole(Papeis.Escalador))
            .AddPolicy(Politicas.Requisitar, politica => politica
                .Combine(comPapel)
                .RequireRole(Papeis.Operador)
                .RequireAssertion(contexto => QuemPede.LerCnpj(contexto.User) is not null));

        return servicos;
    }
}
