using EscalaAvulsos.Dominio.Comum;
using EscalaAvulsos.Dominio.Identificacao;
using EscalaAvulsos.Dominio.Periodos;
using EscalaAvulsos.Dominio.Requisicoes;
using EscalaAvulsos.Dominio.Trabalhadores;

namespace EscalaAvulsos.Dominio.Escalacoes;

/// <summary>
/// A escala de um período, como sai da escalação: quem pegou cada vaga, as
/// vagas que ficaram vazias e, para cada habilitado, como ele ficou e por quê
/// (ADR-0007) — e o que acontece depois dela, até o fim do período: as
/// desistências, os substitutos, as presenças e as faltas (ADR-0008).
/// </summary>
/// <remarks>Só nasce pelo <see cref="Escalador"/>, ou refeita da escala gravada (ADR-0011).</remarks>
public sealed class Escala
{
    /// <summary>Quanto antes do período a presença pode ser confirmada: quem chega cedo ao cais.</summary>
    public static readonly TimeSpan AntecedenciaDaPresenca = TimeSpan.FromHours(1);

    public const int TamanhoMaximoDaJustificativa = 200;

    private readonly List<Engajamento> _engajamentos;

    private readonly List<VagaNaoPreenchida> _vagasNaoPreenchidas;

    private readonly List<EsperaPorVaga> _espera;

    internal Escala(
        Escalacao escalacao,
        Descanso descanso,
        DateTimeOffset feitaEm,
        IReadOnlyList<Engajamento> engajamentos,
        IReadOnlyList<VagaNaoPreenchida> vagasNaoPreenchidas,
        IReadOnlyList<SituacaoNaEscala> situacoes,
        IReadOnlyList<RequisicaoId> requisicoesForaDoPrazo,
        Rodizio rodizioDepois,
        IReadOnlyList<EsperaPorVaga> espera)
        : this(
            EscalaId.Novo(),
            escalacao.Id,
            escalacao.Categoria,
            escalacao.Periodo,
            descanso,
            feitaEm,
            engajamentos,
            vagasNaoPreenchidas,
            situacoes,
            requisicoesForaDoPrazo,
            rodizioDepois,
            espera)
    {
    }

    private Escala(
        EscalaId id,
        EscalacaoId escalacao,
        Categoria categoria,
        Periodo periodo,
        Descanso descanso,
        DateTimeOffset feitaEm,
        IReadOnlyList<Engajamento> engajamentos,
        IReadOnlyList<VagaNaoPreenchida> vagasNaoPreenchidas,
        IReadOnlyList<SituacaoNaEscala> situacoes,
        IReadOnlyList<RequisicaoId> requisicoesForaDoPrazo,
        Rodizio rodizioDepois,
        IReadOnlyList<EsperaPorVaga> espera)
    {
        Id = id;
        Escalacao = escalacao;
        Categoria = categoria;
        Periodo = periodo;
        Descanso = descanso;
        FeitaEm = feitaEm;
        _engajamentos = [.. engajamentos];
        _vagasNaoPreenchidas = [.. vagasNaoPreenchidas];
        Situacoes = situacoes;
        RequisicoesForaDoPrazo = requisicoesForaDoPrazo;
        RodizioDepois = rodizioDepois;
        _espera = [.. espera];
    }

    public EscalaId Id { get; }

    public EscalacaoId Escalacao { get; }

    public Categoria Categoria { get; }

    public Periodo Periodo { get; }

    /// <summary>O descanso entre jornadas com que a escala foi feita — o mesmo que vale para quem entra no lugar de quem desiste.</summary>
    public Descanso Descanso { get; }

    public DateTimeOffset FeitaEm { get; }

    /// <summary>Quem tem cada vaga agora, na ordem das vagas: requisição, terno e a composição do terno.</summary>
    public IReadOnlyList<Engajamento> Engajamentos => _engajamentos.AsReadOnly();

    public IReadOnlyList<VagaNaoPreenchida> VagasNaoPreenchidas => _vagasNaoPreenchidas.AsReadOnly();

    /// <summary>Como está cada habilitado, na ordem das matrículas.</summary>
    public IReadOnlyList<SituacaoNaEscala> Situacoes { get; }

    /// <summary>As requisições que chegaram depois do prazo e não entraram na escala.</summary>
    public IReadOnlyList<RequisicaoId> RequisicoesForaDoPrazo { get; }

