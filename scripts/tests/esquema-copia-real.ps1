[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$Dump)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$dumpPath = [IO.Path]::GetFullPath($Dump)
$tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\') + '\'
if (-not $dumpPath.StartsWith($tempRoot, [StringComparison]::OrdinalIgnoreCase) -or -not (Test-Path -LiteralPath $dumpPath -PathType Leaf)) { throw 'El dump debe estar en TEMP.' }
$context = docker context inspect | ConvertFrom-Json
if ($LASTEXITCODE -ne 0) { throw 'Docker no disponible' }
$hostDocker = $context.Endpoints.docker.Host
if ($env:DOCKER_HOST) { $hostDocker = $env:DOCKER_HOST }
if ($hostDocker -cne 'tcp://127.0.0.1:2375') { throw 'Se requiere Docker local tcp://127.0.0.1:2375' }
$guid = [guid]::NewGuid().ToString('N'); $name = "steelog-ca-test-$guid"; $db = "steelog_test_$guid"
$base = Join-Path $tempRoot "steelog-copia-$guid"
New-Item -ItemType Directory -Path $base | Out-Null
$created = $false
$previousConnection = $env:STEELOG_ESQUEMA_TEST_CONNECTION; $previousDb = $env:STEELOG_ESQUEMA_TEST_DB
. (Join-Path $PSScriptRoot 'docker-exec-api.ps1')
try {
    docker image inspect postgres:17-alpine | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Falta postgres:17-alpine' }
    $id = docker run -d --name $name --label "steelog.baseline=$guid" -e POSTGRES_PASSWORD=tesispassword123 -e TZ=UTC --tmpfs /var/lib/postgresql/data -p '127.0.0.1::5432' postgres:17-alpine
    if ($LASTEXITCODE -ne 0) { throw 'Creación fallida' }; $created = $true
    $binding = docker port $name 5432/tcp
    if ($binding -notmatch '^127\.0\.0\.1:(\d+)$' -or [int]$Matches[1] -eq 5435) { throw 'Puerto no permitido' }
    $port = [int]$Matches[1]
    for ($i=0; $i -lt 60; $i++) {
        $response = Invoke-WebRequest -UseBasicParsing -Uri "http://127.0.0.1:2375/containers/$name/logs?stdout=1&stderr=1"
        $logs = [Text.Encoding]::UTF8.GetString([byte[]]$response.Content)
        if ($logs -match 'PostgreSQL init process complete') { break }; Start-Sleep -Seconds 1
    }
    if ($logs -notmatch 'PostgreSQL init process complete') { throw 'PostgreSQL no inició' }
    # No se lee ni escribe ningún contenedor preexistente. Solo se recibe un dump local.
    Invoke-DockerExec $name psql -U postgres -d postgres -v ON_ERROR_STOP=1 -c "CREATE DATABASE $db;"
    if ($LASTEXITCODE -ne 0) { throw 'Creación DB falló' }
    Invoke-DockerExec $name psql -U postgres -d $db -v ON_ERROR_STOP=1 -c 'CREATE ROLE anon; CREATE ROLE authenticated; CREATE ROLE service_role; CREATE ROLE supabase_admin;'
    if ($LASTEXITCODE -ne 0) { throw 'Roles de compatibilidad de restore fallaron' }
    docker cp $dumpPath "${name}:/tmp/real.sql"
    if ($LASTEXITCODE -ne 0) { throw 'Copia dump falló' }
    Invoke-DockerExec $name psql -U postgres -d $db -v ON_ERROR_STOP=1 -q -f /tmp/real.sql
    if ($LASTEXITCODE -ne 0) { throw 'Restore falló' }
    $env:STEELOG_ESQUEMA_TEST_DB = $db
    $env:STEELOG_ESQUEMA_TEST_CONNECTION = "Host=127.0.0.1;Port=$port;Database=$db;Username=postgres;Password=tesispassword123"
    Copy-Item -LiteralPath (Join-Path $repo 'SteeLog/SteeLog.Tests/EsquemaLecturaIntegrationTests.cs') -Destination $base
    $infrastructure = Join-Path $repo 'SteeLog/SteeLog.Infrastructure/SteeLog.Infrastructure.csproj'
    $project = '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net48</TargetFramework><IsTestProject>true</IsTestProject></PropertyGroup><ItemGroup><PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0"/><PackageReference Include="xunit" Version="2.9.3"/><PackageReference Include="xunit.runner.visualstudio" Version="2.8.2"/><ProjectReference Include="'+$infrastructure+'"/></ItemGroup></Project>'
    $projectPath = Join-Path $base 'Smoke.csproj'
    [IO.File]::WriteAllText($projectPath, $project)
    foreach ($escenario in @('A','B')) {
        if ($escenario -eq 'B') {
            $migrations = @(Get-ChildItem (Join-Path $repo 'docker/migrations/F4*.sql') -File | Sort-Object Name)
            $migrations += @(Get-ChildItem (Join-Path $repo 'docker/migrations/F10_*.sql') -File | Sort-Object Name)
            foreach ($attempt in 1..2) {
                foreach ($migration in $migrations) {
                    # -f evita que el helper guarde una segunda copia del dump en TEMP.
                    docker cp $migration.FullName "${name}:/tmp/migration.sql"
                    if ($LASTEXITCODE -ne 0) { throw 'Copia migración falló' }
                    Invoke-DockerExec $name psql -U postgres -d $db -v ON_ERROR_STOP=1 -q -f /tmp/migration.sql
                    if ($LASTEXITCODE -ne 0) { throw "Falló $($migration.Name), intento $attempt" }
                }
            }
        }
        dotnet test $projectPath --filter 'Category=Integration&FullyQualifiedName~EsquemaLecturaIntegrationTests' --logger "trx;LogFileName=$escenario.trx" --results-directory $base --verbosity minimal
        if ($LASTEXITCODE -ne 0) { throw "Smoke $escenario falló" }
        [xml]$trx = Get-Content -Raw (Join-Path $base "$escenario.trx"); $counters = $trx.TestRun.ResultSummary.Counters
        if ([int]$counters.total -ne 3 -or [int]$counters.passed -ne 3 -or [int]$counters.failed -ne 0 -or [int]$counters.notExecuted -ne 0) { throw "Smoke $escenario incompleto" }
        "PASS escenario $escenario total=3 passed=3 failed=0 skipped=0"
    }
} finally {
    try {
        if ($created) {
            $actual = docker inspect $name | ConvertFrom-Json
            if ($actual.Name -cne "/$name" -or $actual.Id -cne $id -or $actual.Config.Labels.'steelog.baseline' -cne $guid -or $name -cnotmatch '^steelog-ca-test-[0-9a-f]{32}$') { throw 'Limpieza rechazada: identidad incorrecta' }
            docker rm -f $name
            if ($LASTEXITCODE -ne 0) { throw 'No se pudo eliminar contenedor propio' }
            "REMOVED $name"
        }
    } finally {
        $env:STEELOG_ESQUEMA_TEST_CONNECTION = $previousConnection; $env:STEELOG_ESQUEMA_TEST_DB = $previousDb
        Remove-Item -LiteralPath $dumpPath -Force
    }
}
