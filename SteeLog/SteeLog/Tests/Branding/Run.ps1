param(
    [string]$OutputDirectory = (Join-Path $env:TEMP ('steelog-branding-smoke-' + [guid]::NewGuid().ToString('N'))),
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$out = [IO.Path]::GetFullPath($OutputDirectory)
$build = Join-Path $out 'build'
New-Item -ItemType Directory -Force $build | Out-Null
. (Join-Path $PSScriptRoot '..\..\..\..\scripts\vs-herramientas.ps1')
$msbuild = Resolve-MSBuild
$csc = Resolve-Csc $msbuild
& $msbuild (Join-Path $root "..\SteeLog.sln") /t:Restore /p:RestorePackagesConfig=true /v:quiet /nologo
if ($LASTEXITCODE -ne 0) { throw "Solution restore failed: $LASTEXITCODE" }
& $msbuild (Join-Path $root 'SteeLog.csproj') /t:Rebuild "/p:Configuration=$Configuration" "/p:OutputPath=$build\" "/p:BaseIntermediateOutputPath=$out\obj\" /v:minimal /nologo
if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
Copy-Item (Join-Path $root 'packages\itextsharp.dll') $build
& $csc /nologo /target:exe "/out:$build\BrandingSmoke.exe" "/reference:$build\SteeLog.exe" "/reference:$build\itextsharp.dll" /reference:System.Drawing.dll /reference:System.Windows.Forms.dll (Join-Path $PSScriptRoot 'BrandingSmoke.cs')
if ($LASTEXITCODE -ne 0) { throw 'Harness compile failed' }
& (Join-Path $build 'BrandingSmoke.exe') $out
if ($LASTEXITCODE -ne 0) { throw 'Harness failed' }
Write-Host "Artifacts ($Configuration): $out"
