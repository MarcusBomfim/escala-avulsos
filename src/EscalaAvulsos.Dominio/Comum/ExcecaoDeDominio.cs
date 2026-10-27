namespace EscalaAvulsos.Dominio.Comum;

/// <summary>
/// Uma regra do trabalho portuário foi violada.
/// </summary>
/// <remarks>
/// A mensagem é escrita para quem usa o sistema — o trabalhador, o escalador,
/// o operador portuário —, e não para quem depura o código: ela diz o que está
/// errado e, quando dá, o que fazer. As camadas de fora repassam a mensagem
/// como veio, sem reescrever.
/// </remarks>
public sealed class ExcecaoDeDominio : Exception
{
    public ExcecaoDeDominio()
    {
    }

    public ExcecaoDeDominio(string mensagem)
        : base(mensagem)
    {
    }

    public ExcecaoDeDominio(string mensagem, Exception causa)
        : base(mensagem, causa)
    {
    }
}
