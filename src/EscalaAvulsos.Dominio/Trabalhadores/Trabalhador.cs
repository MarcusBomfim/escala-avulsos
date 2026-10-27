using EscalaAvulsos.Dominio.Comum;
using EscalaAvulsos.Dominio.Identificacao;
using EscalaAvulsos.Dominio.Periodos;

namespace EscalaAvulsos.Dominio.Trabalhadores;

/// <summary>O que se informa de um trabalhador ao inscrevê-lo no órgão gestor.</summary>
/// <param name="Matricula">O número dele no órgão gestor.</param>
/// <param name="Nome">O nome completo.</param>
/// <param name="Cpf">O CPF.</param>
/// <param name="Categoria">A atividade em que trabalha, e em cujo rodízio entra.</param>
/// <param name="Inscricao">Se está no cadastro ou no registro.</param>
public sealed record DadosDoTrabalhador(Matricula Matricula, string Nome, Cpf Cpf, Categoria Categoria, Inscricao Inscricao);

/// <summary>
/// O trabalhador portuário avulso, como o órgão gestor o conhece: a matrícula,
/// a categoria, as funções em que é qualificado, o exame médico e os tempos
/// em que não pode trabalhar.
/// </summary>
/// <remarks>
/// <para>
/// A pergunta que importa para a escala é se ele pode trabalhar numa função,
/// num período — e, quando não pode, por quê. <see cref="AptidaoPara"/>
/// responde com todos os motivos de uma vez, e não com um sim ou um não
/// (ADR-0003).
/// </para>
/// <para>
/// A inscrição cancelada congela o trabalhador: ele continua existindo, para
/// o histórico das escalas e dos pagamentos, mas nada mais muda nele.
/// </para>
/// </remarks>
public sealed class Trabalhador
{
    public const int TamanhoMaximoDoNome = 120;

    public const int TamanhoMaximoDoMotivo = 200;

    private readonly List<Qualificacao> _qualificacoes = [];

    private readonly List<Impedimento> _impedimentos = [];

    public Trabalhador(DadosDoTrabalhador dados)
    {
        ArgumentNullException.ThrowIfNull(dados);
        ArgumentNullException.ThrowIfNull(dados.Matricula);
        ArgumentNullException.ThrowIfNull(dados.Cpf);

        var problemas = new List<string>();

        if (Textos.Problema(dados.Nome, "o nome do trabalhador", TamanhoMaximoDoNome) is { } nome)
        {
            problemas.Add(nome);
        }

        if (!Enum.IsDefined(dados.Categoria))
        {
            problemas.Add($"A categoria {(int)dados.Categoria} não existe.");
        }

        if (!Enum.IsDefined(dados.Inscricao))
        {
            problemas.Add($"A inscrição {(int)dados.Inscricao} não existe: o trabalhador é cadastrado ou registrado.");
        }

        if (problemas.Count > 0)
        {
            throw new ExcecaoDeDominio(string.Join(" ", problemas));
        }

        Id = TrabalhadorId.Novo();
        Matricula = dados.Matricula;
        Nome = dados.Nome.Trim();
        Cpf = dados.Cpf;
        Categoria = dados.Categoria;
        Inscricao = dados.Inscricao;
    }

    public TrabalhadorId Id { get; }

    public Matricula Matricula { get; }

    public string Nome { get; }

    public Cpf Cpf { get; }

    public Categoria Categoria { get; }

    public Inscricao Inscricao { get; private set; }

    /// <summary>O exame médico mais recente; nulo, enquanto não houver um.</summary>
    public Aso? Aso { get; private set; }

    /// <summary>O cancelamento da inscrição; nulo, enquanto ela vale.</summary>
    public CancelamentoDaInscricao? Cancelamento { get; private set; }

    /// <summary>As funções que ele pode exercer, uma qualificação por função.</summary>
    public IReadOnlyList<Qualificacao> Qualificacoes => _qualificacoes.AsReadOnly();

    /// <summary>As férias, os afastamentos e as suspensões, na ordem em que foram registrados.</summary>
    public IReadOnlyList<Impedimento> Impedimentos => _impedimentos.AsReadOnly();

    /// <summary>
    /// Qualifica o trabalhador numa função da categoria dele — ou renova a
    /// qualificação que ele já tinha, com as datas do curso novo.
    /// </summary>
    /// <param name="validaAte">O último dia em que vale; nulo, se a função não pede reciclagem.</param>
    public void Qualificar(Funcao funcao, DateOnly desde, DateOnly? validaAte = null)
    {
        ArgumentNullException.ThrowIfNull(funcao);
        ExigirInscricaoValida();

        if (funcao.Categoria != Categoria)
        {
            throw new ExcecaoDeDominio(
                $"{funcao.Nome} é função {funcao.Categoria.ComDe}, e {Nome} é {Categoria.ComDe}: a qualificação é numa função da categoria do trabalhador.");
        }

        if (validaAte is { } ultimo && ultimo < desde)
        {
            throw new ExcecaoDeDominio(
                $"A qualificação de {funcao.NaFrase} não pode valer só até {FormatoBrasileiro.Data(ultimo)}, antes de começar, em {FormatoBrasileiro.Data(desde)}.");
        }

        _qualificacoes.RemoveAll(qualificacao => qualificacao.Funcao == funcao);
        _qualificacoes.Add(new Qualificacao(funcao, desde, validaAte));
    }

