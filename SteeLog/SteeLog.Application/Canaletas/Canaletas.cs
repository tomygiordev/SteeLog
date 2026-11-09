using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.Core.Application.Common;
using SteeLog.Core.Application.Identidad;
using SteeLog.Domain.Canaletas;
namespace SteeLog.Core.Application.Canaletas
{
    public sealed class CanaletaLectura
    {
        public int Id
        {
            get;
            set;
        }

        public int ObraId
        {
            get;
            set;
        }

        public int CantidadFotos
        {
            get;
            set;
        }

        public string NombreObra
        {
            get;
            set;
        }

        public string Descripcion
        {
            get;
            set;
        }

        public decimal Precio
        {
            get;
            set;
        }

        public bool Pagado
        {
            get;
            set;
        }

        public DateTime FechaCreacion
        {
            get;
            set;
        }
    }

    public sealed class FotoCanaletaLectura
    {
        public int Id
        {
            get;
            set;
        }

        public int CanaletaId
        {
            get;
            set;
        }

        public int Orden
        {
            get;
            set;
        }

        public string ObjectPath
        {
            get;
            set;
        }

        public string NombreArchivo
        {
            get;
            set;
        }

        public string ContentType
        {
            get;
            set;
        }

        public long TamanioBytes
        {
            get;
            set;
        }

        public DateTime FechaCreacion
        {
            get;
            set;
        }
    }

    public interface ICanaletasConsulta
    {
        Task<IReadOnlyList<CanaletaLectura>> ListarAsync(int? obraId, CancellationToken t);
        Task<CanaletaLectura> ObtenerAsync(int id, CancellationToken t);
        Task<IReadOnlyList<FotoCanaletaLectura>> FotosAsync(int id, CancellationToken t);
        Task<bool> ReferenciadaAsync(string path, CancellationToken t);
        Task<FotoCanaletaLectura> FotoPorPathAsync(string path, CancellationToken t);
    }

    public interface ICanaletasEscritura
    {
        Task<CanaletaPlegado> BloquearAsync(int id, CancellationToken t);
        Task<int> GuardarAsync(CanaletaPlegado canaleta, CancellationToken t);
        Task<int> MaximoOrdenAsync(int id, CancellationToken t);
        Task<int> AdjuntarAsync(int id, string path, string nombre, string mime, long bytes, int orden, CancellationToken t);
        Task<FotoCanaletaLectura> BloquearFotoAsync(int id, CancellationToken t);
        Task<IReadOnlyList<FotoCanaletaLectura>> FotosAsync(int id, CancellationToken t);
        Task EliminarFotoAsync(int id, CancellationToken t);
        Task EliminarAsync(int id, CancellationToken t);
    }

    public interface IArchivosCanaleta
    {
        Task<string> PrepararAsync(int id, byte[] bytes, string extension, CancellationToken t);
        Task<byte[]> LeerAsync(string path, CancellationToken t);
        Task PendienteAsync(string path, CancellationToken t);
        Task CompletarAsync(string path, bool eliminar, CancellationToken t);
        Task<IReadOnlyList<string>> PendientesAsync(CancellationToken t);
        Task<bool> TomarPendienteAsync(string path, CancellationToken t);
        Task LiberarAsync(string path);
    }

    public sealed class ConsultarCanaletas
    {
        private readonly Session session;
        private readonly ICanaletasConsulta consulta;
        private readonly IArchivosCanaleta archivos;
        public ConsultarCanaletas(Session session, ICanaletasConsulta consulta, IArchivosCanaleta archivos)
        {
            this.session = session;
            this.consulta = consulta;
            this.archivos = archivos;
        }

        public Task<IReadOnlyList<CanaletaLectura>> ListarAsync(int? obraId, CancellationToken t = default)
        {
            session.Require(Role.Galpon);
            return consulta.ListarAsync(obraId, t);
        }

        public Task<CanaletaLectura> ObtenerAsync(int id, CancellationToken t = default)
        {
            session.Require(Role.Galpon);
            return consulta.ObtenerAsync(id, t);
        }

        public Task<IReadOnlyList<FotoCanaletaLectura>> FotosAsync(int id, CancellationToken t = default)
        {
            session.Require(Role.Galpon);
            return consulta.FotosAsync(id, t);
        }

