using Npgsql;
using System.Collections.Generic;
using SteeLog.Modelos;

namespace SteeLog.Datos
{
    public class HerramientaDatos
    {
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

    }
}
