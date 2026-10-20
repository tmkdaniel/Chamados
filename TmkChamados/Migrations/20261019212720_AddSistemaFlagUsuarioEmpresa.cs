using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TmkChamados.Migrations
{
    /// <inheritdoc />
    public partial class AddSistemaFlagUsuarioEmpresa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Sistema",
                table: "Usuarios",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Sistema",
                table: "Empresas",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Sistema",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "Sistema",
                table: "Empresas");
        }
    }
}
