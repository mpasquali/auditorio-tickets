using QRCoder;

namespace AuditorioTickets.Api.Services;

public class QrService : IQrService
{
    public string GenerarQrBase64(string contenido)
    {
        using var generador = new QRCodeGenerator();
        using var qrData = generador.CreateQrCode(contenido, QRCodeGenerator.ECCLevel.Q);

        // PngByteQRCode no depende de System.Drawing: funciona en Linux/contenedores sin problema.
        var pngQr = new PngByteQRCode(qrData);
        byte[] bytes = pngQr.GetGraphic(20);

        return Convert.ToBase64String(bytes);
    }
}