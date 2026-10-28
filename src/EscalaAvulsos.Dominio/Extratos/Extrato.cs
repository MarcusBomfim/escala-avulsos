using EscalaAvulsos.Dominio.Comum;
using EscalaAvulsos.Dominio.Escalacoes;
using EscalaAvulsos.Dominio.Identificacao;
using EscalaAvulsos.Dominio.Trabalhadores;

namespace EscalaAvulsos.Dominio.Extratos;

/// <summary>
/// O extrato do mês de um trabalhador: cada período trabalhado, com o valor, o
/// adicional noturno e o prazo do pagamento — e, no total, as provisões de
/// férias e de 13º e o FGTS depositado (ADR-0010).
/// </summary>
/// <remarks>
/// <para>
/// Só entra o período trabalhado: na escala, com a presença confirmada. Falta,
/// mesmo justificada, e desistência não são pagas.
/// </para>
/// <para>
/// As provisões seguem a conta da remuneração do avulso: as férias com o terço
/// são 1/12 mais um terço disso — 1/9 da remuneração —, o 13º é 1/12, e o FGTS
/// é 8% da soma dos três. Os descontos — INSS, imposto de renda, contribuições
/// — vêm da folha do órgão gestor, e não entram aqui.
/// </para>
/// </remarks>
public sealed class Extrato
{
    /// <summary>O FGTS: 8% da remuneração, das férias e do 13º.</summary>
    public const decimal AliquotaDoFgts = 0.08m;

    private Extrato(Trabalhador trabalhador, Mes mes, IReadOnlyList<LinhaDoExtrato> linhas)
    {
        Trabalhador = trabalhador.Id;
        Matricula = trabalhador.Matricula;
        Nome = trabalhador.Nome;
        Mes = mes;
        Linhas = linhas;
        Remuneracao = linhas.Aggregate(Dinheiro.Zero, (soma, linha) => soma + linha.Total);
        Ferias = (Remuneracao * (1m / 9m)).EmCentavos();
        DecimoTerceiro = (Remuneracao * (1m / 12m)).EmCentavos();
        Fgts = ((Remuneracao + Ferias + DecimoTerceiro) * AliquotaDoFgts).EmCentavos();
    }

    public TrabalhadorId Trabalhador { get; }

    public Matricula Matricula { get; }

    public string Nome { get; }

    public Mes Mes { get; }

    /// <summary>Os períodos trabalhados no mês, na ordem em que aconteceram.</summary>
    public IReadOnlyList<LinhaDoExtrato> Linhas { get; }

    /// <summary>A soma dos períodos trabalhados, com o adicional noturno.</summary>
    public Dinheiro Remuneracao { get; }

    /// <summary>A provisão de férias, com o terço: 1/9 da remuneração.</summary>
    public Dinheiro Ferias { get; }

    /// <summary>A provisão de 13º: 1/12 da remuneração.</summary>
    public Dinheiro DecimoTerceiro { get; }

    /// <summary>O FGTS depositado: 8% da remuneração, das férias e do 13º.</summary>
    public Dinheiro Fgts { get; }

    /// <summary>
    /// O extrato do trabalhador no mês, tirado das escalas. Os períodos são do
    /// mês do dia em que começam: a noite de 30/11 é de novembro (ADR-0002).
    /// </summary>
    public static Extrato De(Trabalhador trabalhador, Mes mes, IEnumerable<Escala> escalas, TabelaDeRemuneracao tabela)
    {
        ArgumentNullException.ThrowIfNull(trabalhador);
        ArgumentNullException.ThrowIfNull(mes);
        ArgumentNullException.ThrowIfNull(escalas);
        ArgumentNullException.ThrowIfNull(tabela);

        var linhas = escalas
            .Where(escala => Mes.De(escala.Periodo.Dia) == mes)
            .Select(escala => escala.Situacoes.SingleOrDefault(situacao => situacao.Trabalhador == trabalhador.Id))
            .Where(situacao => situacao is { Escalado: true, Comparecimento: Comparecimento.Presente })
            .Select(situacao => new LinhaDoExtrato(situacao!.Engajamento!, tabela))
            .OrderBy(linha => linha.Periodo)
            .ToList();

        return new Extrato(trabalhador, mes, linhas);
    }

    /// <summary>
    /// "Antônio Ferreira (matrícula 4127), em 11/2026: 2 períodos, R$ 524,00;
    /// férias com 1/3, R$ 58,22; 13º, R$ 43,67; FGTS depositado, R$ 50,07."
    /// </summary>
    public override string ToString() =>
        Linhas.Count == 0
            ? $"{Nome} (matrícula {Matricula}), em {Mes}: nenhum período trabalhado."
            : $"{Nome} (matrícula {Matricula}), em {Mes}: {Linhas.Count} {(Linhas.Count == 1 ? "período" : "períodos")}, {Remuneracao}; "
              + $"férias com 1/3, {Ferias}; 13º, {DecimoTerceiro}; FGTS depositado, {Fgts}.";
}
