using EscalaAvulsos.Dominio.Avisos;
using EscalaAvulsos.Dominio.Trabalhadores;
using EscalaAvulsos.Infraestrutura.Persistencia.Registros;
using Microsoft.EntityFrameworkCore;

namespace EscalaAvulsos.Infraestrutura.Persistencia.Repositorios;

/// <summary>Um aviso como está guardado: o que ele diz, e se o trabalhador já abriu os recados depois dele.</summary>
public sealed record AvisoGuardado(Aviso Aviso, DateTimeOffset? LidoEm);

/// <summary>Um aviso reservado para ir aos celulares, com quantas vezes já se tentou entregá-lo.</summary>
public sealed record AvisoParaEntregar(Aviso Aviso, int Tentativas);

/// <summary>
/// Os recados de cada trabalhador: os que a escala deixou, os que o tempo
/// trouxe — a habilitação que vai fechar, o exame que vai vencer —, quais ele
/// já leu e quais ainda faltam ir ao celular dele (ADR-0019, ADR-0020).
/// </summary>
/// <remarks>
/// A entrega aos celulares é de quem pegar primeiro: o servidor reserva os
/// avisos que vai mandar, e outro servidor só pega os mesmos depois que a
/// reserva vence. Se o primeiro cair no meio, o aviso não se perde — vai na
/// próxima volta, de quem estiver de pé.
/// </remarks>
public sealed class RepositorioDeAvisos(ContextoDoBanco contexto, TimeProvider relogio)
{
    /// <summary>Quantas vezes se tenta entregar um aviso antes de desistir dele: o recado continua no aplicativo.</summary>
    public const int TentativasDeEntrega = 5;

    /// <summary>Guarda os avisos novos; o que já existe, com a mesma chave, fica como estava.</summary>
    /// <returns>Quantos eram novos.</returns>
    public Task<int> Guardar(IReadOnlyCollection<Aviso> avisos, CancellationToken cancelamento = default)
    {
        ArgumentNullException.ThrowIfNull(avisos);

        return CaixaDeAvisos.Guardar(contexto, avisos, cancelamento);
    }

    /// <summary>Os recados do trabalhador, do mais novo para o mais antigo.</summary>
    public async Task<IReadOnlyList<AvisoGuardado>> DoTrabalhador(TrabalhadorId trabalhador, int quantos, CancellationToken cancelamento = default)
    {
        ArgumentNullException.ThrowIfNull(trabalhador);

        var registros = await contexto.Avisos
            .AsNoTracking()
            .Where(registro => registro.TrabalhadorId == trabalhador.Valor)
            .OrderByDescending(registro => registro.CriadoEm)
            .ThenBy(registro => registro.Chave)
            .Take(quantos)
            .ToListAsync(cancelamento);

        return [.. registros.Select(registro => new AvisoGuardado(Abrir(registro), registro.LidoEm))];
    }

    /// <summary>Quantos recados ele ainda não leu.</summary>
    public Task<int> NaoLidos(TrabalhadorId trabalhador, CancellationToken cancelamento = default)
    {
        ArgumentNullException.ThrowIfNull(trabalhador);

        return contexto.Avisos.CountAsync(registro => registro.TrabalhadorId == trabalhador.Valor && registro.LidoEm == null, cancelamento);
    }

    /// <summary>Ele abriu os recados: os que ainda não tinha lido passam a lidos, agora.</summary>
    /// <returns>Quantos estavam por ler.</returns>
    public Task<int> MarcarLidos(TrabalhadorId trabalhador, CancellationToken cancelamento = default)
    {
        ArgumentNullException.ThrowIfNull(trabalhador);

        var agora = relogio.GetUtcNow();

        return contexto.Avisos
            .Where(registro => registro.TrabalhadorId == trabalhador.Valor && registro.LidoEm == null)
            .ExecuteUpdateAsync(colunas => colunas.SetProperty(registro => registro.LidoEm, agora), cancelamento);
    }

    /// <summary>
    /// Reserva, para este servidor, os avisos mais antigos que faltam entregar
    /// — os que ninguém reservou, ou cuja reserva já venceu —, e conta a tentativa.
    /// </summary>
    /// <param name="reserva">Por quanto tempo os avisos ficam com este servidor antes de outro poder pegá-los.</param>
    public async Task<IReadOnlyList<AvisoParaEntregar>> Reservar(int quantos, TimeSpan reserva, CancellationToken cancelamento = default)
    {
        var agora = relogio.GetUtcNow();
        var vencida = agora - reserva;

        var reservados = await contexto.Avisos
            .FromSql(
                $"""
                update escala.avisos aviso
                set reservado_em = {agora}, tentativas = aviso.tentativas + 1
                from (
                    select trabalhador_id, chave
                    from escala.avisos
                    where entregue_em is null and tentativas < {TentativasDeEntrega} and (reservado_em is null or reservado_em < {vencida})
                    order by criado_em
                    limit {quantos}
                    for update skip locked
                ) livre
                where aviso.trabalhador_id = livre.trabalhador_id and aviso.chave = livre.chave
                returning aviso.*
                """)
            .AsNoTracking()
            .ToListAsync(cancelamento);

        return [.. reservados.OrderBy(registro => registro.CriadoEm).Select(registro => new AvisoParaEntregar(Abrir(registro), registro.Tentativas))];
    }

    /// <summary>O aviso foi entregue aos celulares do trabalhador — ou não há celular para entregar: não se tenta mais.</summary>
    public Task<int> Entregue(Aviso aviso, CancellationToken cancelamento = default)
    {
        ArgumentNullException.ThrowIfNull(aviso);

        var agora = relogio.GetUtcNow();

        return contexto.Avisos
            .Where(registro => registro.TrabalhadorId == aviso.Trabalhador.Valor && registro.Chave == aviso.Chave)
            .ExecuteUpdateAsync(colunas => colunas.SetProperty(registro => registro.EntregueEm, agora), cancelamento);
    }

    private static Aviso Abrir(RegistroDeAviso registro) =>
        new(new TrabalhadorId(registro.TrabalhadorId), Enum.Parse<TipoDoAviso>(registro.Tipo), registro.Chave, registro.Titulo, registro.Texto, registro.CriadoEm);
}
