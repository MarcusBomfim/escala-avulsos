using EscalaAvulsos.Dominio.Periodos;
using EscalaAvulsos.Dominio.Requisicoes;
using EscalaAvulsos.Dominio.Trabalhadores;
using EscalaAvulsos.Infraestrutura.Persistencia.Registros;

namespace EscalaAvulsos.Infraestrutura.Persistencia.Repositorios;

/// <summary>Grava e lê requisições, com concorrência otimista (ADR-0012).</summary>
public sealed class RepositorioDeRequisicoes(ContextoDoBanco contexto, TimeProvider relogio)
{
    // A requisição não tem restrição própria: o que a identifica é o id.
    private static readonly Dictionary<string, string> SemRestricaoPropria = [];

    private readonly TabelaDeAgregados<RegistroDeRequisicao> _tabela = new(contexto, relogio, "a requisição");

    public Task<Versionado<Requisicao>?> Obter(RequisicaoId id, CancellationToken cancelamento = default)
    {
        ArgumentNullException.ThrowIfNull(id);

        return _tabela.Obter(id.Valor, registro => registro.ParaRequisicao(), cancelamento);
    }

    /// <summary>
    /// As requisições da categoria para o período, na ordem em que chegaram —
    /// todas, as canceladas e as atrasadas também: quem as separa é a escala.
    /// </summary>
    public async Task<IReadOnlyList<Requisicao>> DoPeriodo(Categoria categoria, Periodo periodo, CancellationToken cancelamento = default)
    {
        ArgumentNullException.ThrowIfNull(periodo);

        var inicio = periodo.Inicio.ToUniversalTime();
        var requisicoes = await _tabela.Consultar(
            registros => registros.Where(registro => registro.Categoria == categoria && registro.Inicio == inicio).OrderBy(registro => registro.CriadoEm).ThenBy(registro => registro.Id),
            registro => registro.ParaRequisicao(),
            cancelamento);

        return [.. requisicoes.Select(requisicao => requisicao.Agregado)];
    }

    /// <returns>A versão gravada: 1.</returns>
    public Task<int> Adicionar(Requisicao requisicao, CancellationToken cancelamento = default)
    {
        ArgumentNullException.ThrowIfNull(requisicao);

        return _tabela.Inserir(RegistroDeRequisicao.De(requisicao, versao: 1, _tabela.Agora), SemRestricaoPropria, cancelamento);
    }

    /// <param name="versaoLida">A versão em que a requisição foi lida.</param>
    /// <returns>A versão nova.</returns>
    public Task<int> Atualizar(Requisicao requisicao, int versaoLida, CancellationToken cancelamento = default)
    {
        ArgumentNullException.ThrowIfNull(requisicao);

        return _tabela.Atualizar(RegistroDeRequisicao.De(requisicao, versaoLida + 1, _tabela.Agora), versaoLida, null, SemRestricaoPropria, cancelamento);
    }
}
