using EscalaAvulsos.Dominio.Escalacoes;
using EscalaAvulsos.Dominio.Requisicoes;
using EscalaAvulsos.Dominio.Trabalhadores;
using EscalaAvulsos.Infraestrutura.Documentos;

namespace EscalaAvulsos.Infraestrutura.Persistencia.Registros;

/*
 * Um registro por agregado. Cada um sabe virar do agregado — serializando o
 * documento e copiando para as colunas o que o banco precisa enxergar — e
 * voltar para ele, conferindo o formato e passando pela reconstituição do
 * domínio (ADR-0011). As colunas são cópias do documento: quem manda é ele,
 * e elas existem para o banco procurar e conferir sem abri-lo.
 */

internal sealed class RegistroDeTrabalhador : RegistroDeAgregado
{
    /// <summary>Única: é como o trabalhador se identifica no aplicativo.</summary>
    public int Matricula { get; set; }

    /// <summary>Só os dígitos; único, porque é a pessoa.</summary>
    public string Cpf { get; set; } = string.Empty;

    public static RegistroDeTrabalhador De(Trabalhador trabalhador, int versao, DateTimeOffset agora)
    {
        var registro = new RegistroDeTrabalhador { Matricula = trabalhador.Matricula.Numero, Cpf = trabalhador.Cpf.Numero };
        var documento = Json.Escrever(DocumentoDoTrabalhador.De(trabalhador), ContextoJson.Default.DocumentoDoTrabalhador);

        return registro.Preencher<RegistroDeTrabalhador>(trabalhador.Id.Valor, trabalhador.Categoria, versao, DocumentoDoTrabalhador.Formato, documento, agora);
    }

    public Trabalhador ParaTrabalhador()
    {
        ConferirFormato(DocumentoDoTrabalhador.Formato, "do trabalhador");

        return Json.Ler(Documento, ContextoJson.Default.DocumentoDoTrabalhador).ParaTrabalhador(new TrabalhadorId(Id));
    }
}

internal sealed class RegistroDeRequisicao : RegistroDoPeriodo
{
    public static RegistroDeRequisicao De(Requisicao requisicao, int versao, DateTimeOffset agora)
    {
        var registro = new RegistroDeRequisicao();
        registro.NoPeriodo(requisicao.Periodo);
        var documento = Json.Escrever(DocumentoDaRequisicao.De(requisicao), ContextoJson.Default.DocumentoDaRequisicao);

        return registro.Preencher<RegistroDeRequisicao>(requisicao.Id.Valor, requisicao.Categoria, versao, DocumentoDaRequisicao.Formato, documento, agora);
    }

    public Requisicao ParaRequisicao()
    {
        ConferirFormato(DocumentoDaRequisicao.Formato, "da requisição");

        return Json.Ler(Documento, ContextoJson.Default.DocumentoDaRequisicao).ParaRequisicao(new RequisicaoId(Id));
    }
}

internal sealed class RegistroDeEscalacao : RegistroDoPeriodo
{
    /// <summary>Quem se habilitou: é por aqui que se acham as escalações de um trabalhador no mês.</summary>
    public Guid[] Habilitados { get; set; } = [];

    public static RegistroDeEscalacao De(Escalacao escalacao, int versao, DateTimeOffset agora)
    {
        var registro = new RegistroDeEscalacao { Habilitados = [.. escalacao.Habilitacoes.Select(habilitacao => habilitacao.Trabalhador.Valor)] };
        registro.NoPeriodo(escalacao.Periodo);
        var documento = Json.Escrever(DocumentoDaEscalacao.De(escalacao), ContextoJson.Default.DocumentoDaEscalacao);

        return registro.Preencher<RegistroDeEscalacao>(escalacao.Id.Valor, escalacao.Categoria, versao, DocumentoDaEscalacao.Formato, documento, agora);
    }