    /// <summary>
    /// Onde o rodízio parou: é dele que a próxima escala da categoria começa. O
    /// substituto de quem desiste anda a vez até ele.
    /// </summary>
    public Rodizio RodizioDepois { get; private set; }

    /// <summary>Quando a escala foi encerrada, depois do fim do período; nulo, enquanto não foi.</summary>
    public DateTimeOffset? EncerradaEm { get; private set; }

    /// <summary>As vagas das requisições que entraram na escala.</summary>
    public int VagasPedidas => _engajamentos.Count + _vagasNaoPreenchidas.Sum(vaga => vaga.Quantidade);

    public SituacaoNaEscala? SituacaoDe(Matricula matricula) => Situacoes.SingleOrDefault(situacao => situacao.Matricula == matricula);

    /// <summary>
    /// Quem está esperando uma vaga da função, na ordem em que entra se alguém
    /// desistir: os registrados primeiro, cada um na sua vez, e depois os cadastrados.
    /// </summary>
    public IReadOnlyList<Matricula> FilaDeEsperaDe(Funcao funcao) =>
        [.. _espera.Where(esperando => esperando.Funcao == funcao && AindaEspera(esperando)).Select(esperando => esperando.Matricula)];

    /// <summary>
    /// O trabalhador desiste da vaga antes de o período começar. A vaga vai para
    /// o primeiro da fila de espera da função que tenha o descanso em dia, e a
    /// vez do rodízio anda até ele; se não há ninguém, a vaga fica vazia, para o
    /// operador saber.
    /// </summary>
    /// <param name="jornadas">
    /// Os períodos em que os que esperam já estão escalados, em outras escalas.
    /// Quem pegou uma jornada depois desta escala, e ficaria sem o descanso com
    /// que ela foi feita, é pulado: continua na fila, mas não entra nesta vaga.
    /// </param>
    /// <returns>A situação do substituto, ou nula, se a vaga ficou vazia.</returns>
    public SituacaoNaEscala? Desistir(TrabalhadorId trabalhador, DateTimeOffset em, IReadOnlyCollection<Jornada> jornadas)
    {
        ArgumentNullException.ThrowIfNull(jornadas);

        var situacao = NaEscala(trabalhador);

        if (em >= Periodo.Inicio)
        {
            throw new ExcecaoDeDominio($"O período de {Periodo}, já começou: quem não vai trabalhar fica com falta, e não desiste da vaga.");
        }

        if (situacao.Comparecimento == Comparecimento.Presente)
        {
            throw new ExcecaoDeDominio($"{Quem(situacao)} já está com a presença confirmada: não dá mais para desistir.");
        }

        var vaga = situacao.Engajamento!;
        var posicao = _engajamentos.IndexOf(vaga);
        situacao.Desistir(em);

        var jornadasDe = jornadas.ToLookup(jornada => jornada.Trabalhador, jornada => jornada.Periodo);
        var proximo = _espera.FirstOrDefault(esperando =>
            esperando.Funcao == vaga.Funcao && AindaEspera(esperando) && Descanso.ConflitoCom(Periodo, jornadasDe[esperando.Trabalhador]) is null);

        if (proximo is null)
        {
            _engajamentos.RemoveAt(posicao);
            DeixarVazia(vaga);

            return null;
        }

        var substituto = SituacaoDo(proximo.Trabalhador);
        substituto.Substituir(vaga);
        _engajamentos[posicao] = substituto.Engajamento!;
        RodizioDepois = RodizioDepois.ComAVezEm(proximo.Funcao, proximo.Inscricao, proximo.Matricula);

        return substituto;
    }

    /// <summary>Confirma que o trabalhador está no local de trabalho: de uma hora antes do período até o fim dele.</summary>
    public void ConfirmarPresenca(TrabalhadorId trabalhador, DateTimeOffset em)
    {
        var situacao = NaEscala(trabalhador);

        if (em < Periodo.Inicio - AntecedenciaDaPresenca)
        {
            throw new ExcecaoDeDominio(
                $"A presença na {Descricao} só pode ser confirmada a partir de {FormatoBrasileiro.Instante(Periodo.Inicio - AntecedenciaDaPresenca)}.");
        }

        if (em >= Periodo.Fim)
        {
            throw new ExcecaoDeDominio($"O período de {Periodo}, já terminou: a presença não pode mais ser confirmada.");
        }

        ExigirAguardando(situacao);
        situacao.ConfirmarPresenca(em);
    }

