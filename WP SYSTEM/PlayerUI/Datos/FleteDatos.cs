using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WP_Inventario.Modelos;
namespace WP_Inventario.Datos
{
    public class FleteDatos
    {
        public FleteDatos() { }
        public int GuardarFlete(string flete)
        {
            ConexionDB con = new ConexionDB();
            using (var cone = con.GetConnection())
            {
                string sql = "INSERT INTO fletes(nombre) VALUES(@nom) RETURNING id_flete";
                using (var cmd = new NpgsqlCommand(sql, cone))
                {
                    cmd.Parameters.AddWithValue("@nom", flete);
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }
        public List<Flete> GetFletes(bool soloActivos = false, int? incluirId = null)
        {
            List<Flete> fletes = new List<Flete>();
            ConexionDB con = new ConexionDB();
            using (var cone = con.GetConnection())
            {
                string sql = "SELECT id_flete, nombre, activo FROM fletes";
                if (soloActivos)
                {
                    if (incluirId.HasValue && incluirId.Value > 0)
                    {
                        sql += " WHERE activo = true OR id_flete = @incluirId";
                    }
                    else
                    {
                        sql += " WHERE activo = true";
                    }
                }
                sql += " ORDER BY nombre ASC";

                using (var cmd = new NpgsqlCommand(sql, cone))
                {
                    if (soloActivos && incluirId.HasValue && incluirId.Value > 0)
                    {
                        cmd.Parameters.AddWithValue("@incluirId", incluirId.Value);
                    }

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            fletes.Add(
                                new Flete
                                {
                                    id = reader.GetInt32(0),
                                    nombre = reader.GetString(1),
                                    activo = reader.IsDBNull(2) ? true : reader.GetBoolean(2)
                                });
                        }
                    }
                }
            }
            return fletes;
        }
    }
}
