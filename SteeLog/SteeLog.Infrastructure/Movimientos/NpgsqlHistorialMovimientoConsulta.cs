using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using NpgsqlTypes;
using SteeLog.Core.Application.Movimientos;
using SteeLog.Infrastructure.Persistence;
namespace SteeLog.Infrastructure.Movimientos
{
    public sealed class NpgsqlHistorialMovimientoConsulta : IHistorialMovimientoConsulta
    {
        private readonly PostgresConnectionFactory factory;
        public NpgsqlHistorialMovimientoConsulta(PostgresConnectionFactory factory) { this.factory=factory; }
        public async Task<IReadOnlyList<HistorialMovimientoLectura>> ListarAsync(DateTime? desde,DateTime? hasta,string tipo,CancellationToken token=default)
        {
            var lista=new List<HistorialMovimientoLectura>();
            using(var cn=factory.Create())
            {
                await cn.OpenAsync(token).ConfigureAwait(false);
                using(var c=new NpgsqlCommand(@"SELECT * FROM (
                    SELECT m.id_movimiento,mm.id_material,m.fecha,m.nroremito,tm.nombre,COALESCE(mat.nombre,mm.material_nombre_eventual,''),mm.cantidad,
                    CASE m.tipo_origen WHEN 'OBRA' THEN (SELECT nombre FROM obras WHERE id=m.id_origen) WHEN 'PROVEEDOR' THEN (SELECT nombre FROM proveedores WHERE id_proveedores=m.id_origen) WHEN 'UBICACION' THEN (SELECT nombre FROM ubicacion WHERE id_ubicacion=m.id_origen) ELSE '-' END,
                    CASE m.tipo_destino WHEN 'OBRA' THEN (SELECT nombre FROM obras WHERE id=m.id_destino) WHEN 'PROVEEDOR' THEN (SELECT nombre FROM proveedores WHERE id_proveedores=m.id_destino) WHEN 'UBICACION' THEN (SELECT nombre FROM ubicacion WHERE id_ubicacion=m.id_destino) ELSE '-' END,
                    m.observaciones,COALESCE(mm.esrecorte,false),mm.medida_recorte,false,COALESCE(mm.es_compra_eventual,false),CASE WHEN mm.es_compra_eventual THEN COALESCE(mm.precio_unitario,0) ELSE COALESCE(mat.preciounitario,0) END,COALESCE(m.precio_total,0),mm.id_movimientomaterial
                    FROM public.movimientos m JOIN public.movimientomaterial mm ON mm.id_movimiento=m.id_movimiento LEFT JOIN public.tipo_movimiento tm ON tm.id_tipomovimiento=m.id_tipomovimiento LEFT JOIN public.materiales mat ON mat.id=mm.id_material
                    WHERE (@desde IS NULL OR m.fecha>=@desde) AND (@hasta IS NULL OR m.fecha<=@hasta) AND (@tipo IS NULL OR tm.nombre=@tipo)
                    UNION ALL
                    SELECT m.id_movimiento,i.id_insumo,m.fecha,m.nroremito,tm.nombre,i.nombre,1::numeric,im.obra_origen,im.obra_destino,m.observaciones,false,NULL::numeric,true,false,0::numeric,0::numeric,NULL::integer
                    FROM public.movimientos m JOIN public.insumo_movimientos im ON im.id_movimiento=m.id_movimiento JOIN public.insumos i ON i.id_insumo=im.id_insumo LEFT JOIN public.tipo_movimiento tm ON tm.id_tipomovimiento=m.id_tipomovimiento
                    WHERE (@desde IS NULL OR m.fecha>=@desde) AND (@hasta IS NULL OR m.fecha<=@hasta) AND (@tipo IS NULL OR tm.nombre=@tipo)
                    ) x ORDER BY 3 DESC,1 DESC",cn))
                {
                    c.Parameters.Add("desde",NpgsqlDbType.Date).Value=(object)desde?.Date ?? DBNull.Value;
                    c.Parameters.Add("hasta",NpgsqlDbType.Date).Value=(object)hasta?.Date ?? DBNull.Value;
                    c.Parameters.Add("tipo",NpgsqlDbType.Text).Value=(object)tipo ?? DBNull.Value;
                    using(var r=await c.ExecuteReaderAsync(token).ConfigureAwait(false)) while(await r.ReadAsync(token).ConfigureAwait(false))
                        lista.Add(new HistorialMovimientoLectura { IdMovimiento=r.GetInt32(0),IdMaterial=r.IsDBNull(1)?(int?)null:r.GetInt32(1),Fecha=r.GetDateTime(2),Remito=r.IsDBNull(3)?"":r.GetString(3),Tipo=r.IsDBNull(4)?"-":r.GetString(4),Material=r.IsDBNull(5)?"":r.GetString(5),Cantidad=r.IsDBNull(6)?0:r.GetDecimal(6),Origen=r.IsDBNull(7)?"-":r.GetString(7),Destino=r.IsDBNull(8)?"-":r.GetString(8),Observaciones=r.IsDBNull(9)?"":r.GetString(9),EsRecorte=r.GetBoolean(10),Medida=r.IsDBNull(11)?(decimal?)null:r.GetDecimal(11),EsInsumo=r.GetBoolean(12),Eventual=r.GetBoolean(13),PrecioUnitario=r.IsDBNull(14)?0:r.GetDecimal(14),PrecioTotal=r.IsDBNull(15)?0:r.GetDecimal(15),IdLinea=r.IsDBNull(16)?(int?)null:r.GetInt32(16) });
                }
            }
            return lista;
        }
    }
}
