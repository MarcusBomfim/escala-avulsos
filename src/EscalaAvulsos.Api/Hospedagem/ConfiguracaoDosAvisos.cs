using EscalaAvulsos.Dominio.Avisos;
using EscalaAvulsos.Infraestrutura.Avisos;

namespace EscalaAvulsos.Api.Hospedagem;

/// <summary>
/// Os avisos, da seção <c>Avisos</c>: quanto antes avisar, de quanto em
/// quanto conferir e entregar, e as chaves do push (ADR-0019, ADR-0020).
/// </summary>
/// <remarks>
/// Sem a subseção <c>Push</c>, o aviso não vai ao celular — fica só nos
/// recados do aplicativo. As chaves pela metade, ou que não formam um par,
/// impedem a API de subir: melhor do que descobrir no primeiro aviso.
/// </remarks>
public sealed record ConfiguracaoDosAvisos(
    TimeSpan AntecedenciaDoFechamento,
    int DiasAntesDoVencimento,
    TimeSpan IntervaloDaConferencia,
    TimeSpan IntervaloDaEntrega,
    bool ConferirEEntregarSozinho,
    ConfiguracaoDoPush? Push)
{
    public static ConfiguracaoDosAvisos Ler(IConfigurationSection avisos)
    {
        ArgumentNullException.ThrowIfNull(avisos);

        var push = avisos.GetSection("Push");
        ConfiguracaoDoPush? doPush = null;

        if (push.Exists())
        {
            var publica = push["ChavePublica"];
            var privada = push["ChavePrivada"];
            var contato = push["Contato"];

            if (string.IsNullOrWhiteSpace(publica) || string.IsNullOrWhiteSpace(privada) || string.IsNullOrWhiteSpace(contato))
            {
                throw new InvalidOperationException($"A configuração {push.Path} precisa de ChavePublica, ChavePrivada e Contato — ou de não existir.");
            }

            try
            {
                doPush = new ConfiguracaoDoPush(ChavesVapid.Ler(publica, privada), contato);
            }
            catch (FormatException erro)
            {
                throw new InvalidOperationException($"A configuração {push.Path} não serve: {erro.Message}", erro);
            }
        }

        return new ConfiguracaoDosAvisos(
            avisos.GetValue<TimeSpan?>("AntecedenciaDoFechamento") ?? AvisosDosPrazos.AntecedenciaDoFechamento,
            avisos.GetValue<int?>("DiasAntesDoVencimento") ?? AvisosDosPrazos.DiasAntesDoVencimento,
            avisos.GetValue<TimeSpan?>("IntervaloDaConferencia") ?? TimeSpan.FromMinutes(1),
            avisos.GetValue<TimeSpan?>("IntervaloDaEntrega") ?? TimeSpan.FromSeconds(30),
            avisos.GetValue<bool?>("ConferirEEntregarSozinho") ?? true,
            doPush);
    }
}
