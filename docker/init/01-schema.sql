DO $$
BEGIN
  IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = 'anon') THEN
    CREATE ROLE anon NOLOGIN;
  END IF;
  IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = 'authenticated') THEN
    CREATE ROLE authenticated NOLOGIN;
  END IF;
  IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = 'service_role') THEN
    CREATE ROLE service_role NOLOGIN;
  END IF;
  IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = 'supabase_admin') THEN
    CREATE ROLE supabase_admin NOLOGIN;
  END IF;
END
$$;
--
-- PostgreSQL database dump
--

\restrict 6KHWA15UrKVDq4paFUiUBPUGpyIuNXpoPK3ctuZCThNA3mQ7I4h8GQCTuOfiqEa

-- Dumped from database version 17.6
-- Dumped by pg_dump version 18.1

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET transaction_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--
-- Name: public; Type: SCHEMA; Schema: -; Owner: pg_database_owner
--

CREATE SCHEMA IF NOT EXISTS public;


ALTER SCHEMA public OWNER TO pg_database_owner;

--
-- Name: SCHEMA public; Type: COMMENT; Schema: -; Owner: pg_database_owner
--

COMMENT ON SCHEMA public IS 'standard public schema';


--
-- Name: var_unidad; Type: TYPE; Schema: public; Owner: postgres
--

CREATE TYPE public.var_unidad AS ENUM (
    'kg',
    'un',
    'm',
    'm2',
    'm3'
);


ALTER TYPE public.var_unidad OWNER TO postgres;

--
-- Name: check_invoice_payment_consistency(); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.check_invoice_payment_consistency() RETURNS trigger
    LANGUAGE plpgsql
    SET search_path TO 'public', 'pg_temp'
    AS $$
DECLARE
  invoice_id INTEGER;
  invoice facturas%ROWTYPE;
  recorded NUMERIC;
BEGIN
  IF TG_TABLE_NAME = 'facturas' THEN invoice_id := NEW.id_factura;
  ELSIF TG_OP = 'DELETE' THEN invoice_id := OLD.id_factura;
  ELSE invoice_id := NEW.id_factura;
  END IF;
  SELECT * INTO invoice FROM facturas WHERE id_factura = invoice_id FOR UPDATE;
  IF NOT FOUND THEN RETURN NULL; END IF;
  SELECT COALESCE(sum(importe), 0) INTO recorded
    FROM factura_pagos WHERE id_factura = invoice_id;
  IF invoice.monto_pagado IS DISTINCT FROM recorded
     OR recorded < 0 OR recorded > invoice.total THEN
    RAISE EXCEPTION 'El monto pagado debe coincidir con el historial. ActualizÃ¡ el programa y registrÃ¡ el pago completo.'
      USING ERRCODE = '23514';
  END IF;
  IF invoice.tipo_comprobante <> 'NOTA_CREDITO' AND invoice.estado_pago <> 'ANULADA' THEN
    IF (invoice.estado_pago = 'PAGADO' AND recorded <> invoice.total)
       OR (invoice.estado_pago = 'PAGO_PARCIAL' AND NOT (recorded > 0 AND recorded < invoice.total))
       OR (invoice.estado_pago = 'PENDIENTE' AND recorded <> 0) THEN
      RAISE EXCEPTION 'El estado de pago no coincide con los pagos registrados.'
        USING ERRCODE = '23514';
    END IF;
  END IF;
  RETURN NULL;
END
$$;


ALTER FUNCTION public.check_invoice_payment_consistency() OWNER TO postgres;

--
-- Name: check_obligation_payment_consistency(); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.check_obligation_payment_consistency() RETURNS trigger
    LANGUAGE plpgsql
    SET search_path TO 'public', 'pg_temp'
    AS $$
DECLARE obligation_id BIGINT; obligation obligaciones_pago%ROWTYPE; paid NUMERIC;
BEGIN
  obligation_id:=NEW.id_obligacion;
  SELECT * INTO obligation FROM obligaciones_pago WHERE id_obligacion=obligation_id FOR UPDATE;
  IF NOT FOUND THEN RETURN NULL; END IF;
  SELECT COALESCE(sum(importe),0) INTO paid FROM obligaciones_pagos WHERE id_obligacion=obligation_id;
  IF (paid>0 AND (obligation.importe_por_definir OR obligation.importe IS NULL))
      OR paid>COALESCE(obligation.importe,0)
      OR (obligation.estado_pago IN ('PENDIENTE','ANULADA') AND paid<>0)
      OR (obligation.estado_pago='PAGO_PARCIAL' AND NOT (paid>0 AND paid<obligation.importe))
      OR (obligation.estado_pago='PAGADO' AND NOT (paid>0 AND paid=obligation.importe))
      OR (obligation.estado_pago='CANCELADA' AND NOT (paid>0 AND paid+obligation.monto_cancelado=obligation.importe)) THEN
    RAISE EXCEPTION 'El estado y el saldo de la obligaciÃ³n deben coincidir con los pagos y la cancelaciÃ³n registrados.' USING ERRCODE='23514';
  END IF;
  RETURN NULL;
END
$$;


ALTER FUNCTION public.check_obligation_payment_consistency() OWNER TO postgres;

--
-- Name: check_payroll_distribution(); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.check_payroll_distribution() RETURNS trigger
    LANGUAGE plpgsql
    SET search_path TO 'public', 'pg_temp'
    AS $$
DECLARE settlement_id UUID; settlement liquidaciones_sueldo%ROWTYPE; total_amount NUMERIC; total_percentage NUMERIC;
BEGIN
  IF TG_TABLE_NAME='liquidacion_sueldo_distribuciones' THEN
    IF TG_OP='UPDATE' THEN
      IF OLD.id_liquidacion IS DISTINCT FROM NEW.id_liquidacion THEN
        RAISE EXCEPTION 'Una distribuciÃ³n no se puede trasladar a otra liquidaciÃ³n.' USING ERRCODE='23514';
      END IF;
    END IF;
  END IF;
  IF TG_TABLE_NAME='liquidaciones_sueldo' THEN settlement_id=NEW.id;
  ELSIF TG_OP='DELETE' THEN settlement_id=OLD.id_liquidacion;
  ELSE settlement_id=NEW.id_liquidacion; END IF;
  SELECT * INTO settlement FROM liquidaciones_sueldo WHERE id=settlement_id FOR UPDATE;
  IF NOT FOUND THEN RETURN NULL; END IF;
  SELECT COALESCE(sum(importe),0),COALESCE(sum(porcentaje),0) INTO total_amount,total_percentage FROM liquidacion_sueldo_distribuciones WHERE id_liquidacion=settlement_id;
  IF total_amount<>settlement.neto OR (COALESCE(settlement.distribucion_modo,'PORCENTAJE')='PORCENTAJE' AND total_percentage<>100) THEN
    RAISE EXCEPTION 'La distribuciÃ³n debe completar el 100%% y coincidir con el neto.' USING ERRCODE='23514';
  END IF;
  RETURN NULL;
END $$;


ALTER FUNCTION public.check_payroll_distribution() OWNER TO postgres;

--
-- Name: guard_invoice_identity(); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.guard_invoice_identity() RETURNS trigger
    LANGUAGE plpgsql
    SET search_path TO 'public', 'pg_temp'
    AS $$
DECLARE party TEXT; document_number TEXT;
BEGIN
  IF TG_OP='UPDATE' AND ROW(NEW.tipo_operacion,NEW.tipo_comprobante,NEW.cuit,NEW.denominacion,NEW.numero_factura,NEW.estado_pago)
    IS NOT DISTINCT FROM ROW(OLD.tipo_operacion,OLD.tipo_comprobante,OLD.cuit,OLD.denominacion,OLD.numero_factura,OLD.estado_pago) THEN RETURN NEW; END IF;
  party=CASE WHEN NULLIF(NEW.cuit,'') IS NOT NULL THEN regexp_replace(NEW.cuit,'[^0-9]','','g') ELSE lower(btrim(NEW.denominacion)) END;
  document_number=upper(regexp_replace(NEW.numero_factura,'\s','','g'));
  PERFORM pg_advisory_xact_lock(hashtext(concat_ws(':','invoice',NEW.tipo_operacion,NEW.tipo_comprobante,party,document_number)));
  IF NEW.estado_pago<>'ANULADA' AND EXISTS(SELECT 1 FROM facturas f WHERE f.id_factura IS DISTINCT FROM NEW.id_factura AND f.estado_pago<>'ANULADA'
    AND f.tipo_operacion=NEW.tipo_operacion AND f.tipo_comprobante=NEW.tipo_comprobante
    AND (CASE WHEN NULLIF(f.cuit,'') IS NOT NULL THEN regexp_replace(f.cuit,'[^0-9]','','g') ELSE lower(btrim(f.denominacion)) END)=party
    AND upper(regexp_replace(f.numero_factura,'\s','','g'))=document_number) THEN
    RAISE EXCEPTION 'Ya existe un comprobante con ese nÃºmero, tipo y emisor.' USING ERRCODE='23505';
  END IF;
  RETURN NEW;
END $$;


ALTER FUNCTION public.guard_invoice_identity() OWNER TO postgres;

--
-- Name: guard_invoice_payment(); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.guard_invoice_payment() RETURNS trigger
    LANGUAGE plpgsql
    SET search_path TO 'public', 'pg_temp'
    AS $$
DECLARE invoice facturas%ROWTYPE;
BEGIN
  IF TG_OP<>'INSERT' THEN RAISE EXCEPTION 'El historial de pagos es inmutable.' USING ERRCODE='23514'; END IF;
  SELECT * INTO invoice FROM facturas WHERE id_factura=NEW.id_factura FOR UPDATE;
  IF invoice.tipo_comprobante='NOTA_CREDITO' OR invoice.estado_pago='ANULADA' THEN
    RAISE EXCEPTION 'Este comprobante no admite pagos.' USING ERRCODE='23514';
  END IF;
  IF NEW.importe + COALESCE((SELECT sum(importe) FROM factura_pagos WHERE id_factura=NEW.id_factura),0)>invoice.total THEN
    RAISE EXCEPTION 'El pago supera el total de la factura.' USING ERRCODE='23514';
  END IF;
  RETURN NEW;
END $$;


ALTER FUNCTION public.guard_invoice_payment() OWNER TO postgres;

--
-- Name: guard_invoice_payment_history(); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.guard_invoice_payment_history() RETURNS trigger
    LANGUAGE plpgsql
    SET search_path TO 'public', 'pg_temp'
    AS $$
DECLARE has_payments BOOLEAN;
BEGIN
  SELECT COALESCE(OLD.monto_pagado,0)>0 OR EXISTS(SELECT 1 FROM factura_pagos WHERE id_factura=OLD.id_factura) INTO has_payments;
  IF has_payments THEN
    IF TG_OP='DELETE' THEN RAISE EXCEPTION 'No se puede eliminar una factura con historial de pagos.' USING ERRCODE='23514'; END IF;
    IF NEW.estado_pago='ANULADA' AND OLD.estado_pago IS DISTINCT FROM 'ANULADA' THEN
      RAISE EXCEPTION 'No se puede anular una factura con pagos.' USING ERRCODE='23514';
    END IF;
    IF ROW(NEW.tipo_operacion,NEW.tipo_comprobante,NEW.numero_factura,NEW.denominacion,NEW.cuit,NEW.fecha,NEW.mes_imputacion,NEW.neto_gravado,NEW.importe_exento,NEW.iva,NEW.otros_impuestos,NEW.total,NEW.clasificacion_gerencial)
       IS DISTINCT FROM ROW(OLD.tipo_operacion,OLD.tipo_comprobante,OLD.numero_factura,OLD.denominacion,OLD.cuit,OLD.fecha,OLD.mes_imputacion,OLD.neto_gravado,OLD.importe_exento,OLD.iva,OLD.otros_impuestos,OLD.total,OLD.clasificacion_gerencial) THEN
      RAISE EXCEPTION 'Los datos contables de una factura con pagos no se pueden modificar.' USING ERRCODE='23514';
    END IF;
  END IF;
  IF TG_OP='DELETE' THEN RETURN OLD; END IF;
  RETURN NEW;
END $$;


ALTER FUNCTION public.guard_invoice_payment_history() OWNER TO postgres;

--
-- Name: guard_obligation_payment(); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.guard_obligation_payment() RETURNS trigger
    LANGUAGE plpgsql
    SET search_path TO 'public', 'pg_temp'
    AS $$
DECLARE obligation obligaciones_pago%ROWTYPE; paid NUMERIC;
BEGIN
  IF TG_OP<>'INSERT' THEN
    RAISE EXCEPTION 'Los pagos registrados se conservan; no se pueden modificar ni eliminar.' USING ERRCODE='23514';
  END IF;
  SELECT * INTO obligation FROM obligaciones_pago WHERE id_obligacion=NEW.id_obligacion FOR UPDATE;
  IF NOT FOUND OR obligation.estado_pago NOT IN ('PENDIENTE','PAGO_PARCIAL')
      OR obligation.archivada_at IS NOT NULL OR obligation.importe_por_definir OR obligation.importe IS NULL THEN
    RAISE EXCEPTION 'Solo se pueden pagar obligaciones abiertas con importe definido.' USING ERRCODE='23514';
  END IF;
  SELECT COALESCE(sum(importe),0) INTO paid FROM obligaciones_pagos WHERE id_obligacion=NEW.id_obligacion;
  IF NEW.importe<=0 OR paid+NEW.importe>obligation.importe THEN
    RAISE EXCEPTION 'El pago debe ser positivo y no superar el saldo pendiente.' USING ERRCODE='23514';
  END IF;
  RETURN NEW;
END
$$;


ALTER FUNCTION public.guard_obligation_payment() OWNER TO postgres;

--
-- Name: guard_obligation_payment_history(); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.guard_obligation_payment_history() RETURNS trigger
    LANGUAGE plpgsql
    SET search_path TO 'public', 'pg_temp'
    AS $$
DECLARE
  paid NUMERIC;
  mutable_columns TEXT[] := ARRAY['estado_pago','fecha_pago','updated_at'];
BEGIN
  IF TG_OP='INSERT' THEN
    IF NEW.estado_pago='CANCELADA' THEN
      RAISE EXCEPTION 'Una obligaciÃ³n se cancela Ãºnicamente despuÃ©s de registrar sus pagos.' USING ERRCODE='23514';
    END IF;
    RETURN NEW;
  END IF;
  PERFORM 1 FROM obligaciones_pago WHERE id_obligacion=OLD.id_obligacion FOR UPDATE;
  SELECT COALESCE(sum(importe),0) INTO paid FROM obligaciones_pagos WHERE id_obligacion=OLD.id_obligacion;
  IF paid>0 THEN
    IF TG_OP='DELETE' THEN
      RAISE EXCEPTION 'No se puede eliminar una obligaciÃ³n con pagos.' USING ERRCODE='23514';
    END IF;
    IF OLD.estado_pago='CANCELADA' AND (to_jsonb(NEW)-'updated_at') IS DISTINCT FROM (to_jsonb(OLD)-'updated_at') THEN
      RAISE EXCEPTION 'La obligaciÃ³n con saldo cancelado conserva su historial y no puede reabrirse.' USING ERRCODE='23514';
    END IF;
    IF NEW.estado_pago='ANULADA' THEN
      RAISE EXCEPTION 'No se puede anular una obligaciÃ³n con pagos. UsÃ¡ cancelar saldo para conservar lo abonado.' USING ERRCODE='23514';
    END IF;
    IF NEW.estado_pago='CANCELADA' AND OLD.estado_pago<>'CANCELADA' THEN
      IF OLD.estado_pago<>'PAGO_PARCIAL' OR paid>=OLD.importe
         OR NEW.monto_cancelado IS DISTINCT FROM OLD.importe-paid THEN
        RAISE EXCEPTION 'Solo se puede cancelar exactamente el saldo de una obligaciÃ³n parcialmente pagada.' USING ERRCODE='23514';
      END IF;
      mutable_columns := mutable_columns || ARRAY['monto_cancelado','motivo_cancelacion','cancelada_at','cancelada_por'];
    END IF;
    IF NEW.id_tipo_obligacion IS DISTINCT FROM OLD.id_tipo_obligacion
       OR NEW.tipo_obligacion IS DISTINCT FROM OLD.tipo_obligacion THEN
      -- Check both references before the later synchronization trigger runs.
      -- A legacy-code-only update must not bypass the group restriction.
      IF NOT EXISTS (
        SELECT 1 FROM tipos_obligacion old_type
        JOIN tipos_obligacion new_type ON new_type.grupo_control=old_type.grupo_control
        WHERE old_type.id=OLD.id_tipo_obligacion
          AND new_type.id=NEW.id_tipo_obligacion AND new_type.codigo=NEW.tipo_obligacion
      ) THEN
        RAISE EXCEPTION 'La recategorizaciÃ³n con pagos debe conservar el grupo de control e indicar un tipo consistente.' USING ERRCODE='23514';
      END IF;
      mutable_columns := mutable_columns || ARRAY['id_tipo_obligacion','tipo_obligacion'];
    END IF;
    IF (to_jsonb(NEW)-mutable_columns) IS DISTINCT FROM (to_jsonb(OLD)-mutable_columns) THEN
      RAISE EXCEPTION 'Los datos originales de una obligaciÃ³n con pagos no pueden modificarse.' USING ERRCODE='23514';
    END IF;
  END IF;
  IF TG_OP='DELETE' THEN RETURN OLD; END IF;
  IF NEW.estado_pago='ANULADA' AND OLD.estado_pago IS DISTINCT FROM 'ANULADA'
      AND NULLIF(btrim(NEW.motivo_anulacion),'') IS NULL THEN
    RAISE EXCEPTION 'El motivo de anulaciÃ³n es obligatorio.' USING ERRCODE='23514';
  END IF;
  RETURN NEW;
END
$$;


ALTER FUNCTION public.guard_obligation_payment_history() OWNER TO postgres;

--
-- Name: guard_payroll_payment(); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.guard_payroll_payment() RETURNS trigger
    LANGUAGE plpgsql
    SET search_path TO 'public', 'pg_temp'
    AS $$
DECLARE settlement liquidaciones_sueldo%ROWTYPE; paid NUMERIC; distributed NUMERIC; percentage NUMERIC;
BEGIN
  SELECT * INTO settlement FROM liquidaciones_sueldo WHERE id=NEW.id_liquidacion FOR UPDATE;
  IF settlement.estado NOT IN ('CONFIRMADA','PAGO_PARCIAL') THEN RAISE EXCEPTION 'La liquidaciÃ³n no admite pagos en su estado actual.' USING ERRCODE='23514'; END IF;
  SELECT COALESCE(sum(importe),0),COALESCE(sum(porcentaje),0) INTO distributed,percentage FROM liquidacion_sueldo_distribuciones WHERE id_liquidacion=NEW.id_liquidacion;
  IF distributed<>settlement.neto OR (COALESCE(settlement.distribucion_modo,'PORCENTAJE')='PORCENTAJE' AND percentage<>100) THEN
    RAISE EXCEPTION 'RevisÃ¡ la distribuciÃ³n antes de registrar el pago.' USING ERRCODE='23514';
  END IF;
  SELECT COALESCE(sum(importe),0) INTO paid FROM sueldos_pagos WHERE id_liquidacion=NEW.id_liquidacion AND estado='ACTIVO';
  IF NEW.importe<=0 OR paid+NEW.importe>settlement.neto THEN RAISE EXCEPTION 'El pago supera el saldo pendiente.' USING ERRCODE='23514'; END IF;
  RETURN NEW;
END $$;


ALTER FUNCTION public.guard_payroll_payment() OWNER TO postgres;

--
-- Name: normalize_operational_identity(text); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.normalize_operational_identity(value text) RETURNS text
    LANGUAGE sql IMMUTABLE
    AS $$
  SELECT lower(regexp_replace(trim(unaccent(value)), '\\s+', ' ', 'g'))
$$;


ALTER FUNCTION public.normalize_operational_identity(value text) OWNER TO postgres;

--
-- Name: set_movimientos_timestamps(); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.set_movimientos_timestamps() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
begin
  if tg_op = 'INSERT' then
    new.created_at := coalesce(new.created_at, now());
    new.updated_at := coalesce(new.updated_at, new.created_at, now());
  elsif tg_op = 'UPDATE' then
    new.created_at := old.created_at;
    new.updated_at := now();
  end if;

  return new;
end;
$$;


ALTER FUNCTION public.set_movimientos_timestamps() OWNER TO postgres;

--
-- Name: set_ordenes_compra_updated_at(); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.set_ordenes_compra_updated_at() RETURNS trigger
    LANGUAGE plpgsql
    SET search_path TO 'public'
    AS $$
begin
  new.updated_at = now();
  return new;
end;
$$;


ALTER FUNCTION public.set_ordenes_compra_updated_at() OWNER TO postgres;

--
-- Name: sync_obligation_type_reference(); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.sync_obligation_type_reference() RETURNS trigger
    LANGUAGE plpgsql
    SET search_path TO 'public', 'pg_temp'
    AS $$
DECLARE type_id INTEGER; type_code TEXT;
BEGIN
  IF TG_OP = 'UPDATE' THEN
    IF NEW.id_tipo_obligacion IS NOT DISTINCT FROM OLD.id_tipo_obligacion
       AND NEW.tipo_obligacion IS NOT DISTINCT FROM OLD.tipo_obligacion THEN RETURN NEW; END IF;
    IF NEW.tipo_obligacion IS DISTINCT FROM OLD.tipo_obligacion
       AND NEW.id_tipo_obligacion IS NOT DISTINCT FROM OLD.id_tipo_obligacion THEN
      NEW.id_tipo_obligacion := NULL;
    ELSIF NEW.id_tipo_obligacion IS DISTINCT FROM OLD.id_tipo_obligacion
       AND NEW.tipo_obligacion IS NOT DISTINCT FROM OLD.tipo_obligacion THEN
      NEW.tipo_obligacion := NULL;
    END IF;
  END IF;
  IF NEW.id_tipo_obligacion IS NOT NULL THEN
    SELECT id, codigo INTO type_id, type_code FROM tipos_obligacion WHERE id = NEW.id_tipo_obligacion FOR SHARE;
  ELSE
    SELECT id, codigo INTO type_id, type_code FROM tipos_obligacion WHERE codigo = NEW.tipo_obligacion FOR SHARE;
  END IF;
  IF type_id IS NULL OR (NEW.tipo_obligacion IS NOT NULL AND NEW.tipo_obligacion <> type_code) THEN
    RAISE EXCEPTION 'El tipo de obligaciÃ³n no existe o la referencia no coincide.' USING ERRCODE = '23503';
  END IF;
  NEW.id_tipo_obligacion := type_id;
  NEW.tipo_obligacion := type_code;
  RETURN NEW;
