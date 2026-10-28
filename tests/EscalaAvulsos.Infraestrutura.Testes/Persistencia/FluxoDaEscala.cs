using EscalaAvulsos.Dominio.Escalacoes;
using EscalaAvulsos.Dominio.Requisicoes;
using EscalaAvulsos.Dominio.Trabalhadores;
using EscalaAvulsos.Infraestrutura.Persistencia.Repositorios;

namespace EscalaAvulsos.Infraestrutura.Testes.Persistencia;

/// <summary>
/// A escala feita e desistida como a API vai fazer (Parte 5): ler o que o
/// domínio precisa, decidir no domínio, gravar com as versões lidas. Cada
/// passo na sua sessão, como cada pedido terá a sua.
/// </summary>
internal static class FluxoDaEscala
{
    private static CancellationToken Cancelamento => TestContext.Current.CancellationToken;

    public static RepositorioDeEscalas Escalas(this Sessao sessao) => new(sessao.Contexto, TimeProvider.System);

    /// <summary>Grava os trabalhadores, as requisições e as escalações, como o cadastro, o operador e os habilitados gravariam.</summary>
    public static async Task Gravar(this BancoDoTeste banco, IEnumerable<Trabalhador> trabalhadores, IEnumerable<Requisicao> requisicoes, params Escalacao[] escalacoes)
    {
        await using var sessao = banco.NovaSessao();

        foreach (var trabalhador in trabalhadores)
        {
            await sessao.Trabalhadores.Adicionar(trabalhador, Cancelamento);
        }

        foreach (var requisicao in requisicoes)
        {
            await sessao.Requisicoes.Adicionar(requisicao, Cancelamento);
        }

        foreach (var escalacao in escalacoes)
        {
            await sessao.Escalacoes.Adicionar(escalacao, Cancelamento);
        }
    }

    /// <summary>
    /// Lê a escalação, as requisições do período, os habilitados, o rodízio da
    /// categoria e as jornadas deles; escala; grava com a versão do rodízio.
    /// </summary>
    /// <param name="jornadasVelhas">Em vez das jornadas do banco, nenhuma: como se tivessem sido lidas antes de a outra escala ser gravada.</param>
    public static async Task<Escala> FazerAEscala(this BancoDoTeste banco, EscalacaoId id, DateTimeOffset em, bool jornadasVelhas = false)
    {
        await using var sessao = banco.NovaSessao();
        var escalas = sessao.Escalas();
        var escalacao = (await sessao.Escalacoes.Obter(id, Cancelamento))!.Agregado;
        var requisicoes = await sessao.Requisicoes.DoPeriodo(escalacao.Categoria, escalacao.Periodo, Cancelamento);
        var habilitados = await sessao.Trabalhadores.Obter(escalacao.Habilitacoes.Select(habilitacao => habilitacao.Trabalhador), Cancelamento);
        var rodizio = await escalas.RodizioDa(escalacao.Categoria, Cancelamento);
        var jornadas = jornadasVelhas
            ? []
            : await escalas.JornadasPerto(habilitados.Select(trabalhador => trabalhador.Id), escalacao.Periodo, Descanso.DaLei, Cancelamento);

        var escala = Escalador.Escalar(escalacao, requisicoes, habilitados, jornadas, rodizio.Agregado, Descanso.DaLei, em);
        await escalas.Adicionar(escala, rodizio.Versao, Cancelamento);

        return escala;
    }

    /// <summary>Lê a escala e as jornadas de quem está fora dela; desiste; grava com a versão lida.</summary>
    /// <param name="jornadasVelhas">Em vez das jornadas do banco, nenhuma: como se tivessem sido lidas antes de outra escala ser gravada.</param>
    /// <returns>Quem entrou na vaga, ou nulo, se ela ficou vazia.</returns>
    public static async Task<SituacaoNaEscala?> Desistir(this BancoDoTeste banco, EscalaId id, TrabalhadorId quem, DateTimeOffset em, bool jornadasVelhas = false)
    {
        await using var sessao = banco.NovaSessao();
        var escalas = sessao.Escalas();
        var lida = (await escalas.Obter(id, Cancelamento))!;
        var escala = lida.Agregado;
        var jornadas = jornadasVelhas
            ? []
            : await escalas.JornadasPerto(
                escala.Situacoes.Where(situacao => !situacao.Escalado).Select(situacao => situacao.Trabalhador),
                escala.Periodo,
                escala.Descanso,
                Cancelamento);

        var substituto = escala.Desistir(quem, em, jornadas);
        await escalas.Atualizar(escala, lida.Versao, Cancelamento);

        return substituto;
    }
}
