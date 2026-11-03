$ErrorActionPreference = 'Stop'

# Las aserciones del harness anterior se migraron a InventarioIntegrationTests.
# El runner nuevo usa un clúster propio y nunca modifica steelog-db.
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../../../..')).Path
$runner = Join-Path $repo 'scripts/tests/inventario-integracion.ps1'
& powershell -NoProfile -ExecutionPolicy Bypass -File $runner
if ($LASTEXITCODE -ne 0) { throw "La prueba de integridad fallo: $LASTEXITCODE" }
