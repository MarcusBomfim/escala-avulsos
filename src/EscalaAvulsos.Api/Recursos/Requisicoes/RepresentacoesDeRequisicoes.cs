using EscalaAvulsos.Dominio.Periodos;
using EscalaAvulsos.Dominio.Requisicoes;
using EscalaAvulsos.Dominio.Trabalhadores;

namespace EscalaAvulsos.Api.Recursos.Requisicoes;

/// <summary>
/// O pedido de mão de obra do operador: a categoria, o navio, o local, o
/// período e os ternos — em cada um, as vagas de cada função, pelo nome da
/// função. O operador é o do token.
/// </summary>
public sealed record NovaRequisicao(
    Categoria Categoria,
    NavioDaRequisicao Navio,
    LocalDaRequisicao Local,
    DateOnly Dia,
    Turno Turno,
    IReadOnlyList<IReadOnlyList<VagasPedidas>> Ternos);

/// <summary>O navio, pelo nome e pelo número IMO, como o operador escreve: <c>IMO 9876543</c> ou <c>9876543</c>.</summary>
public sealed record NavioDaRequisicao(string Nome, string Imo);

public sealed record LocalDaRequisicao(string Terminal, string? Berco = null);

/// <summary>Quantas vagas de uma função num terno: "Estivador", 8.</summary>
public sealed record VagasPedidas(string Funcao, int Quantidade);

public sealed record CancelamentoDaRequisicao(string Motivo);

public sealed record OperadorResposta(string Nome, string Cnpj);

public sealed record NavioResposta(string Nome, string Imo);

public sealed record LocalResposta(string Terminal, string? Berco);

public sealed record TernoResposta(int Numero, IReadOnlyList<VagasPedidas> Vagas);

/// <param name="Descricao">"Cais Forte Operações Portuárias pede 1 terno da estiva, 3 vagas, para o navio Atlântico Sul (IMO 9876543), …".</param>
public sealed record RequisicaoResposta(
    Guid Id,
    string Descricao,
    OperadorResposta Operador,
    NavioResposta Navio,
    LocalResposta Local,
    PeriodoResposta Periodo,
    Categoria Categoria,
    IReadOnlyList<TernoResposta> Ternos,
    int TotalDeVagas,
    DateTimeOffset FeitaEm,
    SituacaoDaRequisicao Situacao,
    string? MotivoDoCancelamento,
    DateTimeOffset? CanceladaEm)
{
    public static RequisicaoResposta De(Requisicao requisicao)
    {
        ArgumentNullException.ThrowIfNull(requisicao);

        return new(
            requisicao.Id.Valor,
            requisicao.ToString(),
            DoOperador(requisicao.Operador),
            DoNavio(requisicao.Navio),
            DoLocal(requisicao.Local),
            PeriodoResposta.De(requisicao.Periodo),
            requisicao.Categoria,
            [.. requisicao.Ternos.Select(terno => new TernoResposta(terno.Numero, [.. terno.Vagas.Select(vagas => new VagasPedidas(vagas.Funcao.Nome, vagas.Quantidade))]))],
            requisicao.TotalDeVagas,
            requisicao.FeitaEm,
            requisicao.Situacao,
            requisicao.MotivoDoCancelamento,
            requisicao.CanceladaEm);
    }

    public static OperadorResposta DoOperador(OperadorPortuario operador)
    {
        ArgumentNullException.ThrowIfNull(operador);

        return new(operador.Nome, operador.Cnpj.Formatado);
    }

    public static NavioResposta DoNavio(Navio navio)
    {
        ArgumentNullException.ThrowIfNull(navio);

        return new(navio.Nome, navio.Imo.ToString());
    }

    public static LocalResposta DoLocal(LocalDaOperacao local)
    {
        ArgumentNullException.ThrowIfNull(local);

        return new(local.Terminal, local.Berco);
    }
}