        public async Task<byte[]> LeerFotoAsync(string path, CancellationToken t = default)
        {
            session.Require(Role.Galpon);
            if (!await consulta.ReferenciadaAsync(path, t).ConfigureAwait(false)) throw new InvalidOperationException("La foto no está registrada.");
            return await archivos.LeerAsync(path, t).ConfigureAwait(false);
        }
    }

    public sealed class CanaletaResultadoInciertoException : Exception
    {
        public string ObjectPath
        {
            get;
        }

        public CanaletaResultadoInciertoException(string path, Exception inner) : base("No se pudo confirmar el resultado de la operación; verifique antes de reintentar.", inner)
        {
            ObjectPath = path;
        }
    }

    public sealed class CanaletaLimpiezaPendienteException : Exception
    {
        public string ObjectPath
        {
            get;
        }

        public bool OperacionConfirmada => true;
        public int? ResultadoId
        {
            get;
        }

        public CanaletaLimpiezaPendienteException(string path, int? resultadoId, Exception inner) : base("Operación confirmada; quedó pendiente la limpieza de archivos.", inner)
        {
            ObjectPath = path;
            ResultadoId = resultadoId;
        }
    }

    public sealed class OperacionesCanaleta
    {
        private readonly Session session;
        private readonly IUnitOfWork uow;
        private readonly ICanaletasEscritura repo;
        private readonly ICanaletasConsulta consulta;
        private readonly IArchivosCanaleta archivos;
        public OperacionesCanaleta(Session session, IUnitOfWork uow, ICanaletasEscritura repo, ICanaletasConsulta consulta, IArchivosCanaleta archivos)
        {
            this.session = session;
            this.uow = uow;
            this.repo = repo;
            this.consulta = consulta;
            this.archivos = archivos;
        }

        private async Task<T> Transaccion<T>(Func<Task<T>> accion, CancellationToken t, Func<string> path = null)
        {
            await uow.BeginAsync(t).ConfigureAwait(false);
            bool commit = false;
            try
            {
                var r = await accion().ConfigureAwait(false);
                t.ThrowIfCancellationRequested();
                commit = true;
                await uow.CommitAsync(t).ConfigureAwait(false);
                return r;
            }
            catch (Exception ex)
            {
                try
                {
                    await uow.RollbackAsync().ConfigureAwait(false);
                }
                catch
                {
                }
                if (commit) throw new CanaletaResultadoInciertoException(path?.Invoke(), ex);
                throw;
            }
        }

        public Task<int> GuardarAsync(int? id, int obraId, string descripcion, decimal precio, bool pagado, CancellationToken t = default)
        {
            session.Require(Role.Galpon);
            var validado = new CanaletaPlegado(id ?? 0, obraId, descripcion, precio, pagado);
            return Transaccion(async () =>
           {
               if (id.HasValue)
               {
                   var actual = await repo.BloquearAsync(id.Value, t).ConfigureAwait(false);
                   actual.Modificar(obraId, descripcion, precio, pagado);
                   validado = actual;
               }
               return await repo.GuardarAsync(validado, t).ConfigureAwait(false);
           }
           , t);
        }

        private static uint BigEndian(byte[] b, int i) => ((uint)b[i] << 24) | ((uint)b[i + 1] << 16) | ((uint)b[i + 2] << 8) | b[i + 3];

        private static uint LittleEndian(byte[] b, int i) => b[i] | ((uint)b[i + 1] << 8) | ((uint)b[i + 2] << 16) | ((uint)b[i + 3] << 24);

        public const string FiltroDialogoFotos = "Imágenes (*.jpg;*.jpeg;*.png;*.webp)|*.jpg;*.jpeg;*.png;*.webp";

