using System;
using System.Collections.Generic;
using Npgsql;
using WP_Inventario.Modelos;

namespace WP_Inventario.Datos
{
    public class CanaletaPlegadoDatos
    {
        public CanaletaPlegadoDatos() { }

        public List<CanaletaPlegado> ObtenerCanaletasPlegados(int? idObra = null)
        {
            var lista = new List<CanaletaPlegado>();
            ConexionDB conexionDB = new ConexionDB();

            using (var conexion = conexionDB.GetConnection())
            {
                var condiciones = new List<string>();
                if (idObra.HasValue && idObra.Value > 0)
                    condiciones.Add("cp.id_obra = @idObra");

                string where = condiciones.Count > 0 ? " WHERE " + string.Join(" AND ", condiciones) : "";

                string query = @"
                    SELECT cp.id, cp.id_obra, o.nombre as nombre_obra,
                           cp.descripcion, cp.precio, cp.pagado, cp.fecha_creacion
                    FROM public.canaletas_plegados cp
                    INNER JOIN public.obras o ON cp.id_obra = o.id" + where + @"
                    ORDER BY cp.fecha_creacion DESC, cp.id DESC";

                using (var cmd = new NpgsqlCommand(query, conexion))
                {
                    if (idObra.HasValue && idObra.Value > 0)
                        cmd.Parameters.AddWithValue("@idObra", idObra.Value);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lista.Add(new CanaletaPlegado
                            {
                                id = reader.GetInt32(0),
                                id_obra = reader.GetInt32(1),
                                nombre_obra = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                descripcion = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                precio = reader.IsDBNull(4) ? 0 : reader.GetDecimal(4),
                                pagado = reader.IsDBNull(5) ? false : reader.GetBoolean(5),
                                fecha_creacion = reader.IsDBNull(6) ? DateTime.MinValue : reader.GetDateTime(6)
                            });
                        }
                    }
                }
            }
            return lista;
        }

        public int GuardarCanaletaPlegado(CanaletaPlegado item)
        {
            ConexionDB conexionDB = new ConexionDB();
            using (var conexion = conexionDB.GetConnection())
            {
                string query = @"
                    INSERT INTO public.canaletas_plegados (id_obra, descripcion, precio, pagado)
                    VALUES (@id_obra, @descripcion, @precio, @pagado)
                    RETURNING id";

                using (var cmd = new NpgsqlCommand(query, conexion))
                {
                    cmd.Parameters.AddWithValue("@id_obra", item.id_obra);
                    cmd.Parameters.AddWithValue("@descripcion", item.descripcion ?? "");
                    cmd.Parameters.AddWithValue("@precio", item.precio);
                    cmd.Parameters.AddWithValue("@pagado", item.pagado);
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        public void ActualizarCanaletaPlegado(CanaletaPlegado item)
        {
            ConexionDB conexionDB = new ConexionDB();
            using (var conexion = conexionDB.GetConnection())
            {
                string query = @"
                    UPDATE public.canaletas_plegados
                    SET id_obra = @id_obra,
                        descripcion = @descripcion,
                        precio = @precio,
                        pagado = @pagado
                    WHERE id = @id";

                using (var cmd = new NpgsqlCommand(query, conexion))
                {
                    cmd.Parameters.AddWithValue("@id", item.id);
                    cmd.Parameters.AddWithValue("@id_obra", item.id_obra);
                    cmd.Parameters.AddWithValue("@descripcion", item.descripcion ?? "");
                    cmd.Parameters.AddWithValue("@precio", item.precio);
                    cmd.Parameters.AddWithValue("@pagado", item.pagado);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void EliminarCanaletaPlegado(int id)
        {
            ConexionDB conexionDB = new ConexionDB();
            using (var conexion = conexionDB.GetConnection())
            {
                string query = "DELETE FROM public.canaletas_plegados WHERE id = @id";
                using (var cmd = new NpgsqlCommand(query, conexion))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