    public Escalacao ParaEscalacao()
    {
        ConferirFormato(DocumentoDaEscalacao.Formato, "da escalação");

        return Json.Ler(Documento, ContextoJson.Default.DocumentoDaEscalacao).ParaEscalacao(new EscalacaoId(Id));
    }
}

internal sealed class RegistroDeEscala : RegistroDoPeriodo
{
    /// <summary>A escalação de onde a escala saiu — chave estrangeira, e única: cada escalação tem uma escala só.</summary>
    public Guid EscalacaoId { get; set; }

    /// <summary>Todos os que se habilitaram, com vaga ou sem: é por aqui que se acham as escalas de um trabalhador no mês.</summary>
    public Guid[] Trabalhadores { get; set; } = [];

    public static RegistroDeEscala De(Escala escala, int versao, DateTimeOffset agora)
    {
        var registro = new RegistroDeEscala
        {
            EscalacaoId = escala.Escalacao.Valor,
            Trabalhadores = [.. escala.Situacoes.Select(situacao => situacao.Trabalhador.Valor)],
        };
        registro.NoPeriodo(escala.Periodo);
        var documento = Json.Escrever(DocumentoDaEscala.De(escala), ContextoJson.Default.DocumentoDaEscala);

        return registro.Preencher<RegistroDeEscala>(escala.Id.Valor, escala.Categoria, versao, DocumentoDaEscala.Formato, documento, agora);
    }

    public Escala ParaEscala()
    {
        ConferirFormato(DocumentoDaEscala.Formato, "da escala");

        return Json.Ler(Documento, ContextoJson.Default.DocumentoDaEscala).ParaEscala(new EscalaId(Id));
    }
}

/// <summary>
/// Quem está em cada vaga de cada escala, numa linha por vaga — a cópia das
/// vagas do documento que o banco confere: o mesmo trabalhador não pega duas
/// jornadas que se cruzam, contando o descanso depois de cada uma (ADR-0013).
/// </summary>
internal sealed class RegistroDeEngajamento
{
    public Guid EscalaId { get; set; }

    public Guid TrabalhadorId { get; set; }

    public Guid RequisicaoId { get; set; }

    /// <summary>O começo da jornada, em UTC.</summary>
    public DateTimeOffset Inicio { get; set; }

    public DateTimeOffset Fim { get; set; }

    /// <summary>Quando termina o descanso depois da jornada, com o descanso da escala: até lá, o trabalhador não pega outra.</summary>
    public DateTimeOffset DescansadoEm { get; set; }

    /// <summary>As linhas da escala, uma por vaga ocupada agora.</summary>
    public static IEnumerable<RegistroDeEngajamento> Da(Escala escala) =>
        escala.Engajamentos.Select(engajamento => new RegistroDeEngajamento
        {
            EscalaId = escala.Id.Valor,
            TrabalhadorId = engajamento.Trabalhador.Valor,
            RequisicaoId = engajamento.Requisicao.Valor,
            Inicio = escala.Periodo.Inicio.ToUniversalTime(),
            Fim = escala.Periodo.Fim.ToUniversalTime(),
            DescansadoEm = (escala.Periodo.Fim + escala.Descanso.Minimo).ToUniversalTime(),
        });
}

/// <summary>
/// De onde continua o rodízio de cada categoria: a escala mais recente, e uma
/// versão que sobe a cada vez que o rodízio anda (ADR-0013).
/// </summary>
/// <remarks>
/// O rodízio em si está no documento da escala; a linha só aponta para ela.
/// Duas escalas da mesma categoria feitas ao mesmo tempo, a partir da mesma
/// versão, não gravam as duas: a segunda é recusada, e refeita com o rodízio
/// que a primeira deixou.
/// </remarks>
internal sealed class RegistroDoRodizio
{
    public Categoria Categoria { get; set; }

    public int Versao { get; set; }

    /// <summary>A escala de onde o rodízio continua — chave estrangeira.</summary>
    public Guid EscalaId { get; set; }

    public DateTimeOffset AtualizadoEm { get; set; }
}
