// ==================================================================
// seed-data.js
// Datos iniciales equivalentes a Database/GESTION_PROYECTOS_MONSTER_DML.sql
// pero en forma de documentos MongoDB, según el esquema descrito en
// README.md.
//
// Uso:
//   mongosh "mongodb://localhost:27017" Database/MongoDB/seed-data.js
//
// NOTA: este script es destructivo para las colecciones que rellena
// (usa deleteMany antes de insertar) para que se pueda ejecutar
// varias veces de forma idempotente en un entorno de desarrollo.
// ==================================================================

const db = db.getSiblingDB("GestionProyectosMonster");

// ---------------------------------------------------------------
// Catálogos simples
// ---------------------------------------------------------------

db.estadosCiviles.deleteMany({});
db.estadosCiviles.insertMany([
    { _id: "S", descripcion: "Soltero/a" },
    { _id: "C", descripcion: "Casado/a" },
    { _id: "D", descripcion: "Divorciado/a" },
    { _id: "V", descripcion: "Viudo/a" },
    { _id: "U", descripcion: "Unión Libre" }
]);

db.parentescos.deleteMany({});
db.parentescos.insertMany([
    { _id: "PRT001", descripcion: "Padre" },
    { _id: "PRT002", descripcion: "Madre" },
    { _id: "PRT003", descripcion: "Cónyuge" },
    { _id: "PRT004", descripcion: "Hijo/a" }
]);

db.estadosUsuario.deleteMany({});
db.estadosUsuario.insertMany([
    { _id: "A", descripcion: "Activo" },
    { _id: "I", descripcion: "Inactivo" }
]);

// ---------------------------------------------------------------
// Perfiles de acceso (sin opciones asignadas todavía)
// ---------------------------------------------------------------

db.perfiles.deleteMany({});
db.perfiles.insertMany([
    { _id: "PERF0001", descripcion: "Administrador", opcionesAsignadas: [] },
    { _id: "PERF0002", descripcion: "Recursos Humanos", opcionesAsignadas: [] },
    { _id: "PERF0003", descripcion: "Jefe de Departamento", opcionesAsignadas: [] },
    { _id: "PERF0004", descripcion: "Empleado", opcionesAsignadas: [] }
]);

// ---------------------------------------------------------------
// Opciones del sistema (con sistemaDescripcion denormalizado)
// ---------------------------------------------------------------

const sistemas = { "1": "Personal", "2": "Gestión de Proyectos", "3": "Seguridad" };

db.opciones.deleteMany({});
db.opciones.insertMany([
    { _id: "001", sistemaCodigo: "1", sistemaDescripcion: sistemas["1"], descripcion: "Inicio" },
    { _id: "002", sistemaCodigo: "3", sistemaDescripcion: sistemas["3"], descripcion: "Gestión de Usuarios" },
    { _id: "003", sistemaCodigo: "1", sistemaDescripcion: sistemas["1"], descripcion: "Departamentos" },
    { _id: "004", sistemaCodigo: "3", sistemaDescripcion: sistemas["3"], descripcion: "Asignar Perfiles" },
    { _id: "005", sistemaCodigo: "3", sistemaDescripcion: sistemas["3"], descripcion: "Gestión de Contraseñas" },
    { _id: "006", sistemaCodigo: "3", sistemaDescripcion: sistemas["3"], descripcion: "Asignar Opciones al Perfil" },
    { _id: "007", sistemaCodigo: "3", sistemaDescripcion: sistemas["3"], descripcion: "Cambiar Mi Contraseña" },
    { _id: "008", sistemaCodigo: "1", sistemaDescripcion: sistemas["1"], descripcion: "Gestión de Personal" },
    { _id: "009", sistemaCodigo: "1", sistemaDescripcion: sistemas["1"], descripcion: "Familiares" },
    { _id: "010", sistemaCodigo: "1", sistemaDescripcion: sistemas["1"], descripcion: "Organigrama" },
    { _id: "011", sistemaCodigo: "1", sistemaDescripcion: sistemas["1"], descripcion: "Reporte de Personal" },
    { _id: "012", sistemaCodigo: "2", sistemaDescripcion: sistemas["2"], descripcion: "Gestión de Proyectos" },
    { _id: "013", sistemaCodigo: "2", sistemaDescripcion: sistemas["2"], descripcion: "Asignación de Personal" },
    { _id: "014", sistemaCodigo: "2", sistemaDescripcion: sistemas["2"], descripcion: "Aprobación de Horas" },
    { _id: "015", sistemaCodigo: "2", sistemaDescripcion: sistemas["2"], descripcion: "Reportes de Horas" },
    { _id: "016", sistemaCodigo: "2", sistemaDescripcion: sistemas["2"], descripcion: "Registrar Horas Reales" },
    { _id: "017", sistemaCodigo: "3", sistemaDescripcion: sistemas["3"], descripcion: "Gestión de Perfiles" }
]);

