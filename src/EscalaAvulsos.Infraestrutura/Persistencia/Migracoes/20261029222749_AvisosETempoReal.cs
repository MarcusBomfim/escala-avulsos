using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EscalaAvulsos.Infraestrutura.Persistencia.Migracoes
{
    /// <inheritdoc />
    public partial class AvisosETempoReal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "avisos",
                schema: "escala",
                columns: table => new
                {
                    trabalhador_id = table.Column<Guid>(type: "uuid", nullable: false),
                    chave = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    tipo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    titulo = table.Column<string>(type: "text", nullable: false),
                    texto = table.Column<string>(type: "text", nullable: false),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    lido_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    entregue_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reservado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    tentativas = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_avisos", x => new { x.trabalhador_id, x.chave });
                    table.CheckConstraint("ck_avisos_tentativas", "tentativas >= 0");
                    table.CheckConstraint("ck_avisos_tipo", "tipo in ('VaiTrabalhar', 'FicouDeFora', 'EntrouPelaEspera', 'HabilitacaoVaiFechar', 'ExameVaiVencer', 'ReciclagemVaiVencer')");
                    table.ForeignKey(
                        name: "fk_avisos_trabalhadores",
                        column: x => x.trabalhador_id,
                        principalSchema: "escala",
                        principalTable: "trabalhadores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "inscricoes_de_push",
                schema: "escala",
                columns: table => new
                {
                    endereco = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    trabalhador_id = table.Column<Guid>(type: "uuid", nullable: false),
                    chave_publica = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    segredo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    criada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inscricoes_de_push", x => x.endereco);
                    table.CheckConstraint("ck_inscricoes_de_push_endereco", "endereco like 'https://%'");
                    table.ForeignKey(
                        name: "fk_inscricoes_de_push_trabalhadores",
                        column: x => x.trabalhador_id,
                        principalSchema: "escala",
                        principalTable: "trabalhadores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_avisos_por_entregar",
                schema: "escala",
                table: "avisos",
                column: "criado_em",
                filter: "entregue_em is null");

            migrationBuilder.CreateIndex(
                name: "ix_avisos_trabalhador_id_criado_em",
                schema: "escala",
                table: "avisos",
                columns: new[] { "trabalhador_id", "criado_em" });

            migrationBuilder.CreateIndex(
                name: "ix_inscricoes_de_push_trabalhador_id",
                schema: "escala",
                table: "inscricoes_de_push",
                column: "trabalhador_id");

            // O banco anuncia o que mudou, na transação de quem gravou: o anúncio
            // só sai se a gravação for confirmada, e sai uma vez por período
            // mesmo que a transação grave várias linhas dele (ADR-0020).
            migrationBuilder.Sql(
                """
                create function escala.anunciar_periodo() returns trigger language plpgsql as $$
                begin
                    perform pg_notify('escala_mudou', json_build_object('tabela', tg_table_name, 'categoria', new.categoria, 'inicio', new.inicio)::text);
                    return null;
                end
                $$;

                create function escala.anunciar_aviso() returns trigger language plpgsql as $$
                begin
                    perform pg_notify('escala_mudou', json_build_object('tabela', tg_table_name, 'trabalhador', new.trabalhador_id)::text);
                    return null;
                end
                $$;

                create trigger tg_escalacoes_anunciar after insert or update on escala.escalacoes for each row execute function escala.anunciar_periodo();
                create trigger tg_escalas_anunciar after insert or update on escala.escalas for each row execute function escala.anunciar_periodo();
                create trigger tg_requisicoes_anunciar after insert or update on escala.requisicoes for each row execute function escala.anunciar_periodo();
                create trigger tg_avisos_anunciar after insert on escala.avisos for each row execute function escala.anunciar_aviso();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                drop trigger tg_escalacoes_anunciar on escala.escalacoes;
                drop trigger tg_escalas_anunciar on escala.escalas;
                drop trigger tg_requisicoes_anunciar on escala.requisicoes;
                drop function escala.anunciar_periodo();
                """);

            migrationBuilder.DropTable(
                name: "avisos",
                schema: "escala");

            migrationBuilder.DropTable(
                name: "inscricoes_de_push",
                schema: "escala");

            migrationBuilder.Sql("drop function escala.anunciar_aviso();");
        }
    }
}
