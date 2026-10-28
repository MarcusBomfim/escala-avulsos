namespace EscalaAvulsos.Infraestrutura.Testes.Persistencia;

/// <summary>Os testes que gravam: cada um com um banco só seu, vazio, já com o esquema.</summary>
public abstract class TestesComBanco(BancoDeTestes bancos) : IAsyncLifetime
{
    protected static CancellationToken Cancelamento => TestContext.Current.CancellationToken;

    protected BancoDoTeste Banco { get; private set; } = null!;

    public async ValueTask InitializeAsync() => Banco = await bancos.NovoBanco();

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);

        return ValueTask.CompletedTask;
    }
}
