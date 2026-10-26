using Npgsql;
using System;
using System.Collections.Generic;
using WP_Inventario.Modelos;

namespace WP_Inventario.Datos
{
    public class InsumoDatos
    {
        public InsumoDatos() { }

        public void GuardarInsumo(string nombre, int? idObra, string observaciones)
        {
            ConexionDB con = new ConexionDB();
            using (var cone = con.GetConnection())
            {
                string sql = "INSERT INTO insumos(nombre, id_obra, consumido, observaciones) VALUES(@nom, @obra::integer, false, @obs::varchar) RETURNING id_insumo";
                int newId;
                using (var cmd = new NpgsqlCommand(sql, cone))
                {
                    cmd.Parameters.AddWithValue("@nom", nombre);
                    cmd.Parameters.AddWithValue("@obra", (object)idObra ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@obs", (object)observaciones ?? DBNull.Value);
                    newId = Convert.ToInt32(cmd.ExecuteScalar());
                }

                string obraDestino = idObra.HasValue ? ObtenerNombreObra(cone, idObra.Value) : "Galpón";
                RegistrarMovimiento(cone, newId, "Galpón", obraDestino);
            }
        }

        public List<Insumo> GetInsumos()
        {
            List<Insumo> lista = new List<Insumo>();
            ConexionDB con = new ConexionDB();
            using (var cone = con.GetConnection())
            {
                string sql = @"SELECT i.id_insumo, i.nombre, i.id_obra, 
                               COALESCE(o.nombre, 'Galpón') as nombre_obra, i.consumido, i.observaciones
                               FROM insumos i
                               LEFT JOIN obras o ON i.id_obra = o.id
                               ORDER BY i.nombre ASC";
                using (var cmd = new NpgsqlCommand(sql, cone))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lista.Add(new Insumo
                        {
                            id = reader.GetInt32(0),
                            nombre = reader.GetString(1),
                            id_obra = reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2),
                            nombre_obra = reader.GetString(3),
                            consumido = reader.GetBoolean(4),
                            observaciones = reader.IsDBNull(5) ? "" : reader.GetString(5)
                        });
                    }
                }
            }
            return lista;
        }

        public void ActualizarInsumo(int id, string nombre, int? idObra, bool consumido, string observaciones)
        {
            ConexionDB con = new ConexionDB();
            using (var cone = con.GetConnection())
            {
                // Obtener obra anterior para comparar
                int? obraAnterior = null;
                string sqlGet = "SELECT id_obra FROM insumos WHERE id_insumo=@id";
                using (var cmdGet = new NpgsqlCommand(sqlGet, cone))
                {
                    cmdGet.Parameters.AddWithValue("@id", id);
                    var result = cmdGet.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                        obraAnterior = Convert.ToInt32(result);
                }

                // Actualizar
                string sql = "UPDATE insumos SET nombre=@nom, id_obra=@obra::integer, consumido=@cons, observaciones=@obs::varchar WHERE id_insumo=@id";
                using (var cmd = new NpgsqlCommand(sql, cone))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.Parameters.AddWithValue("@nom", nombre);
                    cmd.Parameters.AddWithValue("@obra", (object)idObra ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@cons", consumido);
                    cmd.Parameters.AddWithValue("@obs", (object)observaciones ?? DBNull.Value);
                    cmd.ExecuteNonQuery();
                }

                // Registrar movimiento si cambió la obra
                if (obraAnterior != idObra)
                {
                    string origen = obraAnterior.HasValue ? ObtenerNombreObra(cone, obraAnterior.Value) : "Galpón";
                    string destino = idObra.HasValue ? ObtenerNombreObra(cone, idObra.Value) : "Galpón";
                    RegistrarMovimiento(cone, id, origen, destino);
                }
            }
        }

        public void EliminarInsumo(int id)
        {
            ConexionDB con = new ConexionDB();
            using (var cone = con.GetConnection())
            {
                // Eliminar historial de movimientos primero
                string sqlHist = "DELETE FROM insumo_movimientos WHERE id_insumo=@id";
                using (var cmdHist = new NpgsqlCommand(sqlHist, cone))
                {
                    cmdHist.Parameters.AddWithValue("@id", id);
                    cmdHist.ExecuteNonQuery();
                }

                string sql = "DELETE FROM insumos WHERE id_insumo=@id";
                using (var cmd = new NpgsqlCommand(sql, cone))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public List<InsumoMovimiento> GetHistorial()
        {
            List<InsumoMovimiento> lista = new List<InsumoMovimiento>();
            ConexionDB con = new ConexionDB();
            using (var cone = con.GetConnection())
            {
                string sql = @"SELECT im.id, i.nombre, im.obra_origen, im.obra_destino, im.fecha
                               FROM insumo_movimientos im
                               INNER JOIN insumos i ON im.id_insumo = i.id_insumo
                               ORDER BY im.fecha DESC";
                using (var cmd = new NpgsqlCommand(sql, cone))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lista.Add(new InsumoMovimiento
                        {
                            id = reader.GetInt32(0),
                            nombre_insumo = reader.GetString(1),
                            obra_origen = reader.GetString(2),
                            obra_destino = reader.GetString(3),
                            fecha = reader.GetDateTime(4)
                        });
                    }
                }
            }
            return lista;
        }

        private void RegistrarMovimiento(NpgsqlConnection cone, int idInsumo, string obraOrigen, string obraDestino, int? idMovimiento = null)
        {
            string sql = "INSERT INTO insumo_movimientos(id_insumo, obra_origen, obra_destino, id_movimiento) VALUES(@id, @origen, @destino, @id_movimiento::integer)";
            using (var cmd = new NpgsqlCommand(sql, cone))
            {
                cmd.Parameters.AddWithValue("@id", idInsumo);
                cmd.Parameters.AddWithValue("@origen", obraOrigen);
                cmd.Parameters.AddWithValue("@destino", obraDestino);
                cmd.Parameters.AddWithValue("@id_movimiento", (object)idMovimiento ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }

        public void MoverInsumo(int idInsumo, int? nuevaIdObra, string obraOrigen, string obraDestino, int? idMovimiento = null)
        {
            ConexionDB con = new ConexionDB();
            using (var cone = con.GetConnection())
            {
                string sql = "UPDATE insumos SET id_obra=@obra::integer WHERE id_insumo=@id";
                using (var cmd = new NpgsqlCommand(sql, cone))
                {
                    cmd.Parameters.AddWithValue("@id", idInsumo);
                    cmd.Parameters.AddWithValue("@obra", (object)nuevaIdObra ?? DBNull.Value);
                    cmd.ExecuteNonQuery();
                }

                RegistrarMovimiento(cone, idInsumo, obraOrigen, obraDestino, idMovimiento);
            }
        }

        private string ObtenerNombreObra(NpgsqlConnection cone, int idObra)
        {
            string sql = "SELECT nombre FROM obras WHERE id=@id";
            using (var cmd = new NpgsqlCommand(sql, cone))
            {
                cmd.Parameters.AddWithValue("@id", idObra);
                var result = cmd.ExecuteScalar();
                return result?.ToString() ?? "Desconocida";
            }
        }
    }
}
