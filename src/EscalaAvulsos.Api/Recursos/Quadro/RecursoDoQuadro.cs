using EscalaAvulsos.Api.Recursos.Requisicoes;
using EscalaAvulsos.Api.Seguranca;
using EscalaAvulsos.Dominio.Escalacoes;
using EscalaAvulsos.Dominio.Periodos;
using EscalaAvulsos.Dominio.Requisicoes;
using EscalaAvulsos.Dominio.Trabalhadores;
using EscalaAvulsos.Infraestrutura.Persistencia.Repositorios;
using Microsoft.AspNetCore.Http.HttpResults;

namespace EscalaAvulsos.Api.Recursos.Quadro;

/// <summary>Em que pé está a escalação da categoria — o que o quadro de hoje chama de "Travada" é a habilitação fechada, esperando a escala.</summary>
public enum SituacaoDaCategoriaNoQuadro
{
    /// <summary>Há requisição, mas a escalação do período ainda não foi aberta.</summary>
    SemEscalacao,

    HabilitacaoVaiAbrir,

    HabilitacaoAberta,

    /// <summary>A habilitação fechou, e a escala ainda não saiu.</summary>
    Travada,

    /// <summary>A escala saiu.</summary>
    Escalada,

    /// <summary>O período acabou, e a escala foi encerrada.</summary>
    Encerrada,
}

/// <summary>Como está o terno de uma requisição.</summary>
public enum SituacaoDoTernoNoQuadro
{
    /// <summary>A escala ainda não saiu.</summary>
    AguardandoAEscala,

    /// <summary>Todas as vagas do terno têm alguém.</summary>
    Completo,

    /// <summary>Faltou gente: há vagas em aberto.</summary>
    Incompleto,

    /// <summary>O operador cancelou a requisição.</summary>
    Cancelado,

    /// <summary>A requisição chegou depois do prazo, e não entrou na escala.</summary>
    ForaDoPrazo,
}

public sealed record PessoaNoQuadro(int Matricula, string Nome);

/// <summary>As vagas de uma função no terno, e quem está nelas.</summary>
public sealed record FuncaoNoQuadro(string Funcao, int Pedidas, IReadOnlyList<PessoaNoQuadro> Escalados);

/// <summary>Uma linha do quadro: um terno de uma requisição, com as vagas pedidas, as escaladas e as em aberto.</summary>
public sealed record TernoNoQuadro(
    Guid Requisicao,
    OperadorResposta Operador,
    NavioResposta Navio,
    LocalResposta Local,
    int Terno,
    int Pedidas,
    int Escaladas,
    int EmAberto,
    SituacaoDoTernoNoQuadro Situacao,
    IReadOnlyList<FuncaoNoQuadro> Funcoes);

/// <summary>Uma categoria no quadro: a escalação dela no período, e os ternos pedidos.</summary>
/// <param name="AbreEm">Quando a habilitação abre; nulo, sem escalação.</param>
/// <param name="FechaEm">Quando a habilitação fecha; nulo, sem escalação.</param>
public sealed record CategoriaNoQuadro(
    Categoria Categoria,
    SituacaoDaCategoriaNoQuadro Situacao,
    DateTimeOffset? AbreEm,
    DateTimeOffset? FechaEm,
    int Habilitados,
    IReadOnlyList<TernoNoQuadro> Ternos);

/// <summary>O quadro de um período: como no quadro de parede, as categorias, e em cada uma os ternos pedidos.</summary>
public sealed record QuadroResposta(PeriodoResposta Periodo, IReadOnlyList<CategoriaNoQuadro> Categorias);

/// <summary>
/// O quadro do período, como o de parede da escala de hoje (ADR-0021): para
/// cada categoria, a situação da escalação e, em cada terno pedido, as vagas,
/// as escaladas e as em aberto, com quem está em cada uma. O escalador vê
/// todas as requisições; o operador, só as dele.
/// </summary>
public sealed class RecursoDoQuadro : IRecursoDaApi
{
    public void Mapear(IEndpointRouteBuilder api)
    {
        api.MapGet("/quadro/{dia}/{turno}", Obter).WithTags("Quadro").RequireAuthorization(Politicas.VerOQuadro);
    }

