using EscalaAvulsos.Dominio.Trabalhadores;

namespace EscalaAvulsos.Dominio.Requisicoes;

/// <summary>Quantas vagas de uma função: "estivador, 8".</summary>
/// <remarks>
/// É só o que o operador escreveu: a requisição confere a quantidade e a
/// função repetida, e diz todos os problemas do pedido de uma vez.
/// </remarks>
public sealed record VagasDaFuncao(Funcao Funcao, int Quantidade);
