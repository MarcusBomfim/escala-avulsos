using System.Globalization;
using EscalaAvulsos.Dominio.Comum;
using EscalaAvulsos.Dominio.Escalacoes;
using EscalaAvulsos.Dominio.Extratos;
using EscalaAvulsos.Dominio.Frequencias;
using EscalaAvulsos.Dominio.Trabalhadores;

namespace EscalaAvulsos.Api.Hospedagem;

/// <summary>
/// O que cada porto combina com os sindicatos e os operadores, e o domínio
/// recebe pronto: os horários da escalação, o descanso entre jornadas, a
/// frequência mínima e a tabela de remuneração — da seção <c>Porto</c>.
/// </summary>
/// <remarks>
/// O que não vier na configuração fica com o padrão do domínio, menos a
/// tabela, que não tem padrão: os valores são da convenção de cada porto.
/// Configuração que o domínio recusa impede a API de subir — melhor do que
/// escalar ou pagar pela regra errada.
/// </remarks>
public sealed record ConfiguracaoDoPorto(
    CalendarioDaEscalacao Calendario,
    Descanso Descanso,
    RegraDeFrequencia Frequencia,
    TabelaDeRemuneracao Remuneracao)
{
    public static ConfiguracaoDoPorto Ler(IConfigurationSection porto)
    {
        ArgumentNullException.ThrowIfNull(porto);

        try
        {
            var escalacao = porto.GetSection("Escalacao");
            var calendario = escalacao.Exists()
                ? new CalendarioDaEscalacao(
                    Exigir<TimeSpan>(escalacao, "AntecedenciaDasRequisicoes"),
                    Exigir<TimeSpan>(escalacao, "AntecedenciaDaAbertura"),
                    Exigir<TimeSpan>(escalacao, "AntecedenciaDoFechamento"))
                : CalendarioDaEscalacao.Padrao;

            var descanso = porto.GetValue<TimeSpan?>("DescansoEntreJornadas") is { } minimo ? new Descanso(minimo) : Descanso.DaLei;

            var frequencia = porto.GetSection("Frequencia");
            var regra = frequencia.Exists()
                ? new RegraDeFrequencia(Exigir<int>(frequencia, "MinimoDeHabilitacoes"), Exigir<int>(frequencia, "MaximoDeFaltas"))
                : RegraDeFrequencia.Padrao;

            var remuneracao = porto.GetSection("Remuneracao");
            var valores = remuneracao.GetSection("Valores").GetChildren()
                .Select(valor => new ValorDaFuncao(
                    new Funcao(Enum.Parse<Categoria>(Exigir<string>(valor, "Categoria")), Exigir<string>(valor, "Funcao")),
                    Dinheiro.Reais(Exigir<decimal>(valor, "PorPeriodo"))))
                .ToList();
            var tabela = new TabelaDeRemuneracao(valores, Exigir<decimal>(remuneracao, "AdicionalNoturno"));

            return new ConfiguracaoDoPorto(calendario, descanso, regra, tabela);
        }
        catch (Exception erro) when (erro is ExcecaoDeDominio or ArgumentException or FormatException)
        {
            throw new InvalidOperationException($"A configuração {porto.Path} não serve: {erro.Message}", erro);
        }
    }

    private static T Exigir<T>(IConfigurationSection secao, string chave) =>
        secao[chave] is not null
            ? secao.GetValue<T>(chave)!
            : throw new InvalidOperationException(string.Create(CultureInfo.InvariantCulture, $"Falta {secao.Path}:{chave} na configuração."));
}