// ---------------------------------------------------------------
// Permisos por perfil (opcionesAsignadas embebidas en perfiles)
// ---------------------------------------------------------------

const hoy = new Date();
function asign(codigos) {
    return codigos.map(c => ({ opcionCodigo: c, fechaAsignacion: hoy, fechaRetiro: null }));
}

db.perfiles.updateOne({ _id: "PERF0001" }, { $set: { opcionesAsignadas: asign(
    ["001","002","003","004","005","006","007","008","009","010","011","012","013","014","015","016","017"]) } });
db.perfiles.updateOne({ _id: "PERF0002" }, { $set: { opcionesAsignadas: asign(
    ["001","003","007","008","009","010","011"]) } });
db.perfiles.updateOne({ _id: "PERF0003" }, { $set: { opcionesAsignadas: asign(
    ["001","007","012","013","014","015"]) } });
db.perfiles.updateOne({ _id: "PERF0004" }, { $set: { opcionesAsignadas: asign(
    ["001","007","016"]) } });

// ---------------------------------------------------------------
// Departamentos (con cargos embebidos)
// ---------------------------------------------------------------

db.departamentos.deleteMany({});
db.departamentos.insertMany([
    { _id: "001", descripcion: "Administración", cargos: [
        { codigo: "ADM", descripcion: "Administrador General" },
        { codigo: "ASI", descripcion: "Asistente Administrativo" }
    ]},
    { _id: "002", descripcion: "Recursos Humanos", cargos: [
        { codigo: "RHH", descripcion: "Jefe de Recursos Humanos" },
        { codigo: "ANA", descripcion: "Analista de RRHH" }
    ]},
    { _id: "003", descripcion: "Tecnología de Información", cargos: [
        { codigo: "JPR", descripcion: "Jefe de Proyecto" },
        { codigo: "DES", descripcion: "Desarrollador de Software" },
        { codigo: "DBA", descripcion: "Administrador de Base de Datos" },
        { codigo: "QA",  descripcion: "Analista de Calidad" }
    ]},
    { _id: "004", descripcion: "Contabilidad", cargos: [
        { codigo: "CON", descripcion: "Contador General" },
        { codigo: "AUX", descripcion: "Auxiliar Contable" }
    ]},
    { _id: "005", descripcion: "Marketing", cargos: [
        { codigo: "MKT", descripcion: "Jefe de Marketing" },
        { codigo: "DIS", descripcion: "Diseñador Gráfico" }
    ]}
]);

// ---------------------------------------------------------------
// Empleados (4 usuarios de prueba)
// ---------------------------------------------------------------

db.empleados.deleteMany({});
db.empleados.insertMany([
    {
        _id: "EMP001", nombre: "Marco Adrian", apellido: "Padilla Triviño",
        cedula: "1718457072", email: "marco.padilla@monster.com", telefono: "0955046792",
        departamentoCodigo: "001", cargoCodigo: "ADM", sexoCodigo: "M", estadoCivilCodigo: "C",
        fechaNacimiento: new Date("1990-05-15"), fechaSalida: new Date("2025-01-01"),
        direccion: "Quito - Av. Amazonas 123", pasaporte: "N/A", cargosFamiliares: 0,
        fotoRuta: null, educacion: [], familiares: []
    },
    {
        _id: "EMP002", nombre: "Edgar", apellido: "Toscano",
        cedula: "1718134966", email: "edgar.toscano@monster.com", telefono: "0995481145",
        departamentoCodigo: "002", cargoCodigo: "RHH", sexoCodigo: "M", estadoCivilCodigo: "S",
        fechaNacimiento: new Date("1992-08-20"), fechaSalida: new Date("2025-01-01"),
        direccion: "Quito - Av. Colón 456", pasaporte: "N/A", cargosFamiliares: 0,
        fotoRuta: null, educacion: [], familiares: []
    },
    {
        _id: "EMP003", nombre: "Kenned", apellido: "Sigcha",
        cedula: "1737191317", email: "kenned.sigcha@monster.com", telefono: "0901437056",
        departamentoCodigo: "003", cargoCodigo: "JPR", sexoCodigo: "M", estadoCivilCodigo: "C",
        fechaNacimiento: new Date("1988-03-10"), fechaSalida: new Date("2025-01-01"),
        direccion: "Quito - Calle Sucre 789", pasaporte: "N/A", cargosFamiliares: 0,
        fotoRuta: null, educacion: [], familiares: []
    },
    {
        _id: "EMP004", nombre: "Carla", apellido: "Benitez",
        cedula: "1780593249", email: "carla.benitez@monster.com", telefono: "0932883312",
        departamentoCodigo: "003", cargoCodigo: "DES", sexoCodigo: "F", estadoCivilCodigo: "S",
        fechaNacimiento: new Date("1995-11-25"), fechaSalida: new Date("2025-01-01"),
        direccion: "Quito - Av. Naciones Unidas 321", pasaporte: "N/A", cargosFamiliares: 0,
        fotoRuta: null, educacion: [], familiares: []
    }
]);

