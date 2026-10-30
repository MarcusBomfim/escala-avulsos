using EscalaAvulsos.Dominio.Identificacao;
using EscalaAvulsos.Dominio.Trabalhadores;
using EscalaAvulsos.Infraestrutura.Persistencia.Registros;
using Microsoft.EntityFrameworkCore;

namespace EscalaAvulsos.Infraestrutura.Persistencia.Repositorios;

/// <summary>Grava e lê trabalhadores, com concorrência otimista (ADR-0012).</summary>
public sealed class RepositorioDeTrabalhadores(ContextoDoBanco contexto, TimeProvider relogio)
{
    private readonly TabelaDeAgregados<RegistroDeTrabalhador> _tabela = new(contexto, relogio, "o trabalhador");

    public Task<Versionado<Trabalhador>?> Obter(TrabalhadorId id, CancellationToken cancelamento = default)
    {
        ArgumentNullException.ThrowIfNull(id);

        return _tabela.Obter(id.Valor, registro => registro.ParaTrabalhador(), cancelamento);
    }

    /// <summary>O trabalhador da matrícula — como ele se identifica no aplicativo.</summary>
    public async Task<Versionado<Trabalhador>?> ObterPelaMatricula(Matricula matricula, CancellationToken cancelamento = default)
    {
        ArgumentNullException.ThrowIfNull(matricula);

        var trabalhadores = await _tabela.Consultar(
            registros => registros.Where(registro => registro.Matricula == matricula.Numero),
            registro => registro.ParaTrabalhador(),
            cancelamento);

        return trabalhadores.SingleOrDefault();
    }

    /// <summary>Os trabalhadores dos ids, na ordem das matrículas — os habilitados de uma escalação, para a escala.</summary>
    /// <remarks>Os que não estão gravados não vêm; o <c>Escalador</c> diz quem falta.</remarks>
    public async Task<IReadOnlyList<Trabalhador>> Obter(IEnumerable<TrabalhadorId> ids, CancellationToken cancelamento = default)
    {
        ArgumentNullException.ThrowIfNull(ids);

        var valores = ids.Select(id => id.Valor).Distinct().ToArray();
        var trabalhadores = await _tabela.Consultar(
            registros => registros.Where(registro => valores.Contains(registro.Id)).OrderBy(registro => registro.Matricula),
            registro => registro.ParaTrabalhador(),
            cancelamento);

        return [.. trabalhadores.Select(trabalhador => trabalhador.Agregado)];
    }

    /// <summary>Os trabalhadores da categoria, na ordem das matrículas: quem o servidor confere para os avisos (ADR-0019).</summary>
    public async Task<IReadOnlyList<Trabalhador>> DaCategoria(Categoria categoria, CancellationToken cancelamento = default)
    {
        var trabalhadores = await _tabela.Consultar(
            registros => registros.Where(registro => registro.Categoria == categoria).OrderBy(registro => registro.Matricula),
            registro => registro.ParaTrabalhador(),
            cancelamento);

        return [.. trabalhadores.Select(trabalhador => trabalhador.Agregado)];
    }

    /// <returns>A versão gravada: 1.</returns>
    public Task<int> Adicionar(Trabalhador trabalhador, CancellationToken cancelamento = default)
    {
        ArgumentNullException.ThrowIfNull(trabalhador);

        return _tabela.Inserir(RegistroDeTrabalhador.De(trabalhador, versao: 1, _tabela.Agora), Restricoes(trabalhador), cancelamento);
    }

    /// <param name="versaoLida">A versão em que o trabalhador foi lido.</param>
    /// <returns>A versão nova.</returns>
    public Task<int> Atualizar(Trabalhador trabalhador, int versaoLida, CancellationToken cancelamento = default)
    {
        ArgumentNullException.ThrowIfNull(trabalhador);

        var novo = RegistroDeTrabalhador.De(trabalhador, versaoLida + 1, _tabela.Agora);

        return _tabela.Atualizar(
            novo,
            versaoLida,
            colunas => colunas
                .SetProperty(registro => registro.Matricula, novo.Matricula)
                .SetProperty(registro => registro.Cpf, novo.Cpf),
            Restricoes(trabalhador),
            cancelamento);
    }

    private static Dictionary<string, string> Restricoes(Trabalhador trabalhador) => new()
    {
        ["ux_trabalhadores_matricula"] = $"Já existe trabalhador com a matrícula {trabalhador.Matricula}.",
        ["ux_trabalhadores_cpf"] = $"Já existe trabalhador com o CPF {trabalhador.Cpf}.",
    };
}
