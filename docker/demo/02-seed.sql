-- Datos ficticios para una DB nueva. No reutilizar sobre instalaciones existentes.
BEGIN;
DO $guard$ BEGIN
 IF current_database() !~ '^steelog_(test|demo)_[0-9a-f]{32}$' OR EXISTS(SELECT FROM materiales) THEN
  RAISE EXCEPTION 'Seed reservado a una DB demo/test nueva y vacía de materiales';
 END IF;
END $guard$;
-- =============================================================================
-- SEED DATA PARA ENTORNO LOCAL / DEMO TESIS
-- =============================================================================

-- Tipos de Movimiento
INSERT INTO public.tipo_movimiento (id_tipomovimiento, nombre) VALUES
(1, 'Carga inicial de inventario'),
(2, 'Compra'),
(3, 'Compra eventual'),
(4, 'Envio a Obra'),
(5, 'Devolucion de obra'),
(6, 'Aporte de cliente'),
(7, 'Venta de material'),
(8, 'Uso interno'),
(11, 'REGISTRO INTERNO')
ON CONFLICT (id_tipomovimiento) DO NOTHING;

SELECT setval('public.tipo_movimiento_id_tipomovimiento_seq', COALESCE((SELECT MAX(id_tipomovimiento) FROM public.tipo_movimiento), 1));

-- Ubicaciones estándar
INSERT INTO public.ubicacion (id_ubicacion, nombre) VALUES
(1, '-'),
(2, 'Depósito Central'),
(3, 'Proveedor'),
(4, 'Cliente/Consumo')
ON CONFLICT (id_ubicacion) DO NOTHING;

-- No reutilizar valores consumidos si el seed se ejecuta nuevamente.
DO $$
DECLARE
    seq regclass := pg_get_serial_sequence('public.ubicacion', 'id_ubicacion')::regclass;
    seq_value bigint;
    seq_called boolean;
    max_id bigint;
BEGIN
    SELECT MAX(id_ubicacion) INTO max_id FROM public.ubicacion;
    EXECUTE format('SELECT last_value, is_called FROM %s', seq) INTO seq_value, seq_called;
    IF max_id > seq_value OR (max_id = seq_value AND NOT seq_called) THEN
        PERFORM setval(seq, max_id, true);
    END IF;
END $$;

-- Categorías demo
INSERT INTO public.categorias (id_categoria, nombre, recortable) VALUES
(1, 'PERFILERIA', false),
(2, 'PLACAS', true),
(3, 'SELLADORES', false),
(4, 'FIJACIONES', false),
(5, 'AISLACIONES', false),
(6, 'DRYWALL', false),
(7, 'FERRETERIA', false)
ON CONFLICT (id_categoria) DO NOTHING;

SELECT setval('public.categorias_id_categoria_seq', COALESCE((SELECT MAX(id_categoria) FROM public.categorias), 1));

-- Obras demo
INSERT INTO public.obras (id, nombre, direccion, activa) VALUES
(1, 'Obra Residencial - Torre Belgrano', 'Calle Ficticia 100', true),
(2, 'Obra Comercial - Galería San Martín', 'Pasaje Demo 200', true),
(3, 'Obra Nave Industrial - Parque Logístico', 'Parque de Exposición - Lote Demo', true)
ON CONFLICT (id) DO NOTHING;

SELECT setval('public.obras_id_seq', COALESCE((SELECT MAX(id) FROM public.obras), 1));

-- Proveedores demo
INSERT INTO public.proveedores (id_proveedores, nombre) VALUES
(1, 'Distribuidora Siderúrgica S.A.'),
(2, 'Placas & Aislaciones del Litoral'),
(3, 'Ferretería Industrial del Centro')
ON CONFLICT (id_proveedores) DO NOTHING;

SELECT setval('public.proveedores_id_proveedores_seq', COALESCE((SELECT MAX(id_proveedores) FROM public.proveedores), 1));

-- Materiales demo
INSERT INTO public.materiales (id, id_categoria, nombre, preciounitario, stockactual, stockminimo, codigo, id_ubicacion, oculto) VALUES
(1, 1, 'Perfil Montante 69mm x 2.60m', 3500.00, 150.00, 30.00, 'MON-69', 2, false),
(2, 1, 'Perfil Solera 70mm x 2.60m', 3200.00, 120.00, 25.00, 'SOL-70', 2, false),
(3, 2, 'Placa Yeso Estándar 12.5mm 1.20x2.40m', 8500.00, 85.00, 20.00, 'PLC-EST-125', 2, false),
(4, 2, 'Placa Yeso Antihumedad RU 12.5mm', 11200.00, 40.00, 10.00, 'PLC-RU-125', 2, false),
(5, 4, 'Tornillos T1 Aguja (Caja x 1000)', 4800.00, 15.00, 5.00, 'TRN-T1', 2, false),
(6, 4, 'Tornillos T2 Mecha (Caja x 1000)', 5400.00, 12.00, 4.00, 'TRN-T2', 2, false),
(7, 5, 'Lana de Vidrio con Aluminio 50mm', 14500.00, 25.00, 8.00, 'LAN-VID-50', 2, false),
(8, 7, 'Masilla Lista para Usar (Balde 32kg)', 9800.00, 30.00, 10.00, 'MAS-32KG', 2, false),
(9, 7, 'Cinta de Papel Microperforada (Rollo 150m)', 2100.00, 18.00, 5.00, 'CIN-PAP-150', 2, false)
ON CONFLICT (id) DO NOTHING;