    private static async Task<Ok<QuadroResposta>> Obter(
        string dia,
        string turno,
        RepositorioDeEscalacoes escalacoes,
        RepositorioDeRequisicoes requisicoes,
        RepositorioDeEscalas escalas,
        TimeProvider relogio,
        HttpContext http,
        CancellationToken cancelamento)
    {
        var periodo = TextosDoPedido.ComoPeriodo(dia, turno);
        var operador = http.User.IsInRole(Papeis.Escalador) ? null : QuemPede.LerCnpj(http.User);
        var agora = relogio.GetUtcNow();

        var doPeriodo = (await escalacoes.QueComecamEntre(periodo.Inicio, periodo.Fim, cancelamento))
            .Where(escalacao => escalacao.Periodo == periodo)
            .ToDictionary(escalacao => escalacao.Categoria);

        var categorias = new List<CategoriaNoQuadro>();

        foreach (var categoria in Enum.GetValues<Categoria>())
        {
            var pedidas = (await requisicoes.DoPeriodo(categoria, periodo, cancelamento))
                .Where(requisicao => operador is null || requisicao.Operador.Cnpj == operador)
                .ToList();
            doPeriodo.TryGetValue(categoria, out var escalacao);

            // O operador vê as categorias em que pediu; o escalador, também as que têm escalação aberta.
            if (pedidas.Count == 0 && (escalacao is null || operador is not null))
            {
                continue;
            }

            var escala = escalacao is null ? null : (await escalas.DaEscalacao(escalacao.Id, cancelamento))?.Agregado;

            categorias.Add(new CategoriaNoQuadro(
                categoria,
                Situacao(escalacao, escala, agora),
                escalacao?.AberturaDaHabilitacao,
                escalacao?.FechamentoDaHabilitacao,
                escalacao?.Habilitacoes.Count ?? 0,
                [.. pedidas.SelectMany(requisicao => Ternos(requisicao, escala))]));
        }

        return TypedResults.Ok(new QuadroResposta(PeriodoResposta.De(periodo), categorias));
    }

    private static SituacaoDaCategoriaNoQuadro Situacao(Escalacao? escalacao, Escala? escala, DateTimeOffset agora) =>
        (escalacao, escala) switch
        {
            (null, _) => SituacaoDaCategoriaNoQuadro.SemEscalacao,
            (_, { EncerradaEm: not null }) => SituacaoDaCategoriaNoQuadro.Encerrada,
            (_, not null) => SituacaoDaCategoriaNoQuadro.Escalada,
            _ when agora < escalacao.AberturaDaHabilitacao => SituacaoDaCategoriaNoQuadro.HabilitacaoVaiAbrir,
            _ when escalacao.HabilitacaoAbertaEm(agora) => SituacaoDaCategoriaNoQuadro.HabilitacaoAberta,
            _ => SituacaoDaCategoriaNoQuadro.Travada,
        };

    /// <summary>Uma linha por terno da requisição, com quem a escala pôs em cada vaga.</summary>
    private static IEnumerable<TernoNoQuadro> Ternos(Requisicao requisicao, Escala? escala)
    {
        // Fora do prazo: a que a escala deixou de fora pelo prazo, e a que chegou depois de a escala sair, que ela nem viu.
        var vistaPelaEscala = escala is null
            || escala.Engajamentos.Any(vaga => vaga.Requisicao == requisicao.Id)
            || escala.VagasNaoPreenchidas.Any(vaga => vaga.Requisicao == requisicao.Id);
        var foraDoPrazo = escala is not null && (escala.RequisicoesForaDoPrazo.Contains(requisicao.Id) || !vistaPelaEscala);

        foreach (var terno in requisicao.Ternos)
        {
            var noTerno = escala?.Engajamentos.Where(vaga => vaga.Requisicao == requisicao.Id && vaga.Terno == terno.Numero).ToList() ?? [];
            var funcoes = terno.Vagas
                .Select(vagas => new FuncaoNoQuadro(
                    vagas.Funcao.Nome,
                    vagas.Quantidade,
                    [.. noTerno.Where(vaga => vaga.Funcao == vagas.Funcao).Select(vaga => new PessoaNoQuadro(vaga.Matricula.Numero, vaga.Nome))]))
                .ToList();
            var escaladas = noTerno.Count;

            var situacao = requisicao.Situacao == SituacaoDaRequisicao.Cancelada ? SituacaoDoTernoNoQuadro.Cancelado
                : foraDoPrazo ? SituacaoDoTernoNoQuadro.ForaDoPrazo
                : escala is null ? SituacaoDoTernoNoQuadro.AguardandoAEscala
                : escaladas == terno.TotalDeVagas ? SituacaoDoTernoNoQuadro.Completo
                : SituacaoDoTernoNoQuadro.Incompleto;

            yield return new TernoNoQuadro(
                requisicao.Id.Valor,
                RequisicaoResposta.DoOperador(requisicao.Operador),
                RequisicaoResposta.DoNavio(requisicao.Navio),
                RequisicaoResposta.DoLocal(requisicao.Local),
                terno.Numero,
                terno.TotalDeVagas,
                escaladas,
                situacao is SituacaoDoTernoNoQuadro.Cancelado or SituacaoDoTernoNoQuadro.ForaDoPrazo ? 0 : terno.TotalDeVagas - escaladas,
                situacao,
                funcoes);
        }
    }
}
