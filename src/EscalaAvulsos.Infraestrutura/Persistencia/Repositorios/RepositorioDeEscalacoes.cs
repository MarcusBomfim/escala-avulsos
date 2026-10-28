using EscalaAvulsos.Dominio.Comum;
using EscalaAvulsos.Dominio.Escalacoes;
using EscalaAvulsos.Dominio.Periodos;
using EscalaAvulsos.Dominio.Trabalhadores;
using EscalaAvulsos.Infraestrutura.Persistencia.Registros;

namespace EscalaAvulsos.Infraestrutura.Persistencia.Repositorios;

/// <summary>Grava e lê escalações, com concorrência otimista (ADR-0012).</summary>
/// <remarks>
/// A habilitação é a gravação mais disputada do sistema: na hora em que a
/// janela abre, centenas de trabalhadores se habilitam na mesma escalação.
/// Cada um lê, habilita e grava com a versão que leu; quem perde a corrida
/// lê de novo e refaz — e a vez continua pela matrícula, e não pela ordem de
/// chegada (ADR-0005).
/// </remarks>
public sealed class RepositorioDeEscalacoes(ContextoDoBanco contexto, TimeProvider relogio)
{
    private readonly TabelaDeAgregados<RegistroDeEscalacao> _tabela = new(contexto, relogio, "a escalação");

    public Task<Versionado<Escalacao>?> Obter(EscalacaoId id, CancellationToken cancelamento = default)
    {
        ArgumentNullException.ThrowIfNull(id);

        return _tabela.Obter(id.Valor, registro => registro.ParaEscalacao(), cancelamento);
    }

    /// <summary>A escalação da categoria para o período; nula, se ainda não foi aberta.</summary>
    public async Task<Versionado<Escalacao>?> Da(Categoria categoria, Periodo periodo, CancellationToken cancelamento = default)
    {
        ArgumentNullException.ThrowIfNull(periodo);

        var inicio = periodo.Inicio.ToUniversalTime();
        var escalacoes = await _tabela.Consultar(
            registros => registros.Where(registro => registro.Categoria == categoria && registro.Inicio == inicio),
            registro => registro.ParaEscalacao(),
            cancelamento);

        return escalacoes.SingleOrDefault();
    }

    /// <summary>As escalações do mês em que o trabalhador se habilitou, na ordem dos períodos — para a frequência (ADR-0009).</summary>
    public async Task<IReadOnlyList<Escalacao>> DoTrabalhadorNoMes(TrabalhadorId trabalhador, Mes mes, CancellationToken cancelamento = default)
    {
        ArgumentNullException.ThrowIfNull(trabalhador);
        ArgumentNullException.ThrowIfNull(mes);

        var escalacoes = await _tabela.Consultar(
            registros => registros
                .Where(registro => registro.Dia >= mes.PrimeiroDia && registro.Dia <= mes.UltimoDia && registro.Habilitados.Contains(trabalhador.Valor))
                .OrderBy(registro => registro.Inicio)
                .ThenBy(registro => registro.Categoria),
            registro => registro.ParaEscalacao(),
            cancelamento);

        return [.. escalacoes.Select(escalacao => escalacao.Agregado)];
    }

    /// <returns>A versão gravada: 1.</returns>
    public Task<int> Adicionar(Escalacao escalacao, CancellationToken cancelamento = default)
    {
        ArgumentNullException.ThrowIfNull(escalacao);

        return _tabela.Inserir(RegistroDeEscalacao.De(escalacao, versao: 1, _tabela.Agora), Restricoes(escalacao), cancelamento);
    }

    /// <param name="versaoLida">A versão em que a escalação foi lida.</param>
    /// <returns>A versão nova.</returns>
    public Task<int> Atualizar(Escalacao escalacao, int versaoLida, CancellationToken cancelamento = default)
    {
        ArgumentNullException.ThrowIfNull(escalacao);

        var nova = RegistroDeEscalacao.De(escalacao, versaoLida + 1, _tabela.Agora);

        return _tabela.Atualizar(
            nova,
            versaoLida,
            colunas => colunas.SetProperty(registro => registro.Habilitados, nova.Habilitados),
            Restricoes(escalacao),
            cancelamento);
    }

    private static Dictionary<string, string> Restricoes(Escalacao escalacao) => new()
    {
        ["ux_escalacoes_categoria_inicio"] = $"A {escalacao}, já está aberta: cada categoria tem uma escalação por período.",
    };
}
