using EscalaAvulsos.Dominio.Comum;
using EscalaAvulsos.Dominio.Escalacoes;
using EscalaAvulsos.Dominio.Periodos;
using EscalaAvulsos.Dominio.Trabalhadores;
using EscalaAvulsos.Infraestrutura.Persistencia.Registros;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EscalaAvulsos.Infraestrutura.Persistencia.Repositorios;

/// <summary>
/// Grava e lê escalas — e, junto com cada uma, numa transação só, as vagas,
/// que o banco confere, e o rodízio da categoria (ADR-0013).
/// </summary>
/// <remarks>
/// <para>
/// O caminho de uma escala nova: ler o rodízio da categoria e as jornadas
/// dos habilitados, fazer a escala no domínio, e gravá-la com a versão do
/// rodízio que leu. Se outra escala da categoria passou no meio, a gravação
/// é recusada e a escala é refeita com o rodízio novo. Se um habilitado pegou
/// no meio uma jornada que não cabe com o descanso, o banco recusa as vagas.
/// </para>
/// <para>
/// O mesmo vale para a desistência: as jornadas dos que esperam são lidas
/// antes, e o substituto que já não cabe é recusado pelo banco.
/// </para>
/// </remarks>
public sealed class RepositorioDeEscalas(ContextoDoBanco contexto, TimeProvider relogio)
{
    private const string ExclusaoDasJornadas = "ex_engajamentos_jornada";

    private readonly TabelaDeAgregados<RegistroDeEscala> _tabela = new(contexto, relogio, "a escala");

    public Task<Versionado<Escala>?> Obter(EscalaId id, CancellationToken cancelamento = default)
    {
        ArgumentNullException.ThrowIfNull(id);

        return _tabela.Obter(id.Valor, registro => registro.ParaEscala(), cancelamento);
    }

    /// <summary>A escala que saiu da escalação; nula, se ainda não foi feita.</summary>
    public async Task<Versionado<Escala>?> DaEscalacao(EscalacaoId escalacao, CancellationToken cancelamento = default)
    {
        ArgumentNullException.ThrowIfNull(escalacao);

        var escalas = await _tabela.Consultar(
            registros => registros.Where(registro => registro.EscalacaoId == escalacao.Valor),
            registro => registro.ParaEscala(),
            cancelamento);

        return escalas.SingleOrDefault();
    }

    /// <summary>
    /// As escalas do mês em que o trabalhador aparece, com vaga ou sem, na
    /// ordem dos períodos — para a frequência e o extrato (ADR-0009, ADR-0010).
    /// </summary>
    public Task<IReadOnlyList<Escala>> DoTrabalhadorNoMes(TrabalhadorId trabalhador, Mes mes, CancellationToken cancelamento = default)
    {
        ArgumentNullException.ThrowIfNull(mes);

        return DoTrabalhadorEntre(trabalhador, mes.PrimeiroDia, mes.UltimoDia, cancelamento);
    }

    /// <summary>
    /// As escalas em que o trabalhador aparece, com vaga ou sem, dos períodos
    /// de um dia a outro, os dois incluídos, na ordem dos períodos — para a
    /// tela "minha escala".
    /// </summary>
    public async Task<IReadOnlyList<Escala>> DoTrabalhadorEntre(TrabalhadorId trabalhador, DateOnly de, DateOnly ate, CancellationToken cancelamento = default)
    {
        ArgumentNullException.ThrowIfNull(trabalhador);

        var escalas = await _tabela.Consultar(
            registros => registros
                .Where(registro => registro.Dia >= de && registro.Dia <= ate && registro.Trabalhadores.Contains(trabalhador.Valor))
                .OrderBy(registro => registro.Inicio)
                .ThenBy(registro => registro.Categoria),
            registro => registro.ParaEscala(),
            cancelamento);

        return [.. escalas.Select(escala => escala.Agregado)];
    }

