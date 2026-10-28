using EscalaAvulsos.Dominio.Identificacao;
using EscalaAvulsos.Dominio.Periodos;
using EscalaAvulsos.Dominio.Testes.Trabalhadores;
using EscalaAvulsos.Dominio.Trabalhadores;
using EscalaAvulsos.Infraestrutura.Persistencia;
using EscalaAvulsos.Infraestrutura.Testes.Documentos;

namespace EscalaAvulsos.Infraestrutura.Testes.Persistencia;

public sealed class RepositorioDeTrabalhadoresTests(BancoDeTestes bancos) : TestesComBanco(bancos)
{
    [Fact]
    public async Task O_trabalhador_gravado_volta_inteiro_na_versao_1()
    {
        var antonio = Exemplos.TrabalhadorCompleto();
        await using (var sessao = Banco.NovaSessao())
        {
            Assert.Equal(1, await sessao.Trabalhadores.Adicionar(antonio, Cancelamento));
        }

        await using var outra = Banco.NovaSessao();
        var lido = await outra.Trabalhadores.Obter(antonio.Id, Cancelamento);

        Assert.Equal(1, lido?.Versao);
        Assert.Equal(Exemplos.Json(antonio), Exemplos.Json(lido!.Agregado));
    }

    [Fact]
    public async Task O_trabalhador_e_achado_pela_matricula()
    {
        await using var sessao = Banco.NovaSessao();
        var antonio = TrabalhadoresDeTeste.Estivador();
        await sessao.Trabalhadores.Adicionar(antonio, Cancelamento);

        Assert.Equal(antonio.Id, (await sessao.Trabalhadores.ObterPelaMatricula(Matricula.De("004127"), Cancelamento))?.Agregado.Id);
        Assert.Null(await sessao.Trabalhadores.ObterPelaMatricula(Matricula.De(4128), Cancelamento));
        Assert.Null(await sessao.Trabalhadores.Obter(TrabalhadorId.Novo(), Cancelamento));
    }

    [Fact]
    public async Task Os_habilitados_vem_na_ordem_das_matriculas_e_os_que_nao_existem_nao_vem()
    {
        await using var sessao = Banco.NovaSessao();
        var benedito = TrabalhadoresDeTeste.Estivador(4130, "Benedito Lima");
        var antonio = TrabalhadoresDeTeste.Estivador();
        await sessao.Trabalhadores.Adicionar(benedito, Cancelamento);
        await sessao.Trabalhadores.Adicionar(antonio, Cancelamento);

        var habilitados = await sessao.Trabalhadores.Obter([benedito.Id, TrabalhadorId.Novo(), antonio.Id, benedito.Id], Cancelamento);

        Assert.Equal(["Antônio Ferreira", "Benedito Lima"], habilitados.Select(trabalhador => trabalhador.Nome));
    }

    [Fact]
    public async Task A_alteracao_grava_a_versao_seguinte()
    {
        await using var sessao = Banco.NovaSessao();
        var antonio = TrabalhadoresDeTeste.Estivador();
        await sessao.Trabalhadores.Adicionar(antonio, Cancelamento);

        var lido = (await sessao.Trabalhadores.Obter(antonio.Id, Cancelamento))!;
        lido.Agregado.Impedir(new Impedimento(TipoDeImpedimento.Suspensao, new DateOnly(2026, 11, 5), new DateOnly(2026, 11, 6)));
        var versao = await sessao.Trabalhadores.Atualizar(lido.Agregado, lido.Versao, Cancelamento);

        var relido = (await sessao.Trabalhadores.Obter(antonio.Id, Cancelamento))!;
        Assert.Equal((2, 2), (versao, relido.Versao));
        Assert.False(relido.Agregado.AptidaoPara(Funcoes.Estivador, Periodo.De(new DateOnly(2026, 11, 5), Turno.Tarde)).Apto);
    }

    [Fact]
    public async Task A_matricula_e_o_cpf_nao_se_repetem()
    {
        await using var sessao = Banco.NovaSessao();
        await sessao.Trabalhadores.Adicionar(TrabalhadoresDeTeste.Estivador(), Cancelamento);

        var mesmaMatricula = await Assert.ThrowsAsync<AgregadoJaExiste>(() =>
            sessao.Trabalhadores.Adicionar(Estivador(4127, "Outro Antônio", "111.444.777-35"), Cancelamento));
        var mesmoCpf = await Assert.ThrowsAsync<AgregadoJaExiste>(() =>
            sessao.Trabalhadores.Adicionar(Estivador(4130, "Benedito Lima", "529.984.127-26"), Cancelamento));

        Assert.Equal("Já existe trabalhador com a matrícula 4127.", mesmaMatricula.Message);
        Assert.Equal("Já existe trabalhador com o CPF 529.984.127-26.", mesmoCpf.Message);
    }

    [Fact]
    public async Task O_mesmo_trabalhador_nao_e_gravado_duas_vezes()
    {
        await using var sessao = Banco.NovaSessao();
        var antonio = TrabalhadoresDeTeste.Estivador();
        await sessao.Trabalhadores.Adicionar(antonio, Cancelamento);

        var erro = await Assert.ThrowsAsync<AgregadoJaExiste>(() => sessao.Trabalhadores.Adicionar(antonio, Cancelamento));

        Assert.Equal($"Já existe o trabalhador {antonio.Id}.", erro.Message);
    }

    [Fact]
    public async Task Alterar_quem_nao_foi_gravado_e_recusado()
    {
        await using var sessao = Banco.NovaSessao();
        var antonio = TrabalhadoresDeTeste.Estivador();

        var erro = await Assert.ThrowsAsync<AgregadoNaoEncontrado>(() => sessao.Trabalhadores.Atualizar(antonio, 1, Cancelamento));

        Assert.Equal($"Não existe o trabalhador {antonio.Id}.", erro.Message);
    }

    private static Trabalhador Estivador(int matricula, string nome, string cpf)
    {
        var trabalhador = new Trabalhador(new DadosDoTrabalhador(Matricula.De(matricula), nome, Cpf.De(cpf), Categoria.Estiva, Inscricao.Registrado));
        trabalhador.Qualificar(Funcoes.Estivador, new DateOnly(1994, 3, 1));

        return trabalhador;
    }
}
