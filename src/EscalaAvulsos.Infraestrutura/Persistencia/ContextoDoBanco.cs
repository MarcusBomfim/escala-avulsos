using EscalaAvulsos.Dominio.Avisos;
using EscalaAvulsos.Dominio.Trabalhadores;
using EscalaAvulsos.Infraestrutura.Persistencia.Idempotencia;
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

    internal DbSet<RegistroDeIdempotencia> ChavesDeIdempotencia => Set<RegistroDeIdempotencia>();

    internal DbSet<RegistroDeAviso> Avisos => Set<RegistroDeAviso>();

    internal DbSet<RegistroDeInscricaoDePush> InscricoesDePush => Set<RegistroDeInscricaoDePush>();

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
        MapearChavesDeIdempotencia(modelBuilder);
        MapearAvisos(modelBuilder);
        MapearInscricoesDePush(modelBuilder);
    }

    /// <summary>
    /// Os avisos de cada trabalhador, um por fato: a chave do aviso com o
    /// trabalhador é a chave da linha. Cada aviso novo é anunciado pelo banco
    /// (<c>pg_notify</c>), num gatilho escrito na migração (ADR-0019, ADR-0020).
    /// </summary>
    private static void MapearAvisos(ModelBuilder modelBuilder)
    {
        var avisos = modelBuilder.Entity<RegistroDeAviso>();

        avisos.ToTable("avisos", tabela =>
        {
            tabela.HasCheckConstraint("ck_avisos_tipo", $"tipo in ({string.Join(", ", Enum.GetNames<TipoDoAviso>().Select(nome => $"'{nome}'"))})");
            tabela.HasCheckConstraint("ck_avisos_tentativas", "tentativas >= 0");
        });

        avisos.HasKey(registro => new { registro.TrabalhadorId, registro.Chave }).HasName("pk_avisos");
        avisos.Property(registro => registro.TrabalhadorId).HasColumnName("trabalhador_id");
        avisos.Property(registro => registro.Chave).HasColumnName("chave").HasMaxLength(Aviso.TamanhoMaximoDaChave);
        avisos.Property(registro => registro.Tipo).HasColumnName("tipo").HasMaxLength(30);
        avisos.Property(registro => registro.Titulo).HasColumnName("titulo");
        avisos.Property(registro => registro.Texto).HasColumnName("texto");
        avisos.Property(registro => registro.CriadoEm).HasColumnName("criado_em");
        avisos.Property(registro => registro.LidoEm).HasColumnName("lido_em");
        avisos.Property(registro => registro.EntregueEm).HasColumnName("entregue_em");
        avisos.Property(registro => registro.ReservadoEm).HasColumnName("reservado_em");
        avisos.Property(registro => registro.Tentativas).HasColumnName("tentativas");

        // Os recados de um trabalhador, do mais novo para o mais antigo: o que a tela dele lê.
        avisos.HasIndex(registro => new { registro.TrabalhadorId, registro.CriadoEm }).HasDatabaseName("ix_avisos_trabalhador_id_criado_em");

        // Só os que faltam entregar: é por aqui que o servidor procura o que mandar aos celulares.
        avisos.HasIndex(registro => registro.CriadoEm).HasDatabaseName("ix_avisos_por_entregar").HasFilter("entregue_em is null");

        avisos
            .HasOne<RegistroDeTrabalhador>()
            .WithMany()
            .HasForeignKey(registro => registro.TrabalhadorId)
            .HasConstraintName("fk_avisos_trabalhadores")
            .OnDelete(DeleteBehavior.Cascade);
    }

    /// <summary>Os celulares que aceitaram os avisos, pelo endereço do serviço de push de cada um (ADR-0020).</summary>
    private static void MapearInscricoesDePush(ModelBuilder modelBuilder)
    {
        var inscricoes = modelBuilder.Entity<RegistroDeInscricaoDePush>();

        inscricoes.ToTable("inscricoes_de_push", tabela =>
            tabela.HasCheckConstraint("ck_inscricoes_de_push_endereco", "endereco like 'https://%'"));

        inscricoes.HasKey(registro => registro.Endereco).HasName("pk_inscricoes_de_push");
        inscricoes.Property(registro => registro.Endereco).HasColumnName("endereco").HasMaxLength(2048);
        inscricoes.Property(registro => registro.TrabalhadorId).HasColumnName("trabalhador_id");
        inscricoes.Property(registro => registro.ChavePublica).HasColumnName("chave_publica").HasMaxLength(100);
        inscricoes.Property(registro => registro.Segredo).HasColumnName("segredo").HasMaxLength(50);
        inscricoes.Property(registro => registro.CriadaEm).HasColumnName("criada_em");
        inscricoes.HasIndex(registro => registro.TrabalhadorId).HasDatabaseName("ix_inscricoes_de_push_trabalhador_id");
        inscricoes
            .HasOne<RegistroDeTrabalhador>()
            .WithMany()
            .HasForeignKey(registro => registro.TrabalhadorId)
            .HasConstraintName("fk_inscricoes_de_push_trabalhadores")
            .OnDelete(DeleteBehavior.Cascade);
    }

    /// <summary>A chave de idempotência de cada pedido de criação, de quem pediu, com a resposta dada a ele (ADR-0015).</summary>
    private static void MapearChavesDeIdempotencia(ModelBuilder modelBuilder)
    {
        var chaves = modelBuilder.Entity<RegistroDeIdempotencia>();

        chaves.ToTable("chaves_de_idempotencia", tabela =>
            tabela.HasCheckConstraint("ck_chaves_de_idempotencia_chave", "length(chave) between 1 and 255"));

        chaves.HasKey(registro => new { registro.Dono, registro.Chave }).HasName("pk_chaves_de_idempotencia");
        chaves.Property(registro => registro.Dono).HasColumnName("dono").HasMaxLength(255);
        chaves.Property(registro => registro.Chave).HasColumnName("chave").HasMaxLength(Idempotencia.ChavesDeIdempotencia.TamanhoMaximoDaChave);
        chaves.Property(registro => registro.ImpressaoDoPedido).HasColumnName("impressao_do_pedido").HasMaxLength(64);
        chaves.Property(registro => registro.Status).HasColumnName("status");
        chaves.Property(registro => registro.Cabecalhos).HasColumnName("cabecalhos").HasColumnType("jsonb");
        chaves.Property(registro => registro.Corpo).HasColumnName("corpo");
        chaves.Property(registro => registro.CriadaEm).HasColumnName("criada_em");
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
