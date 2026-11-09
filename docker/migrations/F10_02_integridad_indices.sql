-- F10: integridad exigida por el dominio y acceso por FK. No corrige datos.
BEGIN;
-- Excluir escrituras concurrentes entre el diagnÃ³stico y la creaciÃ³n de restricciones.
LOCK TABLE public.materiales, public.movimientomaterial, public.movimientos, public.tipo_movimiento IN SHARE ROW EXCLUSIVE MODE;
DO $$
DECLARE item record; conflictos bigint;
BEGIN
    FOR item IN SELECT * FROM (VALUES
        ('materiales','stockactual'),
        ('materiales','stockminimo'),
        ('materiales','preciounitario'),
        ('movimientomaterial','cantidad'),
        ('movimientos','id_origen'),
        ('movimientos','tipo_origen'),
        ('movimientos','tipo_destino'),
        ('movimientos','id_tipomovimiento'),
        ('movimientos','costoflete')
    ) AS columnas(tabla,columna) LOOP
        EXECUTE format('SELECT count(*) FROM public.%I WHERE %I IS NULL',item.tabla,item.columna) INTO conflictos;
        IF conflictos > 0 THEN RAISE EXCEPTION 'F10: %.%: % filas NULL; no se modificaron datos',item.tabla,item.columna,conflictos; END IF;
        EXECUTE format('ALTER TABLE public.%I ALTER COLUMN %I SET NOT NULL',item.tabla,item.columna);
    END LOOP;
    SELECT count(*) INTO conflictos FROM public.movimientos m LEFT JOIN public.tipo_movimiento t USING(id_tipomovimiento) WHERE t.id_tipomovimiento IS NULL;
    IF conflictos > 0 THEN RAISE EXCEPTION 'F10: movimientos.id_tipomovimiento: % referencias huerfanas; no se modificaron datos',conflictos; END IF;
    IF NOT EXISTS (SELECT FROM pg_constraint WHERE conrelid='public.movimientos'::regclass AND conname='movimientos_tipo_movimiento_fkey') THEN
        ALTER TABLE public.movimientos ADD CONSTRAINT movimientos_tipo_movimiento_fkey FOREIGN KEY (id_tipomovimiento) REFERENCES public.tipo_movimiento(id_tipomovimiento);
    END IF;
    IF NOT EXISTS (SELECT FROM pg_constraint WHERE conrelid='public.materiales'::regclass AND conname='f10_materiales_precio_no_negativo') THEN
        SELECT count(*) INTO conflictos FROM public.materiales WHERE NOT (preciounitario >= 0);
        IF conflictos > 0 THEN RAISE EXCEPTION 'F10: f10_materiales_precio_no_negativo: % filas en conflicto; no se modificaron datos',conflictos; END IF;
        ALTER TABLE public.materiales ADD CONSTRAINT f10_materiales_precio_no_negativo CHECK (preciounitario >= 0);
    END IF;
    IF NOT EXISTS (SELECT FROM pg_constraint WHERE conrelid='public.movimientomaterial'::regclass AND conname='f10_linea_precio_no_negativo') THEN
        SELECT count(*) INTO conflictos FROM public.movimientomaterial WHERE NOT (precio_unitario >= 0);
        IF conflictos > 0 THEN RAISE EXCEPTION 'F10: f10_linea_precio_no_negativo: % filas en conflicto; no se modificaron datos',conflictos; END IF;
        ALTER TABLE public.movimientomaterial ADD CONSTRAINT f10_linea_precio_no_negativo CHECK (precio_unitario >= 0);
    END IF;
    IF NOT EXISTS (SELECT FROM pg_constraint WHERE conrelid='public.movimientomaterial'::regclass AND conname='f10_linea_stock_no_negativo') THEN
        SELECT count(*) INTO conflictos FROM public.movimientomaterial WHERE NOT (stock_actual >= 0);
        IF conflictos > 0 THEN RAISE EXCEPTION 'F10: f10_linea_stock_no_negativo: % filas en conflicto; no se modificaron datos',conflictos; END IF;
        ALTER TABLE public.movimientomaterial ADD CONSTRAINT f10_linea_stock_no_negativo CHECK (stock_actual >= 0);
    END IF;
    IF NOT EXISTS (SELECT FROM pg_constraint WHERE conrelid='public.movimientos'::regclass AND conname='f10_movimiento_importes_no_negativos') THEN
        SELECT count(*) INTO conflictos FROM public.movimientos WHERE NOT (costoflete >= 0 AND precio_total >= 0);
        IF conflictos > 0 THEN RAISE EXCEPTION 'F10: f10_movimiento_importes_no_negativos: % filas en conflicto; no se modificaron datos',conflictos; END IF;
        ALTER TABLE public.movimientos ADD CONSTRAINT f10_movimiento_importes_no_negativos CHECK (costoflete >= 0 AND precio_total >= 0);
    END IF;
    -- cantidad tambiÃ©n registra ajustes internos CON SIGNO hasta el mÃ¡ximo de Material.
    -- numeric(10,4) no alcanzaba para el delta vÃ¡lido de 99999999.99.
    IF EXISTS (SELECT FROM information_schema.columns WHERE table_schema='public' AND table_name='movimientomaterial' AND column_name='cantidad' AND numeric_precision=10 AND numeric_scale=4) THEN
        ALTER TABLE public.movimientomaterial ALTER COLUMN cantidad TYPE numeric(12,4);
    END IF;
