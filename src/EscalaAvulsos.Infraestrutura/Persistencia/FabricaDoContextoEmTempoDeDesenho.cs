using Microsoft.EntityFrameworkCore.Design;

namespace EscalaAvulsos.Infraestrutura.Persistencia;

/// <summary>
/// O contexto que o dotnet-ef usa para gerar e aplicar migrações. Gerar
/// migração não abre conexão: a cadeia de conexão aqui só diz que o banco é
/// PostgreSQL. Aplicar abre, e a cadeia vem de <c>--connection</c> ou da
/// variável de ambiente.
/// </summary>
public sealed class FabricaDoContextoEmTempoDeDesenho : IDesignTimeDbContextFactory<ContextoDoBanco>
{
    public ContextoDoBanco CreateDbContext(string[] args) =>
        ContextoDoBanco.Criar(Environment.GetEnvironmentVariable("ConnectionStrings__Escala") ?? "Host=localhost;Database=escala_avulsos");
}
