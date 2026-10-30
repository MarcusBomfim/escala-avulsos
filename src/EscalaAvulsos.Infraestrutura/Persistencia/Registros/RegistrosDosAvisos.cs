namespace EscalaAvulsos.Infraestrutura.Persistencia.Registros;

/// <summary>
/// Um aviso guardado para o trabalhador: o recado que ele lê no aplicativo e
/// que vai ao celular dele. A chave do aviso, com o trabalhador, é a chave da
/// linha — o mesmo fato não vira dois avisos (ADR-0019).
/// </summary>
internal sealed class RegistroDeAviso
{
    public Guid TrabalhadorId { get; set; }

    public string Chave { get; set; } = string.Empty;

    public string Tipo { get; set; } = string.Empty;

    public string Titulo { get; set; } = string.Empty;

    public string Texto { get; set; } = string.Empty;

    public DateTimeOffset CriadoEm { get; set; }

    /// <summary>Quando ele abriu os recados depois do aviso; nulo, enquanto não abriu.</summary>
    public DateTimeOffset? LidoEm { get; set; }

    /// <summary>Quando o aviso foi entregue aos celulares dele — ou dado por entregue; nulo, enquanto não foi.</summary>
    public DateTimeOffset? EntregueEm { get; set; }

    /// <summary>
    /// Quando um servidor pegou o aviso para entregar. Outro só pega o mesmo
    /// aviso depois que a reserva vence: dois servidores não entregam duas vezes.
    /// </summary>
    public DateTimeOffset? ReservadoEm { get; set; }

    public int Tentativas { get; set; }
}

/// <summary>
/// Um celular onde o trabalhador aceitou receber os avisos: o endereço do
/// serviço de push do navegador dele, e as duas chaves com que a mensagem é
/// cifrada para só aquele navegador ler (RFC 8291).
/// </summary>
internal sealed class RegistroDeInscricaoDePush
{
    public string Endereco { get; set; } = string.Empty;

    public Guid TrabalhadorId { get; set; }

    /// <summary>A chave pública do navegador (p256dh), em base64url.</summary>
    public string ChavePublica { get; set; } = string.Empty;

    /// <summary>O segredo de autenticação do navegador (auth), em base64url.</summary>
    public string Segredo { get; set; } = string.Empty;

    public DateTimeOffset CriadaEm { get; set; }
}
