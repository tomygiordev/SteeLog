[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$Paquete)
$ErrorActionPreference = 'Stop'
$package = (Resolve-Path -LiteralPath $Paquete).Path
$dest = Join-Path ([IO.Path]::GetTempPath()) ('steelog-install-test-'+[guid]::NewGuid().ToString('N'))
$state = $null
try {
    & (Join-Path $package 'tools/preparar-demo.ps1') -Paquete $package -Destino $dest
    $state = Get-Content -Raw (Join-Path $dest 'instalacion.json') | ConvertFrom-Json
    if ($state.id -cnotmatch '^[0-9a-f]{32}$' -or $state.contenedor -cne "steelog-demo-$($state.id)" -or $state.volumen -cne "steelog-demo-data-$($state.id)") { throw 'Estado de instalación no validado.' }
    $actual = docker inspect $state.contenedor | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or $actual.Id -cne $state.containerId -or $actual.Name -cne "/$($state.contenedor)" -or $actual.Config.Labels.'steelog.demo' -cne $state.id) { throw 'Reinicio de contenedor no identificado rechazado.' }
    docker restart $state.containerId | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Reinicio falló.' }
    . (Join-Path $PSScriptRoot 'docker-exec-api.ps1')
    $ready=$false
    foreach ($attempt in 1..60) {
        Invoke-DockerExec $state.contenedor pg_isready -h 127.0.0.1 -U postgres -d $state.base
        if ($LASTEXITCODE -eq 0) { $ready=$true; break }; Start-Sleep -Seconds 1
    }
    if (-not $ready) { throw 'DB no volvió tras reiniciar.' }
    & (Join-Path $dest 'ejecutar-demo.ps1') -SinEjecutar
    $binding = docker port $state.contenedor 5432/tcp
    if ($LASTEXITCODE -ne 0 -or $binding -notmatch '^127\.0\.0\.1:(\d+)$') { throw 'Puerto posterior no validado.' }
    $port = [int]$Matches[1]
    $config = Get-Content -Raw (Join-Path $dest 'app/appsettings.json') | ConvertFrom-Json
    if ($config.ConnectionStrings.DefaultConnection -notlike "*Port=$port;*") { throw 'Conexión conserva un puerto anterior.' }
    $assembly = [Reflection.Assembly]::LoadFrom((Join-Path $dest 'app/SteeLog.exe'))
    [Runtime.CompilerServices.RuntimeHelpers]::RunModuleConstructor($assembly.ManifestModule.ModuleHandle)
    $null = [Reflection.Assembly]::Load('Npgsql')
    $connection = New-Object Npgsql.NpgsqlConnection($config.ConnectionStrings.DefaultConnection)
    try {
        $connection.Open()
        $cmd=$connection.CreateCommand()
        try {
            $cmd.CommandText="SELECT current_database() || ':' || (SELECT count(*) FROM pg_tables WHERE schemaname='public') || ':' || (SELECT stockactual::integer FROM materiales WHERE id=1)"
            if ($cmd.ExecuteScalar() -cne "$($state.base):23:140") { throw 'DB/seed externos incorrectos.' }
        } finally { $cmd.Dispose() }
    } finally { $connection.Dispose(); [Npgsql.NpgsqlConnection]::ClearAllPools() }
    'PASS instalación real, reinicio, puerto actualizado y consulta TCP mediante Npgsql embebido; Main NO ejecutado'
    [IO.File]::WriteAllText((Join-Path $dest 'datos/.steelog-demo'), 'marcador-incorrecto')
    $rejected=$false
    try { & (Join-Path $dest 'ejecutar-demo.ps1') -SinEjecutar }
    catch { if ($_.Exception.Message -notlike 'Datos demo no identificados*') { throw }; $rejected=$true }
    if (-not $rejected) { throw 'No se rechazó raíz incorrecta.' }
    'PASS raíz de datos inválida rechazada sin ejecutar Main'
} finally {
    if (-not $state -and (Test-Path -LiteralPath (Join-Path $dest 'instalacion.json'))) { $state=Get-Content -Raw (Join-Path $dest 'instalacion.json') | ConvertFrom-Json }
    if ($state) {
        $actual = docker inspect $state.contenedor | ConvertFrom-Json
        if ($LASTEXITCODE -ne 0 -or $state.id -cnotmatch '^[0-9a-f]{32}$' -or $actual.Id -cne $state.containerId -or $actual.Name -cne "/steelog-demo-$($state.id)" -or $actual.Config.Labels.'steelog.demo' -cne $state.id) { throw 'Limpieza rechazada: contenedor no identificado.' }
        docker rm -f $state.containerId
        if ($LASTEXITCODE -ne 0) { throw 'Limpieza de contenedor propio falló.' }
        $v=docker volume inspect $state.volumen | ConvertFrom-Json
        if ($LASTEXITCODE -ne 0 -or $v.Name -cne "steelog-demo-data-$($state.id)" -or $v.Labels.'steelog.demo' -cne $state.id) { throw 'Limpieza rechazada: volumen no identificado.' }
        docker volume rm $state.volumen
        if ($LASTEXITCODE -ne 0) { throw 'Limpieza de volumen propio falló.' }
        "REMOVED recursos de prueba; archivos de evidencia en $dest"
    }
}
