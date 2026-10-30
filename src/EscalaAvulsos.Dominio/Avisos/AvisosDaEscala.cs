using EscalaAvulsos.Dominio.Comum;
using EscalaAvulsos.Dominio.Escalacoes;
using EscalaAvulsos.Dominio.Periodos;

namespace EscalaAvulsos.Dominio.Avisos;

/// <summary>
/// Os avisos que a escala dá: quando sai, um para cada habilitado — quem vai
/// trabalhar e onde, quem ficou de fora e por quê —; depois, um para quem
/// entra no lugar de quem desistiu.
/// </summary>
public static class AvisosDaEscala
{
    /// <summary>Um aviso para cada habilitado da escala que acabou de sair.</summary>
    public static IReadOnlyList<Aviso> DaEscalaQueSaiu(Escala escala)
    {
        ArgumentNullException.ThrowIfNull(escala);

        return [.. escala.Situacoes.Select(situacao => situacao.Engajamento is { } vaga
            ? new Aviso(
                situacao.Trabalhador,
                TipoDoAviso.VaiTrabalhar,
                ChaveDaEscala(escala),
                "Você vai trabalhar",
                $"{PorExtenso(escala.Periodo)}: {NaVaga(vaga)}.",
                escala.FeitaEm)
            : new Aviso(
                situacao.Trabalhador,
                TipoDoAviso.FicouDeFora,
                ChaveDaEscala(escala),
                "Você ficou de fora da escala",
                $"{PorExtenso(escala.Periodo)}: {Porque(situacao)}{NaEspera(escala, situacao)}",
                escala.FeitaEm))];
    }

    /// <summary>
    /// Os avisos do que mudou na escala depois que ela saiu: quem não tinha
    /// vaga e agora tem — a vaga de quem desistiu, que passou para ele.
    /// </summary>
    /// <param name="em">Quando a mudança foi gravada.</param>
    public static IReadOnlyList<Aviso> DaMudanca(Escala antes, Escala depois, DateTimeOffset em)
    {
        ArgumentNullException.ThrowIfNull(antes);
        ArgumentNullException.ThrowIfNull(depois);

        return [.. depois.Situacoes
            .Where(situacao => situacao.Engajamento is not null && antes.SituacaoDe(situacao.Matricula)?.Engajamento is null)
            .Select(situacao => new Aviso(
                situacao.Trabalhador,
                TipoDoAviso.EntrouPelaEspera,
                $"espera:{depois.Id}",
                "Você entrou na escala",
                $"Quem estava na vaga desistiu, e ela passou para você. {PorExtenso(depois.Periodo)}: {NaVaga(situacao.Engajamento!)}.",
                em))];
    }

    /// <summary>"Quinta-feira, 5 de novembro, das 13h às 19h".</summary>
    internal static string PorExtenso(Periodo periodo)
    {
        var dia = FormatoBrasileiro.DiaPorExtenso(periodo.Dia);

        return $"{char.ToUpperInvariant(dia[0])}{dia[1..]}, {periodo.Horario}";
    }

    private static string ChaveDaEscala(Escala escala) => $"escala:{escala.Id}";

    /// <summary>"sinaleiro no terno 1 do navio Atlântico Sul, no Terminal Cais Leste, berço 4" — sem o IMO, que o trabalhador não usa.</summary>
    private static string NaVaga(Engajamento vaga) => $"{vaga.Funcao.NaFrase} no terno {vaga.Terno} do navio {vaga.Navio.Nome}, no {vaga.Local}";

    /// <summary>Os motivos, como o domínio já os diz para o trabalhador: "o rodízio parou 1 posição antes da sua vez."</summary>
    private static string Porque(SituacaoNaEscala situacao) =>
        situacao.Motivos.Count == 0
            ? "não houve vaga para você."
            : $"{string.Join("; ", situacao.Motivos.Select(motivo => motivo.Descricao.TrimEnd('.')))}.";

    /// <summary>O melhor lugar dele na espera, se está em alguma: é quem entra se alguém desistir.</summary>
    private static string NaEspera(Escala escala, SituacaoNaEscala situacao)
    {
        var melhor = situacao.Funcoes
            .Select(funcao => (Funcao: funcao, Posicao: escala.FilaDeEsperaDe(funcao).ToList().IndexOf(situacao.Matricula) + 1))
            .Where(lugar => lugar.Posicao > 0)
            .OrderBy(lugar => lugar.Posicao)
            .FirstOrDefault();

        return melhor.Funcao is null
            ? string.Empty
            : $" Você é o {melhor.Posicao}º da espera de {melhor.Funcao.NaFrase}: se alguém desistir, a vaga pode ser sua.";
    }
}
