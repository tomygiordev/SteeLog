$ErrorActionPreference = 'Stop'
# Este runner admite únicamente el contexto Docker local tcp://127.0.0.1:2375.
$contextJson = docker context inspect
if ($LASTEXITCODE -ne 0) { throw 'No se pudo inspeccionar el contexto Docker activo.' }
$context = $contextJson | ConvertFrom-Json
$dockerHost = $context.Endpoints.docker.Host
if ($env:DOCKER_HOST) { $dockerHost = $env:DOCKER_HOST }
if ($dockerHost -ne 'tcp://127.0.0.1:2375') { throw "Contexto Docker no soportado: $dockerHost; se requiere tcp://127.0.0.1:2375." }
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$base = Join-Path ([IO.Path]::GetTempPath()) ('steelog-demo-bootstrap-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $base | Out-Null
$previousEnv = @{}
foreach ($key in @('TEMP','TMP','STEELOG_ORDENES_TEST_CONNECTION','STEELOG_ORDENES_TEST_DB','STEELOG_INVENTARIO_TEST_CONNECTION','STEELOG_INVENTARIO_TEST_DB','STEELOG_CONTEOS_TEST_CONNECTION','STEELOG_CONTEOS_TEST_DB','STEELOG_CANALETAS_TEST_CONNECTION','STEELOG_CANALETAS_TEST_DB','STEELOG_F7_TEST_CONNECTION','STEELOG_F7_TEST_DB','STEELOG_F8_TEST_CONNECTION','STEELOG_F8_TEST_DB')) { $previousEnv[$key] = [Environment]::GetEnvironmentVariable($key, 'Process') }
$guid = [guid]::NewGuid().ToString('N'); $name = "steelog-ca-test-$guid"
$volume = "steelog-demo-test-$guid"; $volumeCreated = $false
$db = 'steelog_test_' + [guid]::NewGuid().ToString('N'); $created = $false; $dbCreated = $false; $transcribing = $false
try {
    $env:TEMP = $base; $env:TMP = $base
    . (Join-Path $PSScriptRoot 'docker-exec-api.ps1')
    Start-Transcript -Path (Join-Path $base 'runner.log') | Out-Null; $transcribing = $true
    if ($name -cnotmatch '^steelog-ca-test-[0-9a-f]{32}$' -or $db -cnotmatch '^steelog_test_[0-9a-f]{32}$') { throw 'Nombre inválido' }
    docker image inspect postgres:17-alpine | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Falta imagen local postgres:17-alpine' }
    $found = docker volume ls --filter "name=^${volume}$" --format '{{.Name}}'
    if ($LASTEXITCODE -ne 0 -or $found) { throw 'El volumen de prueba ya existe; no se reutiliza' }
    docker volume create --label "steelog.baseline=$guid" $volume | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo crear volumen propio' }; $volumeCreated = $true
    $v = docker volume inspect $volume | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or $v.Name -cne $volume -or $v.Labels.'steelog.baseline' -cne $guid) { throw 'Identidad de volumen no validada' }
    $id = docker run -d --name $name --label "steelog.baseline=$guid" -e POSTGRES_PASSWORD=tesispassword123 -e TZ=UTC --mount "type=volume,source=$volume,target=/var/lib/postgresql/data" -p '127.0.0.1::5432' postgres:17-alpine
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo crear contenedor' }; $created = $true
    $actual = docker inspect $name | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or $actual.Name -cne "/$name" -or $actual.Config.Labels.'steelog.baseline' -cne $guid -or $actual.Id -cne $id) { throw 'Identidad inicial de contenedor no validada.' }
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
        ($guard + "`n" + (Get-Content -Raw -Encoding UTF8 (Join-Path $repo "docker/demo/$file"))) | Invoke-DockerExec $name psql -U postgres -d $db -v ON_ERROR_STOP=1 -q
        if ($LASTEXITCODE -ne 0) { throw "Error en $file" }
    }
    $tables = (Get-Content -Raw -Encoding UTF8 (Join-Path $repo 'docker/demo/tablas.json') | ConvertFrom-Json).tablas
    $quoted = ($tables | ForEach-Object { "'$_'" }) -join ','
    $verify = @"
