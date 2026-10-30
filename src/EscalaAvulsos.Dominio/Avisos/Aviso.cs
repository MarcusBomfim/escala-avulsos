using EscalaAvulsos.Dominio.Trabalhadores;

namespace EscalaAvulsos.Dominio.Avisos;

/// <summary>O que o aviso conta ao trabalhador.</summary>
public enum TipoDoAviso
{
    /// <summary>A escala saiu, e ele tem vaga.</summary>
    VaiTrabalhar,

    /// <summary>A escala saiu, e ele ficou de fora — com o porquê e o lugar na espera.</summary>
    FicouDeFora,

    /// <summary>Quem tinha a vaga desistiu, e a vaga passou para ele.</summary>
    EntrouPelaEspera,

    /// <summary>A habilitação de um período em que ele pode trabalhar vai fechar, e ele ainda não se habilitou.</summary>
    HabilitacaoVaiFechar,

    /// <summary>O exame médico (ASO) vai vencer.</summary>
    ExameVaiVencer,

    /// <summary>A reciclagem de uma função vai vencer.</summary>
    ReciclagemVaiVencer,
}

/// <summary>
/// Um recado para o trabalhador, escrito para ele, em segunda pessoa, e com
/// as datas por extenso: o aviso chega ao celular sem o aplicativo aberto, e
/// pode ser lido dias depois — "hoje" e "amanhã" ficariam errados (ADR-0019).
/// </summary>
/// <remarks>
/// A chave diz de que é o aviso — "escala:…", "habilitacao:…", "aso:2026-11-30"
/// —, e o mesmo trabalhador não recebe dois avisos com a mesma chave: avisar
/// de novo do mesmo fato não acontece, por mais vezes que ele seja conferido.
/// </remarks>
public sealed record Aviso
{
    public const int TamanhoMaximoDaChave = 120;

    internal Aviso(TrabalhadorId trabalhador, TipoDoAviso tipo, string chave, string titulo, string texto, DateTimeOffset criadoEm)
    {
        Trabalhador = trabalhador;
        Tipo = tipo;
        Chave = chave;
        Titulo = titulo;
        Texto = texto;
        CriadoEm = criadoEm;
    }

    public TrabalhadorId Trabalhador { get; }

    public TipoDoAviso Tipo { get; }

    /// <summary>De que fato é o aviso: o mesmo trabalhador não recebe dois com a mesma chave.</summary>
    public string Chave { get; }

    /// <summary>"Você vai trabalhar": o que aparece em negrito na notificação.</summary>
    public string Titulo { get; }

    /// <summary>"Quinta-feira, 5 de novembro, das 13h às 19h: sinaleiro no terno 1 do navio Atlântico Sul, no Terminal Cais Leste, berço 4."</summary>
    public string Texto { get; }

    public DateTimeOffset CriadoEm { get; }

    public override string ToString() => $"{Titulo}. {Texto}";
}
