using EscalaAvulsos.Api.Hospedagem;
using EscalaAvulsos.Dominio.Avisos;
using EscalaAvulsos.Dominio.Trabalhadores;
using EscalaAvulsos.Infraestrutura.Persistencia.Repositorios;

namespace EscalaAvulsos.Api.Avisos;

/// <summary>
/// Confere o que o tempo traz (ADR-0019): a habilitação que vai fechar sem
/// quem pode trabalhar, e o exame e a reciclagem que vão vencer. Quem decide
/// quem avisar é o domínio; daqui sai só a hora de perguntar e a gravação. O
/// aviso que já existe não se repete — a chave dele não deixa.
/// </summary>
public sealed class VigiaDosPrazos(IServiceScopeFactory escopos, ConfiguracaoDosAvisos configuracao, TimeProvider relogio)
{
    /// <summary>Até onde olhar as escalações: o fechamento de qualquer uma que começa dentro de um dia.</summary>
    private static readonly TimeSpan Horizonte = TimeSpan.FromDays(1);

    /// <summary>As habilitações que fecham logo: avisa quem ainda não se habilitou e pode trabalhar.</summary>
    /// <returns>Quantos avisos novos.</returns>
    public async Task<int> ConferirAsHabilitacoes(CancellationToken cancelamento = default)
    {
        await using var escopo = escopos.CreateAsyncScope();
        var escalacoes = escopo.ServiceProvider.GetRequiredService<RepositorioDeEscalacoes>();
        var trabalhadores = escopo.ServiceProvider.GetRequiredService<RepositorioDeTrabalhadores>();
        var avisos = escopo.ServiceProvider.GetRequiredService<RepositorioDeAvisos>();

        var agora = relogio.GetUtcNow();
        var abertas = (await escalacoes.QueComecamEntre(agora, agora + Horizonte, cancelamento))
            .Where(escalacao => escalacao.HabilitacaoAbertaEm(agora) && escalacao.FechamentoDaHabilitacao - agora <= configuracao.AntecedenciaDoFechamento)
            .ToList();

        var novos = 0;
        var daCategoria = new Dictionary<Categoria, IReadOnlyList<Trabalhador>>();

        foreach (var escalacao in abertas)
        {
            if (!daCategoria.TryGetValue(escalacao.Categoria, out var deles))
            {
                deles = await trabalhadores.DaCategoria(escalacao.Categoria, cancelamento);
                daCategoria[escalacao.Categoria] = deles;
            }

            novos += await avisos.Guardar(
                AvisosDosPrazos.DaHabilitacaoQueVaiFechar(escalacao, deles, agora, configuracao.AntecedenciaDoFechamento),
                cancelamento);
        }

        return novos;
    }

    /// <summary>O exame e as reciclagens que vencem nos próximos dias, de todas as categorias.</summary>
    /// <returns>Quantos avisos novos.</returns>
    public async Task<int> ConferirOsVencimentos(CancellationToken cancelamento = default)
    {
        await using var escopo = escopos.CreateAsyncScope();
        var trabalhadores = escopo.ServiceProvider.GetRequiredService<RepositorioDeTrabalhadores>();
        var avisos = escopo.ServiceProvider.GetRequiredService<RepositorioDeAvisos>();

        var agora = relogio.GetUtcNow();
        var novos = 0;

        foreach (var categoria in Enum.GetValues<Categoria>())
        {
            var deles = await trabalhadores.DaCategoria(categoria, cancelamento);

            novos += await avisos.Guardar(
                [.. deles.SelectMany(trabalhador => AvisosDosPrazos.DosVencimentos(trabalhador, agora, configuracao.DiasAntesDoVencimento))],
                cancelamento);
        }

        return novos;
    }
}

/// <summary>A vigia rodando sozinha: as habilitações a cada intervalo, e os vencimentos de hora em hora.</summary>
internal sealed partial class VigiaDosPrazosSozinha(
    VigiaDosPrazos vigia,
    ConfiguracaoDosAvisos configuracao,
    TimeProvider relogio,
    ILogger<VigiaDosPrazosSozinha> registro) : BackgroundService
{
    private static readonly TimeSpan DosVencimentos = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        DateTimeOffset? vencimentosConferidosEm = null;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await vigia.ConferirAsHabilitacoes(stoppingToken);

                if (vencimentosConferidosEm is not { } ultima || relogio.GetUtcNow() - ultima >= DosVencimentos)
                {
                    await vigia.ConferirOsVencimentos(stoppingToken);
                    vencimentosConferidosEm = relogio.GetUtcNow();
                }
            }
            catch (Exception erro) when (erro is not OperationCanceledException)
            {
                ConferenciaFalhou(erro);
            }

            try
            {
                await Task.Delay(configuracao.IntervaloDaConferencia, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "A conferência dos prazos falhou; tenta de novo na próxima volta.")]
    private partial void ConferenciaFalhou(Exception erro);
}
