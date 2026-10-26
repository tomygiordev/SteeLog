using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using WP_Inventario.Modelos;

namespace WP_Inventario.Datos
{
    // Modelos internos para el informe
    public class InformeMovimientoDetalle
    {
        public int id_movimiento { get; set; }
        public DateTime fecha { get; set; }
        public string nroremito { get; set; }
        public string tipo_movimiento { get; set; }
        public string material { get; set; }
        public decimal cantidad { get; set; }
        public string origen { get; set; }
        public string destino { get; set; }
        public string observaciones { get; set; }
        public decimal costoflete { get; set; }
        public bool esrecorte { get; set; }
        public string medida_recorte { get; set; }
        public bool esinsumo { get; set; }
        public bool es_compra_eventual { get; set; }
        public decimal precio_unitario { get; set; }
        public decimal precio_total { get; set; }
    }

    public class InformeObraResumen
    {
        public int CantRemitos { get; set; }
        public int CantMovimientos { get; set; }
        public int TotalMateriales { get; set; }
        public decimal TotalFletes { get; set; }
        public decimal TotalPendiente { get; set; }
    }

    public class InformeHerramientaDetalle
    {
        public int id_herramienta { get; set; }
        public string nombre { get; set; }
        public string estado { get; set; }
        public DateTime? fecha_ingreso { get; set; }
        public DateTime? fecha_retorno { get; set; }
        public bool en_obra { get; set; }
    }

    public class InformeInsumoDetalle
    {
        public string nombre { get; set; }
        public bool consumido { get; set; }
        public DateTime? fecha_ingreso { get; set; }
        public DateTime? fecha_retorno { get; set; }
        public bool en_obra { get; set; }
    }

    public class InformeCanaletaPlegadoDetalle
    {
        public int id { get; set; }
        public int id_obra { get; set; }
        public string descripcion { get; set; }
        public decimal precio { get; set; }
        public bool pagado { get; set; }
        public DateTime fecha_creacion { get; set; }
    }