END $$;
ALTER TABLE public.movimientos ALTER COLUMN costoflete SET DEFAULT 0;
CREATE INDEX IF NOT EXISTS idx_f10_materiales_categoria ON public.materiales (id_categoria);
CREATE INDEX IF NOT EXISTS idx_f10_materiales_ubicacion ON public.materiales (id_ubicacion);
CREATE INDEX IF NOT EXISTS idx_f10_proveedores_materiales ON public.materiales_proveedores (id_proveedor);
CREATE INDEX IF NOT EXISTS idx_f10_lineas_material_movimiento ON public.movimientomaterial (id_material, id_movimiento);
CREATE INDEX IF NOT EXISTS idx_f10_lineas_movimiento ON public.movimientomaterial (id_movimiento);
CREATE INDEX IF NOT EXISTS idx_f10_lineas_recorte ON public.movimientomaterial (id_recorte);
CREATE INDEX IF NOT EXISTS idx_f10_movimientos_tipo_fecha ON public.movimientos (id_tipomovimiento, fecha, id_movimiento DESC);
CREATE INDEX IF NOT EXISTS idx_f10_movimientos_fecha ON public.movimientos (fecha DESC, id_movimiento DESC);
CREATE INDEX IF NOT EXISTS idx_f10_movimientos_obra ON public.movimientos (id_obra);
CREATE INDEX IF NOT EXISTS idx_f10_movimientos_flete ON public.movimientos (id_flete);
CREATE INDEX IF NOT EXISTS idx_f10_recortes_material ON public.recortes (id_material, medida);
CREATE INDEX IF NOT EXISTS idx_f10_insumo_historial ON public.insumo_movimientos (id_insumo, id DESC);
CREATE INDEX IF NOT EXISTS idx_f10_insumo_movimiento ON public.insumo_movimientos (id_movimiento);
CREATE INDEX IF NOT EXISTS idx_f10_herramienta_historial ON public.herramienta_movimientos (id_herramienta, fecha DESC);
CREATE INDEX IF NOT EXISTS idx_f10_herramientas_obra ON public.herramientas (id_obra);
CREATE INDEX IF NOT EXISTS idx_f10_herramientas_ubicacion ON public.herramientas (id_ubicacion);
CREATE INDEX IF NOT EXISTS idx_f10_insumos_obra ON public.insumos (id_obra);
CREATE INDEX IF NOT EXISTS idx_f10_insumos_ubicacion ON public.insumos (id_ubicacion);
CREATE INDEX IF NOT EXISTS idx_f10_ordenes_proveedor ON public.ordenes_compra (id_proveedor);
CREATE INDEX IF NOT EXISTS idx_f10_ordenes_destino ON public.ordenes_compra (id_destino);
COMMIT;
