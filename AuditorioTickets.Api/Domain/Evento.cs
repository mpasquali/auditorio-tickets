using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuditorioTickets.Api.Domain;

public class Evento
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(200)]
    public string Titulo { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Descripcion { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Lugar { get; set; } = string.Empty;

    public DateTime FechaEvento { get; set; }

    public int CapacidadMaxima { get; set; }

    /// <summary>
    /// Cupos "apartados": suma de boletos en estado PendientePago + Pagado.
    /// Se incrementa atómicamente al INICIAR la compra (antes de ir a MercadoPago).
    /// Este es el contador crítico para evitar sobreventa.
    /// </summary>
    public int EntradasReservadas { get; set; } = 0;

    /// <summary>
    /// Boletos efectivamente PAGADOS. Se incrementa desde el Webhook de MercadoPago.
    /// Es el valor que se muestra en el dashboard de ventas.
    /// </summary>
    public int EntradasVendidas { get; set; } = 0;

    [Column(TypeName = "decimal(10,2)")]
    public decimal Precio { get; set; }

    /// <summary>
    /// Token de concurrencia optimista. SQL Server lo gestiona como rowversion.
    /// EF Core lo compara automáticamente en el UPDATE (WHERE RowVersion = @original)
    /// y lanza DbUpdateConcurrencyException si otro proceso lo modificó primero.
    /// </summary>
    [Timestamp]
    // public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    [ConcurrencyCheck]
    public Guid ConcurrencyStamp { get; set; } = Guid.NewGuid();

    public ICollection<Boleto> Boletos { get; set; } = new List<Boleto>();

    [NotMapped]
    public int CupoDisponible => CapacidadMaxima - EntradasReservadas;
}