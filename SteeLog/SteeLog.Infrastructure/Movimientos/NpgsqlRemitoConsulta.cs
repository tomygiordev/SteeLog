using System;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using SteeLog.Core.Application.Movimientos;
using SteeLog.Infrastructure.Persistence;

namespace SteeLog.Infrastructure.Movimientos
{
    public sealed class NpgsqlRemitoConsulta : IRemitoConsulta
    {
        private readonly PostgresConnectionFactory factory;
        public NpgsqlRemitoConsulta(PostgresConnectionFactory factory) { this.factory = factory ?? throw new ArgumentNullException(nameof(factory)); }
        public async Task<RemitoLectura> ObtenerAsync(int idMovimiento, CancellationToken token = default)
        {
            RemitoLectura remito = null;
            using (var conexion = factory.Create())
            {
                await conexion.OpenAsync(token).ConfigureAwait(false);
                using (var cmd = new NpgsqlCommand(@"
                    SELECT
                        m.id_movimiento,
                        m.fecha,
                        m.nroremito,
                        tm.nombre as tipo_movimiento,
                        COALESCE(mat.nombre, mm.material_nombre_eventual) as material_nombre,
                        mm.cantidad,
                        CASE
                            WHEN m.tipo_origen = 'OBRA' THEN COALESCE((SELECT nombre FROM obras WHERE id = m.id_origen), '-')
                            WHEN m.tipo_origen = 'PROVEEDOR' THEN COALESCE((SELECT nombre FROM proveedores WHERE id_proveedores = m.id_origen), '-')
                            WHEN m.tipo_origen = 'UBICACION' THEN COALESCE((SELECT nombre FROM ubicacion WHERE id_ubicacion = m.id_origen), '-')
                            ELSE '-'
                        END as origen_nombre,
                        CASE
                            WHEN m.tipo_destino = 'OBRA' THEN COALESCE((SELECT nombre FROM obras WHERE id = m.id_destino), '-')
                            WHEN m.tipo_destino = 'PROVEEDOR' THEN COALESCE((SELECT nombre FROM proveedores WHERE id_proveedores = m.id_destino), '-')
                            WHEN m.tipo_destino = 'UBICACION' THEN COALESCE((SELECT nombre FROM ubicacion WHERE id_ubicacion = m.id_destino), '-')
                            ELSE '-'
                        END as destino_nombre,
                        m.observaciones,
                        m.costoflete,
                        COALESCE(mm.esrecorte, false) as esrecorte,
                        CASE WHEN r.id IS NOT NULL THEN CONCAT(r.ancho, 'x', r.altura) ELSE NULL END as medida_recorte,
                        false as esinsumo,
                        COALESCE(mm.es_compra_eventual, false) as es_compra_eventual,
                        CASE
                            WHEN COALESCE(mm.es_compra_eventual, false) THEN COALESCE(mm.precio_unitario, 0)
                            ELSE COALESCE(mat.preciounitario, 0)
                        END as precio_unitario,
                        COALESCE(m.precio_total, 0) as precio_total
                    FROM public.movimientos m
                    INNER JOIN public.movimientomaterial mm ON m.id_movimiento = mm.id_movimiento
                    LEFT JOIN public.tipo_movimiento tm ON m.id_tipomovimiento = tm.id_tipomovimiento
                    LEFT JOIN public.materiales mat ON mm.id_material = mat.id
                    LEFT JOIN public.recortes r ON mm.id_recorte = r.id
                    WHERE m.id_movimiento = @idMov

                    UNION ALL

                    SELECT
                        m.id_movimiento,
                        m.fecha,
                        m.nroremito,
                        tm.nombre as tipo_movimiento,
                        i.nombre as material_nombre,
                        1 as cantidad,
                        im.obra_origen as origen_nombre,
                        im.obra_destino as destino_nombre,
                        m.observaciones,
                        m.costoflete,
                        false as esrecorte,
                        NULL as medida_recorte,
                        true as esinsumo,
                        false as es_compra_eventual,
                        0 as precio_unitario,
                        COALESCE(m.precio_total, 0) as precio_total
                    FROM public.movimientos m
                    INNER JOIN public.insumo_movimientos im ON m.id_movimiento = im.id_movimiento
                    INNER JOIN public.insumos i ON im.id_insumo = i.id_insumo
                    LEFT JOIN public.tipo_movimiento tm ON m.id_tipomovimiento = tm.id_tipomovimiento
                    WHERE m.id_movimiento = @idMov

                    ORDER BY material_nombre ASC
                ", conexion))
                {
                    cmd.Parameters.AddWithValue("idMov", idMovimiento);
                    using (var reader = await cmd.ExecuteReaderAsync(token).ConfigureAwait(false))
                    {
                        while (await reader.ReadAsync(token).ConfigureAwait(false))
                        {
                            if (remito == null)
                            {
                                remito = new RemitoLectura {
                                    IdMovimiento = reader.GetInt32(0), Fecha = reader.GetDateTime(1), FechaArchivo = DateTime.Now,
                                    NroRemito = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                    TipoMovimiento = reader.IsDBNull(3) ? "-" : reader.GetString(3),
                                    Origen = reader.IsDBNull(6) ? "-" : reader.GetString(6),
                                    Destino = reader.IsDBNull(7) ? "-" : reader.GetString(7),
                                    Observaciones = reader.IsDBNull(8) ? "" : reader.GetString(8),
                                    CostoFlete = reader.IsDBNull(9) ? 0 : Convert.ToDecimal(reader.GetValue(9)),
                                    PrecioTotal = Convert.ToDecimal(reader.GetValue(15))
                                };
                            }
                            remito.Lineas.Add(new RemitoLinea {
                                Material = reader.GetString(4), Cantidad = reader.IsDBNull(5) ? 0 : Convert.ToDecimal(reader.GetValue(5)),
                                EsRecorte = reader.GetBoolean(10), MedidaRecorte = reader.IsDBNull(11) ? "" : reader.GetString(11),
                                EsInsumo = reader.GetBoolean(12), EsCompraEventual = reader.GetBoolean(13),
                                PrecioUnitario = Convert.ToDecimal(reader.GetValue(14))
                            });
                        }
                    }
                }
            }
            return remito;
        }
    }
}
