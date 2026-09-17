using System.Net.Http.Json;
using AuditorioTickets.Shared.Dtos;

namespace AuditorioTickets.Client.Services;

public class EventoApiService
{
    private readonly HttpClient _http;
    
    public EventoApiService(HttpClient http) => _http = http;

    public async Task<List<EventoDto>> ObtenerEventosAsync() =>
        await _http.GetFromJsonAsync<List<EventoDto>>("api/eventos") ?? new();

    public async Task<List<EventoDto>> ObtenerDashboardAsync() =>
        await _http.GetFromJsonAsync<List<EventoDto>>("api/eventos/dashboard") ?? new();

    public async Task<(bool Exito, string? Error)> CrearEventoAsync(CrearEventoDto dto)
    {
        var respuesta = await _http.PostAsJsonAsync("api/eventos", dto);
        if (respuesta.IsSuccessStatusCode) return (true, null);

        var error = await respuesta.Content.ReadAsStringAsync();
        return (false, string.IsNullOrWhiteSpace(error) ? "No se pudo crear el evento." : error);
    }

    // --- NUEVOS MÉTODOS DE ACTUALIZACIÓN Y ELIMINACIÓN ---
    
    public async Task<(bool Exito, string? Error)> ActualizarEventoAsync(Guid id, CrearEventoDto dto)
    {
        var respuesta = await _http.PutAsJsonAsync($"api/eventos/{id}", dto);
        if (respuesta.IsSuccessStatusCode) return (true, null);

        var error = await respuesta.Content.ReadAsStringAsync();
        return (false, string.IsNullOrWhiteSpace(error) ? "No se pudo actualizar el evento." : error);
    }

    public async Task<(bool Exito, string? Error)> EliminarEventoAsync(Guid id)
    {
        var respuesta = await _http.DeleteAsync($"api/eventos/{id}");
        if (respuesta.IsSuccessStatusCode) return (true, null);

        var error = await respuesta.Content.ReadAsStringAsync();
        return (false, string.IsNullOrWhiteSpace(error) ? "No se pudo eliminar el evento." : error);
    }

    // --- MÉTODOS DE BÚSQUEDA A PRUEBA DE BALAS ---

    // 1. Por si tu base de datos y modelo usan Guid
    public async Task<EventoDto?> ObtenerPorIdAsync(Guid id) =>
        await _http.GetFromJsonAsync<EventoDto>($"api/eventos/{id}");

    // 2. Por si Claude generó la vista asumiendo que el ID es un número (int)
    public async Task<EventoDto?> ObtenerPorIdAsync(int id) =>
        await _http.GetFromJsonAsync<EventoDto>($"api/eventos/{id}");

    // 3. Por si Blazor está capturando el ID de la URL como texto plano (string)
    public async Task<EventoDto?> ObtenerPorIdAsync(string id) =>
        await _http.GetFromJsonAsync<EventoDto>($"api/eventos/{id}");
}