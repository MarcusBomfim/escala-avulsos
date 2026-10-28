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
/// <remarks>Só nasce pelo <see cref="Escalador"/>.</remarks>
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
        DateTimeOffset feitaEm,
        IReadOnlyList<Engajamento> engajamentos,
        IReadOnlyList<VagaNaoPreenchida> vagasNaoPreenchidas,
        IReadOnlyList<SituacaoNaEscala> situacoes,
        IReadOnlyList<RequisicaoId> requisicoesForaDoPrazo,
        Rodizio rodizioDepois,
        IReadOnlyList<EsperaPorVaga> espera)
    {
        Id = EscalaId.Novo();
        Escalacao = escalacao.Id;
        Categoria = escalacao.Categoria;
        Periodo = escalacao.Periodo;
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
    /// o primeiro da fila de espera da função, e a vez do rodízio anda até ele;
    /// se não há ninguém na fila, a vaga fica vazia, para o operador saber.
    /// </summary>
    /// <returns>A situação do substituto, ou nula, se a vaga ficou vazia.</returns>
    public SituacaoNaEscala? Desistir(TrabalhadorId trabalhador, DateTimeOffset em)
    {
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

        var proximo = _espera.FirstOrDefault(esperando => esperando.Funcao == vaga.Funcao && AindaEspera(esperando));

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

    /// <summary>"escala da estiva de 05/11/2026, das 13h às 19h".</summary>
    private string Descricao => $"escala {Categoria.ComDe} de {Periodo}";

    private static string Quem(SituacaoNaEscala situacao) => $"{situacao.Nome} (matrícula {situacao.Matricula})";

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
