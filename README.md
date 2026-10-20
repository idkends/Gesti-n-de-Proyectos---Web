# Gestión de Proyectos — CRUD con MVC, .NET 8 y MongoDB

Aplicación web de gestión de proyectos, empleados y horas trabajadas, construida con **ASP.NET Core MVC (.NET 8)** y **MongoDB** (`MongoDB.Driver`). El acceso a datos se hace con DAOs que trabajan directamente sobre colecciones, sin ORM.

## Funcionalidades

- Inicio de sesión con perfiles y opciones de menú asignables por perfil.
- Gestión de empleados (con datos familiares y de educación), departamentos y organigrama.
- Gestión de proyectos y asignación de personal.
- Registro, aprobación y reporte de horas.
- Administración de usuarios, contraseñas y perfiles.
- Reportes.

Perfiles incluidos en los datos de prueba: Administrador, Recursos Humanos, Jefe de Departamento y Empleado.

## Estructura del repositorio

| Carpeta | Contenido |
|---|---|
| `01. APLICATIVO DOTNET/` | Solución de Visual Studio y código de la aplicación |
| `02. DOCUMENTACIÓN/` | Documento del proyecto (.docx) |
| `03. BDD/` | Scripts de MongoDB (índices y datos iniciales) y descripción del esquema |

## Requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [MongoDB Community Server](https://www.mongodb.com/try/download/community) escuchando en `localhost:27017` (o un cluster de Atlas)
- [mongosh](https://www.mongodb.com/try/download/shell) para cargar los scripts

## Puesta en marcha

1. Desde la raíz del repositorio, crea los índices y carga los datos de prueba:

   ```
   mongosh "mongodb://localhost:27017" "03. BDD/init-indexes.js"
   mongosh "mongodb://localhost:27017" "03. BDD/seed-data.js"
   ```

   El script de datos borra y recrea las colecciones que rellena, así que úsalo solo en desarrollo.

2. Ejecuta la aplicación:

   ```
   cd "01. APLICATIVO DOTNET/GestionProyectosMonster/GestionProyectosMonster"
   dotnet run
   ```

   Queda disponible en `http://localhost:5000` (o `https://localhost:7000` con el perfil `https`).

La conexión se configura en `appsettings.json`:

```json
"MongoDB": {
  "ConnectionString": "mongodb://localhost:27017",
  "DatabaseName": "GestionProyectosMonster"
}
```

Si usas MongoDB Atlas, no escribas el usuario y la contraseña en este archivo: usa [user-secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) o variables de entorno (`MongoDB__ConnectionString`).

## Usuarios de prueba

Cuentas ficticias creadas por el script de datos, solo para desarrollo:

| Empleado | Contraseña | Perfil |
|---|---|---|
| EMP001 | AdminMonster2026 | Administrador |
| EMP002 | RrhhMonster2026 | Recursos Humanos |
| EMP003 | JefeMonster2026 | Jefe de Departamento |
| EMP004 | EmpleadoMonster2026 | Empleado |

Los datos personales (cédulas, teléfonos, correos) de los datos de prueba son inventados. Si despliegas la aplicación fuera de tu equipo, cambia estas contraseñas.

## Base de datos

El esquema de colecciones, los índices y las decisiones de diseño están descritos en [03. BDD/README.md](03.%20BDD/README.md).
