-- Firma de esquema y datos para demostrar que F4/F10 son un no-op sobre init/demo.
CREATE FUNCTION pg_temp.firma_f10() RETURNS jsonb LANGUAGE plpgsql AS $$
DECLARE tabla text; firma text; datos jsonb := '{}'::jsonb;
BEGIN
    FOR tabla IN SELECT tablename FROM pg_tables WHERE schemaname='public' ORDER BY tablename LOOP
        EXECUTE format('SELECT md5(coalesce(string_agg(to_jsonb(fila)::text, '''' ORDER BY to_jsonb(fila)::text),'''')) FROM public.%I fila',tabla) INTO firma;
        datos := datos || jsonb_build_object(tabla,firma);
    END LOOP;
    RETURN jsonb_build_object(
        'datos',datos,
        'columnas',(SELECT jsonb_agg(to_jsonb(c) ORDER BY table_name,ordinal_position) FROM information_schema.columns c WHERE table_schema='public'),
        'restricciones',(SELECT jsonb_agg(jsonb_build_array(conrelid::regclass::text,conname,pg_get_constraintdef(oid),convalidated) ORDER BY conrelid,conname) FROM pg_constraint WHERE connamespace='public'::regnamespace),
        'indices',(SELECT jsonb_agg(to_jsonb(i) ORDER BY tablename,indexname) FROM pg_indexes i WHERE schemaname='public'),
        'triggers',(SELECT jsonb_agg(pg_get_triggerdef(oid) ORDER BY tgrelid,tgname) FROM pg_trigger WHERE NOT tgisinternal AND tgrelid IN (SELECT oid FROM pg_class WHERE relnamespace='public'::regnamespace)),
        'funciones',(SELECT jsonb_agg(pg_get_functiondef(oid) ORDER BY proname,oid) FROM pg_proc WHERE pronamespace='public'::regnamespace AND prokind='f'));
END $$;
CREATE TEMP TABLE firma_inicial_f10 AS SELECT pg_temp.firma_f10() AS firma;
