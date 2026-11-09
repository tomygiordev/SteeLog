$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$dir = Join-Path $env:TEMP ('steelog-config-' + [Guid]::NewGuid().ToString('N'))
$build = Join-Path $dir 'build'
New-Item -ItemType Directory -Path $build -Force | Out-Null
. (Join-Path $PSScriptRoot '..\..\..\..\scripts\vs-herramientas.ps1')
$msbuild = Resolve-MSBuild
$csc = Resolve-Csc $msbuild
& $msbuild (Join-Path $root "..\SteeLog.sln") /t:Restore /p:RestorePackagesConfig=true /v:quiet /nologo
if ($LASTEXITCODE -ne 0) { throw "Solution restore failed: $LASTEXITCODE" }
function CheckNative { if ($LASTEXITCODE -ne 0) { throw "Native command failed: $LASTEXITCODE" } }
foreach ($config in @('Debug', 'Release')) {
 $out = Join-Path $dir $config; $obj = Join-Path $dir ("obj-" + $config)
 & $msbuild (Join-Path $root 'SteeLog.csproj') /t:Build "/p:Configuration=$config" "/p:OutputPath=$out\" "/p:BaseIntermediateOutputPath=$obj\" /v:quiet /nologo; CheckNative
}
Copy-Item (Join-Path $dir 'Debug\*') $build -Recurse -Force
& $csc /nologo /target:exe "/out:$build\ConfigurationTests.exe" "/reference:$build\SteeLog.exe" /reference:System.Data.dll (Join-Path $PSScriptRoot 'ConfigurationTests.cs'); CheckNative
Copy-Item (Join-Path $build 'SteeLog.exe.config') (Join-Path $build 'ConfigurationTests.exe.config')
foreach ($mode in @('override','fallback','malformed','empty','invalid','leak','failure')) {
 $scenario = Join-Path $dir $mode; Copy-Item $build $scenario -Recurse
 $config = Join-Path $scenario 'appsettings.json'
 if (Test-Path $config) { Remove-Item $config }
 switch ($mode) {
  'override' { '{"ConnectionStrings":{"DefaultConnection":"Host=localhost;Database=override_test;Username=test;Password=test"}}' | Set-Content -Encoding ASCII $config }
  'malformed' { '{"ConnectionStrings": "secret_marker"' | Set-Content -Encoding ASCII $config }
  'empty' { '{"ConnectionStrings":{"DefaultConnection":""}}' | Set-Content -Encoding ASCII $config }
  'invalid' { '{"ConnectionStrings":{"DefaultConnection":"bad_secret_marker"}}' | Set-Content -Encoding ASCII $config }
 }
 & (Join-Path $scenario 'ConfigurationTests.exe') $mode; CheckNative
}
Write-Host "Temporary output: $dir"

