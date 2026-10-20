namespace GestionProyectosMonster.Models;

public class Educacion
{
    public string Codigo { get; set; } = "";
    public string CodigoEmpleado { get; set; } = "";
    public string Titulo { get; set; } = "";
    public string Institucion { get; set; } = "";
    public DateTime FechaGrado { get; set; }
    public DateTime FechaInicio { get; set; }
}
