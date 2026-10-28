using EscalaAvulsos.Dominio.Comum;
using EscalaAvulsos.Dominio.Escalacoes;
using EscalaAvulsos.Dominio.Identificacao;
using EscalaAvulsos.Dominio.Testes.Trabalhadores;
using EscalaAvulsos.Dominio.Trabalhadores;

namespace EscalaAvulsos.Dominio.Testes.Escalacoes;

public sealed class RodizioTests
{
    private static readonly Matricula[] Habilitados = [.. new[] { 4188, 4101, 4152, 4127, 4130 }.Select(Matricula.De)];

    [Fact]
    public void O_rodizio_novo_comeca_pela_menor_matricula()
    {
        var ordem = Rodizio.Novo.NaOrdemDaVez(Funcoes.Estivador, Inscricao.Registrado, Habilitados);

        Assert.Equal([4101, 4127, 4130, 4152, 4188], ordem.Select(matricula => matricula.Numero));
        Assert.Null(Rodizio.Novo.UltimoEscalado(Funcoes.Estivador, Inscricao.Registrado));
        Assert.Empty(Rodizio.Novo.Vezes);
    }

    [Fact]
    public void Continua_depois_do_ultimo_escalado_e_da_a_volta()
    {
        var rodizio = Rodizio.Novo.ComAVezEm(Funcoes.Estivador, Inscricao.Registrado, Matricula.De(4127));

        var ordem = rodizio.NaOrdemDaVez(Funcoes.Estivador, Inscricao.Registrado, Habilitados);

        Assert.Equal([4130, 4152, 4188, 4101, 4127], ordem.Select(matricula => matricula.Numero));
    }

    [Fact]
    public void O_ultimo_escalado_nao_precisa_estar_entre_os_habilitados()
    {
        var parouNa4140 = Rodizio.Novo.ComAVezEm(Funcoes.Estivador, Inscricao.Registrado, Matricula.De(4140));
        var parouDepoisDeTodos = Rodizio.Novo.ComAVezEm(Funcoes.Estivador, Inscricao.Registrado, Matricula.De(9000));

        Assert.Equal([4152, 4188, 4101, 4127, 4130], parouNa4140.NaOrdemDaVez(Funcoes.Estivador, Inscricao.Registrado, Habilitados).Select(matricula => matricula.Numero));
        Assert.Equal([4101, 4127, 4130, 4152, 4188], parouDepoisDeTodos.NaOrdemDaVez(Funcoes.Estivador, Inscricao.Registrado, Habilitados).Select(matricula => matricula.Numero));
    }

    [Fact]
    public void Matricula_repetida_conta_uma_vez_e_ninguem_habilitado_e_ninguem_na_vez()
    {
        Matricula[] repetidas = [Matricula.De(4130), Matricula.De("04130"), Matricula.De(4101)];

        Assert.Equal([4101, 4130], Rodizio.Novo.NaOrdemDaVez(Funcoes.Estivador, Inscricao.Registrado, repetidas).Select(matricula => matricula.Numero));
        Assert.Empty(Rodizio.Novo.NaOrdemDaVez(Funcoes.Estivador, Inscricao.Registrado, []));
    }

    [Fact]
    public void Cada_funcao_e_cada_inscricao_tem_a_sua_vez()
    {
        var rodizio = Rodizio.Novo
            .ComAVezEm(Funcoes.Estivador, Inscricao.Registrado, Matricula.De(4130))
            .ComAVezEm(Funcoes.Estivador, Inscricao.Cadastrado, Matricula.De(5310))
            .ComAVezEm(Funcoes.Sinaleiro, Inscricao.Registrado, Matricula.De(4152));

        Assert.Equal(4130, rodizio.UltimoEscalado(Funcoes.Estivador, Inscricao.Registrado)?.Numero);
        Assert.Equal(5310, rodizio.UltimoEscalado(Funcoes.Estivador, Inscricao.Cadastrado)?.Numero);
        Assert.Equal(4152, rodizio.UltimoEscalado(new Funcao(Categoria.Estiva, "SINALEIRO"), Inscricao.Registrado)?.Numero);
        Assert.Null(rodizio.UltimoEscalado(Funcoes.Sinaleiro, Inscricao.Cadastrado));
        Assert.Null(rodizio.UltimoEscalado(Funcoes.OperadorDeGuindaste, Inscricao.Registrado));
    }

