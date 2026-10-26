using System;
using System.Collections.Generic;
using System.Data;
using Npgsql;
using WP_Inventario.Modelos;

namespace WP_Inventario.Datos
{
    public class RecorteDatos
    {
        public List<Recorte> ObtenerRecortes(int? idMaterial = null, decimal? ancho = null, decimal? altura = null, decimal? medida = null)
        {
            List<Recorte> lista = new List<Recorte>();
            ConexionDB conexionDB = new ConexionDB();

            using (var conexion = conexionDB.GetConnection())
            {
                var condiciones = new System.Collections.Generic.List<string> { "r.cantidad > 0" };
                if (idMaterial.HasValue) condiciones.Add("r.id_material = @idMaterial");
                if (ancho.HasValue)     condiciones.Add("r.ancho = @ancho");
                if (altura.HasValue)    condiciones.Add("r.altura = @altura");
                if (medida.HasValue)    condiciones.Add("r.medida = @medida");

                string query = @"
                    SELECT r.id, r.id_material, m.nombre as nombre_material,
                           cat.nombre as nombre_categoria, r.ancho, r.altura, r.cantidad, r.fecha_creacion, r.medida, r.estado
                    FROM recortes r
                    JOIN materiales m ON r.id_material = m.id
                    JOIN categorias cat ON m.id_categoria = cat.id_categoria
                    WHERE " + string.Join(" AND ", condiciones) + " ORDER BY r.fecha_creacion DESC";

                using (var cmd = new NpgsqlCommand(query, conexion))
                {
                    if (idMaterial.HasValue) cmd.Parameters.AddWithValue("@idMaterial", idMaterial.Value);
                    if (ancho.HasValue)      cmd.Parameters.AddWithValue("@ancho", ancho.Value);
                    if (altura.HasValue)     cmd.Parameters.AddWithValue("@altura", altura.Value);
                    if (medida.HasValue)     cmd.Parameters.AddWithValue("@medida", medida.Value);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lista.Add(new Recorte
                            {
                                id = reader.GetInt32(0),
                                id_material = reader.GetInt32(1),
                                nombre_material = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                nombre_categoria = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                ancho = reader.IsDBNull(4) ? 0 : reader.GetDecimal(4),
                                altura = reader.IsDBNull(5) ? 0 : reader.GetDecimal(5),
                                cantidad = reader.IsDBNull(6) ? 0 : reader.GetDecimal(6),
                                fecha_creacion = reader.IsDBNull(7) ? DateTime.MinValue : reader.GetDateTime(7),
                                medida = reader.IsDBNull(8) ? 0 : reader.GetDecimal(8),
                                estado = reader.IsDBNull(9) ? null : reader.GetString(9)
                            });
                        }
                    }
                }
            }
            return lista;
        }

        public int GuardarRecorte(Recorte recorte)
        {
            ConexionDB conexionDB = new ConexionDB();
            using (var conexion = conexionDB.GetConnection())
            {
                // Verificar si ya existe un recorte para ese material, ancho, altura y medida
                string checkQuery = @"SELECT id, cantidad 
                                      FROM recortes 
                                      WHERE id_material = @id_material 
                                        AND ancho = @ancho 
                                        AND altura = @altura 
                                        AND medida = @medida
                                        AND ((estado = @estado) OR (estado IS NULL AND @estado IS NULL))";
                using (var checkCmd = new NpgsqlCommand(checkQuery, conexion))
                {
                    checkCmd.Parameters.AddWithValue("@id_material", recorte.id_material);
                    checkCmd.Parameters.AddWithValue("@ancho", recorte.ancho);
                    checkCmd.Parameters.AddWithValue("@altura", recorte.altura);
                    checkCmd.Parameters.AddWithValue("@medida", recorte.medida);
                    checkCmd.Parameters.AddWithValue("@estado", string.IsNullOrWhiteSpace(recorte.estado) ? (object)DBNull.Value : recorte.estado);
                    
                    using (var reader = checkCmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            int existingId = reader.GetInt32(0);
                            decimal existingCant = reader.GetDecimal(1);
                            reader.Close(); // Importante cerrar antes de ejecutar otro comando

                            // Si existe, acumulamos la cantidad
                            string updateQuery = "UPDATE recortes SET cantidad = @cantidad WHERE id = @id";
                            using (var updateCmd = new NpgsqlCommand(updateQuery, conexion))
                            {
                                updateCmd.Parameters.AddWithValue("@cantidad", existingCant + recorte.cantidad);
                                updateCmd.Parameters.AddWithValue("@id", existingId);
                                updateCmd.ExecuteNonQuery();
                            }
                            return existingId;
                        }
                    }
                }

                // Si no existe, insertar nuevo
                string query = @"
                    INSERT INTO recortes (id_material, ancho, altura, medida, cantidad, estado)
                    VALUES (@id_material, @ancho, @altura, @medida, @cantidad, @estado)
                    RETURNING id";

                using (var cmd = new NpgsqlCommand(query, conexion))
                {
                    cmd.Parameters.AddWithValue("@id_material", recorte.id_material);
                    cmd.Parameters.AddWithValue("@ancho", recorte.ancho);
                    cmd.Parameters.AddWithValue("@altura", recorte.altura);
                    cmd.Parameters.AddWithValue("@medida", recorte.medida);
                    cmd.Parameters.AddWithValue("@cantidad", recorte.cantidad);
                    cmd.Parameters.AddWithValue("@estado", string.IsNullOrWhiteSpace(recorte.estado) ? (object)DBNull.Value : recorte.estado);
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        public void EliminarRecorte(int id)
        {
            ConexionDB conexionDB = new ConexionDB();
            using (var conexion = conexionDB.GetConnection())
            {
                string query = "DELETE FROM recortes WHERE id = @id";
                using (var cmd = new NpgsqlCommand(query, conexion))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void ActualizarCantidad(int id, decimal nuevaCantidad)
        {
            ConexionDB conexionDB = new ConexionDB();
            using (var conexion = conexionDB.GetConnection())
            {
                string query = "UPDATE recortes SET cantidad = @cantidad WHERE id = @id";
                using (var cmd = new NpgsqlCommand(query, conexion))
                {
                    cmd.Parameters.AddWithValue("@cantidad", nuevaCantidad);
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
