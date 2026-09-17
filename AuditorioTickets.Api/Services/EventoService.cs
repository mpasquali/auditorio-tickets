using AuditorioTickets.Api.Data;
using AuditorioTickets.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace AuditorioTickets.Api.Services;

public class EventoService : IEventoService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<EventoService> _logger;
    private const int MaxReintentos = 5;

    public EventoService(ApplicationDbContext context, ILogger<EventoService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<Evento>> ObtenerTodosAsync() =>
        await _context.Eventos.AsNoTracking().OrderBy(e => e.FechaEvento).ToListAsync();

    public async Task<Evento?> ObtenerPorIdAsync(Guid id) =>
        await _context.Eventos.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);

    public async Task<Evento> CrearAsync(Evento evento)
    {
        _context.Eventos.Add(evento);
        await _context.SaveChangesAsync();
        return evento;
    }

    public async Task ActualizarAsync(Evento evento)
    {
        _context.Eventos.Update(evento);
        await _context.SaveChangesAsync();
    }

    public async Task EliminarAsync(Guid id)
    {
        var evento = await _context.Eventos.FindAsync(id);
        if (evento != null)
        {
            _context.Eventos.Remove(evento);
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// CONTROL DE CONCURRENCIA CRÍTICO.
    /// Estrategia: concurrencia optimista (RowVersion) + reintento con backoff corto,
    /// dentro de una transacción explícita para que el chequeo de cupo y el incremento
    /// sean una sola operación atómica a nivel de aplicación.
    /// </summary>
    public async Task<bool> IntentarReservarCupoAsync(Guid eventoId)
    {
        for (int intento = 0; intento < MaxReintentos; intento++)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.ReadCommitted);

            try
            {
                var evento = await _context.Eventos.FirstOrDefaultAsync(e => e.Id == eventoId);
                if (evento is null)
                    return false;

                if (evento.EntradasReservadas >= evento.CapacidadMaxima)
                {
                    // No hay cupo. No es un error de concurrencia, es una regla de negocio.
                    await transaction.RollbackAsync();
                    return false;
                }

                evento.EntradasReservadas += 1;
                evento.ConcurrencyStamp = Guid.NewGuid();
                await _context.SaveChangesAsync();

                // CORRECCIÓN: Confirmar la transacción y devolver éxito.
                await transaction.CommitAsync();
                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                // Otro request modificó el mismo Evento entre nuestro SELECT y nuestro UPDATE.
                // Descartamos el tracking y reintentamos leyendo el estado más fresco.
                await transaction.RollbackAsync();
                foreach (var entry in _context.ChangeTracker.Entries())
                    entry.State = EntityState.Detached;

                _logger.LogWarning(
                    "Conflicto de concurrencia reservando cupo para evento {EventoId}, reintento {Intento}",
                    eventoId, intento + 1);

                await Task.Delay(50 * (intento + 1)); // backoff simple
            }
        }

        _logger.LogError("No se pudo reservar cupo para {EventoId} tras {Max} reintentos", eventoId, MaxReintentos);
        return false;
    }

    public async Task LiberarCupoAsync(Guid eventoId, int cantidad = 1)
    {
        if (cantidad <= 0) return;

        for (int intento = 0; intento < MaxReintentos; intento++)
        {
            try
            {
                var evento = await _context.Eventos.FirstOrDefaultAsync(e => e.Id == eventoId);
                if (evento is null) return;

                // Math.Max evita que un bug o una doble ejecución deje el contador en negativo.
                evento.EntradasReservadas = Math.Max(0, evento.EntradasReservadas - cantidad);
                evento.ConcurrencyStamp = Guid.NewGuid();

                await _context.SaveChangesAsync();
                return;
            }
            catch (DbUpdateConcurrencyException)
            {
                foreach (var entry in _context.ChangeTracker.Entries())
                    entry.State = EntityState.Detached;

                _logger.LogWarning("Conflicto liberando {Cantidad} cupo(s) del evento {EventoId}, reintento {Intento}",
                    cantidad, eventoId, intento + 1);

                await Task.Delay(50 * (intento + 1));
            }
        }

        _logger.LogError("No se pudieron liberar {Cantidad} cupo(s) del evento {EventoId} tras {Max} reintentos",
            cantidad, eventoId, MaxReintentos);
    }

    public async Task ConfirmarVentaAsync(Guid eventoId)
    {
        // Llamado desde el Webhook: suma +1 a EntradasVendidas (no toca EntradasReservadas,
        // porque ese cupo ya estaba apartado desde IntentarReservarCupoAsync).
        for (int intento = 0; intento < MaxReintentos; intento++)
        {
            try
            {
                var evento = await _context.Eventos.FirstOrDefaultAsync(e => e.Id == eventoId);
                if (evento is null) return;

                evento.EntradasVendidas += 1;
                evento.ConcurrencyStamp = Guid.NewGuid();  
                await _context.SaveChangesAsync();

                return;
            }
            catch (DbUpdateConcurrencyException)
            {
                foreach (var entry in _context.ChangeTracker.Entries())
                    entry.State = EntityState.Detached;
                await Task.Delay(50 * (intento + 1));
            }
        }
    }
}