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
        private static async Task ExigirUnaAsync(NpgsqlCommand cmd, CancellationToken token) { if (await cmd.ExecuteNonQueryAsync(token).ConfigureAwait(false) != 1) throw new InvalidOperationException("El registro ya no existe."); }
        public async Task<int> CrearCategoriaAsync(Categoria valor, CancellationToken token = default)
        { using (var cmd=Cmd("INSERT INTO public.categorias(nombre, recortable) VALUES(@nombre, @estado) RETURNING id_categoria")) { cmd.Parameters.AddWithValue("nombre", valor.Nombre); cmd.Parameters.AddWithValue("estado", valor.Recortable); return Convert.ToInt32(await cmd.ExecuteScalarAsync(token).ConfigureAwait(false)); } }
        public async Task EditarCategoriaAsync(Categoria valor, CancellationToken token = default)
        { using (var cmd=Cmd("UPDATE public.categorias SET nombre=@nombre, recortable=@estado WHERE id_categoria=@id")) { cmd.Parameters.AddWithValue("id", valor.Id); cmd.Parameters.AddWithValue("nombre", valor.Nombre); cmd.Parameters.AddWithValue("estado", valor.Recortable); await ExigirUnaAsync(cmd, token).ConfigureAwait(false); } }
        public async Task EliminarCategoriaAsync(int id, CancellationToken token = default)
        { using (var cmd=Cmd("DELETE FROM public.categorias WHERE id_categoria=@id")) { cmd.Parameters.AddWithValue("id", id); await ExigirUnaAsync(cmd, token).ConfigureAwait(false); } }
        public async Task<int> CrearObraAsync(Obra valor, CancellationToken token = default)
        { using (var cmd=Cmd("INSERT INTO public.obras(nombre, activa) VALUES(@nombre, @estado) RETURNING id")) { cmd.Parameters.AddWithValue("nombre", valor.Nombre); cmd.Parameters.AddWithValue("estado", valor.Activa); return Convert.ToInt32(await cmd.ExecuteScalarAsync(token).ConfigureAwait(false)); } }
        public async Task EditarObraAsync(Obra valor, CancellationToken token = default)
        { using (var cmd=Cmd("UPDATE public.obras SET nombre=@nombre, activa=@estado WHERE id=@id")) { cmd.Parameters.AddWithValue("id", valor.Id); cmd.Parameters.AddWithValue("nombre", valor.Nombre); cmd.Parameters.AddWithValue("estado", valor.Activa); await ExigirUnaAsync(cmd, token).ConfigureAwait(false); } }
        public async Task EliminarObraAsync(int id, CancellationToken token = default)
        { using (var cmd=Cmd("DELETE FROM public.obras WHERE id=@id")) { cmd.Parameters.AddWithValue("id", id); await ExigirUnaAsync(cmd, token).ConfigureAwait(false); } }
        public async Task<int> CrearProveedorAsync(Proveedor valor, CancellationToken token = default)
        { using (var cmd=Cmd("INSERT INTO public.proveedores(nombre) VALUES(@nombre) RETURNING id_proveedores")) { cmd.Parameters.AddWithValue("nombre", valor.Nombre); return Convert.ToInt32(await cmd.ExecuteScalarAsync(token).ConfigureAwait(false)); } }
        public async Task EditarProveedorAsync(Proveedor valor, CancellationToken token = default)
        { using (var cmd=Cmd("UPDATE public.proveedores SET nombre=@nombre WHERE id_proveedores=@id")) { cmd.Parameters.AddWithValue("id", valor.Id); cmd.Parameters.AddWithValue("nombre", valor.Nombre); await ExigirUnaAsync(cmd, token).ConfigureAwait(false); } }
        public async Task EliminarProveedorAsync(int id, CancellationToken token = default)
        { using (var cmd=Cmd("DELETE FROM public.proveedores WHERE id_proveedores=@id")) { cmd.Parameters.AddWithValue("id", id); await ExigirUnaAsync(cmd, token).ConfigureAwait(false); } }
        public async Task EditarFleteAsync(Flete valor, CancellationToken token = default)
        { using (var cmd=Cmd("UPDATE public.fletes SET nombre=@nombre, activo=@estado WHERE id_flete=@id")) { cmd.Parameters.AddWithValue("id", valor.Id); cmd.Parameters.AddWithValue("nombre", valor.Nombre); cmd.Parameters.AddWithValue("estado", valor.Activo); await ExigirUnaAsync(cmd, token).ConfigureAwait(false); } }
        public async Task<int> CrearUbicacionAsync(Ubicacion valor, CancellationToken token = default)
        { using (var cmd=Cmd("INSERT INTO public.ubicacion(nombre) VALUES(@nombre) RETURNING id_ubicacion")) { cmd.Parameters.AddWithValue("nombre", valor.Nombre); return Convert.ToInt32(await cmd.ExecuteScalarAsync(token).ConfigureAwait(false)); } }
        public async Task EditarUbicacionAsync(Ubicacion valor, CancellationToken token = default)
        { using (var cmd=Cmd("UPDATE public.ubicacion SET nombre=@nombre WHERE id_ubicacion=@id")) { cmd.Parameters.AddWithValue("id", valor.Id); cmd.Parameters.AddWithValue("nombre", valor.Nombre); await ExigirUnaAsync(cmd, token).ConfigureAwait(false); } }
        public async Task EliminarUbicacionAsync(int id, CancellationToken token = default)
        { using (var cmd=Cmd("DELETE FROM public.ubicacion WHERE id_ubicacion=@id")) { cmd.Parameters.AddWithValue("id", id); await ExigirUnaAsync(cmd, token).ConfigureAwait(false); } }
    }
}
