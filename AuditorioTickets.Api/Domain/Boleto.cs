using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AuditorioTickets.Shared.Enums;

namespace AuditorioTickets.Api.Domain;

public class Boleto
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid EventoId { get; set; }
    public Evento Evento { get; set; } = null!;

    [Required, MaxLength(150)]
    public string CompradorNombre { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string CompradorEmail { get; set; } = string.Empty;

    public EstadoBoleto Estado { get; set; } = EstadoBoleto.PendientePago;

    [Column(TypeName = "decimal(10,2)")]
    public decimal PrecioPagado { get; set; }

    // Referencias de MercadoPago
    [MaxLength(100)]
    public string? MercadoPagoPreferenceId { get; set; }

    [MaxLength(100)]
    public string? MercadoPagoPaymentId { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaConfirmacion { get; set; }

    // QR generado al confirmarse el pago (guardamos el PNG en base64 para simplificar el MVP)
    public string? CodigoQrBase64 { get; set; }
}