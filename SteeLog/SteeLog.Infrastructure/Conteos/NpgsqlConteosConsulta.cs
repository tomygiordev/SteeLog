using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using NpgsqlTypes;
using SteeLog.Core.Application.Conteos;
using SteeLog.Infrastructure.Persistence;

namespace SteeLog.Infrastructure.Conteos
{
    public sealed class NpgsqlConteosConsulta : IConteosConsulta
    {
        private readonly PostgresConnectionFactory factory;
        public NpgsqlConteosConsulta(PostgresConnectionFactory factory) { this.factory = factory; }
        private const string Cabecera = @"SELECT c.id_conteo,c.fecha_inicio,c.fecha_fin,c.usuario_inicio,c.usuario_fin,c.estado,c.observaciones,
          (SELECT count(*)::integer FROM public.conteos_inventario_detalle d WHERE d.id_conteo=c.id_conteo),
          (SELECT count(*)::integer FROM public.conteos_inventario_detalle d WHERE d.id_conteo=c.id_conteo AND d.diferencia<>0) FROM public.conteos_inventario c ";
        private static ConteoLectura Mapear(NpgsqlDataReader r) => new ConteoLectura {
            Id=r.GetInt32(0),Inicio=r.GetDateTime(1),Fin=r.IsDBNull(2)?(DateTime?)null:r.GetDateTime(2),UsuarioInicio=r.IsDBNull(3)?null:r.GetString(3),
            UsuarioFin=r.IsDBNull(4)?null:r.GetString(4),Estado=r.IsDBNull(5)?null:r.GetString(5),Observaciones=r.IsDBNull(6)?null:r.GetString(6),Materiales=r.GetInt32(7),Diferencias=r.GetInt32(8) };
        public async Task<ConteoLectura> ObtenerAsync(int id, CancellationToken token = default)
        {
            using(var con=factory.Create()) { await con.OpenAsync(token).ConfigureAwait(false); using(var cmd=new NpgsqlCommand(Cabecera+"WHERE c.id_conteo=@id",con)) {
                cmd.Parameters.AddWithValue("id",id); using(var r=await cmd.ExecuteReaderAsync(token).ConfigureAwait(false)) return await r.ReadAsync(token).ConfigureAwait(false)?Mapear(r):null;
            } }
        }
        public async Task<ConteoLectura> AbiertoAsync(string email, CancellationToken token = default)
        {
            var legado = string.Equals(email,"admin@tesis.local",StringComparison.OrdinalIgnoreCase) ? "admin" : string.Equals(email,"galpon@tesis.local",StringComparison.OrdinalIgnoreCase) ? "galpon" : "";
            using(var con=factory.Create()) { await con.OpenAsync(token).ConfigureAwait(false); using(var cmd=new NpgsqlCommand(Cabecera+"WHERE c.estado='abierto' AND (lower(trim(c.usuario_inicio))=lower(@email) OR (@legado<>'' AND lower(trim(c.usuario_inicio))=@legado)) ORDER BY c.fecha_inicio DESC,c.id_conteo DESC LIMIT 1",con)) {
                cmd.Parameters.AddWithValue("email",email); cmd.Parameters.AddWithValue("legado",legado); using(var r=await cmd.ExecuteReaderAsync(token).ConfigureAwait(false)) return await r.ReadAsync(token).ConfigureAwait(false)?Mapear(r):null;
            } }
        }
        public async Task<IReadOnlyList<ConteoLectura>> HistorialAsync(DateTime? desde, DateTime? hasta, string busqueda, CancellationToken token = default)
        {
            var lista=new List<ConteoLectura>(); using(var con=factory.Create()) { await con.OpenAsync(token).ConfigureAwait(false);
                using(var cmd=new NpgsqlCommand(Cabecera+@"WHERE (@desde IS NULL OR c.fecha_inicio>=@desde) AND (@hasta IS NULL OR c.fecha_inicio<@hasta)
                 AND (@busqueda='' OR lower(coalesce(c.usuario_inicio,'')) LIKE @patron OR lower(coalesce(c.usuario_fin,'')) LIKE @patron OR lower(coalesce(c.observaciones,'')) LIKE @patron OR EXISTS
                 (SELECT 1 FROM public.conteos_inventario_detalle d WHERE d.id_conteo=c.id_conteo AND (lower(coalesce(d.material_nombre_snapshot,'')) LIKE @patron OR lower(coalesce(d.material_categoria_snapshot,'')) LIKE @patron))) ORDER BY c.fecha_inicio DESC,c.id_conteo DESC",con)) {
                    cmd.Parameters.Add("desde",NpgsqlDbType.Timestamp).Value=desde.HasValue?(object)desde.Value.Date:DBNull.Value;
                    cmd.Parameters.Add("hasta",NpgsqlDbType.Timestamp).Value=hasta.HasValue?(object)hasta.Value.Date.AddDays(1):DBNull.Value;
                    var buscar=(busqueda??"").Trim().ToLowerInvariant(); cmd.Parameters.AddWithValue("busqueda",buscar); cmd.Parameters.AddWithValue("patron","%"+buscar+"%");
                    using(var r=await cmd.ExecuteReaderAsync(token).ConfigureAwait(false)) while(await r.ReadAsync(token).ConfigureAwait(false)) lista.Add(Mapear(r));
                }
            } return lista;
        }
        public async Task<IReadOnlyList<DetalleConteoLectura>> DetallesAsync(int id, CancellationToken token = default)
        {
            var lista=new List<DetalleConteoLectura>(); using(var con=factory.Create()) { await con.OpenAsync(token).ConfigureAwait(false);
                using(var cmd=new NpgsqlCommand("SELECT id_detalle,id_conteo,id_material,material_nombre_snapshot,material_categoria_snapshot,cantidad_contada,stock_sistema,diferencia,fecha_carga FROM public.conteos_inventario_detalle WHERE id_conteo=@id ORDER BY material_nombre_snapshot,id_detalle",con)) {
                    cmd.Parameters.AddWithValue("id",id); using(var r=await cmd.ExecuteReaderAsync(token).ConfigureAwait(false)) while(await r.ReadAsync(token).ConfigureAwait(false)) lista.Add(new DetalleConteoLectura { Id=r.GetInt32(0),ConteoId=r.GetInt32(1),MaterialId=r.GetInt32(2),Nombre=r.IsDBNull(3)?null:r.GetString(3),Categoria=r.IsDBNull(4)?null:r.GetString(4),Cantidad=r.GetDecimal(5),Stock=r.IsDBNull(6)?(decimal?)null:r.GetDecimal(6),Diferencia=r.IsDBNull(7)?(decimal?)null:r.GetDecimal(7),FechaCarga=r.GetDateTime(8) });
                }
            } return lista;
        }
        public async Task<IReadOnlyDictionary<int, SeccionConteo>> SeccionesAsync(CancellationToken token = default)
        {
            var resultado = new Dictionary<int, SeccionConteo>();
            try {
                using (var con = factory.Create()) { await con.OpenAsync(token).ConfigureAwait(false);
                    using (var cmd = new NpgsqlCommand("SELECT rm.id_material,s.nombre,s.orden FROM public.reporte_stock_materiales rm JOIN public.reporte_stock_secciones s ON s.id=rm.id_seccion", con))
                    using (var r = await cmd.ExecuteReaderAsync(token).ConfigureAwait(false)) while (await r.ReadAsync(token).ConfigureAwait(false)) resultado[r.GetInt32(0)] = new SeccionConteo { Nombre = r.GetString(1), Orden = r.GetInt32(2) };
                }
            } catch (PostgresException ex) when (ex.SqlState == "42P01") { }
            return resultado;
        }
    }
}
