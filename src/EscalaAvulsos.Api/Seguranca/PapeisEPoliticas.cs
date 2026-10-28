namespace EscalaAvulsos.Api.Seguranca;

/// <summary>Os papéis de quem usa a escala, como vêm no token — na reivindicação <c>role</c> (ADR-0014).</summary>
public static class Papeis
{
    /// <summary>O trabalhador avulso: entra pela matrícula, vê o cadastro e a escala dele, se habilita, desiste.</summary>
    public const string Trabalhador = "trabalhador";

    /// <summary>Quem trabalha no órgão gestor: cadastra os trabalhadores, abre as escalações, faz a escala, registra a presença e a falta.</summary>
    public const string Escalador = "escalador";

    /// <summary>O operador portuário: pede a mão de obra, pelo CNPJ dele.</summary>
    public const string Operador = "operador";
}

/// <summary>O que cada pedido exige de quem pede (ADR-0014).</summary>
public static class Politicas
{
    /// <summary>O papel de trabalhador, com uma matrícula válida no token: é por ela que ele é achado.</summary>
    public const string Trabalhador = "trabalhador";

    /// <summary>O papel de escalador.</summary>
    public const string Escalar = "escalar";

    /// <summary>O papel de operador, com um CNPJ válido no token: é em nome dele que as requisições são feitas.</summary>
    public const string Requisitar = "requisitar";
}

/// <summary>Os nomes das reivindicações que a API lê do token, tal como vêm nele.</summary>
public static class Reivindicacoes
{
    public const string Papel = "role";

    public const string Nome = "name";

    /// <summary>A matrícula do trabalhador, como o provedor de identidade a conferiu na entrada.</summary>
    public const string Matricula = "matricula";

    /// <summary>O CNPJ do operador portuário, como o provedor de identidade o conferiu na entrada.</summary>
    public const string Cnpj = "cnpj";
}
