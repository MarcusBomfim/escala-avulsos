using EscalaAvulsos.Dominio.Periodos;
using EscalaAvulsos.Dominio.Trabalhadores;

namespace EscalaAvulsos.Dominio.Escalacoes;

/// <summary>Um período em que o trabalhador já está escalado, em outra escala — para conferir o descanso.</summary>
public sealed record Jornada(TrabalhadorId Trabalhador, Periodo Periodo);
