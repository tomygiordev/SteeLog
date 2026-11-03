using System;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using SteeLog.Core.Application.Movimientos;
using SteeLog.Infrastructure.Persistence;
namespace SteeLog.Infrastructure.Movimientos
{
    public sealed class NpgsqlMovimientoEdicionConsulta : IMovimientoEdicionConsulta
    {
        private readonly PostgresConnectionFactory factory;
        public NpgsqlMovimientoEdicionConsulta(PostgresConnectionFactory factory) { this.factory=factory; }
        private static int? Int(NpgsqlDataReader r,int i) => r.IsDBNull(i)?(int?)null:r.GetInt32(i);
        private static decimal? Dec(NpgsqlDataReader r,int i) => r.IsDBNull(i)?(decimal?)null:r.GetDecimal(i);
        private static string Str(NpgsqlDataReader r,int i) => r.IsDBNull(i)?null:r.GetString(i);
        public async Task<MovimientoEdicion> ObtenerAsync(int id,CancellationToken token=default)
        {
            using(var cn=factory.Create())
            {
                await cn.OpenAsync(token).ConfigureAwait(false);
                using(var tx=cn.BeginTransaction(System.Data.IsolationLevel.RepeatableRead))
                {
                    MovimientoEdicion m;
                    using(var c=new NpgsqlCommand("SELECT fecha,nroremito,id_origen,tipo_origen,id_destino,tipo_destino,id_tipomovimiento,id_obra,costoflete,id_flete,observaciones,precio_total FROM public.movimientos WHERE id_movimiento=@id",cn,tx))
                    { c.Parameters.AddWithValue("id",id); using(var r=await c.ExecuteReaderAsync(token).ConfigureAwait(false)) { if(!await r.ReadAsync(token).ConfigureAwait(false)) return null; m=new MovimientoEdicion { Cabecera=new CabeceraMovimiento { Fecha=r.GetDateTime(0),Remito=Str(r,1),OrigenId=Int(r,2)??0,TipoOrigen=Str(r,3),DestinoId=r.GetInt32(4),TipoDestino=Str(r,5),TipoMovimientoId=Int(r,6)??0,ObraId=Int(r,7),CostoFlete=Dec(r,8)??0,FleteId=Int(r,9),Observaciones=Str(r,10),PrecioTotal=Dec(r,11)??0 } }; } }
                    using(var c=new NpgsqlCommand(@"SELECT mm.id_material,mm.id_recorte,mm.medida_recorte,mm.cantidad,mm.material_nombre_eventual,mm.es_compra_eventual,mm.precio_unitario,COALESCE(mat.nombre,mm.material_nombre_eventual,''),mm.esrecorte FROM public.movimientomaterial mm LEFT JOIN public.materiales mat ON mat.id=mm.id_material WHERE mm.id_movimiento=@id ORDER BY mm.id_movimientomaterial",cn,tx))
                    { c.Parameters.AddWithValue("id",id); using(var r=await c.ExecuteReaderAsync(token).ConfigureAwait(false)) while(await r.ReadAsync(token).ConfigureAwait(false)) { bool esRecorte=!r.IsDBNull(8) && r.GetBoolean(8); if(esRecorte != !r.IsDBNull(1)) throw new InvalidOperationException("Línea histórica de recorte sin identidad coherente."); m.Lineas.Add(new LineaEdicion { Nombre=Str(r,7),Linea=new LineaMovimiento { MaterialId=Int(r,0),RecorteId=Int(r,1),MedidaRecorte=Dec(r,2),Cantidad=Dec(r,3)??0,MaterialEventual=Str(r,4),CompraEventual=!r.IsDBNull(5)&&r.GetBoolean(5),PrecioUnitario=Dec(r,6)??0 } }); } }
                    using(var c=new NpgsqlCommand(@"SELECT im.id_insumo,COALESCE(i.nombre,im.nombre_insumo,'') FROM public.insumo_movimientos im LEFT JOIN public.insumos i ON i.id_insumo=im.id_insumo WHERE im.id_movimiento=@id ORDER BY im.id",cn,tx))
                    { c.Parameters.AddWithValue("id",id); using(var r=await c.ExecuteReaderAsync(token).ConfigureAwait(false)) while(await r.ReadAsync(token).ConfigureAwait(false)) { if(r.IsDBNull(0)) throw new InvalidOperationException("El historial del movimiento contiene un insumo sin identidad."); m.Lineas.Add(new LineaEdicion { InsumoId=r.GetInt32(0),Nombre=Str(r,1) }); } }
                    tx.Commit(); return m;
                }
            }
        }
    }
}
