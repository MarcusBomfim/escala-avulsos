using EscalaAvulsos.Dominio.Identificacao;
using EscalaAvulsos.Dominio.Periodos;
using EscalaAvulsos.Dominio.Requisicoes;
using EscalaAvulsos.Dominio.Trabalhadores;

namespace EscalaAvulsos.Dominio.Escalacoes;

/// <summary>
/// A escala de um período, como sai da escalação: quem pegou cada vaga, as
/// vagas que ficaram vazias e, para cada habilitado, como ele ficou e por quê
/// (ADR-0007).
/// </summary>
/// <remarks>Só nasce pelo <see cref="Escalador"/>.</remarks>
public sealed class Escala
{
    internal Escala(
        Escalacao escalacao,
        DateTimeOffset feitaEm,
        IReadOnlyList<Engajamento> engajamentos,
        IReadOnlyList<VagaNaoPreenchida> vagasNaoPreenchidas,
        IReadOnlyList<SituacaoNaEscala> situacoes,
        IReadOnlyList<RequisicaoId> requisicoesForaDoPrazo,
        Rodizio rodizioDepois)
    {
        Id = EscalaId.Novo();
        Escalacao = escalacao.Id;
        Categoria = escalacao.Categoria;
        Periodo = escalacao.Periodo;
        FeitaEm = feitaEm;
        Engajamentos = engajamentos;
        VagasNaoPreenchidas = vagasNaoPreenchidas;
        Situacoes = situacoes;
        RequisicoesForaDoPrazo = requisicoesForaDoPrazo;
        RodizioDepois = rodizioDepois;
    }

    public EscalaId Id { get; }

    public EscalacaoId Escalacao { get; }

    public Categoria Categoria { get; }

    public Periodo Periodo { get; }

    public DateTimeOffset FeitaEm { get; }

    /// <summary>Quem pegou cada vaga, na ordem das vagas: requisição, terno e a composição do terno.</summary>
    public IReadOnlyList<Engajamento> Engajamentos { get; }

    public IReadOnlyList<VagaNaoPreenchida> VagasNaoPreenchidas { get; }

    /// <summary>Como ficou cada habilitado, na ordem das matrículas.</summary>
    public IReadOnlyList<SituacaoNaEscala> Situacoes { get; }

    /// <summary>As requisições que chegaram depois do prazo e não entraram na escala.</summary>
    public IReadOnlyList<RequisicaoId> RequisicoesForaDoPrazo { get; }

    /// <summary>Onde o rodízio parou depois desta escala: é dele que a próxima escala da categoria começa.</summary>
    public Rodizio RodizioDepois { get; }

    /// <summary>As vagas das requisições que entraram na escala.</summary>
    public int VagasPedidas => Engajamentos.Count + VagasNaoPreenchidas.Sum(vaga => vaga.Quantidade);

    public SituacaoNaEscala? SituacaoDe(Matricula matricula) => Situacoes.SingleOrDefault(situacao => situacao.Matricula == matricula);

    /// <summary>"escala da estiva de 05/11/2026, das 13h às 19h: 3 de 3 vagas preenchidas".</summary>
    public override string ToString() =>
        $"escala {Categoria.ComDe} de {Periodo}: {Engajamentos.Count} de {VagasPedidas} {(VagasPedidas == 1 ? "vaga preenchida" : "vagas preenchidas")}";
}
