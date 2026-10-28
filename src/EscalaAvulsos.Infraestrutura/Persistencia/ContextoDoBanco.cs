using EscalaAvulsos.Dominio.Trabalhadores;
using EscalaAvulsos.Infraestrutura.Persistencia.Registros;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EscalaAvulsos.Infraestrutura.Persistencia;

/// <summary>
/// O banco: uma tabela por tipo de agregado, cada linha um documento
/// <c>jsonb</c> com identidade, versão e o que se consulta sem abri-lo — e as
/// vagas de cada escala numa tabela à parte, onde o banco confere que ninguém
/// pega duas jornadas que se cruzam (ADR-0012, ADR-0013).
/// </summary>
/// <remarks>
/// <para>
/// O que o banco consegue garantir sem duplicar regra de negócio, ele
/// garante: identidade única, versão e formato válidos, documento que é um
/// objeto JSON, a categoria que existe, a matrícula e o CPF que não se
/// repetem, uma escalação por categoria e período, uma escala por escalação,
/// e as referências entre os agregados. As regras do negócio ficam no
/// domínio, e valem também na leitura (ADR-0011).
/// </para>
/// <para>
/// Nomes em snake_case, no esquema <c>escala</c>, como se escreve SQL à mão —
/// quem consulta o banco direto não precisa de aspas.
/// </para>
/// </remarks>
public sealed class ContextoDoBanco(DbContextOptions<ContextoDoBanco> opcoes) : DbContext(opcoes)
{
    public const string Esquema = "escala";

    internal DbSet<RegistroDeTrabalhador> Trabalhadores => Set<RegistroDeTrabalhador>();

    internal DbSet<RegistroDeRequisicao> Requisicoes => Set<RegistroDeRequisicao>();

    internal DbSet<RegistroDeEscalacao> Escalacoes => Set<RegistroDeEscalacao>();

    internal DbSet<RegistroDeEscala> Escalas => Set<RegistroDeEscala>();

    internal DbSet<RegistroDeEngajamento> Engajamentos => Set<RegistroDeEngajamento>();

    internal DbSet<RegistroDoRodizio> Rodizios => Set<RegistroDoRodizio>();

    /// <summary>
    /// O Npgsql configurado como as migrações esperam — com o histórico delas
    /// dentro do esquema do projeto. Usado pela aplicação, pelo dotnet-ef e
    /// pelos testes, para os três verem o mesmo banco.
    /// </summary>
    public static void Configurar(DbContextOptionsBuilder opcoes, string conexao)
    {
        ArgumentNullException.ThrowIfNull(opcoes);

        opcoes.UseNpgsql(conexao, npgsql => npgsql.MigrationsHistoryTable("historico_de_migracoes", Esquema));
    }

    /// <summary>Um contexto avulso para a cadeia de conexão — para ferramentas e testes, fora da injeção de dependência.</summary>
    public static ContextoDoBanco Criar(string conexao)
    {
        var opcoes = new DbContextOptionsBuilder<ContextoDoBanco>();
        Configurar(opcoes, conexao);

        return new ContextoDoBanco(opcoes.Options);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Esquema);

        // O operador de igualdade do uuid num índice GiST, para a restrição de exclusão das jornadas (ADR-0013).
        modelBuilder.HasPostgresExtension("btree_gist");

        var trabalhadores = Agregado<RegistroDeTrabalhador>(modelBuilder, "trabalhadores");
        trabalhadores.Property(registro => registro.Matricula).HasColumnName("matricula");
        trabalhadores.Property(registro => registro.Cpf).HasColumnName("cpf").HasMaxLength(11);
        trabalhadores.ToTable(tabela =>
        {
            tabela.HasCheckConstraint("ck_trabalhadores_matricula", "matricula between 1 and 9999999");
            tabela.HasCheckConstraint("ck_trabalhadores_cpf", "cpf ~ '^[0-9]{11}$'");
        });
        trabalhadores.HasIndex(registro => registro.Matricula).IsUnique().HasDatabaseName("ux_trabalhadores_matricula");
        trabalhadores.HasIndex(registro => registro.Cpf).IsUnique().HasDatabaseName("ux_trabalhadores_cpf");

