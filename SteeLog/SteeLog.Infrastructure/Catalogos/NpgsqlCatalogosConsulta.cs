using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using SteeLog.Core.Application.Catalogos;
using SteeLog.Infrastructure.Persistence;
namespace SteeLog.Infrastructure.Catalogos
{
    public sealed class NpgsqlCatalogosConsulta : ICatalogosConsulta
    {
        private readonly PostgresConnectionFactory factory;
        public NpgsqlCatalogosConsulta(PostgresConnectionFactory factory) { this.factory = factory ?? throw new ArgumentNullException(nameof(factory)); }
        public async Task<IReadOnlyList<CategoriaCatalogo>> CategoriasAsync(CancellationToken token = default)
        {
            var lista = new List<CategoriaCatalogo>();
            using (var con = factory.Create())
            {
                await con.OpenAsync(token).ConfigureAwait(false);
                using (var cmd = new NpgsqlCommand("SELECT id_categoria, nombre, recortable FROM public.categorias ORDER BY nombre ASC", con))
                using (var r = await cmd.ExecuteReaderAsync(token).ConfigureAwait(false))
                    while (await r.ReadAsync(token).ConfigureAwait(false)) lista.Add(new CategoriaCatalogo { Id = r.GetInt32(0), Nombre = r.IsDBNull(1) ? "" : r.GetString(1), Recortable = r.IsDBNull(2) ? false : r.GetBoolean(2) });
            }
            return lista;
        }
        public async Task<IReadOnlyList<ObraCatalogo>> ObrasAsync(CancellationToken token = default)
        {
            var lista = new List<ObraCatalogo>();
            using (var con = factory.Create())
            {
                await con.OpenAsync(token).ConfigureAwait(false);
                using (var cmd = new NpgsqlCommand("SELECT id, nombre, activa FROM public.obras ORDER BY nombre ASC", con))
                using (var r = await cmd.ExecuteReaderAsync(token).ConfigureAwait(false))
                    while (await r.ReadAsync(token).ConfigureAwait(false)) lista.Add(new ObraCatalogo { Id = r.GetInt32(0), Nombre = r.IsDBNull(1) ? "" : r.GetString(1), Activa = r.IsDBNull(2) ? true : r.GetBoolean(2) });
            }
            return lista;
        }
        public async Task<IReadOnlyList<ProveedorCatalogo>> ProveedoresAsync(CancellationToken token = default)
        {
            var lista = new List<ProveedorCatalogo>();
            using (var con = factory.Create())
            {
                await con.OpenAsync(token).ConfigureAwait(false);
                using (var cmd = new NpgsqlCommand("SELECT id_proveedores, nombre FROM public.proveedores ORDER BY nombre ASC", con))
                using (var r = await cmd.ExecuteReaderAsync(token).ConfigureAwait(false))
                    while (await r.ReadAsync(token).ConfigureAwait(false)) lista.Add(new ProveedorCatalogo { Id = r.GetInt32(0), Nombre = r.IsDBNull(1) ? "" : r.GetString(1) });
            }
            return lista;
        }
        public async Task<IReadOnlyList<FleteCatalogo>> FletesAsync(CancellationToken token = default)
        {
            var lista = new List<FleteCatalogo>();
            using (var con = factory.Create())
            {
                await con.OpenAsync(token).ConfigureAwait(false);
                using (var cmd = new NpgsqlCommand("SELECT id_flete, nombre, activo FROM public.fletes ORDER BY nombre ASC", con))
                using (var r = await cmd.ExecuteReaderAsync(token).ConfigureAwait(false))
                    while (await r.ReadAsync(token).ConfigureAwait(false)) lista.Add(new FleteCatalogo { Id = r.GetInt32(0), Nombre = r.IsDBNull(1) ? "" : r.GetString(1), Activo = r.IsDBNull(2) ? true : r.GetBoolean(2) });
            }
            return lista;
        }
        public async Task<IReadOnlyList<UbicacionCatalogo>> UbicacionesAsync(CancellationToken token = default)
        {
            var lista = new List<UbicacionCatalogo>();
            using (var con = factory.Create())
            {
                await con.OpenAsync(token).ConfigureAwait(false);
                using (var cmd = new NpgsqlCommand("SELECT id_ubicacion, nombre FROM public.ubicacion ORDER BY nombre ASC", con))
                using (var r = await cmd.ExecuteReaderAsync(token).ConfigureAwait(false))
                    while (await r.ReadAsync(token).ConfigureAwait(false)) lista.Add(new UbicacionCatalogo { Id = r.GetInt32(0), Nombre = r.IsDBNull(1) ? "" : r.GetString(1) });
            }
            return lista;
        }
    }
}
