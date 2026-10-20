namespace GestionProyectosMonster.Models;

public class Usuario
{
    public const string ROL_ADMINISTRADOR     = "Administrador";
    public const string ROL_RRHH              = "RR. HH.";
    public const string ROL_JEFE_DEPARTAMENTO = "Jefe de Departamento";
    public const string ROL_EMPLEADO          = "Empleado";

    public string CodigoEmpleado { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string EstadoCodigo { get; set; } = "A";
    public string? PerfilCodigo { get; set; }
    public string? PerfilDescripcion { get; set; }
    public string RolUsuario { get; set; } = ROL_EMPLEADO;
    public string? DepartamentoCodigo { get; set; }
    public string? CargoCodigo { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaModificacion { get; set; }
    public string PieFirma { get; set; } = "";
    public string NombreEmpleado { get; set; } = "";
    public string? EstadoDescripcion { get; set; }
    public string? FotoRuta { get; set; }

    public bool EsAdministrador     => RolUsuario == ROL_ADMINISTRADOR;
    public bool EsRRHH              => RolUsuario == ROL_RRHH;
    public bool EsJefeDepartamento  => RolUsuario == ROL_JEFE_DEPARTAMENTO;
    public bool EsEmpleado          => RolUsuario == ROL_EMPLEADO;

    public bool TieneRol(params string[] roles) =>
        roles?.Contains(RolUsuario) == true;
}
