using System.Collections.Concurrent;
using System.Threading.Channels;
using EscalaAvulsos.Infraestrutura.TempoReal;

namespace EscalaAvulsos.Api.TempoReal;

/// <summary>
/// O que chega a quem está escutando o servidor: um anúncio do banco, ou o
/// aviso de que a escuta do banco recomeçou — e o que mudou no meio, quem
/// escuta precisa ler de novo (ADR-0020).
/// </summary>
public sealed record SinalDaCentral(AnuncioDoBanco? Anuncio)
{
    public static SinalDaCentral Recomecou { get; } = new(Anuncio: null);
}

/// <summary>
/// Repassa os anúncios do banco a cada um que está escutando este servidor:
/// a ligação de cada aplicativo aberto, e a entrega dos avisos. Cada um tem a
/// sua fila, curta: quem não acompanha perde os mais velhos, e não segura os
/// outros.
/// </summary>
public sealed class CentralDeAnuncios
{
    /// <summary>Quantos sinais esperam, por ouvinte, antes de os mais velhos irem embora.</summary>
    private const int FilaDeCadaUm = 256;

    private readonly ConcurrentDictionary<Guid, Channel<SinalDaCentral>> _ouvintes = new();

    /// <summary>Se a escuta do banco está de pé: sem ela, ninguém recebe nada, e quem escuta só sabe ao ler de novo.</summary>
    public bool Escutando { get; private set; }

    public Ouvinte Escutar()
    {
        var fila = Channel.CreateBounded<SinalDaCentral>(new BoundedChannelOptions(FilaDeCadaUm)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
        });
        var id = Guid.NewGuid();
        _ouvintes[id] = fila;

        return new Ouvinte(fila.Reader, () =>
        {
            if (_ouvintes.TryRemove(id, out var removida))
            {
                removida.Writer.TryComplete();
            }
        });
    }

    public void Publicar(AnuncioDoBanco anuncio)
    {
        ArgumentNullException.ThrowIfNull(anuncio);

        Repassar(new SinalDaCentral(anuncio));
    }

    /// <summary>A escuta do banco ficou de pé — de novo, ou pela primeira vez.</summary>
    public void Recomecar()
    {
        Escutando = true;
        Repassar(SinalDaCentral.Recomecou);
    }

    /// <summary>A escuta do banco caiu: até ela voltar, nenhum anúncio chega.</summary>
    public void Parar() => Escutando = false;

    private void Repassar(SinalDaCentral sinal)
    {
        foreach (var fila in _ouvintes.Values)
        {
            fila.Writer.TryWrite(sinal);
        }
    }

    /// <summary>Um ouvinte da central: a fila dele, e o fim da escuta, ao descartar.</summary>
    public sealed class Ouvinte(ChannelReader<SinalDaCentral> sinais, Action sair) : IDisposable
    {
        public ChannelReader<SinalDaCentral> Sinais { get; } = sinais;

        public void Dispose() => sair();
    }
}

/// <summary>
/// Escuta o banco enquanto o servidor estiver de pé, e repassa à central o
/// que ele anuncia. A conexão que cai é aberta de novo, esperando cada vez
/// mais — de um segundo até trinta —, para não martelar um banco que está
/// voltando.
/// </summary>
internal sealed partial class EscutaDoBanco(OuvinteDoBanco ouvinte, CentralDeAnuncios central, ILogger<EscutaDoBanco> registro) : BackgroundService
{
    private static readonly TimeSpan PrimeiraEspera = TimeSpan.FromSeconds(1);

    private static readonly TimeSpan MaiorEspera = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var espera = PrimeiraEspera;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await foreach (var anuncio in ouvinte.Ouvir(() => { central.Recomecar(); espera = PrimeiraEspera; }, stoppingToken))
                {
                    central.Publicar(anuncio);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception erro) when (erro is not OperationCanceledException)
            {
                central.Parar();
                EscutaCaiu(erro, espera);
            }

            try
            {
                await Task.Delay(espera, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            espera = TimeSpan.FromTicks(Math.Min(espera.Ticks * 2, MaiorEspera.Ticks));
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "A escuta do banco caiu; tenta de novo em {Espera}.")]
    private partial void EscutaCaiu(Exception erro, TimeSpan espera);
}
