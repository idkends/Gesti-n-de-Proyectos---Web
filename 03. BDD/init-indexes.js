// ==================================================================
// init-indexes.js
// Crea la base de datos GestionProyectosMonster y los índices
// necesarios.
//
// Uso:
//   mongosh "mongodb://localhost:27017" "03. BDD/init-indexes.js"
// ==================================================================

const db = db.getSiblingDB("GestionProyectosMonster");

// ---------- departamentos ----------
// _id ya es único (PK). Se usa mucho ORDER BY descripcion.
db.departamentos.createIndex({ descripcion: 1 });
db.departamentos.createIndex({ "cargos.codigo": 1 });

// ---------- empleados ----------
db.empleados.createIndex({ departamentoCodigo: 1 });
db.empleados.createIndex({ cargoCodigo: 1 });
db.empleados.createIndex({ cedula: 1 }, { unique: true });
db.empleados.createIndex({ "educacion.codigo": 1 });
db.empleados.createIndex({ "familiares.codigo": 1 });

// ---------- proyectos ----------
db.proyectos.createIndex({ departamentoCodigo: 1 });
db.proyectos.createIndex({ "empleadosAsignados.empleadoCodigo": 1 });

// ---------- registroHoras ----------
db.registroHoras.createIndex({ empleadoCodigo: 1, proyectoCodigo: 1, fechaSemana: 1 });
db.registroHoras.createIndex({ proyectoCodigo: 1 });
db.registroHoras.createIndex({ empleadoCodigo: 1 });
db.registroHoras.createIndex({ estado: 1, fechaSemana: -1 });

// ---------- usuarios ----------
// _id = PEEMP_CODIGO (uno por empleado).
db.usuarios.createIndex({ estadoCodigo: 1 });
db.usuarios.createIndex({ "perfilesAsignados.perfilCodigo": 1 });
db.usuarios.createIndex({ "perfilesAsignados.fechaRetiro": 1 });

// ---------- perfiles ----------
db.perfiles.createIndex({ "opcionesAsignadas.opcionCodigo": 1 });
db.perfiles.createIndex({ "opcionesAsignadas.fechaRetiro": 1 });

// ---------- opciones ----------
db.opciones.createIndex({ sistemaCodigo: 1 });

// ---------- catálogos simples ----------
// estadosCiviles, parentescos, estadosUsuario -> _id ya es PK/único.

// ---------- counters ----------
// _id ya es único (nombre de secuencia).

print("Índices de GestionProyectosMonster creados correctamente.");
