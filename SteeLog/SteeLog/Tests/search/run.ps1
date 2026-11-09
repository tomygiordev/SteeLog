$ErrorActionPreference='Stop'
$dir=Join-Path $env:TEMP ('steelog-search-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $dir | Out-Null
$root=Resolve-Path (Join-Path $PSScriptRoot '../..')
. (Join-Path $PSScriptRoot '..\..\..\..\scripts\vs-herramientas.ps1')
$msbuild = Resolve-MSBuild
$csc = Resolve-Csc $msbuild
& $msbuild (Join-Path $root "..\SteeLog.sln") /t:Restore /p:RestorePackagesConfig=true /v:quiet /nologo
if ($LASTEXITCODE -ne 0) { throw "Solution restore failed: $LASTEXITCODE" }
& $msbuild (Join-Path $root 'SteeLog.csproj') /t:Rebuild /p:Configuration=Debug "/p:BaseIntermediateOutputPath=$dir\obj\" "/p:OutputPath=$dir\" /v:quiet /nologo
if($LASTEXITCODE -ne 0){throw 'Build failed'}
& $csc /nologo /target:exe /reference:System.Windows.Forms.dll "/out:$dir\SearchTests.exe" (Join-Path $PSScriptRoot 'SearchTests.cs')
if($LASTEXITCODE -ne 0){throw 'Compilation failed'}
Copy-Item (Join-Path $dir 'SteeLog.exe.config') (Join-Path $dir 'SearchTests.exe.config')
& (Join-Path $dir 'SearchTests.exe')
if($LASTEXITCODE -ne 0){throw 'Search tests failed'}
Write-Host "Test output: $dir"
