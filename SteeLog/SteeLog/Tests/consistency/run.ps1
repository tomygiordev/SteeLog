$ErrorActionPreference = 'Stop'
$db = 'steelog_test_' + [Guid]::NewGuid().ToString('N')
if ($db -cnotmatch '^steelog_test_[0-9a-f]{32}$') { throw 'Invalid test database name' }
$dir = Join-Path $env:TEMP ('steelog-consistency-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $dir | Out-Null
$root = Resolve-Path (Join-Path $PSScriptRoot '../..')
$repo = Resolve-Path (Join-Path $root '../..')
$msbuild = 'C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe'
$csc = 'C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\Roslyn\csc.exe'
function Run-Native { if ($LASTEXITCODE -ne 0) { throw "Native command failed: $LASTEXITCODE" } }
$created = $false
try {
 docker exec steelog-db psql -U postgres -d postgres -v ON_ERROR_STOP=1 -c "CREATE DATABASE $db" | Out-Null; Run-Native; $created = $true
 foreach ($script in @('01-schema.sql','02-seed.sql')) { Get-Content -Raw (Join-Path $repo "docker/init/$script") | docker exec -i steelog-db psql -U postgres -d $db -v ON_ERROR_STOP=1 -q | Out-Null; Run-Native }
 & $msbuild (Join-Path $root 'SteeLog.csproj') /t:Rebuild /p:Configuration=Debug "/p:BaseIntermediateOutputPath=$dir\obj\" "/p:OutputPath=$dir\" /v:quiet /nologo; Run-Native
 '{"ConnectionStrings":{"DefaultConnection":"Server=localhost;Port=5435;Database=' + $db + ';User Id=postgres;Password=tesispassword123;Pooling=true;"}}' | Set-Content -Encoding ASCII (Join-Path $dir 'appsettings.json')
 & $csc /nologo /target:exe "/out:$dir\IntegrityTests.exe" "/reference:$dir\SteeLog.exe" (Join-Path $PSScriptRoot 'IntegrityTests.cs'); Run-Native
 Copy-Item (Join-Path $dir 'SteeLog.exe.config') (Join-Path $dir 'IntegrityTests.exe.config')
 $env:STEELOG_TEST_DB=$db
 & (Join-Path $dir 'IntegrityTests.exe'); Run-Native
} finally {
 Remove-Item Env:STEELOG_TEST_DB -ErrorAction SilentlyContinue
 if ($created -and $db -cmatch '^steelog_test_[0-9a-f]{32}$') { docker exec steelog-db psql -U postgres -d postgres -v ON_ERROR_STOP=1 -c "DROP DATABASE $db WITH (FORCE)" | Out-Null; Run-Native }
 Write-Host "Test output: $dir; database cleaned: $db"
}

