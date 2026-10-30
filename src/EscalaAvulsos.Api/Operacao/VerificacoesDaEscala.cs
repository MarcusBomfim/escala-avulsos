using System.Collections.Concurrent;
using EscalaAvulsos.Api.TempoReal;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EscalaAvulsos.Api.Operacao;

/// <summary>
/// A última volta de cada laço que roda sozinho no servidor — a entrega dos
/// recados e a vigia dos prazos —, para a saúde saber se eles giram.
/// </summary>
public sealed class VoltasDosLacos(TimeProvider relogio)
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> _voltas = new();

    private readonly DateTimeOffset _subiuEm = relogio.GetUtcNow();

    /// <summary>O laço deu mais uma volta, tenha ela entregado ou conferido alguma coisa ou não.</summary>
    public void Girou(string laco) => _voltas[laco] = relogio.GetUtcNow();

    /// <summary>Há quanto tempo o laço não dá uma volta: desde a última, ou desde que o servidor subiu, se ele nunca deu nenhuma.</summary>
    public TimeSpan Parado(string laco) => relogio.GetUtcNow() - _voltas.GetValueOrDefault(laco, _subiuEm);
}

/// <summary>
/// Viva: cada laço deu uma volta há pouco. O laço que parou — preso numa
/// espera que não volta, ou morto por um erro que escapou — não entrega mais
/// recado nenhum, e reiniciar o processo o põe para girar de novo.
/// </summary>
/// <remarks>
/// A tolerância é de três intervalos e mais um minuto: uma volta lenta, com
/// o serviço de push demorando os dez segundos que pode demorar, não derruba
/// ninguém.
/// </remarks>
internal sealed class LacosGirando(VoltasDosLacos voltas, IReadOnlyDictionary<string, TimeSpan> intervalos) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var parados = intervalos
            .Where(laco => voltas.Parado(laco.Key) > (laco.Value * 3) + TimeSpan.FromMinutes(1))
            .Select(laco => laco.Key)
            .Order(StringComparer.Ordinal)
            .ToList();

        return Task.FromResult(parados.Count == 0
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy($"Parou de girar: {string.Join(", ", parados)}."));
    }
}

/// <summary>
/// Pronta, mas pela metade: sem a escuta do banco, a API continua lendo e
/// gravando, mas as telas abertas não se atualizam sozinhas, e a entrega dos
/// recados só acorda no intervalo. Tirar o servidor do ar por isso seria
/// pior: a escuta volta sozinha, com espera crescente (ADR-0020).
/// </summary>
internal sealed class EscutaDePe(CentralDeAnuncios central) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(central.Escutando
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Degraded("A escuta do banco caiu: as telas não se atualizam sozinhas até ela voltar."));
}
