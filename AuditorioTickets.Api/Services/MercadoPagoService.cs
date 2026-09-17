using MercadoPago.Config; 
using MercadoPago.Client.Payment;
using MercadoPago.Client.Preference;

namespace AuditorioTickets.Api.Services;

public class MercadoPagoService : IMercadoPagoService
{
    private readonly IConfiguration _config;
    private readonly ILogger<MercadoPagoService> _logger;

    public MercadoPagoService(IConfiguration config, ILogger<MercadoPagoService> logger)
    {
        _config = config;
        _logger = logger;
        
        // 🔥 INICIALIZACIÓN CLAVE: Autenticamos el SDK con tu cuenta
        MercadoPagoConfig.AccessToken = _config["MercadoPago:AccessToken"];
    }

    public async Task<(string PreferenceId, string InitPoint)> CrearPreferenceAsync(
        Guid boletoId, string tituloEvento, decimal precio, string emailComprador)
    {
        var frontendUrl = _config["MercadoPago:FrontendBaseUrl"];
        if (string.IsNullOrWhiteSpace(frontendUrl))
        {
            frontendUrl = "http://localhost:5289"; // El puerto de tu Blazor
        }
        frontendUrl = frontendUrl.TrimEnd('/');

        var webhookUrl = _config["MercadoPago:WebhookBaseUrl"];
        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            webhookUrl = "https://tudominio.ngrok.io"; // URL genérica temporal
        }
        webhookUrl = webhookUrl.TrimEnd('/');

        var request = new PreferenceRequest
        {
            Items = new List<PreferenceItemRequest>
            {
                new PreferenceItemRequest
                {
                    Title = $"Entrada - {tituloEvento}",
                    Quantity = 1,
                    CurrencyId = "ARS", // Moneda en Pesos Argentinos
                    UnitPrice = precio
                }
            },
            Payer = new PreferencePayerRequest { Email = emailComprador },
            ExternalReference = boletoId.ToString(), // Clave para vincular el pago con el Boleto de la UNAJ en tu webhook
            NotificationUrl = $"{webhookUrl}/api/webhook/mercadopago",
            BackUrls = new PreferenceBackUrlsRequest
            {
                Success = $"{frontendUrl}/compra/exito/{boletoId}",
                Pending = $"{frontendUrl}/compra/pendiente/{boletoId}",
                Failure = $"{frontendUrl}/compra/error/{boletoId}"
            },
            // CORRECCIÓN: Solo activamos el AutoReturn si no estamos en localhost para evitar que la API de MP rechace la petición
            AutoReturn = frontendUrl.Contains("localhost") ? null : "approved",
        };

        var client = new PreferenceClient();
        var preference = await client.CreateAsync(request);

        _logger.LogInformation("Preference {Id} creada para boleto {BoletoId}", preference.Id, boletoId);

        // Devolvemos el ID y el InitPoint (este último es el link de pago que va a usar Blazor)
        return (preference.Id, preference.InitPoint);
    }

    public async Task<(string Status, string? ExternalReference, string PaymentId)> ObtenerPagoAsync(string paymentId)
    {
        var client = new PaymentClient();
        var payment = await client.GetAsync(long.Parse(paymentId));

        return (payment.Status ?? "unknown", payment.ExternalReference, payment.Id.ToString()!);
    }
}