END $$;


ALTER FUNCTION public.sync_obligation_type_reference() OWNER TO postgres;

--
-- Name: sync_visibilidad_modulos_columns(); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.sync_visibilidad_modulos_columns() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
  IF NEW.modulo_key IS NULL OR BTRIM(NEW.modulo_key) = '' THEN
    NEW.modulo_key := NULLIF(BTRIM(NEW.modulo), '');
  ELSIF NEW.modulo IS NULL OR BTRIM(NEW.modulo) = '' THEN
    NEW.modulo := NEW.modulo_key;
  ELSIF BTRIM(NEW.modulo) <> BTRIM(NEW.modulo_key) THEN
    RAISE EXCEPTION 'modulo y modulo_key no pueden tener valores contradictorios';
  END IF;
  RETURN NEW;
END;
$$;


ALTER FUNCTION public.sync_visibilidad_modulos_columns() OWNER TO postgres;

SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: _migracion_origen_destino_map; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public._migracion_origen_destino_map (
    nombre_texto text NOT NULL,
    tipo text NOT NULL,
    id_ref integer NOT NULL
);


ALTER TABLE public._migracion_origen_destino_map OWNER TO postgres;

--
-- Name: assistant_configs; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.assistant_configs (
    organization_id uuid NOT NULL,
    assistant_name text DEFAULT 'Asistente'::text NOT NULL,
    system_prompt text NOT NULL,
    tone text DEFAULT 'claro y pragmatico'::text NOT NULL,
    goals jsonb DEFAULT '[]'::jsonb NOT NULL,
    constraints jsonb DEFAULT '[]'::jsonb NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


ALTER TABLE public.assistant_configs OWNER TO postgres;

--
-- Name: audit_logs; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.audit_logs (
    id uuid NOT NULL,
    organization_id uuid NOT NULL,
    actor_user_id uuid,
    action text NOT NULL,
    entity_type text NOT NULL,
    entity_id uuid,
    metadata jsonb DEFAULT '{}'::jsonb NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL
);


ALTER TABLE public.audit_logs OWNER TO postgres;

--
-- Name: authorization_policy_state; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.authorization_policy_state (
    id boolean DEFAULT true NOT NULL,
    revision integer DEFAULT 1 NOT NULL,
    policy_version text DEFAULT '2026-08-21.1'::text NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT authorization_policy_state_id_check CHECK (id)
);


ALTER TABLE public.authorization_policy_state OWNER TO postgres;

--
-- Name: backup_info_20260325; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.backup_info_20260325 (
    id integer NOT NULL,
    created_at timestamp with time zone DEFAULT now(),
    motivo text,
    filas_movimientos integer,
    filas_movimientomaterial integer,
    filas_recortes integer
);


ALTER TABLE public.backup_info_20260325 OWNER TO postgres;

--
-- Name: backup_info_20260325_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.backup_info_20260325_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.backup_info_20260325_id_seq OWNER TO postgres;

--
-- Name: backup_info_20260325_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.backup_info_20260325_id_seq OWNED BY public.backup_info_20260325.id;


--
-- Name: backup_movimientomaterial_20260325; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.backup_movimientomaterial_20260325 (
    id_movimientomaterial integer,
    cantidad integer,
    id_movimiento integer,
    id_material integer,
    stock_actual numeric(18,2),
    esrecorte boolean,
    id_recorte integer,
    medida_recorte numeric(10,2),
    material_nombre_eventual text,
    es_compra_eventual boolean
);


ALTER TABLE public.backup_movimientomaterial_20260325 OWNER TO postgres;

--
-- Name: backup_movimientos_20260325; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.backup_movimientos_20260325 (
    id_movimiento integer,
    fecha date,
    nroremito bigint,
    id_origen integer,
    tipo_origen character varying(50),
    id_destino integer,
    tipo_destino character varying(50),
    id_tipomovimiento integer,
    id_obra integer,
    costoflete numeric(10,2),
    observaciones text
);


ALTER TABLE public.backup_movimientos_20260325 OWNER TO postgres;

--
-- Name: backup_recortes_20260325; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.backup_recortes_20260325 (
    id integer,
    id_material integer,
    medida numeric(10,2),
    cantidad numeric(10,2),
    fecha_creacion timestamp without time zone
);


ALTER TABLE public.backup_recortes_20260325 OWNER TO postgres;

--
-- Name: bitacora; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.bitacora (
    id integer NOT NULL,
    fecha timestamp with time zone DEFAULT now() NOT NULL,
    usuario_email text DEFAULT 'sistema'::text NOT NULL,
    usuario_rol text DEFAULT 'sistema'::text NOT NULL,
    accion text NOT NULL,
    entidad_tipo text NOT NULL,
    entidad_id integer NOT NULL,
    entidad_nombre text,
    detalles jsonb DEFAULT '{}'::jsonb NOT NULL
);


ALTER TABLE public.bitacora OWNER TO postgres;

--
-- Name: bitacora_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.bitacora_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.bitacora_id_seq OWNER TO postgres;

--
-- Name: bitacora_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.bitacora_id_seq OWNED BY public.bitacora.id;


--
-- Name: business_memories; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.business_memories (
    id uuid NOT NULL,
    organization_id uuid NOT NULL,
    kind text DEFAULT 'fact'::text NOT NULL,
    key text NOT NULL,
    value jsonb NOT NULL,
    relevance_hint text,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


ALTER TABLE public.business_memories OWNER TO postgres;

--
-- Name: canaletas_plegados; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.canaletas_plegados (
    id integer NOT NULL,
    id_obra integer NOT NULL,
    descripcion text NOT NULL,
    precio numeric(20,2) DEFAULT 0.00 NOT NULL,
    fecha_creacion timestamp without time zone DEFAULT now() NOT NULL,
    pagado boolean DEFAULT false NOT NULL,
    CONSTRAINT canaletas_plegados_descripcion_not_blank CHECK ((btrim(descripcion) <> ''::text)),
    CONSTRAINT canaletas_plegados_precio_nonnegative CHECK ((precio >= (0)::numeric))
);


ALTER TABLE public.canaletas_plegados OWNER TO postgres;

--
-- Name: canaletas_plegados_fotos; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.canaletas_plegados_fotos (
    id integer NOT NULL,
    canaleta_plegado_id integer NOT NULL,
    bucket_id text DEFAULT 'canaletas-fotos'::text NOT NULL,
    object_path text NOT NULL,
    nombre_archivo text NOT NULL,
    content_type text NOT NULL,
    tamanio_bytes bigint DEFAULT 0 NOT NULL,
    orden integer DEFAULT 1 NOT NULL,
    fecha_creacion timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT canaletas_plegados_fotos_bucket_chk CHECK ((bucket_id = 'canaletas-fotos'::text)),
    CONSTRAINT canaletas_plegados_fotos_content_type_chk CHECK ((content_type = ANY (ARRAY['image/jpeg'::text, 'image/png'::text, 'image/webp'::text]))),
    CONSTRAINT canaletas_plegados_fotos_tamanio_chk CHECK (((tamanio_bytes >= 0) AND (tamanio_bytes <= 8388608)))
);


ALTER TABLE public.canaletas_plegados_fotos OWNER TO postgres;

--
-- Name: canaletas_plegados_fotos_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

ALTER TABLE public.canaletas_plegados_fotos ALTER COLUMN id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.canaletas_plegados_fotos_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: canaletas_plegados_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.canaletas_plegados_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.canaletas_plegados_id_seq OWNER TO postgres;

--
-- Name: canaletas_plegados_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.canaletas_plegados_id_seq OWNED BY public.canaletas_plegados.id;


--
-- Name: categorias; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.categorias (
    id_categoria integer NOT NULL,
    nombre character varying(50) NOT NULL,
    recortable boolean DEFAULT false NOT NULL
);


ALTER TABLE public.categorias OWNER TO postgres;

--
-- Name: categorias_id_categoria_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.categorias_id_categoria_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.categorias_id_categoria_seq OWNER TO postgres;

--
-- Name: categorias_id_categoria_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.categorias_id_categoria_seq OWNED BY public.categorias.id_categoria;


--
-- Name: conceptos_sueldo; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.conceptos_sueldo (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    codigo text NOT NULL,
    nombre text NOT NULL,
    clase text NOT NULL,
    activo boolean DEFAULT true NOT NULL,
    es_sistema boolean DEFAULT false NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL,
    modalidad_scope text,
    CONSTRAINT conceptos_sueldo_clase_check CHECK ((clase = ANY (ARRAY['HABER'::text, 'DEDUCCION'::text]))),
    CONSTRAINT conceptos_sueldo_modalidad_scope_check CHECK (((modalidad_scope IS NULL) OR (modalidad_scope = ANY (ARRAY['REGISTRADO'::text, 'NO_REGISTRADO'::text]))))
);


ALTER TABLE public.conceptos_sueldo OWNER TO postgres;

--
-- Name: conteos_inventario; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.conteos_inventario (
    id_conteo integer NOT NULL,
    fecha_inicio timestamp without time zone DEFAULT now() NOT NULL,
    fecha_fin timestamp without time zone,
    usuario_inicio text,
    usuario_fin text,
    estado text DEFAULT 'abierto'::text NOT NULL,
    observaciones text,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT conteos_inventario_estado_check CHECK ((estado = ANY (ARRAY['abierto'::text, 'finalizado'::text, 'cancelado'::text]))),
    CONSTRAINT conteos_inventario_fecha_fin_check CHECK (((fecha_fin IS NULL) OR (fecha_fin >= fecha_inicio)))
);


ALTER TABLE public.conteos_inventario OWNER TO postgres;

--
-- Name: TABLE conteos_inventario; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON TABLE public.conteos_inventario IS 'Cabecera de sesiones de conteo fisico de inventario.';


--
-- Name: COLUMN conteos_inventario.estado; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON COLUMN public.conteos_inventario.estado IS 'Estados validos: abierto, finalizado, cancelado.';


--
-- Name: conteos_inventario_detalle; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.conteos_inventario_detalle (
    id_detalle integer NOT NULL,
    id_conteo integer NOT NULL,
    id_material integer NOT NULL,
    material_nombre_snapshot text,
    material_categoria_snapshot text,
    cantidad_contada numeric(18,2) NOT NULL,
    stock_sistema numeric(18,2),
    diferencia numeric(18,2),
    fecha_carga timestamp without time zone DEFAULT now() NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT conteos_inventario_detalle_cantidad_check CHECK ((cantidad_contada >= (0)::numeric))
);


ALTER TABLE public.conteos_inventario_detalle OWNER TO postgres;

--
-- Name: TABLE conteos_inventario_detalle; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON TABLE public.conteos_inventario_detalle IS 'Detalle de materiales contados fisicamente y snapshot de comparacion contra stock del sistema al finalizar.';


--
-- Name: COLUMN conteos_inventario_detalle.stock_sistema; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON COLUMN public.conteos_inventario_detalle.stock_sistema IS 'Snapshot del stock registrado al momento de finalizar el conteo.';


--
-- Name: COLUMN conteos_inventario_detalle.diferencia; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON COLUMN public.conteos_inventario_detalle.diferencia IS 'cantidad_contada - stock_sistema. Positivo=sobrante, negativo=faltante.';


--
-- Name: conteos_inventario_detalle_id_detalle_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.conteos_inventario_detalle_id_detalle_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.conteos_inventario_detalle_id_detalle_seq OWNER TO postgres;

--
-- Name: conteos_inventario_detalle_id_detalle_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.conteos_inventario_detalle_id_detalle_seq OWNED BY public.conteos_inventario_detalle.id_detalle;


--
-- Name: conteos_inventario_id_conteo_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.conteos_inventario_id_conteo_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.conteos_inventario_id_conteo_seq OWNER TO postgres;

--
-- Name: conteos_inventario_id_conteo_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.conteos_inventario_id_conteo_seq OWNED BY public.conteos_inventario.id_conteo;


--
-- Name: conversation_summaries; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.conversation_summaries (
    conversation_id uuid NOT NULL,
    organization_id uuid NOT NULL,
    summary text NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


ALTER TABLE public.conversation_summaries OWNER TO postgres;

--
-- Name: conversations; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.conversations (
    id uuid NOT NULL,
    organization_id uuid NOT NULL,
    created_by_user_id uuid NOT NULL,
    title text DEFAULT 'Nueva conversacion'::text NOT NULL,
    status text DEFAULT 'active'::text NOT NULL,
    last_message_at timestamp with time zone DEFAULT now() NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT conversations_status_check CHECK ((status = ANY (ARRAY['active'::text, 'archived'::text])))
);


ALTER TABLE public.conversations OWNER TO postgres;

--
-- Name: document_chunks; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.document_chunks (
    id uuid NOT NULL,
    document_id uuid NOT NULL,
    organization_id uuid NOT NULL,
    chunk_index integer NOT NULL,
    content text NOT NULL,
    token_count integer NOT NULL,
    metadata jsonb DEFAULT '{}'::jsonb NOT NULL,
    embedding text,
    created_at timestamp with time zone DEFAULT now() NOT NULL
);


ALTER TABLE public.document_chunks OWNER TO postgres;

--
-- Name: documents; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.documents (
    id uuid NOT NULL,
    organization_id uuid NOT NULL,
    uploaded_by_user_id uuid NOT NULL,
    title text NOT NULL,
    source_type text DEFAULT 'manual'::text NOT NULL,
    mime_type text NOT NULL,
    storage_path text,
    status text DEFAULT 'ready'::text NOT NULL,
    metadata jsonb DEFAULT '{}'::jsonb NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT documents_status_check CHECK ((status = ANY (ARRAY['pending'::text, 'processing'::text, 'ready'::text, 'failed'::text])))
);


ALTER TABLE public.documents OWNER TO postgres;

--
-- Name: factura_pagos; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.factura_pagos (
    id_pago bigint NOT NULL,
    id_factura integer NOT NULL,
    fecha_pago date NOT NULL,
    periodo_caja date NOT NULL,
    importe numeric NOT NULL,
    cuenta_origen text,
    medio_pago text,
    responsable_pago text,
    referencia text,
    observaciones text,
    usuario_email text,
    usuario_rol text,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT factura_pagos_importe_check CHECK ((importe > (0)::numeric))
);


ALTER TABLE public.factura_pagos OWNER TO postgres;

--
-- Name: factura_pagos_id_pago_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.factura_pagos_id_pago_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.factura_pagos_id_pago_seq OWNER TO postgres;

--
-- Name: factura_pagos_id_pago_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.factura_pagos_id_pago_seq OWNED BY public.factura_pagos.id_pago;


--
-- Name: facturas; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.facturas (
    id_factura integer NOT NULL,
    tipo_operacion text NOT NULL,
    numero_factura text NOT NULL,
    fecha date NOT NULL,
    mes_imputacion date NOT NULL,
    tipo_comprobante text NOT NULL,
    denominacion text NOT NULL,
    cuit text,
    neto_gravado numeric(14,2) DEFAULT 0 NOT NULL,
    iva numeric(14,2) DEFAULT 0 NOT NULL,
    otros_impuestos numeric(14,2) DEFAULT 0 NOT NULL,
    total numeric(14,2) DEFAULT 0 NOT NULL,
    observaciones text,
    usuario_email text,
    usuario_rol text,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL,
    fecha_vencimiento date NOT NULL,
    monto_pagado numeric(14,2) DEFAULT 0 NOT NULL,
    estado_pago text DEFAULT 'PENDIENTE'::text NOT NULL,
    fecha_pago date,
    motivo_anulacion text,
    category_id integer,
    importe_exento numeric DEFAULT 0 NOT NULL,
    clasificacion_gerencial text DEFAULT 'SIN_CLASIFICAR'::text,
    CONSTRAINT facturas_estado_pago_check CHECK ((estado_pago = ANY (ARRAY['PENDIENTE'::text, 'PAGO_PARCIAL'::text, 'PAGADO'::text, 'ANULADA'::text]))),
    CONSTRAINT facturas_tipo_comprobante_check CHECK ((tipo_comprobante = ANY (ARRAY['FACTURA_A'::text, 'FACTURA_B'::text, 'FACTURA_C'::text, 'NOTA_CREDITO'::text, 'NOTA_DEBITO'::text, 'NOTA_VENTA'::text]))),
    CONSTRAINT facturas_tipo_operacion_check CHECK ((tipo_operacion = ANY (ARRAY['COMPRA'::text, 'VENTA'::text])))
);


ALTER TABLE public.facturas OWNER TO postgres;

--
-- Name: facturas_id_factura_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.facturas_id_factura_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.facturas_id_factura_seq OWNER TO postgres;

--
-- Name: facturas_id_factura_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.facturas_id_factura_seq OWNED BY public.facturas.id_factura;


--
-- Name: financial_operation_requests; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.financial_operation_requests (
    actor text NOT NULL,
    operation text NOT NULL,
    request_key text NOT NULL,
    fingerprint text NOT NULL,
    response jsonb NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL
);


ALTER TABLE public.financial_operation_requests OWNER TO postgres;

--
-- Name: fiscal_periods; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.fiscal_periods (
    period_start date NOT NULL,
    status text NOT NULL,
    closed_at timestamp with time zone,
    closed_by text,
    updated_at timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT fiscal_periods_month_aligned CHECK ((period_start = (date_trunc('month'::text, (period_start)::timestamp with time zone))::date)),
    CONSTRAINT fiscal_periods_status_check CHECK ((status = ANY (ARRAY['OPEN'::text, 'CLOSED'::text])))
);


ALTER TABLE public.fiscal_periods OWNER TO postgres;

--
-- Name: fiscal_thresholds; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.fiscal_thresholds (
    singleton boolean DEFAULT true NOT NULL,
    warning_percent numeric NOT NULL,
    exceeded_percent numeric NOT NULL,
    updated_by text NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT fiscal_thresholds_exceeded_percent_check CHECK ((exceeded_percent >= (0)::numeric)),
    CONSTRAINT fiscal_thresholds_ordered CHECK ((warning_percent < exceeded_percent)),
    CONSTRAINT fiscal_thresholds_singleton_check CHECK (singleton),
    CONSTRAINT fiscal_thresholds_warning_percent_check CHECK ((warning_percent >= (0)::numeric))
);


ALTER TABLE public.fiscal_thresholds OWNER TO postgres;

--
-- Name: fletes; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.fletes (
    id_flete integer NOT NULL,
    nombre character varying(50),
    activo boolean DEFAULT true
);


ALTER TABLE public.fletes OWNER TO postgres;

--
-- Name: fletes_id_flete_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.fletes_id_flete_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.fletes_id_flete_seq OWNER TO postgres;

--
-- Name: fletes_id_flete_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.fletes_id_flete_seq OWNED BY public.fletes.id_flete;


--
-- Name: herramienta_movimientos; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.herramienta_movimientos (
    id integer NOT NULL,
    id_herramienta integer,
    obra_origen character varying(255),
    obra_destino character varying(255),
    fecha timestamp without time zone DEFAULT now() NOT NULL,
    nombre_herramienta character varying
);


ALTER TABLE public.herramienta_movimientos OWNER TO postgres;

--
-- Name: herramienta_movimientos_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.herramienta_movimientos_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.herramienta_movimientos_id_seq OWNER TO postgres;

--
-- Name: herramienta_movimientos_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.herramienta_movimientos_id_seq OWNED BY public.herramienta_movimientos.id;


--
-- Name: herramientas; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.herramientas (
    id_herramienta integer NOT NULL,
    nombre character varying(255) NOT NULL,
    estado character varying(50) DEFAULT 'Ã“ptima'::character varying NOT NULL,
    id_obra integer,
    observaciones character varying(500),
    id_ubicacion integer
);


ALTER TABLE public.herramientas OWNER TO postgres;

--
-- Name: herramientas_id_herramienta_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.herramientas_id_herramienta_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.herramientas_id_herramienta_seq OWNER TO postgres;

--
-- Name: herramientas_id_herramienta_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.herramientas_id_herramienta_seq OWNED BY public.herramientas.id_herramienta;


--
-- Name: ingestion_jobs; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.ingestion_jobs (
    id uuid NOT NULL,
    organization_id uuid NOT NULL,
    document_id uuid,
    status text NOT NULL,
    error_message text,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT ingestion_jobs_status_check CHECK ((status = ANY (ARRAY['queued'::text, 'processing'::text, 'done'::text, 'failed'::text])))
);


ALTER TABLE public.ingestion_jobs OWNER TO postgres;

--
-- Name: insumo_movimientos; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.insumo_movimientos (
    id integer NOT NULL,
    id_insumo integer,
    obra_origen character varying(255) NOT NULL,
    obra_destino character varying(255) NOT NULL,
    fecha timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    id_movimiento integer,
    nombre_insumo character varying
);


ALTER TABLE public.insumo_movimientos OWNER TO postgres;

--
-- Name: insumo_movimientos_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.insumo_movimientos_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.insumo_movimientos_id_seq OWNER TO postgres;

--
-- Name: insumo_movimientos_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.insumo_movimientos_id_seq OWNED BY public.insumo_movimientos.id;


--
-- Name: insumos; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.insumos (
    id_insumo integer NOT NULL,
    nombre character varying(255) NOT NULL,
    id_obra integer,
    consumido boolean DEFAULT false,
    observaciones text,
    id_ubicacion integer
);


ALTER TABLE public.insumos OWNER TO postgres;

--
-- Name: insumos_id_insumo_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.insumos_id_insumo_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.insumos_id_insumo_seq OWNER TO postgres;

--
-- Name: insumos_id_insumo_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.insumos_id_insumo_seq OWNED BY public.insumos.id_insumo;


--
-- Name: invoice_categories; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.invoice_categories (
    id integer NOT NULL,
    display_name text NOT NULL,
    normalized_name text NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    created_by text
);


ALTER TABLE public.invoice_categories OWNER TO postgres;

--
-- Name: invoice_categories_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

ALTER TABLE public.invoice_categories ALTER COLUMN id ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public.invoice_categories_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: liquidacion_sueldo_conceptos; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.liquidacion_sueldo_conceptos (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    id_liquidacion uuid NOT NULL,
    id_concepto uuid NOT NULL,
    codigo_snapshot text NOT NULL,
    nombre_snapshot text NOT NULL,
    clase_snapshot text NOT NULL,
    importe numeric(14,2) NOT NULL,
    orden integer DEFAULT 0 NOT NULL,
    observacion text,
    CONSTRAINT liquidacion_sueldo_conceptos_clase_snapshot_check CHECK ((clase_snapshot = ANY (ARRAY['HABER'::text, 'DEDUCCION'::text]))),
    CONSTRAINT liquidacion_sueldo_conceptos_importe_check CHECK ((importe > (0)::numeric))
);


ALTER TABLE public.liquidacion_sueldo_conceptos OWNER TO postgres;

--
-- Name: liquidacion_sueldo_distribuciones; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.liquidacion_sueldo_distribuciones (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    id_liquidacion uuid NOT NULL,
    destino text NOT NULL,
    id_obra integer,
    obra_nombre_snapshot text,
    porcentaje numeric(8,4) NOT NULL,
    importe numeric(14,2) NOT NULL,
    orden integer DEFAULT 0 NOT NULL,
    observacion text,
    CONSTRAINT liquidacion_sueldo_distribuciones_check CHECK ((((destino = 'OBRA'::text) AND (id_obra IS NOT NULL)) OR ((destino <> 'OBRA'::text) AND (id_obra IS NULL)))),
    CONSTRAINT liquidacion_sueldo_distribuciones_destino_check CHECK ((destino = ANY (ARRAY['OBRA'::text, 'COSTO_COMUN'::text, 'NO_DISTRIBUIDO'::text]))),
    CONSTRAINT liquidacion_sueldo_distribuciones_importe_check CHECK ((importe >= (0)::numeric)),
    CONSTRAINT liquidacion_sueldo_distribuciones_porcentaje_check CHECK ((porcentaje >= (0)::numeric))
);


ALTER TABLE public.liquidacion_sueldo_distribuciones OWNER TO postgres;

--
-- Name: liquidaciones_sueldo; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.liquidaciones_sueldo (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    id_vinculo uuid NOT NULL,
    id_persona uuid NOT NULL,
    periodo date NOT NULL,
    tipo text NOT NULL,
    fecha_vencimiento date,
    modalidad_snapshot text NOT NULL,
    persona_nombre_snapshot text NOT NULL,
    codigo_vinculo_snapshot text NOT NULL,
    puesto_snapshot text,
    estado text DEFAULT 'BORRADOR'::text NOT NULL,
    total_haberes numeric(14,2) DEFAULT 0 NOT NULL,
    total_deducciones numeric(14,2) DEFAULT 0 NOT NULL,
    neto numeric(14,2) DEFAULT 0 NOT NULL,
    observaciones text,
    version integer DEFAULT 1 NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL,
    created_by text,
    updated_by text,
    id_condicion_vinculo uuid,
    distribucion_modo text,
    idempotency_key text,
    anulada_at timestamp with time zone,
    anulada_por text,
    motivo_anulacion text,
    CONSTRAINT liquidaciones_sueldo_check CHECK ((neto = (total_haberes - total_deducciones))),
    CONSTRAINT liquidaciones_sueldo_distribucion_modo_check CHECK (((distribucion_modo IS NULL) OR (distribucion_modo = ANY (ARRAY['PORCENTAJE'::text, 'IMPORTE'::text])))),
    CONSTRAINT liquidaciones_sueldo_estado_check CHECK ((estado = ANY (ARRAY['BORRADOR'::text, 'CONFIRMADA'::text, 'PAGO_PARCIAL'::text, 'PAGADA'::text, 'ANULADA'::text]))),
    CONSTRAINT liquidaciones_sueldo_modalidad_snapshot_check CHECK ((modalidad_snapshot = ANY (ARRAY['REGISTRADO'::text, 'NO_REGISTRADO'::text]))),
    CONSTRAINT liquidaciones_sueldo_periodo_check CHECK ((periodo = (date_trunc('month'::text, (periodo)::timestamp with time zone))::date)),
    CONSTRAINT liquidaciones_sueldo_tipo_check CHECK ((tipo = ANY (ARRAY['MENSUAL'::text, 'SAC'::text, 'BONO'::text, 'ANTICIPO'::text, 'AJUSTE'::text]))),
    CONSTRAINT liquidaciones_sueldo_total_deducciones_check CHECK ((total_deducciones >= (0)::numeric)),
    CONSTRAINT liquidaciones_sueldo_total_haberes_check CHECK ((total_haberes >= (0)::numeric))
);


ALTER TABLE public.liquidaciones_sueldo OWNER TO postgres;

--
-- Name: materiales; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.materiales (
    id integer NOT NULL,
    id_categoria integer NOT NULL,
    nombre character varying(150) NOT NULL,
    preciounitario numeric(12,2) DEFAULT 0.00,
    stockactual numeric(10,2) DEFAULT 0,
    stockminimo numeric(10,2) DEFAULT 0,
    codigo text,
    id_ubicacion integer,
    oculto boolean DEFAULT false NOT NULL,
    CONSTRAINT materiales_stockactual_check CHECK (stockactual >= 0),
    CONSTRAINT materiales_stockminimo_check CHECK (stockminimo >= 0)
);


ALTER TABLE public.materiales OWNER TO postgres;

--
-- Name: COLUMN materiales.oculto; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON COLUMN public.materiales.oculto IS 'Oculta el material en movimientos, historial y PDF regenerados sin eliminar sus datos.';


--
-- Name: materiales_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.materiales_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.materiales_id_seq OWNER TO postgres;

--
-- Name: materiales_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.materiales_id_seq OWNED BY public.materiales.id;


--
-- Name: materiales_proveedores; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.materiales_proveedores (
    id integer NOT NULL,
    id_material integer NOT NULL,
    id_proveedor integer NOT NULL
);


ALTER TABLE public.materiales_proveedores OWNER TO postgres;

--
-- Name: materiales_proveedores_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.materiales_proveedores_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.materiales_proveedores_id_seq OWNER TO postgres;

--
-- Name: materiales_proveedores_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.materiales_proveedores_id_seq OWNED BY public.materiales_proveedores.id;


--
-- Name: messages; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.messages (
    id uuid NOT NULL,
    conversation_id uuid NOT NULL,
    organization_id uuid NOT NULL,
    role text NOT NULL,
    content text NOT NULL,
    token_input integer,
    token_output integer,
    model text,
    sources jsonb DEFAULT '[]'::jsonb NOT NULL,
    metadata jsonb DEFAULT '{}'::jsonb NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT messages_role_check CHECK ((role = ANY (ARRAY['system'::text, 'user'::text, 'assistant'::text, 'tool'::text])))
);


ALTER TABLE public.messages OWNER TO postgres;

--
-- Name: movimientomaterial; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.movimientomaterial (
    id_movimientomaterial integer NOT NULL,
    cantidad numeric(10,4),
    id_movimiento integer NOT NULL,
    id_material integer,
    stock_actual numeric(18,2),
    esrecorte boolean DEFAULT false,
    id_recorte integer,
    medida_recorte numeric(10,2),
    material_nombre_eventual text,
    es_compra_eventual boolean DEFAULT false NOT NULL,
    precio_unitario numeric(20,2) DEFAULT 0.00 NOT NULL
);


ALTER TABLE public.movimientomaterial OWNER TO postgres;

--
-- Name: movimientomaterial_id_movimientomaterial_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.movimientomaterial_id_movimientomaterial_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.movimientomaterial_id_movimientomaterial_seq OWNER TO postgres;

--
-- Name: movimientomaterial_id_movimientomaterial_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.movimientomaterial_id_movimientomaterial_seq OWNED BY public.movimientomaterial.id_movimientomaterial;


--
-- Name: movimientos; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.movimientos (
    id_movimiento integer NOT NULL,
    fecha date NOT NULL,
    nroremito character varying(50) NOT NULL,
    id_origen integer,
    tipo_origen character varying(50),
    id_destino integer NOT NULL,
    tipo_destino character varying(50),
    id_tipomovimiento integer,
    id_obra integer,
    costoflete numeric(10,2),
    observaciones text,
    created_at timestamp with time zone DEFAULT now(),
    updated_at timestamp with time zone DEFAULT now(),
    bitacora_verificado_at timestamp with time zone,
    bitacora_verificado_por text,
    precio_total numeric(20,2) DEFAULT 0.00 NOT NULL,
    id_flete integer,
    CONSTRAINT valido CHECK ((((tipo_origen)::text = ANY ((ARRAY['OBRA'::character varying, 'PROVEEDOR'::character varying, 'UBICACION'::character varying])::text[])) AND ((tipo_destino)::text = ANY ((ARRAY['OBRA'::character varying, 'PROVEEDOR'::character varying, 'UBICACION'::character varying])::text[]))))
);


ALTER TABLE public.movimientos OWNER TO postgres;

--
-- Name: COLUMN movimientos.created_at; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON COLUMN public.movimientos.created_at IS 'Fecha y hora real de alta en la base de datos. Para registros historicos previos a esta migracion puede quedar NULL.';


--
-- Name: COLUMN movimientos.updated_at; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON COLUMN public.movimientos.updated_at IS 'Fecha y hora de la ultima modificacion del registro en la base de datos.';


--
-- Name: COLUMN movimientos.bitacora_verificado_at; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON COLUMN public.movimientos.bitacora_verificado_at IS 'Fecha y hora en que el administrador verifico el remito en la bitacora.';


--
-- Name: COLUMN movimientos.bitacora_verificado_por; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON COLUMN public.movimientos.bitacora_verificado_por IS 'Identificador del usuario administrador que verifico el remito en la bitacora.';


--
-- Name: movimientos_id_movimiento_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.movimientos_id_movimiento_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.movimientos_id_movimiento_seq OWNER TO postgres;

--
-- Name: movimientos_id_movimiento_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.movimientos_id_movimiento_seq OWNED BY public.movimientos.id_movimiento;


--
-- Name: obligaciones_pago; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.obligaciones_pago (
    id_obligacion bigint NOT NULL,
    naturaleza text DEFAULT 'NO_FISCAL'::text NOT NULL,
    tipo_obligacion text NOT NULL,
    entidad text,
    beneficiario text NOT NULL,
    concepto text NOT NULL,
    periodo_operativo date NOT NULL,
    fecha_vencimiento date,
    importe numeric(14,2),
    importe_original numeric(14,2),
    intereses numeric(14,2) DEFAULT 0 NOT NULL,
    importe_por_definir boolean DEFAULT false NOT NULL,
    cuenta_origen text,
    medio_pago text,
    responsable_pago text,
    referencia text,
    external_id text,
    plan_id text,
    estado_pago text DEFAULT 'PENDIENTE'::text NOT NULL,
    fecha_pago date,
    motivo_anulacion text,
    observaciones text,
    usuario_email text,
    usuario_rol text,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL,
    archivada_at timestamp with time zone,
    beneficiary_identity_id bigint NOT NULL,
    entity_identity_id bigint,
    id_tipo_obligacion integer NOT NULL,
    monto_cancelado numeric(14,2) DEFAULT 0 NOT NULL,
    motivo_cancelacion text,
    cancelada_at timestamp with time zone,
    cancelada_por text,
    CONSTRAINT obligaciones_pago_cancelacion_check CHECK (((monto_cancelado >= (0)::numeric) AND (((estado_pago = 'CANCELADA'::text) AND (importe IS NOT NULL) AND (NOT importe_por_definir) AND (monto_cancelado > (0)::numeric) AND (monto_cancelado < importe) AND (NULLIF(btrim(motivo_cancelacion), ''::text) IS NOT NULL) AND (cancelada_at IS NOT NULL) AND (NULLIF(btrim(cancelada_por), ''::text) IS NOT NULL)) OR ((estado_pago <> 'CANCELADA'::text) AND (monto_cancelado = (0)::numeric) AND (motivo_cancelacion IS NULL) AND (cancelada_at IS NULL) AND (cancelada_por IS NULL))))),
    CONSTRAINT obligaciones_pago_estado_pago_check CHECK ((estado_pago = ANY (ARRAY['PENDIENTE'::text, 'PAGO_PARCIAL'::text, 'PAGADO'::text, 'ANULADA'::text, 'CANCELADA'::text]))),
    CONSTRAINT obligaciones_pago_importe_check CHECK (((importe IS NULL) OR (importe >= (0)::numeric))),
    CONSTRAINT obligaciones_pago_importe_definido_check CHECK ((importe_por_definir OR (importe IS NOT NULL))),
    CONSTRAINT obligaciones_pago_importe_original_check CHECK (((importe_original IS NULL) OR (importe_original >= (0)::numeric))),
    CONSTRAINT obligaciones_pago_intereses_check CHECK ((intereses >= (0)::numeric)),
    CONSTRAINT obligaciones_pago_naturaleza_check CHECK ((naturaleza = 'NO_FISCAL'::text))
);


ALTER TABLE public.obligaciones_pago OWNER TO postgres;

--
-- Name: COLUMN obligaciones_pago.tipo_obligacion; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON COLUMN public.obligaciones_pago.tipo_obligacion IS 'Legacy compatibility code synchronized by trigger; canonical relation is id_tipo_obligacion.';


--
-- Name: obligaciones_pago_id_obligacion_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

ALTER TABLE public.obligaciones_pago ALTER COLUMN id_obligacion ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public.obligaciones_pago_id_obligacion_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: obligaciones_pagos; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.obligaciones_pagos (
    id_pago bigint NOT NULL,
    id_obligacion bigint NOT NULL,
    fecha_pago date NOT NULL,
    importe numeric(14,2) NOT NULL,
    cuenta_origen text,
    medio_pago text,
    responsable_pago text,
    referencia text,
    observaciones text,
    usuario_email text,
    usuario_rol text,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT obligaciones_pagos_importe_check CHECK ((importe > (0)::numeric))
);


ALTER TABLE public.obligaciones_pagos OWNER TO postgres;

--
-- Name: obligaciones_pagos_id_pago_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

ALTER TABLE public.obligaciones_pagos ALTER COLUMN id_pago ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public.obligaciones_pagos_id_pago_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: obras; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.obras (
    id integer NOT NULL,
    nombre character varying(100) NOT NULL,
    activa boolean DEFAULT true,
    direccion character varying(200),
    cliente character varying(150),
    encargado character varying(100),
    fecha_inicio date,
    fecha_fin_estimada date,
    observaciones text
);


ALTER TABLE public.obras OWNER TO postgres;

--
-- Name: obras_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.obras_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.obras_id_seq OWNER TO postgres;

--
-- Name: obras_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.obras_id_seq OWNED BY public.obras.id;


--
-- Name: operational_identities; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.operational_identities (
    id bigint NOT NULL,
    identity_type text NOT NULL,
    display_name text NOT NULL,
    normalized_name text NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    created_by text,
    updated_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_by text,
    CONSTRAINT operational_identities_identity_type_check CHECK ((identity_type = ANY (ARRAY['BENEFICIARY'::text, 'ENTITY'::text])))
);


ALTER TABLE public.operational_identities OWNER TO postgres;

--
-- Name: operational_identities_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

ALTER TABLE public.operational_identities ALTER COLUMN id ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public.operational_identities_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: ordenes_compra; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.ordenes_compra (
    id_orden_compra integer NOT NULL,
    fecha date DEFAULT CURRENT_DATE NOT NULL,
    nro_orden character varying(80) NOT NULL,
    id_proveedor integer,
    observaciones text,
    estado character varying(40) DEFAULT 'Pendiente'::character varying NOT NULL,
    usuario_creacion character varying(150),
    usuario_revision character varying(150),
    fecha_revision timestamp without time zone,
    observacion_revision text,
    created_at timestamp without time zone DEFAULT now() NOT NULL,
    updated_at timestamp without time zone DEFAULT now() NOT NULL,
    nro_remito character varying,
    id_destino integer,
    tipo_destino character varying,
    costoflete numeric DEFAULT 0 NOT NULL,
    CONSTRAINT ordenes_compra_estado_check_v2 CHECK (((estado)::text = ANY ((ARRAY['Pendiente'::character varying, 'Recibida Completa'::character varying, 'Recibida Parcial'::character varying])::text[])))
);


ALTER TABLE public.ordenes_compra OWNER TO postgres;

--
-- Name: ordenes_compra_detalle; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.ordenes_compra_detalle (
    id_detalle integer NOT NULL,
    id_orden_compra integer NOT NULL,
    id_material integer NOT NULL,
    cantidad_pedida integer NOT NULL,
    stock_al_crear numeric(18,2) DEFAULT 0 NOT NULL,
    created_at timestamp without time zone DEFAULT now() NOT NULL,
    cantidad_recibida integer DEFAULT 0 NOT NULL,
    cantidad_faltante integer DEFAULT 0 NOT NULL,
    observacion_item text,
    stock_despues_recepcion numeric,
    CONSTRAINT ordenes_compra_detalle_cantidad_chk CHECK ((cantidad_pedida > 0)),
    CONSTRAINT ordenes_compra_detalle_cantidad_faltante_check_v2 CHECK (((cantidad_faltante >= 0) AND (cantidad_faltante = (cantidad_pedida - cantidad_recibida)))),
    CONSTRAINT ordenes_compra_detalle_cantidad_pedida_check_v2 CHECK ((cantidad_pedida > 0)),
    CONSTRAINT ordenes_compra_detalle_cantidad_recibida_check_v2 CHECK (((cantidad_recibida >= 0) AND (cantidad_recibida <= cantidad_pedida)))
);


ALTER TABLE public.ordenes_compra_detalle OWNER TO postgres;

--
-- Name: ordenes_compra_detalle_id_detalle_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.ordenes_compra_detalle_id_detalle_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.ordenes_compra_detalle_id_detalle_seq OWNER TO postgres;

--
-- Name: ordenes_compra_detalle_id_detalle_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.ordenes_compra_detalle_id_detalle_seq OWNED BY public.ordenes_compra_detalle.id_detalle;


--
-- Name: ordenes_compra_faltantes; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.ordenes_compra_faltantes (
    id_faltante integer NOT NULL,
    id_detalle integer NOT NULL,
    cantidad_faltante integer DEFAULT 0 NOT NULL,
    observaciones text,
    created_at timestamp without time zone DEFAULT now() NOT NULL,
    updated_at timestamp without time zone DEFAULT now() NOT NULL,
    CONSTRAINT ordenes_compra_faltantes_cantidad_chk CHECK ((cantidad_faltante >= 0))
);


ALTER TABLE public.ordenes_compra_faltantes OWNER TO postgres;

--
-- Name: ordenes_compra_faltantes_id_faltante_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.ordenes_compra_faltantes_id_faltante_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.ordenes_compra_faltantes_id_faltante_seq OWNER TO postgres;

--
-- Name: ordenes_compra_faltantes_id_faltante_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.ordenes_compra_faltantes_id_faltante_seq OWNED BY public.ordenes_compra_faltantes.id_faltante;


--
-- Name: ordenes_compra_id_orden_compra_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.ordenes_compra_id_orden_compra_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.ordenes_compra_id_orden_compra_seq OWNER TO postgres;

--
-- Name: ordenes_compra_id_orden_compra_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.ordenes_compra_id_orden_compra_seq OWNED BY public.ordenes_compra.id_orden_compra;


--
-- Name: organization_members; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.organization_members (
    organization_id uuid NOT NULL,
    user_id uuid NOT NULL,
    role text NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT organization_members_role_check CHECK ((role = ANY (ARRAY['owner'::text, 'admin'::text, 'member'::text])))
);


ALTER TABLE public.organization_members OWNER TO postgres;

--
-- Name: organizations; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.organizations (
    id uuid NOT NULL,
    name text NOT NULL,
    industry text,
    timezone text DEFAULT 'America/Argentina/Buenos_Aires'::text NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL
);


ALTER TABLE public.organizations OWNER TO postgres;

--
-- Name: proveedores; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.proveedores (
    id_proveedores integer NOT NULL,
    nombre character varying(50)
);


ALTER TABLE public.proveedores OWNER TO postgres;

--
-- Name: proveedores_id_proveedores_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.proveedores_id_proveedores_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.proveedores_id_proveedores_seq OWNER TO postgres;

--
-- Name: proveedores_id_proveedores_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.proveedores_id_proveedores_seq OWNED BY public.proveedores.id_proveedores;


--
-- Name: recortes; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.recortes (
    id integer NOT NULL,
    id_material integer NOT NULL,
    medida numeric(10,2) NOT NULL,
    cantidad numeric(10,2) DEFAULT 1 NOT NULL,
    fecha_creacion timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    ancho numeric,
    altura numeric,
    estado text,
    CONSTRAINT recortes_estado_check CHECK ((estado = ANY (ARRAY['Buen estado'::text, 'Regular'::text, 'Mal estado'::text]))),
    CONSTRAINT recortes_cantidad_check CHECK (cantidad >= 0)
);


ALTER TABLE public.recortes OWNER TO postgres;

--
-- Name: recortes_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.recortes_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.recortes_id_seq OWNER TO postgres;

--
-- Name: recortes_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.recortes_id_seq OWNED BY public.recortes.id;


--
-- Name: reporte_stock_materiales; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.reporte_stock_materiales (
    id_seccion integer NOT NULL,
    id_material integer NOT NULL,
    orden integer DEFAULT 1 NOT NULL,
    CONSTRAINT reporte_stock_materiales_orden_check CHECK ((orden > 0))
);


ALTER TABLE public.reporte_stock_materiales OWNER TO postgres;

--
-- Name: reporte_stock_secciones; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.reporte_stock_secciones (
    id integer NOT NULL,
    nombre text NOT NULL,
    orden integer DEFAULT 1 NOT NULL,
    CONSTRAINT reporte_stock_secciones_orden_check CHECK ((orden > 0))
);


ALTER TABLE public.reporte_stock_secciones OWNER TO postgres;

--
-- Name: reporte_stock_secciones_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

ALTER TABLE public.reporte_stock_secciones ALTER COLUMN id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.reporte_stock_secciones_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: schema_migrations; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.schema_migrations (
    filename text NOT NULL,
    applied_at timestamp with time zone DEFAULT now() NOT NULL
);


ALTER TABLE public.schema_migrations OWNER TO postgres;

--
-- Name: sueldo_pago_distribuciones; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.sueldo_pago_distribuciones (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    id_pago uuid NOT NULL,
    destino text NOT NULL,
    id_obra integer,
    importe numeric(14,2) NOT NULL,
    obra_nombre_snapshot text,
    porcentaje numeric(8,4),
    observacion text,
    orden integer DEFAULT 0 NOT NULL,
    CONSTRAINT sueldo_pago_distribuciones_destino_check CHECK ((destino = ANY (ARRAY['OBRA'::text, 'COSTO_COMUN'::text, 'NO_DISTRIBUIDO'::text]))),
    CONSTRAINT sueldo_pago_distribuciones_destino_obra_check CHECK ((((destino = 'OBRA'::text) AND (id_obra IS NOT NULL)) OR ((destino <> 'OBRA'::text) AND (id_obra IS NULL)))),
    CONSTRAINT sueldo_pago_distribuciones_importe_check CHECK ((importe >= (0)::numeric))
);


ALTER TABLE public.sueldo_pago_distribuciones OWNER TO postgres;

--
-- Name: sueldo_personas; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.sueldo_personas (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    nombre text NOT NULL,
    apellido text NOT NULL,
    tipo_documento text DEFAULT 'DNI'::text NOT NULL,
    numero_documento text NOT NULL,
    documento_normalizado text NOT NULL,
    cuil text,
    cuil_normalizado text,
    email text,
    telefono text,
    observaciones text,
    version integer DEFAULT 1 NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL,
    created_by text,
    updated_by text,
    CONSTRAINT sueldo_personas_apellido_check CHECK ((btrim(apellido) <> ''::text)),
    CONSTRAINT sueldo_personas_nombre_check CHECK ((btrim(nombre) <> ''::text))
);


ALTER TABLE public.sueldo_personas OWNER TO postgres;

--
-- Name: sueldo_presupuestos_obra; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.sueldo_presupuestos_obra (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    periodo date NOT NULL,
    id_obra integer NOT NULL,
    modalidad text NOT NULL,
    importe numeric(14,2) NOT NULL,
    observaciones text,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT sueldo_presupuestos_obra_importe_check CHECK ((importe >= (0)::numeric)),
    CONSTRAINT sueldo_presupuestos_obra_modalidad_check CHECK ((modalidad = ANY (ARRAY['REGISTRADO'::text, 'NO_REGISTRADO'::text]))),
    CONSTRAINT sueldo_presupuestos_obra_periodo_check CHECK ((periodo = (date_trunc('month'::text, (periodo)::timestamp with time zone))::date))
);


ALTER TABLE public.sueldo_presupuestos_obra OWNER TO postgres;

--
-- Name: sueldo_vinculo_condiciones; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.sueldo_vinculo_condiciones (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    id_vinculo uuid NOT NULL,
    vigencia_desde date NOT NULL,
    vigencia_hasta date,
    puesto text,
    remuneracion_referencia numeric(14,2),
    observaciones text,
    version integer DEFAULT 1 NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL,
    created_by text,
    updated_by text,
    CONSTRAINT sueldo_vinculo_condiciones_check CHECK (((vigencia_hasta IS NULL) OR (vigencia_hasta >= vigencia_desde))),
    CONSTRAINT sueldo_vinculo_condiciones_remuneracion_referencia_check CHECK (((remuneracion_referencia IS NULL) OR (remuneracion_referencia >= (0)::numeric)))
);


ALTER TABLE public.sueldo_vinculo_condiciones OWNER TO postgres;

--
-- Name: sueldo_vinculo_distribuciones; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.sueldo_vinculo_distribuciones (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    id_vinculo uuid NOT NULL,
    modo text NOT NULL,
    destino text NOT NULL,
    id_obra integer,
    porcentaje numeric(8,4),
    orden integer DEFAULT 0 NOT NULL,
    observacion text,
    CONSTRAINT sueldo_vinculo_distribuciones_check CHECK ((((destino = 'OBRA'::text) AND (id_obra IS NOT NULL)) OR ((destino <> 'OBRA'::text) AND (id_obra IS NULL)))),
    CONSTRAINT sueldo_vinculo_distribuciones_destino_check CHECK ((destino = ANY (ARRAY['OBRA'::text, 'COSTO_COMUN'::text, 'NO_DISTRIBUIDO'::text]))),
    CONSTRAINT sueldo_vinculo_distribuciones_modo_check CHECK ((modo = ANY (ARRAY['PORCENTAJE'::text, 'IMPORTE'::text]))),
    CONSTRAINT sueldo_vinculo_distribuciones_porcentaje_check CHECK (((porcentaje IS NULL) OR (porcentaje >= (0)::numeric)))
);


ALTER TABLE public.sueldo_vinculo_distribuciones OWNER TO postgres;

--
-- Name: sueldo_vinculos; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.sueldo_vinculos (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    id_persona uuid NOT NULL,
    codigo text NOT NULL,
    modalidad text NOT NULL,
    fecha_inicio date NOT NULL,
    fecha_baja date,
    motivo_baja text,
    puesto text,
    remuneracion_referencia numeric(14,2),
    observaciones text,
    version integer DEFAULT 1 NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL,
    created_by text,
    updated_by text,
    CONSTRAINT sueldo_vinculos_check CHECK (((fecha_baja IS NULL) OR (fecha_baja >= fecha_inicio))),
    CONSTRAINT sueldo_vinculos_modalidad_check CHECK ((modalidad = ANY (ARRAY['REGISTRADO'::text, 'NO_REGISTRADO'::text]))),
    CONSTRAINT sueldo_vinculos_remuneracion_referencia_check CHECK (((remuneracion_referencia IS NULL) OR (remuneracion_referencia >= (0)::numeric)))
);


ALTER TABLE public.sueldo_vinculos OWNER TO postgres;

--
-- Name: sueldos_auditoria; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.sueldos_auditoria (
    id bigint NOT NULL,
    actor_email text,
    actor_rol text,
    accion text NOT NULL,
    entidad_tipo text NOT NULL,
    entidad_id uuid,
    modalidad_scope text NOT NULL,
    detalles jsonb DEFAULT '{}'::jsonb NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    actor_user_id text,
    CONSTRAINT sueldos_auditoria_modalidad_scope_check CHECK ((modalidad_scope = ANY (ARRAY['REGISTRADO'::text, 'NO_REGISTRADO'::text])))
);


ALTER TABLE public.sueldos_auditoria OWNER TO postgres;

--
-- Name: sueldos_auditoria_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

ALTER TABLE public.sueldos_auditoria ALTER COLUMN id ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public.sueldos_auditoria_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: sueldos_pagos; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.sueldos_pagos (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    id_liquidacion uuid NOT NULL,
    fecha date NOT NULL,
    importe numeric(14,2) NOT NULL,
    medio text,
    cuenta text,
    referencia text,
    observaciones text,
    estado text DEFAULT 'ACTIVO'::text NOT NULL,
    motivo_reversion text,
    revertido_at timestamp with time zone,
    revertido_por text,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    created_by text,
    idempotency_key text,
    version integer DEFAULT 1 NOT NULL,
    CONSTRAINT sueldos_pagos_estado_check CHECK ((estado = ANY (ARRAY['ACTIVO'::text, 'REVERTIDO'::text]))),
    CONSTRAINT sueldos_pagos_importe_check CHECK ((importe > (0)::numeric))
);


ALTER TABLE public.sueldos_pagos OWNER TO postgres;

--
-- Name: tasks; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.tasks (
    id uuid NOT NULL,
    organization_id uuid NOT NULL,
    created_by_user_id uuid,
    title text NOT NULL,
    status text DEFAULT 'open'::text NOT NULL,
    due_at timestamp with time zone,
    metadata jsonb DEFAULT '{}'::jsonb NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT tasks_status_check CHECK ((status = ANY (ARRAY['open'::text, 'done'::text, 'canceled'::text])))
);


ALTER TABLE public.tasks OWNER TO postgres;

--
-- Name: tipo_movimiento; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.tipo_movimiento (
    id_tipomovimiento integer NOT NULL,
    nombre character varying(100)
);


ALTER TABLE public.tipo_movimiento OWNER TO postgres;

--
-- Name: tipo_movimiento_id_tipomovimiento_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

ALTER TABLE public.tipo_movimiento ALTER COLUMN id_tipomovimiento ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.tipo_movimiento_id_tipomovimiento_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: tipos_obligacion; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.tipos_obligacion (
    id integer NOT NULL,
    codigo text NOT NULL,
    nombre text NOT NULL,
    normalized_name text NOT NULL,
    grupo_control text DEFAULT 'GASTO_OPERATIVO'::text NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT tipos_obligacion_grupo_control_check CHECK ((grupo_control = ANY (ARRAY['GASTO_OPERATIVO'::text, 'NO_DEDUCIBLE'::text, 'PLAN_PAGO'::text]))),
    CONSTRAINT tipos_obligacion_nombre_check CHECK (((length(TRIM(BOTH FROM nombre)) >= 1) AND (length(TRIM(BOTH FROM nombre)) <= 100)))
);


ALTER TABLE public.tipos_obligacion OWNER TO postgres;

--
-- Name: tipos_obligacion_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

ALTER TABLE public.tipos_obligacion ALTER COLUMN id ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public.tipos_obligacion_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: ubicacion; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.ubicacion (
    id_ubicacion integer NOT NULL,
    nombre character varying(100) NOT NULL
);


ALTER TABLE public.ubicacion OWNER TO postgres;

--
-- Name: ubicaciones_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.ubicaciones_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.ubicaciones_id_seq OWNER TO postgres;

--
-- Name: ubicaciones_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.ubicaciones_id_seq OWNED BY public.ubicacion.id_ubicacion;


--
-- Name: usage_events; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.usage_events (
    id uuid NOT NULL,
    organization_id uuid NOT NULL,
    conversation_id uuid,
    user_id uuid,
    model text,
    prompt_tokens integer,
    completion_tokens integer,
    total_tokens integer,
    estimated_cost_usd numeric(12,6),
    latency_ms integer,
    metadata jsonb DEFAULT '{}'::jsonb NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL
);


ALTER TABLE public.usage_events OWNER TO postgres;

--
-- Name: users; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.users (
    id uuid NOT NULL,
    email text NOT NULL,
    full_name text,
    created_at timestamp with time zone DEFAULT now() NOT NULL
);


ALTER TABLE public.users OWNER TO postgres;

--
-- Name: visibilidad_modulos; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.visibilidad_modulos (
    id integer NOT NULL,
    rol text NOT NULL,
    modulo_key text NOT NULL,
    visible boolean DEFAULT true NOT NULL,
    modulo text
);


ALTER TABLE public.visibilidad_modulos OWNER TO postgres;

--
-- Name: visibilidad_modulos_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.visibilidad_modulos_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.visibilidad_modulos_id_seq OWNER TO postgres;

--
-- Name: visibilidad_modulos_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.visibilidad_modulos_id_seq OWNED BY public.visibilidad_modulos.id;


--
-- Name: backup_info_20260325 id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.backup_info_20260325 ALTER COLUMN id SET DEFAULT nextval('public.backup_info_20260325_id_seq'::regclass);


--
-- Name: bitacora id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.bitacora ALTER COLUMN id SET DEFAULT nextval('public.bitacora_id_seq'::regclass);


--
-- Name: canaletas_plegados id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.canaletas_plegados ALTER COLUMN id SET DEFAULT nextval('public.canaletas_plegados_id_seq'::regclass);


--
-- Name: categorias id_categoria; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.categorias ALTER COLUMN id_categoria SET DEFAULT nextval('public.categorias_id_categoria_seq'::regclass);


--
-- Name: conteos_inventario id_conteo; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.conteos_inventario ALTER COLUMN id_conteo SET DEFAULT nextval('public.conteos_inventario_id_conteo_seq'::regclass);


--
-- Name: conteos_inventario_detalle id_detalle; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.conteos_inventario_detalle ALTER COLUMN id_detalle SET DEFAULT nextval('public.conteos_inventario_detalle_id_detalle_seq'::regclass);


--
-- Name: factura_pagos id_pago; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.factura_pagos ALTER COLUMN id_pago SET DEFAULT nextval('public.factura_pagos_id_pago_seq'::regclass);


--
-- Name: facturas id_factura; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.facturas ALTER COLUMN id_factura SET DEFAULT nextval('public.facturas_id_factura_seq'::regclass);


--
-- Name: fletes id_flete; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.fletes ALTER COLUMN id_flete SET DEFAULT nextval('public.fletes_id_flete_seq'::regclass);


--
-- Name: herramienta_movimientos id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.herramienta_movimientos ALTER COLUMN id SET DEFAULT nextval('public.herramienta_movimientos_id_seq'::regclass);


--
-- Name: herramientas id_herramienta; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.herramientas ALTER COLUMN id_herramienta SET DEFAULT nextval('public.herramientas_id_herramienta_seq'::regclass);


--
-- Name: insumo_movimientos id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.insumo_movimientos ALTER COLUMN id SET DEFAULT nextval('public.insumo_movimientos_id_seq'::regclass);


--
-- Name: insumos id_insumo; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.insumos ALTER COLUMN id_insumo SET DEFAULT nextval('public.insumos_id_insumo_seq'::regclass);


--
-- Name: materiales id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.materiales ALTER COLUMN id SET DEFAULT nextval('public.materiales_id_seq'::regclass);


--
-- Name: materiales_proveedores id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.materiales_proveedores ALTER COLUMN id SET DEFAULT nextval('public.materiales_proveedores_id_seq'::regclass);


--
-- Name: movimientomaterial id_movimientomaterial; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.movimientomaterial ALTER COLUMN id_movimientomaterial SET DEFAULT nextval('public.movimientomaterial_id_movimientomaterial_seq'::regclass);


--
-- Name: movimientos id_movimiento; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.movimientos ALTER COLUMN id_movimiento SET DEFAULT nextval('public.movimientos_id_movimiento_seq'::regclass);


--
-- Name: obras id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.obras ALTER COLUMN id SET DEFAULT nextval('public.obras_id_seq'::regclass);


--
-- Name: ordenes_compra id_orden_compra; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.ordenes_compra ALTER COLUMN id_orden_compra SET DEFAULT nextval('public.ordenes_compra_id_orden_compra_seq'::regclass);


--
-- Name: ordenes_compra_detalle id_detalle; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.ordenes_compra_detalle ALTER COLUMN id_detalle SET DEFAULT nextval('public.ordenes_compra_detalle_id_detalle_seq'::regclass);


--
-- Name: ordenes_compra_faltantes id_faltante; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.ordenes_compra_faltantes ALTER COLUMN id_faltante SET DEFAULT nextval('public.ordenes_compra_faltantes_id_faltante_seq'::regclass);


--
-- Name: proveedores id_proveedores; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.proveedores ALTER COLUMN id_proveedores SET DEFAULT nextval('public.proveedores_id_proveedores_seq'::regclass);


--
-- Name: recortes id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.recortes ALTER COLUMN id SET DEFAULT nextval('public.recortes_id_seq'::regclass);


--
-- Name: ubicacion id_ubicacion; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.ubicacion ALTER COLUMN id_ubicacion SET DEFAULT nextval('public.ubicaciones_id_seq'::regclass);


--
-- Name: visibilidad_modulos id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.visibilidad_modulos ALTER COLUMN id SET DEFAULT nextval('public.visibilidad_modulos_id_seq'::regclass);


--
-- Name: _migracion_origen_destino_map _migracion_origen_destino_map_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public._migracion_origen_destino_map
    ADD CONSTRAINT _migracion_origen_destino_map_pkey PRIMARY KEY (nombre_texto);


--
-- Name: assistant_configs assistant_configs_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.assistant_configs
    ADD CONSTRAINT assistant_configs_pkey PRIMARY KEY (organization_id);


--
-- Name: audit_logs audit_logs_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.audit_logs
    ADD CONSTRAINT audit_logs_pkey PRIMARY KEY (id);


--
-- Name: authorization_policy_state authorization_policy_state_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.authorization_policy_state
    ADD CONSTRAINT authorization_policy_state_pkey PRIMARY KEY (id);


--
-- Name: backup_info_20260325 backup_info_20260325_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.backup_info_20260325
    ADD CONSTRAINT backup_info_20260325_pkey PRIMARY KEY (id);


--
-- Name: bitacora bitacora_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.bitacora
    ADD CONSTRAINT bitacora_pkey PRIMARY KEY (id);


--
-- Name: business_memories business_memories_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.business_memories
    ADD CONSTRAINT business_memories_pkey PRIMARY KEY (id);


--
-- Name: canaletas_plegados_fotos canaletas_plegados_fotos_object_path_uk; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.canaletas_plegados_fotos
    ADD CONSTRAINT canaletas_plegados_fotos_object_path_uk UNIQUE (bucket_id, object_path);


--
-- Name: canaletas_plegados_fotos canaletas_plegados_fotos_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.canaletas_plegados_fotos
    ADD CONSTRAINT canaletas_plegados_fotos_pkey PRIMARY KEY (id);


--
-- Name: canaletas_plegados canaletas_plegados_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.canaletas_plegados
    ADD CONSTRAINT canaletas_plegados_pkey PRIMARY KEY (id);


--
-- Name: categorias categorias_nombre_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.categorias
    ADD CONSTRAINT categorias_nombre_key UNIQUE (nombre);


--
-- Name: categorias categorias_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.categorias
    ADD CONSTRAINT categorias_pkey PRIMARY KEY (id_categoria);


--
-- Name: conceptos_sueldo conceptos_sueldo_codigo_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.conceptos_sueldo
    ADD CONSTRAINT conceptos_sueldo_codigo_key UNIQUE (codigo);


--
-- Name: conceptos_sueldo conceptos_sueldo_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.conceptos_sueldo
    ADD CONSTRAINT conceptos_sueldo_pkey PRIMARY KEY (id);


--
-- Name: conteos_inventario_detalle conteos_inventario_detalle_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.conteos_inventario_detalle
    ADD CONSTRAINT conteos_inventario_detalle_pkey PRIMARY KEY (id_detalle);


--
-- Name: conteos_inventario_detalle conteos_inventario_detalle_unico_material; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.conteos_inventario_detalle
    ADD CONSTRAINT conteos_inventario_detalle_unico_material UNIQUE (id_conteo, id_material);


--
-- Name: conteos_inventario conteos_inventario_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.conteos_inventario
    ADD CONSTRAINT conteos_inventario_pkey PRIMARY KEY (id_conteo);


--
-- Name: conversation_summaries conversation_summaries_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.conversation_summaries
    ADD CONSTRAINT conversation_summaries_pkey PRIMARY KEY (conversation_id);


--
-- Name: conversations conversations_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.conversations
    ADD CONSTRAINT conversations_pkey PRIMARY KEY (id);


--
-- Name: document_chunks document_chunks_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.document_chunks
    ADD CONSTRAINT document_chunks_pkey PRIMARY KEY (id);


--
-- Name: documents documents_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.documents
    ADD CONSTRAINT documents_pkey PRIMARY KEY (id);


--
-- Name: factura_pagos factura_pagos_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.factura_pagos
    ADD CONSTRAINT factura_pagos_pkey PRIMARY KEY (id_pago);


--
-- Name: facturas facturas_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.facturas
    ADD CONSTRAINT facturas_pkey PRIMARY KEY (id_factura);


--
-- Name: financial_operation_requests financial_operation_requests_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.financial_operation_requests
    ADD CONSTRAINT financial_operation_requests_pkey PRIMARY KEY (actor, operation, request_key);


--
-- Name: fiscal_periods fiscal_periods_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.fiscal_periods
    ADD CONSTRAINT fiscal_periods_pkey PRIMARY KEY (period_start);


--
-- Name: fiscal_thresholds fiscal_thresholds_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.fiscal_thresholds
    ADD CONSTRAINT fiscal_thresholds_pkey PRIMARY KEY (singleton);


--
-- Name: fletes fletes_nombre_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.fletes
    ADD CONSTRAINT fletes_nombre_key UNIQUE (nombre);


--
-- Name: fletes fletes_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.fletes
    ADD CONSTRAINT fletes_pkey PRIMARY KEY (id_flete);


--
-- Name: herramienta_movimientos herramienta_movimientos_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.herramienta_movimientos
    ADD CONSTRAINT herramienta_movimientos_pkey PRIMARY KEY (id);


--
-- Name: herramientas herramientas_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.herramientas
    ADD CONSTRAINT herramientas_pkey PRIMARY KEY (id_herramienta);


--
-- Name: ingestion_jobs ingestion_jobs_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.ingestion_jobs
    ADD CONSTRAINT ingestion_jobs_pkey PRIMARY KEY (id);


--
-- Name: insumo_movimientos insumo_movimientos_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.insumo_movimientos
    ADD CONSTRAINT insumo_movimientos_pkey PRIMARY KEY (id);


--
-- Name: insumos insumos_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.insumos
    ADD CONSTRAINT insumos_pkey PRIMARY KEY (id_insumo);


--
-- Name: invoice_categories invoice_categories_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.invoice_categories
    ADD CONSTRAINT invoice_categories_pkey PRIMARY KEY (id);


--
-- Name: liquidacion_sueldo_conceptos liquidacion_sueldo_conceptos_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.liquidacion_sueldo_conceptos
    ADD CONSTRAINT liquidacion_sueldo_conceptos_pkey PRIMARY KEY (id);


--
-- Name: liquidacion_sueldo_distribuciones liquidacion_sueldo_distribuciones_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.liquidacion_sueldo_distribuciones
    ADD CONSTRAINT liquidacion_sueldo_distribuciones_pkey PRIMARY KEY (id);


--
-- Name: liquidaciones_sueldo liquidaciones_sueldo_id_modalidad_snapshot_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.liquidaciones_sueldo
    ADD CONSTRAINT liquidaciones_sueldo_id_modalidad_snapshot_key UNIQUE (id, modalidad_snapshot);


--
-- Name: liquidaciones_sueldo liquidaciones_sueldo_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.liquidaciones_sueldo
    ADD CONSTRAINT liquidaciones_sueldo_pkey PRIMARY KEY (id);


--
-- Name: materiales materiales_codigo_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.materiales
    ADD CONSTRAINT materiales_codigo_key UNIQUE (codigo);


--
-- Name: materiales materiales_nombre_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.materiales
    ADD CONSTRAINT materiales_nombre_key UNIQUE (nombre);


--
-- Name: materiales materiales_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.materiales
    ADD CONSTRAINT materiales_pkey PRIMARY KEY (id);


--
-- Name: materiales_proveedores materiales_proveedores_id_material_id_proveedor_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.materiales_proveedores
    ADD CONSTRAINT materiales_proveedores_id_material_id_proveedor_key UNIQUE (id_material, id_proveedor);


--
-- Name: materiales_proveedores materiales_proveedores_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.materiales_proveedores
    ADD CONSTRAINT materiales_proveedores_pkey PRIMARY KEY (id);


--
-- Name: messages messages_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.messages
    ADD CONSTRAINT messages_pkey PRIMARY KEY (id);


--
-- Name: movimientomaterial movimientomaterial_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.movimientomaterial
    ADD CONSTRAINT movimientomaterial_pkey PRIMARY KEY (id_movimientomaterial);


--
-- Name: movimientos movimientos_nroremito_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.movimientos
    ADD CONSTRAINT movimientos_nroremito_key UNIQUE (nroremito);


--
-- Name: movimientos movimientos_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.movimientos
    ADD CONSTRAINT movimientos_pkey PRIMARY KEY (id_movimiento);


--
-- Name: obligaciones_pago obligaciones_pago_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.obligaciones_pago
    ADD CONSTRAINT obligaciones_pago_pkey PRIMARY KEY (id_obligacion);


--
-- Name: obligaciones_pagos obligaciones_pagos_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.obligaciones_pagos
    ADD CONSTRAINT obligaciones_pagos_pkey PRIMARY KEY (id_pago);


--
-- Name: obras obras_nombre_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.obras
    ADD CONSTRAINT obras_nombre_key UNIQUE (nombre);


--
-- Name: obras obras_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.obras
    ADD CONSTRAINT obras_pkey PRIMARY KEY (id);


--
-- Name: operational_identities operational_identities_identity_type_normalized_name_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.operational_identities
    ADD CONSTRAINT operational_identities_identity_type_normalized_name_key UNIQUE (identity_type, normalized_name);


--
-- Name: operational_identities operational_identities_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.operational_identities
    ADD CONSTRAINT operational_identities_pkey PRIMARY KEY (id);


--
-- Name: ordenes_compra_detalle ordenes_compra_detalle_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.ordenes_compra_detalle
    ADD CONSTRAINT ordenes_compra_detalle_pkey PRIMARY KEY (id_detalle);


--
-- Name: ordenes_compra_faltantes ordenes_compra_faltantes_id_detalle_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.ordenes_compra_faltantes
    ADD CONSTRAINT ordenes_compra_faltantes_id_detalle_key UNIQUE (id_detalle);


--
-- Name: ordenes_compra_faltantes ordenes_compra_faltantes_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.ordenes_compra_faltantes
    ADD CONSTRAINT ordenes_compra_faltantes_pkey PRIMARY KEY (id_faltante);


--
-- Name: ordenes_compra ordenes_compra_nro_orden_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.ordenes_compra
    ADD CONSTRAINT ordenes_compra_nro_orden_key UNIQUE (nro_orden);


--
-- Name: ordenes_compra ordenes_compra_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.ordenes_compra
    ADD CONSTRAINT ordenes_compra_pkey PRIMARY KEY (id_orden_compra);


--
-- Name: organization_members organization_members_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.organization_members
    ADD CONSTRAINT organization_members_pkey PRIMARY KEY (organization_id, user_id);


--
-- Name: organizations organizations_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.organizations
    ADD CONSTRAINT organizations_pkey PRIMARY KEY (id);


--
-- Name: proveedores proveedores_nombre_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.proveedores
    ADD CONSTRAINT proveedores_nombre_key UNIQUE (nombre);


--
-- Name: proveedores proveedores_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.proveedores
    ADD CONSTRAINT proveedores_pkey PRIMARY KEY (id_proveedores);


--
-- Name: recortes recortes_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.recortes
    ADD CONSTRAINT recortes_pkey PRIMARY KEY (id);


--
-- Name: reporte_stock_materiales reporte_stock_materiales_id_material_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.reporte_stock_materiales
    ADD CONSTRAINT reporte_stock_materiales_id_material_key UNIQUE (id_material);


--
-- Name: reporte_stock_materiales reporte_stock_materiales_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.reporte_stock_materiales
    ADD CONSTRAINT reporte_stock_materiales_pkey PRIMARY KEY (id_seccion, id_material);


--
-- Name: reporte_stock_secciones reporte_stock_secciones_nombre_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.reporte_stock_secciones
    ADD CONSTRAINT reporte_stock_secciones_nombre_key UNIQUE (nombre);


--
-- Name: reporte_stock_secciones reporte_stock_secciones_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.reporte_stock_secciones
    ADD CONSTRAINT reporte_stock_secciones_pkey PRIMARY KEY (id);


--
-- Name: schema_migrations schema_migrations_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.schema_migrations
    ADD CONSTRAINT schema_migrations_pkey PRIMARY KEY (filename);


--
-- Name: sueldo_pago_distribuciones sueldo_pago_distribuciones_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.sueldo_pago_distribuciones
    ADD CONSTRAINT sueldo_pago_distribuciones_pkey PRIMARY KEY (id);


--
-- Name: sueldo_personas sueldo_personas_cuil_normalizado_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.sueldo_personas
    ADD CONSTRAINT sueldo_personas_cuil_normalizado_key UNIQUE (cuil_normalizado);


--
-- Name: sueldo_personas sueldo_personas_documento_normalizado_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.sueldo_personas
    ADD CONSTRAINT sueldo_personas_documento_normalizado_key UNIQUE (documento_normalizado);


--
-- Name: sueldo_personas sueldo_personas_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.sueldo_personas
    ADD CONSTRAINT sueldo_personas_pkey PRIMARY KEY (id);


--
-- Name: sueldo_presupuestos_obra sueldo_presupuestos_obra_periodo_id_obra_modalidad_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.sueldo_presupuestos_obra
    ADD CONSTRAINT sueldo_presupuestos_obra_periodo_id_obra_modalidad_key UNIQUE (periodo, id_obra, modalidad);


--
-- Name: sueldo_presupuestos_obra sueldo_presupuestos_obra_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.sueldo_presupuestos_obra
    ADD CONSTRAINT sueldo_presupuestos_obra_pkey PRIMARY KEY (id);


--
-- Name: sueldo_vinculo_condiciones sueldo_vinculo_condiciones_id_vinculo_vigencia_desde_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.sueldo_vinculo_condiciones
    ADD CONSTRAINT sueldo_vinculo_condiciones_id_vinculo_vigencia_desde_key UNIQUE (id_vinculo, vigencia_desde);


--
-- Name: sueldo_vinculo_condiciones sueldo_vinculo_condiciones_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.sueldo_vinculo_condiciones
    ADD CONSTRAINT sueldo_vinculo_condiciones_pkey PRIMARY KEY (id);


--
-- Name: sueldo_vinculo_distribuciones sueldo_vinculo_distribuciones_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.sueldo_vinculo_distribuciones
    ADD CONSTRAINT sueldo_vinculo_distribuciones_pkey PRIMARY KEY (id);


--
-- Name: sueldo_vinculos sueldo_vinculos_codigo_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.sueldo_vinculos
    ADD CONSTRAINT sueldo_vinculos_codigo_key UNIQUE (codigo);


--
-- Name: sueldo_vinculos sueldo_vinculos_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.sueldo_vinculos
    ADD CONSTRAINT sueldo_vinculos_pkey PRIMARY KEY (id);


--
-- Name: sueldos_auditoria sueldos_auditoria_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.sueldos_auditoria
    ADD CONSTRAINT sueldos_auditoria_pkey PRIMARY KEY (id);


--
-- Name: sueldos_pagos sueldos_pagos_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.sueldos_pagos
    ADD CONSTRAINT sueldos_pagos_pkey PRIMARY KEY (id);


--
-- Name: tasks tasks_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.tasks
    ADD CONSTRAINT tasks_pkey PRIMARY KEY (id);


--
-- Name: tipo_movimiento tipo_movimiento_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.tipo_movimiento
    ADD CONSTRAINT tipo_movimiento_pkey PRIMARY KEY (id_tipomovimiento);


--
-- Name: tipos_obligacion tipos_obligacion_codigo_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.tipos_obligacion
    ADD CONSTRAINT tipos_obligacion_codigo_key UNIQUE (codigo);


--
-- Name: tipos_obligacion tipos_obligacion_normalized_name_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.tipos_obligacion
    ADD CONSTRAINT tipos_obligacion_normalized_name_key UNIQUE (normalized_name);


--
-- Name: tipos_obligacion tipos_obligacion_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.tipos_obligacion
    ADD CONSTRAINT tipos_obligacion_pkey PRIMARY KEY (id);


--
-- Name: ubicacion ubicaciones_nombre_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.ubicacion
    ADD CONSTRAINT ubicaciones_nombre_key UNIQUE (nombre);


--
-- Name: ubicacion ubicaciones_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.ubicacion
    ADD CONSTRAINT ubicaciones_pkey PRIMARY KEY (id_ubicacion);


--
-- Name: usage_events usage_events_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.usage_events
    ADD CONSTRAINT usage_events_pkey PRIMARY KEY (id);


--
-- Name: users users_email_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.users
    ADD CONSTRAINT users_email_key UNIQUE (email);


--
-- Name: users users_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.users
    ADD CONSTRAINT users_pkey PRIMARY KEY (id);


--
-- Name: visibilidad_modulos visibilidad_modulos_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.visibilidad_modulos
    ADD CONSTRAINT visibilidad_modulos_pkey PRIMARY KEY (id);


--
-- Name: visibilidad_modulos visibilidad_modulos_rol_modulo_key_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.visibilidad_modulos
    ADD CONSTRAINT visibilidad_modulos_rol_modulo_key_key UNIQUE (rol, modulo_key);


--
-- Name: idx_business_memories_org_key; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_business_memories_org_key ON public.business_memories USING btree (organization_id, key);


--
-- Name: idx_canaletas_plegados_fecha; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_canaletas_plegados_fecha ON public.canaletas_plegados USING btree (fecha_creacion DESC);


--
-- Name: idx_canaletas_plegados_fotos_canaleta; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_canaletas_plegados_fotos_canaleta ON public.canaletas_plegados_fotos USING btree (canaleta_plegado_id, orden, id);


--
-- Name: idx_canaletas_plegados_obra; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_canaletas_plegados_obra ON public.canaletas_plegados USING btree (id_obra);


--
-- Name: idx_conteos_inventario_detalle_conteo; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_conteos_inventario_detalle_conteo ON public.conteos_inventario_detalle USING btree (id_conteo);


--
-- Name: idx_conteos_inventario_detalle_diferencia; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_conteos_inventario_detalle_diferencia ON public.conteos_inventario_detalle USING btree (diferencia);


--
-- Name: idx_conteos_inventario_detalle_material; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_conteos_inventario_detalle_material ON public.conteos_inventario_detalle USING btree (id_material);


--
-- Name: idx_conteos_inventario_estado; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_conteos_inventario_estado ON public.conteos_inventario USING btree (estado);


--
-- Name: idx_conteos_inventario_fecha_inicio; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_conteos_inventario_fecha_inicio ON public.conteos_inventario USING btree (fecha_inicio DESC);


--
-- Name: idx_conteos_inventario_usuario_inicio; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_conteos_inventario_usuario_inicio ON public.conteos_inventario USING btree (usuario_inicio);


--
-- Name: idx_conversations_org_last_message; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_conversations_org_last_message ON public.conversations USING btree (organization_id, last_message_at DESC);


--
-- Name: idx_document_chunks_org_document; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_document_chunks_org_document ON public.document_chunks USING btree (organization_id, document_id, chunk_index);


--
-- Name: idx_documents_org_status; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_documents_org_status ON public.documents USING btree (organization_id, status);


--
-- Name: idx_factura_pagos_id_factura; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_factura_pagos_id_factura ON public.factura_pagos USING btree (id_factura);


--
-- Name: idx_factura_pagos_periodo_caja; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_factura_pagos_periodo_caja ON public.factura_pagos USING btree (periodo_caja);


--
-- Name: idx_facturas_category_id; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_facturas_category_id ON public.facturas USING btree (category_id);


--
-- Name: idx_facturas_estado_pago; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_facturas_estado_pago ON public.facturas USING btree (estado_pago);


--
-- Name: idx_facturas_fecha; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_facturas_fecha ON public.facturas USING btree (fecha DESC);


--
-- Name: idx_facturas_fecha_vencimiento; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_facturas_fecha_vencimiento ON public.facturas USING btree (fecha_vencimiento);


--
-- Name: idx_facturas_fiscal_scan; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_facturas_fiscal_scan ON public.facturas USING btree (mes_imputacion, tipo_operacion, estado_pago);


--
-- Name: idx_facturas_mes_imputacion; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_facturas_mes_imputacion ON public.facturas USING btree (mes_imputacion DESC);


--
-- Name: idx_facturas_tipo_operacion; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_facturas_tipo_operacion ON public.facturas USING btree (tipo_operacion);


--
-- Name: idx_liquidaciones_sueldo_scope; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_liquidaciones_sueldo_scope ON public.liquidaciones_sueldo USING btree (modalidad_snapshot, periodo DESC, estado);


--
-- Name: idx_liquidaciones_sueldo_vinculo_periodo; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_liquidaciones_sueldo_vinculo_periodo ON public.liquidaciones_sueldo USING btree (id_vinculo, periodo DESC, estado);


--
-- Name: idx_messages_conversation_created; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_messages_conversation_created ON public.messages USING btree (conversation_id, created_at DESC);


--
-- Name: idx_movimientos_bitacora_pendientes; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_movimientos_bitacora_pendientes ON public.movimientos USING btree (created_at DESC, id_movimiento DESC) WHERE ((bitacora_verificado_at IS NULL) AND (created_at IS NOT NULL));


--
-- Name: idx_movimientos_created_at_desc; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_movimientos_created_at_desc ON public.movimientos USING btree (created_at DESC) WHERE (created_at IS NOT NULL);


--
-- Name: idx_obligaciones_pago_abiertas_vencimiento; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_obligaciones_pago_abiertas_vencimiento ON public.obligaciones_pago USING btree (fecha_vencimiento) WHERE (estado_pago = ANY (ARRAY['PENDIENTE'::text, 'PAGO_PARCIAL'::text]));


--
-- Name: idx_obligaciones_pago_activas; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_obligaciones_pago_activas ON public.obligaciones_pago USING btree (periodo_operativo DESC, fecha_vencimiento) WHERE (archivada_at IS NULL);


--
-- Name: idx_obligaciones_pago_beneficiary_identity; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_obligaciones_pago_beneficiary_identity ON public.obligaciones_pago USING btree (beneficiary_identity_id);


--
-- Name: idx_obligaciones_pago_entity_identity; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_obligaciones_pago_entity_identity ON public.obligaciones_pago USING btree (entity_identity_id);


--
-- Name: idx_obligaciones_pago_periodo; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_obligaciones_pago_periodo ON public.obligaciones_pago USING btree (periodo_operativo DESC);


--
-- Name: idx_obligaciones_pago_tipo_estado; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_obligaciones_pago_tipo_estado ON public.obligaciones_pago USING btree (tipo_obligacion, estado_pago);


--
-- Name: idx_obligaciones_pago_tipo_id_estado; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_obligaciones_pago_tipo_id_estado ON public.obligaciones_pago USING btree (id_tipo_obligacion, estado_pago);


--
-- Name: idx_obligaciones_pagos_obligacion_fecha; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_obligaciones_pagos_obligacion_fecha ON public.obligaciones_pagos USING btree (id_obligacion, fecha_pago DESC, id_pago DESC);


--
-- Name: idx_operational_identities_type_name; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_operational_identities_type_name ON public.operational_identities USING btree (identity_type, normalized_name);


--
-- Name: idx_ordenes_compra_detalle_material; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_ordenes_compra_detalle_material ON public.ordenes_compra_detalle USING btree (id_material);


--
-- Name: idx_ordenes_compra_detalle_orden; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_ordenes_compra_detalle_orden ON public.ordenes_compra_detalle USING btree (id_orden_compra);


--
-- Name: idx_ordenes_compra_estado; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_ordenes_compra_estado ON public.ordenes_compra USING btree (estado);


--
-- Name: idx_ordenes_compra_fecha; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_ordenes_compra_fecha ON public.ordenes_compra USING btree (fecha DESC);


--
-- Name: idx_reporte_stock_materiales_orden; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_reporte_stock_materiales_orden ON public.reporte_stock_materiales USING btree (id_seccion, orden);


--
-- Name: idx_reporte_stock_secciones_orden; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_reporte_stock_secciones_orden ON public.reporte_stock_secciones USING btree (orden);


--
-- Name: idx_sueldo_condiciones_vigencia; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_sueldo_condiciones_vigencia ON public.sueldo_vinculo_condiciones USING btree (id_vinculo, vigencia_desde DESC, vigencia_hasta);


--
-- Name: idx_sueldo_distribuciones_obra; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_sueldo_distribuciones_obra ON public.liquidacion_sueldo_distribuciones USING btree (id_obra, id_liquidacion);


--
-- Name: idx_sueldo_presupuesto_obra_periodo; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_sueldo_presupuesto_obra_periodo ON public.sueldo_presupuestos_obra USING btree (id_obra, periodo DESC, modalidad);


--
-- Name: idx_sueldo_vinculos_modalidad; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_sueldo_vinculos_modalidad ON public.sueldo_vinculos USING btree (modalidad, fecha_baja);


--
-- Name: idx_sueldo_vinculos_persona_modalidad; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_sueldo_vinculos_persona_modalidad ON public.sueldo_vinculos USING btree (id_persona, modalidad, fecha_baja);


--
-- Name: idx_sueldos_auditoria_entity_scope; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_sueldos_auditoria_entity_scope ON public.sueldos_auditoria USING btree (modalidad_scope, entidad_tipo, entidad_id);


--
-- Name: idx_sueldos_auditoria_scope_fecha; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_sueldos_auditoria_scope_fecha ON public.sueldos_auditoria USING btree (modalidad_scope, created_at DESC);


--
-- Name: idx_sueldos_pagos_liquidacion; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_sueldos_pagos_liquidacion ON public.sueldos_pagos USING btree (id_liquidacion, estado, fecha DESC);


--
-- Name: idx_usage_events_org_created; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_usage_events_org_created ON public.usage_events USING btree (organization_id, created_at DESC);


--
-- Name: uq_conceptos_sueldo_scope_codigo; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX uq_conceptos_sueldo_scope_codigo ON public.conceptos_sueldo USING btree (COALESCE(modalidad_scope, 'GLOBAL'::text), codigo);


--
-- Name: uq_invoice_categories_normalized; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX uq_invoice_categories_normalized ON public.invoice_categories USING btree (normalized_name);


--
-- Name: uq_liquidaciones_sueldo_idempotencia; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX uq_liquidaciones_sueldo_idempotencia ON public.liquidaciones_sueldo USING btree (created_by, idempotency_key) WHERE (idempotency_key IS NOT NULL);


--
-- Name: uq_obligaciones_pago_external_id; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX uq_obligaciones_pago_external_id ON public.obligaciones_pago USING btree (external_id) WHERE (external_id IS NOT NULL);


--
-- Name: uq_ordenes_compra_nro; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX uq_ordenes_compra_nro ON public.ordenes_compra USING btree (nro_orden);


--
-- Name: uq_sueldo_mensual_vinculo_periodo; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX uq_sueldo_mensual_vinculo_periodo ON public.liquidaciones_sueldo USING btree (id_vinculo, periodo) WHERE ((tipo = 'MENSUAL'::text) AND (estado <> 'ANULADA'::text));


--
-- Name: uq_sueldo_vinculo_activo_modalidad; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX uq_sueldo_vinculo_activo_modalidad ON public.sueldo_vinculos USING btree (id_persona, modalidad) WHERE (fecha_baja IS NULL);


--
-- Name: uq_sueldos_pagos_idempotencia; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX uq_sueldos_pagos_idempotencia ON public.sueldos_pagos USING btree (created_by, idempotency_key) WHERE (idempotency_key IS NOT NULL);


--
-- Name: uq_visibilidad_modulos_rol_modulo_key; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX uq_visibilidad_modulos_rol_modulo_key ON public.visibilidad_modulos USING btree (rol, modulo_key);


--
-- Name: factura_pagos invoice_event_payment_consistency; Type: TRIGGER; Schema: public; Owner: postgres
--

CREATE CONSTRAINT TRIGGER invoice_event_payment_consistency AFTER INSERT OR DELETE OR UPDATE ON public.factura_pagos DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION public.check_invoice_payment_consistency();


--
-- Name: facturas invoice_header_payment_consistency; Type: TRIGGER; Schema: public; Owner: postgres
--

CREATE CONSTRAINT TRIGGER invoice_header_payment_consistency AFTER INSERT OR UPDATE OF monto_pagado, estado_pago, total, tipo_comprobante ON public.facturas DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION public.check_invoice_payment_consistency();


--
-- Name: facturas invoice_identity_guard; Type: TRIGGER; Schema: public; Owner: postgres
--

CREATE TRIGGER invoice_identity_guard BEFORE INSERT OR UPDATE OF tipo_operacion, tipo_comprobante, cuit, denominacion, numero_factura, estado_pago ON public.facturas FOR EACH ROW EXECUTE FUNCTION public.guard_invoice_identity();


--
-- Name: factura_pagos invoice_payment_guard; Type: TRIGGER; Schema: public; Owner: postgres
--

CREATE TRIGGER invoice_payment_guard BEFORE INSERT OR DELETE OR UPDATE ON public.factura_pagos FOR EACH ROW EXECUTE FUNCTION public.guard_invoice_payment();


--
-- Name: facturas invoice_payment_history_guard; Type: TRIGGER; Schema: public; Owner: postgres
--

CREATE TRIGGER invoice_payment_history_guard BEFORE DELETE OR UPDATE ON public.facturas FOR EACH ROW EXECUTE FUNCTION public.guard_invoice_payment_history();


--
-- Name: obligaciones_pagos obligation_event_payment_consistency; Type: TRIGGER; Schema: public; Owner: postgres
--

CREATE CONSTRAINT TRIGGER obligation_event_payment_consistency AFTER INSERT ON public.obligaciones_pagos DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION public.check_obligation_payment_consistency();


--
-- Name: obligaciones_pago obligation_header_payment_consistency; Type: TRIGGER; Schema: public; Owner: postgres
--

CREATE CONSTRAINT TRIGGER obligation_header_payment_consistency AFTER INSERT OR UPDATE ON public.obligaciones_pago DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION public.check_obligation_payment_consistency();


--
-- Name: obligaciones_pagos obligation_payment_guard; Type: TRIGGER; Schema: public; Owner: postgres
--

CREATE TRIGGER obligation_payment_guard BEFORE INSERT OR DELETE OR UPDATE ON public.obligaciones_pagos FOR EACH ROW EXECUTE FUNCTION public.guard_obligation_payment();


--
-- Name: obligaciones_pago obligation_payment_history_guard; Type: TRIGGER; Schema: public; Owner: postgres
--

CREATE TRIGGER obligation_payment_history_guard BEFORE INSERT OR DELETE OR UPDATE ON public.obligaciones_pago FOR EACH ROW EXECUTE FUNCTION public.guard_obligation_payment_history();


--
-- Name: liquidacion_sueldo_distribuciones payroll_distribution_lines_guard; Type: TRIGGER; Schema: public; Owner: postgres
--

CREATE CONSTRAINT TRIGGER payroll_distribution_lines_guard AFTER INSERT OR DELETE OR UPDATE ON public.liquidacion_sueldo_distribuciones DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION public.check_payroll_distribution();


--
-- Name: liquidaciones_sueldo payroll_distribution_total_guard; Type: TRIGGER; Schema: public; Owner: postgres
--

CREATE CONSTRAINT TRIGGER payroll_distribution_total_guard AFTER INSERT OR UPDATE OF neto, distribucion_modo ON public.liquidaciones_sueldo DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION public.check_payroll_distribution();


--
-- Name: sueldos_pagos payroll_payment_guard; Type: TRIGGER; Schema: public; Owner: postgres
--

CREATE TRIGGER payroll_payment_guard BEFORE INSERT ON public.sueldos_pagos FOR EACH ROW EXECUTE FUNCTION public.guard_payroll_payment();


--
-- Name: obligaciones_pago sync_obligation_type_reference; Type: TRIGGER; Schema: public; Owner: postgres
--

CREATE TRIGGER sync_obligation_type_reference BEFORE INSERT OR UPDATE OF id_tipo_obligacion, tipo_obligacion ON public.obligaciones_pago FOR EACH ROW EXECUTE FUNCTION public.sync_obligation_type_reference();


--
-- Name: movimientos trg_movimientos_timestamps; Type: TRIGGER; Schema: public; Owner: postgres
--

CREATE TRIGGER trg_movimientos_timestamps BEFORE INSERT OR UPDATE ON public.movimientos FOR EACH ROW EXECUTE FUNCTION public.set_movimientos_timestamps();


--
-- Name: ordenes_compra_faltantes trg_ordenes_compra_faltantes_updated_at; Type: TRIGGER; Schema: public; Owner: postgres
--

CREATE TRIGGER trg_ordenes_compra_faltantes_updated_at BEFORE UPDATE ON public.ordenes_compra_faltantes FOR EACH ROW EXECUTE FUNCTION public.set_ordenes_compra_updated_at();


--
-- Name: ordenes_compra trg_ordenes_compra_updated_at; Type: TRIGGER; Schema: public; Owner: postgres
--

CREATE TRIGGER trg_ordenes_compra_updated_at BEFORE UPDATE ON public.ordenes_compra FOR EACH ROW EXECUTE FUNCTION public.set_ordenes_compra_updated_at();


--
-- Name: visibilidad_modulos trg_sync_visibilidad_modulos_columns; Type: TRIGGER; Schema: public; Owner: postgres
--

CREATE TRIGGER trg_sync_visibilidad_modulos_columns BEFORE INSERT OR UPDATE ON public.visibilidad_modulos FOR EACH ROW EXECUTE FUNCTION public.sync_visibilidad_modulos_columns();


--
-- Name: assistant_configs assistant_configs_organization_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.assistant_configs
    ADD CONSTRAINT assistant_configs_organization_id_fkey FOREIGN KEY (organization_id) REFERENCES public.organizations(id) ON DELETE CASCADE;


--
-- Name: audit_logs audit_logs_actor_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.audit_logs
    ADD CONSTRAINT audit_logs_actor_user_id_fkey FOREIGN KEY (actor_user_id) REFERENCES public.users(id) ON DELETE SET NULL;


--
-- Name: audit_logs audit_logs_organization_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.audit_logs
    ADD CONSTRAINT audit_logs_organization_id_fkey FOREIGN KEY (organization_id) REFERENCES public.organizations(id) ON DELETE CASCADE;


--
-- Name: business_memories business_memories_organization_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.business_memories
    ADD CONSTRAINT business_memories_organization_id_fkey FOREIGN KEY (organization_id) REFERENCES public.organizations(id) ON DELETE CASCADE;


--
-- Name: canaletas_plegados_fotos canaletas_plegados_fotos_canaleta_plegado_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.canaletas_plegados_fotos
    ADD CONSTRAINT canaletas_plegados_fotos_canaleta_plegado_id_fkey FOREIGN KEY (canaleta_plegado_id) REFERENCES public.canaletas_plegados(id) ON DELETE CASCADE;


--
-- Name: canaletas_plegados canaletas_plegados_id_obra_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.canaletas_plegados
    ADD CONSTRAINT canaletas_plegados_id_obra_fkey FOREIGN KEY (id_obra) REFERENCES public.obras(id) ON DELETE RESTRICT;


--
-- Name: conteos_inventario_detalle conteos_inventario_detalle_id_conteo_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.conteos_inventario_detalle
    ADD CONSTRAINT conteos_inventario_detalle_id_conteo_fkey FOREIGN KEY (id_conteo) REFERENCES public.conteos_inventario(id_conteo) ON DELETE CASCADE;


--
-- Name: conteos_inventario_detalle conteos_inventario_detalle_id_material_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.conteos_inventario_detalle
    ADD CONSTRAINT conteos_inventario_detalle_id_material_fkey FOREIGN KEY (id_material) REFERENCES public.materiales(id);


--
-- Name: conversation_summaries conversation_summaries_conversation_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.conversation_summaries
    ADD CONSTRAINT conversation_summaries_conversation_id_fkey FOREIGN KEY (conversation_id) REFERENCES public.conversations(id) ON DELETE CASCADE;


--
-- Name: conversation_summaries conversation_summaries_organization_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.conversation_summaries
    ADD CONSTRAINT conversation_summaries_organization_id_fkey FOREIGN KEY (organization_id) REFERENCES public.organizations(id) ON DELETE CASCADE;


--
-- Name: conversations conversations_created_by_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.conversations
    ADD CONSTRAINT conversations_created_by_user_id_fkey FOREIGN KEY (created_by_user_id) REFERENCES public.users(id);


--
-- Name: conversations conversations_organization_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.conversations
    ADD CONSTRAINT conversations_organization_id_fkey FOREIGN KEY (organization_id) REFERENCES public.organizations(id) ON DELETE CASCADE;


--
-- Name: document_chunks document_chunks_document_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.document_chunks
    ADD CONSTRAINT document_chunks_document_id_fkey FOREIGN KEY (document_id) REFERENCES public.documents(id) ON DELETE CASCADE;


--
-- Name: document_chunks document_chunks_organization_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.document_chunks
    ADD CONSTRAINT document_chunks_organization_id_fkey FOREIGN KEY (organization_id) REFERENCES public.organizations(id) ON DELETE CASCADE;


--
-- Name: documents documents_organization_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.documents
    ADD CONSTRAINT documents_organization_id_fkey FOREIGN KEY (organization_id) REFERENCES public.organizations(id) ON DELETE CASCADE;


--
-- Name: documents documents_uploaded_by_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.documents
    ADD CONSTRAINT documents_uploaded_by_user_id_fkey FOREIGN KEY (uploaded_by_user_id) REFERENCES public.users(id);


--
-- Name: factura_pagos factura_pagos_id_factura_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.factura_pagos
    ADD CONSTRAINT factura_pagos_id_factura_fkey FOREIGN KEY (id_factura) REFERENCES public.facturas(id_factura) ON DELETE RESTRICT;


--
-- Name: facturas facturas_category_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.facturas
    ADD CONSTRAINT facturas_category_id_fkey FOREIGN KEY (category_id) REFERENCES public.invoice_categories(id) ON DELETE RESTRICT;


--
-- Name: materiales fk_categoria; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.materiales
    ADD CONSTRAINT fk_categoria FOREIGN KEY (id_categoria) REFERENCES public.categorias(id_categoria);


--
-- Name: herramienta_movimientos herramienta_movimientos_id_herramienta_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.herramienta_movimientos
    ADD CONSTRAINT herramienta_movimientos_id_herramienta_fkey FOREIGN KEY (id_herramienta) REFERENCES public.herramientas(id_herramienta) ON DELETE SET NULL;


--
-- Name: herramientas herramientas_id_obra_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.herramientas
    ADD CONSTRAINT herramientas_id_obra_fkey FOREIGN KEY (id_obra) REFERENCES public.obras(id);


--
-- Name: herramientas herramientas_id_ubicacion_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.herramientas
    ADD CONSTRAINT herramientas_id_ubicacion_fkey FOREIGN KEY (id_ubicacion) REFERENCES public.ubicacion(id_ubicacion) ON DELETE SET NULL;


--
-- Name: ingestion_jobs ingestion_jobs_document_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.ingestion_jobs
    ADD CONSTRAINT ingestion_jobs_document_id_fkey FOREIGN KEY (document_id) REFERENCES public.documents(id) ON DELETE CASCADE;


--
-- Name: ingestion_jobs ingestion_jobs_organization_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.ingestion_jobs
    ADD CONSTRAINT ingestion_jobs_organization_id_fkey FOREIGN KEY (organization_id) REFERENCES public.organizations(id) ON DELETE CASCADE;


--
-- Name: insumo_movimientos insumo_movimientos_id_insumo_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.insumo_movimientos
    ADD CONSTRAINT insumo_movimientos_id_insumo_fkey FOREIGN KEY (id_insumo) REFERENCES public.insumos(id_insumo) ON DELETE SET NULL;


--
-- Name: insumo_movimientos insumo_movimientos_id_movimiento_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.insumo_movimientos
    ADD CONSTRAINT insumo_movimientos_id_movimiento_fkey FOREIGN KEY (id_movimiento) REFERENCES public.movimientos(id_movimiento) ON DELETE SET NULL;


--
-- Name: insumos insumos_id_obra_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.insumos
    ADD CONSTRAINT insumos_id_obra_fkey FOREIGN KEY (id_obra) REFERENCES public.obras(id);


--
-- Name: insumos insumos_id_ubicacion_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.insumos
    ADD CONSTRAINT insumos_id_ubicacion_fkey FOREIGN KEY (id_ubicacion) REFERENCES public.ubicacion(id_ubicacion) ON DELETE SET NULL;


--
-- Name: liquidacion_sueldo_conceptos liquidacion_sueldo_conceptos_id_concepto_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.liquidacion_sueldo_conceptos
    ADD CONSTRAINT liquidacion_sueldo_conceptos_id_concepto_fkey FOREIGN KEY (id_concepto) REFERENCES public.conceptos_sueldo(id) ON DELETE RESTRICT;


--
-- Name: liquidacion_sueldo_conceptos liquidacion_sueldo_conceptos_id_liquidacion_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.liquidacion_sueldo_conceptos
    ADD CONSTRAINT liquidacion_sueldo_conceptos_id_liquidacion_fkey FOREIGN KEY (id_liquidacion) REFERENCES public.liquidaciones_sueldo(id) ON DELETE RESTRICT;


--
-- Name: liquidacion_sueldo_distribuciones liquidacion_sueldo_distribuciones_id_liquidacion_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.liquidacion_sueldo_distribuciones
    ADD CONSTRAINT liquidacion_sueldo_distribuciones_id_liquidacion_fkey FOREIGN KEY (id_liquidacion) REFERENCES public.liquidaciones_sueldo(id) ON DELETE RESTRICT;


--
-- Name: liquidacion_sueldo_distribuciones liquidacion_sueldo_distribuciones_id_obra_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.liquidacion_sueldo_distribuciones
    ADD CONSTRAINT liquidacion_sueldo_distribuciones_id_obra_fkey FOREIGN KEY (id_obra) REFERENCES public.obras(id) ON DELETE RESTRICT;


--
-- Name: liquidaciones_sueldo liquidaciones_sueldo_id_condicion_vinculo_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.liquidaciones_sueldo
    ADD CONSTRAINT liquidaciones_sueldo_id_condicion_vinculo_fkey FOREIGN KEY (id_condicion_vinculo) REFERENCES public.sueldo_vinculo_condiciones(id) ON DELETE RESTRICT;


--
-- Name: liquidaciones_sueldo liquidaciones_sueldo_id_persona_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.liquidaciones_sueldo
    ADD CONSTRAINT liquidaciones_sueldo_id_persona_fkey FOREIGN KEY (id_persona) REFERENCES public.sueldo_personas(id) ON DELETE RESTRICT;


--
-- Name: liquidaciones_sueldo liquidaciones_sueldo_id_vinculo_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.liquidaciones_sueldo
    ADD CONSTRAINT liquidaciones_sueldo_id_vinculo_fkey FOREIGN KEY (id_vinculo) REFERENCES public.sueldo_vinculos(id) ON DELETE RESTRICT;


--
-- Name: materiales materiales_id_ubicacion_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.materiales
    ADD CONSTRAINT materiales_id_ubicacion_fkey FOREIGN KEY (id_ubicacion) REFERENCES public.ubicacion(id_ubicacion) ON DELETE SET NULL;


--
-- Name: materiales_proveedores materiales_proveedores_id_material_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.materiales_proveedores
    ADD CONSTRAINT materiales_proveedores_id_material_fkey FOREIGN KEY (id_material) REFERENCES public.materiales(id) ON DELETE CASCADE;


--
-- Name: materiales_proveedores materiales_proveedores_id_proveedor_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.materiales_proveedores
    ADD CONSTRAINT materiales_proveedores_id_proveedor_fkey FOREIGN KEY (id_proveedor) REFERENCES public.proveedores(id_proveedores) ON DELETE CASCADE;


--
-- Name: movimientomaterial materialmm; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.movimientomaterial
    ADD CONSTRAINT materialmm FOREIGN KEY (id_material) REFERENCES public.materiales(id);


--
-- Name: messages messages_conversation_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.messages
    ADD CONSTRAINT messages_conversation_id_fkey FOREIGN KEY (conversation_id) REFERENCES public.conversations(id) ON DELETE CASCADE;


--
-- Name: messages messages_organization_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.messages
    ADD CONSTRAINT messages_organization_id_fkey FOREIGN KEY (organization_id) REFERENCES public.organizations(id) ON DELETE CASCADE;


--
-- Name: movimientomaterial movimientomaterial_id_recorte_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.movimientomaterial
    ADD CONSTRAINT movimientomaterial_id_recorte_fkey FOREIGN KEY (id_recorte) REFERENCES public.recortes(id) ON DELETE SET NULL;


--
-- Name: movimientomaterial movimientomm; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.movimientomaterial
    ADD CONSTRAINT movimientomm FOREIGN KEY (id_movimiento) REFERENCES public.movimientos(id_movimiento) ON DELETE CASCADE;


--
-- Name: movimientos movimientos_id_flete_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.movimientos
    ADD CONSTRAINT movimientos_id_flete_fkey FOREIGN KEY (id_flete) REFERENCES public.fletes(id_flete) ON DELETE SET NULL;


--
-- Name: obligaciones_pago obligaciones_pago_beneficiary_identity_fk; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.obligaciones_pago
    ADD CONSTRAINT obligaciones_pago_beneficiary_identity_fk FOREIGN KEY (beneficiary_identity_id) REFERENCES public.operational_identities(id) ON DELETE RESTRICT;


--
-- Name: obligaciones_pago obligaciones_pago_entity_identity_fk; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.obligaciones_pago
    ADD CONSTRAINT obligaciones_pago_entity_identity_fk FOREIGN KEY (entity_identity_id) REFERENCES public.operational_identities(id) ON DELETE RESTRICT;


--
-- Name: obligaciones_pago obligaciones_pago_tipo_obligacion_fk; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.obligaciones_pago
    ADD CONSTRAINT obligaciones_pago_tipo_obligacion_fk FOREIGN KEY (id_tipo_obligacion) REFERENCES public.tipos_obligacion(id) ON UPDATE RESTRICT ON DELETE RESTRICT;


--
-- Name: obligaciones_pagos obligaciones_pagos_id_obligacion_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.obligaciones_pagos
    ADD CONSTRAINT obligaciones_pagos_id_obligacion_fkey FOREIGN KEY (id_obligacion) REFERENCES public.obligaciones_pago(id_obligacion) ON DELETE RESTRICT;


--
-- Name: movimientos obramovimiento; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.movimientos
    ADD CONSTRAINT obramovimiento FOREIGN KEY (id_obra) REFERENCES public.obras(id);


--
-- Name: ordenes_compra_detalle ordenes_compra_detalle_id_material_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.ordenes_compra_detalle
    ADD CONSTRAINT ordenes_compra_detalle_id_material_fkey FOREIGN KEY (id_material) REFERENCES public.materiales(id);


--
-- Name: ordenes_compra_detalle ordenes_compra_detalle_id_orden_compra_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.ordenes_compra_detalle
    ADD CONSTRAINT ordenes_compra_detalle_id_orden_compra_fkey FOREIGN KEY (id_orden_compra) REFERENCES public.ordenes_compra(id_orden_compra) ON DELETE CASCADE;


--
-- Name: ordenes_compra_faltantes ordenes_compra_faltantes_id_detalle_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.ordenes_compra_faltantes
    ADD CONSTRAINT ordenes_compra_faltantes_id_detalle_fkey FOREIGN KEY (id_detalle) REFERENCES public.ordenes_compra_detalle(id_detalle) ON DELETE CASCADE;


--
-- Name: ordenes_compra ordenes_compra_id_destino_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.ordenes_compra
    ADD CONSTRAINT ordenes_compra_id_destino_fkey FOREIGN KEY (id_destino) REFERENCES public.ubicacion(id_ubicacion);


--
-- Name: ordenes_compra ordenes_compra_id_proveedor_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.ordenes_compra
    ADD CONSTRAINT ordenes_compra_id_proveedor_fkey FOREIGN KEY (id_proveedor) REFERENCES public.proveedores(id_proveedores);


--
-- Name: organization_members organization_members_organization_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.organization_members
    ADD CONSTRAINT organization_members_organization_id_fkey FOREIGN KEY (organization_id) REFERENCES public.organizations(id) ON DELETE CASCADE;


--
-- Name: organization_members organization_members_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.organization_members
    ADD CONSTRAINT organization_members_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.users(id) ON DELETE CASCADE;


--
-- Name: recortes recortes_id_material_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.recortes
    ADD CONSTRAINT recortes_id_material_fkey FOREIGN KEY (id_material) REFERENCES public.materiales(id) ON DELETE CASCADE;


--
-- Name: reporte_stock_materiales reporte_stock_materiales_id_material_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.reporte_stock_materiales
    ADD CONSTRAINT reporte_stock_materiales_id_material_fkey FOREIGN KEY (id_material) REFERENCES public.materiales(id) ON DELETE CASCADE;


--
-- Name: reporte_stock_materiales reporte_stock_materiales_id_seccion_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.reporte_stock_materiales
    ADD CONSTRAINT reporte_stock_materiales_id_seccion_fkey FOREIGN KEY (id_seccion) REFERENCES public.reporte_stock_secciones(id) ON DELETE CASCADE;


--
-- Name: sueldo_pago_distribuciones sueldo_pago_distribuciones_id_obra_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.sueldo_pago_distribuciones
    ADD CONSTRAINT sueldo_pago_distribuciones_id_obra_fkey FOREIGN KEY (id_obra) REFERENCES public.obras(id) ON DELETE RESTRICT;


--
-- Name: sueldo_pago_distribuciones sueldo_pago_distribuciones_id_pago_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.sueldo_pago_distribuciones
    ADD CONSTRAINT sueldo_pago_distribuciones_id_pago_fkey FOREIGN KEY (id_pago) REFERENCES public.sueldos_pagos(id) ON DELETE RESTRICT;


--
-- Name: sueldo_presupuestos_obra sueldo_presupuestos_obra_id_obra_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.sueldo_presupuestos_obra
    ADD CONSTRAINT sueldo_presupuestos_obra_id_obra_fkey FOREIGN KEY (id_obra) REFERENCES public.obras(id) ON DELETE RESTRICT;


--
-- Name: sueldo_vinculo_condiciones sueldo_vinculo_condiciones_id_vinculo_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.sueldo_vinculo_condiciones
    ADD CONSTRAINT sueldo_vinculo_condiciones_id_vinculo_fkey FOREIGN KEY (id_vinculo) REFERENCES public.sueldo_vinculos(id) ON DELETE RESTRICT;


--
-- Name: sueldo_vinculo_distribuciones sueldo_vinculo_distribuciones_id_obra_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.sueldo_vinculo_distribuciones
    ADD CONSTRAINT sueldo_vinculo_distribuciones_id_obra_fkey FOREIGN KEY (id_obra) REFERENCES public.obras(id) ON DELETE RESTRICT;


--
-- Name: sueldo_vinculo_distribuciones sueldo_vinculo_distribuciones_id_vinculo_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.sueldo_vinculo_distribuciones
    ADD CONSTRAINT sueldo_vinculo_distribuciones_id_vinculo_fkey FOREIGN KEY (id_vinculo) REFERENCES public.sueldo_vinculos(id) ON DELETE RESTRICT;


--
-- Name: sueldo_vinculos sueldo_vinculos_id_persona_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.sueldo_vinculos
    ADD CONSTRAINT sueldo_vinculos_id_persona_fkey FOREIGN KEY (id_persona) REFERENCES public.sueldo_personas(id) ON DELETE RESTRICT;


--
-- Name: sueldos_pagos sueldos_pagos_id_liquidacion_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.sueldos_pagos
    ADD CONSTRAINT sueldos_pagos_id_liquidacion_fkey FOREIGN KEY (id_liquidacion) REFERENCES public.liquidaciones_sueldo(id) ON DELETE RESTRICT;


--
-- Name: tasks tasks_created_by_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.tasks
    ADD CONSTRAINT tasks_created_by_user_id_fkey FOREIGN KEY (created_by_user_id) REFERENCES public.users(id) ON DELETE SET NULL;


--
-- Name: tasks tasks_organization_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.tasks
    ADD CONSTRAINT tasks_organization_id_fkey FOREIGN KEY (organization_id) REFERENCES public.organizations(id) ON DELETE CASCADE;


--
-- Name: usage_events usage_events_conversation_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.usage_events
    ADD CONSTRAINT usage_events_conversation_id_fkey FOREIGN KEY (conversation_id) REFERENCES public.conversations(id) ON DELETE SET NULL;


--
-- Name: usage_events usage_events_organization_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.usage_events
    ADD CONSTRAINT usage_events_organization_id_fkey FOREIGN KEY (organization_id) REFERENCES public.organizations(id) ON DELETE CASCADE;


--
-- Name: usage_events usage_events_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.usage_events
    ADD CONSTRAINT usage_events_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.users(id) ON DELETE SET NULL;


--
-- Name: categorias Solo usuarios autenticados pueden ver categorias; Type: POLICY; Schema: public; Owner: postgres
--

CREATE POLICY "Solo usuarios autenticados pueden ver categorias" ON public.categorias FOR SELECT TO authenticated USING (true);


--
-- Name: fletes Solo usuarios autenticados pueden ver fletes; Type: POLICY; Schema: public; Owner: postgres
--

CREATE POLICY "Solo usuarios autenticados pueden ver fletes" ON public.fletes FOR SELECT TO authenticated USING (true);


--
-- Name: materiales Solo usuarios autenticados pueden ver materiales; Type: POLICY; Schema: public; Owner: postgres
--

CREATE POLICY "Solo usuarios autenticados pueden ver materiales" ON public.materiales FOR SELECT TO authenticated USING (true);


--
-- Name: obras Solo usuarios autenticados pueden ver obras; Type: POLICY; Schema: public; Owner: postgres
--

CREATE POLICY "Solo usuarios autenticados pueden ver obras" ON public.obras FOR SELECT TO authenticated USING (true);


--
-- Name: proveedores Solo usuarios autenticados pueden ver proveedores; Type: POLICY; Schema: public; Owner: postgres
--

CREATE POLICY "Solo usuarios autenticados pueden ver proveedores" ON public.proveedores FOR SELECT TO authenticated USING (true);


--
-- Name: _migracion_origen_destino_map; Type: ROW SECURITY; Schema: public; Owner: postgres
--

ALTER TABLE public._migracion_origen_destino_map ENABLE ROW LEVEL SECURITY;

--
-- Name: assistant_configs; Type: ROW SECURITY; Schema: public; Owner: postgres
--

ALTER TABLE public.assistant_configs ENABLE ROW LEVEL SECURITY;

--
-- Name: audit_logs; Type: ROW SECURITY; Schema: public; Owner: postgres
--

ALTER TABLE public.audit_logs ENABLE ROW LEVEL SECURITY;

--
-- Name: authorization_policy_state; Type: ROW SECURITY; Schema: public; Owner: postgres
--

ALTER TABLE public.authorization_policy_state ENABLE ROW LEVEL SECURITY;

--
-- Name: backup_info_20260325; Type: ROW SECURITY; Schema: public; Owner: postgres
--

ALTER TABLE public.backup_info_20260325 ENABLE ROW LEVEL SECURITY;

--
-- Name: backup_movimientomaterial_20260325; Type: ROW SECURITY; Schema: public; Owner: postgres
--

ALTER TABLE public.backup_movimientomaterial_20260325 ENABLE ROW LEVEL SECURITY;

--
-- Name: backup_movimientos_20260325; Type: ROW SECURITY; Schema: public; Owner: postgres
--

ALTER TABLE public.backup_movimientos_20260325 ENABLE ROW LEVEL SECURITY;

--
-- Name: backup_recortes_20260325; Type: ROW SECURITY; Schema: public; Owner: postgres
--

ALTER TABLE public.backup_recortes_20260325 ENABLE ROW LEVEL SECURITY;

--
-- Name: bitacora; Type: ROW SECURITY; Schema: public; Owner: postgres
--

ALTER TABLE public.bitacora ENABLE ROW LEVEL SECURITY;

--
-- Name: business_memories; Type: ROW SECURITY; Schema: public; Owner: postgres
--

ALTER TABLE public.business_memories ENABLE ROW LEVEL SECURITY;

--
-- Name: canaletas_plegados; Type: ROW SECURITY; Schema: public; Owner: postgres
--

ALTER TABLE public.canaletas_plegados ENABLE ROW LEVEL SECURITY;

--
-- Name: canaletas_plegados_fotos; Type: ROW SECURITY; Schema: public; Owner: postgres
--

ALTER TABLE public.canaletas_plegados_fotos ENABLE ROW LEVEL SECURITY;

--
-- Name: canaletas_plegados_fotos canaletas_plegados_fotos_delete_authenticated; Type: POLICY; Schema: public; Owner: postgres
--

CREATE POLICY canaletas_plegados_fotos_delete_authenticated ON public.canaletas_plegados_fotos FOR DELETE TO authenticated USING ((bucket_id = 'canaletas-fotos'::text));


--
-- Name: canaletas_plegados_fotos canaletas_plegados_fotos_insert_authenticated; Type: POLICY; Schema: public; Owner: postgres
--

CREATE POLICY canaletas_plegados_fotos_insert_authenticated ON public.canaletas_plegados_fotos FOR INSERT TO authenticated WITH CHECK ((bucket_id = 'canaletas-fotos'::text));


--
-- Name: canaletas_plegados_fotos canaletas_plegados_fotos_select_authenticated; Type: POLICY; Schema: public; Owner: postgres
--

CREATE POLICY canaletas_plegados_fotos_select_authenticated ON public.canaletas_plegados_fotos FOR SELECT TO authenticated USING ((bucket_id = 'canaletas-fotos'::text));


--
-- Name: canaletas_plegados_fotos canaletas_plegados_fotos_update_authenticated; Type: POLICY; Schema: public; Owner: postgres
--

CREATE POLICY canaletas_plegados_fotos_update_authenticated ON public.canaletas_plegados_fotos FOR UPDATE TO authenticated USING ((bucket_id = 'canaletas-fotos'::text)) WITH CHECK ((bucket_id = 'canaletas-fotos'::text));


--
-- Name: categorias; Type: ROW SECURITY; Schema: public; Owner: postgres
--

ALTER TABLE public.categorias ENABLE ROW LEVEL SECURITY;

--
-- Name: conceptos_sueldo; Type: ROW SECURITY; Schema: public; Owner: postgres
--

ALTER TABLE public.conceptos_sueldo ENABLE ROW LEVEL SECURITY;

--
-- Name: conteos_inventario; Type: ROW SECURITY; Schema: public; Owner: postgres
--

ALTER TABLE public.conteos_inventario ENABLE ROW LEVEL SECURITY;

--
-- Name: conteos_inventario_detalle; Type: ROW SECURITY; Schema: public; Owner: postgres
--

ALTER TABLE public.conteos_inventario_detalle ENABLE ROW LEVEL SECURITY;

--
-- Name: conversation_summaries; Type: ROW SECURITY; Schema: public; Owner: postgres
--

ALTER TABLE public.conversation_summaries ENABLE ROW LEVEL SECURITY;

--
-- Name: conversations; Type: ROW SECURITY; Schema: public; Owner: postgres
--

ALTER TABLE public.conversations ENABLE ROW LEVEL SECURITY;

--
-- Name: document_chunks; Type: ROW SECURITY; Schema: public; Owner: postgres
--

ALTER TABLE public.document_chunks ENABLE ROW LEVEL SECURITY;

--
-- Name: documents; Type: ROW SECURITY; Schema: public; Owner: postgres
--

ALTER TABLE public.documents ENABLE ROW LEVEL SECURITY;

--
-- Name: factura_pagos; Type: ROW SECURITY; Schema: public; Owner: postgres
--

ALTER TABLE public.factura_pagos ENABLE ROW LEVEL SECURITY;

--
-- Name: facturas; Type: ROW SECURITY; Schema: public; Owner: postgres
--

ALTER TABLE public.facturas ENABLE ROW LEVEL SECURITY;

--
-- Name: financial_operation_requests; Type: ROW SECURITY; Schema: public; Owner: postgres
--

ALTER TABLE public.financial_operation_requests ENABLE ROW LEVEL SECURITY;

--
-- Name: fiscal_periods; Type: ROW SECURITY; Schema: public; Owner: postgres
--

ALTER TABLE public.fiscal_periods ENABLE ROW LEVEL SECURITY;

--
-- Name: fiscal_thresholds; Type: ROW SECURITY; Schema: public; Owner: postgres
--

ALTER TABLE public.fiscal_thresholds ENABLE ROW LEVEL SECURITY;

--
-- Name: fletes; Type: ROW SECURITY; Schema: public; Owner: postgres
--

ALTER TABLE public.fletes ENABLE ROW LEVEL SECURITY;

--
-- Name: herramienta_movimientos; Type: ROW SECURITY; Schema: public; Owner: postgres
--

ALTER TABLE public.herramienta_movimientos ENABLE ROW LEVEL SECURITY;

--
-- Name: herramientas; Type: ROW SECURITY; Schema: public; Owner: postgres
--

ALTER TABLE public.herramientas ENABLE ROW LEVEL SECURITY;

--
-- Name: ingestion_jobs; Type: ROW SECURITY; Schema: public; Owner: postgres
--

ALTER TABLE public.ingestion_jobs ENABLE ROW LEVEL SECURITY;

--
-- Name: insumo_movimientos; Type: ROW SECURITY; Schema: public; Owner: postgres
--

ALTER TABLE public.insumo_movimientos ENABLE ROW LEVEL SECURITY;

--
-- Name: insumos; Type: ROW SECURITY; Schema: public; Owner: postgres
--

ALTER TABLE public.insumos ENABLE ROW LEVEL SECURITY;

--
-- Name: invoice_categories; Type: ROW SECURITY; Schema: public; Owner: postgres
--

ALTER TABLE public.invoice_categories ENABLE ROW LEVEL SECURITY;

--
-- Name: liquidacion_sueldo_conceptos; Type: ROW SECURITY; Schema: public; Owner: postgres
--

ALTER TABLE public.liquidacion_sueldo_conceptos ENABLE ROW LEVEL SECURITY;

--
-- Name: liquidacion_sueldo_distribuciones; Type: ROW SECURITY; Schema: public; Owner: postgres
--

ALTER TABLE public.liquidacion_sueldo_distribuciones ENABLE ROW LEVEL SECURITY;

--
-- Name: liquidaciones_sueldo; Type: ROW SECURITY; Schema: public; Owner: postgres
--

ALTER TABLE public.liquidaciones_sueldo ENABLE ROW LEVEL SECURITY;

--
-- Name: materiales; Type: ROW SECURITY; Schema: public; Owner: postgres
--

ALTER TABLE public.materiales ENABLE ROW LEVEL SECURITY;

--
-- Name: materiales_proveedores; Type: ROW SECURITY; Schema: public; Owner: postgres
--

ALTER TABLE public.materiales_proveedores ENABLE ROW LEVEL SECURITY;

--
-- Name: messages; Type: ROW SECURITY; Schema: public; Owner: postgres
--

ALTER TABLE public.messages ENABLE ROW LEVEL SECURITY;

--
-- Name: movimientomaterial; Type: ROW SECURITY; Schema: public; Owner: postgres
--

ALTER TABLE public.movimientomaterial ENABLE ROW LEVEL SECURITY;

--
-- Name: movimientos; Type: ROW SECURITY; Schema: public; Owner: postgres
--

ALTER TABLE public.movimientos ENABLE ROW LEVEL SECURITY;

--
-- Name: obligaciones_pago; Type: ROW SECURITY; Schema: public; Owner: postgres
--

ALTER TABLE public.obligaciones_pago ENABLE ROW LEVEL SECURITY;

--
-- Name: obligaciones_pagos; Type: ROW SECURITY; Schema: public; Owner: postgres
--

ALTER TABLE public.obligaciones_pagos ENABLE ROW LEVEL SECURITY;

--
-- Name: obras; Type: ROW SECURITY; Schema: public; Owner: postgres
--

ALTER TABLE public.obras ENABLE ROW LEVEL SECURITY;

--
-- Name: operational_identities; Type: ROW SECURITY; Schema: public; Owner: postgres
--

ALTER TABLE public.operational_identities ENABLE ROW LEVEL SECURITY;

--
-- Name: ordenes_compra; Type: ROW SECURITY; Schema: public; Owner: postgres
--

ALTER TABLE public.ordenes_compra ENABLE ROW LEVEL SECURITY;

--
-- Name: ordenes_compra_detalle; Type: ROW SECURITY; Schema: public; Owner: postgres
--

ALTER TABLE public.ordenes_compra_detalle ENABLE ROW LEVEL SECURITY;

--
-- Name: ordenes_compra_faltantes; Type: ROW SECURITY; Schema: public; Owner: postgres
--

ALTER TABLE public.ordenes_compra_faltantes ENABLE ROW LEVEL SECURITY;

--
-- Name: organization_members; Type: ROW SECURITY; Schema: public; Owner: postgres
--

ALTER TABLE public.organization_members ENABLE ROW LEVEL SECURITY;

--
-- Name: organizations; Type: ROW SECURITY; Schema: public; Owner: postgres
--

ALTER TABLE public.organizations ENABLE ROW LEVEL SECURITY;

--
-- Name: proveedores; Type: ROW SECURITY; Schema: public; Owner: postgres
--

ALTER TABLE public.proveedores ENABLE ROW LEVEL SECURITY;

--
-- Name: recortes; Type: ROW SECURITY; Schema: public; Owner: postgres
--

ALTER TABLE public.recortes ENABLE ROW LEVEL SECURITY;

--
-- Name: reporte_stock_materiales; Type: ROW SECURITY; Schema: public; Owner: postgres
--

ALTER TABLE public.reporte_stock_materiales ENABLE ROW LEVEL SECURITY;

--
-- Name: reporte_stock_secciones; Type: ROW SECURITY; Schema: public; Owner: postgres
--

ALTER TABLE public.reporte_stock_secciones ENABLE ROW LEVEL SECURITY;

--
-- Name: schema_migrations; Type: ROW SECURITY; Schema: public; Owner: postgres
--

ALTER TABLE public.schema_migrations ENABLE ROW LEVEL SECURITY;

--
-- Name: sueldo_pago_distribuciones; Type: ROW SECURITY; Schema: public; Owner: postgres
--

ALTER TABLE public.sueldo_pago_distribuciones ENABLE ROW LEVEL SECURITY;

--
-- Name: sueldo_personas; Type: ROW SECURITY; Schema: public; Owner: postgres
--

ALTER TABLE public.sueldo_personas ENABLE ROW LEVEL SECURITY;

--
-- Name: sueldo_presupuestos_obra; Type: ROW SECURITY; Schema: public; Owner: postgres
--

ALTER TABLE public.sueldo_presupuestos_obra ENABLE ROW LEVEL SECURITY;

--
-- Name: sueldo_vinculo_condiciones; Type: ROW SECURITY; Schema: public; Owner: postgres
--

ALTER TABLE public.sueldo_vinculo_condiciones ENABLE ROW LEVEL SECURITY;

--
-- Name: sueldo_vinculo_distribuciones; Type: ROW SECURITY; Schema: public; Owner: postgres
--

ALTER TABLE public.sueldo_vinculo_distribuciones ENABLE ROW LEVEL SECURITY;

--
-- Name: sueldo_vinculos; Type: ROW SECURITY; Schema: public; Owner: postgres
--

ALTER TABLE public.sueldo_vinculos ENABLE ROW LEVEL SECURITY;

--
-- Name: sueldos_auditoria; Type: ROW SECURITY; Schema: public; Owner: postgres
--

ALTER TABLE public.sueldos_auditoria ENABLE ROW LEVEL SECURITY;

--
-- Name: sueldos_pagos; Type: ROW SECURITY; Schema: public; Owner: postgres
--

ALTER TABLE public.sueldos_pagos ENABLE ROW LEVEL SECURITY;

--
-- Name: tasks; Type: ROW SECURITY; Schema: public; Owner: postgres
--

ALTER TABLE public.tasks ENABLE ROW LEVEL SECURITY;

--
-- Name: tipo_movimiento; Type: ROW SECURITY; Schema: public; Owner: postgres
--

ALTER TABLE public.tipo_movimiento ENABLE ROW LEVEL SECURITY;

--
-- Name: tipos_obligacion; Type: ROW SECURITY; Schema: public; Owner: postgres
--

ALTER TABLE public.tipos_obligacion ENABLE ROW LEVEL SECURITY;

--
-- Name: ubicacion; Type: ROW SECURITY; Schema: public; Owner: postgres
--

ALTER TABLE public.ubicacion ENABLE ROW LEVEL SECURITY;

--
-- Name: usage_events; Type: ROW SECURITY; Schema: public; Owner: postgres
--

ALTER TABLE public.usage_events ENABLE ROW LEVEL SECURITY;

--
-- Name: users; Type: ROW SECURITY; Schema: public; Owner: postgres
--

ALTER TABLE public.users ENABLE ROW LEVEL SECURITY;

--
-- Name: visibilidad_modulos; Type: ROW SECURITY; Schema: public; Owner: postgres
--

ALTER TABLE public.visibilidad_modulos ENABLE ROW LEVEL SECURITY;

--
-- Name: SCHEMA public; Type: ACL; Schema: -; Owner: pg_database_owner
--

GRANT USAGE ON SCHEMA public TO postgres;
GRANT USAGE ON SCHEMA public TO anon;
GRANT USAGE ON SCHEMA public TO authenticated;
GRANT USAGE ON SCHEMA public TO service_role;


--
-- Name: FUNCTION check_invoice_payment_consistency(); Type: ACL; Schema: public; Owner: postgres
--

REVOKE ALL ON FUNCTION public.check_invoice_payment_consistency() FROM PUBLIC;
GRANT ALL ON FUNCTION public.check_invoice_payment_consistency() TO service_role;


--
-- Name: FUNCTION check_obligation_payment_consistency(); Type: ACL; Schema: public; Owner: postgres
--

REVOKE ALL ON FUNCTION public.check_obligation_payment_consistency() FROM PUBLIC;
GRANT ALL ON FUNCTION public.check_obligation_payment_consistency() TO service_role;


--
-- Name: FUNCTION check_payroll_distribution(); Type: ACL; Schema: public; Owner: postgres
--

REVOKE ALL ON FUNCTION public.check_payroll_distribution() FROM PUBLIC;
GRANT ALL ON FUNCTION public.check_payroll_distribution() TO service_role;


--
-- Name: FUNCTION guard_invoice_identity(); Type: ACL; Schema: public; Owner: postgres
--

REVOKE ALL ON FUNCTION public.guard_invoice_identity() FROM PUBLIC;
GRANT ALL ON FUNCTION public.guard_invoice_identity() TO service_role;


--
-- Name: FUNCTION guard_invoice_payment(); Type: ACL; Schema: public; Owner: postgres
--

REVOKE ALL ON FUNCTION public.guard_invoice_payment() FROM PUBLIC;
GRANT ALL ON FUNCTION public.guard_invoice_payment() TO service_role;


--
-- Name: FUNCTION guard_invoice_payment_history(); Type: ACL; Schema: public; Owner: postgres
--

REVOKE ALL ON FUNCTION public.guard_invoice_payment_history() FROM PUBLIC;
GRANT ALL ON FUNCTION public.guard_invoice_payment_history() TO service_role;


--
-- Name: FUNCTION guard_obligation_payment(); Type: ACL; Schema: public; Owner: postgres
--

REVOKE ALL ON FUNCTION public.guard_obligation_payment() FROM PUBLIC;
GRANT ALL ON FUNCTION public.guard_obligation_payment() TO service_role;


--
-- Name: FUNCTION guard_obligation_payment_history(); Type: ACL; Schema: public; Owner: postgres
--

REVOKE ALL ON FUNCTION public.guard_obligation_payment_history() FROM PUBLIC;
GRANT ALL ON FUNCTION public.guard_obligation_payment_history() TO service_role;


--
-- Name: FUNCTION guard_payroll_payment(); Type: ACL; Schema: public; Owner: postgres
--

REVOKE ALL ON FUNCTION public.guard_payroll_payment() FROM PUBLIC;
GRANT ALL ON FUNCTION public.guard_payroll_payment() TO service_role;


--
-- Name: FUNCTION normalize_operational_identity(value text); Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON FUNCTION public.normalize_operational_identity(value text) TO anon;
GRANT ALL ON FUNCTION public.normalize_operational_identity(value text) TO authenticated;
GRANT ALL ON FUNCTION public.normalize_operational_identity(value text) TO service_role;


--
-- Name: FUNCTION set_movimientos_timestamps(); Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON FUNCTION public.set_movimientos_timestamps() TO anon;
GRANT ALL ON FUNCTION public.set_movimientos_timestamps() TO authenticated;
GRANT ALL ON FUNCTION public.set_movimientos_timestamps() TO service_role;


--
-- Name: FUNCTION set_ordenes_compra_updated_at(); Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON FUNCTION public.set_ordenes_compra_updated_at() TO anon;
GRANT ALL ON FUNCTION public.set_ordenes_compra_updated_at() TO authenticated;
GRANT ALL ON FUNCTION public.set_ordenes_compra_updated_at() TO service_role;


--
-- Name: FUNCTION sync_obligation_type_reference(); Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON FUNCTION public.sync_obligation_type_reference() TO anon;
GRANT ALL ON FUNCTION public.sync_obligation_type_reference() TO authenticated;
GRANT ALL ON FUNCTION public.sync_obligation_type_reference() TO service_role;


--
-- Name: FUNCTION sync_visibilidad_modulos_columns(); Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON FUNCTION public.sync_visibilidad_modulos_columns() TO anon;
GRANT ALL ON FUNCTION public.sync_visibilidad_modulos_columns() TO authenticated;
GRANT ALL ON FUNCTION public.sync_visibilidad_modulos_columns() TO service_role;


--
-- Name: TABLE _migracion_origen_destino_map; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public._migracion_origen_destino_map TO service_role;


--
-- Name: TABLE assistant_configs; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.assistant_configs TO service_role;


--
-- Name: TABLE audit_logs; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.audit_logs TO service_role;


--
-- Name: TABLE authorization_policy_state; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.authorization_policy_state TO service_role;


--
-- Name: TABLE backup_info_20260325; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.backup_info_20260325 TO service_role;


--
-- Name: SEQUENCE backup_info_20260325_id_seq; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON SEQUENCE public.backup_info_20260325_id_seq TO service_role;


--
-- Name: TABLE backup_movimientomaterial_20260325; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.backup_movimientomaterial_20260325 TO service_role;


--
-- Name: TABLE backup_movimientos_20260325; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.backup_movimientos_20260325 TO service_role;


--
-- Name: TABLE backup_recortes_20260325; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.backup_recortes_20260325 TO service_role;


--
-- Name: TABLE bitacora; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.bitacora TO service_role;


--
-- Name: SEQUENCE bitacora_id_seq; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON SEQUENCE public.bitacora_id_seq TO service_role;


--
-- Name: TABLE business_memories; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.business_memories TO service_role;


--
-- Name: TABLE canaletas_plegados; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.canaletas_plegados TO anon;
GRANT ALL ON TABLE public.canaletas_plegados TO authenticated;
GRANT ALL ON TABLE public.canaletas_plegados TO service_role;


--
-- Name: TABLE canaletas_plegados_fotos; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.canaletas_plegados_fotos TO anon;
GRANT ALL ON TABLE public.canaletas_plegados_fotos TO authenticated;
GRANT ALL ON TABLE public.canaletas_plegados_fotos TO service_role;


--
-- Name: SEQUENCE canaletas_plegados_fotos_id_seq; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON SEQUENCE public.canaletas_plegados_fotos_id_seq TO anon;
GRANT ALL ON SEQUENCE public.canaletas_plegados_fotos_id_seq TO authenticated;
GRANT ALL ON SEQUENCE public.canaletas_plegados_fotos_id_seq TO service_role;


--
-- Name: SEQUENCE canaletas_plegados_id_seq; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON SEQUENCE public.canaletas_plegados_id_seq TO anon;
GRANT ALL ON SEQUENCE public.canaletas_plegados_id_seq TO authenticated;
GRANT ALL ON SEQUENCE public.canaletas_plegados_id_seq TO service_role;


--
-- Name: TABLE categorias; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.categorias TO anon;
GRANT ALL ON TABLE public.categorias TO authenticated;
GRANT ALL ON TABLE public.categorias TO service_role;


--
-- Name: SEQUENCE categorias_id_categoria_seq; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON SEQUENCE public.categorias_id_categoria_seq TO anon;
GRANT ALL ON SEQUENCE public.categorias_id_categoria_seq TO authenticated;
GRANT ALL ON SEQUENCE public.categorias_id_categoria_seq TO service_role;


--
-- Name: TABLE conceptos_sueldo; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.conceptos_sueldo TO service_role;


--
-- Name: TABLE conteos_inventario; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.conteos_inventario TO anon;
GRANT ALL ON TABLE public.conteos_inventario TO authenticated;
GRANT ALL ON TABLE public.conteos_inventario TO service_role;


--
-- Name: TABLE conteos_inventario_detalle; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.conteos_inventario_detalle TO anon;
GRANT ALL ON TABLE public.conteos_inventario_detalle TO authenticated;
GRANT ALL ON TABLE public.conteos_inventario_detalle TO service_role;


--
-- Name: SEQUENCE conteos_inventario_detalle_id_detalle_seq; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON SEQUENCE public.conteos_inventario_detalle_id_detalle_seq TO anon;
GRANT ALL ON SEQUENCE public.conteos_inventario_detalle_id_detalle_seq TO authenticated;
GRANT ALL ON SEQUENCE public.conteos_inventario_detalle_id_detalle_seq TO service_role;


--
-- Name: SEQUENCE conteos_inventario_id_conteo_seq; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON SEQUENCE public.conteos_inventario_id_conteo_seq TO anon;
GRANT ALL ON SEQUENCE public.conteos_inventario_id_conteo_seq TO authenticated;
GRANT ALL ON SEQUENCE public.conteos_inventario_id_conteo_seq TO service_role;


--
-- Name: TABLE conversation_summaries; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.conversation_summaries TO service_role;


--
-- Name: TABLE conversations; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.conversations TO service_role;


--
-- Name: TABLE document_chunks; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.document_chunks TO service_role;


--
-- Name: TABLE documents; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.documents TO service_role;


--
-- Name: TABLE factura_pagos; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.factura_pagos TO service_role;


--
-- Name: SEQUENCE factura_pagos_id_pago_seq; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON SEQUENCE public.factura_pagos_id_pago_seq TO service_role;


--
-- Name: TABLE facturas; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.facturas TO service_role;


--
-- Name: SEQUENCE facturas_id_factura_seq; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON SEQUENCE public.facturas_id_factura_seq TO service_role;


--
-- Name: TABLE financial_operation_requests; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.financial_operation_requests TO service_role;


--
-- Name: TABLE fiscal_periods; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.fiscal_periods TO service_role;


--
-- Name: TABLE fiscal_thresholds; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.fiscal_thresholds TO service_role;


--
-- Name: TABLE fletes; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.fletes TO anon;
GRANT ALL ON TABLE public.fletes TO authenticated;
GRANT ALL ON TABLE public.fletes TO service_role;


--
-- Name: SEQUENCE fletes_id_flete_seq; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON SEQUENCE public.fletes_id_flete_seq TO anon;
GRANT ALL ON SEQUENCE public.fletes_id_flete_seq TO authenticated;
GRANT ALL ON SEQUENCE public.fletes_id_flete_seq TO service_role;


--
-- Name: TABLE herramienta_movimientos; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.herramienta_movimientos TO anon;
GRANT ALL ON TABLE public.herramienta_movimientos TO authenticated;
GRANT ALL ON TABLE public.herramienta_movimientos TO service_role;


--
-- Name: SEQUENCE herramienta_movimientos_id_seq; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON SEQUENCE public.herramienta_movimientos_id_seq TO anon;
GRANT ALL ON SEQUENCE public.herramienta_movimientos_id_seq TO authenticated;
GRANT ALL ON SEQUENCE public.herramienta_movimientos_id_seq TO service_role;


--
-- Name: TABLE herramientas; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.herramientas TO anon;
GRANT ALL ON TABLE public.herramientas TO authenticated;
GRANT ALL ON TABLE public.herramientas TO service_role;


--
-- Name: SEQUENCE herramientas_id_herramienta_seq; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON SEQUENCE public.herramientas_id_herramienta_seq TO anon;
GRANT ALL ON SEQUENCE public.herramientas_id_herramienta_seq TO authenticated;
GRANT ALL ON SEQUENCE public.herramientas_id_herramienta_seq TO service_role;


--
-- Name: TABLE ingestion_jobs; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.ingestion_jobs TO service_role;


--
-- Name: TABLE insumo_movimientos; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.insumo_movimientos TO anon;
GRANT ALL ON TABLE public.insumo_movimientos TO authenticated;
GRANT ALL ON TABLE public.insumo_movimientos TO service_role;


--
-- Name: SEQUENCE insumo_movimientos_id_seq; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON SEQUENCE public.insumo_movimientos_id_seq TO anon;
GRANT ALL ON SEQUENCE public.insumo_movimientos_id_seq TO authenticated;
GRANT ALL ON SEQUENCE public.insumo_movimientos_id_seq TO service_role;


--
-- Name: TABLE insumos; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.insumos TO anon;
GRANT ALL ON TABLE public.insumos TO authenticated;
GRANT ALL ON TABLE public.insumos TO service_role;


--
-- Name: SEQUENCE insumos_id_insumo_seq; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON SEQUENCE public.insumos_id_insumo_seq TO anon;
GRANT ALL ON SEQUENCE public.insumos_id_insumo_seq TO authenticated;
GRANT ALL ON SEQUENCE public.insumos_id_insumo_seq TO service_role;


--
-- Name: TABLE invoice_categories; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.invoice_categories TO service_role;


--
-- Name: SEQUENCE invoice_categories_id_seq; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON SEQUENCE public.invoice_categories_id_seq TO service_role;


--
-- Name: TABLE liquidacion_sueldo_conceptos; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.liquidacion_sueldo_conceptos TO service_role;


--
-- Name: TABLE liquidacion_sueldo_distribuciones; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.liquidacion_sueldo_distribuciones TO service_role;


--
-- Name: TABLE liquidaciones_sueldo; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.liquidaciones_sueldo TO service_role;


--
-- Name: TABLE materiales; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.materiales TO anon;
GRANT ALL ON TABLE public.materiales TO authenticated;
GRANT ALL ON TABLE public.materiales TO service_role;


--
-- Name: SEQUENCE materiales_id_seq; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON SEQUENCE public.materiales_id_seq TO anon;
GRANT ALL ON SEQUENCE public.materiales_id_seq TO authenticated;
GRANT ALL ON SEQUENCE public.materiales_id_seq TO service_role;


--
-- Name: TABLE materiales_proveedores; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.materiales_proveedores TO anon;
GRANT ALL ON TABLE public.materiales_proveedores TO authenticated;
GRANT ALL ON TABLE public.materiales_proveedores TO service_role;


--
-- Name: SEQUENCE materiales_proveedores_id_seq; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON SEQUENCE public.materiales_proveedores_id_seq TO anon;
GRANT ALL ON SEQUENCE public.materiales_proveedores_id_seq TO authenticated;
GRANT ALL ON SEQUENCE public.materiales_proveedores_id_seq TO service_role;


--
-- Name: TABLE messages; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.messages TO service_role;


--
-- Name: TABLE movimientomaterial; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.movimientomaterial TO anon;
GRANT ALL ON TABLE public.movimientomaterial TO authenticated;
GRANT ALL ON TABLE public.movimientomaterial TO service_role;


--
-- Name: SEQUENCE movimientomaterial_id_movimientomaterial_seq; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON SEQUENCE public.movimientomaterial_id_movimientomaterial_seq TO anon;
GRANT ALL ON SEQUENCE public.movimientomaterial_id_movimientomaterial_seq TO authenticated;
GRANT ALL ON SEQUENCE public.movimientomaterial_id_movimientomaterial_seq TO service_role;


--
-- Name: TABLE movimientos; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.movimientos TO anon;
GRANT ALL ON TABLE public.movimientos TO authenticated;
GRANT ALL ON TABLE public.movimientos TO service_role;


--
-- Name: SEQUENCE movimientos_id_movimiento_seq; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON SEQUENCE public.movimientos_id_movimiento_seq TO anon;
GRANT ALL ON SEQUENCE public.movimientos_id_movimiento_seq TO authenticated;
GRANT ALL ON SEQUENCE public.movimientos_id_movimiento_seq TO service_role;


--
-- Name: TABLE obligaciones_pago; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.obligaciones_pago TO service_role;


--
-- Name: SEQUENCE obligaciones_pago_id_obligacion_seq; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON SEQUENCE public.obligaciones_pago_id_obligacion_seq TO service_role;


--
-- Name: TABLE obligaciones_pagos; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.obligaciones_pagos TO service_role;


--
-- Name: SEQUENCE obligaciones_pagos_id_pago_seq; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON SEQUENCE public.obligaciones_pagos_id_pago_seq TO service_role;


--
-- Name: TABLE obras; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.obras TO anon;
GRANT ALL ON TABLE public.obras TO authenticated;
GRANT ALL ON TABLE public.obras TO service_role;


--
-- Name: SEQUENCE obras_id_seq; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON SEQUENCE public.obras_id_seq TO anon;
GRANT ALL ON SEQUENCE public.obras_id_seq TO authenticated;
GRANT ALL ON SEQUENCE public.obras_id_seq TO service_role;


--
-- Name: TABLE operational_identities; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.operational_identities TO service_role;


--
-- Name: SEQUENCE operational_identities_id_seq; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON SEQUENCE public.operational_identities_id_seq TO service_role;


--
-- Name: TABLE ordenes_compra; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.ordenes_compra TO anon;
GRANT ALL ON TABLE public.ordenes_compra TO authenticated;
GRANT ALL ON TABLE public.ordenes_compra TO service_role;


--
-- Name: TABLE ordenes_compra_detalle; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.ordenes_compra_detalle TO anon;
GRANT ALL ON TABLE public.ordenes_compra_detalle TO authenticated;
GRANT ALL ON TABLE public.ordenes_compra_detalle TO service_role;


--
-- Name: SEQUENCE ordenes_compra_detalle_id_detalle_seq; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON SEQUENCE public.ordenes_compra_detalle_id_detalle_seq TO anon;
GRANT ALL ON SEQUENCE public.ordenes_compra_detalle_id_detalle_seq TO authenticated;
GRANT ALL ON SEQUENCE public.ordenes_compra_detalle_id_detalle_seq TO service_role;


--
-- Name: TABLE ordenes_compra_faltantes; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.ordenes_compra_faltantes TO anon;
GRANT ALL ON TABLE public.ordenes_compra_faltantes TO authenticated;
GRANT ALL ON TABLE public.ordenes_compra_faltantes TO service_role;


--
-- Name: SEQUENCE ordenes_compra_faltantes_id_faltante_seq; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON SEQUENCE public.ordenes_compra_faltantes_id_faltante_seq TO anon;
GRANT ALL ON SEQUENCE public.ordenes_compra_faltantes_id_faltante_seq TO authenticated;
GRANT ALL ON SEQUENCE public.ordenes_compra_faltantes_id_faltante_seq TO service_role;


--
-- Name: SEQUENCE ordenes_compra_id_orden_compra_seq; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON SEQUENCE public.ordenes_compra_id_orden_compra_seq TO anon;
GRANT ALL ON SEQUENCE public.ordenes_compra_id_orden_compra_seq TO authenticated;
GRANT ALL ON SEQUENCE public.ordenes_compra_id_orden_compra_seq TO service_role;


--
-- Name: TABLE organization_members; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.organization_members TO service_role;


--
-- Name: TABLE organizations; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.organizations TO service_role;


--
-- Name: TABLE proveedores; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.proveedores TO anon;
GRANT ALL ON TABLE public.proveedores TO authenticated;
GRANT ALL ON TABLE public.proveedores TO service_role;


--
-- Name: SEQUENCE proveedores_id_proveedores_seq; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON SEQUENCE public.proveedores_id_proveedores_seq TO anon;
GRANT ALL ON SEQUENCE public.proveedores_id_proveedores_seq TO authenticated;
GRANT ALL ON SEQUENCE public.proveedores_id_proveedores_seq TO service_role;


--
-- Name: TABLE recortes; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.recortes TO anon;
GRANT ALL ON TABLE public.recortes TO authenticated;
GRANT ALL ON TABLE public.recortes TO service_role;


--
-- Name: SEQUENCE recortes_id_seq; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON SEQUENCE public.recortes_id_seq TO anon;
GRANT ALL ON SEQUENCE public.recortes_id_seq TO authenticated;
GRANT ALL ON SEQUENCE public.recortes_id_seq TO service_role;


--
-- Name: TABLE reporte_stock_materiales; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.reporte_stock_materiales TO service_role;


--
-- Name: TABLE reporte_stock_secciones; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.reporte_stock_secciones TO service_role;


--
-- Name: SEQUENCE reporte_stock_secciones_id_seq; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON SEQUENCE public.reporte_stock_secciones_id_seq TO service_role;


--
-- Name: TABLE schema_migrations; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.schema_migrations TO service_role;


--
-- Name: TABLE sueldo_pago_distribuciones; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.sueldo_pago_distribuciones TO service_role;


--
-- Name: TABLE sueldo_personas; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.sueldo_personas TO service_role;


--
-- Name: TABLE sueldo_presupuestos_obra; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.sueldo_presupuestos_obra TO service_role;


--
-- Name: TABLE sueldo_vinculo_condiciones; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.sueldo_vinculo_condiciones TO service_role;


--
-- Name: TABLE sueldo_vinculo_distribuciones; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.sueldo_vinculo_distribuciones TO service_role;


--
-- Name: TABLE sueldo_vinculos; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.sueldo_vinculos TO service_role;


--
-- Name: TABLE sueldos_auditoria; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.sueldos_auditoria TO service_role;


--
-- Name: SEQUENCE sueldos_auditoria_id_seq; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON SEQUENCE public.sueldos_auditoria_id_seq TO anon;
GRANT ALL ON SEQUENCE public.sueldos_auditoria_id_seq TO authenticated;
GRANT ALL ON SEQUENCE public.sueldos_auditoria_id_seq TO service_role;


--
-- Name: TABLE sueldos_pagos; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.sueldos_pagos TO service_role;


--
-- Name: TABLE tasks; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.tasks TO service_role;


--
-- Name: TABLE tipo_movimiento; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.tipo_movimiento TO anon;
GRANT ALL ON TABLE public.tipo_movimiento TO authenticated;
GRANT ALL ON TABLE public.tipo_movimiento TO service_role;


--
-- Name: SEQUENCE tipo_movimiento_id_tipomovimiento_seq; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON SEQUENCE public.tipo_movimiento_id_tipomovimiento_seq TO anon;
GRANT ALL ON SEQUENCE public.tipo_movimiento_id_tipomovimiento_seq TO authenticated;
GRANT ALL ON SEQUENCE public.tipo_movimiento_id_tipomovimiento_seq TO service_role;


--
-- Name: TABLE tipos_obligacion; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.tipos_obligacion TO service_role;


--
-- Name: SEQUENCE tipos_obligacion_id_seq; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON SEQUENCE public.tipos_obligacion_id_seq TO service_role;


--
-- Name: TABLE ubicacion; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.ubicacion TO anon;
GRANT ALL ON TABLE public.ubicacion TO authenticated;
GRANT ALL ON TABLE public.ubicacion TO service_role;


--
-- Name: SEQUENCE ubicaciones_id_seq; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON SEQUENCE public.ubicaciones_id_seq TO anon;
GRANT ALL ON SEQUENCE public.ubicaciones_id_seq TO authenticated;
GRANT ALL ON SEQUENCE public.ubicaciones_id_seq TO service_role;


--
-- Name: TABLE usage_events; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.usage_events TO service_role;


--
-- Name: TABLE users; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.users TO service_role;


--
-- Name: TABLE visibilidad_modulos; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.visibilidad_modulos TO service_role;


--
-- Name: SEQUENCE visibilidad_modulos_id_seq; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON SEQUENCE public.visibilidad_modulos_id_seq TO service_role;


--
-- Name: DEFAULT PRIVILEGES FOR SEQUENCES; Type: DEFAULT ACL; Schema: public; Owner: postgres
--

ALTER DEFAULT PRIVILEGES FOR ROLE postgres IN SCHEMA public GRANT ALL ON SEQUENCES TO postgres;
ALTER DEFAULT PRIVILEGES FOR ROLE postgres IN SCHEMA public GRANT ALL ON SEQUENCES TO service_role;


--
-- Name: DEFAULT PRIVILEGES FOR SEQUENCES; Type: DEFAULT ACL; Schema: public; Owner: supabase_admin
--

ALTER DEFAULT PRIVILEGES FOR ROLE supabase_admin IN SCHEMA public GRANT ALL ON SEQUENCES TO postgres;
ALTER DEFAULT PRIVILEGES FOR ROLE supabase_admin IN SCHEMA public GRANT ALL ON SEQUENCES TO anon;
ALTER DEFAULT PRIVILEGES FOR ROLE supabase_admin IN SCHEMA public GRANT ALL ON SEQUENCES TO authenticated;
ALTER DEFAULT PRIVILEGES FOR ROLE supabase_admin IN SCHEMA public GRANT ALL ON SEQUENCES TO service_role;


--
-- Name: DEFAULT PRIVILEGES FOR FUNCTIONS; Type: DEFAULT ACL; Schema: public; Owner: postgres
--

ALTER DEFAULT PRIVILEGES FOR ROLE postgres IN SCHEMA public GRANT ALL ON FUNCTIONS TO postgres;
ALTER DEFAULT PRIVILEGES FOR ROLE postgres IN SCHEMA public GRANT ALL ON FUNCTIONS TO anon;
ALTER DEFAULT PRIVILEGES FOR ROLE postgres IN SCHEMA public GRANT ALL ON FUNCTIONS TO authenticated;
ALTER DEFAULT PRIVILEGES FOR ROLE postgres IN SCHEMA public GRANT ALL ON FUNCTIONS TO service_role;


--
-- Name: DEFAULT PRIVILEGES FOR FUNCTIONS; Type: DEFAULT ACL; Schema: public; Owner: supabase_admin
--

ALTER DEFAULT PRIVILEGES FOR ROLE supabase_admin IN SCHEMA public GRANT ALL ON FUNCTIONS TO postgres;
ALTER DEFAULT PRIVILEGES FOR ROLE supabase_admin IN SCHEMA public GRANT ALL ON FUNCTIONS TO anon;
ALTER DEFAULT PRIVILEGES FOR ROLE supabase_admin IN SCHEMA public GRANT ALL ON FUNCTIONS TO authenticated;
ALTER DEFAULT PRIVILEGES FOR ROLE supabase_admin IN SCHEMA public GRANT ALL ON FUNCTIONS TO service_role;


--
-- Name: DEFAULT PRIVILEGES FOR TABLES; Type: DEFAULT ACL; Schema: public; Owner: postgres
--

ALTER DEFAULT PRIVILEGES FOR ROLE postgres IN SCHEMA public GRANT ALL ON TABLES TO postgres;
ALTER DEFAULT PRIVILEGES FOR ROLE postgres IN SCHEMA public GRANT ALL ON TABLES TO service_role;


--
-- Name: DEFAULT PRIVILEGES FOR TABLES; Type: DEFAULT ACL; Schema: public; Owner: supabase_admin
--

ALTER DEFAULT PRIVILEGES FOR ROLE supabase_admin IN SCHEMA public GRANT ALL ON TABLES TO postgres;
ALTER DEFAULT PRIVILEGES FOR ROLE supabase_admin IN SCHEMA public GRANT ALL ON TABLES TO anon;
ALTER DEFAULT PRIVILEGES FOR ROLE supabase_admin IN SCHEMA public GRANT ALL ON TABLES TO authenticated;
ALTER DEFAULT PRIVILEGES FOR ROLE supabase_admin IN SCHEMA public GRANT ALL ON TABLES TO service_role;


--
-- PostgreSQL database dump complete
--

\unrestrict 6KHWA15UrKVDq4paFUiUBPUGpyIuNXpoPK3ctuZCThNA3mQ7I4h8GQCTuOfiqEa




