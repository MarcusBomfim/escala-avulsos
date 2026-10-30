using System.Text.Json;
using EscalaAvulsos.Api.Recursos.Escalacoes;
using EscalaAvulsos.Api.Recursos.Eventos;
using EscalaAvulsos.Api.TempoReal;
using EscalaAvulsos.Dominio.Periodos;
using EscalaAvulsos.Dominio.Trabalhadores;
using EscalaAvulsos.Infraestrutura.Testes.Persistencia;
using EscalaAvulsos.Infraestrutura.TempoReal;
using Microsoft.Extensions.DependencyInjection;
using static EscalaAvulsos.Api.Testes.Novembro;
using static EscalaAvulsos.Api.Testes.TardeDeQuintaPelaApi;

namespace EscalaAvulsos.Api.Testes;

/*
 * Os eventos do servidor, como o aplicativo aberto os lê: pronto ao
 * conectar, e um "mudou" a cada anúncio do banco que é de quem está
 * conectado — o trabalhador, as escalas da categoria dele e os avisos dele;
 * o escalador, tudo o que mexe no quadro (ADR-0020).
 */
public sealed class EventosTests(BancoDeTestes bancos) : TestesDaApi(bancos)
{
    private static readonly TimeSpan Prazo = TimeSpan.FromSeconds(10);

    private TardeDeQuintaPelaApi Tarde => new(Escalador, () => Operador(), Trabalhador, Relogio);

    [Fact]
    public async Task O_trabalhador_fica_sabendo_que_a_escala_dele_saiu_e_que_tem_recado_novo()
    {
        await Tarde.Preparar();
        await using var doAntonio = await Fluxo.Abrir(Trabalhador(Antonio), EscutaDePe());

        Assert.Equal("pronto", (await doAntonio.Proximo())?.Tipo);

        Relogio.Acertar(Em(5, 10));
        await Escalador().Enviar(HttpMethod.Post, $"{DaTarde}/escala").ComSucesso();

        var eventos = new[] { await doAntonio.Proximo(), await doAntonio.Proximo() };

        Assert.All(eventos, evento => Assert.Equal("mudou", evento?.Tipo));
        Assert.Contains(new EventoResposta(OrigemDoAnuncio.Escalas, Categoria.Estiva, new DateOnly(2026, 11, 5), Turno.Tarde), eventos.Select(evento => evento?.Dados));
        Assert.Contains(new EventoResposta(OrigemDoAnuncio.Avisos, null, null, null), eventos.Select(evento => evento?.Dados));
    }

