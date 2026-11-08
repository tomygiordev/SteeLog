$ErrorActionPreference = 'Stop'
foreach ($relative in @('../preparar-demo.ps1','demo-integracion.ps1','instalacion-demo.ps1')) {
    $lines = @(Get-Content -LiteralPath (Join-Path $PSScriptRoot $relative) | Where-Object { $_ -match 'Invoke-DockerExec.*\bpg_isready\b' })
    if ($lines.Count -ne 1 -or $lines[0] -notmatch '\bpg_isready\s+-h\s+127\.0\.0\.1\b') { throw "Readiness no exige TCP en $relative" }
}
$context = docker context inspect | ConvertFrom-Json
if ($LASTEXITCODE -ne 0) { throw 'Docker no disponible.' }
$endpoint = $context.Endpoints.docker.Host
if ($env:DOCKER_HOST) { $endpoint = $env:DOCKER_HOST }
if ($endpoint -cne 'tcp://127.0.0.1:2375') { throw 'Solo Docker local de prueba.' }
docker image inspect postgres:17-alpine | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Falta imagen local.' }
$idTest = [guid]::NewGuid().ToString('N'); $name = "steelog-ready-test-$idTest"
$base = Join-Path ([IO.Path]::GetTempPath()) $name
New-Item -ItemType Directory -Path $base | Out-Null
$created = $false; $containerId = $null
try {
    . (Join-Path $PSScriptRoot 'docker-exec-api.ps1')
    $init = Join-Path $base 'pause.sh'
    [IO.File]::WriteAllText($init, "touch /tmp/steelog-init-paused`nsleep 15`n", (New-Object Text.UTF8Encoding($false)))
    $containerId = docker create --name $name --label "steelog.ready-test=$idTest" -e POSTGRES_PASSWORD=demo --tmpfs /var/lib/postgresql/data postgres:17-alpine
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo crear contenedor propio.' }; $created = $true
    docker cp $init "${name}:/docker-entrypoint-initdb.d/pause.sh"
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo preparar pausa propia.' }
    docker start $containerId | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo iniciar contenedor propio.' }
    $paused = $false
    foreach ($attempt in 1..60) {
        Invoke-DockerExec $name test -f /tmp/steelog-init-paused
        if ($LASTEXITCODE -eq 0) { $paused = $true; break }
        Start-Sleep -Seconds 1
    }
    if (-not $paused) { throw 'No se alcanzó la pausa de initdb.' }
    Invoke-DockerExec $name pg_isready -U postgres -d postgres
    if ($LASTEXITCODE -ne 0) { throw 'El socket temporal no está listo durante pausa.' }
    Invoke-DockerExec $name pg_isready -h 127.0.0.1 -U postgres -d postgres
    if ($LASTEXITCODE -eq 0) { throw 'TCP aceptó conexiones durante el servidor temporal.' }
    'PASS socket temporal disponible pero TCP no disponible durante initdb'
    $ready = $false
    foreach ($attempt in 1..60) {
        Invoke-DockerExec $name pg_isready -h 127.0.0.1 -U postgres -d postgres
        if ($LASTEXITCODE -eq 0) { $ready = $true; break }
        Start-Sleep -Seconds 1
    }
    if (-not $ready) { throw 'Servidor definitivo no disponible por TCP.' }
    Invoke-DockerExec $name psql -U postgres -d postgres -v ON_ERROR_STOP=1 -c 'SELECT 1'
    if ($LASTEXITCODE -ne 0) { throw 'Sonda SQL válida falló.' }
    Invoke-DockerExec $name psql -U postgres -d postgres -v ON_ERROR_STOP=1 -c 'SELECT columna_inexistente'
    if ($LASTEXITCODE -ne 1) { throw 'Sonda SQL inválida no devolvió exit 1.' }
    'PASS TCP definitivo y transporte SQL con exit 0 y 1'
} finally {
    if ($created) {
        $actual = docker inspect $name | ConvertFrom-Json
        if ($LASTEXITCODE -ne 0 -or $actual.Id -cne $containerId -or $actual.Name -cne "/$name" -or $actual.Config.Labels.'steelog.ready-test' -cne $idTest) { throw 'Limpieza de contenedor no identificado rechazada.' }
        docker rm -f $containerId | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'No se pudo quitar contenedor propio.' }
        "REMOVED $name ($containerId)"
    }
    $full = [IO.Path]::GetFullPath($base)
    if (-not $full.StartsWith([IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\')+'\', [StringComparison]::OrdinalIgnoreCase) -or [IO.Path]::GetFileName($full) -cne $name) { throw 'Limpieza de fixture rechazada.' }
    Remove-Item -LiteralPath $full -Recurse -Force
}
