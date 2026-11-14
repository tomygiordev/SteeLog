[CmdletBinding()]
param(
    # Carpeta bin de PostgreSQL con initdb/pg_ctl/psql/pg_dump/pg_restore. Por defecto, la versión mayor instalada.
    [string]$Bin,
    # Carpeta de trabajo descartable (datos del cluster y copias). Por defecto, una carpeta nueva en TEMP.
    [string]$Trabajo,
    # Puerto del cluster descartable (nunca 5435). Por defecto, uno libre.
    [int]$Puerto = 0
)
# Copias de seguridad contra un PostgreSQL LOCAL DESCARTABLE (initdb + pg_ctl); no usa Docker ni toca el puerto 5435.
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
if (-not $Bin) {
    $Bin = Get-ChildItem 'C:\Program Files\PostgreSQL' -Directory -ErrorAction SilentlyContinue | Where-Object { $_.Name -match '^\d+$' } |
        Sort-Object { [int]$_.Name } -Descending | ForEach-Object { Join-Path $_.FullName 'bin' } | Where-Object { Test-Path (Join-Path $_ 'initdb.exe') } | Select-Object -First 1
}
if (-not $Bin -or -not (Test-Path (Join-Path $Bin 'initdb.exe'))) { throw 'No se encontró una instalación de PostgreSQL con initdb.exe (usar -Bin).' }
if (-not $Trabajo) { $Trabajo = Join-Path ([IO.Path]::GetTempPath()) ('steelog-respaldos-' + [guid]::NewGuid().ToString('N')) }
if (Test-Path $Trabajo) { throw "La carpeta de trabajo ya existe: $Trabajo" }
if ($Puerto -eq 0) {
    $l = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, 0); $l.Start(); $Puerto = $l.LocalEndpoint.Port; $l.Stop()
}
if ($Puerto -eq 5435) { throw 'Puerto reservado al cluster existente.' }
$datos = Join-Path $Trabajo 'datos'; $log = Join-Path $Trabajo 'postgres.log'
$clave = [guid]::NewGuid().ToString('N'); $db = 'steelog_test_' + [guid]::NewGuid().ToString('N')
$iniciado = $false
$previo = @{}
foreach ($k in @('PGPASSWORD','STEELOG_RESPALDOS_TEST_CONNECTION','STEELOG_RESPALDOS_TEST_DB','STEELOG_RESPALDOS_TEST_CARPETA','STEELOG_RESPALDOS_TEST_HERRAMIENTAS')) { $previo[$k] = [Environment]::GetEnvironmentVariable($k, 'Process') }
function Invoke-Checked([string]$Archivo, [string[]]$Argumentos) {
    & $Archivo @Argumentos
    if ($LASTEXITCODE -ne 0) { throw "$Archivo terminó con código $LASTEXITCODE" }
}
try {
    New-Item -ItemType Directory -Path $Trabajo | Out-Null
    $pw = Join-Path $Trabajo 'pw.txt'; [IO.File]::WriteAllText($pw, $clave)
    Invoke-Checked (Join-Path $Bin 'initdb.exe') @('-D', $datos, '-U', 'postgres', '-A', 'scram-sha-256', "--pwfile=$pw", '-E', 'UTF8', '--locale=C')
    Remove-Item $pw -Force
    Invoke-Checked (Join-Path $Bin 'pg_ctl.exe') @('-D', $datos, '-o', "-p $Puerto -c listen_addresses=127.0.0.1 -c fsync=off", '-w', '-t', '60', '-l', $log, 'start')
    $iniciado = $true
    "PG descartable: puerto=$Puerto datos=$datos db=$db bin=$Bin"
    $env:PGPASSWORD = $clave
    $psql = Join-Path $Bin 'psql.exe'
    Invoke-Checked $psql @('-h', '127.0.0.1', '-p', $Puerto, '-U', 'postgres', '-d', 'postgres', '-v', 'ON_ERROR_STOP=1', '-q', '-c', "CREATE DATABASE $db")
    foreach ($f in @('01-schema.sql', '02-seed.sql')) {
        Invoke-Checked $psql @('-h', '127.0.0.1', '-p', $Puerto, '-U', 'postgres', '-d', $db, '-v', 'ON_ERROR_STOP=1', '-q', '-f', (Join-Path $repo "docker/init/$f"))
    }
    # Compila las pruebas de integración aparte (el proyecto de pruebas completo exige MSBuild de Visual Studio).
    foreach ($project in @('SteeLog.Domain', 'SteeLog.Application', 'SteeLog.Infrastructure')) {
        $source = Join-Path $repo "SteeLog/$project"; $dest = Join-Path $Trabajo "src/$project"
        New-Item -ItemType Directory -Force $dest | Out-Null
        Get-ChildItem $source -File -Recurse | Where-Object { $_.Extension -in @('.cs', '.csproj') -and $_.FullName.Substring($source.Length).TrimStart('\') -notmatch '(^|\\)(bin|obj)(\\|$)' } | ForEach-Object {
            $relative = $_.FullName.Substring($source.Length).TrimStart('\'); $target = Join-Path $dest $relative
            New-Item -ItemType Directory -Force (Split-Path $target -Parent) | Out-Null
            Copy-Item -LiteralPath $_.FullName -Destination $target
        }
    }
    $deps = Join-Path $Trabajo 'src/SteeLog/packages'; New-Item -ItemType Directory -Force $deps | Out-Null
    foreach ($dll in @('itextsharp.dll', 'BouncyCastle.Crypto.dll')) { Copy-Item (Join-Path $repo "SteeLog/SteeLog/packages/$dll") (Join-Path $deps $dll) }
    $test = Join-Path $Trabajo 'src/IntegrationTests'; New-Item -ItemType Directory $test | Out-Null
    Copy-Item (Join-Path $repo 'SteeLog/SteeLog.Tests/RespaldosIntegrationTests.cs') $test
    $proyecto = '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net48</TargetFramework><IsTestProject>true</IsTestProject></PropertyGroup><ItemGroup><PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0"/><PackageReference Include="xunit" Version="2.9.3"/><PackageReference Include="xunit.runner.visualstudio" Version="2.8.2"/><ProjectReference Include="..\SteeLog.Infrastructure\SteeLog.Infrastructure.csproj"/><ProjectReference Include="..\SteeLog.Application\SteeLog.Application.csproj"/><ProjectReference Include="..\SteeLog.Domain\SteeLog.Domain.csproj"/></ItemGroup></Project>'
    [IO.File]::WriteAllText((Join-Path $test 'IntegrationTests.csproj'), $proyecto)
    $env:STEELOG_RESPALDOS_TEST_DB = $db
    $env:STEELOG_RESPALDOS_TEST_CONNECTION = "Host=127.0.0.1;Port=$Puerto;Database=$db;Username=postgres;Password=$clave;Pooling=true"
    $env:STEELOG_RESPALDOS_TEST_CARPETA = Join-Path $Trabajo 'copias'; New-Item -ItemType Directory $env:STEELOG_RESPALDOS_TEST_CARPETA | Out-Null
    $env:STEELOG_RESPALDOS_TEST_HERRAMIENTAS = $Bin
    $resultados = Join-Path $Trabajo 'resultados'
    dotnet test (Join-Path $test 'IntegrationTests.csproj') --filter 'Category=Integration&FullyQualifiedName~RespaldosIntegrationTests' --logger 'trx;LogFileName=respaldos.trx' --results-directory $resultados --verbosity minimal
    if ($LASTEXITCODE -ne 0) { throw "Las pruebas fallaron: $LASTEXITCODE" }
    [xml]$trx = Get-Content -Raw (Join-Path $resultados 'respaldos.trx'); $c = $trx.TestRun.ResultSummary.Counters
    if ([int]$c.total -lt 1 -or [int]$c.executed -ne [int]$c.total -or [int]$c.passed -ne [int]$c.total) { throw 'Pruebas ausentes, omitidas o fallidas' }
    "PASS integración de respaldos total=$($c.total) pasadas=$($c.passed)"
} finally {
    try {
        if ($iniciado) { & (Join-Path $Bin 'pg_ctl.exe') -D $datos -m immediate -w stop | Out-Null; "PG detenido" }
    } finally {
        foreach ($k in $previo.Keys) { [Environment]::SetEnvironmentVariable($k, $previo[$k], 'Process') }
        if ((Test-Path $Trabajo) -and $Trabajo -match 'steelog-respaldos-|pg-resp') {
            Remove-Item $Trabajo -Recurse -Force -ErrorAction Continue; "Carpeta de trabajo eliminada: $Trabajo"
        }
    }
}