    /// <summary>
    /// De onde a próxima escala da categoria parte: o rodízio depois da escala
    /// mais recente, e a versão dele — o rodízio novo na versão 0, se a
    /// categoria ainda não teve escala.
    /// </summary>
    public async Task<Versionado<Rodizio>> RodizioDa(Categoria categoria, CancellationToken cancelamento = default)
    {
        // Numa consulta só: a versão e a escala para onde ela aponta, do mesmo instante.
        var linha = await (
                from rodizio in contexto.Rodizios
                join escala in contexto.Escalas on rodizio.EscalaId equals escala.Id
                where rodizio.Categoria == categoria
                select new { rodizio.Versao, Escala = escala })
            .AsNoTracking()
            .SingleOrDefaultAsync(cancelamento);

        return linha is null
            ? new Versionado<Rodizio>(Rodizio.Novo, 0)
            : new Versionado<Rodizio>(_tabela.Abrir(linha.Escala, registro => registro.ParaEscala()).RodizioDepois, linha.Versao);
    }

    /// <summary>
    /// As jornadas dos trabalhadores perto do período — as que terminam menos
    /// de um descanso antes dele, ou começam menos de um descanso depois: o
    /// que a escala confere antes de pôr alguém numa vaga.
    /// </summary>
    public async Task<IReadOnlyList<Jornada>> JornadasPerto(
        IEnumerable<TrabalhadorId> trabalhadores,
        Periodo periodo,
        Descanso descanso,
        CancellationToken cancelamento = default)
    {
        ArgumentNullException.ThrowIfNull(trabalhadores);
        ArgumentNullException.ThrowIfNull(periodo);
        ArgumentNullException.ThrowIfNull(descanso);

        var ids = trabalhadores.Select(trabalhador => trabalhador.Valor).Distinct().ToArray();
        var desde = (periodo.Inicio - descanso.Minimo).ToUniversalTime();
        var ate = (periodo.Fim + descanso.Minimo).ToUniversalTime();

        var jornadas = await contexto.Engajamentos
            .AsNoTracking()
            .Where(engajamento => ids.Contains(engajamento.TrabalhadorId) && engajamento.Fim > desde && engajamento.Inicio < ate)
            .OrderBy(engajamento => engajamento.Inicio)
            .Select(engajamento => new { engajamento.TrabalhadorId, engajamento.Inicio })
            .ToListAsync(cancelamento);

        return [.. jornadas.Select(jornada => new Jornada(new TrabalhadorId(jornada.TrabalhadorId), Periodo.Contendo(jornada.Inicio)))];
    }

    /// <summary>Grava a escala nova, as vagas dela e o rodízio da categoria, que passa a partir dela.</summary>
    /// <param name="versaoDoRodizio">A versão do rodízio de onde a escala partiu, como <see cref="RodizioDa"/> a deu.</param>
    /// <returns>A versão gravada: 1.</returns>
    /// <exception cref="ConflitoDeConcorrencia">Outra escala da categoria andou o rodízio depois que ele foi lido.</exception>
    /// <exception cref="JornadaSemDescanso">Alguém da escala pegou, em outra, uma jornada que não cabe com o descanso.</exception>
    public async Task<int> Adicionar(Escala escala, int versaoDoRodizio, CancellationToken cancelamento = default)
    {
        ArgumentNullException.ThrowIfNull(escala);

        await using var transacao = await contexto.Database.BeginTransactionAsync(cancelamento);

        try
        {
            var versao = await _tabela.Inserir(RegistroDeEscala.De(escala, versao: 1, _tabela.Agora), Restricoes(escala), cancelamento);
            await GravarVagas(escala, cancelamento);
            await AndarORodizio(escala, versaoDoRodizio, cancelamento);
            await transacao.CommitAsync(cancelamento);

            return versao;
        }
        catch (DbUpdateException erro) when (erro.InnerException is PostgresException { SqlState: PostgresErrorCodes.ExclusionViolation, ConstraintName: ExclusaoDasJornadas })
        {
            await transacao.RollbackAsync(cancelamento);

            throw await SemDescanso(escala, erro, cancelamento);
        }
    }

