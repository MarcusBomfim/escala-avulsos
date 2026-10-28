using EscalaAvulsos.Dominio.Periodos;
using EscalaAvulsos.Dominio.Trabalhadores;

namespace EscalaAvulsos.Infraestrutura.Persistencia.Registros;

/// <summary>
/// A linha de uma tabela de agregados: o documento inteiro, e ao lado dele o
/// que o banco precisa enxergar sem abri-lo.
/// </summary>
/// <remarks>
/// Tipo de persistência, e não do domínio: tem setters, porque é o Entity
/// Framework que o preenche. O agregado só entra e sai daqui pelos
/// documentos (ADR-0011).
/// </remarks>
internal abstract class RegistroDeAgregado
{
    public Guid Id { get; set; }

    /// <summary>Começa em 1 e cresce a cada gravação: é o que a concorrência otimista confere (ADR-0012).</summary>
    public int Versao { get; set; }

    /// <summary>A versão do formato do documento gravado.</summary>
    public short Formato { get; set; }

    /// <summary>O agregado inteiro, em JSON — coluna <c>jsonb</c>.</summary>
    public string Documento { get; set; } = string.Empty;

    /// <summary>A categoria do trabalhador, da requisição, da escalação ou da escala: cada uma tem o seu rodízio.</summary>
    public Categoria Categoria { get; set; }

    public DateTimeOffset CriadoEm { get; set; }

    public DateTimeOffset AtualizadoEm { get; set; }

    /// <summary>
    /// Este código lê um formato; documento gravado em outro é recusado com a
    /// razão, em vez de lido pela metade.
    /// </summary>
    /// <param name="doAgregado">De que é o documento, numa frase: "do trabalhador", "da escala".</param>
    protected void ConferirFormato(short esperado, string doAgregado)
    {
        if (Formato != esperado)
        {
            throw new InvalidOperationException(
                $"O documento {doAgregado} {Id} está no formato {Formato}, e esta versão do sistema lê o formato {esperado}.");
        }
    }

    /// <param name="agora">O instante da gravação, guardado em UTC: é o que o <c>timestamptz</c> do PostgreSQL recebe.</param>
    protected T Preencher<T>(Guid id, Categoria categoria, int versao, short formato, string documento, DateTimeOffset agora)
        where T : RegistroDeAgregado
    {
        Id = id;
        Categoria = categoria;
        Versao = versao;
        Formato = formato;
        Documento = documento;
        CriadoEm = agora.ToUniversalTime();
        AtualizadoEm = agora.ToUniversalTime();

        return (T)this;
    }
}

/// <summary>Um registro de agregado de um período: a requisição, a escalação, a escala.</summary>
internal abstract class RegistroDoPeriodo : RegistroDeAgregado
{
    /// <summary>Quando o período começa, em UTC.</summary>
    public DateTimeOffset Inicio { get; set; }

    /// <summary>O dia do período — o dia em que ele começa (ADR-0002) —, para as consultas do mês.</summary>
    public DateOnly Dia { get; set; }

    protected void NoPeriodo(Periodo periodo)
    {
        Inicio = periodo.Inicio.ToUniversalTime();
        Dia = periodo.Dia;
    }
}
