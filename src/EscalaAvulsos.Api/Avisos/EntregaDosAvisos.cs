using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Buffers.Text;
using EscalaAvulsos.Api.Hospedagem;
using EscalaAvulsos.Api.TempoReal;
using EscalaAvulsos.Dominio.Avisos;
using EscalaAvulsos.Infraestrutura.Avisos;
using EscalaAvulsos.Infraestrutura.Persistencia.Repositorios;
using EscalaAvulsos.Infraestrutura.TempoReal;

namespace EscalaAvulsos.Api.Avisos;

/// <summary>O que o service worker do aplicativo recebe: o que mostrar, e o que abrir quando o trabalhador toca.</summary>
/// <param name="Etiqueta">De que fato é: a notificação nova do mesmo fato substitui a antiga na tela do celular.</param>
public sealed record ConteudoDoPush(string Titulo, string Texto, string Endereco, string Etiqueta);

/// <summary>
/// Leva os avisos guardados aos celulares de cada trabalhador (ADR-0020):
/// reserva os que faltam entregar, manda a cada navegador inscrito, apaga a
/// inscrição que venceu, e dá por entregue o aviso que chegou a todos — ou o
/// de quem não tem celular inscrito. O que o serviço de push recusou fica para
/// a próxima volta, depois que a reserva vence.
/// </summary>
public sealed partial class EntregaDosAvisos(
    IServiceScopeFactory escopos,
    ConfiguracaoDosAvisos configuracao,
    IHttpClientFactory clientes,
    TimeProvider relogio,
    ILogger<EntregaDosAvisos> registro)
{
    /// <summary>Por quanto tempo os avisos reservados ficam com este servidor.</summary>
    public static readonly TimeSpan Reserva = TimeSpan.FromMinutes(2);

    private const int DeCadaVez = 50;

    /// <summary>Entrega o que está esperando, de cinquenta em cinquenta, até não sobrar nada livre.</summary>
    /// <returns>Quantos avisos foram dados por entregues.</returns>
    public async Task<int> Entregar(CancellationToken cancelamento = default)
    {
        var entregues = 0;

        while (!cancelamento.IsCancellationRequested)
        {
            await using var escopo = escopos.CreateAsyncScope();
            var avisos = escopo.ServiceProvider.GetRequiredService<RepositorioDeAvisos>();
            var inscricoes = escopo.ServiceProvider.GetRequiredService<RepositorioDeInscricoesDePush>();

            var reservados = await avisos.Reservar(DeCadaVez, Reserva, cancelamento);

            foreach (var reservado in reservados)
            {
                if (configuracao.Push is not { } push || await EntregarA(reservado.Aviso, push, inscricoes, cancelamento))
                {
                    await avisos.Entregue(reservado.Aviso, cancelamento);
                    entregues++;
                }
            }

            if (reservados.Count < DeCadaVez)
            {
                break;
            }
        }

        return entregues;
    }

    /// <summary>O conteúdo da notificação de um aviso: o texto dele e a tela que o toque abre.</summary>
    public static ConteudoDoPush Conteudo(Aviso aviso)
    {
        ArgumentNullException.ThrowIfNull(aviso);

        var endereco = aviso.Tipo switch
        {
            TipoDoAviso.HabilitacaoVaiFechar => "/habilitacao",
            TipoDoAviso.ExameVaiVencer or TipoDoAviso.ReciclagemVaiVencer => "/meus-dados",
            _ => "/",
        };

        return new ConteudoDoPush(aviso.Titulo, aviso.Texto, endereco, aviso.Chave);
    }

    /// <summary>Manda a todos os navegadores do trabalhador; verdadeiro se nenhum ficou para depois.</summary>
    private async Task<bool> EntregarA(Aviso aviso, ConfiguracaoDoPush push, RepositorioDeInscricoesDePush inscricoes, CancellationToken cancelamento)
    {
        var carteiro = new CarteiroDoPush(clientes.CreateClient(nameof(EntregaDosAvisos)), push, relogio);
        var mensagem = Mensagem(aviso);
        var todos = true;

        foreach (var inscricao in await inscricoes.DoTrabalhador(aviso.Trabalhador, cancelamento))
        {
            ResultadoDoPush resultado;

            try
            {
                resultado = await carteiro.Entregar(inscricao, mensagem, cancelamento);
            }
            catch (HttpRequestException erro)
            {
                ServicoForaDoAr(erro, inscricao.Endereco.Host);
                resultado = ResultadoDoPush.Recusado;
            }

            if (resultado == ResultadoDoPush.InscricaoVencida)
            {
                await inscricoes.Remover(inscricao.Endereco, cancelamento: cancelamento);
            }

            todos &= resultado != ResultadoDoPush.Recusado;
        }

        return todos;
    }

    /// <summary>A pressa e o prazo de cada aviso: a habilitação que vai fechar não serve depois de fechada.</summary>
    private static MensagemDoPush Mensagem(Aviso aviso)
    {
        var (validade, urgencia) = aviso.Tipo switch
        {
            TipoDoAviso.HabilitacaoVaiFechar => (TimeSpan.FromHours(1), UrgenciaDoPush.Alta),
            TipoDoAviso.VaiTrabalhar or TipoDoAviso.FicouDeFora or TipoDoAviso.EntrouPelaEspera => (TimeSpan.FromHours(12), UrgenciaDoPush.Alta),
            _ => (TimeSpan.FromDays(3), UrgenciaDoPush.Normal),
        };

        return new MensagemDoPush(JsonSerializer.SerializeToUtf8Bytes(Conteudo(aviso), JsonSerializerOptions.Web), validade, urgencia, Topico(aviso.Chave));
    }

    /// <summary>O tópico do serviço de push: até 32 caracteres do alfabeto do base64url — o resumo da chave do aviso.</summary>
    private static string Topico(string chave) => Base64Url.EncodeToString(SHA256.HashData(Encoding.UTF8.GetBytes(chave)).AsSpan(0, 24));

    [LoggerMessage(Level = LogLevel.Warning, Message = "O serviço de push de {Servico} não respondeu; o aviso fica para a próxima volta.")]
    private partial void ServicoForaDoAr(Exception erro, string servico);
}

/// <summary>
/// A entrega rodando sozinha no servidor: acorda a cada aviso novo que o
/// banco anuncia, e de tempos em tempos, para o que ficou para depois.
/// </summary>
internal sealed partial class EntregaDosAvisosSozinha(
    EntregaDosAvisos entrega,
    CentralDeAnuncios central,
    ConfiguracaoDosAvisos configuracao,
    ILogger<EntregaDosAvisosSozinha> registro) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var ouvinte = central.Escutar();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await entrega.Entregar(stoppingToken);
            }
            catch (Exception erro) when (erro is not OperationCanceledException)
            {
                EntregaFalhou(erro);
            }

            try
            {
                using var espera = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                espera.CancelAfter(configuracao.IntervaloDaEntrega);

                // Até chegar um aviso novo — ou a escuta recomeçar, e algum ter passado sem anúncio —, ou dar o intervalo.
                while ((await ouvinte.Sinais.ReadAsync(espera.Token)).Anuncio is { Origem: not OrigemDoAnuncio.Avisos })
                {
                }
            }
            catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
            {
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "A entrega dos avisos falhou; tenta de novo na próxima volta.")]
    private partial void EntregaFalhou(Exception erro);
}