    /// <summary>
    /// Grava o que mudou na escala — a desistência, o substituto, a presença, a
    /// falta —, com as vagas de agora. Se a vez andou e o rodízio da categoria
    /// ainda parte desta escala, ele muda de versão: quem o leu antes refaz.
    /// </summary>
    /// <param name="versaoLida">A versão em que a escala foi lida.</param>
    /// <returns>A versão nova.</returns>
    /// <exception cref="JornadaSemDescanso">O substituto pegou, em outra escala, uma jornada que não cabe com o descanso.</exception>
    public async Task<int> Atualizar(Escala escala, int versaoLida, CancellationToken cancelamento = default)
    {
        ArgumentNullException.ThrowIfNull(escala);

        var nova = RegistroDeEscala.De(escala, versaoLida + 1, _tabela.Agora);

        await using var transacao = await contexto.Database.BeginTransactionAsync(cancelamento);

        try
        {
            // Antes de a escala nova ser gravada, enquanto o banco tem a de antes para comparar.
            await contexto.Database.ExecuteSqlAsync(
                $"""
                update escala.rodizios rodizio
                set versao = rodizio.versao + 1, atualizado_em = {nova.AtualizadoEm}
                from escala.escalas escala
                where rodizio.categoria = {escala.Categoria.ToString()} and rodizio.escala_id = {nova.Id} and escala.id = rodizio.escala_id
                  and escala.documento -> 'rodizioDepois' is distinct from ({nova.Documento}::jsonb -> 'rodizioDepois')
                """,
                cancelamento);

            var versao = await _tabela.Atualizar(
                nova,
                versaoLida,
                colunas => colunas.SetProperty(registro => registro.Trabalhadores, nova.Trabalhadores),
                Restricoes(escala),
                cancelamento);

            await contexto.Engajamentos.Where(engajamento => engajamento.EscalaId == nova.Id).ExecuteDeleteAsync(cancelamento);
            await GravarVagas(escala, cancelamento);
            await transacao.CommitAsync(cancelamento);

            return versao;
        }
        catch (DbUpdateException erro) when (erro.InnerException is PostgresException { SqlState: PostgresErrorCodes.ExclusionViolation, ConstraintName: ExclusaoDasJornadas })
        {
            await transacao.RollbackAsync(cancelamento);

            throw await SemDescanso(escala, erro, cancelamento);
        }
    }

