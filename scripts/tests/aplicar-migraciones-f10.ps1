function Invoke-SteelogMigracionesNuevaInstalacion {
    param([string]$Container, [string]$Database, [string]$Repo)
    if ($Container -cnotmatch '^steelog-ca-test-[0-9a-f]{32}$' -or $Database -cnotmatch '^steelog_test_[0-9a-f]{32}$') { throw 'Destino de migraciones no permitido' }
    $migrations = @(Get-ChildItem -Path (Join-Path $Repo 'docker/migrations') -Filter 'F4*.sql' -File)
    if ($migrations.Count -ne 1) { throw 'Se requiere exactamente una migracion F4 explicita.' }
    $f10 = @(Get-ChildItem -Path (Join-Path $Repo 'docker/migrations') -Filter 'F10_*.sql' -File | Sort-Object Name)
    if ($f10.Count -lt 1) { throw 'No se encontraron migraciones F10' }
    $migrations += $f10
    $sql = "DO `$guard`$ BEGIN IF current_database() <> '$Database' THEN RAISE EXCEPTION 'Wrong test database'; END IF; END `$guard`$;`n"
    $sql += Get-Content -Raw -Encoding UTF8 (Join-Path $PSScriptRoot 'f10-firma.sql')
    foreach ($attempt in 1..2) {
        foreach ($migration in $migrations) { $sql += "`n" + (Get-Content -Raw -Encoding UTF8 $migration.FullName) }
        $sql += @'

DO $$ BEGIN
    IF (SELECT firma FROM firma_inicial_f10) IS DISTINCT FROM pg_temp.firma_f10() THEN
        RAISE EXCEPTION 'F4/F10 modificaron esquema o datos de una instalacion nueva';
    END IF;
END $$;
'@
    }
    $sql | Invoke-DockerExec $Container psql -U postgres -d $Database -v ON_ERROR_STOP=1 -q
    if ($LASTEXITCODE -ne 0) { throw 'Migraciones F4/F10 o verificacion de no-op fallaron' }
    'PASS F4/F10 ordenadas dos veces: esquema y datos sin cambios'
}
