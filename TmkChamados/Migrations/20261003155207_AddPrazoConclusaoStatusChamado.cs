using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TmkChamados.Migrations
{
    /// <inheritdoc />
    public partial class AddPrazoConclusaoStatusChamado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DataConclusao",
                table: "Chamados",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DataPrazo",
                table: "Chamados",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DataConclusao",
                table: "Chamados");

            migrationBuilder.DropColumn(
                name: "DataPrazo",
                table: "Chamados");
        }
    }
}
