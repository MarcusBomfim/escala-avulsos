using EscalaAvulsos.Dominio.Escalacoes;
using EscalaAvulsos.Dominio.Identificacao;
using EscalaAvulsos.Dominio.Periodos;
using EscalaAvulsos.Dominio.Requisicoes;
using EscalaAvulsos.Dominio.Trabalhadores;

namespace EscalaAvulsos.Infraestrutura.Documentos;

/// <summary>
/// Como uma escala é gravada: quem está em cada vaga, as vagas vazias, como
/// ficou cada habilitado e por quê, a fila de espera, o rodízio depois dela —
/// e o descanso com que foi feita, que vale também para o substituto.
/// </summary>
/// <remarks>
/// <para>
/// Cada requisição vem uma vez, com o operador, o navio e o local; as vagas
/// dizem só a requisição, o terno e a função. O nome e a matrícula de quem
/// está na vaga vêm da situação dele. É a escala que junta as partes, e
/// recusa as que não fecham (ADR-0011).
/// </para>
/// <para>
/// Os motivos de quem ficou de fora são refeitos pelo tipo e pelos dados,
/// e a frase sai de novo; os da aptidão, não: dizem o que valia naquela hora,
/// e são gravados com a frase.
/// </para>
/// </remarks>
internal sealed record DocumentoDaEscala
{
    public const short Formato = 1;

    public required Guid Escalacao { get; init; }

    public required Categoria Categoria { get; init; }

    public required PeriodoGravado Periodo { get; init; }

    /// <summary>O descanso mínimo entre jornadas, como <c>"11:00:00"</c>.</summary>
    public required TimeSpan Descanso { get; init; }

    public required DateTimeOffset FeitaEm { get; init; }

    public required DateTimeOffset? EncerradaEm { get; init; }

    public required IReadOnlyList<RequisicaoDaEscalaGravada> Requisicoes { get; init; }

    /// <summary>As vagas ocupadas, na ordem da escala.</summary>
    public required IReadOnlyList<OcupacaoGravada> Vagas { get; init; }

    public required IReadOnlyList<VaziasGravadas> VagasNaoPreenchidas { get; init; }

    /// <summary>Na ordem das matrículas.</summary>
    public required IReadOnlyList<SituacaoDaEscalaGravada> Situacoes { get; init; }

    public required IReadOnlyList<Guid> RequisicoesForaDoPrazo { get; init; }

    public required IReadOnlyList<VezGravada> RodizioDepois { get; init; }

    /// <summary>Na ordem em que entram, função por função.</summary>
    public required IReadOnlyList<EsperaGravada> Espera { get; init; }

    public static DocumentoDaEscala De(Escala escala)
    {
        var gravada = escala.Gravada;

        return new()
        {
            Escalacao = gravada.Escalacao.Valor,
            Categoria = gravada.Categoria,
            Periodo = PeriodoGravado.De(gravada.Periodo),
            Descanso = gravada.Descanso.Minimo,
            FeitaEm = gravada.FeitaEm,
            EncerradaEm = gravada.EncerradaEm,
            Requisicoes = [.. gravada.Requisicoes.Select(RequisicaoDaEscalaGravada.De)],
            Vagas = [.. gravada.Vagas.Select(OcupacaoGravada.De)],
            VagasNaoPreenchidas = [.. gravada.VagasNaoPreenchidas.Select(VaziasGravadas.De)],
            Situacoes = [.. gravada.Situacoes.Select(SituacaoDaEscalaGravada.De)],
            RequisicoesForaDoPrazo = [.. gravada.RequisicoesForaDoPrazo.Select(requisicao => requisicao.Valor)],
            RodizioDepois = [.. gravada.RodizioDepois.Vezes.Select(VezGravada.De)],
            Espera = [.. gravada.Espera.Select(EsperaGravada.De)],
        };
    }

    /// <remarks>A escala não é feita de novo: as partes são juntadas e conferidas (ADR-0011).</remarks>
    public Escala ParaEscala(EscalaId id)
    {
        var periodo = Periodo.ParaPeriodo();
        var descanso = new Dominio.Escalacoes.Descanso(Descanso);

        return Escala.Reconstituir(new EscalaGravada(
            id,
            new EscalacaoId(Escalacao),
            Categoria,
            periodo,
            descanso,
            FeitaEm,
            [.. Requisicoes.Select(requisicao => requisicao.ParaRequisicao())],
            [.. Vagas.Select(ocupada => ocupada.ParaVagaOcupada(Categoria))],
            [.. VagasNaoPreenchidas.Select(vazias => vazias.ParaVagasVazias(Categoria))],
            [.. Situacoes.Select(situacao => situacao.ParaSituacao(Categoria, periodo, descanso))],
            [.. RequisicoesForaDoPrazo.Select(requisicao => new RequisicaoId(requisicao))],
            Rodizio.De(RodizioDepois.Select(vez => vez.ParaVez(Categoria))),
            [.. Espera.Select(esperando => esperando.ParaEspera(Categoria))],
            EncerradaEm));
    }
}

