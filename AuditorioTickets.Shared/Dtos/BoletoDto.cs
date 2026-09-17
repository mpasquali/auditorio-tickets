using AuditorioTickets.Shared.Enums;

namespace AuditorioTickets.Shared.Dtos;

public class IniciarCompraDto
{
    public Guid EventoId { get; set; }
    public string CompradorNombre { get; set; } = string.Empty;
    public string CompradorEmail { get; set; } = string.Empty;
}

public class IniciarCompraResponseDto
{
    public Guid BoletoId { get; set; }
    public string InitPoint { get; set; } = string.Empty; // Link de pago de MercadoPago
}

public class BoletoDto
{
    public Guid Id { get; set; }
    public Guid EventoId { get; set; }
    public string TituloEvento { get; set; } = string.Empty;
    public string CompradorNombre { get; set; } = string.Empty;
    public string CompradorEmail { get; set; } = string.Empty;
    public EstadoBoleto Estado { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaConfirmacion { get; set; }
    public string? CodigoQrBase64 { get; set; }
}