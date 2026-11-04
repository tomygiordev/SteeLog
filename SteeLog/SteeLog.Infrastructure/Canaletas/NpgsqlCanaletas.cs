using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using SteeLog.Core.Application.Canaletas;
using SteeLog.Domain.Canaletas;
using SteeLog.Infrastructure.Persistence;
namespace SteeLog.Infrastructure.Canaletas
{
    public sealed class NpgsqlCanaletasConsulta : ICanaletasConsulta
    {
        private readonly PostgresConnectionFactory factory;
        public NpgsqlCanaletasConsulta(PostgresConnectionFactory factory)
        {
            this.factory = factory;
        }

        private const string Base = @"SELECT c.id,c.id_obra,o.nombre,c.descripcion,c.precio,c.pagado,c.fecha_creacion,(SELECT count(*)::int FROM public.canaletas_plegados_fotos f WHERE f.canaleta_plegado_id=c.id) FROM public.canaletas_plegados c JOIN public.obras o ON o.id=c.id_obra ";

        private static CanaletaLectura Canaleta(NpgsqlDataReader r) => new CanaletaLectura
        {
            Id = r.GetInt32(0),
            ObraId = r.GetInt32(1),
            NombreObra = r.IsDBNull(2) ? "" : r.GetString(2),
            Descripcion = r.IsDBNull(3) ? "" : r.GetString(3),
            Precio = r.IsDBNull(4) ? 0 : r.GetDecimal(4),
            Pagado = !r.IsDBNull(5) && r.GetBoolean(5),
            FechaCreacion = r.IsDBNull(6) ? DateTime.MinValue : r.GetDateTime(6),
            CantidadFotos = r.GetInt32(7)
        }
;

        internal static FotoCanaletaLectura Foto(NpgsqlDataReader r) => new FotoCanaletaLectura
        {
            Id = r.GetInt32(0),
            CanaletaId = r.GetInt32(1),
            ObjectPath = r.GetString(2),
            NombreArchivo = r.GetString(3),
            ContentType = r.GetString(4),
            TamanioBytes = r.GetInt64(5),
            Orden = r.GetInt32(6),
            FechaCreacion = r.IsDBNull(7) ? DateTime.MinValue : r.GetDateTime(7)
        }
;

        internal const string FotosSql = "SELECT id,canaleta_plegado_id,object_path,nombre_archivo,content_type,tamanio_bytes,orden,fecha_creacion FROM public.canaletas_plegados_fotos ";

        public async Task<IReadOnlyList<CanaletaLectura>> ListarAsync(int? obraId, CancellationToken t)
        {
            var lista = new List<CanaletaLectura>();
            using (var c = factory.Create())
            {
                await c.OpenAsync(t).ConfigureAwait(false);
                using (var q = new NpgsqlCommand(Base + (obraId.GetValueOrDefault() > 0 ? "WHERE c.id_obra=@id " : "") + "ORDER BY c.fecha_creacion DESC,c.id DESC", c))
                {
                    if (obraId.GetValueOrDefault() > 0) q.Parameters.AddWithValue("id", obraId.Value);
                    using (var r = await q.ExecuteReaderAsync(t).ConfigureAwait(false)) while (await r.ReadAsync(t).ConfigureAwait(false)) lista.Add(Canaleta(r));
                }
            }
            return lista;
        }

        public async Task<CanaletaLectura> ObtenerAsync(int id, CancellationToken t)
        {
            using (var c = factory.Create())
            {
                await c.OpenAsync(t).ConfigureAwait(false);
                using (var q = new NpgsqlCommand(Base + "WHERE c.id=@id", c))
                {
                    q.Parameters.AddWithValue("id", id);
                    using (var r = await q.ExecuteReaderAsync(t).ConfigureAwait(false)) return await r.ReadAsync(t).ConfigureAwait(false) ? Canaleta(r) : null;
                }
            }
        }

        public async Task<IReadOnlyList<FotoCanaletaLectura>> FotosAsync(int id, CancellationToken t)
        {
            var lista = new List<FotoCanaletaLectura>();
            using (var c = factory.Create())
            {
                await c.OpenAsync(t).ConfigureAwait(false);
                using (var q = new NpgsqlCommand(FotosSql + "WHERE canaleta_plegado_id=@id ORDER BY orden,id", c))
                {
                    q.Parameters.AddWithValue("id", id);
                    using (var r = await q.ExecuteReaderAsync(t).ConfigureAwait(false)) while (await r.ReadAsync(t).ConfigureAwait(false)) lista.Add(Foto(r));
                }
            }
            return lista;
        }

        public async Task<FotoCanaletaLectura> FotoPorPathAsync(string path, CancellationToken t)
        {
            using (var c = factory.Create())
            {
                await c.OpenAsync(t).ConfigureAwait(false);
                using (var q = new NpgsqlCommand(FotosSql + "WHERE object_path=@path ORDER BY id LIMIT 1", c))
                {
                    q.Parameters.AddWithValue("path", path);
                    using (var r = await q.ExecuteReaderAsync(t).ConfigureAwait(false))
                        return await r.ReadAsync(t).ConfigureAwait(false) ? Foto(r) : null;
                }
            }
        }

        public async Task<bool> ReferenciadaAsync(string path, CancellationToken t)
        {
            using (var c = factory.Create())
            {
                await c.OpenAsync(t).ConfigureAwait(false);
                using (var q = new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM public.canaletas_plegados_fotos WHERE lower(regexp_replace(replace(object_path, chr(92), '/'), '[. ]+(/|$)', chr(92) || '1', 'g'))=lower(regexp_replace(replace(@path, chr(92), '/'), '[. ]+(/|$)', chr(92) || '1', 'g')))", c))
                {
                    q.Parameters.AddWithValue("path", path);
                    return (bool)await q.ExecuteScalarAsync(t).ConfigureAwait(false);
                }
            }
        }
    }

