[CmdletBinding()]
param([switch]$SinInterfaz)

$ErrorActionPreference = 'Stop'
$raiz = Split-Path -Parent $PSScriptRoot
$archivoCompose = Join-Path $raiz 'docker-compose.yml'
# Conserva el nombre de proyecto predeterminado que se usaba desde A:\SteeLog.
$compose = "compose --project-name steelog --project-directory `"$raiz`" --file `"$archivoCompose`""

function Invoke-Docker([string]$Argumentos, [int]$TiempoMs = 10000) {
    $inicio = New-Object System.Diagnostics.ProcessStartInfo
    $inicio.FileName = (Get-Command docker -ErrorAction Stop).Source
    $inicio.Arguments = $Argumentos
    $inicio.WorkingDirectory = $raiz
    $inicio.UseShellExecute = $false
    $inicio.CreateNoWindow = $true
    $inicio.RedirectStandardOutput = $true
    $inicio.RedirectStandardError = $true
    $proceso = [System.Diagnostics.Process]::Start($inicio)
    try {
        $salida = $proceso.StandardOutput.ReadToEndAsync()
        $errores = $proceso.StandardError.ReadToEndAsync()
        if (-not $proceso.WaitForExit([Math]::Max(1, $TiempoMs))) {
            $proceso.Kill()
            $proceso.WaitForExit()
            return @{ Codigo = -1; Excedido = $true; Salida = ''; Error = '' }
        }
        return @{ Codigo = $proceso.ExitCode; Excedido = $false; Salida = $salida.Result; Error = $errores.Result }
    }
    finally { $proceso.Dispose() }
}

Push-Location $raiz
try {
    if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
        throw 'Docker no esta instalado o no esta en PATH.'
    }
    if (-not $SinInterfaz -and -not (Get-Command lazydocker -ErrorAction SilentlyContinue)) {
        throw 'LazyDocker no esta instalado o no esta en PATH.'
    }
    $resultado = Invoke-Docker 'compose version'
    if ($resultado.Codigo -ne 0) { throw 'Docker Compose no esta disponible o no respondio en 10 segundos.' }
    $resultado = Invoke-Docker 'info --format {{.ServerVersion}}'
    if ($resultado.Codigo -ne 0) { throw 'El motor de Docker no responde. Inicia Docker Desktop y espera a que este listo.' }
    $resultado = Invoke-Docker "$compose config --quiet"
    if ($resultado.Codigo -ne 0) { throw 'La configuracion de Compose no es valida o no respondio en 10 segundos.' }
    $resultado = Invoke-Docker "$compose up -d" 120000
    if ($resultado.Excedido) {
        throw 'Compose up excedio 120 segundos. El cliente se detuvo, pero Docker podria seguir creando recursos; revisa el estado del proyecto steelog antes de reintentar.'
    }
    if ($resultado.Codigo -ne 0) {
        Write-Host $resultado.Error
        throw 'No se pudo iniciar Compose. Comprueba si el puerto 5435 o el nombre steelog-db estan ocupados.'
    }
    if ($resultado.Error) { Write-Host $resultado.Error }

    $reloj = [System.Diagnostics.Stopwatch]::StartNew()
    $listo = $false
    while ($reloj.ElapsedMilliseconds -lt 60000) {
        $restante = 60000 - $reloj.ElapsedMilliseconds
        $resultado = Invoke-Docker "$compose exec -T db pg_isready -U postgres -d inventario_db" ([int][Math]::Min(5000, $restante))
        if ($resultado.Codigo -eq 0) { $listo = $true; break }
        $restante = 60000 - $reloj.ElapsedMilliseconds
        if ($restante -gt 0) { Start-Sleep -Milliseconds ([int][Math]::Min(2000, $restante)) }
    }
    if (-not $listo) { throw 'PostgreSQL no estuvo listo en 60 segundos. Revisa los logs del proyecto steelog.' }
    Write-Host 'PostgreSQL acepta conexiones. La aplicacion WinForms se ejecuta en Windows, no dentro de Docker.'
    if (-not $SinInterfaz) {
        lazydocker --project steelog --file $archivoCompose
        if ($LASTEXITCODE -ne 0) { throw 'LazyDocker termino con un error.' }
    }
}
finally { Pop-Location }
