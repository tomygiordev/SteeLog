using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;

namespace WP_Inventario.Datos
{
    // =================== MODELOS DE INVENTARIO ===================
    public class InventarioMaterial
    {
        public int id { get; set; }
        public string codigo { get; set; }
        public string nombre { get; set; }
        public string categoria { get; set; }
        public string unidad { get; set; }
        public decimal stock_actual { get; set; }
        public decimal stock_minimo { get; set; }
        public decimal precio_unitario { get; set; }
        public string estado_stock { get; set; }
        public string proveedores { get; set; }
    }

    public class InventarioRecorte
    {
        public int id { get; set; }
        public string material { get; set; }
        public string categoria { get; set; }
        public decimal ancho { get; set; }
        public decimal altura { get; set; }
        public decimal medida { get; set; }
        public decimal cantidad { get; set; }
        public DateTime fecha_creacion { get; set; }
    }

    public class InventarioHerramienta
    {
        public int id { get; set; }
        public string nombre { get; set; }
        public string estado { get; set; }
        public string obra { get; set; }
        public string observaciones { get; set; }
    }

    public class InventarioInsumo
    {
        public int id { get; set; }
        public string nombre { get; set; }
        public string obra { get; set; }
        public bool consumido { get; set; }
        public string estado { get; set; }
        public string observaciones { get; set; }
    }

    public class ResumenInventario
    {
        public int total_materiales { get; set; }
        public int materiales_sin_stock { get; set; }
        public int materiales_stock_bajo { get; set; }
        public int total_recortes { get; set; }
        public int total_herramientas { get; set; }
        public int herramientas_en_obra { get; set; }
        public int total_insumos { get; set; }
        public int insumos_en_obra { get; set; }
    }

    // =================== DATOS ===================
    public class InventarioDatos
    {
        public ResumenInventario ObtenerResumen()
        {
            var resumen = new ResumenInventario();
            ConexionDB conexionDB = new ConexionDB();

            using (var conexion = conexionDB.GetConnection())
            {
                if (conexion.State == ConnectionState.Closed) conexion.Open();

                // Materiales
                using (var cmd = new NpgsqlCommand(@"
                    SELECT 
                        COUNT(*),
                        COUNT(CASE WHEN stockactual <= 0 THEN 1 END),
                        COUNT(CASE WHEN stockactual > 0 AND stockminimo > 0 AND stockactual <= stockminimo THEN 1 END)
                    FROM materiales", conexion))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            resumen.total_materiales = reader.GetInt32(0);
                            resumen.materiales_sin_stock = reader.GetInt32(1);
                            resumen.materiales_stock_bajo = reader.GetInt32(2);
                        }
                    }
                }

                // Recortes
                using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM recortes WHERE cantidad > 0", conexion))
                {
                    resumen.total_recortes = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
                }

                // Herramientas
                using (var cmd = new NpgsqlCommand(@"
                    SELECT 
                        COUNT(*),
                        COUNT(CASE WHEN id_obra IS NOT NULL THEN 1 END)
                    FROM herramientas", conexion))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            resumen.total_herramientas = reader.GetInt32(0);
                            resumen.herramientas_en_obra = reader.GetInt32(1);
                        }
                    }
                }

