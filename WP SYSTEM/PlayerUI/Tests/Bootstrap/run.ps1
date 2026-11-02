$ErrorActionPreference = 'Stop'
$db = 'steelog_test_' + [Guid]::NewGuid().ToString('N')
if ($db -cnotmatch '^steelog_test_[0-9a-f]{32}$') { throw 'Invalid database name' }
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../../../..')).Path
function CheckNative { if ($LASTEXITCODE -ne 0) { throw "Native command failed: $LASTEXITCODE" } }
$guard = "DO `$guard`$ BEGIN IF current_database() <> '$db' THEN RAISE EXCEPTION 'Wrong test database'; END IF; END `$guard`$;"
function Invoke-TestSql([string]$sql) {
 ($guard + "`n" + $sql) | docker exec -i steelog-db psql -U postgres -d $db -v ON_ERROR_STOP=1 -q
 CheckNative
}
$created = $false
try {
 docker exec steelog-db psql -U postgres -d postgres -v ON_ERROR_STOP=1 -c "CREATE DATABASE $db" | Out-Null; CheckNative; $created = $true
 foreach ($script in @('01-schema.sql','02-seed.sql')) { Invoke-TestSql (Get-Content -Raw -Encoding UTF8 (Join-Path $repo "docker/init/$script")) }
 Invoke-TestSql @'
CREATE TABLE public.bootstrap_test_state (consumed bigint NOT NULL);
DO $$
DECLARE inserted bigint; consumed bigint;
BEGIN
 INSERT INTO public.ubicacion (nombre) VALUES ('bootstrap test') RETURNING id_ubicacion INTO inserted;
 IF inserted <= 4 THEN RAISE EXCEPTION 'Default ID did not advance: %', inserted; END IF;
 FOR i IN 1..8 LOOP SELECT nextval(pg_get_serial_sequence('public.ubicacion','id_ubicacion')) INTO consumed; END LOOP;
 INSERT INTO public.ubicacion (id_ubicacion, nombre) VALUES (consumed, 'deleted test');
 DELETE FROM public.ubicacion WHERE id_ubicacion = consumed;
 INSERT INTO public.bootstrap_test_state VALUES (consumed);
 RAISE NOTICE 'first default ID: %, last consumed: %', inserted, consumed;
END $$;
'@
 Invoke-TestSql (Get-Content -Raw -Encoding UTF8 (Join-Path $repo 'docker/init/02-seed.sql'))
 Invoke-TestSql @'
DO $$
DECLARE consumed bigint; next_id bigint;
BEGIN
 SELECT bootstrap_test_state.consumed INTO consumed FROM public.bootstrap_test_state;
 INSERT INTO public.ubicacion (nombre) VALUES ('after reseed') RETURNING id_ubicacion INTO next_id;
 IF next_id <= consumed THEN RAISE EXCEPTION 'Sequence rewound: % <= %', next_id, consumed; END IF;
 RAISE NOTICE 'after reseed ID: %, last consumed: %', next_id, consumed;
END $$;
'@
 Write-Host 'PASS fresh bootstrap and consumed sequence preservation'
} finally {
 if ($created -and $db -cmatch '^steelog_test_[0-9a-f]{32}$') {
  docker exec steelog-db psql -U postgres -d postgres -v ON_ERROR_STOP=1 -c "DROP DATABASE $db WITH (FORCE)" | Out-Null; CheckNative
  Write-Host "Dropped test database: $db"
 }
}
