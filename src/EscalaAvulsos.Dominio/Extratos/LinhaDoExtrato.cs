using EscalaAvulsos.Dominio.Comum;
using EscalaAvulsos.Dominio.Escalacoes;
using EscalaAvulsos.Dominio.Periodos;
using EscalaAvulsos.Dominio.Requisicoes;
using EscalaAvulsos.Dominio.Trabalhadores;

namespace EscalaAvulsos.Dominio.Extratos;

/// <summary>Um período trabalhado, no extrato: onde, em que função, quanto, e até quando é pago.</summary>
public sealed record LinhaDoExtrato
{
    /// <summary>O prazo do pagamento: 48 horas depois do fim do serviço (Lei 9.719/1998, art. 2º).</summary>
    public static readonly TimeSpan PrazoDoPagamento = TimeSpan.FromHours(48);

    internal LinhaDoExtrato(Engajamento engajamento, TabelaDeRemuneracao tabela)
    {
        Periodo = engajamento.Periodo;
        Funcao = engajamento.Funcao;
        Navio = engajamento.Navio;
        Terno = engajamento.Terno;
        Valor = tabela.ValorDe(engajamento.Funcao);
        AdicionalNoturno = engajamento.Periodo.Noturno ? (Valor * tabela.AdicionalNoturno).EmCentavos() : Dinheiro.Zero;
    }

    public Periodo Periodo { get; }

    public Funcao Funcao { get; }

    public Navio Navio { get; }

    public int Terno { get; }

    /// <summary>O valor do período na função, pela tabela.</summary>
    public Dinheiro Valor { get; }

    /// <summary>O adicional da noite e da madrugada; zero de dia.</summary>
    public Dinheiro AdicionalNoturno { get; }

    public Dinheiro Total => Valor + AdicionalNoturno;

    public DateTimeOffset PagoAte => Periodo.Fim + PrazoDoPagamento;

    /// <summary>
    /// "07/11/2026, das 19h à 1h — estivador no navio Atlântico Sul (IMO
    /// 9876543): R$ 220,00 + R$ 44,00 de adicional noturno = R$ 264,00, pago
    /// até 10/11/2026, à 1h".
    /// </summary>
    public override string ToString()
    {
        var quanto = AdicionalNoturno > Dinheiro.Zero ? $"{Valor} + {AdicionalNoturno} de adicional noturno = {Total}" : $"{Total}";

        return $"{Periodo} — {Funcao.NaFrase} no navio {Navio}: {quanto}, pago até {FormatoBrasileiro.Instante(PagoAte)}";
    }
}
