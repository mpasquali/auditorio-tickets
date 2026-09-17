namespace AuditorioTickets.Api.Services;

public interface IEmailService
{
    Task EnviarEntradaAsync(string emailDestino, string nombreComprador, string tituloEvento, Guid boletoId);
}