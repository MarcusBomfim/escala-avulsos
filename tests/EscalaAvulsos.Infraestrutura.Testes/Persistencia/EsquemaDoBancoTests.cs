using EscalaAvulsos.Dominio.Testes.Trabalhadores;
using Npgsql;

namespace EscalaAvulsos.Infraestrutura.Testes.Persistencia;

/*
 * O banco por dentro: o que ele garante sozinho, mesmo para quem escreve SQL
 * à mão, e o que o papel da aplicação pode fazer (ADR-0012).
 */
public sealed class EsquemaDoBancoTests(BancoDeTestes bancos) : TestesComBanco(bancos)
{
    [Theory]
    [InlineData("matricula", "0", "ck_trabalhadores_matricula")]
    [InlineData("cpf", "'5299822472'", "ck_trabalhadores_cpf")]
    [InlineData("categoria", "'Pesca'", "ck_trabalhadores_categoria")]
    [InlineData("documento", "'[]'", "ck_trabalhadores_documento")]
    [InlineData("versao", "0", "ck_trabalhadores_versao")]
    [InlineData("formato", "0", "ck_trabalhadores_formato")]
    public async Task O_banco_recusa_o_que_o_dominio_nao_deixaria_mesmo_em_sql_a_mao(string coluna, string valor, string restricao)
    {
        await using (var sessao = Banco.NovaSessao())
        {
            await sessao.Trabalhadores.Adicionar(TrabalhadoresDeTeste.Estivador(), Cancelamento);
        }

        var erro = await Assert.ThrowsAsync<PostgresException>(() => Banco.ExecutarComoDono($"update escala.trabalhadores set {coluna} = {valor}"));

        Assert.Equal((PostgresErrorCodes.CheckViolation, restricao), (erro.SqlState, erro.ConstraintName));
    }

    [Fact]
    public async Task A_vaga_so_aponta_para_escala_trabalhador_e_requisicao_que_existem()
    {
        var erro = await Assert.ThrowsAsync<PostgresException>(() => Banco.ExecutarComoDono(
            """
            insert into escala.engajamentos (escala_id, trabalhador_id, requisicao_id, inicio, fim, descansado_em)
            values (gen_random_uuid(), gen_random_uuid(), gen_random_uuid(), now(), now() + interval '6 hours', now() + interval '17 hours')
            """));

        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, erro.SqlState);
    }

    [Fact]
    public async Task A_exclusao_das_jornadas_que_se_cruzam_esta_no_banco()
    {
        await using var conexao = new NpgsqlConnection(Banco.ConexaoDoDono);
        await conexao.OpenAsync(Cancelamento);
        await using var comando = new NpgsqlCommand(
            """
            select pg_get_constraintdef(oid) from pg_constraint
            where conname = 'ex_engajamentos_jornada' and contype = 'x'
            """,
            conexao);

        Assert.Equal(
            "EXCLUDE USING gist (trabalhador_id WITH =, tstzrange(inicio, descansado_em) WITH &&)",
            await comando.ExecuteScalarAsync(Cancelamento));
    }

    [Fact]
    public async Task O_papel_da_aplicacao_le_e_grava_mas_nao_muda_o_esquema()
    {
        await using (var sessao = Banco.NovaSessao())
        {
            await sessao.Trabalhadores.Adicionar(TrabalhadoresDeTeste.Estivador(), Cancelamento);
        }

        await using var conexao = new NpgsqlConnection(Banco.ConexaoDaAplicacao);
        await conexao.OpenAsync(Cancelamento);

        foreach (var mudanca in (string[])["create table escala.qualquer (id int)", "alter table escala.trabalhadores drop column cpf", "drop table escala.engajamentos"])
        {
            await using var comando = new NpgsqlCommand(mudanca, conexao);
            var erro = await Assert.ThrowsAsync<PostgresException>(() => comando.ExecuteNonQueryAsync(Cancelamento));

            Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, erro.SqlState);
        }
    }
}
