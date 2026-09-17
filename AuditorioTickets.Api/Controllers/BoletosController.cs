using AuditorioTickets.Api.Data;
using AuditorioTickets.Api.Domain;
using AuditorioTickets.Api.Services;
using AuditorioTickets.Shared.Dtos;
using AuditorioTickets.Shared.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuditorioTickets.Api.Controllers;

[ApiController]
[Route("api/boletos")]
public class BoletosController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IEventoService _eventoService;
    private readonly IMercadoPagoService _mercadoPago;
    private readonly ILogger<BoletosController> _logger;

    public BoletosController(
        ApplicationDbContext context,
        IEventoService eventoService,
        IMercadoPagoService mercadoPago,
        ILogger<BoletosController> logger)
    {
        _context = context;
        _eventoService = eventoService;
        _mercadoPago = mercadoPago;
        _logger = logger;
    }

    /// <summary>
    /// Punto de entrada de la compra. Reserva cupo de forma atómica, crea el Boleto
    /// en estado PendientePago y devuelve el link de pago (InitPoint) de MercadoPago.
    /// </summary>
    [HttpPost("iniciar-compra")]
    public async Task<ActionResult<IniciarCompraResponseDto>> IniciarCompra(IniciarCompraDto dto)
    {
        var evento = await _eventoService.ObtenerPorIdAsync(dto.EventoId);
        if (evento is null)
            return NotFound(new { mensaje = "El evento no existe." });

        if (evento.FechaEvento < DateTime.UtcNow)
            return BadRequest(new { mensaje = "El evento ya finalizó." });

        // 1) Reserva atómica de cupo (usa el ConcurrencyStamp de la sección 0).
        var reservoCupo = await _eventoService.IntentarReservarCupoAsync(dto.EventoId);
        if (!reservoCupo)
            return Conflict(new { mensaje = "No quedan entradas disponibles para este evento." });

        // 2) Boleto pendiente de pago, ANTES de tocar la pasarela.
        var boleto = new Boleto
        {
            EventoId = evento.Id,
            CompradorNombre = dto.CompradorNombre,
            CompradorEmail = dto.CompradorEmail,
            Estado = EstadoBoleto.PendientePago,
            PrecioPagado = evento.Precio
        };

        _context.Boletos.Add(boleto);
        await _context.SaveChangesAsync();

        // 3) Recién acá se genera la Preference de MercadoPago.
        try
        {
            var (preferenceId, initPoint) = await _mercadoPago.CrearPreferenceAsync(
                boleto.Id, evento.Titulo, evento.Precio, dto.CompradorEmail);

            boleto.MercadoPagoPreferenceId = preferenceId;
            await _context.SaveChangesAsync();

            return Ok(new IniciarCompraResponseDto { BoletoId = boleto.Id, InitPoint = initPoint });
        }
        catch (Exception ex)
        {
            // Si MercadoPago falla, deshacemos boleto + cupo reservado.
            _logger.LogError(ex, "Error creando preference para boleto {BoletoId}", boleto.Id);

            _context.Boletos.Remove(boleto);
            await _context.SaveChangesAsync();
            await _eventoService.LiberarCupoAsync(evento.Id);

            return StatusCode(502, new { mensaje = "No se pudo iniciar el pago. Intentá nuevamente." });
        }
    }

    /// <summary>
    /// Usado por la página de "compra exitosa" del frontend para pollear el estado
    /// del boleto (el webhook puede llegar unos segundos después del redirect).
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BoletoDto>> ObtenerBoleto(Guid id)
    {
        var boleto = await _context.Boletos
            .Include(b => b.Evento)
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id);

        if (boleto is null) return NotFound();

        return Ok(new BoletoDto
        {
            Id = boleto.Id,
            EventoId = boleto.EventoId,
            TituloEvento = boleto.Evento.Titulo,
            CompradorNombre = boleto.CompradorNombre,
            CompradorEmail = boleto.CompradorEmail,
            Estado = boleto.Estado,
            FechaCreacion = boleto.FechaCreacion,
            FechaConfirmacion = boleto.FechaConfirmacion,
            CodigoQrBase64 = boleto.CodigoQrBase64
        });
    }
}