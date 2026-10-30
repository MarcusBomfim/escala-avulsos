using EscalaAvulsos.Dominio.Avisos;
using EscalaAvulsos.Dominio.Testes.Escalacoes;
using EscalaAvulsos.Infraestrutura.Persistencia.Repositorios;
using Npgsql;
using static EscalaAvulsos.Dominio.Testes.Escalacoes.EscalacoesDeTeste;

namespace EscalaAvulsos.Infraestrutura.Testes.Persistencia;

/*
 * Os avisos no banco: os que a escala deixa, na transação dela, e os que o
 * servidor reserva para entregar aos celulares. Na tarde de quinta sem
 * rodízio anterior, o Antônio fica com a vaga de sinaleiro, o Edson e o
 * Benedito com as de estivador; o Carlos, o Dirceu e o João ficam de fora.
 */
public sealed class AvisosNoBancoTests(BancoDeTestes bancos) : TestesComBanco(bancos)
{
    private readonly TardeDeQuinta _tarde = new();

    private readonly RelogioParado _relogio = new(Em(5, 10));

    [Fact]
    public async Task A_escala_gravada_deixa_um_aviso_para_cada_habilitado_e_o_substituto_ganha_o_dele()
    {
        await GravarATarde();
        var escala = await Banco.FazerAEscala(_tarde.Escalacao.Id, Em(5, 10));

        await using (var sessao = Banco.NovaSessao())
        {
            var avisos = Avisos(sessao);
            var doAntonio = Assert.Single(await avisos.DoTrabalhador(_tarde.Antonio.Id, 10, Cancelamento));

            Assert.Equal((TipoDoAviso.VaiTrabalhar, $"escala:{escala.Id}", null), (doAntonio.Aviso.Tipo, doAntonio.Aviso.Chave, doAntonio.LidoEm));
            Assert.Equal(
                "Quinta-feira, 5 de novembro, das 13h às 19h: sinaleiro no terno 1 do navio Atlântico Sul, no Terminal Cais Leste, berço 4.",
                doAntonio.Aviso.Texto);
            Assert.Equal(TipoDoAviso.FicouDeFora, Assert.Single(await avisos.DoTrabalhador(_tarde.Carlos.Id, 10, Cancelamento)).Aviso.Tipo);
        }

        // O Edson desiste: a vaga passa para o primeiro da espera, que ganha o aviso — e só ele.
        var substituto = await Banco.Desistir(escala.Id, _tarde.Edson.Id, Em(5, 11));

        await using (var sessao = Banco.NovaSessao())
        {
            var avisos = Avisos(sessao);
            var doSubstituto = await avisos.DoTrabalhador(substituto!.Trabalhador, 10, Cancelamento);

            Assert.Equal([TipoDoAviso.FicouDeFora, TipoDoAviso.EntrouPelaEspera], doSubstituto.Select(guardado => guardado.Aviso.Tipo).Order());
            Assert.Single(await avisos.DoTrabalhador(_tarde.Edson.Id, 10, Cancelamento));
            Assert.Equal(7, await Banco.ContarComoDono("select count(*) from escala.avisos"));
        }
    }

    [Fact]
    public async Task Os_avisos_do_trabalhador_vem_do_mais_novo_para_o_mais_antigo()
    {
        await GravarATarde();

        await using var sessao = Banco.NovaSessao();
        var avisos = Avisos(sessao);

        // O da escala, das 10h, é guardado antes do aviso do exame, das 9h: a ordem é a de quando nasceram, e não a de quando entraram.
        await avisos.Guardar(AvisosDaEscala.DaEscalaQueSaiu(_tarde.Escalar()), Cancelamento);
        await avisos.Guardar(AvisosDosPrazos.DosVencimentos(_tarde.Antonio, Em(5, 9)), Cancelamento);

        Assert.Equal(
            [TipoDoAviso.VaiTrabalhar, TipoDoAviso.ExameVaiVencer],
            (await avisos.DoTrabalhador(_tarde.Antonio.Id, 10, Cancelamento)).Select(guardado => guardado.Aviso.Tipo));
    }

