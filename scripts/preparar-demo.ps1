[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$Paquete, [Parameter(Mandatory=$true)][string]$Destino)
$ErrorActionPreference = 'Stop'
if (-not [IO.Path]::IsPathRooted($Paquete) -or -not [IO.Path]::IsPathRooted($Destino)) { throw 'Se requieren rutas absolutas.' }
$source = (Resolve-Path -LiteralPath $Paquete).Path
$dest = [IO.Path]::GetFullPath($Destino)
if (Test-Path -LiteralPath $dest) { throw 'Destino debe ser nuevo. No se actualizan instalaciones existentes.' }
$manifest = Get-Content -Raw -Encoding UTF8 (Join-Path $source 'manifest.json') | ConvertFrom-Json
foreach ($file in $manifest.archivos) {
    if ($file.archivo -match '(^/|^[A-Za-z]:|(^|[/\\])\.\.([/\\]|$))') { throw 'Ruta inválida en manifest.' }
    $path = [IO.Path]::GetFullPath((Join-Path $source $file.archivo))
    if (-not $path.StartsWith($source.TrimEnd('\')+'\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Escape de ruta.' }
    if ((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -cne $file.sha256) { throw "Hash alterado: $($file.archivo)" }
}
$context = docker context inspect | ConvertFrom-Json
if ($LASTEXITCODE -ne 0) { throw 'Docker no disponible.' }
$hostDocker = $context.Endpoints.docker.Host
if ($env:DOCKER_HOST) { $hostDocker = $env:DOCKER_HOST }
if ($hostDocker -cne 'tcp://127.0.0.1:2375') { throw 'Este instalador admite solamente Docker local tcp://127.0.0.1:2375.' }
docker image inspect postgres:17-alpine | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Preparar previamente la imagen postgres:17-alpine; no se descarga implícitamente.' }
$idDemo = [guid]::NewGuid().ToString('N')
$name = "steelog-demo-$idDemo"; $volume = "steelog-demo-data-$idDemo"; $db = "steelog_demo_$idDemo"
New-Item -ItemType Directory -Path $dest | Out-Null
$stateFile = Join-Path $dest 'instalacion.json'
$state = [ordered]@{ id=$idDemo; contenedor=$name; volumen=$volume; base=$db; estado='preparando' }
$utf8 = New-Object Text.UTF8Encoding($false)
function Save-State { [IO.File]::WriteAllText($stateFile, ($state | ConvertTo-Json), $utf8) }
Save-State
$previousTemp = $env:TEMP; $previousTmp = $env:TMP
$work = Join-Path $dest 'trabajo'
New-Item -ItemType Directory -Path $work | Out-Null
try {
    $env:TEMP = $work; $env:TMP = $work
    $exists = docker volume ls --filter "name=^${volume}$" --format '{{.Name}}'
    if ($LASTEXITCODE -ne 0 -or $exists) { throw 'No se reutiliza un volumen preexistente.' }
    docker volume create --label "steelog.demo=$idDemo" $volume | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Creación de volumen fallida.' }
    $v = docker volume inspect $volume | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or $v.Name -cne $volume -or $v.Labels.'steelog.demo' -cne $idDemo) { throw 'Volumen no validado.' }
    $containerId = docker run -d --name $name --label "steelog.demo=$idDemo" -e POSTGRES_PASSWORD=tesispassword123 -e "POSTGRES_DB=$db" -e TZ=UTC --mount "type=volume,source=$volume,target=/var/lib/postgresql/data" -p '127.0.0.1::5432' postgres:17-alpine
    if ($LASTEXITCODE -ne 0) { throw 'Creación de contenedor fallida.' }
    $state['containerId'] = $containerId; Save-State
    $actual = docker inspect $name | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or $actual.Id -cne $containerId -or $actual.Name -cne "/$name" -or $actual.Config.Labels.'steelog.demo' -cne $idDemo) { throw 'Contenedor no validado.' }
    $binding = docker port $name 5432/tcp
    if ($LASTEXITCODE -ne 0 -or $binding -notmatch '^127\.0\.0\.1:(\d+)$' -or [int]$Matches[1] -eq 5435) { throw 'Puerto no permitido.' }
    $port = [int]$Matches[1]; $state['puerto']=$port; Save-State
    $transport = Join-Path $PSScriptRoot 'docker-exec-api.ps1'
    if (-not (Test-Path -LiteralPath $transport)) { $transport = Join-Path $PSScriptRoot 'tests/docker-exec-api.ps1' }
    . $transport
    $ready = $false
    foreach ($attempt in 1..60) {
        Invoke-DockerExec $name pg_isready -U postgres -d $db
        if ($LASTEXITCODE -eq 0) { $ready=$true; break }; Start-Sleep -Seconds 1
    }
    if (-not $ready) { throw 'PostgreSQL no inició.' }
    Invoke-DockerExec $name psql -U postgres -d $db -v ON_ERROR_STOP=1 -c 'SELECT current_database()'
    if ($LASTEXITCODE -ne 0) { throw 'Sonda válida falló.' }
    Invoke-DockerExec $name psql -U postgres -d $db -v ON_ERROR_STOP=1 -c 'SELECT columna_inexistente'
    if ($LASTEXITCODE -ne 1) { throw 'El transporte no propagó el error SQL.' }
    foreach ($file in @('01-schema.sql','02-seed.sql')) {
        (Get-Content -Raw -Encoding UTF8 (Join-Path $source "database/$file")) | Invoke-DockerExec $name psql -U postgres -d $db -v ON_ERROR_STOP=1 -q
        if ($LASTEXITCODE -ne 0) { throw "Falló $file. No reintentar sobre el mismo volumen." }
    }
    $app = Join-Path $dest 'app'
    New-Item -ItemType Directory -Path $app | Out-Null
    foreach ($file in @('SteeLog.exe','SteeLog.exe.config')) { Copy-Item -LiteralPath (Join-Path $source $file) -Destination $app }
    Copy-Item -LiteralPath (Join-Path $source 'demo') -Destination (Join-Path $app 'demo') -Recurse
    $config = @{ConnectionStrings=@{DefaultConnection="Host=127.0.0.1;Port=$port;Database=$db;Username=postgres;Password=tesispassword123"}} | ConvertTo-Json
    [IO.File]::WriteAllText((Join-Path $app 'appsettings.json'), $config, $utf8)
    $data = Join-Path $dest 'datos'
    New-Item -ItemType Directory -Path $data | Out-Null
    [IO.File]::WriteAllText((Join-Path $data '.steelog-demo'), 'steelog-demo-v1', $utf8)
    Copy-Item -LiteralPath (Join-Path $source 'tools/ejecutar-demo.ps1') -Destination (Join-Path $dest 'ejecutar-demo.ps1')
    $state['estado']='preparada'; Save-State
    "Demo preparada en $app. No se ejecutó Main. Datos ficticios; credencial PostgreSQL pública de demostración."
} catch {
    $state['estado']='incompleta'; Save-State
    Write-Warning "Se conservan recursos propios para diagnóstico: $stateFile. No hay rollback conjunto de archivos/Docker. No se elimina nada automáticamente."
    throw
}

finally { $env:TEMP=$previousTemp; $env:TMP=$previousTmp }
