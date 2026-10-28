using System.Text.Json.Nodes;
using EscalaAvulsos.Dominio.Escalacoes;
using EscalaAvulsos.Dominio.Requisicoes;
using EscalaAvulsos.Dominio.Testes.Escalacoes;
using EscalaAvulsos.Dominio.Testes.Trabalhadores;
using EscalaAvulsos.Dominio.Trabalhadores;
using static EscalaAvulsos.Dominio.Testes.Escalacoes.EscalacoesDeTeste;
using static EscalaAvulsos.Dominio.Testes.Requisicoes.RequisicoesDeTeste;
using static EscalaAvulsos.Infraestrutura.Testes.Documentos.Exemplos;

namespace EscalaAvulsos.Infraestrutura.Testes.Documentos;

/*
 * O formato gravado, escrito por extenso. Renomear uma propriedade do domínio
 * não muda o banco sem ninguém ver (ADR-0011): muda o mapeamento, e aqui o
 * teste falha. Mudar o formato de propósito pede um número de formato novo,
 * e quem lê os documentos antigos.
 */
public sealed class FormatoDosDocumentosTests
{
    [Fact]
    public void O_trabalhador()
    {
        var antonio = TrabalhadoresDeTeste.Estivador();
        antonio.Impedir(new Impedimento(TipoDeImpedimento.Ferias, new DateOnly(2027, 1, 4), new DateOnly(2027, 2, 2)));

        IgualAo(
            """
            {
              "matricula": 4127,
              "nome": "Antônio Ferreira",
              "cpf": "52998412726",
              "categoria": "Estiva",
              "inscricao": "Registrado",
              "qualificacoes": [{ "funcao": "Estivador", "desde": "1994-03-01", "validaAte": null }],
              "aso": { "emitidoEm": "2025-12-01", "validoAte": "2026-11-30" },
              "impedimentos": [{ "tipo": "Ferias", "de": "2027-01-04", "ate": "2027-02-02", "observacao": null }],
              "cancelamento": null
            }
            """,
            Json(antonio));
    }

    [Fact]
    public void A_requisicao()
    {
        var requisicao = new Requisicao(TardeDeQuinta.DaTarde, [[new(Funcoes.Sinaleiro, 1), new(Funcoes.Estivador, 2)]], QuartaAs10h);
        requisicao.Cancelar("O navio atrasou", Em(5, 8));

        IgualAo(
            """
            {
              "operador": { "nome": "Cais Forte Operações Portuárias", "cnpj": "11222333000181" },
              "navio": { "nome": "Atlântico Sul", "imo": "9876543" },
              "local": { "terminal": "Terminal Cais Leste", "berco": "4" },
              "periodo": { "dia": "2026-11-05", "turno": "Tarde" },
              "categoria": "Estiva",
              "ternos": [[{ "funcao": "Sinaleiro", "quantidade": 1 }, { "funcao": "Estivador", "quantidade": 2 }]],
              "feitaEm": "2026-11-04T10:00:00-03:00",
              "cancelamento": { "motivo": "O navio atrasou", "em": "2026-11-05T08:00:00-03:00" }
            }
            """,
            Json(requisicao));
    }

    [Fact]
    public void A_escalacao()
    {
        var antonio = TrabalhadoresDeTeste.Estivador();
        var escalacao = DaEstiva();
        escalacao.Habilitar(antonio, [Funcoes.Estivador], QuintaAs6h);

        IgualAo(
            $$"""
            {
              "categoria": "Estiva",
              "periodo": { "dia": "2026-11-05", "turno": "Tarde" },
              "calendario": { "antecedenciaDasRequisicoes": "04:00:00", "antecedenciaDaAbertura": "12:00:00", "antecedenciaDoFechamento": "03:00:00" },
              "habilitacoes": [{ "trabalhador": "{{antonio.Id}}", "matricula": 4127, "funcoes": ["Estivador"], "em": "2026-11-05T06:00:00-03:00" }]
            }
            """,
            Json(escalacao));
    }

    [Fact]
    public void A_escala()
    {
        var antonio = TrabalhadoresDeTeste.Estivador();
        var benedito = TrabalhadoresDeTeste.Estivador(4130, "Benedito Lima");
        var escalacao = DaEstiva();
        escalacao.Habilitar(antonio, [Funcoes.Estivador], QuintaAs6h);
        escalacao.Habilitar(benedito, [Funcoes.Estivador], QuintaAs6h);
        var requisicao = new Requisicao(TardeDeQuinta.DaTarde, [[new(Funcoes.Estivador, 1)]], QuartaAs10h);
        var escala = Escalador.Escalar(escalacao, [requisicao], [antonio, benedito], [], Rodizio.Novo, Descanso.DaLei, Em(5, 10));
        escala.ConfirmarPresenca(antonio.Id, Em(5, 12, 40));

        IgualAo(
            $$"""
            {
              "escalacao": "{{escalacao.Id}}",
              "categoria": "Estiva",
              "periodo": { "dia": "2026-11-05", "turno": "Tarde" },
              "descanso": "11:00:00",
              "feitaEm": "2026-11-05T10:00:00-03:00",
              "encerradaEm": null,
              "requisicoes": [
                {
                  "id": "{{requisicao.Id}}",
                  "operador": { "nome": "Cais Forte Operações Portuárias", "cnpj": "11222333000181" },
                  "navio": { "nome": "Atlântico Sul", "imo": "9876543" },
                  "local": { "terminal": "Terminal Cais Leste", "berco": "4" }
                }
              ],
              "vagas": [{ "trabalhador": "{{antonio.Id}}", "requisicao": "{{requisicao.Id}}", "terno": 1, "funcao": "Estivador" }],
              "vagasNaoPreenchidas": [],
              "situacoes": [
                {
                  "trabalhador": "{{antonio.Id}}",
                  "matricula": 4127,
                  "nome": "Antônio Ferreira",
                  "funcoes": ["Estivador"],
                  "motivos": [],
                  "substituto": false,
                  "desistencia": null,
                  "comparecimento": "Presente",
                  "presenteDesde": "2026-11-05T12:40:00-03:00",
                  "justificativaDaFalta": null
                },
                {
                  "trabalhador": "{{benedito.Id}}",
                  "matricula": 4130,
                  "nome": "Benedito Lima",
                  "funcoes": ["Estivador"],
                  "motivos": [{ "tipo": "NaoAlcancado", "funcao": "Estivador", "posicoesAntesDaVez": 1, "motivosDaAptidao": [], "jornada": null }],
                  "substituto": false,
                  "desistencia": null,
                  "comparecimento": "Aguardando",
                  "presenteDesde": null,
                  "justificativaDaFalta": null
                }
              ],
              "requisicoesForaDoPrazo": [],
              "rodizioDepois": [{ "funcao": "Estivador", "inscricao": "Registrado", "ultimoEscalado": 4127 }],
              "espera": [{ "trabalhador": "{{benedito.Id}}", "matricula": 4130, "funcao": "Estivador", "inscricao": "Registrado" }]
            }
            """,
            Json(escala));
    }

    /// <summary>Compara os dois JSON sem os espaços, e na ordem das propriedades: a ordem também é o formato.</summary>
    private static void IgualAo(string esperado, string gravado) =>
        Assert.Equal(JsonNode.Parse(esperado)!.ToJsonString(), JsonNode.Parse(gravado)!.ToJsonString());
}
