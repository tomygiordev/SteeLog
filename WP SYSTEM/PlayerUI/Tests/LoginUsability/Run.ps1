$ErrorActionPreference = 'Stop'
$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$out = Join-Path $env:TEMP ('steelog-login-usability-' + [guid]::NewGuid().ToString('N'))
$msbuild = 'C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe'
$csc = 'C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\Roslyn\csc.exe'
foreach ($config in @('Debug','Release')) {
    $build = Join-Path $out $config
    New-Item -ItemType Directory -Force $build | Out-Null
    & $msbuild (Join-Path $root 'WP_Inventario.csproj') /t:Rebuild "/p:Configuration=$config" "/p:OutputPath=$build\" "/p:BaseIntermediateOutputPath=$out\obj-$config\" /v:minimal /nologo
    if ($LASTEXITCODE -ne 0) { throw "$config build failed" }
    & $csc /nologo /target:exe "/out:$build\LoginSmoke.exe" "/reference:$build\SteeLog.exe" /reference:System.Drawing.dll /reference:System.Windows.Forms.dll (Join-Path $PSScriptRoot 'LoginSmoke.cs')
    if ($LASTEXITCODE -ne 0) { throw "$config test compile failed" }
    & (Join-Path $build 'LoginSmoke.exe') $build
    if ($LASTEXITCODE -ne 0) { throw "$config test failed" }
}
Write-Host "Artifacts: $out"
