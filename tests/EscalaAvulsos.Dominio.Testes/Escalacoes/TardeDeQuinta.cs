using EscalaAvulsos.Dominio.Escalacoes;
using EscalaAvulsos.Dominio.Identificacao;
using EscalaAvulsos.Dominio.Periodos;
using EscalaAvulsos.Dominio.Requisicoes;
using EscalaAvulsos.Dominio.Testes.Trabalhadores;
using EscalaAvulsos.Dominio.Trabalhadores;
using static EscalaAvulsos.Dominio.Testes.Escalacoes.EscalacoesDeTeste;
using static EscalaAvulsos.Dominio.Testes.Requisicoes.RequisicoesDeTeste;

namespace EscalaAvulsos.Dominio.Testes.Escalacoes;

/// <summary>
/// A tarde de quinta do README: um terno com um sinaleiro e dois estivadores,
/// seis habilitados — o Dirceu trabalhou de manhã —, e o rodízio parado no
/// Carlos (sinaleiro) e no Benedito (estivador). Escalada às 10h, sai assim:
/// Antônio sinaleiro, Carlos e Edson estivadores; na espera de estivador, o
/// Benedito e, depois dele, o João, que é cadastrado.
/// </summary>
internal sealed class TardeDeQuinta
{
    public static readonly DadosDaRequisicao DaTarde = new(CaisForte, AtlanticoSul, CaisLeste, QuintaATarde);

    public TardeDeQuinta()
    {
        Antonio = TrabalhadoresDeTeste.Estivador(4127, "Antônio Ferreira");
        Antonio.Qualificar(Funcoes.Sinaleiro, new DateOnly(2010, 5, 10));
        Carlos = TrabalhadoresDeTeste.Estivador(4152, "Carlos Mota");
        Carlos.Qualificar(Funcoes.Sinaleiro, new DateOnly(2012, 8, 1));
        Benedito = TrabalhadoresDeTeste.Estivador(4130, "Benedito Lima");
        Dirceu = TrabalhadoresDeTeste.Estivador(4188, "Dirceu Alves");
        Edson = TrabalhadoresDeTeste.Estivador(4101, "Edson Prado");
        Joao = TrabalhadoresDeTeste.Estivador(5310, "João Batista", Inscricao.Cadastrado);

        Escalacao.Habilitar(Antonio, [Funcoes.Sinaleiro, Funcoes.Estivador], Em(5, 6));
        Escalacao.Habilitar(Benedito, [Funcoes.Estivador], Em(5, 2));
        Escalacao.Habilitar(Carlos, [Funcoes.Sinaleiro, Funcoes.Estivador], Em(5, 9, 59));
        Escalacao.Habilitar(Dirceu, [Funcoes.Estivador], Em(5, 1));
        Escalacao.Habilitar(Edson, [Funcoes.Estivador], Em(5, 7));
        Escalacao.Habilitar(Joao, [Funcoes.Estivador], Em(5, 3));

        Trabalhadores = [Antonio, Benedito, Carlos, Dirceu, Edson, Joao];
        Jornadas = [new Jornada(Dirceu.Id, Periodo.De(new DateOnly(2026, 11, 5), Turno.Manha))];
    }

    public Escalacao Escalacao { get; } = DaEstiva();

    public Trabalhador Antonio { get; }

    public Trabalhador Benedito { get; }

    public Trabalhador Carlos { get; }

    public Trabalhador Dirceu { get; }

    public Trabalhador Edson { get; }

    public Trabalhador Joao { get; }

    public Trabalhador[] Trabalhadores { get; }

    public Jornada[] Jornadas { get; }

    public Requisicao Requisicao { get; } = new(DaTarde, [[new(Funcoes.Sinaleiro, 1), new(Funcoes.Estivador, 2)]], QuartaAs10h);

    public Rodizio Rodizio { get; } = Rodizio.Novo
        .ComAVezEm(Funcoes.Sinaleiro, Inscricao.Registrado, Matricula.De(4152))
        .ComAVezEm(Funcoes.Estivador, Inscricao.Registrado, Matricula.De(4130));

    public Escala Escalar(DateTimeOffset? em = null) =>
        Escalador.Escalar(Escalacao, [Requisicao], Trabalhadores, Jornadas, Rodizio, Descanso.DaLei, em ?? Em(5, 10));
}
