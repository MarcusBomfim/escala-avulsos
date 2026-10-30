using EscalaAvulsos.Api.Hospedagem;
using EscalaAvulsos.Infraestrutura.Avisos;

// `dotnet run --project src/EscalaAvulsos.Api -- chaves-vapid`: o par de chaves do push, para a configuração Avisos:Push (ADR-0020).
if (args is ["chaves-vapid"])
{
    var chaves = ChavesVapid.Gerar();
    Console.WriteLine($"Avisos__Push__ChavePublica={chaves.PublicaEmBase64Url}");
    Console.WriteLine($"Avisos__Push__ChavePrivada={chaves.PrivadaEmBase64Url}");

    return;
}

var builder = WebApplication.CreateBuilder(args);
builder.AdicionarApi();

var app = builder.Build();
app.UsarApi();

await app.RunAsync();
