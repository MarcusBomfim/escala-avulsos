using EscalaAvulsos.Api.Recursos.Requisicoes;
using EscalaAvulsos.Dominio.Periodos;
using EscalaAvulsos.Dominio.Trabalhadores;

namespace EscalaAvulsos.Api.Testes;

/// <summary>Os instantes e os pedidos dos exemplos: novembro de 2026, no horário de Brasília, e o navio Atlântico Sul.</summary>
internal static class Novembro
{
    /// <summary>Um instante de novembro de 2026, no horário de Brasília.</summary>
    public static DateTimeOffset Em(int dia, int hora, int minuto = 0) => new(2026, 11, dia, hora, minuto, 0, TimeSpan.FromHours(-3));

    /// <summary>A requisição da Cais Forte para o Atlântico Sul, num período de novembro: um terno, com as vagas dadas.</summary>
    public static NovaRequisicao Requisicao(int dia, Turno turno, params VagasPedidas[] vagas) =>
        new(
            Categoria.Estiva,
            new NavioDaRequisicao("Atlântico Sul", "IMO 9876543"),
            new LocalDaRequisicao("Terminal Cais Leste", "4"),
            new DateOnly(2026, 11, dia),
            turno,
            [vagas]);
}