    [Fact]
    public async Task A_habilitacao_dos_colegas_mexe_no_quadro_do_escalador_e_nao_chama_o_celular_do_trabalhador()
    {
        await Tarde.Preparar();
        await Escalador().CadastrarEstivador(4140, "Gilberto Souza");

        await using var doEscalador = await Fluxo.Abrir(Escalador(), EscutaDePe());
        await using var doAntonio = await Fluxo.Abrir(Trabalhador(Antonio), EscutaDePe());
        await doEscalador.Proximo();
        await doAntonio.Proximo();

        Relogio.Acertar(Em(5, 9));
        await Trabalhador(4140).Enviar(HttpMethod.Put, "/api/eu/habilitacoes/2026-11-05/Tarde", new PedidoDeHabilitacao(["Estivador"])).ComSucesso();

        Assert.Equal(
            ("mudou", new EventoResposta(OrigemDoAnuncio.Escalacoes, Categoria.Estiva, new DateOnly(2026, 11, 5), Turno.Tarde)),
            await doEscalador.Proximo());

        // Ao Antônio, nada chega: a habilitação do Gilberto não muda a escala dele.
        Assert.Null(await doAntonio.Proximo(TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public async Task O_trabalhador_nao_recebe_a_escala_de_outra_categoria_nem_o_recado_de_outro_trabalhador()
    {
        await Tarde.Preparar();
        var central = await EscutaDePe();
        await using var doAntonio = await Fluxo.Abrir(Trabalhador(Antonio), Task.FromResult(central));
        await doAntonio.Proximo();

        var quintaATarde = new DateTimeOffset(2026, 11, 5, 13, 0, 0, TimeSpan.FromHours(-3));
        central.Publicar(new AnuncioDoBanco(OrigemDoAnuncio.Escalas, Categoria.Capatazia, quintaATarde, null));
        central.Publicar(new AnuncioDoBanco(OrigemDoAnuncio.Avisos, null, null, TrabalhadorId.Novo()));
        central.Publicar(new AnuncioDoBanco(OrigemDoAnuncio.Escalas, Categoria.Estiva, quintaATarde, null));

        // Dos três anúncios, só o último é dele: a escala da estiva. A da capatazia e o recado de outro não chegam.
        Assert.Equal(
            ("mudou", new EventoResposta(OrigemDoAnuncio.Escalas, Categoria.Estiva, new DateOnly(2026, 11, 5), Turno.Tarde)),
            await doAntonio.Proximo());
    }

    /// <summary>Espera a escuta do banco desta API ficar de pé: antes dela, nenhum anúncio chega a ninguém.</summary>
    private async Task<CentralDeAnuncios> EscutaDePe()
    {
        var central = Servicos.GetRequiredService<CentralDeAnuncios>();
        var limite = DateTime.UtcNow + Prazo;

        while (!central.Escutando && DateTime.UtcNow < limite)
        {
            await Task.Delay(20, Cancelamento);
        }

        Assert.True(central.Escutando, "A escuta do banco não ficou de pé.");

        return central;
    }

    /// <summary>O fluxo de eventos aberto, lido evento a evento, como o aplicativo lê.</summary>
    private sealed class Fluxo : IAsyncDisposable
    {
        private readonly HttpResponseMessage _resposta;

        private readonly StreamReader _leitor;

        private Fluxo(HttpResponseMessage resposta, StreamReader leitor)
        {
            _resposta = resposta;
            _leitor = leitor;
        }

        public static async Task<Fluxo> Abrir(HttpClient cliente, Task<CentralDeAnuncios> escuta)
        {
            await escuta;
            var resposta = await cliente.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/api/eventos"), HttpCompletionOption.ResponseHeadersRead, Cancelamento);
            resposta.EnsureSuccessStatusCode();
            Assert.Equal("text/event-stream", resposta.Content.Headers.ContentType?.MediaType);

            return new Fluxo(resposta, new StreamReader(await resposta.Content.ReadAsStreamAsync(Cancelamento)));
        }

        /// <summary>O próximo evento que não é sinal de vida; nulo, se não chegar nenhum no prazo.</summary>
        public async Task<(string Tipo, EventoResposta Dados)?> Proximo(TimeSpan? prazo = null)
        {
            using var limite = CancellationTokenSource.CreateLinkedTokenSource(Cancelamento);
            limite.CancelAfter(prazo ?? Prazo);
            string? tipo = null;
            string? dados = null;

            try
            {
                while (await _leitor.ReadLineAsync(limite.Token) is { } linha)
                {
                    if (linha.StartsWith("event: ", StringComparison.Ordinal))
                    {
                        tipo = linha["event: ".Length..];
                    }
                    else if (linha.StartsWith("data: ", StringComparison.Ordinal))
                    {
                        dados = linha["data: ".Length..];
                    }
                    else if (linha.Length == 0 && tipo is not null)
                    {
                        if (tipo != "vivo")
                        {
                            return (tipo, JsonSerializer.Deserialize<EventoResposta>(dados ?? "{}", Pedidos.Json)!);
                        }

                        (tipo, dados) = (null, null);
                    }
                }
            }
            catch (OperationCanceledException) when (!Cancelamento.IsCancellationRequested)
            {
            }

            return null;
        }

        public ValueTask DisposeAsync()
        {
            _leitor.Dispose();
            _resposta.Dispose();

            return ValueTask.CompletedTask;
        }
    }
}
