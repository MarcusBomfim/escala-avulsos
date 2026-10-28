using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EscalaAvulsos.Infraestrutura.Persistencia.Migracoes
{
    /// <inheritdoc />
    public partial class ChavesDeIdempotencia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "chaves_de_idempotencia",
                schema: "escala",
                columns: table => new
                {
                    dono = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    chave = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    impressao_do_pedido = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<short>(type: "smallint", nullable: true),
                    cabecalhos = table.Column<string>(type: "jsonb", nullable: true),
                    corpo = table.Column<string>(type: "text", nullable: true),
                    criada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_chaves_de_idempotencia", x => new { x.dono, x.chave });
                    table.CheckConstraint("ck_chaves_de_idempotencia_chave", "length(chave) between 1 and 255");
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "chaves_de_idempotencia",
                schema: "escala");
        }
    }
}
