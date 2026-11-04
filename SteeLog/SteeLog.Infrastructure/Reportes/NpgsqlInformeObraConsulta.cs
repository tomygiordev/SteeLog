using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using SteeLog.Core.Application.Reportes;
using SteeLog.Infrastructure.Persistence;

namespace SteeLog.Infrastructure.Reportes
{
    public sealed class NpgsqlInformeObraConsulta : IInformeObraConsulta
    {
        private readonly PostgresConnectionFactory factory;
        public NpgsqlInformeObraConsulta(PostgresConnectionFactory factory) { this.factory = factory ?? throw new ArgumentNullException(nameof(factory)); }
        public async Task<IReadOnlyList<InformeMovimientoDetallePdf>> MovimientosAsync(int idObra, CancellationToken token)
        {
            var lista = new List<InformeMovimientoDetallePdf>();

            using (var conexion = factory.Create())
            {
                await conexion.OpenAsync(token).ConfigureAwait(false);

                string query = @"
                    SELECT
                        m.id_movimiento,
                        m.fecha,
                        m.nroremito,
                        tm.nombre as tipo_movimiento,
                        COALESCE(mat.nombre, mm.material_nombre_eventual) as material_nombre,
                        mm.cantidad,
                        CASE
                            WHEN m.tipo_origen = 'OBRA' THEN (SELECT nombre FROM obras WHERE id = m.id_origen)
                            WHEN m.tipo_origen = 'PROVEEDOR' THEN (SELECT nombre FROM proveedores WHERE id_proveedores = m.id_origen)
                            WHEN m.tipo_origen = 'UBICACION' THEN (SELECT nombre FROM ubicacion WHERE id_ubicacion = m.id_origen)
                            ELSE '-'
                        END as origen_nombre,
                        CASE
                            WHEN m.tipo_destino = 'OBRA' THEN (SELECT nombre FROM obras WHERE id = m.id_destino)
                            WHEN m.tipo_destino = 'PROVEEDOR' THEN (SELECT nombre FROM proveedores WHERE id_proveedores = m.id_destino)
                            WHEN m.tipo_destino = 'UBICACION' THEN (SELECT nombre FROM ubicacion WHERE id_ubicacion = m.id_destino)
                            ELSE '-'
                        END as destino_nombre,
                        m.observaciones,
                        COALESCE(mm.esrecorte, false) as esrecorte,
                        CASE WHEN r.id IS NOT NULL THEN CONCAT(r.ancho, 'x', r.altura) ELSE NULL END as medida_recorte,
                        COALESCE(mm.es_compra_eventual, false) as es_compra_eventual,
                        CASE
                            WHEN COALESCE(mm.es_compra_eventual, false) THEN COALESCE(mm.precio_unitario, 0)
                            ELSE COALESCE(mat.preciounitario, 0)
                        END as precio_unitario,
                        COALESCE(m.costoflete, 0) as costoflete,
                        COALESCE(m.precio_total, 0) as precio_total
                    FROM public.movimientos m
                    INNER JOIN public.movimientomaterial mm ON m.id_movimiento = mm.id_movimiento
                    LEFT JOIN public.tipo_movimiento tm ON m.id_tipomovimiento = tm.id_tipomovimiento
                    LEFT JOIN public.materiales mat ON mm.id_material = mat.id
                    LEFT JOIN public.recortes r ON mm.id_recorte = r.id
                    WHERE (m.tipo_destino = 'OBRA' AND m.id_destino = @idObra)
                       OR (m.tipo_origen = 'OBRA' AND m.id_origen = @idObra)
                    ORDER BY tm.nombre ASC, m.fecha ASC, m.nroremito ASC
                ";

                using (var cmd = new NpgsqlCommand(query, conexion))
                {
                    cmd.Parameters.AddWithValue("@idObra", idObra);

                    using (var reader = await cmd.ExecuteReaderAsync(token).ConfigureAwait(false))
                    {
                        while (await reader.ReadAsync(token).ConfigureAwait(false))
                        {
                            lista.Add(new InformeMovimientoDetallePdf
                            {
                                id_movimiento = reader.GetInt32(0),
                                fecha = reader.GetDateTime(1),
                                nroremito = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                tipo_movimiento = reader.IsDBNull(3) ? "-" : reader.GetString(3),
                                material = reader.GetString(4),
                                cantidad = reader.IsDBNull(5) ? 0 : reader.GetDecimal(5),
                                origen = reader.IsDBNull(6) ? "-" : reader.GetString(6),
                                destino = reader.IsDBNull(7) ? "-" : reader.GetString(7),
                                observaciones = reader.IsDBNull(8) ? "" : reader.GetString(8),
                                esrecorte = reader.GetBoolean(9),
                                medida_recorte = reader.IsDBNull(10) ? "" : reader.GetString(10),
                                es_compra_eventual = reader.GetBoolean(11),
                                precio_unitario = reader.GetDecimal(12),
                                costoflete = reader.IsDBNull(13) ? 0 : reader.GetDecimal(13),
                                precio_total = reader.IsDBNull(14) ? 0 : reader.GetDecimal(14)
                            });
                        }
                    }
                }
            }
            return lista;
        }

