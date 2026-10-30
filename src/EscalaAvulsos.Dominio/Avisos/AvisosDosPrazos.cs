using System.Globalization;
using EscalaAvulsos.Dominio.Comum;
using EscalaAvulsos.Dominio.Escalacoes;
using EscalaAvulsos.Dominio.Trabalhadores;

namespace EscalaAvulsos.Dominio.Avisos;

/// <summary>
/// Os avisos do que vai acontecer se o trabalhador não fizer nada: a
/// habilitação que vai fechar sem ele, o exame e a reciclagem que vão vencer.
/// Ninguém grava nada para eles nascerem — o tempo passa —, então quem os
/// confere de tempos em tempos é o servidor (ADR-0019).
/// </summary>
public static class AvisosDosPrazos
{
    /// <summary>Quanto antes de fechar a habilitação o trabalhador é avisado, se a configuração não disser outro.</summary>
    public static readonly TimeSpan AntecedenciaDoFechamento = TimeSpan.FromHours(1);

    /// <summary>Quantos dias antes de vencer o exame e a reciclagem são avisados — os mesmos que a tela "Meus dados" avisa.</summary>
    public const int DiasAntesDoVencimento = 30;

    /// <summary>
    /// A habilitação que fecha logo: um aviso para cada trabalhador da
    /// categoria que ainda não se habilitou e pode trabalhar no período em
    /// alguma das funções dele. Quem não pode em nenhuma não é chamado para
    /// uma habilitação que o recusaria.
    /// </summary>
    /// <param name="antecedencia">Quanto antes do fechamento avisar: a partir daí, até a habilitação fechar.</param>
    public static IReadOnlyList<Aviso> DaHabilitacaoQueVaiFechar(
        Escalacao escalacao,
        IEnumerable<Trabalhador> daCategoria,
        DateTimeOffset agora,
        TimeSpan antecedencia)
    {
        ArgumentNullException.ThrowIfNull(escalacao);
        ArgumentNullException.ThrowIfNull(daCategoria);

        if (!escalacao.HabilitacaoAbertaEm(agora) || escalacao.FechamentoDaHabilitacao - agora > antecedencia)
        {
            return [];
        }

        var periodo = AvisosDaEscala.PorExtenso(escalacao.Periodo);
        var texto =
            $"A habilitação para {char.ToLowerInvariant(periodo[0])}{periodo[1..]} " +
            $"fecha {FormatoBrasileiro.AHora(escalacao.FechamentoDaHabilitacao)}, e você ainda não se habilitou.";

        return [.. daCategoria
            .Where(trabalhador => trabalhador.Categoria == escalacao.Categoria && escalacao.HabilitacaoDe(trabalhador.Id) is null)
            .Where(trabalhador => trabalhador.Qualificacoes.Any(qualificacao => trabalhador.AptidaoPara(qualificacao.Funcao, escalacao.Periodo).Apto))
            .Select(trabalhador => new Aviso(
                trabalhador.Id,
                TipoDoAviso.HabilitacaoVaiFechar,
                $"habilitacao:{escalacao.Id}",
                "A habilitação vai fechar",
                texto,
                agora))];
    }

    /// <summary>
    /// O exame médico e as reciclagens do trabalhador que vencem nos próximos
    /// dias — um aviso por validade: o exame renovado volta a ser avisado
    /// quando a validade nova estiver para vencer.
    /// </summary>
    public static IReadOnlyList<Aviso> DosVencimentos(Trabalhador trabalhador, DateTimeOffset agora, int dias = DiasAntesDoVencimento)
    {
        ArgumentNullException.ThrowIfNull(trabalhador);

        if (trabalhador.Cancelamento is not null)
        {
            return [];
        }

        var hoje = DateOnly.FromDateTime(HorarioDoPorto.Local(agora));
        var limite = hoje.AddDays(dias);
        bool Vence(DateOnly validade) => hoje <= validade && validade <= limite;

        var avisos = new List<Aviso>();

        if (trabalhador.Aso is { } aso && Vence(aso.ValidoAte))
        {
            avisos.Add(new Aviso(
                trabalhador.Id,
                TipoDoAviso.ExameVaiVencer,
                $"aso:{Dia(aso.ValidoAte)}",
                "O exame médico vai vencer",
                $"O seu exame médico (ASO) vale até {FormatoBrasileiro.Data(aso.ValidoAte)}. Sem ele em dia, você não entra na escala.",
                agora));
        }

        foreach (var qualificacao in trabalhador.Qualificacoes)
        {
            if (qualificacao.ValidaAte is { } validade && Vence(validade))
            {
                avisos.Add(new Aviso(
                    trabalhador.Id,
                    TipoDoAviso.ReciclagemVaiVencer,
                    $"reciclagem:{qualificacao.Funcao.Nome.ToLowerInvariant()}:{Dia(validade)}",
                    "A reciclagem vai vencer",
                    $"A sua reciclagem de {qualificacao.Funcao.NaFrase} vale até {FormatoBrasileiro.Data(validade)}. " +
                    "Depois disso, você não entra na escala nessa função.",
                    agora));
            }
        }

        return avisos;
    }

    private static string Dia(DateOnly dia) => dia.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
