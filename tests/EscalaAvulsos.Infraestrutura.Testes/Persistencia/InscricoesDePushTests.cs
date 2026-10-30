using EscalaAvulsos.Dominio.Testes.Escalacoes;
using EscalaAvulsos.Infraestrutura.Avisos;
using EscalaAvulsos.Infraestrutura.Persistencia.Repositorios;

namespace EscalaAvulsos.Infraestrutura.Testes.Persistencia;

public sealed class InscricoesDePushTests(BancoDeTestes bancos) : TestesComBanco(bancos)
{
    // As chaves do navegador do exemplo da RFC 8291.
    private const string ChavePublica = "BCVxsr7N_eNgVRqvHtD0zTZsEc6-VV-JvLexhqUzORcxaOzi6-AYWXvTBHm4bjyPjs7Vd8pZGH6SRpkNtoIAiw4";

    private const string Segredo = "BTBZMqHH6r4Tts7J_aSIgg";

    private readonly TardeDeQuinta _tarde = new();

    [Fact]
    public async Task O_navegador_inscrito_fica_com_quem_entrou_por_ultimo_e_sai_quando_pedem()
    {
        await Banco.Gravar(_tarde.Trabalhadores, []);
        var celular = InscricaoDePush.De("https://push.example.net/push/JzLQ3raZ", ChavePublica, Segredo);
        var outro = InscricaoDePush.De("https://push.example.net/push/outro", ChavePublica, Segredo);

        await using var sessao = Banco.NovaSessao();
        var inscricoes = new RepositorioDeInscricoesDePush(sessao.Contexto, TimeProvider.System);

        await inscricoes.Guardar(_tarde.Antonio.Id, celular, Cancelamento);
        await inscricoes.Guardar(_tarde.Antonio.Id, outro, Cancelamento);
        Assert.Equal([celular.Endereco, outro.Endereco], (await inscricoes.DoTrabalhador(_tarde.Antonio.Id, Cancelamento)).Select(inscricao => inscricao.Endereco));

        // O mesmo celular, com a matrícula do Carlos: os avisos dele passam a ir para lá, e os do Antônio, não.
        await inscricoes.Guardar(_tarde.Carlos.Id, celular, Cancelamento);
        Assert.Equal([outro.Endereco], (await inscricoes.DoTrabalhador(_tarde.Antonio.Id, Cancelamento)).Select(inscricao => inscricao.Endereco));
        Assert.Equal(ChavePublica, Assert.Single(await inscricoes.DoTrabalhador(_tarde.Carlos.Id, Cancelamento)).ChavePublicaEmBase64Url);

        // Só o dono tira a inscrição dele; o serviço de push que a deu por vencida tira de qualquer um.
        Assert.Equal(0, await inscricoes.Remover(celular.Endereco, _tarde.Antonio.Id, Cancelamento));
        Assert.Equal(1, await inscricoes.Remover(celular.Endereco, _tarde.Carlos.Id, Cancelamento));
        Assert.Equal(1, await inscricoes.Remover(outro.Endereco, cancelamento: Cancelamento));
        Assert.Empty(await inscricoes.DoTrabalhador(_tarde.Antonio.Id, Cancelamento));
    }

    [Theory]
    [InlineData("http://push.example.net/x", ChavePublica, Segredo, "O endereço da inscrição de push tem de ser um endereço HTTPS.")]
    [InlineData("https://push.example.net/x", "BCVx", Segredo, "A chave pública (p256dh) tem de ser um ponto P-256 sem compressão, de 65 bytes.")]
    [InlineData("https://push.example.net/x", ChavePublica, "BTBZ", "O segredo (auth) tem de ter 16 bytes.")]
    [InlineData("https://push.example.net/x", ChavePublica, "não é base64!", "O segredo (auth) não está em base64url.")]
    public void A_inscricao_que_nao_e_de_push_e_recusada(string endereco, string chave, string segredo, string mensagem)
    {
        Assert.Equal(mensagem, Assert.Throws<FormatException>(() => InscricaoDePush.De(endereco, chave, segredo)).Message);
    }
}
