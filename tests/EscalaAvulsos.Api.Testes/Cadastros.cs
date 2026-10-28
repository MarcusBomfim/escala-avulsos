using EscalaAvulsos.Api.Recursos.Trabalhadores;
using EscalaAvulsos.Dominio.Testes.Trabalhadores;
using EscalaAvulsos.Dominio.Trabalhadores;

namespace EscalaAvulsos.Api.Testes;

/// <summary>Os trabalhadores dos exemplos, cadastrados pela API, como o órgão gestor faria.</summary>
internal static class Cadastros
{
    /// <summary>
    /// Um estivador qualificado como estivador desde 1994 — e nas outras
    /// funções dadas —, com o exame válido até 30/11/2026: o mesmo dos testes
    /// do domínio, com o CPF tirado da matrícula.
    /// </summary>
    /// <returns>A versão em que ele ficou.</returns>
    public static async Task<int> CadastrarEstivador(
        this HttpClient escalador,
        int matricula,
        string nome,
        Inscricao inscricao = Inscricao.Registrado,
        params string[] outrasFuncoes)
    {
        var cpf = TrabalhadoresDeTeste.CpfDaMatricula(matricula).Formatado;
        var versao = (await escalador.Enviar(HttpMethod.Post, "/api/trabalhadores", new NovoTrabalhador(matricula, nome, cpf, Categoria.Estiva, inscricao)).ComSucesso()).Versao();

        foreach (var funcao in (string[])["Estivador", .. outrasFuncoes])
        {
            versao = (await escalador.Enviar(HttpMethod.Post, $"/api/trabalhadores/{matricula}/qualificacoes", new NovaQualificacao(funcao, new DateOnly(1994, 3, 1)), versao).ComSucesso()).Versao();
        }

        return (await escalador.Enviar(HttpMethod.Put, $"/api/trabalhadores/{matricula}/aso", new NovoAso(new DateOnly(2025, 12, 1), new DateOnly(2026, 11, 30)), versao).ComSucesso()).Versao();
    }
}
