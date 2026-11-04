[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$Paquete)
$ErrorActionPreference = 'Stop'
$source = (Resolve-Path -LiteralPath $Paquete).Path
$manifest = Get-Content -Raw -Encoding UTF8 (Join-Path $source 'manifest.json') | ConvertFrom-Json
$files = @($manifest.archivos)
if ($files.Count -lt 10) { throw 'Manifest incompleto.' }
foreach ($file in $files) {
    $path = [IO.Path]::GetFullPath((Join-Path $source $file.archivo))
    if (-not $path.StartsWith($source.TrimEnd('\')+'\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Ruta fuera del paquete.' }
    if ((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -cne $file.sha256) { throw "Hash inválido: $($file.archivo)" }
}
'PASS hashes del paquete'
$assembly = [Reflection.Assembly]::LoadFrom((Join-Path $source 'SteeLog.exe'))
if ($assembly.Location -cne (Join-Path $source 'SteeLog.exe')) { throw 'Se cargó otro ensamblado.' }
$stream = $assembly.GetManifestResourceStream('SteeLog.appsettings.json')
if (-not $stream) { throw 'Falta configuración embebida.' }
$reader = New-Object IO.StreamReader($stream)
try { $embedded = $reader.ReadToEnd() | ConvertFrom-Json } finally { $reader.Dispose() }
$external = Get-Content -Raw (Join-Path $source 'appsettings.json') | ConvertFrom-Json
$expected = 'Host=127.0.0.1;Port=1;Database=steelog_demo_sin_configurar;Username=postgres;Password=demo-no-configurada;Timeout=2'
if ($embedded.ConnectionStrings.DefaultConnection -cne $expected -or $external.ConnectionStrings.DefaultConnection -cne $expected) { throw 'El paquete no contiene únicamente la configuración demo saneada.' }
foreach ($name in @('steelog.domain','steelog.application','steelog.infrastructure','npgsql','itextsharp','microsoft.extensions.dependencyinjection')) {
    if ($assembly.GetManifestResourceNames() -cnotcontains "costura.$name.dll.compressed") { throw "Falta dependencia embebida $name" }
}
$logo = $assembly.GetManifestResourceStream('SteeLog.Resources.Branding.steelog-logo-dark.png')
if (-not $logo) { throw 'Falta logo real.' }; $logo.Dispose()
[Runtime.CompilerServices.RuntimeHelpers]::RunModuleConstructor($assembly.ManifestModule.ModuleHandle)
if ($assembly.GetTypes().Count -lt 20) { throw 'Tipos host no cargables.' }
'PASS carga metadata/recursos/Costura y configuración saneada; Main NO invocado'
$base = Join-Path ([IO.Path]::GetTempPath()) ('steelog-package-test-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $base | Out-Null
try {
    $unpacked = Join-Path $base 'extraido'
    Expand-Archive -LiteralPath ($source+'.zip') -DestinationPath $unpacked
    $zipManifest = Get-Content -Raw -Encoding UTF8 (Join-Path $unpacked 'manifest.json') | ConvertFrom-Json
    foreach ($file in $zipManifest.archivos) {
        if ((Get-FileHash -LiteralPath (Join-Path $unpacked $file.archivo) -Algorithm SHA256).Hash -cne $file.sha256) { throw 'ZIP no coincide con manifest.' }
    }
    'PASS extracción real de ZIP y hashes'
    foreach ($script in Get-ChildItem (Join-Path $unpacked 'tools') -Filter '*.ps1') {
        $errors = $null; $tokens = $null
        $null = [Management.Automation.Language.Parser]::ParseFile($script.FullName, [ref]$tokens, [ref]$errors)
        if ($errors) { throw "Script inválido: $($script.Name) $errors" }
    }
    [IO.File]::AppendAllText((Join-Path $unpacked 'LEEME.md'), 'Alteración intencional de prueba')
    $dest = Join-Path $base 'no-debe-crearse'
    $rejected = $false
    try { & (Join-Path $unpacked 'tools/preparar-demo.ps1') -Paquete $unpacked -Destino $dest }
    catch { if ($_.Exception.Message -notlike 'Hash alterado:*') { throw }; $rejected=$true }
    if (-not $rejected -or (Test-Path -LiteralPath $dest)) { throw 'No se rechazó la alteración antes de crear recursos.' }
    'PASS paquete alterado rechazado antes de crear directorio o llamar Docker'
    New-Item -ItemType Directory -Path $dest | Out-Null
    [IO.File]::WriteAllText((Join-Path $dest 'testigo'), 'conservar')
    $rejected=$false
    try { & (Join-Path $source 'tools/preparar-demo.ps1') -Paquete $source -Destino $dest }
    catch { if ($_.Exception.Message -notlike 'Destino debe ser nuevo*') { throw }; $rejected=$true }
    if (-not $rejected -or [IO.File]::ReadAllText((Join-Path $dest 'testigo')) -cne 'conservar') { throw 'No se preservó destino existente.' }
    'PASS instalación existente rechazada sin tocar su testigo'
} finally {
    $full = [IO.Path]::GetFullPath($base)
    if (-not $full.StartsWith([IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\')+'\', [StringComparison]::OrdinalIgnoreCase) -or [IO.Path]::GetFileName($full) -notmatch '^steelog-package-test-[0-9a-f]{32}$') { throw 'Limpieza rechazada.' }
    Remove-Item -LiteralPath $full -Recurse -Force
}
