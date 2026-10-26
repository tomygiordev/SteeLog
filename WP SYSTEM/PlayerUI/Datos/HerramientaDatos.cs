using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WP_Inventario.Modelos;

namespace WP_Inventario.Datos
{
    public class HerramientaDatos
    {
        public HerramientaDatos() { }

        public void GuardarHerramienta(string nombre, string estado, int? idObra, string observaciones)
        {
            ConexionDB con = new ConexionDB();
            using (var cone = con.GetConnection())
            {
                if (cone.State != System.Data.ConnectionState.Open) cone.Open();

                using (var tx = cone.BeginTransaction())
                {
                    try
                    {
                        // 1) INSERT con el nombre tal cual lo tipeó el usuario para obtener el id real.
                        string sql = "INSERT INTO herramientas(nombre, estado, id_obra, observaciones) VALUES(@nom, @est, @obra::integer, @obs::varchar) RETURNING id_herramienta";
                        int newId;
                        using (var cmd = new NpgsqlCommand(sql, cone, tx))
                        {
                            cmd.Parameters.AddWithValue("@nom", nombre);
                            cmd.Parameters.AddWithValue("@est", estado);
                            cmd.Parameters.AddWithValue("@obra", (object)idObra ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@obs", (object)observaciones ?? DBNull.Value);
                            newId = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        // 2) Si el nombre no empieza con dígitos, prefijar el id REAL (ya no se puede desincronizar).
                        if (!System.Text.RegularExpressions.Regex.IsMatch(nombre, @"^\d+"))
                        {
                            string nombreConId = $"{newId}{nombre}";
                            using (var cmdUpd = new NpgsqlCommand("UPDATE herramientas SET nombre=@nom WHERE id_herramienta=@id", cone, tx))
                            {
                                cmdUpd.Parameters.AddWithValue("@nom", nombreConId);
                                cmdUpd.Parameters.AddWithValue("@id", newId);
                                cmdUpd.ExecuteNonQuery();
                            }
                        }

                        // 3) Registrar movimiento inicial dentro de la misma transacción.
                        string obraDestino = idObra.HasValue ? ObtenerNombreObra(cone, idObra.Value, tx) : "Galpón";
                        RegistrarMovimiento(cone, newId, "Galpón", obraDestino, tx);

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

        public List<Herramienta> GetHerramientas()
        {
            List<Herramienta> lista = new List<Herramienta>();
            ConexionDB con = new ConexionDB();
            using (var cone = con.GetConnection())
            {
                string sql = @"SELECT h.id_herramienta, h.nombre, h.estado, h.id_obra, 
                               COALESCE(o.nombre, 'Galpón') as nombre_obra, h.observaciones
                               FROM herramientas h
                               LEFT JOIN obras o ON h.id_obra = o.id
                               ORDER BY h.nombre ASC";
                using (var cmd = new NpgsqlCommand(sql, cone))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lista.Add(new Herramienta
                        {
                            id = reader.GetInt32(0),
                            nombre = reader.GetString(1),
                            estado = reader.GetString(2),
                            id_obra = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3),
                            nombre_obra = reader.GetString(4),
                            observaciones = reader.IsDBNull(5) ? "" : reader.GetString(5)
                        });
                    }
                }
            }
            return lista;
        }

        public void ActualizarHerramienta(int id, string nombre, string estado, int? idObra, string observaciones)
        {
            ConexionDB con = new ConexionDB();
            using (var cone = con.GetConnection())
            {
                // Obtener obra anterior para comparar
                int? obraAnterior = null;
                string sqlGet = "SELECT id_obra FROM herramientas WHERE id_herramienta=@id";
                using (var cmdGet = new NpgsqlCommand(sqlGet, cone))
                {
                    cmdGet.Parameters.AddWithValue("@id", id);
                    var result = cmdGet.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                        obraAnterior = Convert.ToInt32(result);
                }

                // Actualizar
                string sql = "UPDATE herramientas SET nombre=@nom, estado=@est, id_obra=@obra::integer, observaciones=@obs::varchar WHERE id_herramienta=@id";
                using (var cmd = new NpgsqlCommand(sql, cone))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.Parameters.AddWithValue("@nom", nombre);
                    cmd.Parameters.AddWithValue("@est", estado);
                    cmd.Parameters.AddWithValue("@obra", (object)idObra ?? DBNull.Value);
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

        public void EnviarAGalpon(int idHerramienta)
        {
            ConexionDB con = new ConexionDB();
            using (var cone = con.GetConnection())
            {
                if (cone.State != ConnectionState.Open) cone.Open();

                using (var tx = cone.BeginTransaction())
                {
                    try
                    {
                        int? idObraActual = null;
                        string origen = "Galpón";

                        const string sqlGet = @"
                            SELECT h.id_obra, COALESCE(o.nombre, 'Galpón')
                            FROM herramientas h
                            LEFT JOIN obras o ON h.id_obra = o.id
                            WHERE h.id_herramienta = @id
                            FOR UPDATE";

                        using (var cmdGet = new NpgsqlCommand(sqlGet, cone, tx))
                        {
                            cmdGet.Parameters.AddWithValue("@id", idHerramienta);
                            using (var reader = cmdGet.ExecuteReader())
                            {
                                if (!reader.Read())
                                    throw new InvalidOperationException("La herramienta seleccionada no existe.");

                                idObraActual = reader.IsDBNull(0) ? (int?)null : reader.GetInt32(0);
                                origen = reader.IsDBNull(1) ? "Galpón" : reader.GetString(1);
                            }
                        }

                        if (!idObraActual.HasValue)
                            throw new InvalidOperationException("La herramienta ya se encuentra en el galpón.");

                        const string sqlUpdate = "UPDATE herramientas SET id_obra = NULL WHERE id_herramienta = @id";
                        using (var cmdUpdate = new NpgsqlCommand(sqlUpdate, cone, tx))
                        {
                            cmdUpdate.Parameters.AddWithValue("@id", idHerramienta);
                            cmdUpdate.ExecuteNonQuery();
                        }

                        RegistrarMovimiento(cone, idHerramienta, origen, "Galpón", tx);
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

        public void EliminarHerramienta(int id)
        {
            ConexionDB con = new ConexionDB();
            using (var cone = con.GetConnection())
            {
                // Eliminar historial de movimientos primero
                string sqlHist = "DELETE FROM herramienta_movimientos WHERE id_herramienta=@id";
                using (var cmdHist = new NpgsqlCommand(sqlHist, cone))
                {
                    cmdHist.Parameters.AddWithValue("@id", id);
                    cmdHist.ExecuteNonQuery();
                }

                string sql = "DELETE FROM herramientas WHERE id_herramienta=@id";
                using (var cmd = new NpgsqlCommand(sql, cone))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public List<HerramientaMovimiento> GetHistorial()
        {
            List<HerramientaMovimiento> lista = new List<HerramientaMovimiento>();
            ConexionDB con = new ConexionDB();
            using (var cone = con.GetConnection())
            {
                string sql = @"SELECT hm.id, h.id_herramienta, h.nombre, h.estado, hm.obra_origen, hm.obra_destino, hm.fecha
                               FROM herramienta_movimientos hm
                               INNER JOIN herramientas h ON hm.id_herramienta = h.id_herramienta
                               ORDER BY hm.fecha DESC";
                using (var cmd = new NpgsqlCommand(sql, cone))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lista.Add(new HerramientaMovimiento
                        {
                            id = reader.GetInt32(0),
                            id_herramienta = reader.GetInt32(1),
                            nombre_herramienta = reader.GetString(2),
                            estado = reader.IsDBNull(3) ? "" : reader.GetString(3),
                            obra_origen = reader.GetString(4),
                            obra_destino = reader.GetString(5),
                            fecha = reader.GetDateTime(6)
                        });
                    }
                }
            }
            return lista;
        }

        private void RegistrarMovimiento(NpgsqlConnection cone, int idHerramienta, string obraOrigen, string obraDestino, NpgsqlTransaction tx = null)
        {
            string sql = "INSERT INTO herramienta_movimientos(id_herramienta, obra_origen, obra_destino) VALUES(@id, @origen, @destino)";
            using (var cmd = new NpgsqlCommand(sql, cone))
            {
                if (tx != null) cmd.Transaction = tx;
                cmd.Parameters.AddWithValue("@id", idHerramienta);
                cmd.Parameters.AddWithValue("@origen", obraOrigen);
                cmd.Parameters.AddWithValue("@destino", obraDestino);
                cmd.ExecuteNonQuery();
            }
        }

        private string ObtenerNombreObra(NpgsqlConnection cone, int idObra, NpgsqlTransaction tx = null)
        {
            string sql = "SELECT nombre FROM obras WHERE id=@id";
            using (var cmd = new NpgsqlCommand(sql, cone))
            {
                if (tx != null) cmd.Transaction = tx;
                cmd.Parameters.AddWithValue("@id", idObra);
                var result = cmd.ExecuteScalar();
                return result?.ToString() ?? "Desconocida";
            }
        }
    }
}
