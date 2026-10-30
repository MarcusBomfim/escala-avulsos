using EscalaAvulsos.Dominio.Avisos;
using EscalaAvulsos.Dominio.Testes.Escalacoes;
using EscalaAvulsos.Dominio.Trabalhadores;
using EscalaAvulsos.Infraestrutura.Persistencia.Repositorios;
using EscalaAvulsos.Infraestrutura.TempoReal;
using static EscalaAvulsos.Dominio.Testes.Escalacoes.EscalacoesDeTeste;

namespace EscalaAvulsos.Infraestrutura.Testes.Persistencia;

/*
 * O banco anuncia o que mudou quando a gravação é confirmada, e quem escuta
 * fica sabendo — o período da categoria, ou o trabalhador com aviso novo. A
 * gravação desfeita não anuncia nada.
 */
public sealed class AnunciosDoBancoTests(BancoDeTestes bancos) : TestesComBanco(bancos)
{
    private static readonly TimeSpan Prazo = TimeSpan.FromSeconds(10);

    private readonly TardeDeQuinta _tarde = new();

    [Fact]
    public async Task Quem_escuta_fica_sabendo_da_escalacao_da_requisicao_da_escala_e_dos_avisos()
    {
        await using var escuta = await Escuta.Abrir(Banco.ConexaoDaAplicacao);

        await Banco.Gravar(_tarde.Trabalhadores, [_tarde.Requisicao], _tarde.Escalacao);
        await Banco.FazerAEscala(_tarde.Escalacao.Id, Em(5, 10));

        var anuncios = await escuta.Receber(quantos: 3 + 6);
        var doPeriodo = new AnuncioDoBanco(OrigemDoAnuncio.Requisicoes, Categoria.Estiva, QuintaATarde.Inicio, null);

        Assert.Contains(doPeriodo, anuncios);
        Assert.Contains(doPeriodo with { Origem = OrigemDoAnuncio.Escalacoes }, anuncios);
        Assert.Contains(doPeriodo with { Origem = OrigemDoAnuncio.Escalas }, anuncios);
        Assert.Equal(
            _tarde.Trabalhadores.Select(trabalhador => trabalhador.Id.Valor).Order(),
            anuncios.Where(anuncio => anuncio.Origem == OrigemDoAnuncio.Avisos).Select(anuncio => anuncio.Trabalhador!.Valor).Order());
    }

    [Fact]
    public async Task A_desistencia_e_o_cancelamento_tambem_anunciam_nao_so_a_criacao()
    {
        await Banco.Gravar(_tarde.Trabalhadores, [_tarde.Requisicao], _tarde.Escalacao);
        var escala = await Banco.FazerAEscala(_tarde.Escalacao.Id, Em(5, 10));
        await using var escuta = await Escuta.Abrir(Banco.ConexaoDaAplicacao);

        // A desistência muda a escala; o cancelamento muda a requisição. As duas são atualizações, e não criações.
        await Banco.Desistir(escala.Id, _tarde.Edson.Id, Em(5, 11));
        await using (var sessao = Banco.NovaSessao())
        {
            var lida = (await sessao.Requisicoes.Obter(_tarde.Requisicao.Id, Cancelamento))!;
            lida.Agregado.Cancelar("O navio atrasou", Em(5, 11));
            await sessao.Requisicoes.Atualizar(lida.Agregado, lida.Versao, Cancelamento);
        }

        // O substituto ganha um aviso: são três anúncios, a escala, o aviso e a requisição.
        var anuncios = await escuta.Receber(quantos: 3);
        var doPeriodo = new AnuncioDoBanco(OrigemDoAnuncio.Escalas, Categoria.Estiva, QuintaATarde.Inicio, null);

        Assert.Contains(doPeriodo, anuncios);
        Assert.Contains(doPeriodo with { Origem = OrigemDoAnuncio.Requisicoes }, anuncios);
    }

    [Fact]
    public async Task A_gravacao_desfeita_nao_anuncia_nada()
    {
        await Banco.Gravar(_tarde.Trabalhadores, []);
        await using var escuta = await Escuta.Abrir(Banco.ConexaoDaAplicacao);

        await using (var sessao = Banco.NovaSessao())
        await using (var transacao = await sessao.Contexto.Database.BeginTransactionAsync(Cancelamento))
        {
            await new RepositorioDeAvisos(sessao.Contexto, TimeProvider.System).Guardar(AvisosDosPrazos.DosVencimentos(_tarde.Antonio, Em(5, 9)), Cancelamento);
            await transacao.RollbackAsync(Cancelamento);
        }

        // O anúncio que vale chega; o da transação desfeita, antes dele, não chegou.
        await using (var sessao = Banco.NovaSessao())
        {
            await new RepositorioDeAvisos(sessao.Contexto, TimeProvider.System).Guardar(AvisosDosPrazos.DosVencimentos(_tarde.Benedito, Em(5, 9)), Cancelamento);
        }

        Assert.Equal([new AnuncioDoBanco(OrigemDoAnuncio.Avisos, null, null, _tarde.Benedito.Id)], await escuta.Receber(quantos: 1));
    }

    [Fact]
    public void O_anuncio_que_nao_e_deste_sistema_e_ignorado()
    {
        Assert.Null(AnuncioDoBanco.Ler("não é JSON"));
        Assert.Null(AnuncioDoBanco.Ler("""{"tabela":"outra"}"""));
        Assert.Null(AnuncioDoBanco.Ler("""{"tabela":"escalas","categoria":"Pesca","inicio":"2026-11-05T13:00:00-03:00"}"""));
        Assert.Equal(
            new AnuncioDoBanco(OrigemDoAnuncio.Escalas, Categoria.Estiva, QuintaATarde.Inicio, null),
            AnuncioDoBanco.Ler("""{"tabela":"escalas","categoria":"Estiva","inicio":"2026-11-05T16:00:00+00:00"}"""));
    }

    /// <summary>Uma escuta aberta, juntando o que o banco anuncia.</summary>
    private sealed class Escuta : IAsyncDisposable
    {
        private readonly CancellationTokenSource _parar = new();

        private readonly List<AnuncioDoBanco> _recebidos = [];

        private Task _leitura = Task.CompletedTask;

        public static async Task<Escuta> Abrir(string conexao)
        {
            var escuta = new Escuta();
            var pronta = new TaskCompletionSource();

            escuta._leitura = Task.Run(async () =>
            {
                await foreach (var anuncio in new OuvinteDoBanco(conexao).Ouvir(pronta.SetResult, escuta._parar.Token))
                {
                    lock (escuta._recebidos)
                    {
                        escuta._recebidos.Add(anuncio);
                    }
                }
            });

            await pronta.Task.WaitAsync(Prazo, TestContext.Current.CancellationToken);

            return escuta;
        }

        /// <summary>Espera chegarem tantos anúncios, e os devolve.</summary>
        public async Task<IReadOnlyList<AnuncioDoBanco>> Receber(int quantos)
        {
            var limite = DateTime.UtcNow + Prazo;

            while (DateTime.UtcNow < limite)
            {
                lock (_recebidos)
                {
                    if (_recebidos.Count >= quantos)
                    {
                        return [.. _recebidos];
                    }
                }

                await Task.Delay(20, TestContext.Current.CancellationToken);
            }

            lock (_recebidos)
            {
                return [.. _recebidos];
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _parar.CancelAsync();

            try
            {
                await _leitura;
            }
            catch (OperationCanceledException)
            {
            }

            _parar.Dispose();
        }
    }
}
