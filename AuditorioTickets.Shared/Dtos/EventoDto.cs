namespace AuditorioTickets.Shared.Dtos;

public class EventoDto
{
    public Guid Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Lugar { get; set; } = string.Empty;
    public DateTime FechaEvento { get; set; }
    public int CapacidadMaxima { get; set; }
    public int EntradasVendidas { get; set; }
    public int EntradasReservadas { get; set; }
    public decimal Precio { get; set; }

    public int CupoDisponible => CapacidadMaxima - EntradasReservadas;
}

public class CrearEventoDto
{
    public string Titulo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Lugar { get; set; } = string.Empty;
    public DateTime FechaEvento { get; set; }
    public int CapacidadMaxima { get; set; }
    public decimal Precio { get; set; }
}