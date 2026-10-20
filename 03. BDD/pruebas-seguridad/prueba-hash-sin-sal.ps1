# ============================================================
# Prueba R-02: Hashes de contrasena sin sal (SHA-256)
# P1 - Actividad 1 - Ingenieria de Seguridad de Software
#
# Lee los passwordHash de la coleccion 'usuarios' y los compara
# contra un diccionario de contrasenas candidatas. Como el hash
# es SHA-256 sin sal, cada hash se puede revertir con un simple
# diccionario (ataque de fuerza bruta offline / rainbow table).
#
# Requiere mongosh en el PATH y la BD en localhost:27017.
# Uso:  powershell -ExecutionPolicy Bypass -File prueba-hash-sin-sal.ps1
# ============================================================

function Sha256Hex([string]$texto) {
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($texto)
    $hash  = [System.Security.Cryptography.SHA256]::Create().ComputeHash($bytes)
    return ([System.BitConverter]::ToString($hash)).Replace("-", "").ToLower()
}

Write-Host "=== R-02: Reversion de hashes sin sal ===" -ForegroundColor Cyan

# 1) Obtener los hashes reales desde MongoDB
$json = mongosh "mongodb://localhost:27017/GestionProyectosMonster" --quiet --eval `
        "JSON.stringify(db.usuarios.find({}, {passwordHash:1}).toArray())"
$usuarios = $json | ConvertFrom-Json

Write-Host "`nHashes almacenados en la base:" -ForegroundColor Yellow
$usuarios | ForEach-Object { Write-Host ("  {0} : {1}" -f $_._id, $_.passwordHash) }

# 2) Diccionario de candidatas (un atacante usaria millones; aqui bastan pocas)
$diccionario = @(
    "AdminMonster2026","RrhhMonster2026","JefeMonster2026","EmpleadoMonster2026",
    "123456","password","admin","Monster2026","Admin123"
)

# 3) Precalcular hash de cada candidata y cruzar
$tabla = @{}
foreach ($c in $diccionario) { $tabla[(Sha256Hex $c)] = $c }

Write-Host "`nResultado del ataque de diccionario:" -ForegroundColor Yellow
$rotos = 0
foreach ($u in $usuarios) {
    if ($tabla.ContainsKey($u.passwordHash)) {
        Write-Host ("  [ROTO]  {0} -> '{1}'" -f $u._id, $tabla[$u.passwordHash]) -ForegroundColor Red
        $rotos++
    } else {
        Write-Host ("  [----]  {0} -> no esta en el diccionario" -f $u._id)
    }
}

Write-Host "`n=== RESULTADO ===" -ForegroundColor Yellow
Write-Host "Cuentas descifradas: $rotos de $($usuarios.Count)"
Write-Host "Conclusion: SHA-256 sin sal permite revertir contrasenas con diccionario."
