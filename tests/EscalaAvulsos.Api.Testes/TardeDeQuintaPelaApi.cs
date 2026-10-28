using EscalaAvulsos.Api.Recursos.Escalacoes;
using EscalaAvulsos.Api.Recursos.Escalas;
using EscalaAvulsos.Api.Recursos.Requisicoes;
using EscalaAvulsos.Dominio.Periodos;
using EscalaAvulsos.Dominio.Trabalhadores;
using static EscalaAvulsos.Api.Testes.Novembro;

namespace EscalaAvulsos.Api.Testes;

/// <summary>
/// A tarde de quinta, 05/11/2026, montada inteira pela API, como cada um faria
/// do seu lado: o órgão gestor cadastra os seis e abre a escalação; a Cais
/// Forte pede um sinaleiro e dois estivadores na quarta às 10h; os seis se
/// habilitam às 6h. Sem escala anterior, o rodízio começa pela menor matrícula.
/// </summary>
/// <remarks>
/// Feita às 10h, a escala sai assim: o Antônio sinaleiro, o Edson e o
/// Benedito estivadores; na espera de estivador, o Carlos, o Dirceu e, por ser
/// cadastrado, o João por último.
/// </remarks>
internal sealed class TardeDeQuintaPelaApi(Func<HttpClient> escalador, Func<HttpClient> operador, Func<int, HttpClient> trabalhador, RelogioDosTestes relogio)
{
    public const string DaTarde = "/api/escalacoes/Estiva/2026-11-05/Tarde";

    public const int Antonio = 4127;

    public const int Benedito = 4130;

    public const int Carlos = 4152;

    public const int Dirceu = 4188;

    public const int Edson = 4101;

    public const int Joao = 5310;

    /// <summary>Tudo pronto para a escala: os seis cadastrados, a requisição, a escalação aberta e as habilitações.</summary>
    public async Task Preparar()
    {
        var doOrgaoGestor = escalador();
        await doOrgaoGestor.CadastrarEstivador(Antonio, "Antônio Ferreira", outrasFuncoes: "Sinaleiro");
        await doOrgaoGestor.CadastrarEstivador(Benedito, "Benedito Lima");
        await doOrgaoGestor.CadastrarEstivador(Carlos, "Carlos Mota", outrasFuncoes: "Sinaleiro");
        await doOrgaoGestor.CadastrarEstivador(Dirceu, "Dirceu Alves");
        await doOrgaoGestor.CadastrarEstivador(Edson, "Edson Prado");
        await doOrgaoGestor.CadastrarEstivador(Joao, "João Batista", Inscricao.Cadastrado);

        relogio.Acertar(Em(4, 10));
        await operador()
            .Enviar(HttpMethod.Post, "/api/requisicoes", Requisicao(5, Turno.Tarde, new VagasPedidas("Sinaleiro", 1), new VagasPedidas("Estivador", 2)), chave: "tarde-de-quinta")
            .ComSucesso();
        await doOrgaoGestor.Enviar(HttpMethod.Put, DaTarde).ComSucesso();

        relogio.Acertar(Em(5, 6));
        await Habilitar(Antonio, "Sinaleiro", "Estivador");
        await Habilitar(Benedito, "Estivador");
        await Habilitar(Carlos, "Sinaleiro", "Estivador");
        await Habilitar(Dirceu, "Estivador");
        await Habilitar(Edson, "Estivador");
        await Habilitar(Joao, "Estivador");
    }

    /// <summary>Prepara e faz a escala às 10h, quando a habilitação fecha.</summary>
    public async Task<EscalaResposta> Escalar()
    {
        await Preparar();
        relogio.Acertar(Em(5, 10));

        return await escalador().Enviar(HttpMethod.Post, $"{DaTarde}/escala").LerComSucesso<EscalaResposta>();
    }

    private async Task Habilitar(int matricula, params string[] funcoes) =>
        await trabalhador(matricula).Enviar(HttpMethod.Put, "/api/eu/habilitacoes/2026-11-05/Tarde", new PedidoDeHabilitacao(funcoes)).ComSucesso();
}
