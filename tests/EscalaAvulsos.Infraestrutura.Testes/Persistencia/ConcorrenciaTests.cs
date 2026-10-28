using EscalaAvulsos.Dominio.Escalacoes;
using EscalaAvulsos.Dominio.Identificacao;
using EscalaAvulsos.Dominio.Testes.Trabalhadores;
using EscalaAvulsos.Dominio.Trabalhadores;
using EscalaAvulsos.Infraestrutura.Persistencia;
using static EscalaAvulsos.Dominio.Testes.Escalacoes.EscalacoesDeTeste;

namespace EscalaAvulsos.Infraestrutura.Testes.Persistencia;

/*
 * Ninguém grava por cima do que não viu (ADR-0012): atualizar pede a versão
 * lida, e o banco confere e grava num comando só. Cada gravação aqui tem a
 * sua sessão — a sua conexão —, como dois pedidos que chegam juntos.
 */
public sealed class ConcorrenciaTests(BancoDeTestes bancos) : TestesComBanco(bancos)
{
    [Fact]
    public async Task Quem_grava_a_partir_de_uma_versao_velha_e_recusado()
    {
        var antonio = TrabalhadoresDeTeste.Estivador();
        await using (var cadastro = Banco.NovaSessao())
        {
            await cadastro.Trabalhadores.Adicionar(antonio, Cancelamento);
        }

        await using var daAna = Banco.NovaSessao();
        await using var doBruno = Banco.NovaSessao();
        var lidoPelaAna = (await daAna.Trabalhadores.Obter(antonio.Id, Cancelamento))!;
        var lidoPeloBruno = (await doBruno.Trabalhadores.Obter(antonio.Id, Cancelamento))!;

        lidoPelaAna.Agregado.Qualificar(Funcoes.Sinaleiro, new DateOnly(2010, 5, 10));
        await daAna.Trabalhadores.Atualizar(lidoPelaAna.Agregado, lidoPelaAna.Versao, Cancelamento);
        lidoPeloBruno.Agregado.RegistrarAso(new Aso(new DateOnly(2026, 11, 20), new DateOnly(2027, 11, 19)));
        var erro = await Assert.ThrowsAsync<ConflitoDeConcorrencia>(() =>
            doBruno.Trabalhadores.Atualizar(lidoPeloBruno.Agregado, lidoPeloBruno.Versao, Cancelamento));

        Assert.Equal(
            $"O trabalhador {antonio.Id} mudou desde a versão 1: está na versão 2. Leia de novo e refaça a alteração.",
            erro.Message);
        Assert.Equal(2, erro.VersaoAtual);
        Assert.Equal(2, (await daAna.Trabalhadores.Obter(antonio.Id, Cancelamento))?.Agregado.Qualificacoes.Count);
    }

    [Fact]
    public async Task Das_dez_gravacoes_ao_mesmo_tempo_a_partir_da_mesma_versao_so_uma_passa()
    {
        var escalacao = DaEstiva();
        await using (var abertura = Banco.NovaSessao())
        {
            await abertura.Escalacoes.Adicionar(escalacao, Cancelamento);
        }

        var gravacoes = Estivadores(10).Select(async trabalhador =>
        {
            await using var sessao = Banco.NovaSessao();
            var lida = (await sessao.Escalacoes.Obter(escalacao.Id, Cancelamento))!;
            lida.Agregado.Habilitar(trabalhador, [Funcoes.Estivador], QuintaAs6h);

            try
            {
                await sessao.Escalacoes.Atualizar(lida.Agregado, 1, Cancelamento);

                return true;
            }
            catch (ConflitoDeConcorrencia)
            {
                return false;
            }
        });

        var resultados = await Task.WhenAll(gravacoes);

        await using var conferencia = Banco.NovaSessao();
        Assert.Single(resultados, passou => passou);
        Assert.Single((await conferencia.Escalacoes.Obter(escalacao.Id, Cancelamento))!.Agregado.Habilitacoes);
    }

    [Fact]
    public async Task Na_abertura_da_janela_todos_se_habilitam_e_nenhuma_habilitacao_se_perde()
    {
        /*
         * Vinte trabalhadores se habilitam no mesmo instante. Quem perde a
         * corrida lê de novo e refaz, como o aplicativo vai fazer: no fim,
         * estão os vinte, e a escalação andou uma versão por habilitação. A
         * ordem em que chegaram não importa — a vez é pela matrícula.
         */
        var escalacao = DaEstiva();
        await using (var abertura = Banco.NovaSessao())
        {
            await abertura.Escalacoes.Adicionar(escalacao, Cancelamento);
        }

        var estivadores = Estivadores(20);

        await Task.WhenAll(estivadores.Select(async trabalhador =>
        {
            while (true)
            {
                await using var sessao = Banco.NovaSessao();
                var lida = (await sessao.Escalacoes.Obter(escalacao.Id, Cancelamento))!;
                lida.Agregado.Habilitar(trabalhador, [Funcoes.Estivador], QuintaAs6h);

                try
                {
                    await sessao.Escalacoes.Atualizar(lida.Agregado, lida.Versao, Cancelamento);

                    return;
                }
                catch (ConflitoDeConcorrencia)
                {
                    // Outro gravou no meio: lê de novo e refaz.
                }
            }
        }));

        await using var conferencia = Banco.NovaSessao();
        var final = (await conferencia.Escalacoes.Obter(escalacao.Id, Cancelamento))!;
        Assert.Equal(21, final.Versao);
        Assert.Equal(estivadores.Select(trabalhador => trabalhador.Matricula), final.Agregado.Habilitacoes.Select(habilitacao => habilitacao.Matricula));
    }

    /// <summary>Estivadores de matrículas seguidas, a partir da 5001.</summary>
    private static List<Trabalhador> Estivadores(int quantos) =>
        [.. Enumerable.Range(5001, quantos).Select(matricula => TrabalhadoresDeTeste.Estivador(matricula, $"Estivador {matricula}"))];
}
