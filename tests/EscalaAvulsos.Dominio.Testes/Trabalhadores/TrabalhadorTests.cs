using EscalaAvulsos.Dominio.Comum;
using EscalaAvulsos.Dominio.Identificacao;
using EscalaAvulsos.Dominio.Trabalhadores;

namespace EscalaAvulsos.Dominio.Testes.Trabalhadores;

public sealed class TrabalhadorTests
{
    [Fact]
    public void Nasce_com_a_matricula_o_nome_a_categoria_e_a_inscricao()
    {
        var trabalhador = new Trabalhador(new DadosDoTrabalhador(
            Matricula.De("004127"), "  Antônio Ferreira ", Cpf.De("529.982.247-25"), Categoria.Estiva, Inscricao.Cadastrado));

        Assert.Equal("Antônio Ferreira (matrícula 4127)", trabalhador.ToString());
        Assert.Equal((Categoria.Estiva, Inscricao.Cadastrado), (trabalhador.Categoria, trabalhador.Inscricao));
        Assert.Equal(Cpf.De("52998224725"), trabalhador.Cpf);
        Assert.Empty(trabalhador.Qualificacoes);
        Assert.Empty(trabalhador.Impedimentos);
        Assert.Null(trabalhador.Aso);
        Assert.Null(trabalhador.Cancelamento);
    }

    [Fact]
    public void Cada_trabalhador_tem_a_sua_identidade()
    {
        Assert.NotEqual(TrabalhadoresDeTeste.Estivador().Id, TrabalhadoresDeTeste.Estivador().Id);
    }

    [Fact]
    public void A_identidade_nao_pode_ser_vazia()
    {
        var erro = Assert.Throws<ExcecaoDeDominio>(() => new TrabalhadorId(Guid.Empty));

        Assert.Equal("O identificador não pode ser vazio.", erro.Message);

        var valor = Guid.CreateVersion7();

        Assert.Equal((valor, valor.ToString()), (new TrabalhadorId(valor).Valor, new TrabalhadorId(valor).ToString()));
    }

    [Fact]
    public void Diz_tudo_o_que_esta_errado_de_uma_vez()
    {
        var erro = Assert.Throws<ExcecaoDeDominio>(() => new Trabalhador(new DadosDoTrabalhador(
            Matricula.De(1), " ", Cpf.De("529.982.247-25"), (Categoria)9, (Inscricao)7)));

        Assert.Equal(
            "Falta o nome do trabalhador. A categoria 9 não existe. A inscrição 7 não existe: o trabalhador é cadastrado ou registrado.",
            erro.Message);
    }

    [Fact]
    public void O_nome_tem_no_maximo_cento_e_vinte_caracteres()
    {
        var erro = Assert.Throws<ExcecaoDeDominio>(() => TrabalhadoresDeTeste.Estivador(nome: new string('a', 121)));

        Assert.Equal("O nome do trabalhador passa de 120 caracteres.", erro.Message);
    }

    [Fact]
    public void Se_qualifica_em_funcoes_da_propria_categoria()
    {
        var antonio = TrabalhadoresDeTeste.Estivador();

        antonio.Qualificar(Funcoes.Sinaleiro, new DateOnly(2010, 5, 10), new DateOnly(2027, 5, 9));

        Assert.Equal([Funcoes.Estivador, Funcoes.Sinaleiro], antonio.Qualificacoes.Select(qualificacao => qualificacao.Funcao));

        var erro = Assert.Throws<ExcecaoDeDominio>(() => antonio.Qualificar(Funcoes.Conferente, new DateOnly(2020, 1, 1)));

        Assert.Equal(
            "Conferente é função da conferência de carga, e Antônio Ferreira é da estiva: a qualificação é numa função da categoria do trabalhador.",
            erro.Message);
    }

    [Fact]
    public void A_reciclagem_renova_a_qualificacao_que_ja_existia()
    {
        var antonio = TrabalhadoresDeTeste.Estivador();
        antonio.Qualificar(Funcoes.OperadorDeGuindaste, new DateOnly(2021, 7, 1), new DateOnly(2026, 6, 30));

        antonio.Qualificar(new Funcao(Categoria.Estiva, "operador de guindaste de bordo"), new DateOnly(2026, 7, 15), new DateOnly(2031, 7, 14));

        var guindaste = Assert.Single(antonio.Qualificacoes, qualificacao => qualificacao.Funcao == Funcoes.OperadorDeGuindaste);
        Assert.Equal((new DateOnly(2026, 7, 15), new DateOnly(2031, 7, 14)), (guindaste.Desde, guindaste.ValidaAte!.Value));
        Assert.Equal(2, antonio.Qualificacoes.Count);
    }

