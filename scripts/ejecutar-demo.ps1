param([switch]$SinEjecutar)
$ErrorActionPreference = 'Stop'
$state = Get-Content -Raw -Encoding UTF8 (Join-Path $PSScriptRoot 'instalacion.json') | ConvertFrom-Json
if ($state.estado -cne 'preparada') { throw 'La instalación no está preparada.' }
if ($state.id -cnotmatch '^[0-9a-f]{32}$' -or $state.contenedor -cne "steelog-demo-$($state.id)" -or $state.base -cne "steelog_demo_$($state.id)" -or $state.volumen -cne "steelog-demo-data-$($state.id)") { throw 'Identidad demo inválida.' }
$context = docker context inspect | ConvertFrom-Json
if ($LASTEXITCODE -ne 0) { throw 'Docker no disponible.' }
$endpoint = $context.Endpoints.docker.Host
if ($env:DOCKER_HOST) { $endpoint = $env:DOCKER_HOST }
if ($endpoint -cne 'tcp://127.0.0.1:2375') { throw 'Se requiere el daemon Docker local preparado.' }
$container = docker inspect $state.contenedor | ConvertFrom-Json
if ($LASTEXITCODE -ne 0 -or $container.Id -cne $state.containerId -or $container.Name -cne "/$($state.contenedor)" -or $container.Config.Labels.'steelog.demo' -cne $state.id -or -not $container.State.Running) { throw 'Contenedor demo detenido o identidad no validada; no se inicia automáticamente.' }
$binding = @($container.NetworkSettings.Ports.'5432/tcp')
if ($binding.Count -ne 1 -or $binding[0].HostIp -cne '127.0.0.1' -or [int]$binding[0].HostPort -le 0 -or [int]$binding[0].HostPort -eq 5435) { throw 'Puerto demo no válido.' }
# Docker puede reasignar el puerto dinámico después de reiniciar el contenedor.
$config = @{ConnectionStrings=@{DefaultConnection="Host=127.0.0.1;Port=$($binding[0].HostPort);Database=$($state.base);Username=postgres;Password=tesispassword123"}} | ConvertTo-Json
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'app/appsettings.json'), $config, (New-Object Text.UTF8Encoding($false)))
$root = Join-Path $PSScriptRoot 'datos'
if ((Get-Content -Raw -LiteralPath (Join-Path $root '.steelog-demo')).Trim() -cne 'steelog-demo-v1') { throw 'Datos demo no identificados.' }
if ($SinEjecutar) { 'Conexión demo actualizada, identidad y raíz verificadas. Main NO ejecutado.'; return }
$exe = Join-Path $PSScriptRoot 'app/SteeLog.exe'
$start = New-Object Diagnostics.ProcessStartInfo
$start.FileName = $exe
$start.WorkingDirectory = Split-Path $exe -Parent
$start.UseShellExecute = $false
$start.EnvironmentVariables['STEELOG_DEMO_ROOT'] = $root
# Solo este proceso hijo recibe la ruta. No se altera el perfil de Windows ni el entorno global.
$process = [Diagnostics.Process]::Start($start)
try { $process.WaitForExit(); if ($process.ExitCode -ne 0) { throw "SteeLog terminó con código $($process.ExitCode)" } }
finally { $process.Dispose() }
