using System;
using System.Collections.Generic;
using System.Linq;
using Npgsql;
using WP_Inventario.Modelos;

namespace WP_Inventario.Datos
{
    public class CanaletaPlegadoFotoDatos
    {
        public CanaletaPlegadoFotoDatos() { }

        public List<CanaletaPlegadoFoto> ObtenerPorCanaleta(int canaletaPlegadoId)
        {
            var lista = new List<CanaletaPlegadoFoto>();
            ConexionDB conexionDB = new ConexionDB();

            using (var conexion = conexionDB.GetConnection())
            {
                string query = @"
                    SELECT id, canaleta_plegado_id, bucket_id, object_path,
                           nombre_archivo, content_type, tamanio_bytes, orden, fecha_creacion
                    FROM public.canaletas_plegados_fotos
                    WHERE canaleta_plegado_id = @id
                    ORDER BY orden ASC, id ASC";

                using (var cmd = new NpgsqlCommand(query, conexion))
                {
                    cmd.Parameters.AddWithValue("@id", canaletaPlegadoId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lista.Add(new CanaletaPlegadoFoto
                            {
                                id = reader.GetInt32(0),
                                canaleta_plegado_id = reader.GetInt32(1),
                                bucket_id = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                object_path = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                nombre_archivo = reader.IsDBNull(4) ? "" : reader.GetString(4),
                                content_type = reader.IsDBNull(5) ? "" : reader.GetString(5),
                                tamanio_bytes = reader.IsDBNull(6) ? 0 : reader.GetInt64(6),
                                orden = reader.IsDBNull(7) ? 0 : reader.GetInt32(7),
                                fecha_creacion = reader.IsDBNull(8) ? DateTime.MinValue : reader.GetDateTime(8)
                            });
                        }
                    }
                }
            }
            return lista;
        }

        public int Guardar(CanaletaPlegadoFoto foto)
        {
            ConexionDB conexionDB = new ConexionDB();
            using (var conexion = conexionDB.GetConnection())
            {
                string query = @"
                    INSERT INTO public.canaletas_plegados_fotos
                        (canaleta_plegado_id, bucket_id, object_path, nombre_archivo,
                         content_type, tamanio_bytes, orden)
                    VALUES
                        (@canaleta_plegado_id, @bucket_id, @object_path, @nombre_archivo,
                         @content_type, @tamanio_bytes, @orden)
                    RETURNING id";

                using (var cmd = new NpgsqlCommand(query, conexion))
                {
                    cmd.Parameters.AddWithValue("@canaleta_plegado_id", foto.canaleta_plegado_id);
                    cmd.Parameters.AddWithValue("@bucket_id", foto.bucket_id ?? "");
                    cmd.Parameters.AddWithValue("@object_path", foto.object_path ?? "");
                    cmd.Parameters.AddWithValue("@nombre_archivo", foto.nombre_archivo ?? "");
                    cmd.Parameters.AddWithValue("@content_type", foto.content_type ?? "");
                    cmd.Parameters.AddWithValue("@tamanio_bytes", foto.tamanio_bytes);
                    cmd.Parameters.AddWithValue("@orden", foto.orden);
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        public CanaletaPlegadoFoto Obtener(int id)
        {
            ConexionDB conexionDB = new ConexionDB();
            using (var conexion = conexionDB.GetConnection())
            {
                string query = @"
                    SELECT id, canaleta_plegado_id, bucket_id, object_path,
                           nombre_archivo, content_type, tamanio_bytes, orden, fecha_creacion
                    FROM public.canaletas_plegados_fotos
                    WHERE id = @id";

                using (var cmd = new NpgsqlCommand(query, conexion))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new CanaletaPlegadoFoto
                            {
                                id = reader.GetInt32(0),
                                canaleta_plegado_id = reader.GetInt32(1),
                                bucket_id = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                object_path = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                nombre_archivo = reader.IsDBNull(4) ? "" : reader.GetString(4),
                                content_type = reader.IsDBNull(5) ? "" : reader.GetString(5),
                                tamanio_bytes = reader.IsDBNull(6) ? 0 : reader.GetInt64(6),
                                orden = reader.IsDBNull(7) ? 0 : reader.GetInt32(7),
                                fecha_creacion = reader.IsDBNull(8) ? DateTime.MinValue : reader.GetDateTime(8)
                            };
                        }
                    }
                }
            }
            return null;
        }

        public void Eliminar(int id)
        {
            ConexionDB conexionDB = new ConexionDB();
            using (var conexion = conexionDB.GetConnection())
            {
                string query = "DELETE FROM public.canaletas_plegados_fotos WHERE id = @id";
                using (var cmd = new NpgsqlCommand(query, conexion))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void EliminarPorCanaleta(int canaletaPlegadoId)
        {
            ConexionDB conexionDB = new ConexionDB();
            using (var conexion = conexionDB.GetConnection())
            {
                string query = "DELETE FROM public.canaletas_plegados_fotos WHERE canaleta_plegado_id = @id";
                using (var cmd = new NpgsqlCommand(query, conexion))
                {
                    cmd.Parameters.AddWithValue("@id", canaletaPlegadoId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public int ObtenerSiguienteOrden(int canaletaPlegadoId)
        {
            ConexionDB conexionDB = new ConexionDB();
            using (var conexion = conexionDB.GetConnection())
            {
                string query = "SELECT COALESCE(MAX(orden), 0) + 1 FROM public.canaletas_plegados_fotos WHERE canaleta_plegado_id = @id";
                using (var cmd = new NpgsqlCommand(query, conexion))
                {
                    cmd.Parameters.AddWithValue("@id", canaletaPlegadoId);
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        public Dictionary<int, int> ObtenerConteosPorCanaletas(IEnumerable<int> canaletaPlegadoIds)
        {
            var ids = (canaletaPlegadoIds ?? Enumerable.Empty<int>())
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            var conteos = ids.ToDictionary(id => id, id => 0);
            if (ids.Count == 0)
                return conteos;

            ConexionDB conexionDB = new ConexionDB();
            using (var conexion = conexionDB.GetConnection())
            {
                var parametros = new List<string>();
                for (int i = 0; i < ids.Count; i++)
                    parametros.Add("@id" + i);

                string query = @"
                    SELECT canaleta_plegado_id, COUNT(*)::int
                    FROM public.canaletas_plegados_fotos
                    WHERE canaleta_plegado_id IN (" + string.Join(", ", parametros) + @")
                    GROUP BY canaleta_plegado_id";

                using (var cmd = new NpgsqlCommand(query, conexion))
                {
                    for (int i = 0; i < ids.Count; i++)
                        cmd.Parameters.AddWithValue("@id" + i, ids[i]);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                            conteos[reader.GetInt32(0)] = reader.GetInt32(1);
                    }
                }
            }

            return conteos;
        }
    }
}
