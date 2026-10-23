using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TmkChamados.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketOrcamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TicketId",
                table: "Orcamentos",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orcamentos_TicketId",
                table: "Orcamentos",
                column: "TicketId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orcamentos_Tickets_TicketId",
                table: "Orcamentos",
                column: "TicketId",
                principalTable: "Tickets",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orcamentos_Tickets_TicketId",
                table: "Orcamentos");

            migrationBuilder.DropIndex(
                name: "IX_Orcamentos_TicketId",
                table: "Orcamentos");

            migrationBuilder.DropColumn(
                name: "TicketId",
                table: "Orcamentos");
        }
    }
}
