-- F10: comportamiento explícito; NULL conserva la clasificación histórica.
BEGIN;
ALTER TABLE public.tipo_movimiento ADD COLUMN IF NOT EXISTS impacto text;
-- Mismo orden que ClasificadorMovimiento: registro, excepciones, entradas, salidas, destino.
UPDATE public.tipo_movimiento
SET impacto = CASE
    WHEN upper(coalesce(nombre,'')) LIKE '%REGISTRO INTERNO%' THEN 'registro_interno'
    WHEN upper(coalesce(nombre,'')) LIKE ANY (ARRAY['%APORTE DEL CLIENTE%','%APORTE DE CLIENTE%','%COMPRA EVENTUAL%']) THEN 'sin_impacto'
    WHEN upper(coalesce(nombre,'')) LIKE ANY (ARRAY['%ENTRADA%','%INGRESO%','%CARGA INICIAL%','%COMPRA%','%DEVOLUCION%','%DEVOLUCIÓN%','%AJUSTE POSITIVO%']) THEN 'entrada'
    WHEN upper(coalesce(nombre,'')) LIKE ANY (ARRAY['%SALIDA%','%EGRESO%','%CONSUMO%','%VENTA%','%ENVÍO%','%ENVIO%','%USO INTERNO%','%AJUSTE NEGATIVO%']) THEN 'salida'
    ELSE 'segun_destino'
END
WHERE impacto IS NULL;
DO $$
DECLARE conflictos bigint;
BEGIN
    IF NOT EXISTS (SELECT FROM pg_constraint WHERE conrelid='public.tipo_movimiento'::regclass AND conname='tipo_movimiento_impacto_check') THEN
        ALTER TABLE public.tipo_movimiento ADD CONSTRAINT tipo_movimiento_impacto_check
            CHECK (impacto IN ('entrada','salida','sin_impacto','registro_interno','segun_destino'));
    END IF;
    SELECT count(*) INTO conflictos FROM public.tipo_movimiento WHERE nombre IS NULL;
    IF conflictos > 0 THEN RAISE EXCEPTION 'F10: tipo_movimiento.nombre: % filas NULL; no se modificaron datos', conflictos; END IF;
    SELECT count(*) INTO conflictos FROM (SELECT nombre FROM public.tipo_movimiento GROUP BY nombre HAVING count(*)>1) duplicados;
    IF conflictos > 0 THEN RAISE EXCEPTION 'F10: tipo_movimiento.nombre: % nombres duplicados; no se modificaron datos', conflictos; END IF;
    ALTER TABLE public.tipo_movimiento ALTER COLUMN nombre SET NOT NULL;
    IF NOT EXISTS (SELECT FROM pg_constraint WHERE conrelid='public.tipo_movimiento'::regclass AND conname='tipo_movimiento_nombre_key') THEN
        ALTER TABLE public.tipo_movimiento ADD CONSTRAINT tipo_movimiento_nombre_key UNIQUE (nombre);
    END IF;
END $$;
COMMIT;
