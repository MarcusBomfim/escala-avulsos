using EscalaAvulsos.Dominio.Avisos;
using Microsoft.EntityFrameworkCore;

namespace EscalaAvulsos.Infraestrutura.Persistencia;

/// <summary>
/// Onde os avisos entram no banco: numa instrução só, e sem repetir o que já
/// está lá — o aviso com a mesma chave, para o mesmo trabalhador, fica como
/// estava (ADR-0019). Quem grava a escala chama daqui, na transação dela: o
/// aviso existe se, e só se, a escala foi gravada.
/// </summary>
internal static class CaixaDeAvisos
{
    public static async Task<int> Guardar(ContextoDoBanco contexto, IReadOnlyCollection<Aviso> avisos, CancellationToken cancelamento)
    {
        if (avisos.Count == 0)
        {
            return 0;
        }

        var trabalhadores = avisos.Select(aviso => aviso.Trabalhador.Valor).ToArray();
        var chaves = avisos.Select(aviso => aviso.Chave).ToArray();
        var tipos = avisos.Select(aviso => aviso.Tipo.ToString()).ToArray();
        var titulos = avisos.Select(aviso => aviso.Titulo).ToArray();
        var textos = avisos.Select(aviso => aviso.Texto).ToArray();
        var criados = avisos.Select(aviso => aviso.CriadoEm.ToUniversalTime()).ToArray();

        return await contexto.Database.ExecuteSqlAsync(
            $"""
            insert into escala.avisos (trabalhador_id, chave, tipo, titulo, texto, criado_em, tentativas)
            select trabalhador_id, chave, tipo, titulo, texto, criado_em, 0
            from unnest({trabalhadores}, {chaves}, {tipos}, {titulos}, {textos}, {criados}) as novo(trabalhador_id, chave, tipo, titulo, texto, criado_em)
            on conflict (trabalhador_id, chave) do nothing
            """,
            cancelamento);
    }
}
