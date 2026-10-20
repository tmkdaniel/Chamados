using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TmkChamados.Migrations
{
    /// <inheritdoc />
    public partial class AddMessageIdToTicket : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MessageId",
                table: "Tickets",
                type: "nvarchar(998)",
                maxLength: 998,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_MessageId",
                table: "Tickets",
                column: "MessageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tickets_MessageId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "MessageId",
                table: "Tickets");
        }
    }
}
