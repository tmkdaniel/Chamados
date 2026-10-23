using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TmkChamados.Migrations
{
    /// <inheritdoc />
    public partial class AddNegociacaoOrcamentos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PrazoAtualDias",
                table: "Orcamentos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ValidadeAtual",
                table: "Orcamentos",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorAtual",
                table: "Orcamentos",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VersaoAtual",
                table: "Orcamentos",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "HistoricoOrcamento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrcamentoId = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Data = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VersaoNumero = table.Column<int>(type: "int", nullable: true),
                    Texto = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoricoOrcamento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistoricoOrcamento_Orcamentos_OrcamentoId",
                        column: x => x.OrcamentoId,
                        principalTable: "Orcamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HistoricoOrcamento_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VersoesOrcamento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrcamentoId = table.Column<int>(type: "int", nullable: false),
                    Numero = table.Column<int>(type: "int", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PrazoDias = table.Column<int>(type: "int", nullable: false),
                    Validade = table.Column<DateTime>(type: "date", nullable: false),
                    Observacao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CriadoPorId = table.Column<int>(type: "int", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VersoesOrcamento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VersoesOrcamento_Orcamentos_OrcamentoId",
                        column: x => x.OrcamentoId,
                        principalTable: "Orcamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VersoesOrcamento_Usuarios_CriadoPorId",
                        column: x => x.CriadoPorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HistoricoOrcamento_OrcamentoId",
                table: "HistoricoOrcamento",
                column: "OrcamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_HistoricoOrcamento_UsuarioId",
                table: "HistoricoOrcamento",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_VersoesOrcamento_CriadoPorId",
                table: "VersoesOrcamento",
                column: "CriadoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_VersoesOrcamento_OrcamentoId_Numero",
                table: "VersoesOrcamento",
                columns: new[] { "OrcamentoId", "Numero" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HistoricoOrcamento");

            migrationBuilder.DropTable(
                name: "VersoesOrcamento");

            migrationBuilder.DropColumn(
                name: "PrazoAtualDias",
                table: "Orcamentos");

            migrationBuilder.DropColumn(
                name: "ValidadeAtual",
                table: "Orcamentos");

            migrationBuilder.DropColumn(
                name: "ValorAtual",
                table: "Orcamentos");

            migrationBuilder.DropColumn(
                name: "VersaoAtual",
                table: "Orcamentos");
        }
    }
}