        // As requisições de uma categoria num período: as que a escala vai preencher.
        var requisicoes = DoPeriodo<RegistroDeRequisicao>(modelBuilder, "requisicoes");
        requisicoes.HasIndex(registro => new { registro.Categoria, registro.Inicio }).HasDatabaseName("ix_requisicoes_categoria_inicio");

        var escalacoes = DoPeriodo<RegistroDeEscalacao>(modelBuilder, "escalacoes");
        escalacoes.Property(registro => registro.Habilitados).HasColumnName("habilitados");
        escalacoes.HasIndex(registro => new { registro.Categoria, registro.Inicio }).IsUnique().HasDatabaseName("ux_escalacoes_categoria_inicio");
        escalacoes.HasIndex(registro => registro.Dia).HasDatabaseName("ix_escalacoes_dia");

        var escalas = DoPeriodo<RegistroDeEscala>(modelBuilder, "escalas");
        escalas.Property(registro => registro.EscalacaoId).HasColumnName("escalacao_id");
        escalas.Property(registro => registro.Trabalhadores).HasColumnName("trabalhadores");
        escalas.HasIndex(registro => registro.EscalacaoId).IsUnique().HasDatabaseName("ux_escalas_escalacao_id");
        escalas.HasIndex(registro => registro.Dia).HasDatabaseName("ix_escalas_dia");
        escalas
            .HasOne<RegistroDeEscalacao>()
            .WithMany()
            .HasForeignKey(registro => registro.EscalacaoId)
            .HasConstraintName("fk_escalas_escalacoes")
            .OnDelete(DeleteBehavior.Restrict);

