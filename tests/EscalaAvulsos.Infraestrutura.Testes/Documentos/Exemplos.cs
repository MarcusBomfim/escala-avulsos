using System.Text.Json.Nodes;
using EscalaAvulsos.Dominio.Escalacoes;
using EscalaAvulsos.Dominio.Identificacao;
using EscalaAvulsos.Dominio.Requisicoes;
using EscalaAvulsos.Dominio.Testes.Escalacoes;
using EscalaAvulsos.Dominio.Testes.Trabalhadores;
using EscalaAvulsos.Dominio.Trabalhadores;
using EscalaAvulsos.Infraestrutura.Documentos;
using Gravacao = EscalaAvulsos.Infraestrutura.Documentos.Json;
using static EscalaAvulsos.Dominio.Testes.Escalacoes.EscalacoesDeTeste;
using static EscalaAvulsos.Dominio.Testes.Requisicoes.RequisicoesDeTeste;

namespace EscalaAvulsos.Infraestrutura.Testes.Documentos;

/// <summary>Os agregados que os testes gravam e leem, com tudo o que cada um pode ter.</summary>
internal static class Exemplos
{
    public static readonly Navio MarDoNorte = new("Mar do Norte", NumeroImo.De("IMO 9074729"));

    /// <summary>
    /// O Antônio, com tudo o que o trabalhador pode ter: duas qualificações, uma
    /// que vence; o exame; férias, um afastamento encerrado e outro aberto.
    /// </summary>
    public static Trabalhador TrabalhadorCompleto()
    {
        var antonio = TrabalhadoresDeTeste.Estivador();
        antonio.Qualificar(Funcoes.Sinaleiro, new DateOnly(2010, 5, 10), new DateOnly(2027, 5, 9));
        antonio.Impedir(new Impedimento(TipoDeImpedimento.Ferias, new DateOnly(2027, 1, 4), new DateOnly(2027, 2, 2), "Férias de janeiro"));
        antonio.Impedir(new Impedimento(TipoDeImpedimento.Afastamento, new DateOnly(2026, 3, 10)));
        antonio.EncerrarAfastamento(new DateOnly(2026, 4, 1));
        antonio.Impedir(new Impedimento(TipoDeImpedimento.Afastamento, new DateOnly(2026, 12, 1)));

        return antonio;
    }

    /// <summary>O Benedito, cadastrado e com a inscrição cancelada.</summary>
    public static Trabalhador TrabalhadorCancelado()
    {
        var benedito = TrabalhadoresDeTeste.Estivador(4130, "Benedito Lima", Inscricao.Cadastrado);
        benedito.CancelarInscricao(new DateOnly(2026, 12, 1), "Aposentadoria");

        return benedito;
    }

    public static Requisicao RequisicaoCancelada()
    {
        var requisicao = DoisTernos();
        requisicao.Cancelar("O navio atrasou", QuartaAs10h.AddHours(2));

        return requisicao;
    }

    /// <summary>
    /// A tarde de quinta, com mais: o Fábio, habilitado como estivador e
    /// operador de guindaste e suspenso depois de se habilitar; uma requisição de
    /// contramestre, que ninguém pegou; e uma que chegou depois do prazo. O
    /// Edson desiste e o Benedito entra; o Antônio e o Benedito chegam, o
    /// Carlos falta, e a falta é justificada depois de a escala ser encerrada.
    /// </summary>
    /// <remarks>Todos os motivos de quem fica de fora aparecem nela.</remarks>
    public static (Escala Escala, TardeDeQuinta Tarde) EscalaCompleta()
    {
        var tarde = new TardeDeQuinta();
        var fabio = TrabalhadoresDeTeste.Estivador(4199, "Fábio Rocha");
        fabio.Qualificar(Funcoes.OperadorDeGuindaste, new DateOnly(2015, 1, 1));
        tarde.Escalacao.Habilitar(fabio, [Funcoes.Estivador, Funcoes.OperadorDeGuindaste], Em(5, 8));
        fabio.Impedir(new Impedimento(TipoDeImpedimento.Suspensao, new DateOnly(2026, 11, 5), new DateOnly(2026, 11, 6)));

        var contramestre = new Requisicao(
            new DadosDaRequisicao(CaisForte, MarDoNorte, new LocalDaOperacao("Terminal Cais Oeste"), QuintaATarde),
            [[new(Funcoes.ContramestreDeTerno, 1)]],
            QuartaAs10h);
        var atrasada = new Requisicao(TardeDeQuinta.DaTarde, [[new(Funcoes.Estivador, 1)]], Em(5, 9, 30));

        var escala = Escalador.Escalar(
            tarde.Escalacao,
            [tarde.Requisicao, contramestre, atrasada],
            [.. tarde.Trabalhadores, fabio],
            tarde.Jornadas,
            tarde.Rodizio,
            Descanso.DaLei,
            Em(5, 10));

        escala.Desistir(tarde.Edson.Id, Em(5, 11), []);
        escala.ConfirmarPresenca(tarde.Antonio.Id, Em(5, 12));
        escala.ConfirmarPresenca(tarde.Benedito.Id, Em(5, 12, 30));
        escala.Encerrar(Em(5, 19));
        escala.JustificarFalta(tarde.Carlos.Id, "Atestado médico");

        return (escala, tarde);
    }

    public static string Json(Trabalhador trabalhador) =>
        Gravacao.Escrever(DocumentoDoTrabalhador.De(trabalhador), ContextoJson.Default.DocumentoDoTrabalhador);

    public static string Json(Requisicao requisicao) =>
        Gravacao.Escrever(DocumentoDaRequisicao.De(requisicao), ContextoJson.Default.DocumentoDaRequisicao);

    public static string Json(Escalacao escalacao) =>
        Gravacao.Escrever(DocumentoDaEscalacao.De(escalacao), ContextoJson.Default.DocumentoDaEscalacao);

    public static string Json(Escala escala) =>
        Gravacao.Escrever(DocumentoDaEscala.De(escala), ContextoJson.Default.DocumentoDaEscala);

    public static Trabalhador LerTrabalhador(string json, TrabalhadorId? id = null) =>
        Gravacao.Ler(json, ContextoJson.Default.DocumentoDoTrabalhador).ParaTrabalhador(id ?? TrabalhadorId.Novo());

    public static Requisicao LerRequisicao(string json, RequisicaoId? id = null) =>
        Gravacao.Ler(json, ContextoJson.Default.DocumentoDaRequisicao).ParaRequisicao(id ?? RequisicaoId.Novo());

    public static Escalacao LerEscalacao(string json, EscalacaoId? id = null) =>
        Gravacao.Ler(json, ContextoJson.Default.DocumentoDaEscalacao).ParaEscalacao(id ?? EscalacaoId.Novo());

    public static Escala LerEscala(string json, EscalaId? id = null) =>
        Gravacao.Ler(json, ContextoJson.Default.DocumentoDaEscala).ParaEscala(id ?? EscalaId.Novo());

    /// <summary>O documento com uma mudança feita à mão — como um <c>update</c> direto no banco.</summary>
    public static string Adulterado(string json, Action<JsonObject> mudanca)
    {
        var documento = JsonNode.Parse(json)!.AsObject();
        mudanca(documento);

        return documento.ToJsonString();
    }
}
