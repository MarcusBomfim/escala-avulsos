using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EscalaAvulsos.Infraestrutura.Persistencia.Migracoes
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "escala");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:btree_gist", ",,");

            migrationBuilder.CreateTable(
                name: "escalacoes",
                schema: "escala",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    habilitados = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    versao = table.Column<int>(type: "integer", nullable: false),
                    formato = table.Column<short>(type: "smallint", nullable: false),
                    documento = table.Column<string>(type: "jsonb", nullable: false),
                    categoria = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    inicio = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    dia = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_escalacoes", x => x.id);
                    table.CheckConstraint("ck_escalacoes_categoria", "categoria in ('Capatazia', 'Estiva', 'ConferenciaDeCarga', 'ConsertoDeCarga', 'VigilanciaDeEmbarcacoes', 'Bloco')");
                    table.CheckConstraint("ck_escalacoes_documento", "jsonb_typeof(documento) = 'object'");
                    table.CheckConstraint("ck_escalacoes_formato", "formato >= 1");
                    table.CheckConstraint("ck_escalacoes_versao", "versao >= 1");
                });

            migrationBuilder.CreateTable(
                name: "requisicoes",
                schema: "escala",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    versao = table.Column<int>(type: "integer", nullable: false),
                    formato = table.Column<short>(type: "smallint", nullable: false),
                    documento = table.Column<string>(type: "jsonb", nullable: false),
                    categoria = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    inicio = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    dia = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_requisicoes", x => x.id);
                    table.CheckConstraint("ck_requisicoes_categoria", "categoria in ('Capatazia', 'Estiva', 'ConferenciaDeCarga', 'ConsertoDeCarga', 'VigilanciaDeEmbarcacoes', 'Bloco')");
                    table.CheckConstraint("ck_requisicoes_documento", "jsonb_typeof(documento) = 'object'");
                    table.CheckConstraint("ck_requisicoes_formato", "formato >= 1");
                    table.CheckConstraint("ck_requisicoes_versao", "versao >= 1");
                });

            migrationBuilder.CreateTable(
                name: "trabalhadores",
                schema: "escala",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    matricula = table.Column<int>(type: "integer", nullable: false),
                    cpf = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    versao = table.Column<int>(type: "integer", nullable: false),
                    formato = table.Column<short>(type: "smallint", nullable: false),
                    documento = table.Column<string>(type: "jsonb", nullable: false),
                    categoria = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_trabalhadores", x => x.id);
                    table.CheckConstraint("ck_trabalhadores_categoria", "categoria in ('Capatazia', 'Estiva', 'ConferenciaDeCarga', 'ConsertoDeCarga', 'VigilanciaDeEmbarcacoes', 'Bloco')");
                    table.CheckConstraint("ck_trabalhadores_cpf", "cpf ~ '^[0-9]{11}$'");
                    table.CheckConstraint("ck_trabalhadores_documento", "jsonb_typeof(documento) = 'object'");
                    table.CheckConstraint("ck_trabalhadores_formato", "formato >= 1");
                    table.CheckConstraint("ck_trabalhadores_matricula", "matricula between 1 and 9999999");
                    table.CheckConstraint("ck_trabalhadores_versao", "versao >= 1");
                });

            migrationBuilder.CreateTable(
                name: "escalas",
                schema: "escala",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    escalacao_id = table.Column<Guid>(type: "uuid", nullable: false),
                    trabalhadores = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    versao = table.Column<int>(type: "integer", nullable: false),
                    formato = table.Column<short>(type: "smallint", nullable: false),
                    documento = table.Column<string>(type: "jsonb", nullable: false),
                    categoria = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    inicio = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    dia = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_escalas", x => x.id);
                    table.CheckConstraint("ck_escalas_categoria", "categoria in ('Capatazia', 'Estiva', 'ConferenciaDeCarga', 'ConsertoDeCarga', 'VigilanciaDeEmbarcacoes', 'Bloco')");
                    table.CheckConstraint("ck_escalas_documento", "jsonb_typeof(documento) = 'object'");
                    table.CheckConstraint("ck_escalas_formato", "formato >= 1");
                    table.CheckConstraint("ck_escalas_versao", "versao >= 1");
                    table.ForeignKey(
                        name: "fk_escalas_escalacoes",
                        column: x => x.escalacao_id,
                        principalSchema: "escala",
                        principalTable: "escalacoes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "engajamentos",
                schema: "escala",
                columns: table => new
                {
                    escala_id = table.Column<Guid>(type: "uuid", nullable: false),
                    trabalhador_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requisicao_id = table.Column<Guid>(type: "uuid", nullable: false),
                    inicio = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    fim = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    descansado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_engajamentos", x => new { x.escala_id, x.trabalhador_id });
                    table.CheckConstraint("ck_engajamentos_descanso", "fim <= descansado_em");
                    table.CheckConstraint("ck_engajamentos_jornada", "inicio < fim");
                    table.ForeignKey(
                        name: "fk_engajamentos_escalas",
                        column: x => x.escala_id,
                        principalSchema: "escala",
                        principalTable: "escalas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_engajamentos_requisicoes",
                        column: x => x.requisicao_id,
                        principalSchema: "escala",
                        principalTable: "requisicoes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_engajamentos_trabalhadores",
                        column: x => x.trabalhador_id,
                        principalSchema: "escala",
                        principalTable: "trabalhadores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "rodizios",
                schema: "escala",
                columns: table => new
                {
                    categoria = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    versao = table.Column<int>(type: "integer", nullable: false),
                    escala_id = table.Column<Guid>(type: "uuid", nullable: false),
                    atualizado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rodizios", x => x.categoria);
                    table.CheckConstraint("ck_rodizios_categoria", "categoria in ('Capatazia', 'Estiva', 'ConferenciaDeCarga', 'ConsertoDeCarga', 'VigilanciaDeEmbarcacoes', 'Bloco')");
                    table.CheckConstraint("ck_rodizios_versao", "versao >= 1");
                    table.ForeignKey(
                        name: "fk_rodizios_escalas",
                        column: x => x.escala_id,
                        principalSchema: "escala",
                        principalTable: "escalas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_engajamentos_requisicao_id",
                schema: "escala",
                table: "engajamentos",
                column: "requisicao_id");

            migrationBuilder.CreateIndex(
                name: "ix_engajamentos_trabalhador_id_inicio",
                schema: "escala",
                table: "engajamentos",
                columns: new[] { "trabalhador_id", "inicio" });

            migrationBuilder.CreateIndex(
                name: "ix_escalacoes_dia",
                schema: "escala",
                table: "escalacoes",
                column: "dia");

            migrationBuilder.CreateIndex(
                name: "ux_escalacoes_categoria_inicio",
                schema: "escala",
                table: "escalacoes",
                columns: new[] { "categoria", "inicio" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_escalas_dia",
                schema: "escala",
                table: "escalas",
                column: "dia");

            migrationBuilder.CreateIndex(
                name: "ux_escalas_escalacao_id",
                schema: "escala",
                table: "escalas",
                column: "escalacao_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_requisicoes_categoria_inicio",
                schema: "escala",
                table: "requisicoes",
                columns: new[] { "categoria", "inicio" });

            migrationBuilder.CreateIndex(
                name: "ix_rodizios_escala_id",
                schema: "escala",
                table: "rodizios",
                column: "escala_id");

            migrationBuilder.CreateIndex(
                name: "ux_trabalhadores_cpf",
                schema: "escala",
                table: "trabalhadores",
                column: "cpf",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_trabalhadores_matricula",
                schema: "escala",
                table: "trabalhadores",
                column: "matricula",
                unique: true);

            // Escrita à mão: o Entity Framework não conhece restrição de exclusão.
            // Duas jornadas do mesmo trabalhador não se cruzam, contado o descanso
            // depois de cada uma: do início ao fim do descanso, os intervalos não
            // têm um instante em comum (ADR-0013). O banco confere na hora de
            // gravar, também entre duas gravações ao mesmo tempo.
            migrationBuilder.Sql(
                """
                alter table escala.engajamentos
                    add constraint ex_engajamentos_jornada
                    exclude using gist (trabalhador_id with =, tstzrange(inicio, descansado_em) with &&);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "engajamentos",
                schema: "escala");

            migrationBuilder.DropTable(
                name: "rodizios",
                schema: "escala");

            migrationBuilder.DropTable(
                name: "requisicoes",
                schema: "escala");

            migrationBuilder.DropTable(
                name: "trabalhadores",
                schema: "escala");

            migrationBuilder.DropTable(
                name: "escalas",
                schema: "escala");

            migrationBuilder.DropTable(
                name: "escalacoes",
                schema: "escala");
        }
    }
}
