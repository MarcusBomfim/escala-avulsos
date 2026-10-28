using Microsoft.EntityFrameworkCore;

namespace EscalaAvulsos.Infraestrutura.Persistencia.Idempotencia;

/// <summary>
/// As chaves de idempotência de cada um, com a resposta dada a cada uma
/// (ADR-0015).
/// </summary>
/// <remarks>
/// <para>
/// A reserva da chave anda na mesma transação da operação que ela protege:
/// ou as duas ficam gravadas, ou nenhuma. Operação que falha desfaz a
/// reserva, e o pedido pode ser repetido com a mesma chave.
/// </para>
/// <para>
/// Um pedido repetido enquanto o primeiro ainda roda não executa de novo:
/// a reserva dele espera na chave primária até o primeiro terminar, e então
/// recebe a resposta guardada. É o PostgreSQL que põe os dois em fila — não
/// há janela entre conferir a chave e reservá-la.
/// </para>
/// </remarks>
public sealed class ChavesDeIdempotencia(ContextoDoBanco contexto, TimeProvider relogio)
{
    public const int TamanhoMaximoDaChave = 255;

    /// <summary>
    /// Atende o pedido uma vez só. Chave nova: executa a operação e, se ela
    /// deu certo, guarda a resposta. Chave já atendida: devolve a resposta
    /// guardada, sem executar nada.
    /// </summary>
    /// <param name="dono">Quem pede: a chave é dele, e não esbarra na de outro.</param>
    /// <param name="impressaoDoPedido">O que identifica o pedido, para reconhecê-lo quando ele chegar de novo.</param>
    /// <param name="operacao">
    /// A operação. Ela grava pelo mesmo contexto — o do pedido —, e por isso
    /// dentro da transação da chave.
    /// </param>
    /// <exception cref="ChaveDeIdempotenciaReutilizada">A chave já atendeu outro pedido.</exception>
    public async Task<PedidoAtendido> Atender(
        string dono,
        string chave,
        string impressaoDoPedido,
        Func<CancellationToken, Task<RespostaGuardada>> operacao,
        CancellationToken cancelamento = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dono);
        ArgumentException.ThrowIfNullOrWhiteSpace(chave);
        ArgumentException.ThrowIfNullOrWhiteSpace(impressaoDoPedido);
        ArgumentNullException.ThrowIfNull(operacao);

        if (chave.Length > TamanhoMaximoDaChave)
        {
            throw new ArgumentException($"A chave de idempotência tem mais de {TamanhoMaximoDaChave} caracteres.", nameof(chave));
        }

        await using var transacao = await contexto.Database.BeginTransactionAsync(cancelamento);

        var reservadas = await contexto.Database.ExecuteSqlAsync(
            $"""
            insert into escala.chaves_de_idempotencia (dono, chave, impressao_do_pedido, criada_em)
            values ({dono}, {chave}, {impressaoDoPedido}, {relogio.GetUtcNow()})
            on conflict (dono, chave) do nothing
            """,
            cancelamento);

        if (reservadas == 0)
        {
            var gravada = await contexto.ChavesDeIdempotencia
                .AsNoTracking()
                .SingleAsync(registro => registro.Dono == dono && registro.Chave == chave, cancelamento);

            return gravada.ImpressaoDoPedido == impressaoDoPedido
                ? new PedidoAtendido(gravada.ParaResposta(), Repetido: true)
                : throw new ChaveDeIdempotenciaReutilizada(
                    $"A chave de idempotência {chave} já foi usada num pedido diferente deste. Pedido novo leva chave nova.");
        }

        var resposta = await operacao(cancelamento);

        // Só o sucesso fica. Erro desfaz a operação e a reserva juntas, e o
        // pedido pode ser feito de novo com a mesma chave.
        if (resposta.Status is >= 200 and < 300)
        {
            await contexto.ChavesDeIdempotencia
                .Where(registro => registro.Dono == dono && registro.Chave == chave)
                .ExecuteUpdateAsync(
                    colunas => colunas
                        .SetProperty(registro => registro.Status, (short)resposta.Status)
                        .SetProperty(registro => registro.Cabecalhos, RegistroDeIdempotencia.EscreverCabecalhos(resposta.Cabecalhos))
                        .SetProperty(registro => registro.Corpo, resposta.Corpo),
                    cancelamento);
            await transacao.CommitAsync(cancelamento);
        }

        return new PedidoAtendido(resposta, Repetido: false);
    }
}
