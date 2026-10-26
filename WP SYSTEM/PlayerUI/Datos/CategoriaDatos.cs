using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WP_Inventario.Modelos;
using Npgsql;

namespace WP_Inventario.Datos
{


    public class CategoriaDatos {
        public CategoriaDatos() { }
        public List<Categoria> GetCategorias()
        {
            var list = new List<Categoria>();
            ConexionDB cone = new ConexionDB();
            using (var con = cone.GetConnection())
            {
                string consulta = "SELECT * from categorias";
                using (var sql = new NpgsqlCommand(consulta, con))
                using (var reader = sql.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var cat = new Categoria
                        {
                            id = reader.GetInt32(0),
                            nombre = reader.GetString(1)
                        };
                        // recortable column may not exist yet
                        try { cat.recortable = reader.GetBoolean(reader.GetOrdinal("recortable")); }
                        catch { cat.recortable = false; }
                        list.Add(cat);
                    }
                }
            }
            return list;
        }
    
    public void GuardarCategoria(string nombreCategoria, bool recortable = false)
        {
            ConexionDB cone = new ConexionDB();
            using(var con = cone.GetConnection())
            {
                string consulta = "INSERT INTO categorias(nombre, recortable) VALUES(@nom, @rec)";
                using (var comando = new NpgsqlCommand(consulta, con))
                {
                    comando.Parameters.AddWithValue("@nom",nombreCategoria);
                    comando.Parameters.AddWithValue("@rec", recortable);
                    comando.ExecuteNonQuery();
                }
            }
        }
    } }
