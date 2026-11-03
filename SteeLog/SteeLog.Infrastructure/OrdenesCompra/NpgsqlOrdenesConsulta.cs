using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using SteeLog.Core.Application.OrdenesCompra;
using SteeLog.Infrastructure.Persistence;

namespace SteeLog.Infrastructure.OrdenesCompra
{
    public sealed class NpgsqlOrdenesConsulta : IOrdenesConsulta
    {
        private readonly PostgresConnectionFactory factory;
        public NpgsqlOrdenesConsulta(PostgresConnectionFactory factory) { this.factory = factory; }
        private const string Cabecera = @"SELECT oc.id_orden_compra,oc.fecha,oc.nro_orden,COALESCE(oc.nro_remito,''),COALESCE(oc.id_proveedor,0),COALESCE(p.nombre,'-'),COALESCE(oc.id_destino,0),COALESCE(u.nombre,'-'),COALESCE(oc.costoflete,0),COALESCE(oc.observaciones,''),oc.estado,COALESCE(oc.usuario_creacion,''),COALESCE(oc.usuario_revision,''),oc.fecha_revision,COALESCE(oc.observacion_revision,'') FROM public.ordenes_compra oc LEFT JOIN public.proveedores p ON p.id_proveedores=oc.id_proveedor LEFT JOIN public.ubicacion u ON u.id_ubicacion=oc.id_destino ";
        private static OrdenLectura Mapear(NpgsqlDataReader r) => new OrdenLectura {
            Id=r.GetInt32(0), Fecha=r.GetDateTime(1), Numero=r.GetString(2), Remito=r.GetString(3), ProveedorId=r.GetInt32(4), Proveedor=r.GetString(5), DestinoId=r.GetInt32(6), Destino=r.GetString(7), Flete=r.GetDecimal(8), Observaciones=r.GetString(9), Estado=r.GetString(10), UsuarioCreacion=r.GetString(11), UsuarioRevision=r.GetString(12), FechaRevision=r.IsDBNull(13) ? (DateTime?)null : r.GetDateTime(13), ObservacionRevision=r.GetString(14)
        };
        public async Task<IReadOnlyList<OrdenLectura>> ListarAsync(string estado, bool soloAbiertas, bool historial, bool excluirCompletas, CancellationToken token = default)
        {
            var lista=new List<OrdenLectura>();
            using (var con=factory.Create()) { await con.OpenAsync(token).ConfigureAwait(false);
                string sql=Cabecera.Replace(" FROM public.ordenes_compra oc", ", (SELECT COUNT(*)::integer FROM public.ordenes_compra_detalle d WHERE d.id_orden_compra=oc.id_orden_compra), (SELECT COALESCE(SUM(cantidad_pedida),0) FROM public.ordenes_compra_detalle d WHERE d.id_orden_compra=oc.id_orden_compra), (SELECT COALESCE(SUM(cantidad_recibida),0) FROM public.ordenes_compra_detalle d WHERE d.id_orden_compra=oc.id_orden_compra) FROM public.ordenes_compra oc")+@"WHERE (@todos OR oc.estado=@estado) AND (NOT @abiertas OR oc.estado IN ('Pendiente','Recibida Parcial')) AND (NOT @historial OR oc.estado IN ('Recibida Parcial','Recibida Completa')) AND (NOT @excluir OR oc.estado<>'Recibida Completa') ORDER BY oc.fecha DESC,oc.id_orden_compra DESC";
                using(var cmd=new NpgsqlCommand(sql,con)) {
                    cmd.Parameters.AddWithValue("todos", string.IsNullOrWhiteSpace(estado)); cmd.Parameters.AddWithValue("estado", estado ?? ""); cmd.Parameters.AddWithValue("abiertas", soloAbiertas); cmd.Parameters.AddWithValue("historial",historial); cmd.Parameters.AddWithValue("excluir",excluirCompletas);
                    using(var r=await cmd.ExecuteReaderAsync(token).ConfigureAwait(false)) while(await r.ReadAsync(token).ConfigureAwait(false)) { var o=Mapear(r); o.CantidadItems=r.GetInt32(15); o.TotalPedido=Convert.ToDecimal(r.GetValue(16)); o.TotalRecibido=Convert.ToDecimal(r.GetValue(17)); lista.Add(o); }
                }

            }
            return lista;
        }
        public async Task<OrdenLectura> ObtenerAsync(int id, CancellationToken token = default)
        {
            using(var con=factory.Create()) { await con.OpenAsync(token).ConfigureAwait(false); OrdenLectura orden;
                using(var cmd=new NpgsqlCommand(Cabecera+"WHERE oc.id_orden_compra=@id",con)) { cmd.Parameters.AddWithValue("id",id); using(var r=await cmd.ExecuteReaderAsync(token).ConfigureAwait(false)) { if(!await r.ReadAsync(token).ConfigureAwait(false)) return null; orden=Mapear(r); } }
                using(var cmd=new NpgsqlCommand(@"SELECT d.id_detalle,d.id_orden_compra,d.id_material,m.nombre,d.cantidad_pedida,d.cantidad_recibida,d.cantidad_faltante,COALESCE(d.observacion_item,''),COALESCE(d.stock_al_crear,0),d.stock_despues_recepcion FROM public.ordenes_compra_detalle d JOIN public.materiales m ON m.id=d.id_material WHERE d.id_orden_compra=@id ORDER BY m.nombre",con)) {
                    cmd.Parameters.AddWithValue("id",id); using(var r=await cmd.ExecuteReaderAsync(token).ConfigureAwait(false)) while(await r.ReadAsync(token).ConfigureAwait(false)) orden.Lineas.Add(new LineaLectura { Id=r.GetInt32(0),OrdenId=r.GetInt32(1),MaterialId=r.GetInt32(2),Material=r.GetString(3),Pedida=Convert.ToDecimal(r.GetValue(4)),Recibida=Convert.ToDecimal(r.GetValue(5)),Faltante=Convert.ToDecimal(r.GetValue(6)),Observacion=r.GetString(7),StockAlCrear=r.GetDecimal(8),StockDespues=r.IsDBNull(9) ? (decimal?)null:r.GetDecimal(9) });
                }
                orden.CantidadItems=orden.Lineas.Count; orden.TotalPedido=orden.Lineas.Sum(x=>x.Pedida); orden.TotalRecibido=orden.Lineas.Sum(x=>x.Recibida); return orden;
            }
        }
        public async Task<IReadOnlyList<ProveedorLectura>> ProveedoresAsync(CancellationToken token = default)
        {
            var lista=new List<ProveedorLectura>(); using(var con=factory.Create()) { await con.OpenAsync(token).ConfigureAwait(false); using(var cmd=new NpgsqlCommand("SELECT id_proveedores,nombre FROM public.proveedores",con)) using(var r=await cmd.ExecuteReaderAsync(token).ConfigureAwait(false)) while(await r.ReadAsync(token).ConfigureAwait(false)) lista.Add(new ProveedorLectura { Id=r.GetInt32(0),Nombre=r.GetString(1) }); } return lista;
        }
        public async Task<IReadOnlyList<MaterialLectura>> MaterialesAsync(CancellationToken token = default)
        {
            var lista=new List<MaterialLectura>(); using(var con=factory.Create()) { await con.OpenAsync(token).ConfigureAwait(false); using(var cmd=new NpgsqlCommand("SELECT m.id,m.nombre,m.stockactual,m.stockminimo,c.nombre FROM public.materiales m INNER JOIN public.categorias c ON c.id_categoria=m.id_categoria ORDER BY m.nombre",con)) using(var r=await cmd.ExecuteReaderAsync(token).ConfigureAwait(false)) while(await r.ReadAsync(token).ConfigureAwait(false)) lista.Add(new MaterialLectura { Id=r.GetInt32(0),Nombre=r.GetString(1),Stock=r.GetDecimal(2),Minimo=r.GetDecimal(3),Categoria=r.GetString(4) }); } return lista;
        }
        public async Task<string> DestinoAsync(CancellationToken token = default)
        {
            using(var con=factory.Create()) { await con.OpenAsync(token).ConfigureAwait(false); using(var cmd=new NpgsqlCommand("SELECT nombre FROM public.ubicacion ORDER BY CASE WHEN UPPER(nombre) LIKE '%GALPON%' THEN 0 WHEN UPPER(nombre) LIKE '%DEPÓSITO%' OR UPPER(nombre) LIKE '%DEPOSITO%' THEN 1 ELSE 2 END,nombre LIMIT 1",con)) { var x=await cmd.ExecuteScalarAsync(token).ConfigureAwait(false); if(x==null || x==DBNull.Value) throw new InvalidOperationException("No se encontró una ubicación para Galpón."); return (string)x; } }
        }
        public async Task<IReadOnlyList<PendienteLectura>> PendientesAsync(CancellationToken token = default)
        {
            var lista=new List<PendienteLectura>(); using(var con=factory.Create()) { await con.OpenAsync(token).ConfigureAwait(false); using(var cmd=new NpgsqlCommand(@"SELECT oc.nro_orden,COALESCE(p.nombre,'-'),m.nombre,d.cantidad_pedida,d.cantidad_recibida,d.cantidad_faltante,oc.estado FROM public.ordenes_compra oc JOIN public.ordenes_compra_detalle d ON oc.id_orden_compra=d.id_orden_compra JOIN public.materiales m ON d.id_material=m.id LEFT JOIN public.proveedores p ON oc.id_proveedor=p.id_proveedores WHERE oc.estado IN ('Pendiente','Recibida Parcial') AND d.cantidad_faltante>0 ORDER BY oc.id_orden_compra,m.nombre",con)) using(var r=await cmd.ExecuteReaderAsync(token).ConfigureAwait(false)) while(await r.ReadAsync(token).ConfigureAwait(false)) lista.Add(new PendienteLectura { Numero=r.GetString(0),Proveedor=r.GetString(1),Material=r.GetString(2),Pedida=Convert.ToDecimal(r.GetValue(3)),Recibida=Convert.ToDecimal(r.GetValue(4)),Faltante=Convert.ToDecimal(r.GetValue(5)),Estado=r.GetString(6) }); } return lista;
        }
    }
}
