using EscalaAvulsos.Api.Recursos.Requisicoes;
using EscalaAvulsos.Dominio.Escalacoes;
using EscalaAvulsos.Dominio.Extratos;
using EscalaAvulsos.Dominio.Frequencias;
using EscalaAvulsos.Dominio.Trabalhadores;

namespace EscalaAvulsos.Api.Recursos.Escalas;

/// <summary>O trabalhador numa vaga, ou de quem se fala numa ação do escalador: pela matrícula.</summary>
public sealed record TrabalhadorDaEscala(int Matricula);

/// <summary>A falta justificada: de quem, e por quê — o atestado, por exemplo.</summary>
public sealed record JustificativaDaFalta(int Matricula, string Justificativa);

/// <summary>Uma vaga: a função, o terno, o navio, o local e quem pediu — tudo o que o trabalhador precisa para chegar.</summary>
public sealed record VagaResposta(
    string Funcao,
    int Terno,
    Guid Requisicao,
    NavioResposta Navio,
    LocalResposta Local,
    OperadorResposta Operador)
{
    public static VagaResposta De(Engajamento vaga)
    {
        ArgumentNullException.ThrowIfNull(vaga);

        return new(
            vaga.Funcao.Nome,
            vaga.Terno,
            vaga.Requisicao.Valor,
            RequisicaoResposta.DoNavio(vaga.Navio),
            RequisicaoResposta.DoLocal(vaga.Local),
            RequisicaoResposta.DoOperador(vaga.Operador));
    }
}

/// <summary>Por que ficou de fora numa função: o tipo, a função e a frase.</summary>
public sealed record MotivoResposta(TipoDeMotivoNaEscala Tipo, string? Funcao, string Descricao);

/// <summary>Como ficou um habilitado: a frase, a vaga se tem uma, a presença ou a falta, e os motivos de cada função.</summary>
public sealed record SituacaoResposta(
    int Matricula,
    string Nome,
    string Frase,
    VagaResposta? Vaga,
    bool Substituto,
    VagaResposta? VagaDesistida,
    DateTimeOffset? DesistiuEm,
    Comparecimento Comparecimento,
    DateTimeOffset? PresenteDesde,
    string? JustificativaDaFalta,
    IReadOnlyList<MotivoResposta> Motivos)
{
    public static SituacaoResposta De(SituacaoNaEscala situacao)
    {
        ArgumentNullException.ThrowIfNull(situacao);

        return new(
            situacao.Matricula.Numero,
            situacao.Nome,
            situacao.ToString(),
            situacao.Engajamento is { } vaga ? VagaResposta.De(vaga) : null,
            situacao.Substituto,
            situacao.VagaDesistida is { } desistida ? VagaResposta.De(desistida) : null,
            situacao.DesistiuEm,
            situacao.Comparecimento,
            situacao.PresenteDesde,
            situacao.JustificativaDaFalta,
            [.. situacao.Motivos.Select(motivo => new MotivoResposta(motivo.Tipo, motivo.Funcao?.Nome, motivo.Descricao))]);
    }
}

/// <summary>As vagas de uma função, num terno, que ficaram vazias.</summary>
public sealed record VagasVaziasResposta(string Funcao, int Terno, int Quantidade, Guid Requisicao, NavioResposta Navio, OperadorResposta Operador, string Descricao);

/// <summary>Quem espera uma vaga de cada função, na ordem em que entra; as funções, na ordem em que aparecem nas habilitações, pela matrícula.</summary>
public sealed record EsperaResposta(string Funcao, IReadOnlyList<int> Matriculas);

/// <summary>A escala inteira, como o órgão gestor a vê.</summary>
public sealed record EscalaResposta(
    Guid Id,
    string Descricao,
    Categoria Categoria,
    PeriodoResposta Periodo,
    DateTimeOffset FeitaEm,
    DateTimeOffset? EncerradaEm,
    TimeSpan Descanso,
    int VagasPedidas,
    IReadOnlyList<SituacaoResposta> Vagas,
    IReadOnlyList<VagasVaziasResposta> VagasNaoPreenchidas,
    IReadOnlyList<SituacaoResposta> Situacoes,
    IReadOnlyList<EsperaResposta> Espera,
    IReadOnlyList<Guid> RequisicoesForaDoPrazo)
{
    public static EscalaResposta De(Escala escala)
    {
        ArgumentNullException.ThrowIfNull(escala);

        var situacoes = escala.Situacoes.ToDictionary(situacao => situacao.Trabalhador);
        var funcoes = escala.Situacoes.SelectMany(situacao => situacao.Funcoes).Distinct();

        return new(
            escala.Id.Valor,
            escala.ToString(),
            escala.Categoria,
            PeriodoResposta.De(escala.Periodo),
            escala.FeitaEm,
            escala.EncerradaEm,
            escala.Descanso.Minimo,
            escala.VagasPedidas,
            [.. escala.Engajamentos.Select(vaga => SituacaoResposta.De(situacoes[vaga.Trabalhador]))],
            [.. escala.VagasNaoPreenchidas.Select(vazias => new VagasVaziasResposta(
                vazias.Funcao.Nome,
                vazias.Terno,
                vazias.Quantidade,
                vazias.Requisicao.Valor,
                RequisicaoResposta.DoNavio(vazias.Navio),
                RequisicaoResposta.DoOperador(vazias.Operador),
                vazias.ToString()))],
            [.. escala.Situacoes.Select(SituacaoResposta.De)],
            [
                .. funcoes
                    .Select(funcao => new EsperaResposta(funcao.Nome, [.. escala.FilaDeEsperaDe(funcao).Select(matricula => matricula.Numero)]))
                    .Where(espera => espera.Matriculas.Count > 0),
            ],
            [.. escala.RequisicoesForaDoPrazo.Select(requisicao => requisicao.Valor)]);
    }
}

