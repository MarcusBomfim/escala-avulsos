namespace EscalaAvulsos.Dominio.Comum;

/// <summary>
/// A identidade de um agregado: o que continua igual quando todo o resto muda.
/// </summary>
/// <remarks>
/// <para>
/// Um tipo por agregado — <c>TrabalhadorId</c>, <c>RequisicaoId</c> —, e não
/// <see cref="Guid"/> solto: passar o id de uma requisição onde se espera o
/// de um trabalhador vira erro de compilação. Dois ids de tipos diferentes
/// nunca são iguais, nem com o mesmo Guid.
/// </para>
/// <para>
/// Record de classe, como os outros valores do domínio: o Guid vazio é
/// recusado no construtor, e não há <c>default</c> que fure a regra. Os ids
/// novos são Guid versão 7, que começam pelo instante da criação: criados em
/// sequência, ficam em sequência no índice do banco.
/// </para>
/// </remarks>
public abstract record Identificador
{
    protected Identificador(Guid valor)
    {
        if (valor == Guid.Empty)
        {
            throw new ExcecaoDeDominio("O identificador não pode ser vazio.");
        }

        Valor = valor;
    }

    public Guid Valor { get; }

    public sealed override string ToString() => Valor.ToString();
}
