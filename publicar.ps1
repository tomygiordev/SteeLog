# ============================================================
# publicar.ps1 — Sube el .exe de app.publish a GitHub Release
# Ejecutar DESPUES de hacer Build Release en Visual Studio
# ============================================================

$ErrorActionPreference = "Stop"
trap {
    Write-Host ""
    Write-Host "===== ERROR =====" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host ""
    Read-Host "Presiona Enter para cerrar"
    exit 1
}

$token       = if ($env:GITHUB_TOKEN) { $env:GITHUB_TOKEN } else { "" }
$owner       = "tomygiordev"
$repo        = "SteeLog"
$versionFile = "$PSScriptRoot\WP SYSTEM\PlayerUI\AppVersion.cs"
$exePath     = "$PSScriptRoot\WP SYSTEM\PlayerUI\bin\Release\app.publish\SteeLog.exe"
$assetName   = "SteeLog.exe"

function Get-AppVersionFromSource {
    param([string]$Path)

    $content = Get-Content $Path -Raw
    if ($content -match 'Current = "(\d+\.\d+\.\d+)"') {
        return $Matches[1]
    }

    throw "No se pudo leer la version de AppVersion.cs"
}

function Test-ExeContainsVersion {
    param(
        [string]$Path,
        [string]$Version
    )

    $bytes = [System.IO.File]::ReadAllBytes($Path)
    return [System.Text.Encoding]::UTF8.GetString($bytes).Contains($Version) `
        -or [System.Text.Encoding]::Unicode.GetString($bytes).Contains($Version)
}

function Get-LatestLocalVersionTag {
    $tag = git -C $PSScriptRoot tag --list "v[0-9]*" --sort=-v:refname | Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($tag)) {
        return $null
    }

    return $tag.Trim()
}

function Update-LocalTags {
    git -C $PSScriptRoot fetch --tags origin --quiet
    if ($LASTEXITCODE -ne 0) {
        throw "No se pudieron actualizar los tags desde origin."
    }
}

# --- Verificar que el exe existe ---
if (-not (Test-Path $exePath)) {
    Write-Host "ERROR: No se encontro el exe en app.publish." -ForegroundColor Red
    Write-Host "Hace Build > Publish en Visual Studio primero." -ForegroundColor Yellow
    Read-Host "Presiona Enter para cerrar"
    exit 1
}

# --- Leer version de AppVersion.cs ---
$content = Get-Content $versionFile -Raw
$version = Get-AppVersionFromSource -Path $versionFile
$tagName = "v$version"

Write-Host "Version a publicar: $version" -ForegroundColor Cyan

if (-not (Test-ExeContainsVersion -Path $exePath -Version $version)) {
    Write-Host "ERROR: El exe publicado no contiene la version $version." -ForegroundColor Red
    Write-Host "Hace Publish en Visual Studio despues de actualizar AppVersion.cs y volve a ejecutar este script." -ForegroundColor Yellow
    exit 1
}

Write-Host "Actualizando tags locales..." -ForegroundColor Cyan
Update-LocalTags

$latestTag = Get-LatestLocalVersionTag
if ($latestTag) {
    $commitsDesdeUltimoTag = [int](git -C $PSScriptRoot rev-list "$latestTag..HEAD" --count)
    if ($commitsDesdeUltimoTag -eq 0) {
        Write-Host "ERROR: No hay commits nuevos desde $latestTag. Se cancela para evitar una release vacia." -ForegroundColor Red
        Write-Host "Si cambiaste algo, primero hace commit local. Si no cambiaste nada, ya no hace falta publicar otra version." -ForegroundColor Yellow
        exit 1
    }
}

$headers = @{
    Authorization = "Bearer $token"
    Accept        = "application/vnd.github+json"
    "User-Agent"  = "SteeLog-Publisher"
}

# --- Crear release ---
$body = @{
    tag_name   = $tagName
    name       = $tagName
    body       = "Release v$version"
    draft      = $false
    prerelease = $false
    make_latest = "true"
} | ConvertTo-Json

Write-Host "Verificando si $tagName ya existe en GitHub..." -ForegroundColor Cyan
$releaseExistente = $null
try {
    $releaseExistente = Invoke-RestMethod -Uri "https://api.github.com/repos/$owner/$repo/releases/tags/$tagName" `
        -Method GET -Headers $headers
} catch {
    if ($_.Exception.Response -eq $null -or [int]$_.Exception.Response.StatusCode -ne 404) {
        throw
    }
}

if ($releaseExistente -ne $null) {
    Write-Host "ERROR: Ya existe una release para $tagName. No se publica nada." -ForegroundColor Red
    exit 1
}

Write-Host "Creando release $tagName en GitHub..." -ForegroundColor Cyan
$release = Invoke-RestMethod -Uri "https://api.github.com/repos/$owner/$repo/releases" `
    -Method POST -Headers $headers -Body $body -ContentType "application/json"

$uploadUrl = $release.upload_url -replace '\{\?name,label\}', ''

# --- Subir exe ---
Write-Host "Subiendo $assetName..." -ForegroundColor Cyan
$uploadHeaders = @{
    Authorization  = "Bearer $token"
    Accept         = "application/vnd.github+json"
    "User-Agent"   = "SteeLog-Publisher"
    "Content-Type" = "application/octet-stream"
}
Invoke-RestMethod -Uri "${uploadUrl}?name=$assetName" `
    -Method POST -Headers $uploadHeaders `
    -Body ([System.IO.File]::ReadAllBytes($exePath)) | Out-Null

# --- Incrementar version para la proxima ---
$parts = $version -split '\.'
$nextVersion = "$($parts[0]).$($parts[1]).$([int]$parts[2] + 1)"
$newContent = $content -replace 'Current = "\d+\.\d+\.\d+"', "Current = `"$nextVersion`""
Set-Content $versionFile $newContent -NoNewline

Write-Host ""
Write-Host "LISTO! Release v$version publicado." -ForegroundColor Green
Write-Host "AppVersion.cs listo para la proxima: $nextVersion" -ForegroundColor DarkCyan
Read-Host "Presiona Enter para cerrar"
