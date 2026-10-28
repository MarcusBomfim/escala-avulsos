using EscalaAvulsos.Infraestrutura.Persistencia;
using EscalaAvulsos.Infraestrutura.Persistencia.Repositorios;

namespace EscalaAvulsos.Infraestrutura.Testes.Persistencia;

/// <summary>Um contexto e os repositórios dele: o que a API vai ter em cada pedido.</summary>
public sealed class Sessao : IAsyncDisposable
{
    public Sessao(ContextoDoBanco contexto)
    {
        Contexto = contexto;
        Trabalhadores = new RepositorioDeTrabalhadores(contexto, TimeProvider.System);
        Requisicoes = new RepositorioDeRequisicoes(contexto, TimeProvider.System);
        Escalacoes = new RepositorioDeEscalacoes(contexto, TimeProvider.System);
    }

    public ContextoDoBanco Contexto { get; }

    public RepositorioDeTrabalhadores Trabalhadores { get; }

    public RepositorioDeRequisicoes Requisicoes { get; }

    public RepositorioDeEscalacoes Escalacoes { get; }

    public ValueTask DisposeAsync() => Contexto.DisposeAsync();
}
