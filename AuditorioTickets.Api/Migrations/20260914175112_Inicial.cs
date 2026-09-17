using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuditorioTickets.Api.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Eventos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Titulo = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    Lugar = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    FechaEvento = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CapacidadMaxima = table.Column<int>(type: "INTEGER", nullable: false),
                    EntradasReservadas = table.Column<int>(type: "INTEGER", nullable: false),
                    EntradasVendidas = table.Column<int>(type: "INTEGER", nullable: false),
                    Precio = table.Column<decimal>(type: "decimal(10,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Eventos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Boletos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EventoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CompradorNombre = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    CompradorEmail = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Estado = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    PrecioPagado = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    MercadoPagoPreferenceId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    MercadoPagoPaymentId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaConfirmacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CodigoQrBase64 = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Boletos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Boletos_Eventos_EventoId",
                        column: x => x.EventoId,
                        principalTable: "Eventos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Eventos",
                columns: new[] { "Id", "CapacidadMaxima", "Descripcion", "EntradasReservadas", "EntradasVendidas", "FechaEvento", "Lugar", "Precio", "Titulo" },
                values: new object[] { new Guid("11111111-1111-1111-1111-111111111111"), 300, "Ceremonia de graduación de la promoción 2026", 0, 0, new DateTime(2026, 12, 15, 18, 0, 0, 0, DateTimeKind.Utc), "Auditorio Central", 2500m, "Acto de Colación 2026" });

            migrationBuilder.CreateIndex(
                name: "IX_Boletos_EventoId",
                table: "Boletos",
                column: "EventoId");

            migrationBuilder.CreateIndex(
                name: "IX_Boletos_MercadoPagoPaymentId",
                table: "Boletos",
                column: "MercadoPagoPaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_Boletos_MercadoPagoPreferenceId",
                table: "Boletos",
                column: "MercadoPagoPreferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_Eventos_FechaEvento",
                table: "Eventos",
                column: "FechaEvento");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Boletos");

            migrationBuilder.DropTable(
                name: "Eventos");
        }
    }
}
