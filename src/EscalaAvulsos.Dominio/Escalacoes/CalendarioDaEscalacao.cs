using EscalaAvulsos.Dominio.Comum;

namespace EscalaAvulsos.Dominio.Escalacoes;

/// <summary>
/// Os horários da escalação de cada período, contados para trás a partir do
/// início dele: até quando chegam as requisições, e quando a habilitação abre
/// e fecha (ADR-0005).
/// </summary>
/// <remarks>
/// <para>
/// O padrão — requisições até 4 horas antes do período, habilitação de 12 a 3
/// horas antes — é deste sistema. Os horários de cada porto são combinados
/// entre o órgão gestor, os operadores e os sindicatos, e entram como
/// configuração.
/// </para>
/// <para>
/// A janela é larga de propósito. A vez é pela matrícula, e não pela ordem de
/// chegada: ninguém ganha nada por se habilitar primeiro, e quem tem menos
/// prática com o celular não perde nada por demorar.
/// </para>
/// </remarks>
public sealed record CalendarioDaEscalacao
{
    public CalendarioDaEscalacao(TimeSpan antecedenciaDasRequisicoes, TimeSpan antecedenciaDaAbertura, TimeSpan antecedenciaDoFechamento)
    {
        var problemas = new List<string>();

        if (antecedenciaDoFechamento <= TimeSpan.Zero)
        {
            problemas.Add("A habilitação tem de fechar antes de o período começar.");
        }

        if (antecedenciaDaAbertura <= antecedenciaDoFechamento)
        {
            problemas.Add(
                $"A habilitação tem de abrir antes de fechar, e abriria {FormatoBrasileiro.Duracao(antecedenciaDaAbertura)} antes do período, fechando {FormatoBrasileiro.Duracao(antecedenciaDoFechamento)} antes.");
        }

        if (antecedenciaDasRequisicoes < antecedenciaDoFechamento)
        {
            problemas.Add(
                $"As requisições têm de chegar até a habilitação fechar, porque a escala é feita com elas: chegariam até {FormatoBrasileiro.Duracao(antecedenciaDasRequisicoes)} antes do período, e a habilitação fecha {FormatoBrasileiro.Duracao(antecedenciaDoFechamento)} antes.");
        }

        if (problemas.Count > 0)
        {
            throw new ExcecaoDeDominio(string.Join(" ", problemas));
        }

        AntecedenciaDasRequisicoes = antecedenciaDasRequisicoes;
        AntecedenciaDaAbertura = antecedenciaDaAbertura;
        AntecedenciaDoFechamento = antecedenciaDoFechamento;
    }

    /// <summary>Requisições até 4 horas antes do período; habilitação de 12 a 3 horas antes.</summary>
    public static CalendarioDaEscalacao Padrao { get; } = new(TimeSpan.FromHours(4), TimeSpan.FromHours(12), TimeSpan.FromHours(3));

    /// <summary>Quanto antes do período as requisições têm de chegar para entrar na escala.</summary>
    public TimeSpan AntecedenciaDasRequisicoes { get; }

    /// <summary>Quanto antes do período a habilitação abre.</summary>
    public TimeSpan AntecedenciaDaAbertura { get; }

    /// <summary>Quanto antes do período a habilitação fecha, e a escala é feita.</summary>
    public TimeSpan AntecedenciaDoFechamento { get; }
}
