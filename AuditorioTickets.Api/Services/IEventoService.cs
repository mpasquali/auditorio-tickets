using AuditorioTickets.Api.Domain;

namespace AuditorioTickets.Api.Services;

public interface IEventoService
{
    Task<List<Evento>> ObtenerTodosAsync();
    Task<Evento?> ObtenerPorIdAsync(Guid id);
    Task<Evento> CrearAsync(Evento evento);
    Task ActualizarAsync(Evento evento);
    Task EliminarAsync(Guid id);

    /// <summary>
    /// Intenta reservar 1 cupo de forma atómica. Devuelve true si pudo reservar,
    /// false si no había cupo disponible.
    /// </summary>
    Task<bool> IntentarReservarCupoAsync(Guid eventoId);

    Task LiberarCupoAsync(Guid eventoId);
    Task ConfirmarVentaAsync(Guid eventoId);
}