namespace EscalaAvulsos.Infraestrutura.Persistencia;

/// <summary>
/// Um agregado lido do banco, e a versão em que ele estava.
/// </summary>
/// <remarks>
/// A versão fica aqui, ao lado do agregado, e não dentro dele: é coisa da
/// persistência, e não do negócio. Quem altera e grava de volta devolve a
/// versão que leu — se outro gravou no meio, a gravação é recusada
/// (ADR-0012).
/// </remarks>
public sealed record Versionado<T>(T Agregado, int Versao);
