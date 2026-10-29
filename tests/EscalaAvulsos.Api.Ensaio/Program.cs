using EscalaAvulsos.Api.Ensaio;
using EscalaAvulsos.Api.Hospedagem;
using EscalaAvulsos.Infraestrutura.Persistencia;
using Microsoft.EntityFrameworkCore;

// A API do ensaio (ADR-0018): a mesma API, com o relógio do teste no lugar
// do relógio do sistema, e o banco apagado e migrado a cada subida. A
// configuração é a da API — o appsettings.json dela vem junto na
// compilação —, e o que muda no ensaio chega por variável de ambiente.
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
});
builder.Services.AddSingleton<RelogioDoEnsaio>();
builder.Services.AddSingleton<TimeProvider>(servicos => servicos.GetRequiredService<RelogioDoEnsaio>());
builder.AdicionarApi();

var app = builder.Build();

await using (var escopo = app.Services.CreateAsyncScope())
{
    var banco = escopo.ServiceProvider.GetRequiredService<ContextoDoBanco>().Database;
    await banco.EnsureDeletedAsync();
    await banco.MigrateAsync();
}

app.UsarApi();

// Só aqui, e sem token: o teste diz que horas são, no porto e na API.
app.MapPut("/ensaio/relogio", (AcertoDoRelogio acerto, RelogioDoEnsaio relogio) =>
{
    relogio.Acertar(acerto.Agora);

    return TypedResults.NoContent();
}).AllowAnonymous().ExcludeFromDescription();

await app.RunAsync();

internal sealed record AcertoDoRelogio(DateTimeOffset Agora);
