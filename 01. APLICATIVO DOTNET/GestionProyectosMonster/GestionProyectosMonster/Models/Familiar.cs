namespace GestionProyectosMonster.Models;

public class Familiar
{
    public string Codigo { get; set; } = "";
    public string CodigoEmpleado { get; set; } = "";
    public string ParentescoCodigo { get; set; } = "";
    public string? ParentescoDescripcion { get; set; }
    public string SexoCodigo { get; set; } = "M";
    public string? SexoDescripcion { get; set; }
    public string Nombre { get; set; } = "";
    public string Apellido { get; set; } = "";
    public DateTime FechaNacimiento { get; set; }
}
