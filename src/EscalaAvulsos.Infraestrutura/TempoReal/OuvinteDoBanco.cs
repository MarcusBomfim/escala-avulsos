using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;
using EscalaAvulsos.Dominio.Trabalhadores;
using Npgsql;

namespace EscalaAvulsos.Infraestrutura.TempoReal;

/// <summary>De onde veio o anúncio: a tabela que mudou.</summary>
public enum OrigemDoAnuncio
{
    /// <summary>Alguém se habilitou, trocou de função ou desistiu da habilitação; a escalação abriu.</summary>
    Escalacoes,

    /// <summary>A escala saiu, ou mudou: desistência, presença, falta, encerramento.</summary>
    Escalas,

    /// <summary>O operador pediu, mudou ou cancelou uma requisição.</summary>
    Requisicoes,

    /// <summary>Um trabalhador ganhou um aviso novo.</summary>
    Avisos,
}

/// <summary>
/// O que o banco anunciou, quando a gravação foi confirmada: o período da
/// categoria que mudou, ou o trabalhador que tem aviso novo (ADR-0020). O
/// anúncio diz só onde olhar — o que mudou, quem quiser saber lê de novo.
/// </summary>
public sealed record AnuncioDoBanco(OrigemDoAnuncio Origem, Categoria? Categoria, DateTimeOffset? Inicio, TrabalhadorId? Trabalhador)
{
    /// <summary>O anúncio como o gatilho o escreve; nulo, se não for um que este sistema escreve.</summary>
    public static AnuncioDoBanco? Ler(string texto)
    {
        try
        {
            using var json = JsonDocument.Parse(texto);
            var raiz = json.RootElement;

            return raiz.GetProperty("tabela").GetString() switch
            {
                "avisos" => new AnuncioDoBanco(OrigemDoAnuncio.Avisos, null, null, new TrabalhadorId(raiz.GetProperty("trabalhador").GetGuid())),
                "escalacoes" => DoPeriodo(OrigemDoAnuncio.Escalacoes, raiz),
                "escalas" => DoPeriodo(OrigemDoAnuncio.Escalas, raiz),
                "requisicoes" => DoPeriodo(OrigemDoAnuncio.Requisicoes, raiz),
                _ => null,
            };
        }
        catch (Exception erro) when (erro is JsonException or KeyNotFoundException or InvalidOperationException or FormatException or ArgumentException)
        {
            return null;
        }
    }

    private static AnuncioDoBanco DoPeriodo(OrigemDoAnuncio origem, JsonElement raiz) =>
        new(origem, Enum.Parse<Categoria>(raiz.GetProperty("categoria").GetString()!), raiz.GetProperty("inicio").GetDateTimeOffset(), null);
}

/// <summary>
/// Escuta, numa conexão só dela, o canal em que os gatilhos do banco anunciam
/// o que mudou (<c>LISTEN escala_mudou</c>). Cada servidor da API escuta o
/// seu: o que um grava, todos ficam sabendo.
/// </summary>
/// <remarks>
/// Se a conexão cai, a escuta termina com a exceção, e quem escuta abre de
/// novo. O que mudou enquanto ninguém escutava não volta: quem depende dos
/// anúncios lê tudo de novo ao reabrir.
/// </remarks>
public sealed class OuvinteDoBanco(string conexao)
{
    public const string Canal = "escala_mudou";

    /// <summary>Os anúncios, um a um, a partir de quando a escuta está de pé — quando <paramref name="pronto"/> é chamado.</summary>
    /// <param name="pronto">Chamado quando o <c>LISTEN</c> foi aceito: daí em diante, nada se perde.</param>
    public async IAsyncEnumerable<AnuncioDoBanco> Ouvir(Action? pronto = null, [EnumeratorCancellation] CancellationToken cancelamento = default)
    {
        var anuncios = Channel.CreateUnbounded<AnuncioDoBanco>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });

        await using var ligacao = new NpgsqlConnection(new NpgsqlConnectionStringBuilder(conexao) { Pooling = false, KeepAlive = 30 }.ConnectionString);
        await ligacao.OpenAsync(cancelamento);

        ligacao.Notification += (_, notificacao) =>
        {
            if (notificacao.Channel == Canal && AnuncioDoBanco.Ler(notificacao.Payload) is { } anuncio)
            {
                anuncios.Writer.TryWrite(anuncio);
            }
        };

        await using (var comando = new NpgsqlCommand($"listen {Canal}", ligacao))
        {
            await comando.ExecuteNonQueryAsync(cancelamento);
        }

        pronto?.Invoke();

        using var parar = CancellationTokenSource.CreateLinkedTokenSource(cancelamento);
        var espera = Esperar(ligacao, anuncios.Writer, parar.Token);

        try
        {
            await foreach (var anuncio in anuncios.Reader.ReadAllAsync(cancelamento))
            {
                yield return anuncio;
            }
        }
        finally
        {
            await parar.CancelAsync();
            await espera;
        }
    }

    /// <summary>Fica esperando o banco falar; a conexão que cai encerra a leitura com o erro.</summary>
    private static async Task Esperar(NpgsqlConnection ligacao, ChannelWriter<AnuncioDoBanco> anuncios, CancellationToken cancelamento)
    {
        try
        {
            while (!cancelamento.IsCancellationRequested)
            {
                await ligacao.WaitAsync(cancelamento);
            }

            anuncios.TryComplete();
        }
        catch (OperationCanceledException) when (cancelamento.IsCancellationRequested)
        {
            anuncios.TryComplete();
        }
        catch (Exception erro) when (erro is NpgsqlException or IOException or ObjectDisposedException)
        {
            anuncios.TryComplete(erro);
        }
    }
}