    /// <summary>Registra a falta de quem está na escala e não apareceu, depois de o período começar.</summary>
    public void RegistrarFalta(TrabalhadorId trabalhador, DateTimeOffset em)
    {
        var situacao = NaEscala(trabalhador);

        if (em < Periodo.Inicio)
        {
            throw new ExcecaoDeDominio($"O período de {Periodo}, ainda não começou: não há falta antes dele.");
        }

        ExigirAguardando(situacao);
        situacao.Faltar();
    }

    /// <summary>Justifica a falta — com um atestado, por exemplo. Vale também depois de a escala ser encerrada.</summary>
    public void JustificarFalta(TrabalhadorId trabalhador, string justificativa)
    {
        var situacao = SituacaoDo(trabalhador);

        if (situacao.Comparecimento != Comparecimento.Falta)
        {
            throw new ExcecaoDeDominio($"{Quem(situacao)} não tem falta para justificar na {Descricao}.");
        }

        if (Textos.Problema(justificativa, "a justificativa da falta", TamanhoMaximoDaJustificativa) is { } problema)
        {
            throw new ExcecaoDeDominio(problema);
        }

        situacao.JustificarFalta(justificativa.Trim());
    }

    /// <summary>
    /// Encerra a escala depois do fim do período: quem estava nela e não teve a
    /// presença confirmada fica com falta. Dali em diante, só a justificativa de
    /// falta muda alguma coisa.
    /// </summary>
    public void Encerrar(DateTimeOffset em)
    {
        ExigirAberta();

        if (em < Periodo.Fim)
        {
            throw new ExcecaoDeDominio($"O período de {Periodo}, só termina em {FormatoBrasileiro.Instante(Periodo.Fim)}: a escala é encerrada depois.");
        }

        foreach (var situacao in Situacoes.Where(situacao => situacao.Escalado && situacao.Comparecimento == Comparecimento.Aguardando))
        {
            situacao.Faltar();
        }

        EncerradaEm = em;
    }

    /// <summary>"escala da estiva de 05/11/2026, das 13h às 19h: 3 de 3 vagas preenchidas".</summary>
    public override string ToString() =>
        $"{Descricao}: {_engajamentos.Count} de {VagasPedidas} {(VagasPedidas == 1 ? "vaga preenchida" : "vagas preenchidas")}";

    /// <summary>A escala como se grava: cada requisição uma vez, e cada vaga só com o que é dela (ADR-0011).</summary>
    /// <remarks>
    /// As requisições saem das vagas ocupadas e das vazias. A vaga de quem
    /// desistiu está sempre numa das duas — com o substituto, ou vazia —, e a
    /// requisição dela vem junto.
    /// </remarks>
    internal EscalaGravada Gravada =>
        new(
            Id,
            Escalacao,
            Categoria,
            Periodo,
            Descanso,
            FeitaEm,
            [
                .. _engajamentos.Select(engajamento => engajamento.DaRequisicao)
                    .Concat(_vagasNaoPreenchidas.Select(vazias => vazias.DaRequisicao))
                    .DistinctBy(requisicao => requisicao.Id),
            ],
            [.. _engajamentos.Select(engajamento => new VagaOcupada(engajamento.Trabalhador, engajamento.Gravada))],
            [.. _vagasNaoPreenchidas.Select(vazias => vazias.Gravadas)],
            [.. Situacoes.Select(situacao => situacao.Gravada)],
            RequisicoesForaDoPrazo,
            RodizioDepois,
            [.. _espera],
            EncerradaEm);

