using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WP_Inventario.Modelos;


namespace WP_Inventario.Datos
{
    public class ProveedorDatos
    {
        public ProveedorDatos() { }
        public List<Proveedor> GetProveedor()
        {
            var lista = new List<Proveedor>();
            ConexionDB cone = new ConexionDB();
            using(var con = cone.GetConnection())
            {
                string consulta = "SELECT * FROM proveedores";
                using (var sql = new NpgsqlCommand(consulta, con)) 
                    using(var reader = sql.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lista.Add(
                            new Proveedor
                            {
                                id = reader.GetInt32(0),
                                nombre = reader.GetString(1)
                            }
                            );
                    }
                }
            }
            return lista;
        }
        public void GuardarProveedor(string nombre)
        {
            ConexionDB con = new ConexionDB();
            using(var cone = con.GetConnection())
            {
                string consulta = "INSERT INTO proveedores(nombre) VALUES(@nom)";
                using(var cmd=new NpgsqlCommand(consulta, cone))
                {
                    cmd.Parameters.AddWithValue("@nom", nombre);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