        public static string ValidarTipoFoto(byte[] b)
        {
            if (b == null || b.Length == 0 || b.Length > 8388608)
                throw new ArgumentException("Tamaño de foto inválido.");
            if (b.Length >= 24 && b[0] == 137 && b[1] == 80 && b[2] == 78 && b[3] == 71 && b[4] == 13 && b[5] == 10 && b[6] == 26 && b[7] == 10)
            {
                int p = 8;
                bool header = false, data = false, end = false;
                while (p <= b.Length - 12)
                {
                    uint n = BigEndian(b, p);
                    if (n > b.Length - p - 12) break;
                    string chunk = System.Text.Encoding.ASCII.GetString(b, p + 4, 4);
                    if (p == 8) header = chunk == "IHDR" && n == 13 && BigEndian(b, p + 8) > 0 && BigEndian(b, p + 12) > 0;
                    if (chunk == "IDAT" && n > 0) data = true;
                    p += (int)n + 12;
                    if (chunk == "IEND" && n == 0)
                    {
                        end = p == b.Length;
                        break;
                    }
                }
                if (header && data && end) return "image/png";
            }
            if (b.Length >= 20 && b[0] == 82 && b[1] == 73 && b[2] == 70 && b[3] == 70 && b[8] == 87 && b[9] == 69 && b[10] == 66 && b[11] == 80 && LittleEndian(b, 4) == b.Length - 8)
            {
                int p = 12;
                bool image = false;
                while (p <= b.Length - 8)
                {
                    var n = LittleEndian(b, p + 4);
                    if (n > b.Length - p - 8) break;
                    string chunk = System.Text.Encoding.ASCII.GetString(b, p, 4);
                    if ((chunk == "VP8 " && n >= 10) || (chunk == "VP8L" && n >= 5) || (chunk == "ANMF" && n >= 16)) image = true;
                    long next = (long)p + 8 + n + (n & 1);
                    if (next > b.Length) break;
                    p = (int)next;
                }
                if (image && p == b.Length) return "image/webp";
            }
            if (b.Length >= 16 && b[0] == 0xff && b[1] == 0xd8 && b[b.Length - 2] == 0xff && b[b.Length - 1] == 0xd9)
            {
                int p = 2;
                bool frame = false, scan = false;
                while (p < b.Length - 2 && b[p++] == 0xff)
                {
                    while (p < b.Length && b[p] == 0xff) p++;
                    if (p >= b.Length) break;
                    int marker = b[p++];
                    if (marker == 0xda)
                    {
                        scan = p + 2 <= b.Length && ((b[p] << 8) | b[p + 1]) >= 2 && p + ((b[p] << 8) | b[p + 1]) < b.Length - 2;
                        break;
                    }
                    if (marker == 0xd8 || marker == 0xd9 || marker == 0 || p + 2 > b.Length) break;
                    int len = (b[p] << 8) | b[p + 1];
                    if (len < 2 || p + len > b.Length - 2) break;
                    if (marker >= 0xc0 && marker <= 0xcf && marker != 0xc4 && marker != 0xc8 && marker != 0xcc && len >= 8 && (b[p + 3] != 0 || b[p + 4] != 0) && (b[p + 5] != 0 || b[p + 6] != 0)) frame = true;
                    p += len;
                }
                if (frame && scan) return "image/jpeg";
            }
            throw new ArgumentException("Foto inválida o incompleta; use JPEG, PNG o WebP.");
        }

        private async Task Conciliar(string path)
        {
            try
            {
                var usado = await consulta.ReferenciadaAsync(path, CancellationToken.None).ConfigureAwait(false);
                await archivos.CompletarAsync(path, !usado, CancellationToken.None).ConfigureAwait(false);
            }
            finally
            {
                await archivos.LiberarAsync(path).ConfigureAwait(false);
            }
        }

        public async Task<int> AdjuntarAsync(int id, string nombreArchivo, byte[] bytes, CancellationToken t = default)
        {
            session.Require(Role.Galpon);
            var mime = ValidarTipoFoto(bytes);
            if (string.IsNullOrWhiteSpace(nombreArchivo)) throw new ArgumentException("Falta el nombre de archivo.");
            var path = await archivos.PrepararAsync(id, bytes, mime == "image/jpeg" ? ".jpg" : mime == "image/png" ? ".png" : ".webp", t).ConfigureAwait(false);
            try
            {
                var foto = await Transaccion(async () =>
               {
                   var canaleta = await repo.BloquearAsync(id, t).ConfigureAwait(false);
                   var orden = canaleta.SiguienteOrden(await repo.MaximoOrdenAsync(id, t).ConfigureAwait(false));
                   var metadata = new FotoCanaleta(path, nombreArchivo, mime, bytes.LongLength, orden);
                   return await repo.AdjuntarAsync(id, metadata.ObjectPath, metadata.NombreArchivo, metadata.ContentType, metadata.TamanioBytes, metadata.Orden, t).ConfigureAwait(false);
               }
               , t, () => path).ConfigureAwait(false);
                try
                {
                    await Conciliar(path).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    throw new CanaletaLimpiezaPendienteException(path, foto, ex);
                }
                return foto;
            }
            catch (CanaletaLimpiezaPendienteException)
            {
                throw;
            }
            catch (CanaletaResultadoInciertoException)
            {
                try
                {
                    var persistida = await consulta.FotoPorPathAsync(path, CancellationToken.None).ConfigureAwait(false);
                    if (persistida != null)
                    {
                        try
                        {
                            await archivos.CompletarAsync(path, false, CancellationToken.None).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            throw new CanaletaLimpiezaPendienteException(path, persistida.Id, ex);
                        }
                        return persistida.Id;
                    }
                    await archivos.CompletarAsync(path, true, CancellationToken.None).ConfigureAwait(false);
                }
                catch (CanaletaLimpiezaPendienteException)
                {
                    throw;
                }
                catch
                {
                    await archivos.LiberarAsync(path).ConfigureAwait(false);
                }
                throw;
            }
            catch
            {
                try
                {
                    await archivos.CompletarAsync(path, true, CancellationToken.None).ConfigureAwait(false);
                }
                catch
                {
                }
                finally
                {
                    await archivos.LiberarAsync(path).ConfigureAwait(false);
                }
                throw;
            }
        }

