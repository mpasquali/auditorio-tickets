using AuditorioTickets.Api.Configuration;
using AuditorioTickets.Api.Data;
using AuditorioTickets.Api.Services;
using AuditorioTickets.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AuditorioTickets.Api.BackgroundServices;

/// <summary>
/// Barre periódicamente los boletos que quedaron en PendientePago (checkout abandonado)
/// y libera el cupo que tenían reservado, para que vuelva a estar disponible a la venta.
/// </summary>
public class ExpiracionBoletosService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ExpiracionBoletosOptions _options;
    private readonly ILogger<ExpiracionBoletosService> _logger;

    public ExpiracionBoletosService(
        IServiceScopeFactory scopeFactory,
        IOptions<ExpiracionBoletosOptions> options,
        ILogger<ExpiracionBoletosService> logger)
    {
        // Ojo: acá NO se puede inyectar ApplicationDbContext ni IEventoService.
        // Este servicio es Singleton y esos son Scoped → habría un "captive dependency"
        // (un DbContext vivo para siempre, compartido entre corridas y no thread-safe).
        // Por eso inyectamos la fábrica de scopes y creamos uno nuevo en cada corrida.
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Habilitado)
        {
            _logger.LogInformation("Servicio de expiración de boletos deshabilitado por configuración.");
            return;
        }

        _logger.LogInformation(
            "Servicio de expiración iniciado. Intervalo: {Intervalo} min | Tolerancia: {Tolerancia} min",
            _options.IntervaloMinutos, _options.ToleranciaMinutos);

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(_options.IntervaloMinutos));

        try
        {
            // do/while: corre una vez apenas arranca la API y después en cada tick.
            do
            {
                try
                {
                    await ExpirarBoletosVencidosAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // Nunca dejamos que una excepción mate el loop: si una corrida falla,
                    // la logueamos y esperamos tranquilamente la siguiente.
                    _logger.LogError(ex, "Error durante la corrida de expiración de boletos.");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Servicio de expiración detenido (shutdown de la aplicación).");
        }
    }

    private async Task ExpirarBoletosVencidosAsync(CancellationToken ct)
    {
        // Scope manual: acá nacen y mueren el DbContext y el EventoService de esta corrida.
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var eventoService = scope.ServiceProvider.GetRequiredService<IEventoService>();

        var limite = DateTime.UtcNow.AddMinutes(-_options.ToleranciaMinutos);

        var vencidos = await context.Boletos
            .Where(b => b.Estado == EstadoBoleto.PendientePago && b.FechaCreacion < limite)
            .ToListAsync(ct);

        if (vencidos.Count == 0)
        {
            _logger.LogDebug("Corrida de expiración: no hay boletos vencidos.");
            return;
        }

        // Paso 1: marcar como Expirado.
        foreach (var boleto in vencidos)
            boleto.Estado = EstadoBoleto.Expirado;

        await context.SaveChangesAsync(ct);

        // Paso 2: liberar los cupos, agrupando por evento (una sola operación por evento).
        // El orden importa: si el proceso se cae entre el paso 1 y el 2, perdemos un cupo
        // (queda reservado de más). Al revés, liberaríamos cupo de boletos que todavía
        // figuran pendientes y podríamos sobrevender. Siempre conviene fallar hacia el lado
        // conservador: mejor un asiento vacío que un asiento vendido dos veces.
        foreach (var grupo in vencidos.GroupBy(b => b.EventoId))
        {
            await eventoService.LiberarCupoAsync(grupo.Key, grupo.Count());

            _logger.LogInformation(
                "Expirados {Cantidad} boleto(s) del evento {EventoId}; cupos liberados.",
                grupo.Count(), grupo.Key);
        }
    }
}