/// <summary>O que a escala guarda de uma requisição.</summary>
internal sealed record RequisicaoDaEscalaGravada
{
    public required Guid Id { get; init; }

    public required OperadorGravado Operador { get; init; }

    public required NavioGravado Navio { get; init; }

    public required LocalGravado Local { get; init; }

    public static RequisicaoDaEscalaGravada De(RequisicaoNaEscala requisicao) => new()
    {
        Id = requisicao.Id.Valor,
        Operador = OperadorGravado.De(requisicao.Operador),
        Navio = NavioGravado.De(requisicao.Navio),
        Local = LocalGravado.De(requisicao.Local),
    };

    public RequisicaoNaEscala ParaRequisicao() => new(new RequisicaoId(Id), Operador.ParaOperador(), Navio.ParaNavio(), Local.ParaLocal());
}

/// <summary>Uma vaga ocupada: quem está nela, e a requisição, o terno e a função.</summary>
internal sealed record OcupacaoGravada
{
    public required Guid Trabalhador { get; init; }

    public required Guid Requisicao { get; init; }

    public required int Terno { get; init; }

    public required string Funcao { get; init; }

    public static OcupacaoGravada De(VagaOcupada ocupada) => new()
    {
        Trabalhador = ocupada.Trabalhador.Valor,
        Requisicao = ocupada.Vaga.Requisicao.Valor,
        Terno = ocupada.Vaga.Terno,
        Funcao = ocupada.Vaga.Funcao.Nome,
    };

    public VagaOcupada ParaVagaOcupada(Categoria categoria) =>
        new(new TrabalhadorId(Trabalhador), new VagaGravada(new RequisicaoId(Requisicao), Terno, new Funcao(categoria, Funcao)));
}

internal sealed record VaziasGravadas
{
    public required Guid Requisicao { get; init; }

    public required int Terno { get; init; }

    public required string Funcao { get; init; }

    public required int Quantidade { get; init; }

    public static VaziasGravadas De(VagasVazias vazias) => new()
    {
        Requisicao = vazias.Vaga.Requisicao.Valor,
        Terno = vazias.Vaga.Terno,
        Funcao = vazias.Vaga.Funcao.Nome,
        Quantidade = vazias.Quantidade,
    };

    public VagasVazias ParaVagasVazias(Categoria categoria) =>
        new(new VagaGravada(new RequisicaoId(Requisicao), Terno, new Funcao(categoria, Funcao)), Quantidade);
}

/// <summary>Como ficou um habilitado. A vaga que ele tem agora está nas vagas da escala.</summary>
internal sealed record SituacaoDaEscalaGravada
{
    public required Guid Trabalhador { get; init; }

    public required int Matricula { get; init; }

    public required string Nome { get; init; }

    /// <summary>As funções em que ele se habilitou, na ordem em que escolheu.</summary>
    public required IReadOnlyList<string> Funcoes { get; init; }

    public required IReadOnlyList<MotivoNaEscalaGravado> Motivos { get; init; }

    public required bool Substituto { get; init; }

    public required DesistenciaDaVagaGravada? Desistencia { get; init; }

    public required Comparecimento Comparecimento { get; init; }

    public required DateTimeOffset? PresenteDesde { get; init; }

    public required string? JustificativaDaFalta { get; init; }

    public static SituacaoDaEscalaGravada De(SituacaoGravada situacao) => new()
    {
        Trabalhador = situacao.Trabalhador.Valor,
        Matricula = situacao.Matricula.Numero,
        Nome = situacao.Nome,
        Funcoes = [.. situacao.Funcoes.Select(funcao => funcao.Nome)],
        Motivos = [.. situacao.Motivos.Select(MotivoNaEscalaGravado.De)],
        Substituto = situacao.Substituto,
        Desistencia = situacao.Desistencia is { } desistencia
            ? new DesistenciaDaVagaGravada
            {
                Requisicao = desistencia.Vaga.Requisicao.Valor,
                Terno = desistencia.Vaga.Terno,
                Funcao = desistencia.Vaga.Funcao.Nome,
                Em = desistencia.Em,
            }
            : null,
        Comparecimento = situacao.Comparecimento,
        PresenteDesde = situacao.PresenteDesde,
        JustificativaDaFalta = situacao.JustificativaDaFalta,
    };