/// <summary>
/// Uma escala, como o trabalhador a vê no celular: a frase dele, a vaga com
/// tudo o que precisa para chegar, ou por que ficou de fora — e, se ficou, em
/// que lugar está na espera de cada função.
/// </summary>
public sealed record MinhaEscalaResposta(
    Guid Escala,
    Categoria Categoria,
    PeriodoResposta Periodo,
    bool Encerrada,
    SituacaoResposta Situacao,
    IReadOnlyList<PosicaoNaEspera> Espera)
{
    public static MinhaEscalaResposta De(Escala escala, SituacaoNaEscala minha)
    {
        ArgumentNullException.ThrowIfNull(escala);
        ArgumentNullException.ThrowIfNull(minha);

        return new(
            escala.Id.Valor,
            escala.Categoria,
            PeriodoResposta.De(escala.Periodo),
            escala.EncerradaEm is not null,
            SituacaoResposta.De(minha),
            [
                .. minha.Funcoes
                    .Select(funcao => new PosicaoNaEspera(funcao.Nome, escala.FilaDeEsperaDe(funcao).ToList().IndexOf(minha.Matricula) + 1))
                    .Where(espera => espera.Posicao > 0),
            ]);
    }
}

/// <summary>Em que lugar da espera de uma função ele está: 1 é o primeiro a entrar se alguém desistir.</summary>
public sealed record PosicaoNaEspera(string Funcao, int Posicao);

/// <summary>O mês do trabalhador: a frequência, e o extrato do que ele trabalhou.</summary>
public sealed record MeuMesResposta(string Mes, FrequenciaResposta Frequencia, ExtratoResposta Extrato);

public sealed record FrequenciaResposta(
    string Descricao,
    SituacaoDaFrequencia Situacao,
    int Habilitacoes,
    int MinimoDeHabilitacoes,
    int HabilitacoesQueFaltam,
    int Trabalhados,
    int Faltas,
    int FaltasJustificadas,
    int MaximoDeFaltas,
    int Desistencias,
    int DiasImpedidos)
{
    public static FrequenciaResposta De(FrequenciaDoMes frequencia)
    {
        ArgumentNullException.ThrowIfNull(frequencia);

        return new(
            frequencia.ToString(),
            frequencia.Situacao,
            frequencia.Habilitacoes,
            frequencia.MinimoDeHabilitacoes,
            frequencia.HabilitacoesQueFaltam,
            frequencia.Trabalhados,
            frequencia.Faltas,
            frequencia.FaltasJustificadas,
            frequencia.MaximoDeFaltas,
            frequencia.Desistencias,
            frequencia.DiasImpedidos);
    }
}

public sealed record ExtratoResposta(
    string Descricao,
    IReadOnlyList<LinhaDoExtratoResposta> Linhas,
    decimal Remuneracao,
    decimal Ferias,
    decimal DecimoTerceiro,
    decimal Fgts)
{
    public static ExtratoResposta De(Extrato extrato)
    {
        ArgumentNullException.ThrowIfNull(extrato);

        return new(
            extrato.ToString(),
            [.. extrato.Linhas.Select(linha => new LinhaDoExtratoResposta(
                linha.ToString(),
                PeriodoResposta.De(linha.Periodo),
                linha.Funcao.Nome,
                RequisicaoResposta.DoNavio(linha.Navio),
                linha.Terno,
                linha.Valor.Valor,
                linha.AdicionalNoturno.Valor,
                linha.Total.Valor,
                linha.PagoAte))],
            extrato.Remuneracao.Valor,
            extrato.Ferias.Valor,
            extrato.DecimoTerceiro.Valor,
            extrato.Fgts.Valor);
    }
}

public sealed record LinhaDoExtratoResposta(
    string Descricao,
    PeriodoResposta Periodo,
    string Funcao,
    NavioResposta Navio,
    int Terno,
    decimal Valor,
    decimal AdicionalNoturno,
    decimal Total,
    DateTimeOffset PagoAte);
