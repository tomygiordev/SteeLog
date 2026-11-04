using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using SteeLog.Core.Application.Activos;
using SteeLog.Infrastructure.Persistence;
namespace SteeLog.Infrastructure.Activos
{
    public sealed class NpgsqlHerramientasConsulta : IHerramientasConsulta
    {
        private readonly PostgresConnectionFactory factory;
        public NpgsqlHerramientasConsulta(PostgresConnectionFactory factory) { this.factory=factory; }
        public async Task<IReadOnlyList<HerramientaLectura>> HerramientasAsync(CancellationToken token=default)
        {
            var lista=new List<HerramientaLectura>(); using(var con=factory.Create()) { await con.OpenAsync(token).ConfigureAwait(false); using(var c=new NpgsqlCommand("SELECT h.id_herramienta,h.nombre,h.estado,h.id_obra,COALESCE(o.nombre,'Galpón'),h.observaciones FROM public.herramientas h LEFT JOIN public.obras o ON o.id=h.id_obra ORDER BY h.nombre",con)) using(var r=await c.ExecuteReaderAsync(token).ConfigureAwait(false)) while(await r.ReadAsync(token).ConfigureAwait(false)) lista.Add(new HerramientaLectura { Id=r.GetInt32(0),Nombre=r.GetString(1),Estado=r.GetString(2),ObraId=r.IsDBNull(3)?(int?)null:r.GetInt32(3),Obra=r.GetString(4),Observaciones=r.IsDBNull(5)?"":r.GetString(5) }); } return lista;
        }
        public async Task<IReadOnlyList<ObraHerramientaLectura>> ObrasAsync(CancellationToken token=default)
        {
            var lista=new List<ObraHerramientaLectura>(); using(var con=factory.Create()) { await con.OpenAsync(token).ConfigureAwait(false); using(var c=new NpgsqlCommand("SELECT id,nombre FROM public.obras",con)) using(var r=await c.ExecuteReaderAsync(token).ConfigureAwait(false)) while(await r.ReadAsync(token).ConfigureAwait(false)) lista.Add(new ObraHerramientaLectura { Id=r.GetInt32(0),Nombre=r.GetString(1) }); } return lista;
        }
        public async Task<IReadOnlyList<HistorialHerramientaLectura>> HistorialAsync(CancellationToken token=default)
        {
            var lista=new List<HistorialHerramientaLectura>(); using(var con=factory.Create()) { await con.OpenAsync(token).ConfigureAwait(false); using(var c=new NpgsqlCommand("SELECT hm.id,h.id_herramienta,h.nombre,h.estado,hm.obra_origen,hm.obra_destino,hm.fecha FROM public.herramienta_movimientos hm JOIN public.herramientas h ON h.id_herramienta=hm.id_herramienta ORDER BY hm.fecha DESC",con)) using(var r=await c.ExecuteReaderAsync(token).ConfigureAwait(false)) while(await r.ReadAsync(token).ConfigureAwait(false)) lista.Add(new HistorialHerramientaLectura { Id=r.GetInt32(0),HerramientaId=r.GetInt32(1),Nombre=r.GetString(2),Estado=r.IsDBNull(3)?"":r.GetString(3),Origen=r.GetString(4),Destino=r.GetString(5),Fecha=r.GetDateTime(6) }); } return lista;
        }
    }
}
