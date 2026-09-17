using System.Net;
using System.Net.Http.Json;
using AuditorioTickets.Shared.Dtos;

namespace AuditorioTickets.Client.Services;

public class BoletoApiService
{
    private readonly HttpClient _http;
    public BoletoApiService(HttpClient http) => _http = http;

    public async Task<(bool Exito, IniciarCompraResponseDto? Datos, string? Error)> IniciarCompraAsync(IniciarCompraDto dto)
    {
        var respuesta = await _http.PostAsJsonAsync("api/boletos/iniciar-compra", dto);

        if (respuesta.IsSuccessStatusCode)
            return (true, await respuesta.Content.ReadFromJsonAsync<IniciarCompraResponseDto>(), null);

        if (respuesta.StatusCode == HttpStatusCode.Conflict)
            return (false, null, "No quedan entradas disponibles para este evento.");

        return (false, null, "No se pudo iniciar la compra. Intentá nuevamente.");
    }

    public async Task<BoletoDto?> ObtenerBoletoAsync(Guid id) =>
        await _http.GetFromJsonAsync<BoletoDto>($"api/boletos/{id}");
}