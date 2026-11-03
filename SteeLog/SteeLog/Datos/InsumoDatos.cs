using Npgsql;
using System.Collections.Generic;
using SteeLog.Modelos;

namespace SteeLog.Datos
{
    public class InsumoDatos
    {
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
    }
}