        public async Task<IReadOnlyList<InformeHerramientaDetallePdf>> HerramientasAsync(int idObra, string nombreObra, CancellationToken token)
        {
            var lista = new List<InformeHerramientaDetallePdf>();

            using (var conexion = factory.Create())
            {
                await conexion.OpenAsync(token).ConfigureAwait(false);

                // Obtener herramientas que estuvieron o están en esta obra
                string query = @"
                    SELECT DISTINCT h.id_herramienta, h.nombre, h.estado, h.id_obra,
                        (SELECT MIN(hm.fecha) FROM herramienta_movimientos hm
                         WHERE hm.id_herramienta = h.id_herramienta
                         AND hm.obra_destino = @nombreObra) as fecha_ingreso,
                        (SELECT MAX(hm.fecha) FROM herramienta_movimientos hm
                         WHERE hm.id_herramienta = h.id_herramienta
                         AND hm.obra_origen = @nombreObra) as fecha_retorno
                    FROM herramientas h
                    INNER JOIN herramienta_movimientos hm ON h.id_herramienta = hm.id_herramienta
                    WHERE hm.obra_destino = @nombreObra OR hm.obra_origen = @nombreObra
                    ORDER BY h.nombre ASC
                ";

                using (var cmd = new NpgsqlCommand(query, conexion))
                {
                    cmd.Parameters.AddWithValue("@nombreObra", nombreObra);

                    using (var reader = await cmd.ExecuteReaderAsync(token).ConfigureAwait(false))
                    {
                        while (await reader.ReadAsync(token).ConfigureAwait(false))
                        {
                            int? idObraActual = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3);
                            DateTime? fechaIngreso = reader.IsDBNull(4) ? (DateTime?)null : reader.GetDateTime(4);
                            DateTime? fechaRetorno = reader.IsDBNull(5) ? (DateTime?)null : reader.GetDateTime(5);

                            bool enObra = idObraActual.HasValue && idObraActual.Value == idObra;

                            lista.Add(new InformeHerramientaDetallePdf
                            {
                                id_herramienta = reader.GetInt32(0),
                                nombre = reader.GetString(1),
                                estado = reader.GetString(2),
                                fecha_ingreso = fechaIngreso,
                                fecha_retorno = enObra ? null : fechaRetorno,
                                en_obra = enObra
                            });
                        }
                    }
                }
            }
            return lista;
        }

        public async Task<IReadOnlyList<InformeInsumoDetallePdf>> InsumosAsync(int idObra, string nombreObra, CancellationToken token)
        {
            var lista = new List<InformeInsumoDetallePdf>();

            using (var conexion = factory.Create())
            {
                await conexion.OpenAsync(token).ConfigureAwait(false);

                string query = @"
                    SELECT DISTINCT i.nombre, i.consumido, i.id_obra,
                        (SELECT MIN(im.fecha) FROM insumo_movimientos im
                         WHERE im.id_insumo = i.id_insumo
                         AND im.obra_destino = @nombreObra) as fecha_ingreso,
                        (SELECT MAX(im.fecha) FROM insumo_movimientos im
                         WHERE im.id_insumo = i.id_insumo
                         AND im.obra_origen = @nombreObra) as fecha_retorno
                    FROM insumos i
                    INNER JOIN insumo_movimientos im ON i.id_insumo = im.id_insumo
                    WHERE im.obra_destino = @nombreObra OR im.obra_origen = @nombreObra
                    ORDER BY i.nombre ASC
                ";

                using (var cmd = new NpgsqlCommand(query, conexion))
                {
                    cmd.Parameters.AddWithValue("@nombreObra", nombreObra);

                    using (var reader = await cmd.ExecuteReaderAsync(token).ConfigureAwait(false))
                    {
                        while (await reader.ReadAsync(token).ConfigureAwait(false))
                        {
                            int? idObraActual = reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2);
                            DateTime? fechaIngreso = reader.IsDBNull(3) ? (DateTime?)null : reader.GetDateTime(3);
                            DateTime? fechaRetorno = reader.IsDBNull(4) ? (DateTime?)null : reader.GetDateTime(4);

                            bool enObra = idObraActual.HasValue && idObraActual.Value == idObra;

                            lista.Add(new InformeInsumoDetallePdf
                            {
                                nombre = reader.GetString(0),
                                consumido = reader.GetBoolean(1),
                                fecha_ingreso = fechaIngreso,
                                fecha_retorno = enObra ? null : fechaRetorno,
                                en_obra = enObra
                            });
                        }
                    }
                }
            }
            return lista;
        }

        public async Task<DateTime?> PrimeraFechaRemitoAsync(int idObra, CancellationToken token)
        {
            using (var conexion = factory.Create())
            {
                await conexion.OpenAsync(token).ConfigureAwait(false);

                string query = @"
                    SELECT MIN(m.fecha)
                    FROM movimientos m
                    WHERE ((m.tipo_destino = 'OBRA' AND m.id_destino = @idObra)
                       OR (m.tipo_origen = 'OBRA' AND m.id_origen = @idObra))
                      AND m.nroremito IS NOT NULL AND m.nroremito <> ''
                ";

                using (var cmd = new NpgsqlCommand(query, conexion))
                {
                    cmd.Parameters.AddWithValue("@idObra", idObra);
                    var result = await cmd.ExecuteScalarAsync(token).ConfigureAwait(false);
                    return result != null && result != DBNull.Value
                        ? Convert.ToDateTime(result)
                        : (DateTime?)null;
                }
            }
        }
    }
}