DO `$check`$ BEGIN
 IF (SELECT count(*) FROM pg_tables WHERE schemaname='public') <> $($tables.Count)
 OR EXISTS(SELECT FROM pg_tables WHERE schemaname='public' AND tablename NOT IN ($quoted))
 THEN RAISE EXCEPTION 'Cierre de tablas incorrecto'; END IF;
 IF (SELECT stockactual FROM materiales WHERE id=1) <> 140
 OR (SELECT sum(impacto_stock_real) FROM movimientomaterial WHERE id_material=1) <> 140
 OR (SELECT cantidad_pedida-cantidad_recibida FROM ordenes_compra_detalle WHERE id_orden_compra=1) <> 10
 OR (SELECT estado FROM conteos_inventario WHERE id_conteo=1) <> 'abierto'
 THEN RAISE EXCEPTION 'Seed inconsistente'; END IF;
 IF EXISTS(SELECT FROM canaletas_plegados_fotos) THEN RAISE EXCEPTION 'No debe haber fotos sin archivo'; END IF;
END `$check`$;
"@
    $verify | Invoke-DockerExec $name psql -U postgres -d $db -v ON_ERROR_STOP=1 -q
    if ($LASTEXITCODE -ne 0) { throw 'Falló cierre/consistencia demo' }
    $actual = docker inspect $name | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or $actual.Id -cne $id -or $actual.Name -cne "/$name" -or $actual.Config.Labels.'steelog.baseline' -cne $guid) { throw 'Reinicio rechazado: identidad incorrecta' }
    docker restart $id | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo reiniciar contenedor propio' }
    $ready = $false
    foreach ($attempt in 1..60) {
        Invoke-DockerExec $name pg_isready -h 127.0.0.1 -U postgres -d $db
        if ($LASTEXITCODE -eq 0) { $ready = $true; break }; Start-Sleep -Seconds 1
    }
    if (-not $ready) { throw 'PostgreSQL no volvió tras reinicio' }
    $binding = docker port $name 5432/tcp
    if ($LASTEXITCODE -ne 0 -or $binding -notmatch '^127\.0\.0\.1:(\d+)$' -or [int]$Matches[1] -eq 5435) { throw 'Puerto tras reinicio no permitido' }
    $port = [int]$Matches[1]
    $verify | Invoke-DockerExec $name psql -U postgres -d $db -v ON_ERROR_STOP=1 -q
    if ($LASTEXITCODE -ne 0) { throw 'Datos demo no persistieron tras reinicio' }
    'PASS persistencia en volumen nuevo tras reiniciar contenedor propio'
    (Get-Content -Raw -Encoding UTF8 (Join-Path $repo 'docker/demo/01-schema.sql')) | Invoke-DockerExec $name psql -U postgres -d $db -v ON_ERROR_STOP=1 -q
    if ($LASTEXITCODE -ne 3) { throw 'El esquema debe rechazar una DB ya inicializada' }
    "PASS cierre $($tables.Count) tablas, seed coherente y rechazo de reinstalación"
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
        $trackedPath = "SteeLog/SteeLog/packages/$dll"
        $trackedHash = git -C $repo rev-parse ":$trackedPath"
        if ($LASTEXITCODE -ne 0 -or -not $trackedHash) { throw "DLL no versionada: $trackedPath" }
        $actualHash = git -C $repo hash-object -- $trackedPath
        if ($LASTEXITCODE -ne 0 -or $actualHash -cne $trackedHash) { throw "DLL alterada respecto del índice: $trackedPath" }
        $targetDll = Join-Path $deps $dll
        Copy-Item -LiteralPath $sourceDll -Destination $targetDll
        if ((Get-FileHash -Algorithm SHA256 -LiteralPath $sourceDll).Hash -ne (Get-FileHash -Algorithm SHA256 -LiteralPath $targetDll).Hash) { throw "Hash distinto al copiar $dll" }
    }
    $test = Join-Path $base 'IntegrationTests'; New-Item -ItemType Directory $test | Out-Null
    foreach ($file in @('OrdenCompraIntegrationTests.cs','InventarioIntegrationTests.cs','ConteoIntegrationTests.cs','CanaletaIntegrationTests.cs','CatalogosIntegrationTests.cs','HerramientasIntegrationTests.cs','ReportesIntegrationTests.cs')) { Copy-Item (Join-Path $repo "SteeLog/SteeLog.Tests/$file") $test }
    $projectFile = '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net48</TargetFramework><IsTestProject>true</IsTestProject></PropertyGroup><ItemGroup><PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0"/><PackageReference Include="xunit" Version="2.9.3"/><PackageReference Include="xunit.runner.visualstudio" Version="2.8.2"/><ProjectReference Include="..\SteeLog.Infrastructure\SteeLog.Infrastructure.csproj"/><ProjectReference Include="..\SteeLog.Application\SteeLog.Application.csproj"/><ProjectReference Include="..\SteeLog.Domain\SteeLog.Domain.csproj"/></ItemGroup></Project>'
    [IO.File]::WriteAllText((Join-Path $test 'IntegrationTests.csproj'), $projectFile)
    foreach ($prefix in @('STEELOG_ORDENES_TEST','STEELOG_INVENTARIO_TEST','STEELOG_CONTEOS_TEST','STEELOG_CANALETAS_TEST','STEELOG_F7_TEST','STEELOG_F8_TEST')) {
        [Environment]::SetEnvironmentVariable($prefix+'_DB', $db, 'Process')
        [Environment]::SetEnvironmentVariable($prefix+'_CONNECTION', "Host=127.0.0.1;Port=$port;Database=$db;Username=postgres;Password=tesispassword123;Pooling=true", 'Process')
    }
    $total = 0
    foreach ($suite in @('OrdenCompra','Inventario','Conteo','Canaleta','Catalogos','Herramientas','Reportes')) {
        dotnet test (Join-Path $test 'IntegrationTests.csproj') --filter "Category=Integration&FullyQualifiedName~${suite}IntegrationTests" --logger "trx;LogFileName=$suite.trx" --results-directory (Join-Path $base 'results') --verbosity minimal
        if ($LASTEXITCODE -ne 0) { throw "Suite $suite falló: $LASTEXITCODE" }
        [xml]$trx = Get-Content -Raw (Join-Path $base "results/$suite.trx"); $counters = $trx.TestRun.ResultSummary.Counters
        if ([int]$counters.total -lt 1 -or [int]$counters.executed -ne [int]$counters.total -or [int]$counters.passed -ne [int]$counters.total -or [int]$counters.failed -ne 0 -or [int]$counters.notExecuted -ne 0) { throw "Suite $suite ausente, omitida o fallida" }
        $total += [int]$counters.total
        "PASS $suite total=$($counters.total) executed=$($counters.executed)"
    }
    "PASS bootstrap demo integration total=$total"

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
        try {
            if ($volumeCreated) {
                $v = docker volume inspect $volume | ConvertFrom-Json
                if ($LASTEXITCODE -ne 0 -or $v.Name -cne $volume -or $v.Labels.'steelog.baseline' -cne $guid) { throw 'Limpieza de volumen rechazada' }
                docker volume rm $volume
                if ($LASTEXITCODE -ne 0) { throw 'No se pudo eliminar volumen propio' }
            }
        } finally {
        foreach ($key in $previousEnv.Keys) { [Environment]::SetEnvironmentVariable($key, $previousEnv[$key], 'Process') }
        if ($transcribing) { Stop-Transcript | Out-Null }
        }
    }
}
