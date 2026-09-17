namespace AuditorioTickets.Api.Services;

public interface IMercadoPagoService
{
    Task<(string PreferenceId, string InitPoint)> CrearPreferenceAsync(
        Guid boletoId, string tituloEvento, decimal precio, string emailComprador);

    Task<(string Status, string? ExternalReference, string PaymentId)> ObtenerPagoAsync(string paymentId);
}