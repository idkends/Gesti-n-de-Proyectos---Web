namespace GestionProyectosMonster.Models;

public class Empleado
{
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Apellido { get; set; } = "";
    public string Cedula { get; set; } = "";
    public string Email { get; set; } = "";
    public string Telefono { get; set; } = "";
    public string DepartamentoCodigo { get; set; } = "";
    public string CargoCodigo { get; set; } = "";
    public string SexoCodigo { get; set; } = "M";
    public string? EstadoCivilCodigo { get; set; }
    public DateTime FechaNacimiento { get; set; }
    public DateTime FechaSalida { get; set; }
    public string Direccion { get; set; } = "";
    public string Pasaporte { get; set; } = "N/A";
    public int CargosFamiliares { get; set; }
    public string? FotoRuta { get; set; }

    // Campos descriptivos (JOIN)
    public string? DepartamentoDescripcion { get; set; }
    public string? CargoDescripcion { get; set; }
    public string? SexoDescripcion { get; set; }
    public string? EstadoCivilDescripcion { get; set; }

    public string NombreCompleto => $"{Apellido}, {Nombre}";
}
