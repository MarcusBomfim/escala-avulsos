using EscalaAvulsos.Dominio.Trabalhadores;
using EscalaAvulsos.Infraestrutura.Avisos;
using Microsoft.EntityFrameworkCore;

namespace EscalaAvulsos.Infraestrutura.Persistencia.Repositorios;

/// <summary>
/// Os celulares de cada trabalhador que aceitaram os avisos (ADR-0020). O
/// endereço é do navegador, e não do trabalhador: o mesmo celular que passa
/// a outro trabalhador — o filho que entra com a matrícula do pai — passa a
/// receber os avisos de quem entrou por último.
/// </summary>
public sealed class RepositorioDeInscricoesDePush(ContextoDoBanco contexto, TimeProvider relogio)
{
    /// <summary>Guarda a inscrição do navegador para o trabalhador — a mesma, se o navegador já estava inscrito.</summary>
    public Task<int> Guardar(TrabalhadorId trabalhador, InscricaoDePush inscricao, CancellationToken cancelamento = default)
    {
        ArgumentNullException.ThrowIfNull(trabalhador);
        ArgumentNullException.ThrowIfNull(inscricao);

        var agora = relogio.GetUtcNow();

        return contexto.Database.ExecuteSqlAsync(
            $"""
            insert into escala.inscricoes_de_push (endereco, trabalhador_id, chave_publica, segredo, criada_em)
            values ({inscricao.Endereco.AbsoluteUri}, {trabalhador.Valor}, {inscricao.ChavePublicaEmBase64Url}, {inscricao.SegredoEmBase64Url}, {agora})
            on conflict (endereco) do update
            set trabalhador_id = excluded.trabalhador_id, chave_publica = excluded.chave_publica, segredo = excluded.segredo, criada_em = excluded.criada_em
            """,
            cancelamento);
    }

    /// <summary>Os navegadores do trabalhador, do inscrito há mais tempo para o mais novo.</summary>
    public async Task<IReadOnlyList<InscricaoDePush>> DoTrabalhador(TrabalhadorId trabalhador, CancellationToken cancelamento = default)
    {
        ArgumentNullException.ThrowIfNull(trabalhador);

        var registros = await contexto.InscricoesDePush
            .AsNoTracking()
            .Where(registro => registro.TrabalhadorId == trabalhador.Valor)
            .OrderBy(registro => registro.CriadaEm)
            .ToListAsync(cancelamento);

        return [.. registros.Select(registro => InscricaoDePush.De(registro.Endereco, registro.ChavePublica, registro.Segredo))];
    }

    /// <summary>Tira o navegador: o trabalhador não quer mais os avisos nele, ou o serviço de push disse que a inscrição venceu.</summary>
    /// <param name="trabalhador">Só se for deste trabalhador; nulo, qualquer que seja o dono.</param>
    public Task<int> Remover(Uri endereco, TrabalhadorId? trabalhador = null, CancellationToken cancelamento = default)
    {
        ArgumentNullException.ThrowIfNull(endereco);

        var texto = endereco.AbsoluteUri;
        var dono = trabalhador?.Valor;

        return contexto.InscricoesDePush
            .Where(registro => registro.Endereco == texto && (dono == null || registro.TrabalhadorId == dono))
            .ExecuteDeleteAsync(cancelamento);
    }
}
