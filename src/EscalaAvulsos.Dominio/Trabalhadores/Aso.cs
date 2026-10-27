using EscalaAvulsos.Dominio.Comum;

namespace EscalaAvulsos.Dominio.Trabalhadores;

/// <summary>
/// O atestado de saúde ocupacional: o exame médico que diz que o trabalhador
/// pode trabalhar no porto, e até quando.
/// </summary>
/// <remarks>
/// Sem exame válido não há escala: a NR-29 exige o exame do trabalhador
/// portuário, e a NR-7 diz de quanto em quanto tempo ele se repete. O
/// período é dado pelo médico, e chega aqui como a data de validade.
/// </remarks>
public sealed record Aso
{
    public Aso(DateOnly emitidoEm, DateOnly validoAte)
    {
        if (validoAte < emitidoEm)
        {
            throw new ExcecaoDeDominio(
                $"O exame médico emitido em {FormatoBrasileiro.Data(emitidoEm)} não pode valer só até {FormatoBrasileiro.Data(validoAte)}, antes de ser emitido.");
        }

        EmitidoEm = emitidoEm;
        ValidoAte = validoAte;
    }

    public DateOnly EmitidoEm { get; }

    /// <summary>O último dia em que o exame vale.</summary>
    public DateOnly ValidoAte { get; }
}
