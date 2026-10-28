using EscalaAvulsos.Dominio.Escalacoes;
using EscalaAvulsos.Dominio.Periodos;
using EscalaAvulsos.Dominio.Trabalhadores;
using EscalaAvulsos.Infraestrutura.Persistencia;
using EscalaAvulsos.Infraestrutura.Persistencia.Repositorios;

namespace EscalaAvulsos.Api.Recursos.Escalacoes;

/// <summary>A escalação como o órgão gestor a vê: o calendário dela, e quem se habilitou, em que funções, na ordem das matrículas.</summary>
public sealed record EscalacaoResposta(
    Categoria Categoria,
    PeriodoResposta Periodo,
    DateTimeOffset PrazoDasRequisicoes,
    DateTimeOffset AberturaDaHabilitacao,
    DateTimeOffset FechamentoDaHabilitacao,
    IReadOnlyList<HabilitacaoResposta> Habilitacoes)
{
    public static EscalacaoResposta De(Escalacao escalacao)
    {
        ArgumentNullException.ThrowIfNull(escalacao);

        return new(
            escalacao.Categoria,
            PeriodoResposta.De(escalacao.Periodo),
            escalacao.PrazoDasRequisicoes,
            escalacao.AberturaDaHabilitacao,
            escalacao.FechamentoDaHabilitacao,
            [.. escalacao.Habilitacoes.Select(habilitacao => new HabilitacaoResposta(
                habilitacao.Matricula.Numero,
                [.. habilitacao.Funcoes.Select(funcao => funcao.Nome)],
                habilitacao.Em))]);
    }
}

public sealed record HabilitacaoResposta(int Matricula, IReadOnlyList<string> Funcoes, DateTimeOffset Em);

/// <summary>As funções em que o trabalhador quer concorrer, pelo nome.</summary>
public sealed record PedidoDeHabilitacao(IReadOnlyList<string> Funcoes);

/// <summary>
/// A habilitação de quem pede, numa escalação: se ele está habilitado, em que
/// funções, e quando a janela abre e fecha — o que o aplicativo mostra antes
/// e depois de ele se habilitar.
/// </summary>
public sealed record MinhaHabilitacaoResposta(
    PeriodoResposta Periodo,
    DateTimeOffset AbreEm,
    DateTimeOffset FechaEm,
    bool JanelaAberta,
    bool Habilitado,
    IReadOnlyList<string> Funcoes,
    DateTimeOffset? Em)
{
    public static MinhaHabilitacaoResposta De(Escalacao escalacao, TrabalhadorId trabalhador, DateTimeOffset agora)
    {
        ArgumentNullException.ThrowIfNull(escalacao);

        var habilitacao = escalacao.HabilitacaoDe(trabalhador);

        return new(
            PeriodoResposta.De(escalacao.Periodo),
            escalacao.AberturaDaHabilitacao,
            escalacao.FechamentoDaHabilitacao,
            escalacao.HabilitacaoAbertaEm(agora),
            habilitacao is not null,
            [.. habilitacao?.Funcoes.Select(funcao => funcao.Nome) ?? []],
            habilitacao?.Em);
    }
}

/// <summary>Ler a escalação de uma categoria num período — ou 404, dizendo que ela ainda não foi aberta.</summary>
internal static class LeituraDaEscalacao
{
    public static async Task<Versionado<Escalacao>> Ler(
        RepositorioDeEscalacoes repositorio,
        Categoria categoria,
        Periodo periodo,
        CancellationToken cancelamento) =>
        await repositorio.Da(categoria, periodo, cancelamento)
        ?? throw new AgregadoNaoEncontrado($"A escala {categoria.ComDe} de {periodo}, ainda não foi aberta.");
}
