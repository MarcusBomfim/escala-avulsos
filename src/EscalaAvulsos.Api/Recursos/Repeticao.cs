using EscalaAvulsos.Infraestrutura.Persistencia;

namespace EscalaAvulsos.Api.Recursos;

/// <summary>
/// O que o servidor relê e refaz sozinho quando outro gravou no meio: as
/// gravações que são só de quem pede — a habilitação, a desistência — e a
/// escala, que parte do rodízio da categoria (ADR-0015).
/// </summary>
/// <remarks>
/// Cada tentativa lê de novo, decide de novo pelo domínio e grava com a versão
/// que acabou de ler — e com as jornadas de agora, se o banco recusou uma vaga
/// por jornada sem descanso (ADR-0013). Na abertura da janela, centenas de trabalhadores se
/// habilitam na mesma escalação; cada conflito custa uma leitura e uma
/// gravação, e a vez continua pela matrícula, e não pela ordem de chegada.
/// </remarks>
internal static class Repeticao
{
    /// <summary>Quantas vezes, no máximo: mais do que isso é uma disputa que não acaba, e o conflito chega a quem pediu.</summary>
    public const int Tentativas = 50;

    public static async Task<T> AteGravar<T>(Func<Task<T>> tentativa)
    {
        ArgumentNullException.ThrowIfNull(tentativa);

        for (var vez = 1; ; vez++)
        {
            try
            {
                return await tentativa();
            }
            catch (Exception erro) when (erro is ConflitoDeConcorrencia or JornadaSemDescanso && vez < Tentativas)
            {
                // Outro gravou no meio — a mesma escalação, o rodízio, uma jornada de quem ia entrar: lê de novo e refaz.
            }
        }
    }
}
