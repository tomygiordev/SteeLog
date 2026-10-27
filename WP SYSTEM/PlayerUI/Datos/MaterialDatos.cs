using Npgsql;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using WP_Inventario.Modelos;

namespace WP_Inventario.Datos
{
    public class MaterialDatos
    {
        public MaterialDatos() { }
        public int guardarMaterial(
            string nombre, decimal stockActual, decimal stockMinimo, int id_categoria, List<int> idsProveedores = null
            )
        {
            ConexionDB con = new ConexionDB();
            using(var cone = con.GetConnection())
            {
                using (var tx = cone.BeginTransaction())
                {
                    try
                    {
                        string sql = "INSERT INTO materiales" +
                            "(nombre,precioUnitario,stockActual,stockMinimo,id_categoria) " +
                            "VALUES(@nom,0,@stact,@stmin,@idCat) RETURNING id";
                        int idMaterial;
                        using(var cmd=new NpgsqlCommand(sql,cone,tx))
                        {
                            cmd.Parameters.AddWithValue("@nom", nombre);
                            cmd.Parameters.AddWithValue("@stact", stockActual);
                            cmd.Parameters.AddWithValue("@stmin", stockMinimo);
                            cmd.Parameters.AddWithValue("@idCat", id_categoria);
                            object result = cmd.ExecuteScalar();
                            idMaterial = result != null && result != DBNull.Value ? Convert.ToInt32(result) : 0;
                        }

                        if (idMaterial > 0 && idsProveedores != null && idsProveedores.Count > 0)
                        {
                            GuardarProveedoresMaterial(idMaterial, idsProveedores, cone, tx);
                        }

                        tx.Commit();
                        return idMaterial;
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }
        public void editarMaterial(string nombre, decimal stockActual, decimal stockMinimo, int id_categoria, int id, List<int> idsProveedores = null)
        {
            ConexionDB con = new ConexionDB();
            using(var cone = con.GetConnection())
            {
                using (var tx = cone.BeginTransaction())
                {
                    try
                    {
                        string sql = "UPDATE materiales SET " +
                            "nombre=@nom, stockActual=@stact," +
                            "stockMinimo=@stmin, id_categoria=@idCat " +
                            "WHERE id = @id_mat";
                        using(var cmd=new NpgsqlCommand(sql, cone, tx))
                        {
                            cmd.Parameters.AddWithValue("@nom", nombre);
                            cmd.Parameters.AddWithValue("@stact", stockActual);
                            cmd.Parameters.AddWithValue("@stmin", stockMinimo);
                            cmd.Parameters.AddWithValue("@idCat", id_categoria);
                            cmd.Parameters.AddWithValue("@id_mat", id);

                            cmd.ExecuteNonQuery();
                        }

                        if (idsProveedores != null)
                        {
                            GuardarProveedoresMaterial(id, idsProveedores, cone, tx);
                        }

                        tx.Commit();
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }
        public void eliminarMaterial(int id)
        {
            ConexionDB con = new ConexionDB();
            using(var cone = con.GetConnection())
            {
                string sql = "DELETE FROM materiales " +
                    "WHERE id=@idMat";
                using (var cmd = new NpgsqlCommand(sql, cone))
                {
                    cmd.Parameters.AddWithValue("@idMat", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }
        public List<Material> obtenerMateriales(string nombre = null, bool soloStockBajo = false)
        {
            List<Material> materiales = new List<Material>();
            ConexionDB con = new ConexionDB();
            using (var cone = con.GetConnection())
            {
                var condiciones = new System.Collections.Generic.List<string>();
                if (!string.IsNullOrEmpty(nombre))
                    condiciones.Add("m.nombre ILIKE @nombre");
                if (soloStockBajo)
                    condiciones.Add("m.stockminimo > 0 AND m.stockactual <= m.stockminimo");

                string where = condiciones.Count > 0 ? "WHERE " + string.Join(" AND ", condiciones) : "";

                string sql = "SELECT m.id, m.nombre, m.preciounitario, " +
                    "m.stockactual, m.stockminimo, m.id_categoria, " +
                    "c.nombre as nombre_categoria " +
                    "FROM materiales m " +
                    "INNER JOIN categorias c ON m.id_categoria=c.id_categoria " +
                    where +
                    " ORDER BY m.nombre ASC";

                using (var cmd = new NpgsqlCommand(sql, cone))
                {
                    if (!string.IsNullOrEmpty(nombre))
                        cmd.Parameters.AddWithValue("@nombre", "%" + nombre + "%");

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
                                nombre_categoria = reader.GetString(6)
                            });
                        }
                    }
                }
            }
            return materiales;
        }

        public List<Material> obtenerMateriales(FiltroMaterialParams filtro)
        {
            if (filtro == null) return obtenerMateriales();

            List<Material> materiales = new List<Material>();
            ConexionDB con = new ConexionDB();
            using (var cone = con.GetConnection())
            {
                var condiciones = new List<string>();
                if (!string.IsNullOrEmpty(filtro.Nombre))     condiciones.Add("m.nombre ILIKE @nombre");
                if (filtro.DesdeCantidad.HasValue)            condiciones.Add("m.stockactual >= @desdeCantidad");
                if (filtro.HastaCantidad.HasValue)            condiciones.Add("m.stockactual <= @hastaCantidad");
                if (filtro.SoloStockBajo)                     condiciones.Add("m.stockminimo > 0 AND m.stockactual <= m.stockminimo");
                if (filtro.IdsCategoria.Count > 0)            condiciones.Add("m.id_categoria = ANY(@idsCategoria)");
                if (filtro.IdsProveedor.Count > 0)            condiciones.Add("m.id IN (SELECT id_material FROM materiales_proveedores WHERE id_proveedor = ANY(@idsProveedor))");

                string where = condiciones.Count > 0 ? "WHERE " + string.Join(" AND ", condiciones) : "";

                string sql = "SELECT m.id, m.nombre, m.preciounitario, " +
                    "m.stockactual, m.stockminimo, m.id_categoria, " +
                    "c.nombre as nombre_categoria " +
                    "FROM materiales m " +
                    "INNER JOIN categorias c ON m.id_categoria=c.id_categoria " +
                    where + " ORDER BY m.nombre ASC";

                using (var cmd = new NpgsqlCommand(sql, cone))
                {
                    if (!string.IsNullOrEmpty(filtro.Nombre))  cmd.Parameters.AddWithValue("@nombre", "%" + filtro.Nombre + "%");
                    if (filtro.DesdeCantidad.HasValue)         cmd.Parameters.AddWithValue("@desdeCantidad", filtro.DesdeCantidad.Value);
                    if (filtro.HastaCantidad.HasValue)         cmd.Parameters.AddWithValue("@hastaCantidad", filtro.HastaCantidad.Value);
                    if (filtro.IdsCategoria.Count > 0)         cmd.Parameters.AddWithValue("@idsCategoria", filtro.IdsCategoria.ToArray());
                    if (filtro.IdsProveedor.Count > 0)         cmd.Parameters.AddWithValue("@idsProveedor", filtro.IdsProveedor.ToArray());

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
                                nombre_categoria = reader.GetString(6)
                            });
                        }
                    }
                }
            }
            return materiales;
        }

        public List<Material> ObtenerMaterialesStockBajoPriorizados()
        {
            var materiales = new List<Material>();
            ConexionDB con = new ConexionDB();
            using (var cone = con.GetConnection())
            {
                string sql = @"
                    SELECT
                        m.id,
                        m.nombre,
                        m.preciounitario,
                        m.stockactual,
                        m.stockminimo,
                        m.id_categoria,
                        c.nombre as nombre_categoria,
                        COALESCE(SUM(ABS(mm.cantidad)), 0) as cantidad_usada,
                        COUNT(mm.id_movimientomaterial) as cantidad_movimientos
                    FROM materiales m
                    INNER JOIN categorias c ON m.id_categoria = c.id_categoria
                    LEFT JOIN movimientomaterial mm ON mm.id_material = m.id
                    WHERE m.stockminimo > 0
                      AND m.stockactual <= m.stockminimo
                      AND EXISTS (
                          SELECT 1
                          FROM movimientomaterial mm_uso
                          WHERE mm_uso.id_material = m.id
                      )
                    GROUP BY
                        m.id, m.nombre, m.preciounitario, m.stockactual,
                        m.stockminimo, m.id_categoria, c.nombre
                    ORDER BY
                        COALESCE(SUM(ABS(mm.cantidad)), 0) DESC,
                        COUNT(mm.id_movimientomaterial) DESC,
                        (m.stockminimo - m.stockactual) DESC,
                        m.nombre ASC";

                using (var cmd = new NpgsqlCommand(sql, cone))
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
                            cantidadUsada = reader.GetDecimal(7),
                            cantidadMovimientos = Convert.ToInt32(reader.GetInt64(8))
                        });
                    }
                }
            }

            return materiales;
        }

        public void GuardarProveedoresMaterial(int idMaterial, List<int> idsProveedores)
        {
            ConexionDB con = new ConexionDB();
            using (var cone = con.GetConnection())
            {
                using (var tx = cone.BeginTransaction())
                {
                    try
                    {
                        GuardarProveedoresMaterial(idMaterial, idsProveedores, cone, tx);
                        tx.Commit();
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }

        public void GuardarProveedoresMaterial(int idMaterial, List<int> idsProveedores, NpgsqlConnection cone, NpgsqlTransaction tx)
        {
            // Borrar relaciones anteriores
            using (var cmd = new NpgsqlCommand("DELETE FROM materiales_proveedores WHERE id_material=@idMat", cone, tx))
            {
                cmd.Parameters.AddWithValue("@idMat", idMaterial);
                cmd.ExecuteNonQuery();
            }

            // Insertar nuevas
            if (idsProveedores != null)
            {
                foreach (int idProv in idsProveedores.Distinct())
                {
                    using (var cmd = new NpgsqlCommand(
                        "INSERT INTO materiales_proveedores(id_material, id_proveedor) VALUES(@idMat, @idProv)", cone, tx))
                    {
                        cmd.Parameters.AddWithValue("@idMat", idMaterial);
                        cmd.Parameters.AddWithValue("@idProv", idProv);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        public List<int> ObtenerProveedoresMaterial(int idMaterial)
        {
            var ids = new List<int>();
            ConexionDB con = new ConexionDB();
            using (var cone = con.GetConnection())
            {
                using (var cmd = new NpgsqlCommand(
                    "SELECT id_proveedor FROM materiales_proveedores WHERE id_material=@idMat", cone))
                {
                    cmd.Parameters.AddWithValue("@idMat", idMaterial);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                            ids.Add(reader.GetInt32(0));
                    }
                }
            }
            return ids;
        }
        public List<string> ObtenerOpcionesEnum(string nombreEnumPostgres)
        {
            List<string> opciones = new List<string>();
            ConexionDB con = new ConexionDB();

            using (var conexion = con.GetConnection())
            {
                if (conexion.State != System.Data.ConnectionState.Open)
                    conexion.Open();

                string query = "SELECT e.enumlabel FROM pg_enum e JOIN pg_type t ON e.enumtypid = t.oid WHERE t.typname = @nombreEnum";

                using (var cmd = new NpgsqlCommand(query, conexion))
                {
                    cmd.Parameters.AddWithValue("@nombreEnum", nombreEnumPostgres);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            opciones.Add(reader.GetString(0));
                        }
                    }
                }
            }
            return opciones;
        }
    }
}
