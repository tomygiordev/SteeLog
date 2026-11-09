using System;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using SteeLog.Core.Application.Catalogos;
using SteeLog.Domain.Catalogos;
using SteeLog.Infrastructure.Persistence;
namespace SteeLog.Infrastructure.Catalogos
{
    public sealed class NpgsqlCatalogos : ICatalogosEscritura
    {
        private readonly NpgsqlUnitOfWork uow;
        public NpgsqlCatalogos(NpgsqlUnitOfWork uow) { this.uow = uow ?? throw new ArgumentNullException(nameof(uow)); }
        private NpgsqlCommand Cmd(string sql) => new NpgsqlCommand(sql, uow.Connection, uow.Transaction) { CommandTimeout = 15 };
        internal static async Task<T> Unico<T>(Func<Task<T>> accion, string entidad)
        {
            try { return await accion().ConfigureAwait(false); }
            catch (PostgresException error) when (error.SqlState == PostgresErrorCodes.UniqueViolation) { throw new CatalogoDuplicadoException(entidad, error); }
        }
        internal static async Task<T> EnUso<T>(Func<Task<T>> accion, string entidad, string referencia)
        {
            try { return await accion().ConfigureAwait(false); }
            catch (PostgresException error) when (error.SqlState == PostgresErrorCodes.ForeignKeyViolation) { throw new CatalogoEnUsoException(entidad, referencia, error); }
        }
        private static async Task ExigirEliminadaAsync(NpgsqlCommand cmd, CancellationToken token, string entidad, string referencia)
        {
            int filas = await EnUso(() => cmd.ExecuteNonQueryAsync(token), entidad, referencia).ConfigureAwait(false);
            if (filas != 1) throw new InvalidOperationException("El registro ya no existe.");
        }
        private static async Task ExigirUnaAsync(NpgsqlCommand cmd, CancellationToken token, string entidad = null)
        {
            int filas = entidad == null ? await cmd.ExecuteNonQueryAsync(token).ConfigureAwait(false) : await Unico(() => cmd.ExecuteNonQueryAsync(token), entidad).ConfigureAwait(false);
            if (filas != 1) throw new InvalidOperationException("El registro ya no existe.");
        }
        public async Task<int> CrearCategoriaAsync(Categoria valor, CancellationToken token = default)
        { using (var cmd=Cmd("INSERT INTO public.categorias(nombre, recortable) VALUES(@nombre, @estado) RETURNING id_categoria")) { cmd.Parameters.AddWithValue("nombre", valor.Nombre); cmd.Parameters.AddWithValue("estado", valor.Recortable); return Convert.ToInt32(await Unico(() => cmd.ExecuteScalarAsync(token), "una categoría").ConfigureAwait(false)); } }
        public async Task EditarCategoriaAsync(Categoria valor, CancellationToken token = default)
        { using (var cmd=Cmd("UPDATE public.categorias SET nombre=@nombre, recortable=@estado WHERE id_categoria=@id")) { cmd.Parameters.AddWithValue("id", valor.Id); cmd.Parameters.AddWithValue("nombre", valor.Nombre); cmd.Parameters.AddWithValue("estado", valor.Recortable); await ExigirUnaAsync(cmd, token, "una categoría").ConfigureAwait(false); } }
        public async Task EliminarCategoriaAsync(int id, CancellationToken token = default)
        { using (var cmd=Cmd("DELETE FROM public.categorias WHERE id_categoria=@id")) { cmd.Parameters.AddWithValue("id", id); await ExigirEliminadaAsync(cmd, token, "la categoría", "materiales").ConfigureAwait(false); } }
        public async Task<int> CrearObraAsync(Obra valor, CancellationToken token = default)
        { using (var cmd=Cmd("INSERT INTO public.obras(nombre, activa) VALUES(@nombre, @estado) RETURNING id")) { cmd.Parameters.AddWithValue("nombre", valor.Nombre); cmd.Parameters.AddWithValue("estado", valor.Activa); return Convert.ToInt32(await Unico(() => cmd.ExecuteScalarAsync(token), "una obra").ConfigureAwait(false)); } }
        public async Task EditarObraAsync(Obra valor, CancellationToken token = default)
        { using (var cmd=Cmd("UPDATE public.obras SET nombre=@nombre, activa=@estado WHERE id=@id")) { cmd.Parameters.AddWithValue("id", valor.Id); cmd.Parameters.AddWithValue("nombre", valor.Nombre); cmd.Parameters.AddWithValue("estado", valor.Activa); await ExigirUnaAsync(cmd, token, "una obra").ConfigureAwait(false); } }
        public async Task EliminarObraAsync(int id, CancellationToken token = default)
        { using (var cmd=Cmd("DELETE FROM public.obras WHERE id=@id")) { cmd.Parameters.AddWithValue("id", id); await ExigirEliminadaAsync(cmd, token, "la obra", "movimientos, canaletas, herramientas o insumos").ConfigureAwait(false); } }
        public async Task<int> CrearProveedorAsync(Proveedor valor, CancellationToken token = default)
        { using (var cmd=Cmd("INSERT INTO public.proveedores(nombre) VALUES(@nombre) RETURNING id_proveedores")) { cmd.Parameters.AddWithValue("nombre", valor.Nombre); return Convert.ToInt32(await Unico(() => cmd.ExecuteScalarAsync(token), "un proveedor").ConfigureAwait(false)); } }
        public async Task EditarProveedorAsync(Proveedor valor, CancellationToken token = default)
        { using (var cmd=Cmd("UPDATE public.proveedores SET nombre=@nombre WHERE id_proveedores=@id")) { cmd.Parameters.AddWithValue("id", valor.Id); cmd.Parameters.AddWithValue("nombre", valor.Nombre); await ExigirUnaAsync(cmd, token, "un proveedor").ConfigureAwait(false); } }
        public async Task EliminarProveedorAsync(int id, CancellationToken token = default)
        { using (var cmd=Cmd("DELETE FROM public.proveedores WHERE id_proveedores=@id")) { cmd.Parameters.AddWithValue("id", id); await ExigirEliminadaAsync(cmd, token, "el proveedor", "órdenes de compra").ConfigureAwait(false); } }
        public async Task EditarFleteAsync(Flete valor, CancellationToken token = default)
        { using (var cmd=Cmd("UPDATE public.fletes SET nombre=@nombre, activo=@estado WHERE id_flete=@id")) { cmd.Parameters.AddWithValue("id", valor.Id); cmd.Parameters.AddWithValue("nombre", valor.Nombre); cmd.Parameters.AddWithValue("estado", valor.Activo); await ExigirUnaAsync(cmd, token, "un flete").ConfigureAwait(false); } }
        public async Task<int> CrearUbicacionAsync(Ubicacion valor, CancellationToken token = default)
        { using (var cmd=Cmd("INSERT INTO public.ubicacion(nombre) VALUES(@nombre) RETURNING id_ubicacion")) { cmd.Parameters.AddWithValue("nombre", valor.Nombre); return Convert.ToInt32(await Unico(() => cmd.ExecuteScalarAsync(token), "una ubicación").ConfigureAwait(false)); } }
        public async Task EditarUbicacionAsync(Ubicacion valor, CancellationToken token = default)
        { using (var cmd=Cmd("UPDATE public.ubicacion SET nombre=@nombre WHERE id_ubicacion=@id")) { cmd.Parameters.AddWithValue("id", valor.Id); cmd.Parameters.AddWithValue("nombre", valor.Nombre); await ExigirUnaAsync(cmd, token, "una ubicación").ConfigureAwait(false); } }
        public async Task EliminarUbicacionAsync(int id, CancellationToken token = default)
        {
            // movimientos.id_origen/id_destino son polimórficos (sin FK): sin este chequeo la ubicación se borraría dejando referencias colgantes.
            using (var uso = Cmd("SELECT EXISTS(SELECT 1 FROM public.movimientos WHERE (tipo_origen='UBICACION' AND id_origen=@id) OR (tipo_destino='UBICACION' AND id_destino=@id))"))
            {
                uso.Parameters.AddWithValue("id", id);
                if ((bool)await uso.ExecuteScalarAsync(token).ConfigureAwait(false)) throw new CatalogoEnUsoException("la ubicación", "movimientos");
            }
            using (var cmd=Cmd("DELETE FROM public.ubicacion WHERE id_ubicacion=@id")) { cmd.Parameters.AddWithValue("id", id); await ExigirEliminadaAsync(cmd, token, "la ubicación", "órdenes de compra").ConfigureAwait(false); }
        }
    }
}
