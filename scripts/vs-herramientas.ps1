# Resuelve MSBuild y csc de Visual Studio / Build Tools con vswhere (sin rutas fijas).
# Uso: . (Join-Path $PSScriptRoot 'vs-herramientas.ps1'); $msbuild = Resolve-MSBuild; $csc = Resolve-Csc $msbuild
function Resolve-MSBuild {
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    $msbuild = $null
    if (Test-Path $vswhere) { $msbuild = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1 }
    if (-not $msbuild) { $command = Get-Command MSBuild.exe -ErrorAction SilentlyContinue; if ($command) { $msbuild = $command.Source } }
    if (-not $msbuild) { throw 'No se encontró MSBuild de Visual Studio; instalar Build Tools o agregar MSBuild.exe al PATH.' }
    return $msbuild
}
function Resolve-Csc([string]$MSBuild) {
    $bin = Split-Path -Parent $MSBuild
    foreach ($carpeta in @($bin, (Split-Path -Parent $bin))) {
        $csc = Join-Path $carpeta 'Roslyn\csc.exe'
        if (Test-Path $csc) { return $csc }
    }
    throw "No se encontró csc.exe junto a $MSBuild."
}
