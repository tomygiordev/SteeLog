using System;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using SteeLog.Core.Application.Activos;
using SteeLog.Domain.Activos;
using SteeLog.Infrastructure.Persistence;

namespace SteeLog.Infrastructure.Activos
{
    public sealed class NpgsqlInsumos : IInsumosEscritura
    {
        private readonly NpgsqlUnitOfWork uow;
        public NpgsqlInsumos(NpgsqlUnitOfWork uow) { this.uow=uow; }
        private NpgsqlCommand Cmd(string sql) => new NpgsqlCommand(sql,uow.Connection,uow.Transaction);
        private static object Db(object value) => value ?? DBNull.Value;
        public async Task<Insumo> BloquearAsync(int id,CancellationToken token=default)
        {
            using(var c=Cmd("SELECT nombre,id_obra,COALESCE(consumido,false),observaciones FROM public.insumos WHERE id_insumo=@id FOR UPDATE"))
            { c.Parameters.AddWithValue("id",id); using(var r=await c.ExecuteReaderAsync(token).ConfigureAwait(false)) { if(!await r.ReadAsync(token).ConfigureAwait(false)) return null; return new Insumo(id,r.GetString(0),r.IsDBNull(1)?(int?)null:r.GetInt32(1),r.GetBoolean(2),r.IsDBNull(3)?null:r.GetString(3)); } }
        }
        private static void Agregar(NpgsqlCommand c,Insumo i)
        { c.Parameters.AddWithValue("nombre",i.Nombre); c.Parameters.AddWithValue("obra",Db(i.ObraId)); c.Parameters.AddWithValue("consumido",i.Consumido); c.Parameters.AddWithValue("observaciones",Db(i.Observaciones)); }
        public async Task<int> CrearAsync(Insumo i,CancellationToken token=default)
        { using(var c=Cmd("INSERT INTO public.insumos(nombre,id_obra,consumido,observaciones) VALUES(@nombre,@obra::integer,@consumido,@observaciones::text) RETURNING id_insumo")) { Agregar(c,i); return Convert.ToInt32(await c.ExecuteScalarAsync(token).ConfigureAwait(false)); } }
        public async Task ActualizarAsync(Insumo i,CancellationToken token=default)
        { using(var c=Cmd("UPDATE public.insumos SET nombre=@nombre,id_obra=@obra::integer,consumido=@consumido,observaciones=@observaciones::text WHERE id_insumo=@id")) { Agregar(c,i); c.Parameters.AddWithValue("id",i.Id); if(await c.ExecuteNonQueryAsync(token).ConfigureAwait(false)!=1) throw new InvalidOperationException("No se encontró el insumo."); } }
        public async Task RegistrarHistorialAsync(int id,int? origen,int? destino,bool consumidoOrigen,bool consumidoDestino,CancellationToken token=default)
        {
            using(var c=Cmd(@"INSERT INTO public.insumo_movimientos(id_insumo,obra_origen,obra_destino,id_obra_origen_snapshot,id_obra_destino_snapshot,consumido_origen_snapshot,consumido_destino_snapshot,snapshot_completo) VALUES(@id,COALESCE((SELECT nombre FROM public.obras WHERE id=@origen::integer),'Galpón'),COALESCE((SELECT nombre FROM public.obras WHERE id=@destino::integer),'Galpón'),@origen::integer,@destino::integer,@cons_origen,@cons_destino,true)"))
            { c.Parameters.AddWithValue("id",id); c.Parameters.AddWithValue("origen",Db(origen)); c.Parameters.AddWithValue("destino",Db(destino)); c.Parameters.AddWithValue("cons_origen",consumidoOrigen); c.Parameters.AddWithValue("cons_destino",consumidoDestino); await c.ExecuteNonQueryAsync(token).ConfigureAwait(false); }
        }
        public async Task EliminarAsync(int id,CancellationToken token=default)
        {
            using(var c=Cmd("SELECT EXISTS(SELECT 1 FROM public.insumo_movimientos WHERE id_insumo=@id AND id_movimiento IS NOT NULL)")) { c.Parameters.AddWithValue("id",id); if(Convert.ToBoolean(await c.ExecuteScalarAsync(token).ConfigureAwait(false))) throw new InvalidOperationException("No se puede eliminar el insumo mientras tenga movimientos vinculados."); }
            using(var c=Cmd("DELETE FROM public.insumo_movimientos WHERE id_insumo=@id; DELETE FROM public.insumos WHERE id_insumo=@id")) { c.Parameters.AddWithValue("id",id); await c.ExecuteNonQueryAsync(token).ConfigureAwait(false); }
        }
    }
}
