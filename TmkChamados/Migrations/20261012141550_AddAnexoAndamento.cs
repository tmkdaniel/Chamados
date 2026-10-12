using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TmkChamados.Migrations
{
    /// <inheritdoc />
    public partial class AddAnexoAndamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnexosAndamento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AndamentoId = table.Column<int>(type: "int", nullable: false),
                    NomeArquivoOriginal = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TamanhoBytes = table.Column<long>(type: "bigint", nullable: false),
                    Conteudo = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnexosAndamento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnexosAndamento_Andamentos_AndamentoId",
                        column: x => x.AndamentoId,
                        principalTable: "Andamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnexosAndamento_AndamentoId",
                table: "AnexosAndamento",
                column: "AndamentoId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnexosAndamento");
        }
    }
}