    public class InformeObraDatos
    {
        public static InformeObraResumen CalcularResumen(IEnumerable<InformeMovimientoDetalle> movimientos, bool incluirValorizacion, decimal totalCanaletasPendientes = 0)
        {
            var lista = (movimientos ?? Enumerable.Empty<InformeMovimientoDetalle>()).ToList();
            var resumen = new InformeObraResumen
            {
                CantRemitos = lista
                    .Where(m => !string.IsNullOrWhiteSpace(m.nroremito))
                    .Select(m => m.nroremito.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count(),
                CantMovimientos = lista.Select(m => m.id_movimiento).Distinct().Count(),
                TotalMateriales = lista
                    .Where(m => !m.esrecorte && !string.IsNullOrWhiteSpace(m.material))
                    .Select(m => m.material.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count()
            };

            if (!incluirValorizacion)
                return resumen;

            foreach (var movimiento in lista.GroupBy(m => m.id_movimiento))
            {
                string tipo = (movimiento.First().tipo_movimiento ?? "").ToUpperInvariant();
                decimal valorMovimiento = CalcularValorMovimientos(movimiento);

                if (tipo.Contains("ENVÍO") || tipo.Contains("ENVIO") || tipo.Contains("COMPRA EVENTUAL") || tipo.Contains("APORTE"))
                    resumen.TotalPendiente += valorMovimiento;
                else if (tipo.Contains("DEVOLUCIÓN") || tipo.Contains("DEVOLUCION"))
                    resumen.TotalPendiente -= valorMovimiento;
            }

            resumen.TotalFletes = lista
                .Where(m => m.costoflete > 0)
                .GroupBy(m => m.id_movimiento)
                .Select(g => g.First().costoflete)
                .Sum();
            resumen.TotalPendiente += resumen.TotalFletes;
            resumen.TotalPendiente += totalCanaletasPendientes;

            return resumen;
        }

        public static decimal CalcularValorMovimientos(IEnumerable<InformeMovimientoDetalle> movimientos)
        {
            return (movimientos ?? Enumerable.Empty<InformeMovimientoDetalle>())
                .GroupBy(m => m.id_movimiento)
                .Sum(grupo =>
                {
                    string tipo = (grupo.First().tipo_movimiento ?? "").ToUpperInvariant();
                    decimal precioTotal = grupo.Max(m => m.precio_total);
                    decimal totalDetalles = grupo.Sum(m => m.cantidad * m.precio_unitario);

                    if (tipo.Contains("COMPRA EVENTUAL") && totalDetalles == 0 && precioTotal > 0)
                        return precioTotal;

                    return totalDetalles;
                });
        }

        /// <summary>
        /// Obtiene todos los movimientos de materiales asociados a una obra,
        /// desglosados por remito (sin acumular).
        /// </summary>
        public List<InformeMovimientoDetalle> ObtenerDetalleMovimiento(int idMovimiento)
        {
            var lista = new List<InformeMovimientoDetalle>();
            ConexionDB conexionDB = new ConexionDB();

            using (var conexion = conexionDB.GetConnection())
            {
                if (conexion.State == ConnectionState.Closed) conexion.Open();

                string query = @"
                    SELECT 
                        m.id_movimiento,
                        m.fecha,
                        m.nroremito,
                        tm.nombre as tipo_movimiento,
                        COALESCE(mat.nombre, mm.material_nombre_eventual) as material_nombre,
                        mm.cantidad,
                        CASE 
                            WHEN m.tipo_origen = 'OBRA' THEN COALESCE((SELECT nombre FROM obras WHERE id = m.id_origen), '-')
                            WHEN m.tipo_origen = 'PROVEEDOR' THEN COALESCE((SELECT nombre FROM proveedores WHERE id_proveedores = m.id_origen), '-')
                            WHEN m.tipo_origen = 'UBICACION' THEN COALESCE((SELECT nombre FROM ubicacion WHERE id_ubicacion = m.id_origen), '-')
                            ELSE '-'
                        END as origen_nombre,
                        CASE 
                            WHEN m.tipo_destino = 'OBRA' THEN COALESCE((SELECT nombre FROM obras WHERE id = m.id_destino), '-')
                            WHEN m.tipo_destino = 'PROVEEDOR' THEN COALESCE((SELECT nombre FROM proveedores WHERE id_proveedores = m.id_destino), '-')
                            WHEN m.tipo_destino = 'UBICACION' THEN COALESCE((SELECT nombre FROM ubicacion WHERE id_ubicacion = m.id_destino), '-')
                            ELSE '-'
                        END as destino_nombre,
                        m.observaciones,
                        m.costoflete,
                        COALESCE(mm.esrecorte, false) as esrecorte,
                        CASE WHEN r.id IS NOT NULL THEN CONCAT(r.ancho, 'x', r.altura) ELSE NULL END as medida_recorte,
                        false as esinsumo,
                        COALESCE(mm.es_compra_eventual, false) as es_compra_eventual,
                        CASE
                            WHEN COALESCE(mm.es_compra_eventual, false) THEN COALESCE(mm.precio_unitario, 0)
                            ELSE COALESCE(mat.preciounitario, 0)
                        END as precio_unitario,
                        COALESCE(m.precio_total, 0) as precio_total
                    FROM public.movimientos m
                    INNER JOIN public.movimientomaterial mm ON m.id_movimiento = mm.id_movimiento
                    LEFT JOIN public.tipo_movimiento tm ON m.id_tipomovimiento = tm.id_tipomovimiento
                    LEFT JOIN public.materiales mat ON mm.id_material = mat.id
                    LEFT JOIN public.recortes r ON mm.id_recorte = r.id
                    WHERE m.id_movimiento = @idMov
                    
                    UNION ALL
                    
                    SELECT 
                        m.id_movimiento,
                        m.fecha,
                        m.nroremito,
                        tm.nombre as tipo_movimiento,
                        i.nombre as material_nombre,
                        1 as cantidad,
                        im.obra_origen as origen_nombre,
                        im.obra_destino as destino_nombre,
                        m.observaciones,
                        m.costoflete,
                        false as esrecorte,
                        NULL as medida_recorte,
                        true as esinsumo,
                        false as es_compra_eventual,
                        0 as precio_unitario,
                        COALESCE(m.precio_total, 0) as precio_total
                    FROM public.movimientos m
                    INNER JOIN public.insumo_movimientos im ON m.id_movimiento = im.id_movimiento
                    INNER JOIN public.insumos i ON im.id_insumo = i.id_insumo
                    LEFT JOIN public.tipo_movimiento tm ON m.id_tipomovimiento = tm.id_tipomovimiento
                    WHERE m.id_movimiento = @idMov
                    
                    ORDER BY material_nombre ASC
                ";

                using (var cmd = new NpgsqlCommand(query, conexion))
                {
                    cmd.Parameters.AddWithValue("@idMov", idMovimiento);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lista.Add(new InformeMovimientoDetalle
                            {
                                id_movimiento = reader.GetInt32(0),
                                fecha = reader.GetDateTime(1),
                                nroremito = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                tipo_movimiento = reader.IsDBNull(3) ? "-" : reader.GetString(3),
                                material = reader.GetString(4),
                                cantidad = reader.IsDBNull(5) ? 0 : reader.GetDecimal(5),
                                origen = reader.IsDBNull(6) ? "-" : reader.GetString(6),
                                destino = reader.IsDBNull(7) ? "-" : reader.GetString(7),
                                observaciones = reader.IsDBNull(8) ? "" : reader.GetString(8),
                                costoflete = reader.IsDBNull(9) ? 0 : reader.GetDecimal(9),
                                esrecorte = reader.GetBoolean(10),
                                medida_recorte = reader.IsDBNull(11) ? "" : reader.GetString(11),
                                esinsumo = reader.GetBoolean(12),
                                es_compra_eventual = reader.GetBoolean(13),
                                precio_unitario = reader.GetDecimal(14),
                                precio_total = reader.GetDecimal(15)
                            });
                        }
                    }
                }
            }
            return lista;
        }

