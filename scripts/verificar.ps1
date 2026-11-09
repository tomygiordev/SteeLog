[CmdletBinding()]
param([switch]$LegacyLocal)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
. (Join-Path $PSScriptRoot 'vs-herramientas.ps1')
$msbuild = Resolve-MSBuild
function Invoke-Checked([string]$File, [string[]]$Arguments) {
    & $File @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$File terminó con código $LASTEXITCODE" }
}
$previousHostAssembly = $env:STEELOG_HOST_ASSEMBLY
Push-Location $root
try {
    foreach ($configuration in @('Debug', 'Release')) {
        Invoke-Checked $msbuild @('SteeLog/SteeLog.sln', '/restore', '/p:RestorePackagesConfig=true', "/p:Configuration=$configuration", '/m', '/v:minimal', '/nologo')
        $env:STEELOG_HOST_ASSEMBLY = Join-Path $root "SteeLog\SteeLog\bin\$configuration\SteeLog.exe"
        Invoke-Checked 'dotnet' @('test', 'SteeLog/SteeLog.Tests/SteeLog.Tests.csproj', '--configuration', $configuration, '--no-build', '--no-restore', '--filter', 'Category!=Integration', '--logger', 'console;verbosity=normal')
    }
    if ($LegacyLocal) {
        foreach ($suite in @('Configuration', 'search', 'Branding', 'LoginUsability')) {
            Invoke-Checked 'powershell.exe' @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', "SteeLog/SteeLog/Tests/$suite/run.ps1")
        }
    }
    Write-Host 'Verificación local completada. No incluye SQL, diseñador interactivo ni publicación.'
} finally {
    if ($null -eq $previousHostAssembly) { Remove-Item Env:STEELOG_HOST_ASSEMBLY -ErrorAction SilentlyContinue }
    else { $env:STEELOG_HOST_ASSEMBLY = $previousHostAssembly }
    Pop-Location
}
