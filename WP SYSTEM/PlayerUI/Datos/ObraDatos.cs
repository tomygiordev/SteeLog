using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WP_Inventario.Modelos;

namespace WP_Inventario.Datos
{
    public class ObraDatos
    {
        public ObraDatos() { }
        public void GuardarObra(string obra, bool activa)
        {
            ConexionDB con = new ConexionDB();
            using(var cone = con.GetConnection())
            {
                string sql = "INSERT INTO obras(nombre, activa) VALUES(@nom, @act)";
                using(var cmd=new NpgsqlCommand(sql, cone))
                {
                    cmd.Parameters.AddWithValue("@nom", obra);
                    cmd.Parameters.AddWithValue("@act", activa);
                    cmd.ExecuteNonQuery();
                }
            }
        }
        public List<Obra> GetObras() {
            List<Obra> Obras = new List<Obra>();
            ConexionDB con = new ConexionDB();
            using(var cone = con.GetConnection())
            {
                string sql = "SELECT * FROM obras";
                using (var cmd =new NpgsqlCommand(sql,cone))
                    using(var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var obra = new Obra {
                            id = reader.GetInt32(reader.GetOrdinal("id")),
                            nombre = reader.GetString(reader.GetOrdinal("nombre"))
                        };
                        
                        // Manejar la nueva columna activa
                        int ordinalActiva = reader.GetOrdinal("activa");
                        obra.activa = reader.IsDBNull(ordinalActiva) ? true : reader.GetBoolean(ordinalActiva);
                        
                        Obras.Add(obra);
                    }
                }
            }
            return Obras;
        }
    }
}
