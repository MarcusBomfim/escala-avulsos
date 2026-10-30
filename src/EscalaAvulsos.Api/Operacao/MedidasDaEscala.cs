using System.Diagnostics;
using System.Diagnostics.Metrics;
using EscalaAvulsos.Infraestrutura.TempoReal;

namespace EscalaAvulsos.Api.Operacao;

/// <summary>
/// Os nomes da telemetria da escala (ADR-0022). Começam como o prefixo que a
/// observabilidade coleta, e por isso entram sem ninguém registrar.
/// </summary>
public static class TelemetriaDaEscala
{
    /// <summary>A fonte dos rastros e o medidor dos recados: a entrega aos celulares e a vigia dos prazos.</summary>
    public const string Avisos = "EscalaAvulsos.Avisos";

    /// <summary>O medidor das telas ligadas ao vivo.</summary>
    public const string TempoReal = "EscalaAvulsos.TempoReal";

    internal static readonly ActivitySource Fonte = new(Avisos);
}

/// <summary>
/// O que os recados medem: cada entrega a um celular, com o resultado, e os
/// recados que a vigia dos prazos deixou.
/// </summary>
/// <remarks>
/// Os atributos são só o resultado e o prazo: matrícula ou endereço de
/// celular numa métrica multiplicaria as séries sem fim — e são dados de
/// gente, que não vão para a telemetria.
/// </remarks>
public sealed class MedidasDosAvisos
{
    public const string Entregas = "escala.avisos.entregas";

    public const string Prazos = "escala.avisos.prazos";

    private readonly Counter<long> _entregas;

    private readonly Counter<long> _prazos;

    public MedidasDosAvisos(IMeterFactory medidores)
    {
        ArgumentNullException.ThrowIfNull(medidores);

        var medidor = medidores.Create(TelemetriaDaEscala.Avisos);

        _entregas = medidor.CreateCounter<long>(
            Entregas,
            unit: "{entrega}",
            description: "Recados levados a um celular inscrito, pelo resultado: entregue, recusado — fica para a próxima volta — ou inscrição vencida, que é apagada.");

        _prazos = medidor.CreateCounter<long>(
            Prazos,
            unit: "{recado}",
            description: "Recados que a vigia dos prazos deixou: a habilitação que vai fechar e o exame ou a reciclagem que vão vencer.");
    }

    internal void Entrega(string resultado) => _entregas.Add(1, new KeyValuePair<string, object?>("escala.entrega.resultado", resultado));

    internal void Prazo(string prazo, int recados)
    {
        if (recados > 0)
        {
            _prazos.Add(recados, new KeyValuePair<string, object?>("escala.prazo", prazo));
        }
    }
}

/// <summary>
/// O que o tempo real mede: quantas telas estão ligadas agora, e quantas
/// mudanças foram mandadas a elas, de que origem.
/// </summary>
public sealed class MedidasDoTempoReal
{
    public const string Ligacoes = "escala.eventos.ligacoes";

    public const string Mudancas = "escala.eventos.mudancas";

    private readonly UpDownCounter<long> _ligacoes;

    private readonly Counter<long> _mudancas;

    public MedidasDoTempoReal(IMeterFactory medidores)
    {
        ArgumentNullException.ThrowIfNull(medidores);

        var medidor = medidores.Create(TelemetriaDaEscala.TempoReal);

        _ligacoes = medidor.CreateUpDownCounter<long>(
            Ligacoes,
            unit: "{ligacao}",
            description: "Telas ligadas agora aos eventos do servidor, que se atualizam sozinhas.");

        _mudancas = medidor.CreateCounter<long>(
            Mudancas,
            unit: "{evento}",
            description: "Mudanças mandadas às telas ligadas, pela origem: escalações, requisições, escalas ou recados.");
    }

    /// <summary>Uma tela ligou; desliga quando o que voltar for descartado.</summary>
    internal IDisposable Ligada()
    {
        _ligacoes.Add(1);

        return new Desligar(_ligacoes);
    }

    internal void Mudanca(OrigemDoAnuncio origem) => _mudancas.Add(1, new KeyValuePair<string, object?>("escala.anuncio.origem", origem.ToString()));

    private sealed class Desligar(UpDownCounter<long> ligacoes) : IDisposable
    {
        public void Dispose() => ligacoes.Add(-1);
    }
}