                // Insumos
                using (var cmd = new NpgsqlCommand(@"
                    SELECT 
                        COUNT(*),
                        COUNT(CASE WHEN id_obra IS NOT NULL AND consumido = false THEN 1 END)
                    FROM insumos", conexion))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            resumen.total_insumos = reader.GetInt32(0);
                            resumen.insumos_en_obra = reader.GetInt32(1);
                        }
                    }
                }
            }
            return resumen;
        }

        public List<InventarioMaterial> ObtenerMateriales(string busqueda = "", string categoria = "", string estadoStock = "")
        {
            var lista = new List<InventarioMaterial>();
            ConexionDB conexionDB = new ConexionDB();

            using (var conexion = conexionDB.GetConnection())
            {
                if (conexion.State == ConnectionState.Closed) conexion.Open();

                string query = @"
                    SELECT m.id, m.nombre, c.nombre as categoria,
                           m.stockactual, m.stockminimo,
                           COALESCE(m.preciounitario, 0) as precio_unitario,
                           COALESCE(
                               (SELECT string_agg(p.nombre, ', ')
                                FROM materiales_proveedores mp
                                JOIN proveedores p ON mp.id_proveedor = p.id_proveedores
                                WHERE mp.id_material = m.id), '-') as proveedores,
                           m.codigo
                    FROM materiales m
                    INNER JOIN categorias c ON m.id_categoria = c.id_categoria
                    WHERE 1=1 ";

                if (!string.IsNullOrEmpty(busqueda))
                    query += " AND (LOWER(m.nombre) LIKE @busq OR LOWER(c.nombre) LIKE @busq)";

                if (!string.IsNullOrEmpty(categoria) && categoria != "TODAS")
                    query += " AND c.nombre = @cat";

                if (estadoStock == "SIN STOCK")
                    query += " AND m.stockactual <= 0";
                else if (estadoStock == "STOCK BAJO")
                    query += " AND m.stockminimo > 0 AND m.stockactual > 0 AND m.stockactual <= m.stockminimo";
                else if (estadoStock == "CON STOCK")
                    query += " AND m.stockactual > 0";

                query += " ORDER BY m.nombre ASC";

                using (var cmd = new NpgsqlCommand(query, conexion))
                {
                    if (!string.IsNullOrEmpty(busqueda))
                        cmd.Parameters.AddWithValue("@busq", "%" + busqueda.ToLower() + "%");
                    if (!string.IsNullOrEmpty(categoria) && categoria != "TODAS")
                        cmd.Parameters.AddWithValue("@cat", categoria);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            decimal stockAct = reader.GetDecimal(3);
                            decimal stockMin = reader.GetDecimal(4);
                            decimal precioUnitario = reader.GetDecimal(5);
                            string estado = stockAct <= 0 ? "❌ Sin stock" :
                                           (stockMin > 0 && stockAct <= stockMin) ? "⚠️ Stock bajo" : "✓ OK";

                            lista.Add(new InventarioMaterial
                            {
                                id = reader.GetInt32(0),
                                nombre = reader.GetString(1),
                                categoria = reader.GetString(2),
                                stock_actual = stockAct,
                                stock_minimo = stockMin,
                                precio_unitario = precioUnitario,
                                estado_stock = estado,
                                proveedores = reader.GetString(6),
                                codigo = reader.IsDBNull(7) ? "" : reader.GetString(7)
                            });
                        }
                    }
                }
            }
            return lista;
        }

        public List<InventarioRecorte> ObtenerRecortes(string busqueda = "", string categoria = "")
        {
            var lista = new List<InventarioRecorte>();
            ConexionDB conexionDB = new ConexionDB();

            using (var conexion = conexionDB.GetConnection())
            {
                if (conexion.State == ConnectionState.Closed) conexion.Open();

                string query = @"
                    SELECT r.id, m.nombre, c.nombre as categoria, r.ancho, r.altura, r.medida, r.cantidad, r.fecha_creacion
                    FROM recortes r
                    INNER JOIN materiales m ON r.id_material = m.id
                    INNER JOIN categorias c ON m.id_categoria = c.id_categoria
                    WHERE r.cantidad > 0 ";

                if (!string.IsNullOrEmpty(busqueda))
                    query += " AND (LOWER(m.nombre) LIKE @busq OR LOWER(c.nombre) LIKE @busq)";
                if (!string.IsNullOrEmpty(categoria) && categoria != "TODAS")
                    query += " AND c.nombre = @cat";

                query += " ORDER BY m.nombre ASC, r.ancho ASC, r.altura ASC, r.medida ASC";

                using (var cmd = new NpgsqlCommand(query, conexion))
                {
                    if (!string.IsNullOrEmpty(busqueda))
                        cmd.Parameters.AddWithValue("@busq", "%" + busqueda.ToLower() + "%");
                    if (!string.IsNullOrEmpty(categoria) && categoria != "TODAS")
                        cmd.Parameters.AddWithValue("@cat", categoria);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lista.Add(new InventarioRecorte
                            {
                                id = reader.GetInt32(0),
                                material = reader.GetString(1),
                                categoria = reader.GetString(2),
                                ancho = reader.IsDBNull(3) ? 0 : reader.GetDecimal(3),
                                altura = reader.IsDBNull(4) ? 0 : reader.GetDecimal(4),
                                medida = reader.IsDBNull(5) ? 0 : reader.GetDecimal(5),
                                cantidad = reader.IsDBNull(6) ? 0 : reader.GetDecimal(6),
                                fecha_creacion = reader.IsDBNull(7) ? DateTime.MinValue : reader.GetDateTime(7)
                            });
                        }
                    }
                }
            }
            return lista;
        }

        public List<InventarioHerramienta> ObtenerHerramientas(string busqueda = "", string estadoFiltro = "", string obraFiltro = "")
        {
            var lista = new List<InventarioHerramienta>();
            ConexionDB conexionDB = new ConexionDB();

            using (var conexion = conexionDB.GetConnection())
            {
                if (conexion.State == ConnectionState.Closed) conexion.Open();

                string query = @"
                    SELECT h.id_herramienta, h.nombre, h.estado,
                           COALESCE(o.nombre, '-') as obra,
                           COALESCE(h.observaciones, '') as observaciones
                    FROM herramientas h
                    LEFT JOIN obras o ON h.id_obra = o.id
                    WHERE 1=1 ";

                if (!string.IsNullOrEmpty(busqueda))
                    query += " AND (LOWER(h.nombre) LIKE @busq OR LOWER(COALESCE(h.observaciones,'')) LIKE @busq)";
                if (!string.IsNullOrEmpty(estadoFiltro) && estadoFiltro != "TODOS")
                {
                    if (estadoFiltro == "EN OBRA")
                        query += " AND h.id_obra IS NOT NULL";
                    else if (estadoFiltro == "DISPONIBLE")
                        query += " AND h.id_obra IS NULL";
                }
                if (!string.IsNullOrEmpty(obraFiltro) && obraFiltro != "TODAS")
                    query += " AND o.nombre = @obraFiltro";

                query += " ORDER BY h.nombre ASC";

                using (var cmd = new NpgsqlCommand(query, conexion))
                {
                    if (!string.IsNullOrEmpty(busqueda))
                        cmd.Parameters.AddWithValue("@busq", "%" + busqueda.ToLower() + "%");
                    if (!string.IsNullOrEmpty(obraFiltro) && obraFiltro != "TODAS")
                        cmd.Parameters.AddWithValue("@obraFiltro", obraFiltro);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lista.Add(new InventarioHerramienta
                            {
                                id = reader.GetInt32(0),
                                nombre = reader.GetString(1),
                                estado = reader.GetString(2),
                                obra = reader.GetString(3),
                                observaciones = reader.GetString(4)
                            });
                        }
                    }
                }
            }
            return lista;
        }

        public List<InventarioInsumo> ObtenerInsumos(string busqueda = "", string estadoFiltro = "", string obraFiltro = "")
        {
            var lista = new List<InventarioInsumo>();
            ConexionDB conexionDB = new ConexionDB();

            using (var conexion = conexionDB.GetConnection())
            {
                if (conexion.State == ConnectionState.Closed) conexion.Open();

                string query = @"
                    SELECT i.id_insumo, i.nombre, 
                           COALESCE(o.nombre, '-') as obra, 
                           i.consumido,
                           COALESCE(i.observaciones, '') as observaciones
                    FROM insumos i
                    LEFT JOIN obras o ON i.id_obra = o.id
                    WHERE 1=1 ";

                if (!string.IsNullOrEmpty(busqueda))
                    query += " AND (LOWER(i.nombre) LIKE @busq OR LOWER(COALESCE(i.observaciones,'')) LIKE @busq)";
                if (!string.IsNullOrEmpty(estadoFiltro) && estadoFiltro != "TODOS")
                {
                    if (estadoFiltro == "EN OBRA")
                        query += " AND i.id_obra IS NOT NULL AND i.consumido = false";
                    else if (estadoFiltro == "CONSUMIDO")
                        query += " AND i.consumido = true";
                    else if (estadoFiltro == "DISPONIBLE")
                        query += " AND i.id_obra IS NULL AND i.consumido = false";
                }
                if (!string.IsNullOrEmpty(obraFiltro) && obraFiltro != "TODAS")
                    query += " AND o.nombre = @obraFiltro";

                query += " ORDER BY i.nombre ASC";

                using (var cmd = new NpgsqlCommand(query, conexion))
                {
                    if (!string.IsNullOrEmpty(busqueda))
                        cmd.Parameters.AddWithValue("@busq", "%" + busqueda.ToLower() + "%");
                    if (!string.IsNullOrEmpty(obraFiltro) && obraFiltro != "TODAS")
                        cmd.Parameters.AddWithValue("@obraFiltro", obraFiltro);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            bool consumido = reader.GetBoolean(3);
                            bool enObra = !reader.IsDBNull(2) && reader.GetString(2) != "-";
                            string estado = consumido ? "CONSUMIDO" : (enObra ? "EN OBRA" : "DISPONIBLE");

                            lista.Add(new InventarioInsumo
                            {
                                id = reader.GetInt32(0),
                                nombre = reader.GetString(1),
                                obra = reader.GetString(2),
                                consumido = consumido,
                                estado = estado,
                                observaciones = reader.GetString(4)
                            });
                        }
                    }
                }
            }
            return lista;
        }

        public List<string> ObtenerCategorias()
        {
            var lista = new List<string> { "TODAS" };
            ConexionDB conexionDB = new ConexionDB();
            using (var conexion = conexionDB.GetConnection())
            {
                if (conexion.State == ConnectionState.Closed) conexion.Open();
                using (var cmd = new NpgsqlCommand("SELECT nombre FROM categorias ORDER BY nombre ASC", conexion))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        lista.Add(reader.GetString(0));
                }
            }
            return lista;
        }

        public List<string> ObtenerObrasActivas()
        {
            var lista = new List<string> { "TODAS" };
            ConexionDB conexionDB = new ConexionDB();
            using (var conexion = conexionDB.GetConnection())
            {
                if (conexion.State == ConnectionState.Closed) conexion.Open();
                using (var cmd = new NpgsqlCommand("SELECT nombre FROM obras WHERE activa = true ORDER BY nombre ASC", conexion))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        lista.Add(reader.GetString(0));
                }
            }
            return lista;
        }

        public List<string> ObtenerObrasConHerramientas()
        {
            var lista = new List<string> { "TODAS" };
            ConexionDB conexionDB = new ConexionDB();
            using (var conexion = conexionDB.GetConnection())
            {
                if (conexion.State == ConnectionState.Closed) conexion.Open();
                using (var cmd = new NpgsqlCommand(@"
                    SELECT DISTINCT o.nombre
                    FROM obras o
                    INNER JOIN herramientas h ON h.id_obra = o.id
                    ORDER BY o.nombre ASC", conexion))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        lista.Add(reader.GetString(0));
                }
            }
            return lista;
        }
    }
}
