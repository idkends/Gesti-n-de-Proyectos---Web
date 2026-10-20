# GestionProyectosMonster — MongoDB

Esta es la base de datos del proyecto, construida sobre MongoDB (`MongoDB.Driver`).
El acceso a datos se realiza mediante DAOs que trabajan directamente contra
colecciones y documentos, sin ORM ni mapeo de clases con atributos Bson.

## 1. Puesta en marcha

### Opción A: MongoDB local (mongod)

1. Instala MongoDB Community Server (https://www.mongodb.com/try/download/community).
2. Verifica que el servicio esté escuchando en `localhost:27017` (puedes
   confirmarlo conectándote con MongoDB Compass a `mongodb://localhost:27017`).
3. La base de datos `GestionProyectosMonster` se crea automáticamente al
   insertar el primer documento (no requiere un `CREATE DATABASE` explícito).
4. Ejecuta los scripts de este folder con `mongosh`:
   ```
   mongosh "mongodb://localhost:27017" Database/MongoDB/init-indexes.js
   mongosh "mongodb://localhost:27017" Database/MongoDB/seed-data.js
   ```

### Opción B: MongoDB Atlas (nube)

1. Crea un cluster gratuito en https://www.mongodb.com/atlas
2. Crea un usuario de base de datos y agrega tu IP a la lista blanca (o
   `0.0.0.0/0` para pruebas).
3. Copia el connection string (`mongodb+srv://usuario:password@cluster.../`).
4. Actualiza `appsettings.json` (o mejor, `appsettings.Development.json` /
   variables de entorno / user-secrets para no commitear credenciales):
   ```json
   "MongoDB": {
     "ConnectionString": "mongodb+srv://usuario:password@cluster0.xxxxx.mongodb.net/",
     "DatabaseName": "GestionProyectosMonster"
   }
   ```
5. Ejecuta los mismos scripts `mongosh` apuntando al connection string de Atlas:
   ```
   mongosh "mongodb+srv://usuario:password@cluster0.xxxxx.mongodb.net/" Database/MongoDB/init-indexes.js
   mongosh "mongodb+srv://usuario:password@cluster0.xxxxx.mongodb.net/" Database/MongoDB/seed-data.js
   ```

### Ejecutar la aplicación

```
cd GestionProyectosMonster
dotnet run
```

Usuarios de prueba:

| Empleado | Password              | Perfil                |
|----------|------------------------|------------------------|
| EMP001   | AdminMonster2026        | Administrador          |
| EMP002   | RrhhMonster2026         | Recursos Humanos       |
| EMP003   | JefeMonster2026         | Jefe de Departamento   |
| EMP004   | EmpleadoMonster2026     | Empleado               |

## 2. Esquema de colecciones

El modelo es de documentos denormalizados: las relaciones 1:N fuertemente
ligadas a su padre (cargos de un departamento, educación/familiares de un
empleado, empleados asignados a un proyecto, perfiles de un usuario,
opciones de un perfil) se embeben como subdocumento/array dentro del
documento padre. Las relaciones entre agregados (empleado ↔ departamento,
proyecto ↔ departamento, registroHoras ↔ empleado/proyecto) se referencian
por código de negocio (string), por ejemplo `"EMP001"`, `"P01"`, `"PERF0001"`.

### `departamentos`
```jsonc
{
  _id: "001",
  descripcion: "Administración",
  cargos: [
    { codigo: "ADM", descripcion: "Administrador General" }
  ]
}
```

### `empleados`
```jsonc
{
  _id: "EMP001",
  nombre: "Marco Adrian",
  apellido: "Padilla Triviño",
  cedula: "1718457072",
  email: "marco.padilla@monster.com",
  telefono: "0955046792",
  departamentoCodigo: "001",
  cargoCodigo: "ADM",
  sexoCodigo: "M",              // M/F/H (catálogo fijo en código, no hay colección)
  estadoCivilCodigo: "C",       // referencia a estadosCiviles._id (nullable)
  fechaNacimiento: ISODate(...),
  fechaSalida: ISODate(...),
  direccion: "Quito - Av. Amazonas 123",
  pasaporte: "N/A",
  cargosFamiliares: 0,          // contador denormalizado = familiares.length
  fotoRuta: null,
  educacion: [
    { codigo: "EDU001", titulo: "...", institucion: "...", fechaGrado: ISODate, fechaInicio: ISODate }
  ],
  familiares: [
    { codigo: "FAM001", parentescoCodigo: "PRT001", sexoCodigo: "M",
      nombre: "...", apellido: "...", fechaNacimiento: ISODate, fechaActualizacion: ISODate }
  ]
}
```

### `proyectos`
```jsonc
{
  _id: "P01",
  nombre: "Sistema de Gestión de Personal",
  departamentoCodigo: "003",   // nullable
  empleadosAsignados: [
    { empleadoCodigo: "EMP003", horasEstimadas: 40.0 }
  ]
}
```

### `registroHoras`
```jsonc
{
  _id: "TI0001",
  empleadoCodigo: "EMP003",
  proyectoCodigo: "P01",
  fechaSemana: ISODate(...), // lunes de la semana
  horas: 8.0,
  estado: "P",                // P=Pendiente, A=Aprobado, R=Rechazado
  observaciones: null,
  fechaCreacion: ISODate(...),
  fechaModificacion: ISODate(...)
}
```

### `usuarios`
Un documento por empleado, con el historial de asignaciones de perfil
embebido.
```jsonc
{
  _id: "EMP001",
  passwordHash: "0044987c...",
  estadoCodigo: "A",           // referencia a estadosUsuario._id
  fechaCreacion: ISODate(...),
  fechaModificacion: ISODate(...),
  pieFirma: "Administrador del Sistema",
  perfilesAsignados: [
    { perfilCodigo: "PERF0001", fechaAsignacion: ISODate(...), fechaRetiro: null }
  ]
}
```
El perfil activo es el elemento de `perfilesAsignados` con `fechaRetiro == null`
y `fechaAsignacion` más reciente.

### `perfiles`
```jsonc
{
  _id: "PERF0001",
  descripcion: "Administrador",
  opcionesAsignadas: [
    { opcionCodigo: "001", fechaAsignacion: ISODate(...), fechaRetiro: null }
  ]
}
```

### `opciones` (catálogo)
```jsonc
{
  _id: "001",
  sistemaCodigo: "1",
  sistemaDescripcion: "Personal",
  descripcion: "Inicio"
}
```

### Catálogos simples
- `estadosCiviles`: `{ _id: "S", descripcion: "Soltero/a" }`
- `parentescos`: `{ _id: "PRT001", descripcion: "Padre" }`
- `estadosUsuario`: `{ _id: "A", descripcion: "Activo" }`

`ListarSexos()` en `DAOEmpleado` está hardcodeado en C# (M/F/H), por lo que
no existe una colección `sexos`.

### `counters`
Genera códigos secuenciales (`EMP001`, `EDU001`, `FAM001`, `P01`, `TI0001`)
de forma atómica con `findOneAndUpdate` + `$inc`.
```jsonc
{ _id: "empleado", seq: 4 }
{ _id: "educacion", seq: 0 }
{ _id: "familiar", seq: 0 }
{ _id: "proyecto", seq: 3 }
{ _id: "registroHoras", seq: 0 }
```

## 3. Índices recomendados

Ver `init-indexes.js` para el script ejecutable. Resumen:

| Colección       | Índice                                              | Motivo                                    |
|------------------|------------------------------------------------------|--------------------------------------------|
| empleados        | `departamentoCodigo`, `cargoCodigo`                  | filtros por departamento/cargo             |
| empleados        | `cedula` (unique)                                    | integridad, búsquedas                      |
| proyectos        | `departamentoCodigo`                                 | reportes por departamento                  |
| proyectos        | `empleadosAsignados.empleadoCodigo`                  | `ListarPorEmpleado`                        |
| registroHoras    | `empleadoCodigo + proyectoCodigo + fechaSemana`      | `ListarSemanaEmpleado` / `GuardarSemana`   |
| registroHoras    | `proyectoCodigo`, `empleadoCodigo`, `estado`         | reportes, aprobación                       |
| usuarios         | `perfilesAsignados.perfilCodigo/fechaRetiro`         | resolver perfil activo                     |
| perfiles         | `opcionesAsignadas.opcionCodigo/fechaRetiro`         | `GetCodigosParaPerfil`                     |

## 4. Decisiones de diseño

- **`usuarios._id = PEEMP_CODIGO`**: un documento por empleado, ya que el
  sistema solo necesita la contraseña vigente. Al cambiar la contraseña se
  reemplaza `passwordHash` y se reinicia `perfilesAsignados` con el perfil
  activo anterior.
- **Sin transacciones multi-documento**: las operaciones de
  `InsertarPerfil` / `CambiarPerfil` / `CambiarPassword` se resuelven con
  operaciones secuenciales sobre el documento `usuarios` (que ya es un solo
  documento con el array embebido `perfilesAsignados`), evitando la
  necesidad de un replica set para levantar el proyecto con un `mongod`
  standalone simple.
- **`registroHoras`** arranca vacía; los registros se generan desde la
  aplicación a medida que los empleados cargan sus horas semanales.