        public List<InformeMovimientoDetalle> ObtenerMovimientosObra(int idObra)
        {
            var lista = new List<InformeMovimientoDetalle>();
            ConexionDB conexionDB = new ConexionDB();

            using (var conexion = conexionDB.GetConnection())
            {
                if (conexion.State == ConnectionState.Closed) conexion.Open();

                string query = @"
                    SELECT 
                        m.id_movimiento,
                        m.fecha,
                        m.nroremito,
                        tm.nombre as tipo_movimiento,
                        COALESCE(mat.nombre, mm.material_nombre_eventual) as material_nombre,
                        mm.cantidad,
                        CASE 
                            WHEN m.tipo_origen = 'OBRA' THEN (SELECT nombre FROM obras WHERE id = m.id_origen)
                            WHEN m.tipo_origen = 'PROVEEDOR' THEN (SELECT nombre FROM proveedores WHERE id_proveedores = m.id_origen)
                            WHEN m.tipo_origen = 'UBICACION' THEN (SELECT nombre FROM ubicacion WHERE id_ubicacion = m.id_origen)
                            ELSE '-'
                        END as origen_nombre,
                        CASE 
                            WHEN m.tipo_destino = 'OBRA' THEN (SELECT nombre FROM obras WHERE id = m.id_destino)
                            WHEN m.tipo_destino = 'PROVEEDOR' THEN (SELECT nombre FROM proveedores WHERE id_proveedores = m.id_destino)
                            WHEN m.tipo_destino = 'UBICACION' THEN (SELECT nombre FROM ubicacion WHERE id_ubicacion = m.id_destino)
                            ELSE '-'
                        END as destino_nombre,
                        m.observaciones,
                        COALESCE(mm.esrecorte, false) as esrecorte,
                        CASE WHEN r.id IS NOT NULL THEN CONCAT(r.ancho, 'x', r.altura) ELSE NULL END as medida_recorte,
                        COALESCE(mm.es_compra_eventual, false) as es_compra_eventual,
                        CASE
                            WHEN COALESCE(mm.es_compra_eventual, false) THEN COALESCE(mm.precio_unitario, 0)
                            ELSE COALESCE(mat.preciounitario, 0)
                        END as precio_unitario,
                        COALESCE(m.costoflete, 0) as costoflete,
                        COALESCE(m.precio_total, 0) as precio_total
                    FROM public.movimientos m
                    INNER JOIN public.movimientomaterial mm ON m.id_movimiento = mm.id_movimiento
                    LEFT JOIN public.tipo_movimiento tm ON m.id_tipomovimiento = tm.id_tipomovimiento
                    LEFT JOIN public.materiales mat ON mm.id_material = mat.id
                    LEFT JOIN public.recortes r ON mm.id_recorte = r.id
                    WHERE (m.tipo_destino = 'OBRA' AND m.id_destino = @idObra)
                       OR (m.tipo_origen = 'OBRA' AND m.id_origen = @idObra)
                    ORDER BY tm.nombre ASC, m.fecha ASC, m.nroremito ASC
                ";

                using (var cmd = new NpgsqlCommand(query, conexion))
                {
                    cmd.Parameters.AddWithValue("@idObra", idObra);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lista.Add(new InformeMovimientoDetalle
                            {
                                id_movimiento = reader.GetInt32(0),
                                fecha = reader.GetDateTime(1),
                                nroremito = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                tipo_movimiento = reader.IsDBNull(3) ? "-" : reader.GetString(3),
                                material = reader.GetString(4),
                                cantidad = reader.IsDBNull(5) ? 0 : reader.GetDecimal(5),
                                origen = reader.IsDBNull(6) ? "-" : reader.GetString(6),
                                destino = reader.IsDBNull(7) ? "-" : reader.GetString(7),
                                observaciones = reader.IsDBNull(8) ? "" : reader.GetString(8),
                                esrecorte = reader.GetBoolean(9),
                                medida_recorte = reader.IsDBNull(10) ? "" : reader.GetString(10),
                                es_compra_eventual = reader.GetBoolean(11),
                                precio_unitario = reader.GetDecimal(12),
                                costoflete = reader.IsDBNull(13) ? 0 : reader.GetDecimal(13),
                                precio_total = reader.IsDBNull(14) ? 0 : reader.GetDecimal(14)
                            });
                        }
                    }
                }
            }
            return lista;
        }

        /// <summary>
        /// Obtiene herramientas asociadas a la obra con sus fechas de ingreso/retorno.
        /// </summary>
        public List<InformeHerramientaDetalle> ObtenerHerramientasObra(int idObra, string nombreObra)
        {
            var lista = new List<InformeHerramientaDetalle>();
            ConexionDB conexionDB = new ConexionDB();

            using (var conexion = conexionDB.GetConnection())
            {
                if (conexion.State == ConnectionState.Closed) conexion.Open();

                // Obtener herramientas que estuvieron o están en esta obra
                string query = @"
                    SELECT DISTINCT h.id_herramienta, h.nombre, h.estado, h.id_obra,
                        (SELECT MIN(hm.fecha) FROM herramienta_movimientos hm 
                         WHERE hm.id_herramienta = h.id_herramienta 
                         AND hm.obra_destino = @nombreObra) as fecha_ingreso,
                        (SELECT MAX(hm.fecha) FROM herramienta_movimientos hm 
                         WHERE hm.id_herramienta = h.id_herramienta 
                         AND hm.obra_origen = @nombreObra) as fecha_retorno
                    FROM herramientas h
                    INNER JOIN herramienta_movimientos hm ON h.id_herramienta = hm.id_herramienta
                    WHERE hm.obra_destino = @nombreObra OR hm.obra_origen = @nombreObra
                    ORDER BY h.nombre ASC
                ";

                using (var cmd = new NpgsqlCommand(query, conexion))
                {
                    cmd.Parameters.AddWithValue("@nombreObra", nombreObra);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int? idObraActual = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3);
                            DateTime? fechaIngreso = reader.IsDBNull(4) ? (DateTime?)null : reader.GetDateTime(4);
                            DateTime? fechaRetorno = reader.IsDBNull(5) ? (DateTime?)null : reader.GetDateTime(5);
                            
                            bool enObra = idObraActual.HasValue && idObraActual.Value == idObra;

                            lista.Add(new InformeHerramientaDetalle
                            {
                                id_herramienta = reader.GetInt32(0),
                                nombre = reader.GetString(1),
                                estado = reader.GetString(2),
                                fecha_ingreso = fechaIngreso,
                                fecha_retorno = enObra ? null : fechaRetorno,
                                en_obra = enObra
                            });
                        }
                    }
                }
            }
            return lista;
        }

        /// <summary>
        /// Obtiene insumos asociados a la obra con sus fechas de ingreso/retorno.
        /// </summary>
        public List<InformeInsumoDetalle> ObtenerInsumosObra(int idObra, string nombreObra)
        {
            var lista = new List<InformeInsumoDetalle>();
            ConexionDB conexionDB = new ConexionDB();

            using (var conexion = conexionDB.GetConnection())
            {
                if (conexion.State == ConnectionState.Closed) conexion.Open();

                string query = @"
                    SELECT DISTINCT i.nombre, i.consumido, i.id_obra,
                        (SELECT MIN(im.fecha) FROM insumo_movimientos im 
                         WHERE im.id_insumo = i.id_insumo 
                         AND im.obra_destino = @nombreObra) as fecha_ingreso,
                        (SELECT MAX(im.fecha) FROM insumo_movimientos im 
                         WHERE im.id_insumo = i.id_insumo 
                         AND im.obra_origen = @nombreObra) as fecha_retorno
                    FROM insumos i
                    INNER JOIN insumo_movimientos im ON i.id_insumo = im.id_insumo
                    WHERE im.obra_destino = @nombreObra OR im.obra_origen = @nombreObra
                    ORDER BY i.nombre ASC
                ";

                using (var cmd = new NpgsqlCommand(query, conexion))
                {
                    cmd.Parameters.AddWithValue("@nombreObra", nombreObra);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int? idObraActual = reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2);
                            DateTime? fechaIngreso = reader.IsDBNull(3) ? (DateTime?)null : reader.GetDateTime(3);
                            DateTime? fechaRetorno = reader.IsDBNull(4) ? (DateTime?)null : reader.GetDateTime(4);

                            bool enObra = idObraActual.HasValue && idObraActual.Value == idObra;

                            lista.Add(new InformeInsumoDetalle
                            {
                                nombre = reader.GetString(0),
                                consumido = reader.GetBoolean(1),
                                fecha_ingreso = fechaIngreso,
                                fecha_retorno = enObra ? null : fechaRetorno,
                                en_obra = enObra
                            });
                        }
                    }
                }
            }
            return lista;
        }

        public List<InformeCanaletaPlegadoDetalle> ObtenerCanaletasPlegadosObra(int idObra)
        {
            var lista = new List<InformeCanaletaPlegadoDetalle>();
            ConexionDB conexionDB = new ConexionDB();

            using (var conexion = conexionDB.GetConnection())
            {
                if (conexion.State == ConnectionState.Closed) conexion.Open();

                const string query = @"
                    SELECT id, id_obra, descripcion, precio, pagado, fecha_creacion
                    FROM public.canaletas_plegados
                    WHERE id_obra = @idObra
                    ORDER BY fecha_creacion DESC, id DESC
                ";

                using (var cmd = new NpgsqlCommand(query, conexion))
                {
                    cmd.Parameters.AddWithValue("@idObra", idObra);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lista.Add(new InformeCanaletaPlegadoDetalle
                            {
                                id = reader.GetInt32(0),
                                id_obra = reader.GetInt32(1),
                                descripcion = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                precio = reader.IsDBNull(3) ? 0 : reader.GetDecimal(3),
                                pagado = reader.IsDBNull(4) ? false : reader.GetBoolean(4),
                                fecha_creacion = reader.IsDBNull(5) ? DateTime.MinValue : reader.GetDateTime(5)
                            });
                        }
                    }
                }
            }

            return lista;
        }

        /// <summary>
        /// Obtiene la fecha del primer remito asociado a la obra.
        /// </summary>
        public DateTime? ObtenerPrimeraFechaRemitos(int idObra)
        {
            ConexionDB conexionDB = new ConexionDB();
            using (var conexion = conexionDB.GetConnection())
            {
                if (conexion.State == ConnectionState.Closed) conexion.Open();

                string query = @"
                    SELECT MIN(m.fecha)
                    FROM movimientos m
                    WHERE ((m.tipo_destino = 'OBRA' AND m.id_destino = @idObra)
                       OR (m.tipo_origen = 'OBRA' AND m.id_origen = @idObra))
                      AND m.nroremito IS NOT NULL AND m.nroremito <> ''
                ";

                using (var cmd = new NpgsqlCommand(query, conexion))
                {
                    cmd.Parameters.AddWithValue("@idObra", idObra);
                    var result = cmd.ExecuteScalar();
                    return result != null && result != DBNull.Value
                        ? Convert.ToDateTime(result)
                        : (DateTime?)null;
                }
            }
        }

        /// <summary>
        /// Cuenta la cantidad de remitos únicos asociados a la obra.
        /// </summary>
        public int ContarRemitos(int idObra)
        {
            ConexionDB conexionDB = new ConexionDB();
            using (var conexion = conexionDB.GetConnection())
            {
                if (conexion.State == ConnectionState.Closed) conexion.Open();

                string query = @"
                    SELECT COUNT(DISTINCT m.nroremito) 
                    FROM movimientos m 
                    WHERE ((m.tipo_destino = 'OBRA' AND m.id_destino = @idObra)
                       OR (m.tipo_origen = 'OBRA' AND m.id_origen = @idObra))
                      AND m.nroremito IS NOT NULL AND m.nroremito <> ''
                ";

                using (var cmd = new NpgsqlCommand(query, conexion))
                {
                    cmd.Parameters.AddWithValue("@idObra", idObra);
                    var result = cmd.ExecuteScalar();
                    return result != null ? Convert.ToInt32(result) : 0;
                }
            }
        }
    }
}
