[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$Destino)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
if (-not [IO.Path]::IsPathRooted($Destino)) { throw 'Destino debe ser absoluto.' }
$dest = [IO.Path]::GetFullPath($Destino)
if ((Test-Path -LiteralPath $dest) -or (Test-Path -LiteralPath ($dest+'.zip'))) { throw 'El destino debe ser nuevo; no se sobrescriben paquetes.' }
$snapshot = Join-Path ([IO.Path]::GetTempPath()) ('steelog-package-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $snapshot | Out-Null
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
$msbuild = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
if (-not $msbuild) { throw 'Se requiere MSBuild de Visual Studio y targeting pack net48.' }
# Solo fuentes versionadas. Nunca copiar bin/obj, sesión DPAPI o configuración privada.
$gitEncoding = [Console]::OutputEncoding
try {
    [Console]::OutputEncoding = New-Object Text.UTF8Encoding($false)
    $tracked = git -c core.quotepath=false -C $repo ls-files -- SteeLog demo docker/demo scripts/ejecutar-demo.ps1 scripts/preparar-demo.ps1 scripts/tests/docker-exec-api.ps1 docs/arquitectura/GUIA_DEMO_Y_DEFENSA.md
    $gitExit = $LASTEXITCODE
} finally { [Console]::OutputEncoding = $gitEncoding }
if ($gitExit -ne 0) { throw 'Git ls-files falló.' }
if ($LASTEXITCODE -ne 0) { throw 'No se pudieron enumerar fuentes versionadas.' }
foreach ($relative in $tracked) {
    if ($relative -match '(^|/)(bin|obj)/|(^|/)appsettings(?!\.example\.json$).*\.json$|\.user$|session\.dat$') { continue }
    $target = Join-Path $snapshot $relative
    New-Item -ItemType Directory -Force -Path (Split-Path $target -Parent) | Out-Null
    Copy-Item -LiteralPath (Join-Path $repo $relative) -Destination $target
}
# Fallar cerrado hasta que la instalación configure su DB nueva. Este JSON también se embebe.
$config = '{"ConnectionStrings":{"DefaultConnection":"Host=127.0.0.1;Port=1;Database=steelog_demo_sin_configurar;Username=postgres;Password=demo-no-configurada;Timeout=2"}}'
$utf8 = New-Object Text.UTF8Encoding($false)
[IO.File]::WriteAllText((Join-Path $snapshot 'SteeLog/SteeLog/appsettings.json'), $config, $utf8)
& $msbuild (Join-Path $snapshot 'SteeLog/SteeLog.sln') /restore /p:RestorePackagesConfig=true /p:Configuration=Release /m /v:minimal /nologo
if ($LASTEXITCODE -ne 0) { throw "Build falló. Snapshot conservado: $snapshot" }
$build = Join-Path $snapshot 'SteeLog/SteeLog/bin/Release'
New-Item -ItemType Directory -Path $dest | Out-Null
foreach ($file in @('SteeLog.exe','SteeLog.exe.config')) { Copy-Item -LiteralPath (Join-Path $build $file) -Destination $dest }
[IO.File]::WriteAllText((Join-Path $dest 'appsettings.json'), $config, $utf8)
New-Item -ItemType Directory -Path (Join-Path $dest 'demo') | Out-Null
Copy-Item -LiteralPath (Join-Path $snapshot 'demo/actualizaciones') -Destination (Join-Path $dest 'demo/actualizaciones') -Recurse
New-Item -ItemType Directory -Path (Join-Path $dest 'database'),(Join-Path $dest 'tools') | Out-Null
Copy-Item (Join-Path $snapshot 'docker/demo/*') (Join-Path $dest 'database')
Copy-Item -LiteralPath (Join-Path $snapshot 'scripts/ejecutar-demo.ps1') -Destination (Join-Path $dest 'tools/ejecutar-demo.ps1')
Copy-Item -LiteralPath (Join-Path $snapshot 'scripts/preparar-demo.ps1') -Destination (Join-Path $dest 'tools/preparar-demo.ps1')
Copy-Item -LiteralPath (Join-Path $snapshot 'scripts/tests/docker-exec-api.ps1') -Destination (Join-Path $dest 'tools/docker-exec-api.ps1')
Copy-Item -LiteralPath (Join-Path $snapshot 'docs/arquitectura/GUIA_DEMO_Y_DEFENSA.md') -Destination (Join-Path $dest 'LEEME.md')
$commit = git -C $repo rev-parse HEAD
if ($LASTEXITCODE -ne 0) { throw 'No se pudo registrar el commit de origen.' }
$hashes = @(Get-ChildItem -LiteralPath $dest -Recurse -File | ForEach-Object {
    [ordered]@{ archivo=$_.FullName.Substring($dest.TrimEnd('\').Length+1).Replace('\','/'); sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash }
})
$dirty = @(git -C $repo status --porcelain)
if ($LASTEXITCODE -ne 0) { throw 'No se pudo consultar el árbol de origen.' }
$manifest = [ordered]@{ commit=$commit; arbolModificado=($dirty.Count -gt 0); configuracion='Release'; perfil='demo local sin configurar'; archivos=$hashes }
[IO.File]::WriteAllText((Join-Path $dest 'manifest.json'), ($manifest | ConvertTo-Json -Depth 5), $utf8)
Compress-Archive -Path (Join-Path $dest '*') -DestinationPath ($dest+'.zip') -ErrorAction Stop
"Paquete: $dest.zip"
"Snapshot saneado conservado: $snapshot"
'No se ejecutó Main ni se publicó el paquete. La primera restauración requiere dependencias preparadas.'
