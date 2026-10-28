using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EscalaAvulsos.Infraestrutura.Persistencia;

/// <summary>O banco e os repositórios, para quem hospeda a persistência — a API.</summary>
public static class ServicosDePersistencia
{
    /// <summary>Os namespaces de onde os repositórios entram sozinhos: repositório novo não mexe aqui.</summary>
    private static readonly string[] DeOndeVem =
    [
        $"{typeof(ContextoDoBanco).Namespace}.Repositorios",
        $"{typeof(ContextoDoBanco).Namespace}.Idempotencia",
    ];

    /// <summary>
    /// Um contexto por pedido, e cada repositório no mesmo escopo — o que um
    /// pedido grava em vários repositórios cabe numa transação só.
    /// </summary>
    public static IServiceCollection AdicionarPersistencia(this IServiceCollection servicos, string conexao)
    {
        ArgumentNullException.ThrowIfNull(servicos);
        ArgumentException.ThrowIfNullOrWhiteSpace(conexao);

        servicos.AddDbContext<ContextoDoBanco>(opcoes => ContextoDoBanco.Configurar(opcoes, conexao));
        servicos.TryAddSingleton(TimeProvider.System);

        // Repositório é quem trabalha com o contexto do banco; os records que eles devolvem moram ao lado, e não entram.
        var repositorios = typeof(ContextoDoBanco).Assembly
            .GetExportedTypes()
            .Where(tipo => tipo is { IsClass: true, IsAbstract: false } && DeOndeVem.Contains(tipo.Namespace))
            .Where(tipo => tipo.GetConstructors().Any(construtor => construtor.GetParameters().Any(parametro => parametro.ParameterType == typeof(ContextoDoBanco))));

        foreach (var repositorio in repositorios)
        {
            servicos.AddScoped(repositorio);
        }

        return servicos;
    }
}
