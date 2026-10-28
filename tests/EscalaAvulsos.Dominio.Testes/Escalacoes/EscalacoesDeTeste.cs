using EscalaAvulsos.Dominio.Escalacoes;
using EscalaAvulsos.Dominio.Periodos;
using EscalaAvulsos.Dominio.Trabalhadores;

namespace EscalaAvulsos.Dominio.Testes.Escalacoes;

/// <summary>
/// A escalação dos exemplos: a da estiva para a tarde de quinta, 05/11/2026,
/// com o calendário padrão — requisições até as 9h, habilitação da 1h às 10h.
/// </summary>
internal static class EscalacoesDeTeste
{
    public static readonly Periodo QuintaATarde = Periodo.De(new DateOnly(2026, 11, 5), Turno.Tarde);

    public static readonly DateTimeOffset QuintaAs6h = Em(5, 6);

    public static Escalacao DaEstiva() => new(Categoria.Estiva, QuintaATarde, CalendarioDaEscalacao.Padrao);

    /// <summary>Um instante de novembro de 2026, no horário de Brasília.</summary>
    public static DateTimeOffset Em(int dia, int hora, int minuto = 0) => new(2026, 11, dia, hora, minuto, 0, TimeSpan.FromHours(-3));
}
