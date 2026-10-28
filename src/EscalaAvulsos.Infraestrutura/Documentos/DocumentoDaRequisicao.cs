using EscalaAvulsos.Dominio.Requisicoes;
using EscalaAvulsos.Dominio.Trabalhadores;

namespace EscalaAvulsos.Infraestrutura.Documentos;

/// <summary>
/// Como uma requisição é gravada: o que o operador pediu — o navio, o local, o
/// período e os ternos, na ordem —, e o cancelamento, se houve.
/// </summary>
/// <remarks>
/// A categoria fica uma vez só, no alto: as funções dos ternos vão pelo nome,
/// e são todas dela (ADR-0004).
/// </remarks>
internal sealed record DocumentoDaRequisicao
{
    public const short Formato = 1;

    public required OperadorGravado Operador { get; init; }

    public required NavioGravado Navio { get; init; }

    public required LocalGravado Local { get; init; }

    public required PeriodoGravado Periodo { get; init; }

    public required Categoria Categoria { get; init; }

    /// <summary>As vagas de cada terno; o primeiro da lista é o terno 1.</summary>
    public required IReadOnlyList<IReadOnlyList<VagasGravadas>> Ternos { get; init; }

    public required DateTimeOffset FeitaEm { get; init; }

    public required CancelamentoDaRequisicaoGravado? Cancelamento { get; init; }

    public static DocumentoDaRequisicao De(Requisicao requisicao) => new()
    {
        Operador = OperadorGravado.De(requisicao.Operador),
        Navio = NavioGravado.De(requisicao.Navio),
        Local = LocalGravado.De(requisicao.Local),
        Periodo = PeriodoGravado.De(requisicao.Periodo),
        Categoria = requisicao.Categoria,
        Ternos = [.. requisicao.Ternos.Select(terno => (IReadOnlyList<VagasGravadas>)[.. terno.Vagas.Select(VagasGravadas.De)])],
        FeitaEm = requisicao.FeitaEm,
        Cancelamento = requisicao.CanceladaEm is { } em
            ? new CancelamentoDaRequisicaoGravado { Motivo = requisicao.MotivoDoCancelamento!, Em = em }
            : null,
    };

    /// <remarks>O pedido e o cancelamento são feitos de novo, pelas regras de sempre (ADR-0011).</remarks>
    public Requisicao ParaRequisicao(RequisicaoId id) =>
        Requisicao.Reconstituir(
            id,
            new DadosDaRequisicao(Operador.ParaOperador(), Navio.ParaNavio(), Local.ParaLocal(), Periodo.ParaPeriodo()),
            [.. Ternos.Select(terno => (IReadOnlyList<VagasDaFuncao>)[.. terno.Select(vagas => vagas.ParaVagas(Categoria))])],
            FeitaEm,
            Cancelamento is { } cancelamento ? (cancelamento.Motivo, cancelamento.Em) : null);
}

internal sealed record VagasGravadas
{
    public required string Funcao { get; init; }

    public required int Quantidade { get; init; }

    public static VagasGravadas De(VagasDaFuncao vagas) => new() { Funcao = vagas.Funcao.Nome, Quantidade = vagas.Quantidade };

    public VagasDaFuncao ParaVagas(Categoria categoria) => new(new Funcao(categoria, Funcao), Quantidade);
}

internal sealed record CancelamentoDaRequisicaoGravado
{
    public required string Motivo { get; init; }

    public required DateTimeOffset Em { get; init; }
}
