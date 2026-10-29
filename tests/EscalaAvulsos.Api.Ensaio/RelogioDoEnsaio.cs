namespace EscalaAvulsos.Api.Ensaio;

/// <summary>
/// O relógio da API do ensaio: o do sistema, adiantado ou atrasado até o
/// instante que o teste acerta — e andando dali em diante, como um relógio
/// de verdade, junto com o do navegador.
/// </summary>
/// <remarks>
/// A escala tem hora: a habilitação abre 12 horas antes do período e fecha
/// 3 horas antes, e a escala só sai depois. Sem acertar o relógio, o teste
/// teria de esperar horas — ou de mexer no calendário do porto, e então não
/// testaria o calendário de verdade.
/// </remarks>
public sealed class RelogioDoEnsaio : TimeProvider
{
    private long _diferenca;

    public void Acertar(DateTimeOffset agora) =>
        Interlocked.Exchange(ref _diferenca, (agora - base.GetUtcNow()).Ticks);

    public override DateTimeOffset GetUtcNow() =>
        base.GetUtcNow() + TimeSpan.FromTicks(Interlocked.Read(ref _diferenca));
}