    [Fact]
    public void Andar_a_vez_devolve_outro_rodizio_e_o_de_antes_fica_como_estava()
    {
        var antes = Rodizio.Novo.ComAVezEm(Funcoes.Estivador, Inscricao.Registrado, Matricula.De(4127));

        var depois = antes.ComAVezEm(Funcoes.Estivador, Inscricao.Registrado, Matricula.De(4152));

        Assert.Equal(4127, antes.UltimoEscalado(Funcoes.Estivador, Inscricao.Registrado)?.Numero);
        Assert.Equal(4152, depois.UltimoEscalado(Funcoes.Estivador, Inscricao.Registrado)?.Numero);
        Assert.Empty(Rodizio.Novo.Vezes);
    }

    [Fact]
    public void Remonta_com_as_vezes_gravadas()
    {
        var rodizio = Rodizio.Novo
            .ComAVezEm(Funcoes.Sinaleiro, Inscricao.Registrado, Matricula.De(4152))
            .ComAVezEm(Funcoes.Estivador, Inscricao.Cadastrado, Matricula.De(5310))
            .ComAVezEm(Funcoes.Estivador, Inscricao.Registrado, Matricula.De(4130));

        var remontado = Rodizio.De(rodizio.Vezes);

        Assert.Equal(
            [
                new VezNoRodizio(Funcoes.Estivador, Inscricao.Cadastrado, Matricula.De(5310)),
                new VezNoRodizio(Funcoes.Estivador, Inscricao.Registrado, Matricula.De(4130)),
                new VezNoRodizio(Funcoes.Sinaleiro, Inscricao.Registrado, Matricula.De(4152)),
            ],
            remontado.Vezes);
    }

    [Fact]
    public void Duas_vezes_para_a_mesma_roda_sao_recusadas()
    {
        var erro = Assert.Throws<ExcecaoDeDominio>(() => Rodizio.De(
        [
            new VezNoRodizio(Funcoes.Estivador, Inscricao.Registrado, Matricula.De(4127)),
            new VezNoRodizio(new Funcao(Categoria.Estiva, "estivador"), Inscricao.Registrado, Matricula.De(4130)),
        ]));

        Assert.Equal("O rodízio de estivador tem duas vezes para os registrados.", erro.Message);
    }

    [Fact]
    public void A_inscricao_tem_de_existir()
    {
        var erro = Assert.Throws<ExcecaoDeDominio>(() => Rodizio.Novo.ComAVezEm(Funcoes.Estivador, (Inscricao)7, Matricula.De(4127)));

        Assert.Equal("A inscrição 7 não existe: o trabalhador é cadastrado ou registrado.", erro.Message);
    }

    [Fact]
    public void Girando_sempre_a_vez_de_todos_chega_e_ninguem_fica_mais_de_uma_vez_na_frente()
    {
        /*
         * Sete habilitados de sempre, três vagas por escala, setenta escalas:
         * depois de cada uma, quem trabalhou mais tem no máximo uma escala a mais
         * que quem trabalhou menos — e a cada sete escalas, todos trabalharam três.
         */
        int[] numeros = [102, 7, 4127, 55, 9_999_999, 300, 1];
        Matricula[] sete = [.. numeros.Select(Matricula.De)];
        var vezes = sete.ToDictionary(matricula => matricula, _ => 0);
        var rodizio = Rodizio.Novo;

        for (var escala = 1; escala <= 70; escala++)
        {
            var escalados = rodizio.NaOrdemDaVez(Funcoes.Estivador, Inscricao.Registrado, sete).Take(3).ToList();

            escalados.ForEach(matricula => vezes[matricula]++);
            rodizio = rodizio.ComAVezEm(Funcoes.Estivador, Inscricao.Registrado, escalados[^1]);

            Assert.True(vezes.Values.Max() - vezes.Values.Min() <= 1, $"Depois da escala {escala}: {string.Join(", ", vezes.Values)}.");

            if (escala % 7 == 0)
            {
                Assert.All(vezes.Values, quantas => Assert.Equal(3 * escala / 7, quantas));
            }
        }
    }
}
