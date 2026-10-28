using System.Globalization;
using EscalaAvulsos.Api.Http;
using EscalaAvulsos.Dominio.Comum;
using EscalaAvulsos.Dominio.Identificacao;
using EscalaAvulsos.Dominio.Periodos;
using EscalaAvulsos.Dominio.Trabalhadores;
using EscalaAvulsos.Infraestrutura.Persistencia;
using EscalaAvulsos.Infraestrutura.Persistencia.Repositorios;

namespace EscalaAvulsos.Api.Recursos;

/// <summary>O que os endpoints fazem com um agregado lido antes de alterá-lo.</summary>
internal static class Lidos
{
    /// <summary>
    /// A alteração parte da versão que o cliente leu. Se o banco já está em
    /// outra, ela nem é aplicada — 412, com a versão atual. O banco confere de
    /// novo ao gravar, para quem gravar no meio do caminho (ADR-0012).
    /// </summary>
    /// <param name="agregado">Como o agregado aparece na mensagem: "O trabalhador da matrícula 4127".</param>
    public static void ConferirVersao<T>(Versionado<T> lido, VersaoLida versao, string agregado)
    {
        ArgumentNullException.ThrowIfNull(lido);
        ArgumentNullException.ThrowIfNull(versao);

        if (lido.Versao != versao.Valor)
        {
            throw new ConflitoDeConcorrencia(
                $"{agregado} mudou desde a versão {versao.Valor}: está na versão {lido.Versao}. Leia de novo e refaça a alteração.",
                lido.Versao);
        }
    }
}

/// <summary>Ler pelo que veio no pedido — ou 404, com a mensagem de sempre.</summary>
internal static class Leituras
{
    public static async Task<Versionado<Trabalhador>> Trabalhador(RepositorioDeTrabalhadores repositorio, Matricula matricula, CancellationToken cancelamento) =>
        await repositorio.ObterPelaMatricula(matricula, cancelamento)
        ?? throw new AgregadoNaoEncontrado($"A matrícula {matricula} não está no cadastro.");
}

/// <summary>
/// O que chega como texto no caminho ou na consulta. O que não está no
/// formato é 400, com o formato; o que está, mas o domínio recusa, é 422,
/// com a explicação dele.
/// </summary>
internal static class TextosDoPedido
{
    /// <summary>Uma opção pelo nome — <c>Estiva</c>, <c>Tarde</c> —, como no corpo: número não é opção, e nome errado é 400, com as que existem.</summary>
    /// <param name="parametro">O nome do parâmetro, para a mensagem.</param>
    public static TOpcao ComoOpcao<TOpcao>(this string texto, string parametro)
        where TOpcao : struct, Enum
    {
        ArgumentNullException.ThrowIfNull(texto);

        return Enum.GetNames<TOpcao>().Contains(texto, StringComparer.Ordinal)
            ? Enum.Parse<TOpcao>(texto)
            : throw new PedidoInvalido($"O valor de {parametro} é um destes: {string.Join(", ", Enum.GetNames<TOpcao>())}; veio \"{texto}\".");
    }

    /// <summary>Um dia, como <c>2026-11-05</c> — e só assim: <c>11/05/2026</c> e <c>05/11/2026</c> são ambíguos.</summary>
    public static DateOnly ComoDia(this string texto, string parametro)
    {
        ArgumentNullException.ThrowIfNull(texto);

        return DateOnly.TryParseExact(texto, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dia)
            ? dia
            : throw new PedidoInvalido($"O valor de {parametro} é um dia, como 2026-11-05; veio \"{texto}\".");
    }

    /// <summary>Um mês, como <c>2026-11</c>.</summary>
    public static Mes ComoMes(this string texto, string parametro)
    {
        ArgumentNullException.ThrowIfNull(texto);

        return DateOnly.TryParseExact(texto, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dia)
            ? Mes.De(dia)
            : throw new PedidoInvalido($"O valor de {parametro} é um mês, como 2026-11; veio \"{texto}\".");
    }

    /// <summary>O período pelo dia e pelo turno do caminho: <c>/2026-11-05/Tarde</c>.</summary>
    public static Periodo ComoPeriodo(string dia, string turno) => Periodo.De(dia.ComoDia("dia"), turno.ComoOpcao<Turno>("turno"));
}

/// <summary>Um período como a API o mostra: o dia, o turno, a frase, o início e o fim.</summary>
public sealed record PeriodoResposta(DateOnly Dia, Turno Turno, string Descricao, DateTimeOffset Inicio, DateTimeOffset Fim)
{
    public static PeriodoResposta De(Periodo periodo)
    {
        ArgumentNullException.ThrowIfNull(periodo);

        return new(periodo.Dia, periodo.Turno, periodo.ToString(), periodo.Inicio, periodo.Fim);
    }
}
