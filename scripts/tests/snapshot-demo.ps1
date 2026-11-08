[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$scriptPath = Join-Path $PSScriptRoot '../empaquetar-demo.ps1'
$source = [IO.File]::ReadAllText((Resolve-Path -LiteralPath $scriptPath))
function Get-Section([string]$start, [string]$end) {
    $a = $source.IndexOf($start, [StringComparison]::Ordinal)
    $b = $source.IndexOf($end, [StringComparison]::Ordinal)
    if ($a -lt 0 -or $b -le $a -or $source.IndexOf($start, $a + $start.Length, [StringComparison]::Ordinal) -ge 0) { throw "Sección no identificada: $start" }
    return $source.Substring($a, $b - $a)
}
# Ejecuta los bloques reales, sin ejecutar MSBuild ni duplicar su lógica en la prueba.
$snapshotSection = Get-Section '# Solo fuentes versionadas.' '# Fallar cerrado'
$copySection = Get-Section "New-Item -ItemType Directory -Path (Join-Path `$dest 'demo')" '$commit = git -C $repo rev-parse HEAD'
$base = Join-Path ([IO.Path]::GetTempPath()) ('steelog-snapshot-test-' + [guid]::NewGuid().ToString('N'))
$repo = Join-Path $base 'repo'; $snapshot = Join-Path $base 'snapshot'; $dest = Join-Path $base 'paquete'
try {
    New-Item -ItemType Directory -Path $repo,$snapshot,$dest | Out-Null
    git -C $repo init -q
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo inicializar el fixture Git.' }
    $files = @('SteeLog/test.cs','demo/actualizaciones/manifiesto.json','demo/actualizaciones/paquete-demo.txt',
        'docker/demo/01-schema.sql','docker/demo/02-seed.sql','docker/demo/tablas.json',
        'scripts/ejecutar-demo.ps1','scripts/preparar-demo.ps1','scripts/tests/docker-exec-api.ps1',
        'docs/arquitectura/GUIA_DEMO_Y_DEFENSA.md')
    foreach ($file in $files) {
        $path = Join-Path $repo $file
        New-Item -ItemType Directory -Force -Path (Split-Path $path -Parent) | Out-Null
        [IO.File]::WriteAllText($path, "tracked:$file")
    }
    git -C $repo add -- .
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo registrar el fixture Git.' }
    [IO.File]::WriteAllText((Join-Path $repo 'demo/actualizaciones/sentinel-no-versionado.txt'), 'sentinel')
    [IO.File]::WriteAllText((Join-Path $repo 'docker/demo/sentinel-no-versionado.sql'), 'sentinel')
    & ([ScriptBlock]::Create($snapshotSection))
    & ([ScriptBlock]::Create($copySection))
    foreach ($file in @('demo/actualizaciones/manifiesto.json','database/01-schema.sql','tools/preparar-demo.ps1','LEEME.md')) {
        if (-not (Test-Path -LiteralPath (Join-Path $dest $file) -PathType Leaf)) { throw "Falta recurso versionado: $file" }
    }
    foreach ($file in @('demo/actualizaciones/sentinel-no-versionado.txt','database/sentinel-no-versionado.sql')) {
        if (Test-Path -LiteralPath (Join-Path $dest $file)) { throw "Se copió archivo no versionado: $file" }
    }
    'PASS snapshot y copia real excluyen archivos no versionados de demo y SQL'
} finally {
    $full = [IO.Path]::GetFullPath($base)
    if (-not $full.StartsWith([IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\')+'\', [StringComparison]::OrdinalIgnoreCase) -or [IO.Path]::GetFileName($full) -notmatch '^steelog-snapshot-test-[0-9a-f]{32}$') { throw 'Limpieza rechazada.' }
    if (Test-Path -LiteralPath $full) { Remove-Item -LiteralPath $full -Recurse -Force }
}
