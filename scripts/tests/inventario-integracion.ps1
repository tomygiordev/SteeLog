$ErrorActionPreference = 'Stop'
# Este runner admite únicamente el contexto Docker local tcp://127.0.0.1:2375.
$contextJson = docker context inspect
if ($LASTEXITCODE -ne 0) { throw 'No se pudo inspeccionar el contexto Docker activo.' }
$context = $contextJson | ConvertFrom-Json
$dockerHost = $context.Endpoints.docker.Host
if ($env:DOCKER_HOST) { $dockerHost = $env:DOCKER_HOST }
if ($dockerHost -ne 'tcp://127.0.0.1:2375') { throw "Contexto Docker no soportado: $dockerHost; se requiere tcp://127.0.0.1:2375." }
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$base = Join-Path ([IO.Path]::GetTempPath()) ('steelog-inventario-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $base | Out-Null
$previousEnv = @{}
foreach ($key in @('TEMP','TMP','STEELOG_INVENTARIO_TEST_CONNECTION','STEELOG_INVENTARIO_TEST_DB')) { $previousEnv[$key] = [Environment]::GetEnvironmentVariable($key, 'Process') }
$guid = [guid]::NewGuid().ToString('N'); $name = "steelog-ca-test-$guid"
$db = 'steelog_test_' + [guid]::NewGuid().ToString('N'); $created = $false; $dbCreated = $false; $transcribing = $false
try {
    $env:TEMP = $base; $env:TMP = $base
    . (Join-Path $PSScriptRoot 'docker-exec-api.ps1')
    Start-Transcript -Path (Join-Path $base 'runner.log') | Out-Null; $transcribing = $true
    if ($name -cnotmatch '^steelog-ca-test-[0-9a-f]{32}$' -or $db -cnotmatch '^steelog_test_[0-9a-f]{32}$') { throw 'Nombre inválido' }
    docker image inspect postgres:17-alpine | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Falta imagen local postgres:17-alpine' }
    $id = docker run -d --name $name --label "steelog.baseline=$guid" -e POSTGRES_PASSWORD=tesispassword123 -e TZ=UTC --tmpfs /var/lib/postgresql/data -p '127.0.0.1::5432' postgres:17-alpine
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo crear contenedor' }; $created = $true
    "TEMP=$base CONTAINER=$name ID=$id DB=$db"
    $binding = docker port $name 5432/tcp
    if ($binding -notmatch '^127\.0\.0\.1:(\d+)$') { throw "Puerto inválido: $binding" }
    $port = [int]$Matches[1]; if ($port -eq 5435) { throw 'Puerto reservado al cluster existente' }
    for ($i=0; $i -lt 60; $i++) {
        $response = Invoke-WebRequest -UseBasicParsing -Uri "http://127.0.0.1:2375/containers/$name/logs?stdout=1&stderr=1"
        $logs = [Text.Encoding]::UTF8.GetString([byte[]]$response.Content)
        if ($logs -match 'PostgreSQL init process complete') { break }; Start-Sleep -Seconds 1
    }
    if ($logs -notmatch 'PostgreSQL init process complete') { throw 'PostgreSQL no inició' }
    Invoke-DockerExec $name psql -U postgres -d postgres -v ON_ERROR_STOP=1 -c 'select current_database(),current_date,current_setting(''TimeZone'')'
    if ($LASTEXITCODE -ne 0) { throw 'Sonda SQL válida falló' }
    Write-Host "Sonda SQL válida: exit $LASTEXITCODE"
    Invoke-DockerExec $name psql -U postgres -d postgres -v ON_ERROR_STOP=1 -c 'select nonexistent_column'
    if ($LASTEXITCODE -ne 1) { throw 'El transporte no propagó el error SQL' }
    Write-Host "Sonda SQL inválida: exit $LASTEXITCODE"
    Invoke-DockerExec $name psql -U postgres -d postgres -v ON_ERROR_STOP=1 -c "CREATE DATABASE $db"
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo crear DB efímera' }; $dbCreated = $true
    $guard = "DO `$guard`$ BEGIN IF current_database() <> '$db' THEN RAISE EXCEPTION 'Wrong test database'; END IF; END `$guard`$;"
    foreach ($file in @('01-schema.sql','02-seed.sql')) {
        ($guard + "`n" + (Get-Content -Raw -Encoding UTF8 (Join-Path $repo "docker/init/$file"))) | Invoke-DockerExec $name psql -U postgres -d $db -v ON_ERROR_STOP=1 -q
        if ($LASTEXITCODE -ne 0) { throw "Error en $file" }
    }
    $migrations = @(Get-ChildItem -Path (Join-Path $repo 'docker/migrations') -Filter 'F4*.sql' -File)
    if ($migrations.Count -ne 1) { throw 'Se requiere exactamente una migración F4 explícita.' }
    $migrations += @(Get-ChildItem -Path (Join-Path $repo 'docker/migrations') -Filter 'F10_*.sql' -File | Sort-Object Name)
    foreach ($attempt in 1..2) {
        foreach ($migration in $migrations) {
            ($guard + "`n" + (Get-Content -Raw -Encoding UTF8 $migration.FullName)) | Invoke-DockerExec $name psql -U postgres -d $db -v ON_ERROR_STOP=1 -q
            if ($LASTEXITCODE -ne 0) { throw "Migración $($migration.Name) falló en aplicación $attempt" }
        }
    }
    foreach ($project in @('SteeLog.Domain','SteeLog.Application','SteeLog.Infrastructure')) {
        $source = Join-Path $repo "SteeLog/$project"; $dest = Join-Path $base $project
        New-Item -ItemType Directory -Force $dest | Out-Null
        Get-ChildItem $source -File -Recurse | Where-Object { $_.Extension -in @('.cs','.csproj') -and $_.FullName.Substring($source.Length).TrimStart('\') -notmatch '(^|\\)(bin|obj)(\\|$)' } | ForEach-Object {
            $relative = $_.FullName.Substring($source.Length).TrimStart('\'); $target = Join-Path $dest $relative
            New-Item -ItemType Directory -Force (Split-Path $target -Parent) | Out-Null
            Copy-Item -LiteralPath $_.FullName -Destination $target
        }
    }
    # Infrastructure compila también el renderer PDF; copiar solo sus DLL versionadas, aunque esta suite no exporta PDF.
    $deps = Join-Path $base 'SteeLog/packages'; New-Item -ItemType Directory -Force $deps | Out-Null
    foreach ($dll in @('itextsharp.dll','BouncyCastle.Crypto.dll')) {
        $sourceDll = Join-Path $repo "SteeLog/SteeLog/packages/$dll"
        if (-not (Test-Path -LiteralPath $sourceDll -PathType Leaf)) { throw "Falta dependencia versionada: $sourceDll" }
        $targetDll = Join-Path $deps $dll
        Copy-Item -LiteralPath $sourceDll -Destination $targetDll
        if ((Get-FileHash -Algorithm SHA256 -LiteralPath $sourceDll).Hash -ne (Get-FileHash -Algorithm SHA256 -LiteralPath $targetDll).Hash) { throw "Hash distinto al copiar $dll" }
    }
    $test = Join-Path $base 'IntegrationTests'; New-Item -ItemType Directory $test | Out-Null
    Copy-Item (Join-Path $repo 'SteeLog/SteeLog.Tests/InventarioIntegrationTests.cs') $test
    $projectFile = '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net48</TargetFramework><IsTestProject>true</IsTestProject></PropertyGroup><ItemGroup><PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0"/><PackageReference Include="xunit" Version="2.9.3"/><PackageReference Include="xunit.runner.visualstudio" Version="2.8.2"/><ProjectReference Include="..\SteeLog.Infrastructure\SteeLog.Infrastructure.csproj"/><ProjectReference Include="..\SteeLog.Application\SteeLog.Application.csproj"/><ProjectReference Include="..\SteeLog.Domain\SteeLog.Domain.csproj"/></ItemGroup></Project>'
    [IO.File]::WriteAllText((Join-Path $test 'IntegrationTests.csproj'), $projectFile)
    $env:STEELOG_INVENTARIO_TEST_DB = $db
    $env:STEELOG_INVENTARIO_TEST_CONNECTION = "Host=127.0.0.1;Port=$port;Database=$db;Username=postgres;Password=tesispassword123;Pooling=true"
    dotnet test (Join-Path $test 'IntegrationTests.csproj') --filter 'Category=Integration&FullyQualifiedName~InventarioIntegrationTests' --logger 'trx;LogFileName=inventario.trx' --results-directory (Join-Path $base 'results') --verbosity minimal
    if ($LASTEXITCODE -ne 0) { throw "Las pruebas fallaron: $LASTEXITCODE" }
    [xml]$trx = Get-Content -Raw (Join-Path $base 'results/inventario.trx'); $counters = $trx.TestRun.ResultSummary.Counters
    if ([int]$counters.total -lt 1 -or [int]$counters.executed -ne [int]$counters.total -or [int]$counters.passed -ne [int]$counters.total -or [int]$counters.failed -ne 0 -or [int]$counters.notExecuted -ne 0) { throw 'Pruebas ausentes, omitidas o fallidas' }
    "PASS integration total=$($counters.total) executed=$($counters.executed)"
} finally {
    try {
        if ($created) {
            $actual = docker inspect $name | ConvertFrom-Json
            if ($actual.Name -eq "/$name" -and $actual.Config.Labels.'steelog.baseline' -eq $guid -and $actual.Id -eq $id) {
                if ($dbCreated -and $db -cmatch '^steelog_test_[0-9a-f]{32}$') {
                    $guard | Invoke-DockerExec $name psql -U postgres -d $db -v ON_ERROR_STOP=1 -q
                    if ($LASTEXITCODE -ne 0) { throw 'Identidad DB no validada antes de DROP' }
                    Invoke-DockerExec $name psql -U postgres -d postgres -v ON_ERROR_STOP=1 -c "DROP DATABASE $db WITH (FORCE)"
                    if ($LASTEXITCODE -ne 0) { throw 'No se pudo eliminar DB efímera' }
                }
                docker rm -f $name; if ($LASTEXITCODE -ne 0) { throw 'No se pudo eliminar contenedor propio' }; "REMOVED $name"
            } else { throw 'Identidad de contenedor no validada; limpieza rechazada' }
        }
    } finally {
        foreach ($key in $previousEnv.Keys) { [Environment]::SetEnvironmentVariable($key, $previousEnv[$key], 'Process') }
        if ($transcribing) { Stop-Transcript | Out-Null }
    }
}
