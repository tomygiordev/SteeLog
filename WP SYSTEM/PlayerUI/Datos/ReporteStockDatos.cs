using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using WP_Inventario.Modelos;

namespace WP_Inventario.Datos
{
    public class ReporteStockDatos
    {
        public List<ReporteStockSeccion> ObtenerSecciones()
        {
            var secciones = new List<ReporteStockSeccion>();
            using (var conexion = new ConexionDB().GetConnection())
            using (var cmd = new NpgsqlCommand(
                "SELECT id, nombre, orden FROM reporte_stock_secciones ORDER BY orden, nombre", conexion))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    secciones.Add(new ReporteStockSeccion
                    {
                        id = reader.GetInt32(0),
                        nombre = reader.GetString(1),
                        orden = reader.GetInt32(2)
                    });
                }
            }
            return secciones;
        }

        public List<ReporteStockMaterialConfigurado> ObtenerMaterialesSeccion(int idSeccion)
        {
            var materiales = new List<ReporteStockMaterialConfigurado>();
            using (var conexion = new ConexionDB().GetConnection())
            using (var cmd = new NpgsqlCommand(@"
                SELECT id_material, orden
                FROM reporte_stock_materiales
                WHERE id_seccion = @idSeccion
                ORDER BY orden", conexion))
            {
                cmd.Parameters.AddWithValue("@idSeccion", idSeccion);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        materiales.Add(new ReporteStockMaterialConfigurado
                        {
                            idMaterial = reader.GetInt32(0),
                            orden = reader.GetInt32(1)
                        });
                    }
                }
            }
            return materiales;
        }

        public int GuardarSeccion(int id, string nombre, int orden)
        {
            using (var conexion = new ConexionDB().GetConnection())
            using (var cmd = new NpgsqlCommand(id == 0
                ? "INSERT INTO reporte_stock_secciones(nombre, orden) VALUES(@nombre, @orden) RETURNING id"
                : "UPDATE reporte_stock_secciones SET nombre=@nombre, orden=@orden WHERE id=@id RETURNING id", conexion))
            {
                cmd.Parameters.AddWithValue("@nombre", nombre.Trim());
                cmd.Parameters.AddWithValue("@orden", orden);
                if (id != 0) cmd.Parameters.AddWithValue("@id", id);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public void EliminarSeccion(int id)
        {
            using (var conexion = new ConexionDB().GetConnection())
            using (var transaccion = conexion.BeginTransaction())
            {
                using (var borrarMateriales = new NpgsqlCommand(
                    "DELETE FROM reporte_stock_materiales WHERE id_seccion=@id", conexion, transaccion))
                {
                    borrarMateriales.Parameters.AddWithValue("@id", id);
                    borrarMateriales.ExecuteNonQuery();
                }

                using (var borrarSeccion = new NpgsqlCommand(
                    "DELETE FROM reporte_stock_secciones WHERE id=@id", conexion, transaccion))
                {
                    borrarSeccion.Parameters.AddWithValue("@id", id);
                    borrarSeccion.ExecuteNonQuery();
                }

                transaccion.Commit();
            }
        }

        public void GuardarMaterialesSeccion(int idSeccion, IEnumerable<ReporteStockMaterialConfigurado> materiales)
        {
            using (var conexion = new ConexionDB().GetConnection())
            using (var transaccion = conexion.BeginTransaction())
            {
                using (var borrar = new NpgsqlCommand(
                    "DELETE FROM reporte_stock_materiales WHERE id_seccion=@idSeccion", conexion, transaccion))
                {
                    borrar.Parameters.AddWithValue("@idSeccion", idSeccion);
                    borrar.ExecuteNonQuery();
                }

                foreach (var material in materiales)
                {
                    using (var insertar = new NpgsqlCommand(@"
                        INSERT INTO reporte_stock_materiales(id_seccion, id_material, orden)
                        VALUES(@idSeccion, @idMaterial, @orden)
                        ON CONFLICT (id_material)
                        DO UPDATE SET id_seccion=EXCLUDED.id_seccion, orden=EXCLUDED.orden", conexion, transaccion))
                    {
                        insertar.Parameters.AddWithValue("@idSeccion", idSeccion);
                        insertar.Parameters.AddWithValue("@idMaterial", material.idMaterial);
                        insertar.Parameters.AddWithValue("@orden", material.orden);
                        insertar.ExecuteNonQuery();
                    }
                }

                transaccion.Commit();
            }
        }

        public List<Material> ObtenerMaterialesReporte(string nombre = null, bool soloStockBajo = false)
        {
            var materiales = new List<Material>();
            using (var conexion = new ConexionDB().GetConnection())
            {
                string sql = @"
                    SELECT m.id, m.nombre, m.preciounitario, m.stockactual, m.stockminimo,
                           m.id_categoria, c.nombre, s.nombre, s.orden, rm.orden,
                           COALESCE(SUM(ABS(mm.cantidad)), 0), COUNT(mm.id_movimientomaterial)
                    FROM reporte_stock_materiales rm
                    INNER JOIN reporte_stock_secciones s ON s.id = rm.id_seccion
                    INNER JOIN materiales m ON m.id = rm.id_material
                    INNER JOIN categorias c ON c.id_categoria = m.id_categoria
                    LEFT JOIN movimientomaterial mm ON mm.id_material = m.id
                    WHERE 1=1";

                if (!string.IsNullOrWhiteSpace(nombre))
                    sql += " AND m.nombre ILIKE @nombre";
                if (soloStockBajo)
                    sql += " AND m.stockminimo > 0 AND m.stockactual <= m.stockminimo";

                sql += @"
                    GROUP BY m.id, m.nombre, m.preciounitario, m.stockactual, m.stockminimo,
                             m.id_categoria, c.nombre, s.nombre, s.orden, rm.orden
                    ORDER BY s.orden, s.nombre, rm.orden, m.nombre";

                using (var cmd = new NpgsqlCommand(sql, conexion))
                {
                    if (!string.IsNullOrWhiteSpace(nombre))
                        cmd.Parameters.AddWithValue("@nombre", "%" + nombre.Trim() + "%");

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            materiales.Add(new Material
                            {
                                id = reader.GetInt32(0),
                                nombre = reader.GetString(1),
                                precioUnitario = reader.GetDecimal(2),
                                stockActual = reader.GetDecimal(3),
                                stockMinimo = reader.GetDecimal(4),
                                id_categoria = reader.GetInt32(5),
                                nombre_categoria = reader.GetString(6),
                                reporteSeccion = reader.GetString(7),
                                reporteOrdenSeccion = reader.GetInt32(8),
                                reporteOrdenMaterial = reader.GetInt32(9),
                                cantidadUsada = reader.GetDecimal(10),
                                cantidadMovimientos = Convert.ToInt32(reader.GetInt64(11))
                            });
                        }
                    }
                }
            }
            return materiales;
        }

        public Dictionary<int, ReporteStockMapeo> ObtenerMapeoSecciones()
        {
            var mapeo = new Dictionary<int, ReporteStockMapeo>();
            try
            {
                using (var conexion = new ConexionDB().GetConnection())
                using (var cmd = new NpgsqlCommand(@"
                    SELECT rm.id_material, s.nombre, s.orden, rm.orden
                    FROM reporte_stock_materiales rm
                    INNER JOIN reporte_stock_secciones s ON s.id = rm.id_seccion", conexion))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        mapeo[reader.GetInt32(0)] = new ReporteStockMapeo
                        {
                            nombreSeccion = reader.GetString(1),
                            ordenSeccion = reader.GetInt32(2),
                            ordenMaterial = reader.GetInt32(3)
                        };
                    }
                }
            }
            catch (PostgresException ex) when (ex.SqlState == "42P01")
            {
            }
            return mapeo;
        }

        public bool TieneConfiguracion()
        {
            try
            {
                using (var conexion = new ConexionDB().GetConnection())
                using (var cmd = new NpgsqlCommand(@"
                    SELECT EXISTS(
                        SELECT 1
                        FROM reporte_stock_secciones s
                        INNER JOIN reporte_stock_materiales rm ON rm.id_seccion = s.id
                    )", conexion))
                    return Convert.ToBoolean(cmd.ExecuteScalar());
            }
            catch (PostgresException ex) when (ex.SqlState == "42P01")
            {
                return false;
            }
        }
    }
}
