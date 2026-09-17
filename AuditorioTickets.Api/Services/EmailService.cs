using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using QRCoder;

namespace AuditorioTickets.Api.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task EnviarEntradaAsync(string emailDestino, string nombreComprador, string tituloEvento, Guid boletoId)
    {
        try
        {
            // 1. Generar el código QR con QRCoder
            byte[] qrBytes;
            using (var qrGenerator = new QRCodeGenerator())
            {
                using var qrCodeData = qrGenerator.CreateQrCode(boletoId.ToString(), QRCodeGenerator.ECCLevel.Q);
                using var qrCode = new PngByteQRCode(qrCodeData);
                qrBytes = qrCode.GetGraphic(20);
            }

            // 2. Configurar el correo electrónico
            var smtpServer = _config["Smtp:Server"] ?? "smtp.gmail.com";
            var smtpPort = int.Parse(_config["Smtp:Port"] ?? "587");
            var smtpUser = _config["Smtp:User"];
            var smtpPass = _config["Smtp:Password"];
            var remitenteNombre = _config["Smtp:SenderName"] ?? "Auditorio UNAJ";

            var mensaje = new MailMessage();
            mensaje.From = new MailAddress(smtpUser!, remitenteNombre);
            mensaje.To.Add(emailDestino);
            mensaje.Subject = $"¡Tu entrada para {tituloEvento}!";
            
            mensaje.Body = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px; background-color: #f7f9fc; border-radius: 8px;'>
                    <h2 style='color: #2980b9;'>¡Hola, {nombreComprador}!</h2>
                    <p>Tu pago para el evento <strong>{tituloEvento}</strong> ha sido confirmado con éxito.</p>
                    <p>Adjunto a este correo encontrarás el código QR de tu entrada.</p>
                    <div style='background: #fff; padding: 15px; border-radius: 6px; display: inline-block; border: 1px solid #e1e8ed; margin-top: 10px;'>
                        <p style='margin: 0; font-size: 12px; color: #7f8c8d;'>ID de Entrada: {boletoId}</p>
                    </div>
                    <p style='margin-top: 20px; font-size: 14px; color: #34495e;'>Presentá este código QR en la entrada del auditorio el día del evento.</p>
                </div>";
            mensaje.IsBodyHtml = true;

            // 3. Adjuntar la imagen del QR
            using var ms = new MemoryStream(qrBytes);
            var attachment = new Attachment(ms, $"Entrada-{boletoId}.png", MediaTypeNames.Image.Png);
            mensaje.Attachments.Add(attachment);

            // 4. Enviar mediante SMTP
            using var smtpClient = new SmtpClient(smtpServer, smtpPort)
            {
                Credentials = new NetworkCredential(smtpUser, smtpPass),
                EnableSsl = true
            };

            await smtpClient.SendMailAsync(mensaje);
            _logger.LogInformation("Correo con entrada enviado exitosamente a {Email}", emailDestino);
        }
        catch (Exception ex) // <--- ¡Acá estaba el detalle faltante!
        {
            _logger.LogError(ex, "Error al enviar el correo con la entrada a {Email}", emailDestino);
            throw;
        }
    }
}