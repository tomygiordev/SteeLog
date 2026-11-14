BEGIN;
-- F11: bitácora de auditoría. Solo agrega objetos nuevos; no modifica ni borra datos existentes.
CREATE TABLE IF NOT EXISTS public.bitacora_auditoria (
    id bigserial PRIMARY KEY,
    fecha timestamptz NOT NULL DEFAULT now(),
    usuario text,
    rol text,
    equipo text,
    version_app text,
    origen text NOT NULL CHECK (origen IN ('BD','APP')),
    -- INSERT/UPDATE/DELETE (origen BD) o evento de la app, por ejemplo ERROR, ERROR_NO_CONTROLADO (origen APP)
    accion text NOT NULL,
    entidad text,
    entidad_id text,
    descripcion text,
    datos_antes jsonb,
    -- en UPDATE solo guarda las columnas que cambiaron
    datos_despues jsonb
);
CREATE INDEX IF NOT EXISTS ix_bitacora_auditoria_fecha ON public.bitacora_auditoria (fecha DESC);
CREATE INDEX IF NOT EXISTS ix_bitacora_auditoria_entidad ON public.bitacora_auditoria (entidad, entidad_id);
CREATE INDEX IF NOT EXISTS ix_bitacora_auditoria_usuario ON public.bitacora_auditoria (usuario);

-- Trigger genérico AFTER ... FOR EACH ROW. El usuario llega por las variables de sesión
-- app.usuario, app.rol, app.equipo y app.version (set_config desde la app).
-- La auditoría NUNCA debe hacer fallar la operación del usuario: cualquier error se degrada a WARNING.
CREATE OR REPLACE FUNCTION public.bitacora_registrar_cambio() RETURNS trigger
    LANGUAGE plpgsql
    SECURITY DEFINER
    SET search_path = public
    AS $fn$
DECLARE
    v_antes jsonb;
    v_despues jsonb;
    v_id text;
    v_pk text;
BEGIN
    BEGIN
        IF TG_OP = 'INSERT' THEN
            v_despues := to_jsonb(NEW);
        ELSIF TG_OP = 'DELETE' THEN
            v_antes := to_jsonb(OLD);
        ELSE
            -- UPDATE sin cambios reales: no se registra
            IF to_jsonb(OLD) = to_jsonb(NEW) THEN
                RETURN NULL;
            END IF;
            -- solo las columnas que cambiaron
            SELECT jsonb_object_agg(o.key, o.value), jsonb_object_agg(n.key, n.value)
              INTO v_antes, v_despues
              FROM jsonb_each(to_jsonb(OLD)) o
              JOIN jsonb_each(to_jsonb(NEW)) n ON n.key = o.key
             WHERE o.value IS DISTINCT FROM n.value;
        END IF;

        -- entidad_id = primera columna de la clave primaria de la tabla
        SELECT a.attname INTO v_pk
          FROM pg_index i
          JOIN pg_attribute a ON a.attrelid = i.indrelid AND a.attnum = i.indkey[0]
         WHERE i.indrelid = TG_RELID AND i.indisprimary;
        IF v_pk IS NOT NULL THEN
            IF TG_OP = 'DELETE' THEN
                v_id := to_jsonb(OLD) ->> v_pk;
            ELSE
                v_id := to_jsonb(NEW) ->> v_pk;
            END IF;
        END IF;

        INSERT INTO public.bitacora_auditoria
            (usuario, rol, equipo, version_app, origen, accion, entidad, entidad_id, datos_antes, datos_despues)
        VALUES
            (NULLIF(current_setting('app.usuario', true), ''),
             NULLIF(current_setting('app.rol', true), ''),
             NULLIF(current_setting('app.equipo', true), ''),
             NULLIF(current_setting('app.version', true), ''),
             'BD', TG_OP, TG_TABLE_NAME, v_id, v_antes, v_despues);
    EXCEPTION WHEN OTHERS THEN
        RAISE WARNING 'bitacora_registrar_cambio falló en %: % (%)', TG_TABLE_NAME, SQLERRM, SQLSTATE;
    END;
    RETURN NULL;
END;
$fn$;

-- Activa (o reactiva) el trigger de bitácora en una tabla.
CREATE OR REPLACE FUNCTION public.bitacora_activar(tabla text) RETURNS void
    LANGUAGE plpgsql
    AS $fn$
BEGIN
    EXECUTE format('DROP TRIGGER IF EXISTS trg_bitacora ON public.%I', tabla);
    EXECUTE format('CREATE TRIGGER trg_bitacora AFTER INSERT OR UPDATE OR DELETE ON public.%I FOR EACH ROW EXECUTE FUNCTION public.bitacora_registrar_cambio()', tabla);
END;
$fn$;

-- Solo tablas de negocio, y solo si existen (la migración no falla si falta alguna).
-- materiales_proveedores y otras tablas sin PK simple quedan con entidad_id nulo si no tienen clave primaria.
DO $$
DECLARE t text;
BEGIN
    FOREACH t IN ARRAY ARRAY[
        'materiales','categorias','movimientos','movimientomaterial','herramientas',
        'herramienta_movimientos','insumos','insumo_movimientos','recortes','obras',
        'proveedores','ubicacion','fletes','canaletas_plegados','conteos_inventario',
        'conteos_inventario_detalle','ordenes_compra','ordenes_compra_detalle',
        'materiales_proveedores','reporte_stock_secciones','tipo_movimiento'
    ] LOOP
        IF EXISTS (SELECT FROM information_schema.tables WHERE table_schema = 'public' AND table_name = t AND table_type = 'BASE TABLE') THEN
            PERFORM public.bitacora_activar(t);
        END IF;
    END LOOP;
END $$;

COMMIT;
