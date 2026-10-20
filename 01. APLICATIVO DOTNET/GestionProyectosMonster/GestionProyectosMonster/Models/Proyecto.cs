namespace GestionProyectosMonster.Models;

public class Proyecto
{
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string? DepartamentoCodigo { get; set; }
    public string? DepartamentoDescripcion { get; set; }
}
