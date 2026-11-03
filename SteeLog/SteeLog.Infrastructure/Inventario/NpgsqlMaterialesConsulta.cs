using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using NpgsqlTypes;
using SteeLog.Core.Application.Inventario;
using SteeLog.Infrastructure.Persistence;
namespace SteeLog.Infrastructure.Inventario
{
    public sealed class NpgsqlMaterialesConsulta : IMaterialesConsulta
    {
        private readonly PostgresConnectionFactory factory;
        public NpgsqlMaterialesConsulta(PostgresConnectionFactory factory) { this.factory=factory; }
        public async Task<IReadOnlyList<MaterialInventarioLectura>> ListarAsync(FiltroMaterialLectura f, CancellationToken token = default)
        {
            var result = new List<MaterialInventarioLectura>();
            using (var c=factory.Create()) {
                await c.OpenAsync(token).ConfigureAwait(false);
                using (var q=c.CreateCommand()) {
                    var where=new List<string>();
                    if (!string.IsNullOrEmpty(f.Nombre)) { where.Add("m.nombre ILIKE @nombre"); q.Parameters.AddWithValue("nombre", "%"+f.Nombre+"%"); }
                    if (f.Desde.HasValue) { where.Add("m.stockactual>=@desde"); q.Parameters.AddWithValue("desde", f.Desde.Value); }
                    if (f.Hasta.HasValue) { where.Add("m.stockactual<=@hasta"); q.Parameters.AddWithValue("hasta", f.Hasta.Value); }
                    if (f.Categorias != null && f.Categorias.Count>0) { where.Add("m.id_categoria=ANY(@categorias)"); q.Parameters.AddWithValue("categorias", f.Categorias.ToArray()); }
                    if (f.Proveedores != null && f.Proveedores.Count>0) { where.Add("EXISTS(SELECT 1 FROM public.materiales_proveedores mp WHERE mp.id_material=m.id AND mp.id_proveedor=ANY(@proveedores))"); q.Parameters.AddWithValue("proveedores", f.Proveedores.ToArray()); }
                    if (f.SoloStockBajo || f.Priorizados) where.Add("m.stockminimo>0 AND m.stockactual<=m.stockminimo");
                    if (f.Priorizados) where.Add("uso.movimientos>0");
                    q.CommandText=@"SELECT m.id,m.nombre,m.id_categoria,c.nombre,COALESCE(m.preciounitario,0),COALESCE(m.stockactual,0),COALESCE(m.stockminimo,0),uso.cantidad,uso.movimientos,m.id_ubicacion FROM public.materiales m JOIN public.categorias c ON c.id_categoria=m.id_categoria LEFT JOIN LATERAL (SELECT COALESCE(SUM(ABS(mm.cantidad)),0) cantidad,COUNT(*) movimientos FROM public.movimientomaterial mm WHERE mm.id_material=m.id) uso ON true" + (where.Count>0 ? " WHERE "+string.Join(" AND ",where) : "") + (f.Priorizados ? " ORDER BY uso.cantidad DESC,uso.movimientos DESC,(m.stockminimo-m.stockactual) DESC,m.nombre ASC" : " ORDER BY m.nombre ASC");
                    using (var r=await q.ExecuteReaderAsync(token).ConfigureAwait(false)) while (await r.ReadAsync(token).ConfigureAwait(false)) result.Add(new MaterialInventarioLectura { Id=r.GetInt32(0),Nombre=r.GetString(1),CategoriaId=r.GetInt32(2),Categoria=r.GetString(3),Precio=r.GetDecimal(4),Stock=r.GetDecimal(5),Minimo=r.GetDecimal(6),CantidadUsada=r.GetDecimal(7),CantidadMovimientos=checked((int)r.GetInt64(8)),UbicacionId=r.IsDBNull(9)?(int?)null:r.GetInt32(9) });
                }
            }
            return result;
        }
        public async Task<IReadOnlyList<CategoriaMaterialLectura>> CategoriasAsync(CancellationToken token = default)
        {
            var result=new List<CategoriaMaterialLectura>(); using(var c=factory.Create()) { await c.OpenAsync(token).ConfigureAwait(false); using(var q=c.CreateCommand()) { q.CommandText="SELECT id_categoria,nombre,recortable FROM public.categorias"; using(var r=await q.ExecuteReaderAsync(token).ConfigureAwait(false)) while(await r.ReadAsync(token).ConfigureAwait(false)) result.Add(new CategoriaMaterialLectura { Id=r.GetInt32(0),Nombre=r.GetString(1),Recortable=r.GetBoolean(2) }); } } return result;
        }
        public async Task<IReadOnlyList<ProveedorMaterialLectura>> CatalogoProveedoresAsync(CancellationToken token = default)
        {
            var lista = new List<ProveedorMaterialLectura>();
            using (var c = factory.Create())
            {
                await c.OpenAsync(token).ConfigureAwait(false);
                using (var cmd = new NpgsqlCommand("SELECT id_proveedores, nombre FROM public.proveedores ORDER BY nombre ASC", c) { CommandTimeout = 15 })
                using (var r = await cmd.ExecuteReaderAsync(token).ConfigureAwait(false))
                    while (await r.ReadAsync(token).ConfigureAwait(false))
                        lista.Add(new ProveedorMaterialLectura { Id = r.GetInt32(0), Nombre = r.GetString(1) });
            }
            return lista;
        }
        public async Task<IReadOnlyList<int>> ProveedoresAsync(int materialId, CancellationToken token = default)
        {
            var result=new List<int>(); using(var c=factory.Create()) { await c.OpenAsync(token).ConfigureAwait(false); using(var q=c.CreateCommand()) { q.CommandText="SELECT id_proveedor FROM public.materiales_proveedores WHERE id_material=@id"; q.Parameters.AddWithValue("id",materialId); using(var r=await q.ExecuteReaderAsync(token).ConfigureAwait(false)) while(await r.ReadAsync(token).ConfigureAwait(false)) result.Add(r.GetInt32(0)); } } return result;
        }
    }
}
