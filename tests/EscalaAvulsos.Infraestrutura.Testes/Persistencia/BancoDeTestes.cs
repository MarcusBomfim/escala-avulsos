using DotNet.Testcontainers.Builders;
using EscalaAvulsos.Infraestrutura.Persistencia;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace EscalaAvulsos.Infraestrutura.Testes.Persistencia;

/// <summary>
/// Um PostgreSQL de verdade, num contêiner, para o assembly de testes
/// inteiro: sobe uma vez e recebe as migrações num banco-modelo. Cada teste
/// ganha um banco seu, copiado do modelo — vazio, já com o esquema.
/// </summary>
/// <remarks>
/// <para>
/// PostgreSQL de verdade, e não SQLite nem o provedor em memória: o que está
/// sendo testado é justamente o que só o PostgreSQL faz — <c>jsonb</c>,
/// restrições, a exclusão das jornadas que se cruzam, os códigos de erro que
/// ele devolve (ADR-0012, ADR-0013).
/// </para>
/// <para>
/// Um banco por teste porque a matrícula, o CPF e a escalação de cada período
/// são únicos: o Antônio, matrícula 4127, aparece em quase todos os testes. A
/// cópia de um banco-modelo é coisa de milissegundos.
/// </para>
/// <para>
/// Duas conexões. A do dono, superusuário, aplica as migrações e serve aos
/// testes que examinam o banco por dentro ou o adulteram. A da aplicação usa
/// um papel comum, como em produção: lê e grava, e não muda o esquema.
/// </para>
/// <para>
/// Sem Docker na máquina de quem desenvolve, os testes que precisam do banco
/// são pulados, com o motivo. No CI não: lá, Docker indisponível é falha —
/// teste de integração que some sem ninguém ver é pior do que teste que falha.
/// </para>
/// </remarks>
public sealed class BancoDeTestes : IAsyncLifetime
{
    /// <summary>Versão fixa: o banco dos testes não muda sozinho de uma execução para outra.</summary>
    public const string Imagem = "postgres:17.6-alpine";

    /// <summary>O papel da aplicação: entra, lê e grava, mas não é superusuário nem dono das tabelas.</summary>
    public const string PapelDaAplicacao = "escala_aplicacao";

    private const string SenhaDaAplicacao = "aplicacao-nos-testes";

    private const string Modelo = "escala_modelo";

    // CREATE DATABASE a partir do modelo, um de cada vez: os testes rodam em paralelo.
    private readonly SemaphoreSlim _umaCopiaPorVez = new(1, 1);

    private PostgreSqlContainer? _conteiner;

    private string? _conexaoDoServidor;

    private string? _indisponivel;

    private static bool EmIntegracaoContinua =>
        string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase);

    private string MotivoDaIndisponibilidade => _indisponivel ?? "O banco de testes não subiu.";

    public async ValueTask InitializeAsync()
    {
        try
        {
            // Os testes rodam em paralelo, cada um com o seu banco — e o pool de conexões dele, e a escuta dos anúncios:
            // as cem conexões do padrão acabam antes dos testes.
            _conteiner = new PostgreSqlBuilder(Imagem).WithCommand("-c", "max_connections=400").Build();
            await _conteiner.StartAsync();
        }
        catch (DockerUnavailableException erro) when (!EmIntegracaoContinua)
        {
            _indisponivel = $"Docker indisponível nesta máquina; os testes com PostgreSQL foram pulados. ({erro.Message})";

            return;
        }

        var servidor = _conteiner.GetConnectionString();
        await Executar(servidor, $"create database {Modelo}");
        await Executar(servidor, $"create role {PapelDaAplicacao} login password '{SenhaDaAplicacao}'");

        // Sem pool: o modelo só pode ser copiado sem ninguém conectado a ele.
        var modelo = new NpgsqlConnectionStringBuilder(servidor) { Database = Modelo, Pooling = false }.ConnectionString;

        await using (var contexto = ContextoDoBanco.Criar(modelo))
        {
            await contexto.Database.MigrateAsync();
        }

        // Os papéis como quem implanta os criaria: a aplicação lê e grava as tabelas do esquema, e só.
        await Executar(
            modelo,
            $"""
            grant usage on schema {ContextoDoBanco.Esquema} to {PapelDaAplicacao};
            grant select, insert, update, delete on all tables in schema {ContextoDoBanco.Esquema} to {PapelDaAplicacao};
            """);

        _conexaoDoServidor = servidor;
    }

    public async ValueTask DisposeAsync()
    {
        _umaCopiaPorVez.Dispose();

        if (_conteiner is not null)
        {
            await _conteiner.DisposeAsync();
        }
    }

    /// <summary>Um banco novo, só deste teste, com o esquema e sem dado nenhum. Pula o teste se não houver banco.</summary>
    public async Task<BancoDoTeste> NovoBanco()
    {
        Assert.SkipWhen(_conexaoDoServidor is null, MotivoDaIndisponibilidade);

        var nome = $"teste_{Guid.NewGuid():N}";
        await _umaCopiaPorVez.WaitAsync(TestContext.Current.CancellationToken);

        try
        {
            await Executar(_conexaoDoServidor!, $"create database {nome} template {Modelo}");
        }
        finally
        {
            _umaCopiaPorVez.Release();
        }

        var doDono = new NpgsqlConnectionStringBuilder(_conexaoDoServidor) { Database = nome };
        var daAplicacao = new NpgsqlConnectionStringBuilder(doDono.ConnectionString) { Username = PapelDaAplicacao, Password = SenhaDaAplicacao };

        return new BancoDoTeste(doDono.ConnectionString, daAplicacao.ConnectionString);
    }

    private static async Task Executar(string conexao, string sql)
    {
        await using var conectado = new NpgsqlConnection(conexao);
        await conectado.OpenAsync();
        await using var comando = new NpgsqlCommand(sql, conectado);
        await comando.ExecuteNonQueryAsync();
    }
}

/// <summary>O banco de um teste: a conexão do dono, para olhar e adulterar por dentro, e a da aplicação.</summary>
public sealed record BancoDoTeste(string ConexaoDoDono, string ConexaoDaAplicacao)
{
    /// <summary>Uma sessão da aplicação: um contexto e os repositórios dele, como cada pedido terá o seu.</summary>
    public Sessao NovaSessao() => new(ContextoDoBanco.Criar(ConexaoDaAplicacao));

    /// <summary>SQL como o dono do banco — o <c>update</c> feito à mão que a leitura tem de recusar.</summary>
    public async Task<int> ExecutarComoDono(string sql, params object[] parametros)
    {
        await using var conexao = new NpgsqlConnection(ConexaoDoDono);
        await conexao.OpenAsync(TestContext.Current.CancellationToken);
        await using var comando = new NpgsqlCommand(sql, conexao);

        foreach (var parametro in parametros)
        {
            comando.Parameters.Add(new NpgsqlParameter { Value = parametro });
        }

        return await comando.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }
}