    public sealed class NpgsqlCanaletas : ICanaletasEscritura
    {
        private readonly NpgsqlUnitOfWork uow;
        public NpgsqlCanaletas(NpgsqlUnitOfWork uow)
        {
            this.uow = uow;
        }

        private NpgsqlCommand Cmd(string sql) => new NpgsqlCommand(sql, uow.Connection, uow.Transaction);

        public async Task<CanaletaPlegado> BloquearAsync(int id, CancellationToken t)
        {
            using (var q = Cmd("SELECT id,id_obra,descripcion,precio,pagado FROM public.canaletas_plegados WHERE id=@id FOR UPDATE"))
            {
                q.Parameters.AddWithValue("id", id);
                using (var r = await q.ExecuteReaderAsync(t).ConfigureAwait(false))
                {
                    if (!await r.ReadAsync(t).ConfigureAwait(false)) throw new InvalidOperationException("La canaleta no existe.");
                    return new CanaletaPlegado(r.GetInt32(0), r.GetInt32(1), r.GetString(2), r.GetDecimal(3), r.GetBoolean(4));
                }
            }
        }

        public async Task<int> GuardarAsync(CanaletaPlegado x, CancellationToken t)
        {
            using (var q = Cmd(x.Id == 0 ? "INSERT INTO public.canaletas_plegados(id_obra,descripcion,precio,pagado) VALUES (@obra,@descripcion,@precio,@pagado) RETURNING id" : "UPDATE public.canaletas_plegados SET id_obra=@obra,descripcion=@descripcion,precio=@precio,pagado=@pagado WHERE id=@id RETURNING id"))
            {
                q.Parameters.AddWithValue("obra", x.ObraId);
                q.Parameters.AddWithValue("descripcion", x.Descripcion);
                q.Parameters.AddWithValue("precio", x.Precio);
                q.Parameters.AddWithValue("pagado", x.Pagado);
                if (x.Id != 0) q.Parameters.AddWithValue("id", x.Id);
                return Convert.ToInt32(await q.ExecuteScalarAsync(t).ConfigureAwait(false));
            }
        }

        public async Task<int> MaximoOrdenAsync(int id, CancellationToken t)
        {
            using (var q = Cmd("SELECT COALESCE(MAX(orden),0) FROM public.canaletas_plegados_fotos WHERE canaleta_plegado_id=@id"))
            {
                q.Parameters.AddWithValue("id", id);
                return Convert.ToInt32(await q.ExecuteScalarAsync(t).ConfigureAwait(false));
            }
        }

        public async Task<int> AdjuntarAsync(int id, string path, string nombre, string mime, long bytes, int orden, CancellationToken t)
        {
            using (var q = Cmd("INSERT INTO public.canaletas_plegados_fotos(canaleta_plegado_id,bucket_id,object_path,nombre_archivo,content_type,tamanio_bytes,orden) VALUES (@id,'canaletas-fotos',@path,@nombre,@mime,@bytes,@orden) RETURNING id"))
            {
                q.Parameters.AddWithValue("id", id);
                q.Parameters.AddWithValue("path", path);
                q.Parameters.AddWithValue("nombre", nombre);
                q.Parameters.AddWithValue("mime", mime);
                q.Parameters.AddWithValue("bytes", bytes);
                q.Parameters.AddWithValue("orden", orden);
                return Convert.ToInt32(await q.ExecuteScalarAsync(t).ConfigureAwait(false));
            }
        }

        public async Task<FotoCanaletaLectura> BloquearFotoAsync(int id, CancellationToken t)
        {
            using (var q = Cmd(NpgsqlCanaletasConsulta.FotosSql + "WHERE id=@id FOR UPDATE"))
            {
                q.Parameters.AddWithValue("id", id);
                using (var r = await q.ExecuteReaderAsync(t).ConfigureAwait(false))
                {
                    if (!await r.ReadAsync(t).ConfigureAwait(false)) throw new InvalidOperationException("La foto no existe.");
                    return NpgsqlCanaletasConsulta.Foto(r);
                }
            }
        }

        public async Task<IReadOnlyList<FotoCanaletaLectura>> FotosAsync(int id, CancellationToken t)
        {
            var lista = new List<FotoCanaletaLectura>();
            using (var q = Cmd(NpgsqlCanaletasConsulta.FotosSql + "WHERE canaleta_plegado_id=@id ORDER BY orden,id FOR UPDATE"))
            {
                q.Parameters.AddWithValue("id", id);
                using (var r = await q.ExecuteReaderAsync(t).ConfigureAwait(false)) while (await r.ReadAsync(t).ConfigureAwait(false)) lista.Add(NpgsqlCanaletasConsulta.Foto(r));
            }
            return lista;
        }

        public async Task EliminarFotoAsync(int id, CancellationToken t)
        {
            using (var q = Cmd("DELETE FROM public.canaletas_plegados_fotos WHERE id=@id"))
            {
                q.Parameters.AddWithValue("id", id);
                await q.ExecuteNonQueryAsync(t).ConfigureAwait(false);
            }
        }

        public async Task EliminarAsync(int id, CancellationToken t)
        {
            using (var q = Cmd("DELETE FROM public.canaletas_plegados WHERE id=@id"))
            {
                q.Parameters.AddWithValue("id", id);
                await q.ExecuteNonQueryAsync(t).ConfigureAwait(false);
            }
        }
    }
}
