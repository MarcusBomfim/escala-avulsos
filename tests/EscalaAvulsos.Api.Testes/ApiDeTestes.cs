using System.Net.Http.Headers;
using EscalaAvulsos.Infraestrutura.Testes.Persistencia;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

// Um PostgreSQL para o assembly de testes inteiro; cada teste ganha um banco seu.
[assembly: AssemblyFixture(typeof(BancoDeTestes))]

namespace EscalaAvulsos.Api.Testes;

/// <summary>
/// Os testes da API: cada um com a API inteira, em memória, contra um banco
/// só dele — vazio, já com o esquema — e um relógio que ele acerta.
/// </summary>
/// <remarks>
/// Uma API por teste, e não uma para todos: a matrícula, o CPF e a escalação
/// de cada período são únicos, e o rodízio de cada categoria é um só. Dois
/// testes no mesmo banco escalariam um no rodízio do outro.
/// </remarks>
public abstract class TestesDaApi(BancoDeTestes bancos) : IAsyncLifetime
{
    private Fabrica? _fabrica;

    protected static CancellationToken Cancelamento => TestContext.Current.CancellationToken;

    protected BancoDoTeste Banco { get; private set; } = null!;

    protected IServiceProvider Servicos => Api.Services;

    /// <summary>O relógio da API deste teste: o do sistema, até ser acertado.</summary>
    protected RelogioDosTestes Relogio => Api.Services.GetRequiredService<RelogioDosTestes>();

    private Fabrica Api => _fabrica ?? throw new InvalidOperationException("A API do teste ainda não subiu.");

    public async ValueTask InitializeAsync()
    {
        Banco = await bancos.NovoBanco();
        _fabrica = new Fabrica(Banco.ConexaoDaAplicacao, Configurar);
    }

    /// <summary>O que o teste muda na API dele — a configuração do porto, por exemplo. Por padrão, nada.</summary>
    protected virtual void Configurar(IWebHostBuilder api)
    {
    }

    public async ValueTask DisposeAsync()
    {
        if (_fabrica is not null)
        {
            await _fabrica.DisposeAsync();
        }

        GC.SuppressFinalize(this);
    }

    protected HttpClient Anonimo() => Api.CreateClient();

    protected HttpClient ComToken(string token)
    {
        var cliente = Anonimo();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return cliente;
    }

    protected HttpClient Escalador() => ComToken(Tokens.Escalador());

    protected HttpClient Trabalhador(int matricula) => ComToken(Tokens.Trabalhador(matricula));

    protected HttpClient Operador(string cnpj = "11.222.333/0001-81", string nome = "Cais Forte Operações Portuárias") =>
        ComToken(Tokens.Operador(cnpj, nome));

    /// <summary>A API configurada para os testes: o banco do teste, os tokens emitidos aqui, o relógio que o teste acerta, e o que o teste mudar.</summary>
    private sealed class Fabrica(string conexao, Action<IWebHostBuilder> ajuste) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testes");
            builder.UseSetting("ConnectionStrings:Escala", conexao);

            // A entrega dos avisos e a vigia dos prazos não rodam sozinhas: o teste as chama quando quer, com o relógio dele.
            builder.UseSetting("Avisos:ConferirEEntregarSozinho", "false");
            ajuste(builder);
            builder.ConfigureTestServices(servicos =>
            {
                servicos.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, opcoes =>
                {
                    opcoes.TokenValidationParameters.ValidIssuer = Tokens.Emissor;
                    opcoes.TokenValidationParameters.ValidAudience = Tokens.Audiencia;
                    opcoes.TokenValidationParameters.IssuerSigningKey = Tokens.Chave;
                });

                servicos.AddSingleton<RelogioDosTestes>();
                servicos.AddSingleton<TimeProvider>(provedor => provedor.GetRequiredService<RelogioDosTestes>());
            });
        }
    }
}

/// <summary>O relógio da API de um teste: marca o instante acertado, parado nele; sem acerto, o do sistema.</summary>
public sealed class RelogioDosTestes : TimeProvider
{
    private DateTimeOffset? _agora;

    public void Acertar(DateTimeOffset agora) => _agora = agora.ToUniversalTime();

    public override DateTimeOffset GetUtcNow() => _agora ?? base.GetUtcNow();
}