    public SituacaoGravada ParaSituacao(Categoria categoria, Periodo periodo, Dominio.Escalacoes.Descanso descanso) =>
        new(
            new TrabalhadorId(Trabalhador),
            Dominio.Identificacao.Matricula.De(Matricula),
            Nome,
            [.. Funcoes.Select(funcao => new Funcao(categoria, funcao))],
            [.. Motivos.Select(motivo => motivo.ParaMotivo(categoria, periodo, descanso))],
            Substituto,
            Desistencia is { } desistencia
                ? new DesistenciaGravada(new VagaGravada(new RequisicaoId(desistencia.Requisicao), desistencia.Terno, new Funcao(categoria, desistencia.Funcao)), desistencia.Em)
                : null,
            Comparecimento,
            PresenteDesde,
            JustificativaDaFalta);
}

/// <summary>A vaga de que o habilitado desistiu, e quando.</summary>
internal sealed record DesistenciaDaVagaGravada
{
    public required Guid Requisicao { get; init; }

    public required int Terno { get; init; }

    public required string Funcao { get; init; }

    public required DateTimeOffset Em { get; init; }
}

/// <summary>Por que o habilitado não pegou vaga numa função: o tipo e o que o explica, sem a frase.</summary>
internal sealed record MotivoNaEscalaGravado
{
    public required TipoDeMotivoNaEscala Tipo { get; init; }

    /// <summary>Nula no descanso, que vale para todas as funções.</summary>
    public required string? Funcao { get; init; }

    public required int? PosicoesAntesDaVez { get; init; }

    public required IReadOnlyList<MotivoDaAptidaoGravado> MotivosDaAptidao { get; init; }

    /// <summary>A jornada que pedia descanso; o descanso é o da escala.</summary>
    public required PeriodoGravado? Jornada { get; init; }

    public static MotivoNaEscalaGravado De(MotivoNaEscala motivo) => new()
    {
        Tipo = motivo.Tipo,
        Funcao = motivo.Funcao?.Nome,
        PosicoesAntesDaVez = motivo.PosicoesAntesDaVez,
        MotivosDaAptidao = [.. motivo.MotivosDaAptidao.Select(MotivoDaAptidaoGravado.Do)],
        Jornada = motivo.Descanso is { } conflito ? PeriodoGravado.De(conflito.Jornada) : null,
    };

    public MotivoNaEscala ParaMotivo(Categoria categoria, Periodo periodo, Dominio.Escalacoes.Descanso descanso) =>
        MotivoNaEscala.Reconstituir(
            Tipo,
            Funcao is null ? null : new Funcao(categoria, Funcao),
            PosicoesAntesDaVez,
            [.. MotivosDaAptidao.Select(motivo => motivo.ParaMotivo())],
            Jornada is { } jornada ? new ConflitoDeDescanso(periodo, jornada.ParaPeriodo(), descanso.Minimo) : null);
}

/// <summary>Um motivo da aptidão, com a frase que foi dita naquela hora.</summary>
internal sealed record MotivoDaAptidaoGravado
{
    public required TipoDeMotivo Tipo { get; init; }

    public required string Descricao { get; init; }

    public required DateOnly? De { get; init; }

    public required DateOnly? Ate { get; init; }

    public static MotivoDaAptidaoGravado Do(Motivo motivo) => new() { Tipo = motivo.Tipo, Descricao = motivo.Descricao, De = motivo.De, Ate = motivo.Ate };

    public Motivo ParaMotivo() => Motivo.Reconstituir(Tipo, Descricao, De, Ate);
}

/// <summary>Onde o rodízio de uma função parou: a matrícula do último escalado.</summary>
internal sealed record VezGravada
{
    public required string Funcao { get; init; }

    public required Inscricao Inscricao { get; init; }

    public required int UltimoEscalado { get; init; }

    public static VezGravada De(VezNoRodizio vez) => new()
    {
        Funcao = vez.Funcao.Nome,
        Inscricao = vez.Inscricao,
        UltimoEscalado = vez.UltimoEscalado.Numero,
    };

    public VezNoRodizio ParaVez(Categoria categoria) => new(new Funcao(categoria, Funcao), Inscricao, Matricula.De(UltimoEscalado));
}

internal sealed record EsperaGravada
{
    public required Guid Trabalhador { get; init; }

    public required int Matricula { get; init; }

    public required string Funcao { get; init; }

    public required Inscricao Inscricao { get; init; }

    public static EsperaGravada De(EsperaPorVaga esperando) => new()
    {
        Trabalhador = esperando.Trabalhador.Valor,
        Matricula = esperando.Matricula.Numero,
        Funcao = esperando.Funcao.Nome,
        Inscricao = esperando.Inscricao,
    };

    public EsperaPorVaga ParaEspera(Categoria categoria) =>
        new(new TrabalhadorId(Trabalhador), new Funcao(categoria, Funcao), Inscricao, Dominio.Identificacao.Matricula.De(Matricula));
}