        MapearEngajamentos(modelBuilder);
        MapearRodizios(modelBuilder);
    }

    /// <summary>
    /// As vagas ocupadas de cada escala. A restrição de exclusão que não deixa
    /// duas jornadas do mesmo trabalhador se cruzarem, contado o descanso, é
    /// escrita na migração: o Entity Framework não a conhece (ADR-0013).
    /// </summary>
    private static void MapearEngajamentos(ModelBuilder modelBuilder)
    {
        var engajamentos = modelBuilder.Entity<RegistroDeEngajamento>();

        engajamentos.ToTable("engajamentos", tabela =>
        {
            tabela.HasCheckConstraint("ck_engajamentos_jornada", "inicio < fim");
            tabela.HasCheckConstraint("ck_engajamentos_descanso", "fim <= descansado_em");
        });

        engajamentos.HasKey(registro => new { registro.EscalaId, registro.TrabalhadorId }).HasName("pk_engajamentos");
        engajamentos.Property(registro => registro.EscalaId).HasColumnName("escala_id");
        engajamentos.Property(registro => registro.TrabalhadorId).HasColumnName("trabalhador_id");
        engajamentos.Property(registro => registro.RequisicaoId).HasColumnName("requisicao_id");
        engajamentos.Property(registro => registro.Inicio).HasColumnName("inicio");
        engajamentos.Property(registro => registro.Fim).HasColumnName("fim");
        engajamentos.Property(registro => registro.DescansadoEm).HasColumnName("descansado_em");

        // As jornadas de um trabalhador perto de um período: o que a escala confere antes de pôr alguém numa vaga.
        engajamentos.HasIndex(registro => new { registro.TrabalhadorId, registro.Inicio }).HasDatabaseName("ix_engajamentos_trabalhador_id_inicio");
        engajamentos.HasIndex(registro => registro.RequisicaoId).HasDatabaseName("ix_engajamentos_requisicao_id");

        // A vaga é da escala: sai com ela. O trabalhador e a requisição não saem enquanto houver vaga deles.
        engajamentos
            .HasOne<RegistroDeEscala>()
            .WithMany()
            .HasForeignKey(registro => registro.EscalaId)
            .HasConstraintName("fk_engajamentos_escalas")
            .OnDelete(DeleteBehavior.Cascade);
        engajamentos
            .HasOne<RegistroDeTrabalhador>()
            .WithMany()
            .HasForeignKey(registro => registro.TrabalhadorId)
            .HasConstraintName("fk_engajamentos_trabalhadores")
            .OnDelete(DeleteBehavior.Restrict);
        engajamentos
            .HasOne<RegistroDeRequisicao>()
            .WithMany()
            .HasForeignKey(registro => registro.RequisicaoId)
            .HasConstraintName("fk_engajamentos_requisicoes")
            .OnDelete(DeleteBehavior.Restrict);
    }

    /// <summary>Uma linha por categoria: a escala de onde o rodízio continua, e a versão dele (ADR-0013).</summary>
    private static void MapearRodizios(ModelBuilder modelBuilder)
    {
        var rodizios = modelBuilder.Entity<RegistroDoRodizio>();

        rodizios.ToTable("rodizios", tabela => tabela.HasCheckConstraint("ck_rodizios_versao", "versao >= 1"));
        rodizios.HasKey(registro => registro.Categoria).HasName("pk_rodizios");
        rodizios.Property(registro => registro.Categoria).HasColumnName("categoria").HasConversion<string>().HasMaxLength(30);
        rodizios.Property(registro => registro.Versao).HasColumnName("versao");
        rodizios.Property(registro => registro.EscalaId).HasColumnName("escala_id");
        rodizios.Property(registro => registro.AtualizadoEm).HasColumnName("atualizado_em");
        rodizios.ToTable(tabela => tabela.HasCheckConstraint("ck_rodizios_categoria", NaCategoria()));
        rodizios.HasIndex(registro => registro.EscalaId).HasDatabaseName("ix_rodizios_escala_id");
        rodizios
            .HasOne<RegistroDeEscala>()
            .WithMany()
            .HasForeignKey(registro => registro.EscalaId)
            .HasConstraintName("fk_rodizios_escalas")
            .OnDelete(DeleteBehavior.Restrict);
    }

    /// <summary>As colunas que toda tabela de agregado tem.</summary>
    private static EntityTypeBuilder<T> Agregado<T>(ModelBuilder modelBuilder, string tabela)
        where T : RegistroDeAgregado
    {
        var entidade = modelBuilder.Entity<T>();

        entidade.ToTable(tabela, tabelaBuilder =>
        {
            tabelaBuilder.HasCheckConstraint($"ck_{tabela}_versao", "versao >= 1");
            tabelaBuilder.HasCheckConstraint($"ck_{tabela}_formato", "formato >= 1");
            tabelaBuilder.HasCheckConstraint($"ck_{tabela}_documento", "jsonb_typeof(documento) = 'object'");
            tabelaBuilder.HasCheckConstraint($"ck_{tabela}_categoria", NaCategoria());
        });

        entidade.HasKey(registro => registro.Id).HasName($"pk_{tabela}");
        entidade.Property(registro => registro.Id).HasColumnName("id").ValueGeneratedNever();
        entidade.Property(registro => registro.Versao).HasColumnName("versao").IsConcurrencyToken();
        entidade.Property(registro => registro.Formato).HasColumnName("formato");
        entidade.Property(registro => registro.Documento).HasColumnName("documento").HasColumnType("jsonb");
        entidade.Property(registro => registro.Categoria).HasColumnName("categoria").HasConversion<string>().HasMaxLength(30);
        entidade.Property(registro => registro.CriadoEm).HasColumnName("criado_em");
        entidade.Property(registro => registro.AtualizadoEm).HasColumnName("atualizado_em");

        return entidade;
    }

    /// <summary>Um agregado de um período, com o início e o dia em colunas.</summary>
    private static EntityTypeBuilder<T> DoPeriodo<T>(ModelBuilder modelBuilder, string tabela)
        where T : RegistroDoPeriodo
    {
        var entidade = Agregado<T>(modelBuilder, tabela);

        entidade.Property(registro => registro.Inicio).HasColumnName("inicio");
        entidade.Property(registro => registro.Dia).HasColumnName("dia");

        return entidade;
    }

    /// <summary>A coluna <c>categoria</c> só com os valores que o enum tem, pelo nome.</summary>
    private static string NaCategoria() =>
        $"categoria in ({string.Join(", ", Enum.GetNames<Categoria>().Select(nome => $"'{nome}'"))})";
}