SELECT setval('public.materiales_id_seq', COALESCE((SELECT MAX(id) FROM public.materiales), 1));

-- Carga inicial consistente con los saldos del catálogo ficticio.
INSERT INTO movimientos(id_movimiento,fecha,nroremito,id_origen,tipo_origen,id_destino,tipo_destino,id_tipomovimiento,observaciones)
VALUES(1,'2026-10-01','DEMO-INICIAL',3,'UBICACION',2,'UBICACION',1,'Datos ficticios: apertura del inventario demo');
INSERT INTO movimientomaterial(id_movimiento,id_material,cantidad,stock_actual,precio_unitario,impacto_stock_real)
SELECT 1,id,stockactual,stockactual,0,stockactual FROM materiales;
-- Convención de la app: precio_unitario de línea solo para compra eventual; el total de cabecera se calcula con el precio de catálogo.
UPDATE movimientos SET precio_total=(SELECT COALESCE(SUM(stockactual*preciounitario),0) FROM materiales) WHERE id_movimiento=1;
INSERT INTO movimientos(id_movimiento,fecha,nroremito,id_origen,tipo_origen,id_destino,tipo_destino,id_tipomovimiento,id_obra,costoflete)
VALUES(2,'2026-10-02','DEMO-ENVIO-001',2,'UBICACION',1,'OBRA',4,1,1500);
UPDATE materiales SET stockactual=stockactual-10 WHERE id=1;
INSERT INTO movimientomaterial(id_movimiento,id_material,cantidad,stock_actual,precio_unitario,impacto_stock_real)
VALUES(2,1,10,140,0,-10);
UPDATE movimientos SET precio_total=10*(SELECT preciounitario FROM materiales WHERE id=1) WHERE id_movimiento=2;
INSERT INTO fletes(id_flete,nombre,activo) VALUES(1,'Transporte Demo',true);
INSERT INTO herramientas(id_herramienta,nombre,estado,id_obra,observaciones)
VALUES(1,'Taladro Demo','Óptima',1,'Herramienta ficticia de exposición');
INSERT INTO herramienta_movimientos(id_herramienta,obra_origen,obra_destino,fecha,nombre_herramienta)
VALUES(1,'Galpón','Obra Residencial - Torre Belgrano','2026-10-02','Taladro Demo');
INSERT INTO insumos(id_insumo,nombre,id_ubicacion,consumido) VALUES(1,'Disco de corte Demo',2,false);
INSERT INTO recortes(id,id_material,medida,cantidad,ancho,altura,estado)
VALUES(1,3,0,3,1.2,1,'Buen estado');
INSERT INTO ordenes_compra(id_orden_compra,fecha,nro_orden,id_proveedor,estado,usuario_creacion,id_destino,tipo_destino)
VALUES(1,'2026-10-03','DEMO-OC-001',1,'Pendiente','admin@tesis.local',2,'UBICACION');
INSERT INTO ordenes_compra_detalle(id_orden_compra,id_material,cantidad_pedida,cantidad_recibida,cantidad_faltante,stock_al_crear)
VALUES(1,2,10,0,10,120);
INSERT INTO conteos_inventario(id_conteo,fecha_inicio,usuario_inicio,estado,observaciones)
VALUES(1,'2026-10-04','admin@tesis.local','abierto','Conteo ficticio para continuar en la demostración');
INSERT INTO conteos_inventario_detalle(id_conteo,id_material,material_nombre_snapshot,material_categoria_snapshot,cantidad_contada)
VALUES(1,1,'Perfil Montante 69mm x 2.60m','PERFILERIA',139);
INSERT INTO canaletas_plegados(id,id_obra,descripcion,precio,pagado,fecha_creacion)
VALUES(1,1,'Canaleta demo de chapa plegada',25000,false,'2026-10-03');
INSERT INTO reporte_stock_secciones(id,nombre,orden) VALUES(1,'Perfiles Demo',1),(2,'Placas Demo',2);
INSERT INTO reporte_stock_materiales(id_seccion,id_material,orden) VALUES(1,1,1),(1,2,2),(2,3,1);
-- Avanzar todas las secuencias poseídas, incluidas las columnas identity, sin adivinar nombres.
DO $sequences$
DECLARE r record; ultimo bigint;
BEGIN
 FOR r IN SELECT table_name,column_name,pg_get_serial_sequence(format('%I.%I',table_schema,table_name),column_name) AS seq
          FROM information_schema.columns WHERE table_schema='public'
 LOOP
  IF r.seq IS NOT NULL THEN
   EXECUTE format('SELECT max(%I) FROM public.%I',r.column_name,r.table_name) INTO ultimo;
   IF ultimo IS NOT NULL THEN PERFORM setval(r.seq::regclass,ultimo,true); END IF;
  END IF;
 END LOOP;
END $sequences$;
COMMIT;
