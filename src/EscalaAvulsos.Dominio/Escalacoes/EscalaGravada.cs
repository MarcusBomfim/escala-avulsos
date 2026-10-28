using EscalaAvulsos.Dominio.Identificacao;
using EscalaAvulsos.Dominio.Periodos;
using EscalaAvulsos.Dominio.Requisicoes;
using EscalaAvulsos.Dominio.Trabalhadores;

namespace EscalaAvulsos.Dominio.Escalacoes;

/*
 * A escala como é gravada, e de onde ela se refaz (ADR-0011): cada vaga diz
 * só a requisição, o terno e a função, e quem está nela; o navio, o local e
 * o operador vêm uma vez por requisição, e o nome e a matrícula, da situação
 * de quem se habilitou. É a escala que junta as partes, e confere se fecham.
 */

/// <summary>O que a escala guarda de uma requisição: o que o trabalhador precisa saber para chegar.</summary>
internal sealed record RequisicaoNaEscala(RequisicaoId Id, OperadorPortuario Operador, Navio Navio, LocalDaOperacao Local)
{
    public static RequisicaoNaEscala De(Requisicao requisicao) => new(requisicao.Id, requisicao.Operador, requisicao.Navio, requisicao.Local);
}

/// <summary>Uma vaga, sem quem está nela: a requisição, o terno e a função.</summary>
internal sealed record VagaGravada(RequisicaoId Requisicao, int Terno, Funcao Funcao);

/// <summary>Uma vaga, e quem está nela agora.</summary>
internal sealed record VagaOcupada(TrabalhadorId Trabalhador, VagaGravada Vaga);

/// <summary>Quantas vagas iguais ficaram vazias.</summary>
internal sealed record VagasVazias(VagaGravada Vaga, int Quantidade);

/// <summary>A vaga de que o trabalhador desistiu, e quando.</summary>
internal sealed record DesistenciaGravada(VagaGravada Vaga, DateTimeOffset Em);

/// <summary>Como ficou um habilitado, sem a vaga que ele tem agora: essa vem das vagas da escala.</summary>
internal sealed record SituacaoGravada(
    TrabalhadorId Trabalhador,
    Matricula Matricula,
    string Nome,
    IReadOnlyList<Funcao> Funcoes,
    IReadOnlyList<MotivoNaEscala> Motivos,
    bool Substituto,
    DesistenciaGravada? Desistencia,
    Comparecimento Comparecimento,
    DateTimeOffset? PresenteDesde,
    string? JustificativaDaFalta);

/// <summary>A escala inteira, como foi gravada.</summary>
/// <param name="Vagas">As vagas ocupadas, na ordem da escala.</param>
internal sealed record EscalaGravada(
    EscalaId Id,
    EscalacaoId Escalacao,
    Categoria Categoria,
    Periodo Periodo,
    Descanso Descanso,
    DateTimeOffset FeitaEm,
    IReadOnlyList<RequisicaoNaEscala> Requisicoes,
    IReadOnlyList<VagaOcupada> Vagas,
    IReadOnlyList<VagasVazias> VagasNaoPreenchidas,
    IReadOnlyList<SituacaoGravada> Situacoes,
    IReadOnlyList<RequisicaoId> RequisicoesForaDoPrazo,
    Rodizio RodizioDepois,
    IReadOnlyList<EsperaPorVaga> Espera,
    DateTimeOffset? EncerradaEm);
