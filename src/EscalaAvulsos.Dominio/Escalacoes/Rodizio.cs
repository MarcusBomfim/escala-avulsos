using EscalaAvulsos.Dominio.Comum;
using EscalaAvulsos.Dominio.Identificacao;
using EscalaAvulsos.Dominio.Trabalhadores;

namespace EscalaAvulsos.Dominio.Escalacoes;

/// <summary>Onde o rodízio de uma função parou: a matrícula do último escalado, entre os registrados ou entre os cadastrados.</summary>
public sealed record VezNoRodizio(Funcao Funcao, Inscricao Inscricao, Matricula UltimoEscalado);

/// <summary>
/// O rodízio de uma categoria: em cada função, e separado para registrados e
/// cadastrados, a matrícula do último escalado (ADR-0006).
/// </summary>
/// <remarks>
/// <para>
/// A escala seguinte começa na matrícula logo depois da do último escalado e
/// anda para cima; passando da maior, volta à menor. É a ordem sequencial e
/// numérica: cada um tem a sua vez, e a vez de todos chega.
/// </para>
/// <para>
/// O rodízio não muda: andar a vez devolve outro, e o de antes continua o
/// mesmo. Com as mesmas entradas, a mesma escala sai de novo.
/// </para>
/// </remarks>
public sealed class Rodizio
{
    private readonly Dictionary<(Funcao Funcao, Inscricao Inscricao), Matricula> _ultimos;

    private Rodizio(Dictionary<(Funcao Funcao, Inscricao Inscricao), Matricula> ultimos) => _ultimos = ultimos;

    /// <summary>O rodízio que ainda não girou: em cada função, a primeira escala começa pela menor matrícula.</summary>
    public static Rodizio Novo { get; } = new(new Dictionary<(Funcao Funcao, Inscricao Inscricao), Matricula>());

    /// <summary>Onde o rodízio parou em cada função, para gravar e remontar.</summary>
    public IReadOnlyList<VezNoRodizio> Vezes =>
    [
        .. _ultimos
            .Select(par => new VezNoRodizio(par.Key.Funcao, par.Key.Inscricao, par.Value))
            .OrderBy(vez => vez.Funcao.Categoria)
            .ThenBy(vez => vez.Funcao.Nome, StringComparer.OrdinalIgnoreCase)
            .ThenBy(vez => vez.Inscricao),
    ];

    /// <summary>Remonta o rodízio com as vezes gravadas.</summary>
    public static Rodizio De(IEnumerable<VezNoRodizio> vezes)
    {
        ArgumentNullException.ThrowIfNull(vezes);

        var ultimos = new Dictionary<(Funcao Funcao, Inscricao Inscricao), Matricula>();

        foreach (var vez in vezes)
        {
            if (!ultimos.TryAdd((vez.Funcao, vez.Inscricao), vez.UltimoEscalado))
            {
                throw new ExcecaoDeDominio(
                    $"O rodízio de {vez.Funcao.NaFrase} tem duas vezes para os {(vez.Inscricao == Inscricao.Registrado ? "registrados" : "cadastrados")}.");
            }
        }

        return new Rodizio(ultimos);
    }

    /// <summary>A matrícula do último escalado na função; nula, se o rodízio dela ainda não girou.</summary>
    public Matricula? UltimoEscalado(Funcao funcao, Inscricao inscricao) => _ultimos.GetValueOrDefault((funcao, inscricao));

    /// <summary>
    /// As matrículas na ordem da vez: primeiro as maiores que a do último
    /// escalado, da menor para a maior; depois, dando a volta, as outras.
    /// </summary>
    public IReadOnlyList<Matricula> NaOrdemDaVez(Funcao funcao, Inscricao inscricao, IEnumerable<Matricula> matriculas)
    {
        ArgumentNullException.ThrowIfNull(funcao);
        ArgumentNullException.ThrowIfNull(matriculas);

        var emOrdem = matriculas.Distinct().Order().ToList();

        return UltimoEscalado(funcao, inscricao) is { } ultimo
            ? [.. emOrdem.Where(matricula => matricula > ultimo), .. emOrdem.Where(matricula => matricula <= ultimo)]
            : emOrdem;
    }

    /// <summary>O rodízio com a vez da função andada até o último escalado.</summary>
    public Rodizio ComAVezEm(Funcao funcao, Inscricao inscricao, Matricula ultimoEscalado)
    {
        ArgumentNullException.ThrowIfNull(funcao);
        ArgumentNullException.ThrowIfNull(ultimoEscalado);

        if (!Enum.IsDefined(inscricao))
        {
            throw new ExcecaoDeDominio($"A inscrição {(int)inscricao} não existe: o trabalhador é cadastrado ou registrado.");
        }

        return new Rodizio(new Dictionary<(Funcao Funcao, Inscricao Inscricao), Matricula>(_ultimos) { [(funcao, inscricao)] = ultimoEscalado });
    }
}