    /// <summary>Uma linha por vaga ocupada: é nelas que o banco confere as jornadas que se cruzam.</summary>
    private async Task GravarVagas(Escala escala, CancellationToken cancelamento)
    {
        contexto.Engajamentos.AddRange(RegistroDeEngajamento.Da(escala));

        try
        {
            await contexto.SaveChangesAsync(cancelamento);
        }
        catch (DbUpdateException erro) when (erro.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation } violacao)
        {
            throw new AgregadoNaoEncontrado(
                violacao.ConstraintName switch
                {
                    "fk_engajamentos_trabalhadores" => "Há vaga na escala para um trabalhador que não está gravado: a vaga é de quem está no cadastro.",
                    "fk_engajamentos_requisicoes" => "Há vaga na escala de uma requisição que não está gravada: a vaga é de um pedido do operador.",
                    _ => throw new InvalidOperationException($"O banco recusou as vagas pela restrição {violacao.ConstraintName}, que ninguém previu.", erro),
                },
                erro);
        }
        finally
        {
            contexto.ChangeTracker.Clear();
        }
    }

    /// <summary>
    /// O rodízio da categoria passa a partir da escala nova — se ainda está na
    /// versão de onde ela partiu. Senão, outra escala andou a roda no meio.
    /// </summary>
    private async Task AndarORodizio(Escala escala, int versaoLida, CancellationToken cancelamento)
    {
        var agora = _tabela.Agora;
        var categoria = escala.Categoria.ToString();

        // Sem linha ainda, a primeira escala da categoria cria a dela; se outra criou no meio, nada muda, e a conta abaixo diz por quê.
        var andou = versaoLida == 0
            ? await contexto.Database.ExecuteSqlAsync(
                $"""
                insert into escala.rodizios (categoria, versao, escala_id, atualizado_em)
                values ({categoria}, 1, {escala.Id.Valor}, {agora})
                on conflict (categoria) do nothing
                """,
                cancelamento)
            : await contexto.Rodizios
                .Where(rodizio => rodizio.Categoria == escala.Categoria && rodizio.Versao == versaoLida)
                .ExecuteUpdateAsync(
                    colunas => colunas
                        .SetProperty(rodizio => rodizio.Versao, versaoLida + 1)
                        .SetProperty(rodizio => rodizio.EscalaId, escala.Id.Valor)
                        .SetProperty(rodizio => rodizio.AtualizadoEm, agora),
                    cancelamento);

        if (andou == 0)
        {
            var atual = await contexto.Rodizios
                .Where(rodizio => rodizio.Categoria == escala.Categoria)
                .Select(rodizio => (int?)rodizio.Versao)
                .SingleOrDefaultAsync(cancelamento);

            throw new ConflitoDeConcorrencia(
                $"O rodízio {escala.Categoria.ComDe} mudou desde a versão {versaoLida}: está na versão {atual ?? 0}. Leia o rodízio de novo e refaça a escala.",
                atual);
        }
    }

    /// <summary>Quem da escala já tem outra jornada que não cabe com o descanso, e onde — lido depois de a gravação ser desfeita.</summary>
    private async Task<JornadaSemDescanso> SemDescanso(Escala escala, Exception erro, CancellationToken cancelamento)
    {
        var inicio = escala.Periodo.Inicio.ToUniversalTime();
        var descansado = (escala.Periodo.Fim + escala.Descanso.Minimo).ToUniversalTime();
        var naEscala = escala.Engajamentos.ToDictionary(engajamento => engajamento.Trabalhador.Valor);
        var ids = naEscala.Keys.ToArray();

        var cruzadas = await contexto.Engajamentos
            .AsNoTracking()
            .Where(engajamento =>
                engajamento.EscalaId != escala.Id.Valor && ids.Contains(engajamento.TrabalhadorId)
                && engajamento.Inicio < descansado && inicio < engajamento.DescansadoEm)
            .OrderBy(engajamento => engajamento.Inicio)
            .Select(engajamento => new { engajamento.TrabalhadorId, engajamento.Inicio })
            .ToListAsync(cancelamento);

        var motivos = cruzadas.Count == 0
            ? ["Alguém da escala pegou, ao mesmo tempo, outra jornada que não cabe com o descanso."]
            : cruzadas.Select(cruzada =>
                $"{naEscala[cruzada.TrabalhadorId].Nome} (matrícula {naEscala[cruzada.TrabalhadorId].Matricula}) já tem jornada em {Periodo.Contendo(cruzada.Inicio)}, e as duas não cabem com o descanso de {escala.Descanso} entre elas.");

        return new JornadaSemDescanso($"{string.Join(" ", motivos)} A escala não foi gravada: leia as jornadas de novo e refaça.", erro);
    }

    private static Dictionary<string, string> Restricoes(Escala escala) => new()
    {
        ["ux_escalas_escalacao_id"] = $"A escala {escala.Categoria.ComDe} de {escala.Periodo}, já foi feita: cada escalação tem uma escala só.",
        ["fk_escalas_escalacoes"] = $"A escalação {escala.Escalacao} não está gravada: a escala sai de uma escalação aberta.",
    };
}
