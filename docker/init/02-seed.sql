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
(8, 'Uso interno')
ON CONFLICT (id_tipomovimiento) DO NOTHING;

-- Ubicaciones estándar
INSERT INTO public.ubicacion (id_ubicacion, nombre) VALUES
(1, '-'),
(2, 'Depósito Central'),
(3, 'Proveedor'),
(4, 'Cliente/Consumo')
ON CONFLICT (id_ubicacion) DO NOTHING;

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
(1, 'Obra Residencial - Torre Belgrano', 'Av. Belgrano 1240', true),
(2, 'Obra Comercial - Galería San Martín', 'San Martín 550', true),
(3, 'Obra Nave Industrial - Parque Logístico', 'Ruta 11 Km 15', true)
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
