$ErrorActionPreference = 'Stop'
$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$out = Join-Path $env:TEMP 'steelog-branding-smoke'
$build = Join-Path $out 'build'
New-Item -ItemType Directory -Force $build | Out-Null
$msbuild = 'C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe'
$csc = 'C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\Roslyn\csc.exe'
& $msbuild (Join-Path $root 'WP_Inventario.csproj') /t:Rebuild /p:Configuration=Release "/p:OutputPath=$build\" "/p:BaseIntermediateOutputPath=$out\obj\" /v:minimal /nologo
if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
Copy-Item (Join-Path $root 'packages\itextsharp.dll') $build
& $csc /nologo /target:exe "/out:$build\BrandingSmoke.exe" "/reference:$build\SteeLog.exe" "/reference:$build\itextsharp.dll" /reference:System.Drawing.dll /reference:System.Windows.Forms.dll (Join-Path $PSScriptRoot 'BrandingSmoke.cs')
if ($LASTEXITCODE -ne 0) { throw 'Harness compile failed' }
& (Join-Path $build 'BrandingSmoke.exe') $out
if ($LASTEXITCODE -ne 0) { throw 'Harness failed' }