        private async Task VerificarIncierto(string path)
        {
            try
            {
                if (!await consulta.ReferenciadaAsync(path, CancellationToken.None).ConfigureAwait(false))
                    await archivos.CompletarAsync(path, true, CancellationToken.None).ConfigureAwait(false);
            }
            catch
            {
                /* No se elimina sin una comprobación exitosa. */
            }
            finally
            {
                await archivos.LiberarAsync(path).ConfigureAwait(false);
            }
        }

        public async Task<bool> EliminarFotoAsync(int fotoId, CancellationToken t = default)
        {
            session.Require(Role.Galpon);
            string path = null;
            bool tomado = false;
            try
            {
                await Transaccion(async () =>
               {
                   var f = await repo.BloquearFotoAsync(fotoId, t).ConfigureAwait(false);
                   path = f.ObjectPath;
                   await archivos.PendienteAsync(path, t).ConfigureAwait(false);
                   tomado = true;
                   await repo.EliminarFotoAsync(fotoId, t).ConfigureAwait(false);
                   return true;
               }
               , t, () => path).ConfigureAwait(false);
            }
            catch (CanaletaResultadoInciertoException)
            {
                if (tomado) await VerificarIncierto(path).ConfigureAwait(false);
                throw;
            }
            catch
            {
                if (tomado) await archivos.LiberarAsync(path).ConfigureAwait(false);
                throw;
            }
            try
            {
                await Conciliar(path).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new CanaletaLimpiezaPendienteException(path, fotoId, ex);
            }
            return true;
        }

        public async Task<bool> EliminarAsync(int id, CancellationToken t = default)
        {
            session.Require(Role.Galpon);
            var paths = new List<string>();
            try
            {
                await Transaccion(async () =>
               {
                   await repo.BloquearAsync(id, t).ConfigureAwait(false);
                   var fotos = await repo.FotosAsync(id, t).ConfigureAwait(false);
                   foreach (var f in fotos)
                   {
                       if (paths.Contains(f.ObjectPath)) continue;
                       await archivos.PendienteAsync(f.ObjectPath, t).ConfigureAwait(false);
                       paths.Add(f.ObjectPath);
                   }
                   await repo.EliminarAsync(id, t).ConfigureAwait(false);
                   return true;
               }
               , t, () => paths.Count > 0 ? paths[0] : null).ConfigureAwait(false);
            }
            catch (CanaletaResultadoInciertoException)
            {
                foreach (var p in paths) await VerificarIncierto(p).ConfigureAwait(false);
                throw;
            }
            catch
            {
                foreach (var p in paths) await archivos.LiberarAsync(p).ConfigureAwait(false);
                throw;
            }
            Exception error = null;
            string fallido = null;
            foreach (var p in paths)
            {
                try
                {
                    await Conciliar(p).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    if (error == null)
                    {
                        error = ex;
                        fallido = p;
                    }
                }
            }
            if (error != null) throw new CanaletaLimpiezaPendienteException(fallido, id, error);
            return true;
        }

        public async Task ConciliarPendientesAsync(CancellationToken t = default)
        {
            session.Require(Role.Galpon);
            foreach (var path in await archivos.PendientesAsync(t).ConfigureAwait(false))
            {
                t.ThrowIfCancellationRequested();
                if (await archivos.TomarPendienteAsync(path, t).ConfigureAwait(false)) await Conciliar(path).ConfigureAwait(false);
            }
        }
    }
}
