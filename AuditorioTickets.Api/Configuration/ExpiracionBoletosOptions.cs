namespace AuditorioTickets.Api.Configuration;

public class ExpiracionBoletosOptions
{
    /// <summary>Cada cuántos minutos corre el barrido.</summary>
    public int IntervaloMinutos { get; set; } = 5;

    /// <summary>Antigüedad mínima (en minutos) para considerar vencido un boleto PendientePago.</summary>
    public int ToleranciaMinutos { get; set; } = 15;

    /// <summary>Permite apagar el servicio sin recompilar (útil en entornos de prueba).</summary>
    public bool Habilitado { get; set; } = true;
}