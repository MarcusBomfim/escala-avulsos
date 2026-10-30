using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace EscalaAvulsos.Api.Testes;

/// <summary>
/// As medidas de uma API só, guardadas como chegam. Os medidores da API nascem
/// da fábrica de medidores dela; os testes rodam em paralelo, cada um com a
/// sua API, e é a fábrica que separa o que é deste teste.
/// </summary>
internal sealed class Medicoes : IDisposable
{
    private readonly MeterListener _ouvinte = new();

    public Medicoes(IMeterFactory fabrica)
    {
        _ouvinte.InstrumentPublished = (instrumento, ouvinte) =>
        {
            if (ReferenceEquals(instrumento.Meter.Scope, fabrica))
            {
                ouvinte.EnableMeasurementEvents(instrumento);
            }
        };
        _ouvinte.SetMeasurementEventCallback<long>((instrumento, valor, atributos, _) =>
            Recebidas.Enqueue((instrumento.Name, valor, atributos.ToArray().ToDictionary(atributo => atributo.Key, atributo => atributo.Value?.ToString()))));
        _ouvinte.Start();
    }

    public ConcurrentQueue<(string Instrumento, long Valor, IReadOnlyDictionary<string, string?> Atributos)> Recebidas { get; } = new();

    /// <summary>A soma do instrumento, nas medidas com este atributo — ou em todas, sem atributo.</summary>
    public long Soma(string instrumento, string? atributo = null, string? valor = null) =>
        Recebidas
            .Where(medida => medida.Instrumento == instrumento && (atributo is null || medida.Atributos.GetValueOrDefault(atributo) == valor))
            .Sum(medida => medida.Valor);

    public void Dispose() => _ouvinte.Dispose();
}