    /// <summary>
    /// Refaz a escala gravada (ADR-0011). Ela não é feita de novo — o rodízio,
    /// a aptidão e o descanso daquela hora não voltam —, mas o que foi gravado
    /// tem de fechar: cada vaga com quem se habilitou na função, presença e falta
    /// só de quem está na escala, desistência antes do período, a fila de espera
    /// com quem se habilitou. Os problemas vêm todos de uma vez.
    /// </summary>
    /// <remarks>As funções chegam da categoria da escala: é com ela que a gravação as refaz.</remarks>
    internal static Escala Reconstituir(EscalaGravada gravada)
    {
        ArgumentNullException.ThrowIfNull(gravada);

        var problemas = ProblemasDa(gravada);

        if (problemas.Count > 0)
        {
            throw new ExcecaoDeDominio(string.Join(" ", problemas));
        }

        var requisicoes = gravada.Requisicoes.ToDictionary(requisicao => requisicao.Id);
        var situacoes = gravada.Situacoes.ToDictionary(situacao => situacao.Trabalhador);

        Engajamento Vaga(TrabalhadorId trabalhador, VagaGravada vaga) =>
            new(trabalhador, situacoes[trabalhador].Matricula, situacoes[trabalhador].Nome, vaga.Funcao, requisicoes[vaga.Requisicao], gravada.Periodo, vaga.Terno);

        var engajamentos = gravada.Vagas.Select(ocupada => Vaga(ocupada.Trabalhador, ocupada.Vaga)).ToList();
        var engajamentoDe = engajamentos.ToDictionary(engajamento => engajamento.Trabalhador);

        return new Escala(
            gravada.Id,
            gravada.Escalacao,
            gravada.Categoria,
            gravada.Periodo,
            gravada.Descanso,
            gravada.FeitaEm,
            engajamentos,
            [
                .. gravada.VagasNaoPreenchidas.Select(vazias =>
                    new VagaNaoPreenchida(requisicoes[vazias.Vaga.Requisicao], vazias.Vaga.Terno, vazias.Vaga.Funcao, vazias.Quantidade)),
            ],
            [
                .. gravada.Situacoes
                    .OrderBy(situacao => situacao.Matricula)
                    .Select(situacao => new SituacaoNaEscala(
                        situacao,
                        engajamentoDe.GetValueOrDefault(situacao.Trabalhador),
                        situacao.Desistencia is { } desistencia ? Vaga(situacao.Trabalhador, desistencia.Vaga) : null)),
            ],
            gravada.RequisicoesForaDoPrazo,
            gravada.RodizioDepois,
            gravada.Espera)
        {
            EncerradaEm = gravada.EncerradaEm,
        };
    }

    /// <summary>"escala da estiva de 05/11/2026, das 13h às 19h".</summary>
    private string Descricao => $"escala {Categoria.ComDe} de {Periodo}";

    private static string Quem(SituacaoNaEscala situacao) => Quem(situacao.Nome, situacao.Matricula);

    private static string Quem(string nome, Matricula matricula) => $"{nome} (matrícula {matricula})";

