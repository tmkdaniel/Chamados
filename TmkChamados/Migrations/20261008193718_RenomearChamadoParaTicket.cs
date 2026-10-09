using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TmkChamados.Migrations
{
    /// <inheritdoc />
    public partial class RenomearChamadoParaTicket : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Renomeação pura de tabela/coluna (preserva os dados existentes), em vez do
            // Drop+Create que o scaffold do EF Core gerou por padrão — ver design.md, Decisão 2.
            migrationBuilder.RenameTable(
                name: "Chamados",
                newName: "Tickets");

            migrationBuilder.Sql("EXEC sp_rename 'PK_Chamados', 'PK_Tickets', 'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename 'FK_Chamados_Usuarios_CriadoPorId', 'FK_Tickets_Usuarios_CriadoPorId', 'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename 'FK_Chamados_Usuarios_ResponsavelId', 'FK_Tickets_Usuarios_ResponsavelId', 'OBJECT';");

            migrationBuilder.RenameIndex(
                name: "IX_Chamados_CriadoPorId",
                table: "Tickets",
                newName: "IX_Tickets_CriadoPorId");

            migrationBuilder.RenameIndex(
                name: "IX_Chamados_ResponsavelId",
                table: "Tickets",
                newName: "IX_Tickets_ResponsavelId");

            migrationBuilder.RenameColumn(
                name: "ChamadoId",
                table: "Andamentos",
                newName: "TicketId");

            migrationBuilder.RenameIndex(
                name: "IX_Andamentos_ChamadoId",
                table: "Andamentos",
                newName: "IX_Andamentos_TicketId");

            migrationBuilder.Sql("EXEC sp_rename 'FK_Andamentos_Chamados_ChamadoId', 'FK_Andamentos_Tickets_TicketId', 'OBJECT';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("EXEC sp_rename 'FK_Andamentos_Tickets_TicketId', 'FK_Andamentos_Chamados_ChamadoId', 'OBJECT';");

            migrationBuilder.RenameIndex(
                name: "IX_Andamentos_TicketId",
                table: "Andamentos",
                newName: "IX_Andamentos_ChamadoId");

            migrationBuilder.RenameColumn(
                name: "TicketId",
                table: "Andamentos",
                newName: "ChamadoId");

            migrationBuilder.RenameIndex(
                name: "IX_Tickets_ResponsavelId",
                table: "Tickets",
                newName: "IX_Chamados_ResponsavelId");

            migrationBuilder.RenameIndex(
                name: "IX_Tickets_CriadoPorId",
                table: "Tickets",
                newName: "IX_Chamados_CriadoPorId");

            migrationBuilder.Sql("EXEC sp_rename 'FK_Tickets_Usuarios_ResponsavelId', 'FK_Chamados_Usuarios_ResponsavelId', 'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename 'FK_Tickets_Usuarios_CriadoPorId', 'FK_Chamados_Usuarios_CriadoPorId', 'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename 'PK_Tickets', 'PK_Chamados', 'OBJECT';");

            migrationBuilder.RenameTable(
                name: "Tickets",
                newName: "Chamados");
        }
    }
}
