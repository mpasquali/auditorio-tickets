namespace AuditorioTickets.Api.Services;

public interface IQrService
{
    string GenerarQrBase64(string contenido);
}