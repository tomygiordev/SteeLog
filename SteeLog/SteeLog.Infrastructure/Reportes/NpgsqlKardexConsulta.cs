using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using SteeLog.Core.Application.Reportes;
using SteeLog.Domain.Inventario;
using SteeLog.Infrastructure.Persistence;
namespace SteeLog.Infrastructure.Reportes
{
    public sealed class NpgsqlKardexConsulta : IKardexConsulta
    {
        private readonly PostgresConnectionFactory factory;
        public NpgsqlKardexConsulta(PostgresConnectionFactory factory) { this.factory = factory ?? throw new ArgumentNullException(nameof(factory)); }
        public async Task<List<KardexMaterial>> MaterialesAsync(CancellationToken token = default)
        {
            var resultado = new List<KardexMaterial>();
            using (var c = factory.Create()) { await c.OpenAsync(token).ConfigureAwait(false);
                using (var cmd = new NpgsqlCommand("SELECT m.id,m.nombre,c.nombre FROM materiales m INNER JOIN categorias c ON m.id_categoria=c.id_categoria ORDER BY m.nombre ASC", c))
                using (var r = await cmd.ExecuteReaderAsync(token).ConfigureAwait(false))
                    while (await r.ReadAsync(token).ConfigureAwait(false)) resultado.Add(new KardexMaterial { id = r.GetInt32(0), nombre = r.GetString(1), nombre_categoria = r.GetString(2) });
            }
            return resultado;
        }
        public async Task<List<KardexItem>> HistorialAsync(int? material, DateTime? desde, DateTime? hasta, CancellationToken token = default)
        {
            var resultado = new List<KardexItem>();
            using (var c = factory.Create()) { await c.OpenAsync(token).ConfigureAwait(false);
                string sql = @"SELECT m.fecha,tm.nombre,mm.cantidad,
                    CASE WHEN m.tipo_origen='OBRA' THEN (SELECT nombre FROM obras WHERE id=m.id_origen) WHEN m.tipo_origen='PROVEEDOR' THEN (SELECT nombre FROM proveedores WHERE id_proveedores=m.id_origen) WHEN m.tipo_origen='UBICACION' THEN (SELECT nombre FROM ubicacion WHERE id_ubicacion=m.id_origen) ELSE 'Desconocido' END,
                    CASE WHEN m.tipo_destino='OBRA' THEN (SELECT nombre FROM obras WHERE id=m.id_destino) WHEN m.tipo_destino='PROVEEDOR' THEN (SELECT nombre FROM proveedores WHERE id_proveedores=m.id_destino) WHEN m.tipo_destino='UBICACION' THEN (SELECT nombre FROM ubicacion WHERE id_ubicacion=m.id_destino) ELSE 'Desconocido' END,
                    m.tipo_destino,mm.stock_actual,m.id_movimiento,mm.id_material,COALESCE(mat.nombre,mm.material_nombre_eventual)
                    FROM public.movimientos m INNER JOIN public.movimientomaterial mm ON m.id_movimiento=mm.id_movimiento LEFT JOIN public.tipo_movimiento tm ON m.id_tipomovimiento=tm.id_tipomovimiento LEFT JOIN public.materiales mat ON mm.id_material=mat.id WHERE 1=1";
                if (material.HasValue) sql += " AND mm.id_material=@material";
                if (desde.HasValue) sql += " AND m.fecha>=@desde";
                if (hasta.HasValue) sql += " AND m.fecha<=@hasta";
                sql += " ORDER BY m.fecha DESC, m.id_movimiento DESC";
                using (var cmd = new NpgsqlCommand(sql, c)) {
                    if (material.HasValue) cmd.Parameters.AddWithValue("material", material.Value);
                    if (desde.HasValue) cmd.Parameters.AddWithValue("desde", desde.Value);
                    if (hasta.HasValue) cmd.Parameters.AddWithValue("hasta", hasta.Value);
                    using (var r = await cmd.ExecuteReaderAsync(token).ConfigureAwait(false)) {
                        decimal acumulado = 0;
                        while (await r.ReadAsync(token).ConfigureAwait(false)) {
                            var item = new KardexItem { fecha=r.GetDateTime(0), tipo_movimiento=r.IsDBNull(1)?"-":r.GetString(1), cantidad=r.IsDBNull(2)?0:r.GetDecimal(2), origen=r.IsDBNull(3)?"-":r.GetString(3), destino=r.IsDBNull(4)?"-":r.GetString(4), id_movimiento=r.GetInt32(7), id_material=r.IsDBNull(8)?(int?)null:r.GetInt32(8), nombre_material=r.IsDBNull(9)?"":r.GetString(9) };
                            var impacto = ClasificadorMovimiento.Clasificar(item.tipo_movimiento, r.IsDBNull(5)?null:r.GetString(5));
                            if (impacto == ImpactoStock.Entrada) acumulado += item.cantidad;
                            else if (impacto == ImpactoStock.Salida) acumulado -= item.cantidad;
                            if (!r.IsDBNull(6)) acumulado = r.GetDecimal(6);
                            item.saldo = acumulado; resultado.Add(item);
                        }
                    }
                }
            }
            return resultado;
        }
    }
}
