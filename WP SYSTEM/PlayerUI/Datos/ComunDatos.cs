using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Npgsql;

namespace WP_Inventario.Datos
{
    public class ComunDatos
    {
        public void GuardarEntidadGenerica(string nombreTabla, Dictionary<string, object> columnasAInsertar)
        {
            ConexionDB con = new ConexionDB();
            using (var cone = con.GetConnection())
            {
                List<string> columnas = new List<string>();
                List<string> parametros = new List<string>();

                foreach (var col in columnasAInsertar.Keys)
                {
                    columnas.Add(col);
                    parametros.Add("@" + col);
                }

                string colsStr = string.Join(", ", columnas);
                string paramsStr = string.Join(", ", parametros);

                string sql = $"INSERT INTO {nombreTabla} ({colsStr}) VALUES ({paramsStr})";

                using (var cmd = new NpgsqlCommand(sql, cone))
                {
                    foreach (var par in columnasAInsertar)
                    {
                        cmd.Parameters.AddWithValue($"@{par.Key}", par.Value ?? DBNull.Value);
                    }
                    cmd.ExecuteNonQuery();
                }
            }
        }
        public void EliminarEntidadGenerica(string nombreTabla, string nombreColumnaId, int id)
        {
            ConexionDB con = new ConexionDB();
            using (var cone = con.GetConnection())
            {
                string sql = $"DELETE FROM {nombreTabla} WHERE {nombreColumnaId} = @id";
                using (var cmd = new NpgsqlCommand(sql, cone))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void ActualizarEntidadGenerica(string nombreTabla, string nombreColumnaId, int id, Dictionary<string, object> columnasAEditar)
        {
            ConexionDB con = new ConexionDB();
            using (var cone = con.GetConnection())
            {
                List<string> sets = new List<string>();
                foreach (var col in columnasAEditar.Keys)
                {
                    sets.Add($"{col} = @{col}");
                }

                string setClause = string.Join(", ", sets);
                string sql = $"UPDATE {nombreTabla} SET {setClause} WHERE {nombreColumnaId} = @id";

                using (var cmd = new NpgsqlCommand(sql, cone))
                {
                    cmd.Parameters.AddWithValue("@id", id);

                    foreach (var par in columnasAEditar)
                    {
                        cmd.Parameters.AddWithValue($"@{par.Key}", par.Value ?? DBNull.Value);
                    }

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public DataTable ObtenerListaGenerica(string nombreTabla, string columnaOrderBy = "nombre", bool soloActivos = false)
        {
            DataTable dt = new DataTable();
            ConexionDB con = new ConexionDB();
            using (var cone = con.GetConnection())
            {
                string sql = $"SELECT * FROM {nombreTabla}";
                if (soloActivos) sql += " WHERE activa = true";
                sql += $" ORDER BY {columnaOrderBy} ASC";

                using (var cmd = new NpgsqlCommand(sql, cone))
                {
                    using (NpgsqlDataAdapter da = new NpgsqlDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                }
            }
            return dt;
        }
    }
}