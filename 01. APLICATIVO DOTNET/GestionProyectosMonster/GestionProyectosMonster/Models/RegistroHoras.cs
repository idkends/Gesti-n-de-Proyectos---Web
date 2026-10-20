namespace GestionProyectosMonster.Models;

public class RegistroHoras
{
    public string Codigo { get; set; } = "";
    public string CodigoEmpleado { get; set; } = "";
    public string CodigoProyecto { get; set; } = "";
    public DateTime FechaSemana { get; set; }
    public decimal Horas { get; set; }
    public string Estado { get; set; } = "P";
    public string? Observaciones { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaModificacion { get; set; }

    // JOIN
    public string? NombreEmpleado { get; set; }
    public string? NombreProyecto { get; set; }
    public decimal? HorasEstimadas { get; set; }

    public string EstadoDescripcion => Estado switch
    {
        "A" => "Aprobado",
        "R" => "Rechazado",
        _   => "Pendiente"
    };
}
