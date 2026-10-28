using EscalaAvulsos.Dominio.Identificacao;
using EscalaAvulsos.Dominio.Trabalhadores;

namespace EscalaAvulsos.Dominio.Escalacoes;

/// <summary>
/// Um habilitado que podia pegar uma vaga da função e não foi alcançado pelo
/// rodízio: é quem entra se alguém desistir.
/// </summary>
/// <remarks>
/// A fila de cada função tem os registrados primeiro e os cadastrados depois,
/// cada grupo na ordem da vez em que o rodízio parou (ADR-0008).
/// </remarks>
internal sealed record EsperaPorVaga(TrabalhadorId Trabalhador, Funcao Funcao, Inscricao Inscricao, Matricula Matricula);
