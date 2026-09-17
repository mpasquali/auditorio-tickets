using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuditorioTickets.Api.Migrations
{
    /// <inheritdoc />
    public partial class MigracionASqlite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ConcurrencyStamp",
                table: "Eventos",
                type: "TEXT",
                rowVersion: true,
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                table: "Eventos");
        }
    }
}
