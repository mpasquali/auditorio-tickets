using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuditorioTickets.Api.Migrations
{
    /// <inheritdoc />
    public partial class IndiceExpiracionBoletos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Boletos_Estado_FechaCreacion",
                table: "Boletos",
                columns: new[] { "Estado", "FechaCreacion" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Boletos_Estado_FechaCreacion",
                table: "Boletos");
        }
    }
}