    [Fact]
    public async Task O_mesmo_aviso_nao_entra_duas_vezes_e_os_lidos_ficam_lidos()
    {
        await GravarATarde();

        await using var sessao = Banco.NovaSessao();
        var avisos = Avisos(sessao);
        var vencimentos = AvisosDosPrazos.DosVencimentos(_tarde.Antonio, Em(5, 9));

        Assert.Equal(1, await avisos.Guardar(vencimentos, Cancelamento));
        Assert.Equal(0, await avisos.Guardar(AvisosDosPrazos.DosVencimentos(_tarde.Antonio, Em(5, 10)), Cancelamento));
        Assert.Equal(1, await avisos.NaoLidos(_tarde.Antonio.Id, Cancelamento));

        Assert.Equal(1, await avisos.MarcarLidos(_tarde.Antonio.Id, Cancelamento));

        Assert.Equal(0, await avisos.NaoLidos(_tarde.Antonio.Id, Cancelamento));
        Assert.Equal(Em(5, 10), Assert.Single(await avisos.DoTrabalhador(_tarde.Antonio.Id, 10, Cancelamento)).LidoEm);
        Assert.Equal(0, await avisos.MarcarLidos(_tarde.Antonio.Id, Cancelamento));
    }

    [Fact]
    public async Task O_aviso_reservado_por_um_servidor_so_vai_para_outro_depois_que_a_reserva_vence()
    {
        await GravarATarde();
        await Banco.FazerAEscala(_tarde.Escalacao.Id, Em(5, 10));
        var reserva = TimeSpan.FromMinutes(2);

        await using var uma = Banco.NovaSessao();
        await using var outra = Banco.NovaSessao();

        var primeiros = await Avisos(uma).Reservar(4, reserva, Cancelamento);
        var restantes = await Avisos(outra).Reservar(10, reserva, Cancelamento);

        Assert.Equal(4, primeiros.Count);
        Assert.Equal(2, restantes.Count);
        Assert.Empty(primeiros.Select(reservado => reservado.Aviso.Trabalhador).Intersect(restantes.Select(reservado => reservado.Aviso.Trabalhador)));
        Assert.All(primeiros, reservado => Assert.Equal(1, reservado.Tentativas));

        // Um dos primeiros foi entregue; os outros três, o servidor que caiu não entregou.
        await Avisos(uma).Entregue(primeiros[0].Aviso, Cancelamento);
        Assert.Empty(await Avisos(outra).Reservar(10, reserva, Cancelamento));

        _relogio.Andar(reserva + TimeSpan.FromSeconds(1));
        var deNovo = await Avisos(outra).Reservar(10, reserva, Cancelamento);

        Assert.Equal(5, deNovo.Count);
        Assert.DoesNotContain(primeiros[0].Aviso.Trabalhador, deNovo.Select(reservado => reservado.Aviso.Trabalhador));
        Assert.All(deNovo, reservado => Assert.Equal(2, reservado.Tentativas));
    }

    [Fact]
    public async Task Depois_de_cinco_tentativas_o_aviso_fica_so_no_aplicativo()
    {
        await GravarATarde();

        await using var sessao = Banco.NovaSessao();
        var avisos = Avisos(sessao);
        await avisos.Guardar(AvisosDosPrazos.DosVencimentos(_tarde.Antonio, Em(5, 9)), Cancelamento);

        for (var tentativa = 1; tentativa <= RepositorioDeAvisos.TentativasDeEntrega; tentativa++)
        {
            Assert.Equal(tentativa, Assert.Single(await avisos.Reservar(10, TimeSpan.Zero, Cancelamento)).Tentativas);
            _relogio.Andar(TimeSpan.FromSeconds(1));
        }

        Assert.Empty(await avisos.Reservar(10, TimeSpan.Zero, Cancelamento));
        Assert.Single(await avisos.DoTrabalhador(_tarde.Antonio.Id, 10, Cancelamento));
    }

    private RepositorioDeAvisos Avisos(Sessao sessao) => new(sessao.Contexto, _relogio);

    private Task GravarATarde() => Banco.Gravar(_tarde.Trabalhadores, [_tarde.Requisicao], _tarde.Escalacao);
}

/// <summary>Um relógio que só anda quando o teste manda.</summary>
internal sealed class RelogioParado(DateTimeOffset agora) : TimeProvider
{
    private DateTimeOffset _agora = agora;

    public void Andar(TimeSpan quanto) => _agora += quanto;

    public override DateTimeOffset GetUtcNow() => _agora.ToUniversalTime();
}

internal static class ConsultasComoDono
{
    /// <summary>Um número, como o dono do banco o vê — o que nenhum repositório mostra.</summary>
    public static async Task<long> ContarComoDono(this BancoDoTeste banco, string sql)
    {
        await using var conexao = new NpgsqlConnection(banco.ConexaoDoDono);
        await conexao.OpenAsync(TestContext.Current.CancellationToken);
        await using var comando = new NpgsqlCommand(sql, conexao);

        return Convert.ToInt64(await comando.ExecuteScalarAsync(TestContext.Current.CancellationToken), System.Globalization.CultureInfo.InvariantCulture);
    }
}