    /// <summary>Tudo o que não fecha na escala gravada.</summary>
    private static List<string> ProblemasDa(EscalaGravada gravada)
    {
        var problemas = new List<string>();
        var periodo = gravada.Periodo;
        var descricao = $"escala {gravada.Categoria.ComDe} de {periodo}";

        if (gravada.FeitaEm >= periodo.Inicio)
        {
            problemas.Add($"A {descricao}, foi feita em {FormatoBrasileiro.Instante(gravada.FeitaEm)}, depois de o período começar.");
        }

        if (gravada.EncerradaEm is { } encerrada && encerrada < periodo.Fim)
        {
            problemas.Add($"A {descricao}, foi encerrada em {FormatoBrasileiro.Instante(encerrada)}, antes de o período terminar.");
        }

        foreach (var repetida in gravada.Requisicoes.GroupBy(requisicao => requisicao.Id).Where(grupo => grupo.Count() > 1))
        {
            problemas.Add($"A requisição {repetida.Key} aparece {repetida.Count()} vezes na escala.");
        }

        var requisicoes = gravada.Requisicoes.Select(requisicao => requisicao.Id).ToHashSet();

        IEnumerable<string> ProblemasDaVaga(VagaGravada vaga)
        {
            if (vaga.Terno < 1)
            {
                yield return $"A vaga de {vaga.Funcao.NaFrase} está no terno {vaga.Terno}, e os ternos contam de 1 em diante.";
            }

            if (!requisicoes.Contains(vaga.Requisicao))
            {
                yield return $"A vaga de {vaga.Funcao.NaFrase} no terno {vaga.Terno} é da requisição {vaga.Requisicao}, que a escala não tem.";
            }
        }

        foreach (var repetido in gravada.Situacoes.GroupBy(situacao => situacao.Trabalhador).Where(grupo => grupo.Count() > 1))
        {
            problemas.Add($"{Quem(repetido.First().Nome, repetido.First().Matricula)} aparece {repetido.Count()} vezes entre os habilitados.");
        }

        var situacaoDe = gravada.Situacoes.DistinctBy(situacao => situacao.Trabalhador).ToDictionary(situacao => situacao.Trabalhador);

        foreach (var repetida in situacaoDe.Values.GroupBy(situacao => situacao.Matricula).Where(grupo => grupo.Count() > 1))
        {
            problemas.Add($"A matrícula {repetida.Key} é de {repetida.Count()} habilitados diferentes.");
        }

        var vagasDe = gravada.Vagas.ToLookup(ocupada => ocupada.Trabalhador);

        foreach (var vagas in vagasDe)
        {
            if (!situacaoDe.TryGetValue(vagas.Key, out var situacao))
            {
                problemas.Add($"A vaga de {vagas.First().Vaga.Funcao.NaFrase} no terno {vagas.First().Vaga.Terno} é do trabalhador {vagas.Key}, que não se habilitou.");
            }
            else if (vagas.Count() > 1)
            {
                problemas.Add($"{Quem(situacao.Nome, situacao.Matricula)} tem {vagas.Count()} vagas na escala.");
            }
            else if (!situacao.Funcoes.Contains(vagas.First().Vaga.Funcao))
            {
                problemas.Add($"{Quem(situacao.Nome, situacao.Matricula)} está na vaga de {vagas.First().Vaga.Funcao.NaFrase}, em que não se habilitou.");
            }
        }

        problemas.AddRange(gravada.Vagas.SelectMany(ocupada => ProblemasDaVaga(ocupada.Vaga)));

        foreach (var situacao in situacaoDe.Values)
        {
            var quem = Quem(situacao.Nome, situacao.Matricula);
            var escalado = vagasDe.Contains(situacao.Trabalhador);

            if (situacao.Funcoes.Count == 0)
            {
                problemas.Add($"{quem} se habilitou sem função nenhuma.");
            }

            foreach (var repetida in situacao.Funcoes.GroupBy(funcao => funcao).Where(grupo => grupo.Count() > 1))
            {
                problemas.Add($"{quem} se habilitou {repetida.Count()} vezes como {repetida.Key.NaFrase}.");
            }

            if (!escalado && situacao.Comparecimento != Comparecimento.Aguardando)
            {
                problemas.Add($"{quem} tem presença ou falta sem estar na escala.");
            }

            if ((situacao.Comparecimento == Comparecimento.Presente) != (situacao.PresenteDesde is not null))
            {
                problemas.Add(situacao.PresenteDesde is null
                    ? $"{quem} está com a presença confirmada, sem a hora em que chegou."
                    : $"{quem} tem a hora em que chegou, sem a presença confirmada.");
            }

            if (situacao.PresenteDesde is { } desde && (desde < periodo.Inicio - AntecedenciaDaPresenca || desde >= periodo.Fim))
            {
                problemas.Add(
                    $"{quem} teve a presença confirmada em {FormatoBrasileiro.Instante(desde)}, fora da hora: ela vale de {FormatoBrasileiro.Instante(periodo.Inicio - AntecedenciaDaPresenca)}, ao fim do período.");
            }

            if ((situacao.Comparecimento == Comparecimento.FaltaJustificada) != (situacao.JustificativaDaFalta is not null))
            {
                problemas.Add(situacao.JustificativaDaFalta is null
                    ? $"{quem} tem a falta justificada, sem a justificativa."
                    : $"{quem} tem justificativa de falta, sem a falta justificada.");
            }

            if (situacao.JustificativaDaFalta is { } justificativa
                && Textos.Problema(justificativa, "a justificativa da falta", TamanhoMaximoDaJustificativa) is { } problemaDaJustificativa)
            {
                problemas.Add($"{quem}: {char.ToLowerInvariant(problemaDaJustificativa[0])}{problemaDaJustificativa[1..]}");
            }

            if (situacao.Desistencia is { } desistencia)
            {
                if (desistencia.Em >= periodo.Inicio)
                {
                    problemas.Add($"{quem} desistiu em {FormatoBrasileiro.Instante(desistencia.Em)}, depois de o período começar.");
                }

                if (escalado)
                {
                    problemas.Add($"{quem} desistiu e está de novo na escala: quem desiste não volta.");
                }

                if (!situacao.Funcoes.Contains(desistencia.Vaga.Funcao))
                {
                    problemas.Add($"{quem} desistiu da vaga de {desistencia.Vaga.Funcao.NaFrase}, em que não se habilitou.");
                }

                problemas.AddRange(ProblemasDaVaga(desistencia.Vaga));
            }
            else if (situacao.Substituto && !escalado)
            {
                problemas.Add($"{quem} é substituto, e não pegou vaga nenhuma.");
            }

            if (gravada.EncerradaEm is not null && escalado && situacao.Comparecimento == Comparecimento.Aguardando)
            {
                problemas.Add($"A {descricao}, foi encerrada com {quem} aguardando: quem não teve a presença confirmada fica com falta.");
            }
        }

        foreach (var vazias in gravada.VagasNaoPreenchidas)
        {
            if (vazias.Quantidade < 1)
            {
                problemas.Add(
                    $"As vagas vazias de {vazias.Vaga.Funcao.NaFrase}, no terno {vazias.Vaga.Terno}, são {vazias.Quantidade}, e têm de ser pelo menos 1.");
            }

            problemas.AddRange(ProblemasDaVaga(vazias.Vaga));
        }

        foreach (var esperas in gravada.Espera.GroupBy(esperando => (esperando.Trabalhador, esperando.Funcao)))
        {
            var esperando = esperas.First();

            if (!situacaoDe.TryGetValue(esperando.Trabalhador, out var situacao))
            {
                problemas.Add($"Na espera de {esperando.Funcao.NaFrase}, a matrícula {esperando.Matricula} não se habilitou.");
            }
            else if (esperas.Count() > 1)
            {
                problemas.Add($"{Quem(situacao.Nome, situacao.Matricula)} está {esperas.Count()} vezes na espera de {esperando.Funcao.NaFrase}.");
            }
            else if (situacao.Matricula != esperando.Matricula)
            {
                problemas.Add($"Na espera de {esperando.Funcao.NaFrase}, {Quem(situacao.Nome, situacao.Matricula)} está com a matrícula {esperando.Matricula}.");
            }
            else if (!situacao.Funcoes.Contains(esperando.Funcao))
            {
                problemas.Add($"{Quem(situacao.Nome, situacao.Matricula)} está na espera de {esperando.Funcao.NaFrase}, em que não se habilitou.");
            }
        }

        return problemas;
    }

