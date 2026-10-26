using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Npgsql;
using WP_Inventario.Modelos;

namespace WP_Inventario.Datos
{
    public class ConteoInventarioDatos
    {
        public ConteoInventarioDatos() { }

        public int CrearConteo(string usuarioInicio)
        {
            if (string.IsNullOrWhiteSpace(usuarioInicio))
                throw new ArgumentException("El usuario de inicio es obligatorio.", nameof(usuarioInicio));

            ConexionDB cone = new ConexionDB();
            using (var con = cone.GetConnection())
            {
                const string sql = @"
                    INSERT INTO public.conteos_inventario (estado, usuario_inicio)
                    VALUES ('abierto', @usuario)
                    RETURNING id_conteo;";

                using (var cmd = new NpgsqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@usuario", usuarioInicio.Trim());
                    object result = cmd.ExecuteScalar();
                    return result != null && result != DBNull.Value ? Convert.ToInt32(result) : 0;
                }
            }
        }

        public ConteoInventario ObtenerConteoAbiertoPorUsuario(string usuario)
        {
            if (string.IsNullOrWhiteSpace(usuario)) return null;

            ConexionDB cone = new ConexionDB();
            using (var con = cone.GetConnection())
            {
                const string sql = @"
                    SELECT id_conteo, fecha_inicio, fecha_fin, usuario_inicio, usuario_fin,
                           estado, observaciones, created_at, updated_at
                    FROM public.conteos_inventario
                    WHERE estado = 'abierto'
                      AND LOWER(usuario_inicio) = LOWER(@usuario)
                    ORDER BY fecha_inicio DESC
                    LIMIT 1;";

                using (var cmd = new NpgsqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@usuario", usuario.Trim());
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return MapConteo(reader);
                        }
                    }
                }
            }
            return null;
        }

        public ConteoInventario ObtenerConteoPorId(int idConteo)
        {
            ConexionDB cone = new ConexionDB();
            using (var con = cone.GetConnection())
            {
                const string sql = @"
                    SELECT id_conteo, fecha_inicio, fecha_fin, usuario_inicio, usuario_fin,
                           estado, observaciones, created_at, updated_at
                    FROM public.conteos_inventario
                    WHERE id_conteo = @idConteo;";

                using (var cmd = new NpgsqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@idConteo", idConteo);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return MapConteo(reader);
                        }
                    }
                }
            }
            return null;
        }

        public void AgregarOActualizarDetalle(int idConteo, int idMaterial, decimal cantidadContada)
        {
            if (idConteo <= 0) throw new ArgumentException("Conteo inválido.", nameof(idConteo));
            if (idMaterial <= 0) throw new ArgumentException("Material inválido.", nameof(idMaterial));
            if (cantidadContada < 0) throw new ArgumentException("La cantidad contada no puede ser negativa.", nameof(cantidadContada));

            ConexionDB cone = new ConexionDB();
            using (var con = cone.GetConnection())
            {
                var snapshot = ObtenerSnapshotMaterial(con, null, idMaterial);
                if (snapshot == null)
                    throw new Exception("No se encontró el material seleccionado.");

                const string sql = @"
                    INSERT INTO public.conteos_inventario_detalle
                        (id_conteo, id_material, material_nombre_snapshot, material_categoria_snapshot, cantidad_contada)
                    VALUES
                        (@idConteo, @idMaterial, @nombre, @categoria, @cantidad)
                    ON CONFLICT (id_conteo, id_material) DO UPDATE
                    SET cantidad_contada = public.conteos_inventario_detalle.cantidad_contada + EXCLUDED.cantidad_contada,
                        updated_at = NOW();";

                using (var cmd = new NpgsqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@idConteo", idConteo);
                    cmd.Parameters.AddWithValue("@idMaterial", idMaterial);
                    cmd.Parameters.AddWithValue("@nombre", (object)snapshot.Value.nombre ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@categoria", (object)snapshot.Value.categoria ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@cantidad", cantidadContada);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public List<ConteoInventarioDetalle> ObtenerDetallesCarga(int idConteo)
        {
            var lista = new List<ConteoInventarioDetalle>();
            ConexionDB cone = new ConexionDB();
            using (var con = cone.GetConnection())
            {
                const string sql = @"
                    SELECT id_detalle, id_conteo, id_material,
                           material_nombre_snapshot, material_categoria_snapshot,
                           cantidad_contada, fecha_carga
                    FROM public.conteos_inventario_detalle
                    WHERE id_conteo = @idConteo
                    ORDER BY material_nombre_snapshot ASC, id_detalle ASC;";

                using (var cmd = new NpgsqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@idConteo", idConteo);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lista.Add(new ConteoInventarioDetalle
                            {
                                id_detalle = reader.GetInt32(0),
                                id_conteo = reader.GetInt32(1),
                                id_material = reader.GetInt32(2),
                                material_nombre_snapshot = reader.IsDBNull(3) ? null : reader.GetString(3),
                                material_categoria_snapshot = reader.IsDBNull(4) ? null : reader.GetString(4),
                                cantidad_contada = reader.GetDecimal(5),
                                fecha_carga = reader.GetDateTime(6)
                            });
                        }
                    }
                }
            }
            return lista;
        }

        public bool ConteoTieneDetalles(int idConteo)
        {
            ConexionDB cone = new ConexionDB();
            using (var con = cone.GetConnection())
            {
                using (var cmd = new NpgsqlCommand(
                    "SELECT COUNT(*) FROM public.conteos_inventario_detalle WHERE id_conteo = @idConteo;", con))
                {
                    cmd.Parameters.AddWithValue("@idConteo", idConteo);
                    object result = cmd.ExecuteScalar();
                    return result != null && result != DBNull.Value && Convert.ToInt32(result) > 0;
                }
            }
        }

        public void FinalizarConteo(int idConteo, string usuarioFin, string observaciones)
        {
            if (idConteo <= 0) throw new ArgumentException("Conteo inválido.", nameof(idConteo));

            ConexionDB cone = new ConexionDB();
            using (var con = cone.GetConnection())
            {
                if (con.State == ConnectionState.Closed) con.Open();

                using (var tx = con.BeginTransaction())
                {
                    try
                    {
                        string estadoActual;
                        using (var cmdEstado = new NpgsqlCommand(
                            "SELECT estado FROM public.conteos_inventario WHERE id_conteo = @idConteo FOR UPDATE;", con, tx))
                        {
                            cmdEstado.Parameters.AddWithValue("@idConteo", idConteo);
                            object res = cmdEstado.ExecuteScalar();
                            if (res == null || res == DBNull.Value)
                                throw new Exception("El conteo que intentás finalizar no existe.");
                            estadoActual = res.ToString();
                        }

                        if (estadoActual != "abierto")
                            throw new Exception("Solo se pueden finalizar conteos en estado 'abierto'.");

                        int totalDetalles;
                        using (var cmdCount = new NpgsqlCommand(
                            "SELECT COUNT(*) FROM public.conteos_inventario_detalle WHERE id_conteo = @idConteo;", con, tx))
                        {
                            cmdCount.Parameters.AddWithValue("@idConteo", idConteo);
                            totalDetalles = Convert.ToInt32(cmdCount.ExecuteScalar() ?? 0);
                        }

                        if (totalDetalles == 0)
                            throw new Exception("No se puede finalizar un conteo sin materiales cargados.");

                        const string updateDetalle = @"
                            UPDATE public.conteos_inventario_detalle d
                            SET stock_sistema = m.stockactual,
                                diferencia = d.cantidad_contada - m.stockactual,
                                updated_at = NOW()
                            FROM public.materiales m
                            WHERE d.id_conteo = @idConteo
                              AND m.id = d.id_material;";

                        using (var cmdUpd = new NpgsqlCommand(updateDetalle, con, tx))
                        {
                            cmdUpd.Parameters.AddWithValue("@idConteo", idConteo);
                            cmdUpd.ExecuteNonQuery();
                        }

                        const string updateCabecera = @"
                            UPDATE public.conteos_inventario
                            SET estado = 'finalizado',
                                fecha_fin = NOW(),
                                usuario_fin = @usuarioFin,
                                observaciones = @observaciones,
                                updated_at = NOW()
                            WHERE id_conteo = @idConteo;";

                        using (var cmdCab = new NpgsqlCommand(updateCabecera, con, tx))
                        {
                            cmdCab.Parameters.AddWithValue("@idConteo", idConteo);
                            cmdCab.Parameters.AddWithValue("@usuarioFin", (object)(usuarioFin ?? "").Trim() ?? DBNull.Value);
                            cmdCab.Parameters.AddWithValue("@observaciones", string.IsNullOrWhiteSpace(observaciones) ? (object)DBNull.Value : observaciones.Trim());
                            cmdCab.ExecuteNonQuery();
                        }

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

        public void EliminarConteo(int idConteo)
        {
            if (idConteo <= 0) throw new ArgumentException("Conteo inválido.", nameof(idConteo));

            ConexionDB cone = new ConexionDB();
            using (var con = cone.GetConnection())
            {
                if (con.State == ConnectionState.Closed) con.Open();

                using (var tx = con.BeginTransaction())
                {
                    try
                    {
                        string estadoActual;
                        using (var cmdEstado = new NpgsqlCommand(
                            "SELECT estado FROM public.conteos_inventario WHERE id_conteo = @idConteo FOR UPDATE;", con, tx))
                        {
                            cmdEstado.Parameters.AddWithValue("@idConteo", idConteo);
                            object res = cmdEstado.ExecuteScalar();
                            if (res == null || res == DBNull.Value)
                                throw new Exception("El conteo que intentás eliminar no existe.");
                            estadoActual = res.ToString();
                        }

                        if (string.Equals(estadoActual, "abierto", StringComparison.OrdinalIgnoreCase))
                            throw new Exception("Solo se pueden eliminar conteos finalizados o cancelados. Para un conteo abierto, primero cancelalo.");

                        using (var cmdDel = new NpgsqlCommand(
                            "DELETE FROM public.conteos_inventario WHERE id_conteo = @idConteo;", con, tx))
                        {
                            cmdDel.Parameters.AddWithValue("@idConteo", idConteo);
                            int filas = cmdDel.ExecuteNonQuery();
                            if (filas == 0)
                                throw new Exception("No se pudo eliminar el conteo.");
                        }

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

        public void EliminarDetalle(int idDetalle)
        {
            if (idDetalle <= 0) throw new ArgumentException("Detalle inválido.", nameof(idDetalle));

            ConexionDB cone = new ConexionDB();
            using (var con = cone.GetConnection())
            {
                using (var cmd = new NpgsqlCommand(
                    "DELETE FROM public.conteos_inventario_detalle WHERE id_detalle = @idDetalle;", con))
                {
                    cmd.Parameters.AddWithValue("@idDetalle", idDetalle);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void ReemplazarCantidadDetalle(int idConteo, int idMaterial, decimal cantidadContada)
        {
            if (idConteo <= 0) throw new ArgumentException("Conteo inválido.", nameof(idConteo));
            if (idMaterial <= 0) throw new ArgumentException("Material inválido.", nameof(idMaterial));
            if (cantidadContada < 0) throw new ArgumentException("La cantidad contada no puede ser negativa.", nameof(cantidadContada));

            ConexionDB cone = new ConexionDB();
            using (var con = cone.GetConnection())
            {
                const string sql = @"
                    UPDATE public.conteos_inventario_detalle
                    SET cantidad_contada = @cantidad,
                        updated_at = NOW()
                    WHERE id_conteo = @idConteo
                      AND id_material = @idMaterial;";

                using (var cmd = new NpgsqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@idConteo", idConteo);
                    cmd.Parameters.AddWithValue("@idMaterial", idMaterial);
                    cmd.Parameters.AddWithValue("@cantidad", cantidadContada);
                    int filas = cmd.ExecuteNonQuery();
                    if (filas == 0)
                        throw new Exception("No se encontró el detalle para ese material en el conteo actual.");
                }
            }
        }

        public void CancelarConteo(int idConteo)
        {
            if (idConteo <= 0) throw new ArgumentException("Conteo inválido.", nameof(idConteo));

            ConexionDB cone = new ConexionDB();
            using (var con = cone.GetConnection())
            {
                const string sql = @"
                    UPDATE public.conteos_inventario
                    SET estado = 'cancelado',
                        fecha_fin = NOW(),
                        updated_at = NOW()
                    WHERE id_conteo = @idConteo
                      AND estado = 'abierto';";

                using (var cmd = new NpgsqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@idConteo", idConteo);
                    int filas = cmd.ExecuteNonQuery();
                    if (filas == 0)
                        throw new Exception("Solo se pueden cancelar conteos que estén en estado 'abierto'.");
                }
            }
        }

        public List<ConteoInventario> ObtenerHistorial(DateTime? desde = null, DateTime? hasta = null, string busqueda = null)
        {
            var lista = new List<ConteoInventario>();
            ConexionDB cone = new ConexionDB();
            using (var con = cone.GetConnection())
            {
                var condiciones = new List<string>();
                var parametros = new List<NpgsqlParameter>();

                if (desde.HasValue)
                {
                    condiciones.Add("c.fecha_inicio >= @desde");
                    parametros.Add(new NpgsqlParameter("@desde", desde.Value.Date));
                }
                if (hasta.HasValue)
                {
                    condiciones.Add("c.fecha_inicio <= @hasta");
                    parametros.Add(new NpgsqlParameter("@hasta", hasta.Value.Date.AddDays(1).AddSeconds(-1)));
                }
                if (!string.IsNullOrWhiteSpace(busqueda))
                {
                    condiciones.Add(@"(LOWER(COALESCE(c.usuario_inicio, '')) LIKE @busqueda
                                    OR LOWER(COALESCE(c.usuario_fin, '')) LIKE @busqueda
                                    OR LOWER(COALESCE(c.observaciones, '')) LIKE @busqueda
                                    OR EXISTS (
                                        SELECT 1
                                        FROM public.conteos_inventario_detalle db
                                        WHERE db.id_conteo = c.id_conteo
                                          AND (
                                              LOWER(COALESCE(db.material_nombre_snapshot, '')) LIKE @busqueda
                                              OR LOWER(COALESCE(db.material_categoria_snapshot, '')) LIKE @busqueda
                                          )
                                    ))");
                    parametros.Add(new NpgsqlParameter("@busqueda", "%" + busqueda.Trim().ToLower() + "%"));
                }

                string where = condiciones.Count > 0 ? "WHERE " + string.Join(" AND ", condiciones) : "";

                string sql = $@"
                    SELECT c.id_conteo, c.fecha_inicio, c.fecha_fin, c.usuario_inicio, c.usuario_fin,
                           c.estado, c.observaciones, c.created_at, c.updated_at,
                           COALESCE(SUM(CASE WHEN d.id_detalle IS NOT NULL THEN 1 ELSE 0 END), 0) AS cantidad_materiales,
                           COALESCE(SUM(CASE WHEN d.diferencia IS NOT NULL AND d.diferencia <> 0 THEN 1 ELSE 0 END), 0) AS cantidad_diferencias
                    FROM public.conteos_inventario c
                    LEFT JOIN public.conteos_inventario_detalle d ON d.id_conteo = c.id_conteo
                    {where}
                    GROUP BY c.id_conteo
                    ORDER BY c.fecha_inicio DESC, c.id_conteo DESC;";

                using (var cmd = new NpgsqlCommand(sql, con))
                {
                    foreach (var p in parametros) cmd.Parameters.Add(p);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var conteo = MapConteo(reader);
                            conteo.cantidad_materiales = Convert.ToInt32(reader.GetInt64(9));
                            conteo.cantidad_diferencias = Convert.ToInt32(reader.GetInt64(10));
                            lista.Add(conteo);
                        }
                    }
                }
            }
            return lista;
        }

        public List<ConteoInventarioDiferencia> ObtenerDiferencias(int idConteo)
        {
            var lista = new List<ConteoInventarioDiferencia>();
            ConexionDB cone = new ConexionDB();
            using (var con = cone.GetConnection())
            {
                const string sql = @"
                    SELECT id_detalle, id_conteo, id_material,
                           material_nombre_snapshot, material_categoria_snapshot,
                           cantidad_contada, stock_sistema, diferencia, fecha_carga
                    FROM public.conteos_inventario_detalle
                    WHERE id_conteo = @idConteo
                    ORDER BY material_nombre_snapshot ASC, id_detalle ASC;";

                using (var cmd = new NpgsqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@idConteo", idConteo);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lista.Add(new ConteoInventarioDiferencia
                            {
                                id_detalle = reader.GetInt32(0),
                                id_conteo = reader.GetInt32(1),
                                id_material = reader.GetInt32(2),
                                material_nombre_snapshot = reader.IsDBNull(3) ? null : reader.GetString(3),
                                material_categoria_snapshot = reader.IsDBNull(4) ? null : reader.GetString(4),
                                cantidad_contada = reader.GetDecimal(5),
                                stock_sistema = reader.IsDBNull(6) ? (decimal?)null : reader.GetDecimal(6),
                                diferencia = reader.IsDBNull(7) ? (decimal?)null : reader.GetDecimal(7),
                                fecha_carga = reader.GetDateTime(8)
                            });
                        }
                    }
                }
            }
            return lista;
        }

        private static (string nombre, string categoria)? ObtenerSnapshotMaterial(NpgsqlConnection con, NpgsqlTransaction tx, int idMaterial)
        {
            const string sql = @"
                SELECT m.nombre, c.nombre
                FROM public.materiales m
                LEFT JOIN public.categorias c ON c.id_categoria = m.id_categoria
                WHERE m.id = @idMaterial;";

            using (var cmd = new NpgsqlCommand(sql, con, tx ?? (NpgsqlTransaction)null))
            {
                cmd.Parameters.AddWithValue("@idMaterial", idMaterial);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        string nombre = reader.IsDBNull(0) ? null : reader.GetString(0);
                        string categoria = reader.IsDBNull(1) ? null : reader.GetString(1);
                        return (nombre, categoria);
                    }
                }
            }
            return null;
        }

        private static ConteoInventario MapConteo(NpgsqlDataReader reader)
        {
            return new ConteoInventario
            {
                id_conteo = reader.GetInt32(0),
                fecha_inicio = reader.GetDateTime(1),
                fecha_fin = reader.IsDBNull(2) ? (DateTime?)null : reader.GetDateTime(2),
                usuario_inicio = reader.IsDBNull(3) ? null : reader.GetString(3),
                usuario_fin = reader.IsDBNull(4) ? null : reader.GetString(4),
                estado = reader.IsDBNull(5) ? null : reader.GetString(5),
                observaciones = reader.IsDBNull(6) ? null : reader.GetString(6),
                created_at = reader.GetDateTime(7),
                updated_at = reader.GetDateTime(8)
            };
        }
    }
}