    /// <summary>
    /// Registra o exame médico mais recente. Um exame mais antigo que o
    /// registrado é recusado: a validade só anda para a frente com exame novo.
    /// </summary>
    public void RegistrarAso(Aso aso)
    {
        ArgumentNullException.ThrowIfNull(aso);
        ExigirInscricaoValida();

        if (Aso is not null && aso.EmitidoEm < Aso.EmitidoEm)
        {
            throw new ExcecaoDeDominio(
                $"O exame médico de {FormatoBrasileiro.Data(aso.EmitidoEm)} é mais antigo que o registrado, de {FormatoBrasileiro.Data(Aso.EmitidoEm)}.");
        }

        Aso = aso;
    }

    /// <summary>
    /// Registra um tempo em que o trabalhador não pode ser escalado. Dois
    /// impedimentos do mesmo tipo não cruzam: duas férias no mesmo dia são
    /// erro de digitação, e não dois descansos.
    /// </summary>
    public void Impedir(Impedimento impedimento)
    {
        ArgumentNullException.ThrowIfNull(impedimento);
        ExigirInscricaoValida();

        if (_impedimentos.FirstOrDefault(existente => existente.Tipo == impedimento.Tipo && existente.Cruza(impedimento)) is { } cruzado)
        {
            throw new ExcecaoDeDominio($"Não dá para registrar {impedimento.ComArtigo}: as datas cruzam com {cruzado.ComArtigo}.");
        }

        _impedimentos.Add(impedimento);
    }

    /// <summary>Encerra o afastamento sem data para acabar: o trabalhador volta a poder ser escalado no dia informado.</summary>
    public void EncerrarAfastamento(DateOnly volta)
    {
        ExigirInscricaoValida();

        var aberto = _impedimentos.SingleOrDefault(impedimento => impedimento.Tipo == TipoDeImpedimento.Afastamento && impedimento.Ate is null)
            ?? throw new ExcecaoDeDominio($"{Nome} não tem afastamento sem data para acabar.");

        if (volta <= aberto.De)
        {
            throw new ExcecaoDeDominio(
                $"A volta, em {FormatoBrasileiro.Data(volta)}, tem de ser depois do primeiro dia do afastamento, {FormatoBrasileiro.Data(aberto.De)}.");
        }

        _impedimentos[_impedimentos.IndexOf(aberto)] = aberto.EncerradoEm(volta.AddDays(-1));
    }

    /// <summary>Passa o trabalhador do cadastro para o registro, quando abre a vaga dele.</summary>
    public void Registrar()
    {
        ExigirInscricaoValida();

        if (Inscricao == Inscricao.Registrado)
        {
            throw new ExcecaoDeDominio($"{this} já está no registro.");
        }

        Inscricao = Inscricao.Registrado;
    }

    /// <summary>Cancela a inscrição a partir de um dia: dali em diante, o trabalhador não é mais escalado.</summary>
    public void CancelarInscricao(DateOnly aPartirDe, string motivo)
    {
        ExigirInscricaoValida();

        if (Textos.Problema(motivo, "o motivo do cancelamento", TamanhoMaximoDoMotivo) is { } problema)
        {
            throw new ExcecaoDeDominio(problema);
        }

        Cancelamento = new CancelamentoDaInscricao(aPartirDe, motivo.Trim());
    }

    /// <summary>Se o trabalhador pode trabalhar na função, no período — e, se não pode, por quê.</summary>
    /// <remarks>
    /// O que se conta em dias — o exame, as qualificações, os impedimentos, o
    /// cancelamento — é conferido no dia em que o período começa (ADR-0002).
    /// </remarks>
    public Aptidao AptidaoPara(Funcao funcao, Periodo periodo)
    {
        ArgumentNullException.ThrowIfNull(funcao);
        ArgumentNullException.ThrowIfNull(periodo);

        return new Aptidao(this, funcao, periodo, [.. MotivosContra(funcao, periodo.Dia)]);
    }

    /// <summary>"Antônio Ferreira (matrícula 4127)".</summary>
    public override string ToString() => $"{Nome} (matrícula {Matricula})";

    private IEnumerable<Motivo> MotivosContra(Funcao funcao, DateOnly dia)
    {
        // Com a inscrição cancelada, o resto não interessa: nenhum exame
        // novo, nenhuma volta de férias faz o trabalhador voltar à escala.
        if (Cancelamento is { } cancelamento && cancelamento.APartirDe <= dia)
        {
            yield return Motivo.InscricaoCancelada(cancelamento);
            yield break;
        }

        if (funcao.Categoria != Categoria)
        {
            yield return Motivo.FuncaoDeOutraCategoria(funcao, Categoria);
        }
        else if (_qualificacoes.SingleOrDefault(qualificacao => qualificacao.Funcao == funcao) is not { } qualificacao)
        {
            yield return Motivo.SemQualificacao(funcao);
        }
        else if (dia < qualificacao.Desde)
        {
            yield return Motivo.QualificacaoAindaNaoVale(qualificacao);
        }
        else if (qualificacao.ValidaAte is { } validaAte && validaAte < dia)
        {
            yield return Motivo.QualificacaoVencida(qualificacao, validaAte);
        }

        if (Aso is null)
        {
            yield return Motivo.SemAso();
        }
        else if (Aso.ValidoAte < dia)
        {
            yield return Motivo.AsoVencido(Aso);
        }

        foreach (var impedimento in _impedimentos.Where(impedimento => impedimento.VigenteEm(dia)).OrderBy(impedimento => impedimento.De))
        {
            yield return Motivo.Impedido(impedimento);
        }
    }

    private void ExigirInscricaoValida()
    {
        if (Cancelamento is { } cancelamento)
        {
            throw new ExcecaoDeDominio(
                $"A inscrição de {this} foi cancelada a partir de {FormatoBrasileiro.Data(cancelamento.APartirDe)}: nada mais muda nela.");
        }
    }
}
