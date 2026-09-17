using System.Security.Cryptography;
using System.Text;
using AuditorioTickets.Api.Data;
using AuditorioTickets.Api.Services;
using AuditorioTickets.Shared.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuditorioTickets.Api.Controllers;

[ApiController]
[Route("api/webhook")]
public class WebhookController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IMercadoPagoService _mercadoPago;
    private readonly IEventoService _eventoService;
    private readonly IQrService _qrService;
    private readonly IEmailService _emailService; // <--- Inyectamos el servicio de correo real
    private readonly ILogger<WebhookController> _logger;
    private readonly string? _webhookSecret;

    public WebhookController(
        ApplicationDbContext context,
        IMercadoPagoService mercadoPago,
        IEventoService eventoService,
        IQrService qrService,
        IEmailService emailService, // <--- Lo recibimos en el constructor
        IConfiguration config,
        ILogger<WebhookController> logger)
    {
        _context = context;
        _mercadoPago = mercadoPago;
        _eventoService = eventoService;
        _qrService = qrService;
        _emailService = emailService;
        _logger = logger;
        _webhookSecret = config["MercadoPago:WebhookSecret"];
    }

    [HttpPost("mercadopago")]
    public async Task<IActionResult> RecibirNotificacion(
        [FromQuery(Name = "type")] string? type,
        [FromQuery(Name = "topic")] string? topic,
        [FromQuery(Name = "data.id")] string? dataId,
        [FromQuery(Name = "id")] string? idLegacy)
    {
        // MercadoPago tiene dos formatos históricos de notificación; contemplamos ambos.
        var esNotificacionDePago = (type ?? topic) == "payment";
        var paymentId = dataId ?? idLegacy;

        if (!esNotificacionDePago || string.IsNullOrEmpty(paymentId))
            return Ok(); // Respondemos 200 igual para que MP no siga reintentando algo que no nos interesa.

        if (!ValidarFirmaWebhook(Request, paymentId))
        {
            _logger.LogWarning("Firma de webhook inválida para payment {PaymentId}", paymentId);
            return Unauthorized();
        }

        try
        {
            var (status, externalReference, mpPaymentId) = await _mercadoPago.ObtenerPagoAsync(paymentId);

            if (string.IsNullOrEmpty(externalReference) || !Guid.TryParse(externalReference, out var boletoId))
            {
                _logger.LogWarning("Payment {PaymentId} sin external_reference válido", paymentId);
                return Ok();
            }

            var boleto = await _context.Boletos.Include(b => b.Evento)
                .FirstOrDefaultAsync(b => b.Id == boletoId);

            if (boleto is null)
            {
                _logger.LogWarning("Boleto {BoletoId} no encontrado (payment {PaymentId})", boletoId, paymentId);
                return Ok();
            }

            // Idempotencia: si ya está pagado, no reprocesamos (MP puede reenviar el mismo evento).
            if (boleto.Estado == EstadoBoleto.Pagado)
                return Ok();

            boleto.MercadoPagoPaymentId = mpPaymentId;

            if (status == "approved")
            {
                boleto.Estado = EstadoBoleto.Pagado;
                boleto.FechaConfirmacion = DateTime.UtcNow;
                boleto.CodigoQrBase64 = _qrService.GenerarQrBase64(boleto.Id.ToString());

                await _context.SaveChangesAsync();

                // Suma +1 a EntradasVendidas (contador separado de la reserva de cupo).
                await _eventoService.ConfirmarVentaAsync(boleto.EventoId);

                // 🔥 ENVÍO REAL DEL CORREO: Dispara el envío SMTP con el QR adjunto al comprador
                try
                {
                    await _emailService.EnviarEntradaAsync(
                        emailDestino: boleto.CompradorEmail,
                        nombreComprador: boleto.CompradorNombre, // Ajustá esta propiedad si en tu modelo se llama distinto (ej: NombreComprador)
                        tituloEvento: boleto.Evento.Titulo,
                        boletoId: boleto.Id
                    );

                    _logger.LogInformation("📧 Email con entrada enviado exitosamente a {Email} para el boleto {BoletoId}", boleto.CompradorEmail, boleto.Id);
                }
                catch (Exception mailEx)
                {
                    // Registramos el error de correo pero no rompemos el flujo del webhook para que MP no repita la notificación si el pago ya se aprobó
                    _logger.LogError(mailEx, "Error al enviar el email de confirmación para el boleto {BoletoId}", boleto.Id);
                }
            }
            else if (status is "rejected" or "cancelled")
            {
                boleto.Estado = EstadoBoleto.Cancelado;
                await _context.SaveChangesAsync();
                await _eventoService.LiberarCupoAsync(boleto.EventoId); // libera el cupo reservado
            }
            else
            {
                // "pending", "in_process", etc. — no hacemos nada más, esperamos la próxima notificación.
                await _context.SaveChangesAsync();
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error procesando webhook de payment {PaymentId}", paymentId);
            // Devolvemos 500 a propósito: así MercadoPago reintenta la notificación más tarde.
            return StatusCode(500);
        }
    }

    /// <summary>
    /// Valida el header x-signature según el algoritmo documentado por MercadoPago
    /// (HMAC-SHA256 sobre un manifest con id + request-id + timestamp).
    /// Si no hay secreto configurado, se omite (solo recomendado en desarrollo local).
    /// </summary>
    private bool ValidarFirmaWebhook(HttpRequest request, string dataId)
    {
        if (string.IsNullOrEmpty(_webhookSecret))
            return true;

        if (!request.Headers.TryGetValue("x-signature", out var xSignature) ||
            !request.Headers.TryGetValue("x-request-id", out var xRequestId))
            return false;

        string? ts = null, v1 = null;
        foreach (var part in xSignature.ToString().Split(','))
        {
            var kv = part.Split('=', 2);
            if (kv.Length != 2) continue;
            if (kv[0].Trim() == "ts") ts = kv[1].Trim();
            if (kv[0].Trim() == "v1") v1 = kv[1].Trim();
        }

        if (ts is null || v1 is null) return false;

        var manifest = $"id:{dataId.ToLower()};request-id:{xRequestId};ts:{ts};";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_webhookSecret));
        var hashHex = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(manifest))).ToLower();

        return hashHex == v1;
    }
}