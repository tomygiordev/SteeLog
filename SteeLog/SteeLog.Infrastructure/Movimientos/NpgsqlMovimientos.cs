using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using SteeLog.Core.Application.Movimientos;
using SteeLog.Infrastructure.Persistence;

namespace SteeLog.Infrastructure.Movimientos
{
    public sealed class NpgsqlMovimientos : IMovimientosEscritura
    {
        private readonly NpgsqlUnitOfWork uow;
        public NpgsqlMovimientos(NpgsqlUnitOfWork uow) { this.uow = uow; }
        private NpgsqlCommand Cmd(string sql) => new NpgsqlCommand(sql, uow.Connection, uow.Transaction);
        private static object Db(object x) => x ?? DBNull.Value;
        private static int? Int(NpgsqlDataReader r, int i) => r.IsDBNull(i) ? (int?)null : r.GetInt32(i);
        private static decimal? Dec(NpgsqlDataReader r, int i) => r.IsDBNull(i) ? (decimal?)null : r.GetDecimal(i);
        private static string Str(NpgsqlDataReader r, int i) => r.IsDBNull(i) ? null : r.GetString(i);
        private static void Parametros(NpgsqlCommand c, CabeceraMovimiento h)
        {
            c.Parameters.AddWithValue("fecha", h.Fecha.Date); c.Parameters.AddWithValue("remito", Db(string.IsNullOrWhiteSpace(h.Remito) ? null : h.Remito.Trim()));
            c.Parameters.AddWithValue("origen", h.OrigenId); c.Parameters.AddWithValue("tipo_origen", Db(h.TipoOrigen));
            c.Parameters.AddWithValue("destino", h.DestinoId); c.Parameters.AddWithValue("tipo_destino", Db(h.TipoDestino));
            c.Parameters.AddWithValue("tipo", h.TipoMovimientoId); c.Parameters.AddWithValue("obra", Db(h.ObraId));
            c.Parameters.AddWithValue("flete", h.CostoFlete); c.Parameters.AddWithValue("id_flete", Db(h.FleteId));
            c.Parameters.AddWithValue("observaciones", Db(h.Observaciones)); c.Parameters.AddWithValue("precio", h.PrecioTotal);
        }
        public async Task<MovimientoPersistido> BloquearAsync(int id, CancellationToken token = default)
        {
            MovimientoPersistido m;
            using (var c = Cmd(@"SELECT m.fecha,m.nroremito,m.id_origen,m.tipo_origen,m.id_destino,m.tipo_destino,m.id_tipomovimiento,m.id_obra,m.costoflete,m.id_flete,m.observaciones,m.precio_total,t.nombre
                FROM public.movimientos m LEFT JOIN public.tipo_movimiento t ON t.id_tipomovimiento=m.id_tipomovimiento WHERE m.id_movimiento=@id FOR UPDATE OF m"))
            {
                c.Parameters.AddWithValue("id", id);
                using (var r = await c.ExecuteReaderAsync(token).ConfigureAwait(false))
                {
                    if (!await r.ReadAsync(token).ConfigureAwait(false)) return null;
                    m = new MovimientoPersistido { Id = id, TipoNombre = Str(r,12), Cabecera = new CabeceraMovimiento { Fecha=r.GetDateTime(0), Remito=Str(r,1), OrigenId=Int(r,2) ?? 0, TipoOrigen=Str(r,3), DestinoId=r.GetInt32(4), TipoDestino=Str(r,5), TipoMovimientoId=Int(r,6) ?? 0, ObraId=Int(r,7), CostoFlete=Dec(r,8) ?? 0, FleteId=Int(r,9), Observaciones=Str(r,10), PrecioTotal=Dec(r,11) ?? 0 } };
                }
            }
            using (var c = Cmd(@"SELECT id_material,id_recorte,medida_recorte,cantidad,material_nombre_eventual,es_compra_eventual,precio_unitario,impacto_stock_real,esrecorte FROM public.movimientomaterial WHERE id_movimiento=@id ORDER BY id_movimientomaterial"))
            {
                c.Parameters.AddWithValue("id",id);
                using (var r = await c.ExecuteReaderAsync(token).ConfigureAwait(false))
                    while (await r.ReadAsync(token).ConfigureAwait(false))
                    {
                        bool esRecorte=!r.IsDBNull(8) && r.GetBoolean(8);
                        if (esRecorte != !r.IsDBNull(1)) throw new InvalidOperationException("No se puede revertir una línea histórica de recorte sin identidad coherente.");
                        m.Lineas.Add(new LineaMovimiento { MaterialId=Int(r,0), RecorteId=Int(r,1), MedidaRecorte=Dec(r,2), Cantidad=Dec(r,3) ?? 0, MaterialEventual=Str(r,4), CompraEventual=!r.IsDBNull(5) && r.GetBoolean(5), EsRecorte=esRecorte, PrecioUnitario=Dec(r,6) ?? 0, ImpactoReal=Dec(r,7) });
                    }
            }
            using (var c = Cmd(@"SELECT id,id_insumo,obra_origen,obra_destino,id_obra_origen_snapshot,id_obra_destino_snapshot,consumido_origen_snapshot,consumido_destino_snapshot,snapshot_completo FROM public.insumo_movimientos WHERE id_movimiento=@id ORDER BY id"))
            {
                c.Parameters.AddWithValue("id",id);
                using (var r = await c.ExecuteReaderAsync(token).ConfigureAwait(false))
                    while (await r.ReadAsync(token).ConfigureAwait(false))
                    {
                        if (r.IsDBNull(1)) throw new InvalidOperationException("Historial de insumo sin identidad: no se puede revertir.");
                        m.Insumos.Add(new TrasladoInsumo { HistorialId=r.GetInt32(0), InsumoId=r.GetInt32(1), Origen=Str(r,2), Destino=Str(r,3), ObraOrigenId=Int(r,4), ObraDestinoId=Int(r,5), ConsumidoOrigen=!r.IsDBNull(6) && r.GetBoolean(6), ConsumidoDestino=!r.IsDBNull(7) && r.GetBoolean(7), SnapshotCompleto=!r.IsDBNull(8) && r.GetBoolean(8) });
                    }
            }
            return m;
        }
        public async Task<string> NombreTipoAsync(int id, CancellationToken token = default)
        {
            using (var c=Cmd("SELECT nombre FROM public.tipo_movimiento WHERE id_tipomovimiento=@id")) { c.Parameters.AddWithValue("id",id); var x=await c.ExecuteScalarAsync(token).ConfigureAwait(false); if (x==null || x==DBNull.Value) throw new InvalidOperationException("No se encontró el tipo de movimiento."); return Convert.ToString(x); }
        }
        public async Task<IDictionary<int,int>> ObtenerMaterialesRecortesAsync(IReadOnlyList<int> ids, CancellationToken token = default)
        {
            var resultado=new Dictionary<int,int>();
            foreach (var id in ids.Distinct().OrderBy(x=>x)) using (var c=Cmd("SELECT id_material FROM public.recortes WHERE id=@id"))
            { c.Parameters.AddWithValue("id",id); var x=await c.ExecuteScalarAsync(token).ConfigureAwait(false); if(x==null || x==DBNull.Value) throw new InvalidOperationException("No se encontró el material del recorte."); resultado.Add(id,Convert.ToInt32(x)); }
            return resultado;
        }
        public async Task<IDictionary<int,decimal>> BloquearRecortesAsync(IReadOnlyList<int> ids, CancellationToken token = default)
        {
            var res=new Dictionary<int,decimal>();
            foreach (var id in ids.Distinct().OrderBy(x=>x)) using (var c=Cmd("SELECT cantidad FROM public.recortes WHERE id=@id FOR UPDATE")) { c.Parameters.AddWithValue("id",id); var x=await c.ExecuteScalarAsync(token).ConfigureAwait(false); if(x==null || x==DBNull.Value) throw new InvalidOperationException("No se encontró el recorte."); res.Add(id,Convert.ToDecimal(x)); }
            return res;
        }
        public async Task<IDictionary<int,InsumoBloqueado>> BloquearInsumosAsync(IReadOnlyList<int> ids, CancellationToken token = default)
        {
            var res=new Dictionary<int,InsumoBloqueado>();
            foreach (var id in ids.Distinct().OrderBy(x=>x)) using (var c=Cmd("SELECT id_obra, COALESCE(consumido,false) FROM public.insumos WHERE id_insumo=@id FOR UPDATE"))
            {
                c.Parameters.AddWithValue("id",id); using(var r=await c.ExecuteReaderAsync(token).ConfigureAwait(false)) { if(!await r.ReadAsync(token).ConfigureAwait(false)) throw new InvalidOperationException("No se encontró el insumo."); res.Add(id,new InsumoBloqueado { Id=id, ObraId=Int(r,0), Consumido=r.GetBoolean(1) }); }
                using(var h=Cmd("SELECT MAX(id) FROM public.insumo_movimientos WHERE id_insumo=@id")) { h.Parameters.AddWithValue("id",id); var x=await h.ExecuteScalarAsync(token).ConfigureAwait(false); res[id].UltimoHistorialId=x==null || x==DBNull.Value ? (int?)null : Convert.ToInt32(x); }
            }
            return res;
        }
        public async Task<decimal> GuardarRecorteAsync(int id, decimal saldo, CancellationToken token=default)
        {
            using(var c=Cmd("UPDATE public.recortes SET cantidad=@saldo WHERE id=@id RETURNING cantidad")) { c.Parameters.AddWithValue("id",id); c.Parameters.AddWithValue("saldo",saldo); var x=await c.ExecuteScalarAsync(token).ConfigureAwait(false); if(x==null) throw new InvalidOperationException("No se encontró el recorte."); return Convert.ToDecimal(x); }
        }
        public async Task<int> ResolverRecorteAsync(int materialId, decimal ancho, decimal altura, decimal medida, CancellationToken token = default)
        {
            using (var c = Cmd("SELECT id FROM public.recortes WHERE id_material=@m AND COALESCE(ancho,0)=@a AND COALESCE(altura,0)=@h AND medida=@med ORDER BY id LIMIT 1 FOR UPDATE"))
            {
                c.CommandTimeout = 15;
                c.Parameters.AddWithValue("m", materialId);
                c.Parameters.AddWithValue("a", ancho);
                c.Parameters.AddWithValue("h", altura);
                c.Parameters.AddWithValue("med", medida);
                var id = await c.ExecuteScalarAsync(token).ConfigureAwait(false);
                if (id != null && id != DBNull.Value) return Convert.ToInt32(id);
            }
            using (var c = Cmd("INSERT INTO public.recortes (id_material, ancho, altura, medida, cantidad, estado) VALUES (@m, @a, @h, @med, 0, NULL) RETURNING id"))
            {
                c.CommandTimeout = 15;
                c.Parameters.AddWithValue("m", materialId);
                c.Parameters.AddWithValue("a", ancho > 0 ? (object)ancho : DBNull.Value);
                c.Parameters.AddWithValue("h", altura > 0 ? (object)altura : DBNull.Value);
                c.Parameters.AddWithValue("med", medida);
                return Convert.ToInt32(await c.ExecuteScalarAsync(token).ConfigureAwait(false));
            }
        }
        public async Task<int> CrearCabeceraAsync(CabeceraMovimiento h, CancellationToken token=default)
        {
            try { using(var c=Cmd(@"INSERT INTO public.movimientos(fecha,nroremito,id_origen,tipo_origen,id_destino,tipo_destino,id_tipomovimiento,id_obra,costoflete,id_flete,observaciones,precio_total) VALUES(@fecha,@remito::varchar,@origen,@tipo_origen::varchar,@destino,@tipo_destino::varchar,@tipo,@obra::integer,@flete,@id_flete::integer,@observaciones::text,@precio) RETURNING id_movimiento")) { Parametros(c,h); return Convert.ToInt32(await c.ExecuteScalarAsync(token).ConfigureAwait(false)); } }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation && ex.ConstraintName == "movimientos_nroremito_key")
            { throw new InvalidOperationException($"Ya existe un movimiento con el remito '{(h.Remito ?? string.Empty).Trim()}'.", ex); }
        }
        public async Task ActualizarCabeceraAsync(int id,CabeceraMovimiento h,CancellationToken token=default)
        {
            try { using(var c=Cmd(@"UPDATE public.movimientos SET fecha=@fecha,nroremito=@remito::varchar,id_origen=@origen,tipo_origen=@tipo_origen::varchar,id_destino=@destino,tipo_destino=@tipo_destino::varchar,id_tipomovimiento=@tipo,id_obra=@obra::integer,costoflete=@flete,id_flete=@id_flete::integer,observaciones=@observaciones::text,precio_total=@precio WHERE id_movimiento=@id")) { Parametros(c,h); c.Parameters.AddWithValue("id",id); if(await c.ExecuteNonQueryAsync(token).ConfigureAwait(false)!=1) throw new InvalidOperationException("No se encontró el movimiento."); } }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation && ex.ConstraintName == "movimientos_nroremito_key")
            { throw new InvalidOperationException($"Ya existe un movimiento con el remito '{(h.Remito ?? string.Empty).Trim()}'.", ex); }
        }
        public async Task AgregarLineaAsync(int id,LineaMovimiento l,decimal saldo,CancellationToken token=default)
        {
            if (l.RecorteId.HasValue)
            {
                using (var verificar = Cmd("SELECT id_material FROM public.recortes WHERE id=@id"))
                {
                    verificar.Parameters.AddWithValue("id", l.RecorteId.Value);
                    var propietario = await verificar.ExecuteScalarAsync(token).ConfigureAwait(false);
                    if (propietario == null || propietario == DBNull.Value || !l.MaterialId.HasValue || Convert.ToInt32(propietario) != l.MaterialId.Value)
                        throw new InvalidOperationException("El recorte no pertenece al material indicado.");
                }
            }
            using(var c=Cmd(@"INSERT INTO public.movimientomaterial(id_movimiento,id_material,cantidad,stock_actual,esrecorte,id_recorte,medida_recorte,material_nombre_eventual,es_compra_eventual,precio_unitario,impacto_stock_real) VALUES(@id,@material::integer,@cantidad,@saldo,@esrecorte,@recorte::integer,@medida::numeric,@nombre::text,@eventual,@precio,@impacto::numeric)"))
            { c.Parameters.AddWithValue("id",id); c.Parameters.AddWithValue("material",Db(l.MaterialId)); c.Parameters.AddWithValue("cantidad",l.Cantidad); c.Parameters.AddWithValue("saldo",saldo); c.Parameters.AddWithValue("esrecorte",l.RecorteId.HasValue); c.Parameters.AddWithValue("recorte",Db(l.RecorteId)); c.Parameters.AddWithValue("medida",Db(l.MedidaRecorte)); c.Parameters.AddWithValue("nombre",Db(l.MaterialEventual)); c.Parameters.AddWithValue("eventual",l.CompraEventual); c.Parameters.AddWithValue("precio",l.PrecioUnitario); c.Parameters.AddWithValue("impacto",Db(l.ImpactoReal)); await c.ExecuteNonQueryAsync(token).ConfigureAwait(false); }
        }
        public async Task RegistrarTrasladoAsync(int id,TrasladoInsumo t,CancellationToken token=default)
        {
            using(var c=Cmd("UPDATE public.insumos SET id_obra=@obra::integer, consumido=@consumido WHERE id_insumo=@id")) { c.Parameters.AddWithValue("obra",Db(t.ObraDestinoId)); c.Parameters.AddWithValue("consumido",t.ConsumidoDestino); c.Parameters.AddWithValue("id",t.InsumoId); if(await c.ExecuteNonQueryAsync(token).ConfigureAwait(false)!=1) throw new InvalidOperationException("No se encontró el insumo."); }
            using(var c=Cmd(@"INSERT INTO public.insumo_movimientos(id_insumo,obra_origen,obra_destino,id_movimiento,id_obra_origen_snapshot,id_obra_destino_snapshot,consumido_origen_snapshot,consumido_destino_snapshot,snapshot_completo,nombre_insumo) VALUES(@insumo,@origen,@destino,@movimiento,@obra_origen::integer,@obra_destino::integer,@cons_origen,@cons_destino,true,(SELECT nombre FROM public.insumos WHERE id_insumo=@insumo))"))
            { c.Parameters.AddWithValue("insumo",t.InsumoId); c.Parameters.AddWithValue("origen",t.Origen ?? "Galpón"); c.Parameters.AddWithValue("destino",t.Destino ?? "Galpón"); c.Parameters.AddWithValue("movimiento",id); c.Parameters.AddWithValue("obra_origen",Db(t.ObraOrigenId)); c.Parameters.AddWithValue("obra_destino",Db(t.ObraDestinoId)); c.Parameters.AddWithValue("cons_origen",t.ConsumidoOrigen); c.Parameters.AddWithValue("cons_destino",t.ConsumidoDestino); await c.ExecuteNonQueryAsync(token).ConfigureAwait(false); }
        }
        public async Task RevertirTrasladoAsync(TrasladoInsumo t,CancellationToken token=default)
        { using(var c=Cmd("UPDATE public.insumos SET id_obra=@obra::integer,consumido=@consumido WHERE id_insumo=@id")) { c.Parameters.AddWithValue("obra",Db(t.ObraOrigenId)); c.Parameters.AddWithValue("consumido",t.ConsumidoOrigen); c.Parameters.AddWithValue("id",t.InsumoId); if(await c.ExecuteNonQueryAsync(token).ConfigureAwait(false)!=1) throw new InvalidOperationException("No se encontró el insumo."); } }
        public async Task EliminarLineasYTrasladosAsync(int id,CancellationToken token=default)
        { using(var c=Cmd("DELETE FROM public.insumo_movimientos WHERE id_movimiento=@id; DELETE FROM public.movimientomaterial WHERE id_movimiento=@id")) { c.Parameters.AddWithValue("id",id); await c.ExecuteNonQueryAsync(token).ConfigureAwait(false); } }
        public async Task EliminarCabeceraAsync(int id,CancellationToken token=default)
        { using(var c=Cmd("DELETE FROM public.movimientos WHERE id_movimiento=@id")) { c.Parameters.AddWithValue("id",id); if(await c.ExecuteNonQueryAsync(token).ConfigureAwait(false)!=1) throw new InvalidOperationException("No se encontró el movimiento."); } }
    }
}