    /// <summary>Se quem estava na espera ainda espera: não pegou vaga, e não desistiu de uma — quem desiste não volta para a fila.</summary>
    private bool AindaEspera(EsperaPorVaga esperando) => SituacaoDo(esperando.Trabalhador) is { Escalado: false, VagaDesistida: null };

    private SituacaoNaEscala SituacaoDo(TrabalhadorId trabalhador) =>
        Situacoes.SingleOrDefault(situacao => situacao.Trabalhador == trabalhador)
            ?? throw new ExcecaoDeDominio($"O trabalhador não se habilitou para a {Descricao}.");

    /// <summary>A situação de quem tem vaga na escala, com a escala ainda aberta.</summary>
    private SituacaoNaEscala NaEscala(TrabalhadorId trabalhador)
    {
        ExigirAberta();

        var situacao = SituacaoDo(trabalhador);

        return situacao.Escalado ? situacao : throw new ExcecaoDeDominio($"{Quem(situacao)} não tem vaga na {Descricao}.");
    }

    private void ExigirAberta()
    {
        if (EncerradaEm is not null)
        {
            throw new ExcecaoDeDominio($"A {Descricao} já foi encerrada.");
        }
    }

    private static void ExigirAguardando(SituacaoNaEscala situacao)
    {
        switch (situacao.Comparecimento)
        {
            case Comparecimento.Presente:
                throw new ExcecaoDeDominio($"{Quem(situacao)} já está com a presença confirmada.");
            case Comparecimento.Falta or Comparecimento.FaltaJustificada:
                throw new ExcecaoDeDominio($"{Quem(situacao)} já tem falta registrada.");
        }
    }

    /// <summary>A vaga entra nas vazias, somada à da mesma requisição, terno e função, se já havia uma.</summary>
    private void DeixarVazia(Engajamento vaga)
    {
        var indice = _vagasNaoPreenchidas.FindIndex(vazia => vazia.Requisicao == vaga.Requisicao && vazia.Terno == vaga.Terno && vazia.Funcao == vaga.Funcao);

        if (indice < 0)
        {
            _vagasNaoPreenchidas.Add(new VagaNaoPreenchida(vaga, 1));
        }
        else
        {
            _vagasNaoPreenchidas[indice] = new VagaNaoPreenchida(vaga, _vagasNaoPreenchidas[indice].Quantidade + 1);
        }
    }
}
