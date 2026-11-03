-- F4: conserva evidencia inequívoca para revertir traslados nuevos.
-- Aplicar explícitamente en una base respaldada; nunca se aplica al arrancar.
BEGIN;
ALTER TABLE public.insumo_movimientos
    ADD COLUMN IF NOT EXISTS id_obra_origen_snapshot integer,
    ADD COLUMN IF NOT EXISTS id_obra_destino_snapshot integer,
    ADD COLUMN IF NOT EXISTS consumido_origen_snapshot boolean,
    ADD COLUMN IF NOT EXISTS consumido_destino_snapshot boolean,
    ADD COLUMN IF NOT EXISTS snapshot_completo boolean NOT NULL DEFAULT false;
ALTER TABLE public.movimientomaterial
    ADD COLUMN IF NOT EXISTS impacto_stock_real numeric(10,2);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conrelid = 'public.insumo_movimientos'::regclass
                   AND conname = 'insumo_movimientos_snapshot_completo_check') THEN
        ALTER TABLE public.insumo_movimientos ADD CONSTRAINT insumo_movimientos_snapshot_completo_check
            CHECK (NOT snapshot_completo OR
                   (consumido_origen_snapshot IS NOT NULL AND consumido_destino_snapshot IS NOT NULL));
    END IF;
END $$;
COMMIT;
