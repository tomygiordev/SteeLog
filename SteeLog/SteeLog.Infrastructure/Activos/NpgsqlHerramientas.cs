using System;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using SteeLog.Core.Application.Activos;
using SteeLog.Domain.Activos;
using SteeLog.Infrastructure.Persistence;
namespace SteeLog.Infrastructure.Activos
{
    public sealed class NpgsqlHerramientas : IHerramientasEscritura
    {
        private readonly NpgsqlUnitOfWork uow;
        public NpgsqlHerramientas(NpgsqlUnitOfWork uow) { this.uow=uow ?? throw new ArgumentNullException(nameof(uow)); }
        private NpgsqlCommand Cmd(string sql) => new NpgsqlCommand(sql,uow.Connection,uow.Transaction) { CommandTimeout = 15 };
        private static object Db(object value) => value ?? DBNull.Value;
        private static void Agregar(NpgsqlCommand c,Herramienta h) { c.Parameters.AddWithValue("nombre",h.Nombre); c.Parameters.AddWithValue("estado",h.Estado); c.Parameters.AddWithValue("obra",Db(h.ObraId)); c.Parameters.AddWithValue("observaciones",Db(h.Observaciones)); }
        public async Task<Herramienta> BloquearAsync(int id,CancellationToken token=default)
        {
            using(var c=Cmd("SELECT nombre,estado,id_obra,observaciones FROM public.herramientas WHERE id_herramienta=@id FOR UPDATE")) { c.Parameters.AddWithValue("id",id); using(var r=await c.ExecuteReaderAsync(token).ConfigureAwait(false)) { if(!await r.ReadAsync(token).ConfigureAwait(false)) return null; return new Herramienta(id,r.GetString(0),r.GetString(1),r.IsDBNull(2)?(int?)null:r.GetInt32(2),r.IsDBNull(3)?null:r.GetString(3)); } }
        }
        public async Task<int> CrearAsync(Herramienta h,CancellationToken token=default)
        {
            int id; using(var c=Cmd("INSERT INTO public.herramientas(nombre,estado,id_obra,observaciones) VALUES(@nombre,@estado,@obra::integer,@observaciones::varchar) RETURNING id_herramienta")) { Agregar(c,h); id=Convert.ToInt32(await c.ExecuteScalarAsync(token).ConfigureAwait(false)); }
            h.AsignarIdentidad(id); using(var c=Cmd("UPDATE public.herramientas SET nombre=@nombre WHERE id_herramienta=@id")) { c.Parameters.AddWithValue("nombre",h.Nombre); c.Parameters.AddWithValue("id",id); await c.ExecuteNonQueryAsync(token).ConfigureAwait(false); }
            return id;
        }
        public async Task ActualizarAsync(Herramienta h,CancellationToken token=default)
        { using(var c=Cmd("UPDATE public.herramientas SET nombre=@nombre,estado=@estado,id_obra=@obra::integer,observaciones=@observaciones::varchar WHERE id_herramienta=@id")) { Agregar(c,h); c.Parameters.AddWithValue("id",h.Id); if(await c.ExecuteNonQueryAsync(token).ConfigureAwait(false)!=1) throw new InvalidOperationException("No se encontró la herramienta."); } }
        public async Task RegistrarHistorialAsync(int id,int? origen,int? destino,CancellationToken token=default)
        { using(var c=Cmd("INSERT INTO public.herramienta_movimientos(id_herramienta,obra_origen,obra_destino) VALUES(@id,CASE WHEN @origen::integer IS NULL THEN 'Galpón' ELSE COALESCE((SELECT nombre FROM public.obras WHERE id=@origen::integer),'Desconocida') END,CASE WHEN @destino::integer IS NULL THEN 'Galpón' ELSE COALESCE((SELECT nombre FROM public.obras WHERE id=@destino::integer),'Desconocida') END)")) { c.Parameters.AddWithValue("id",id); c.Parameters.AddWithValue("origen",Db(origen)); c.Parameters.AddWithValue("destino",Db(destino)); await c.ExecuteNonQueryAsync(token).ConfigureAwait(false); } }
        public async Task EliminarAsync(int id,CancellationToken token=default)
        { using(var c=Cmd("DELETE FROM public.herramienta_movimientos WHERE id_herramienta=@id; DELETE FROM public.herramientas WHERE id_herramienta=@id")) { c.Parameters.AddWithValue("id",id); await c.ExecuteNonQueryAsync(token).ConfigureAwait(false); } }
    }
}
