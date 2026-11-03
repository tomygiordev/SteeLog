using System;
using System.Collections.Generic;
using System.Data;
using Npgsql;
using SteeLog.Modelos;

namespace SteeLog.Datos
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


    }
}
