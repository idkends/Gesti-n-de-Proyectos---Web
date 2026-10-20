# ============================================================
# Prueba R-01: Login sin limite de intentos (fuerza bruta)
# P1 - Actividad 1 - Ingenieria de Seguridad de Software
#
# Envia 50 intentos de login con contrasena incorrecta contra
# EMP001 y verifica si la aplicacion bloquea la cuenta o aplica
# algun retardo. Ejecutar con la app corriendo en localhost:5000.
#
# Uso:  powershell -ExecutionPolicy Bypass -File prueba-fuerza-bruta.ps1
# ============================================================

$base    = "http://localhost:5000"
$usuario = "EMP001"
$intentos = 50

Write-Host "=== Prueba de fuerza bruta contra $usuario ($intentos intentos) ===" -ForegroundColor Cyan
Write-Host "Inicio: $(Get-Date -Format 'HH:mm:ss')"
Write-Host ""

$bloqueado = $false
$inicio = Get-Date

for ($i = 1; $i -le $intentos; $i++) {
    # 1) GET del login para obtener token antiforgery + cookie de sesion
    $sesion = $null
    $get = Invoke-WebRequest -Uri "$base/Account/Login" -SessionVariable sesion -UseBasicParsing
    $token = ($get.InputFields | Where-Object { $_.name -eq "__RequestVerificationToken" }).value

    # 2) POST con contrasena incorrecta
    $body = @{
        "__RequestVerificationToken" = $token
        "codigo"                     = $usuario
        "password"                   = "ClaveIncorrecta$i!"
        "returnUrl"                  = ""
    }

    $code = 0; $contenido = ""
    try {
        $resp = Invoke-WebRequest -Uri "$base/Account/Login" -Method Post -Body $body `
                 -WebSession $sesion -UseBasicParsing -ErrorAction Stop
        $code = $resp.StatusCode
        $contenido = $resp.Content
    } catch {
        $code = $_.Exception.Response.StatusCode.value__
    }

    # La app responde 200 y remuestra el login con el mensaje de error.
    # Un control de bloqueo se veria como HTTP 429/423 o un texto de "demasiados intentos".
    $hayBloqueo = ($code -eq 429 -or $code -eq 423 -or $contenido -match "(?i)demasiados|bloque|intente m")
    $estado = if ($hayBloqueo) { "BLOQUEADO" } else { "rechazado (sin bloqueo)" }
    if ($hayBloqueo) { $bloqueado = $true }

    Write-Host ("Intento {0,3} -> HTTP {1}  [{2}]" -f $i, $code, $estado)
}

$fin = Get-Date
$seg = [math]::Round(($fin - $inicio).TotalSeconds, 1)

Write-Host ""
Write-Host "=== RESULTADO ===" -ForegroundColor Yellow
Write-Host "Intentos enviados : $intentos"
Write-Host "Tiempo total      : $seg s"
Write-Host "Cuenta bloqueada  : $(if ($bloqueado) { 'SI' } else { 'NO - sin limite de intentos' })"
Write-Host "Fin: $(Get-Date -Format 'HH:mm:ss')"
