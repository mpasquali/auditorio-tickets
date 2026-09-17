using AuditorioTickets.Api.Domain;
using AuditorioTickets.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace AuditorioTickets.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Evento> Eventos => Set<Evento>();
    public DbSet<Boleto> Boletos => Set<Boleto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Evento>(entity =>
        {
            entity.HasIndex(e => e.FechaEvento);

            // RowVersion como token de concurrencia optimista (columna rowversion en SQL Server)
            // entity.Property(e => e.RowVersion).IsRowVersion();

            entity.HasMany(e => e.Boletos)
                  .WithOne(b => b.Evento)
                  .HasForeignKey(b => b.EventoId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Boleto>(entity =>
        {
            entity.HasIndex(b => b.MercadoPagoPreferenceId);
            entity.HasIndex(b => b.MercadoPagoPaymentId);
            entity.Property(b => b.Estado)
                  .HasConversion<string>() // guarda el enum como texto legible en la BD
                  .HasMaxLength(30);
        });

        // Seed opcional para pruebas rápidas
        modelBuilder.Entity<Evento>().HasData(new Evento
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Titulo = "Acto de Colación 2026",
            Descripcion = "Ceremonia de graduación de la promoción 2026",
            Lugar = "Auditorio Central",
            FechaEvento = new DateTime(2026, 12, 15, 18, 0, 0, DateTimeKind.Utc),
            CapacidadMaxima = 300,
            EntradasReservadas = 0,
            EntradasVendidas = 0,
            Precio = 2500m,
            // RowVersion = new byte[8] // EF/SQL Server lo completa en runtime real; para HasData hace falta un valor fijo
        });

        modelBuilder.Entity<Evento>(entity =>
        {
            entity.HasIndex(e => e.FechaEvento);

            // Ya no usamos IsRowVersion() (eso es específico de SQL Server).
            entity.Property(e => e.ConcurrencyStamp).IsConcurrencyToken();

            entity.HasMany(e => e.Boletos)
                .WithOne(b => b.Evento)
                .HasForeignKey(b => b.EventoId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}