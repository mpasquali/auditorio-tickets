using AuditorioTickets.Api.Domain;
using AuditorioTickets.Api.Services;
using AuditorioTickets.Shared.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuditorioTickets.Api.Controllers;

[ApiController]
[Route("api/eventos")]
public class EventosController : ControllerBase
{
    private readonly IEventoService _eventoService;

    public EventosController(IEventoService eventoService) => _eventoService = eventoService;

    // Público: listado para la vitrina de eventos
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<EventoDto>>> ObtenerTodos()
    {
        var eventos = await _eventoService.ObtenerTodosAsync();
        return Ok(eventos.Select(MapearDto).ToList());
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<EventoDto>> ObtenerPorId(Guid id)
    {
        var evento = await _eventoService.ObtenerPorIdAsync(id);
        return evento is null ? NotFound() : Ok(MapearDto(evento));
    }

    // Protegido: solo el admin puede crear eventos
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<EventoDto>> Crear(CrearEventoDto dto)
    {
        var evento = new Evento
        {
            Titulo = dto.Titulo,
            Descripcion = dto.Descripcion,
            Lugar = dto.Lugar,
            FechaEvento = dto.FechaEvento,
            CapacidadMaxima = dto.CapacidadMaxima,
            Precio = dto.Precio
        };

        var creado = await _eventoService.CrearAsync(evento);
        return CreatedAtAction(nameof(ObtenerPorId), new { id = creado.Id }, MapearDto(creado));
    }

    // Protegido: actualizar un evento existente
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Actualizar(Guid id, CrearEventoDto dto)
    {
        var eventoExistente = await _eventoService.ObtenerPorIdAsync(id);
        if (eventoExistente is null) 
        {
            return NotFound();
        }

        eventoExistente.Titulo = dto.Titulo;
        eventoExistente.Descripcion = dto.Descripcion;
        eventoExistente.Lugar = dto.Lugar;
        eventoExistente.FechaEvento = dto.FechaEvento;
        eventoExistente.CapacidadMaxima = dto.CapacidadMaxima;
        eventoExistente.Precio = dto.Precio;

        await _eventoService.ActualizarAsync(eventoExistente);
        return NoContent();
    }

    // Protegido: eliminar un evento
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Eliminar(Guid id)
    {
        var eventoExistente = await _eventoService.ObtenerPorIdAsync(id);
        if (eventoExistente is null) 
        {
            return NotFound();
        }

        await _eventoService.EliminarAsync(id);
        return NoContent();
    }

    // Protegido: dashboard de ventas (reutiliza el mismo listado, con los contadores ya incluidos)
    [HttpGet("dashboard")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<EventoDto>>> Dashboard()
    {
        var eventos = await _eventoService.ObtenerTodosAsync();
        return Ok(eventos.Select(MapearDto).ToList());
    }

    private static EventoDto MapearDto(Evento e) => new()
    {
        Id = e.Id,
        Titulo = e.Titulo,
        Descripcion = e.Descripcion,
        Lugar = e.Lugar,
        FechaEvento = e.FechaEvento,
        CapacidadMaxima = e.CapacidadMaxima,
        EntradasVendidas = e.EntradasVendidas,
        EntradasReservadas = e.EntradasReservadas,
        Precio = e.Precio
    };
}