// ---------------------------------------------------------------
// Usuarios (uno por empleado, con el perfil activo asignado)
// Contraseñas en SHA-256 (idénticas a las del DML original):
//   EMP001 -> AdminMonster2026
//   EMP002 -> RrhhMonster2026
//   EMP003 -> JefeMonster2026
//   EMP004 -> EmpleadoMonster2026
// ---------------------------------------------------------------

db.usuarios.deleteMany({});
db.usuarios.insertMany([
    {
        _id: "EMP001",
        passwordHash: "0044987cdf13f5acc8daab9eced9435743e8a013f87ef561dea3bee7c2196696",
        estadoCodigo: "A", fechaCreacion: hoy, fechaModificacion: hoy,
        pieFirma: "Administrador del Sistema",
        perfilesAsignados: [ { perfilCodigo: "PERF0001", fechaAsignacion: hoy, fechaRetiro: null } ]
    },
    {
        _id: "EMP002",
        passwordHash: "c0abb0c3a0c510bff5cefab61b90588865c17e4dcc22d560ff9f6d47314c0b67",
        estadoCodigo: "A", fechaCreacion: hoy, fechaModificacion: hoy,
        pieFirma: "Jefe de Recursos Humanos",
        perfilesAsignados: [ { perfilCodigo: "PERF0002", fechaAsignacion: hoy, fechaRetiro: null } ]
    },
    {
        _id: "EMP003",
        passwordHash: "975ee79f5b5894888012afd43c3548628d479e3d75348fccb5c2bf3cff280831",
        estadoCodigo: "A", fechaCreacion: hoy, fechaModificacion: hoy,
        pieFirma: "Jefe de Proyecto TI",
        perfilesAsignados: [ { perfilCodigo: "PERF0003", fechaAsignacion: hoy, fechaRetiro: null } ]
    },
    {
        _id: "EMP004",
        passwordHash: "c5ffd4a5cf1b0b2941f56283c4b34be0279ec2635aaa8417feb9a6bf137f1c42",
        estadoCodigo: "A", fechaCreacion: hoy, fechaModificacion: hoy,
        pieFirma: "Desarrolladora de Software",
        perfilesAsignados: [ { perfilCodigo: "PERF0004", fechaAsignacion: hoy, fechaRetiro: null } ]
    }
]);

// ---------------------------------------------------------------
// Proyectos (con empleados asignados embebidos)
// ---------------------------------------------------------------

db.proyectos.deleteMany({});
db.proyectos.insertMany([
    { _id: "P01", nombre: "Sistema de Gestión de Personal", departamentoCodigo: "003",
      empleadosAsignados: [
          { empleadoCodigo: "EMP003", horasEstimadas: 40.0 },
          { empleadoCodigo: "EMP004", horasEstimadas: 32.0 }
      ]},
    { _id: "P02", nombre: "Portal Web Corporativo", departamentoCodigo: "003",
      empleadosAsignados: [
          { empleadoCodigo: "EMP003", horasEstimadas: 20.0 }
      ]},
    { _id: "P03", nombre: "Campaña de Marketing Digital", departamentoCodigo: "005",
      empleadosAsignados: [] }
]);

// ---------------------------------------------------------------
// registroHoras: sin datos de ejemplo en el DML original (vacío)
// ---------------------------------------------------------------

db.registroHoras.deleteMany({});

// ---------------------------------------------------------------
// Contadores de secuencia (para generación de códigos siguientes)
// Se inicializan según los datos ya insertados arriba.
// ---------------------------------------------------------------

db.counters.deleteMany({});
db.counters.insertMany([
    { _id: "empleado", seq: 4 },      // EMP001..EMP004 ya existen
    { _id: "educacion", seq: 0 },
    { _id: "familiar", seq: 0 },
    { _id: "proyecto", seq: 3 },      // P01..P03 ya existen
    { _id: "registroHoras", seq: 0 }
]);

print("Datos semilla de GestionProyectosMonster insertados correctamente.");