    [Fact]
    public void A_qualificacao_nao_vence_antes_de_comecar()
    {
        var antonio = TrabalhadoresDeTeste.Estivador();

        var erro = Assert.Throws<ExcecaoDeDominio>(() => antonio.Qualificar(Funcoes.Sinaleiro, new DateOnly(2010, 5, 10), new DateOnly(2010, 5, 9)));

        Assert.Equal("A qualificação de sinaleiro não pode valer só até 09/05/2010, antes de começar, em 10/05/2010.", erro.Message);

        antonio.Qualificar(Funcoes.Sinaleiro, new DateOnly(2010, 5, 10), new DateOnly(2010, 5, 10));
    }

    [Fact]
    public void O_exame_medico_so_anda_para_a_frente()
    {
        var antonio = TrabalhadoresDeTeste.Estivador();
        var novo = new Aso(new DateOnly(2026, 11, 20), new DateOnly(2027, 11, 19));

        antonio.RegistrarAso(novo);

        Assert.Equal(novo, antonio.Aso);

        var erro = Assert.Throws<ExcecaoDeDominio>(() => antonio.RegistrarAso(new Aso(new DateOnly(2025, 12, 1), new DateOnly(2026, 11, 30))));

        Assert.Equal("O exame médico de 01/12/2025 é mais antigo que o registrado, de 20/11/2026.", erro.Message);

        // O mesmo exame, com a validade corrigida, substitui o registrado.
        antonio.RegistrarAso(new Aso(new DateOnly(2026, 11, 20), new DateOnly(2027, 5, 19)));

        Assert.Equal(new DateOnly(2027, 5, 19), antonio.Aso!.ValidoAte);
    }

    [Fact]
    public void O_exame_nao_vence_antes_de_ser_emitido()
    {
        var erro = Assert.Throws<ExcecaoDeDominio>(() => new Aso(new DateOnly(2026, 11, 20), new DateOnly(2026, 11, 19)));

        Assert.Equal("O exame médico emitido em 20/11/2026 não pode valer só até 19/11/2026, antes de ser emitido.", erro.Message);
    }

    [Fact]
    public void O_cadastrado_passa_ao_registro_uma_vez()
    {
        var joao = TrabalhadoresDeTeste.Estivador(5310, "João Batista", Inscricao.Cadastrado);

        joao.Registrar();

        Assert.Equal(Inscricao.Registrado, joao.Inscricao);

        var erro = Assert.Throws<ExcecaoDeDominio>(joao.Registrar);

        Assert.Equal("João Batista (matrícula 5310) já está no registro.", erro.Message);
    }

    [Fact]
    public void A_inscricao_cancelada_congela_o_trabalhador()
    {
        var antonio = TrabalhadoresDeTeste.Estivador();
        antonio.Impedir(new Impedimento(TipoDeImpedimento.Afastamento, new DateOnly(2027, 1, 10)));

        antonio.CancelarInscricao(new DateOnly(2027, 3, 1), " Aposentadoria ");

        Assert.Equal((new DateOnly(2027, 3, 1), "Aposentadoria"), (antonio.Cancelamento!.APartirDe, antonio.Cancelamento.Motivo));

        Action[] mudancas =
        [
            () => antonio.Qualificar(Funcoes.Sinaleiro, new DateOnly(2027, 2, 1)),
            () => antonio.RegistrarAso(new Aso(new DateOnly(2027, 2, 1), new DateOnly(2028, 1, 31))),
            () => antonio.Impedir(new Impedimento(TipoDeImpedimento.Ferias, new DateOnly(2027, 2, 1), new DateOnly(2027, 2, 28))),
            () => antonio.EncerrarAfastamento(new DateOnly(2027, 2, 1)),
            antonio.Registrar,
            () => antonio.CancelarInscricao(new DateOnly(2027, 4, 1), "Outro motivo"),
        ];

        Assert.All(mudancas, mudanca => Assert.Equal(
            "A inscrição de Antônio Ferreira (matrícula 4127) foi cancelada a partir de 01/03/2027: nada mais muda nela.",
            Assert.Throws<ExcecaoDeDominio>(mudanca).Message));
    }

    [Theory]
    [InlineData(null, "Falta o motivo do cancelamento.")]
    [InlineData("  ", "Falta o motivo do cancelamento.")]
    public void O_cancelamento_precisa_de_motivo(string? motivo, string mensagem)
    {
        var antonio = TrabalhadoresDeTeste.Estivador();

        var erro = Assert.Throws<ExcecaoDeDominio>(() => antonio.CancelarInscricao(new DateOnly(2027, 3, 1), motivo!));

        Assert.Equal(mensagem, erro.Message);
        Assert.Null(antonio.Cancelamento);
    }

    [Fact]
    public void O_motivo_do_cancelamento_tem_no_maximo_duzentos_caracteres()
    {
        var erro = Assert.Throws<ExcecaoDeDominio>(() => TrabalhadoresDeTeste.Estivador().CancelarInscricao(new DateOnly(2027, 3, 1), new string('a', 201)));

        Assert.Equal("O motivo do cancelamento passa de 200 caracteres.", erro.Message);
    }
}
