using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using SteeLog.Core.Application.Activos;
using SteeLog.Infrastructure.Persistence;

namespace SteeLog.Infrastructure.Activos
{
    public sealed class NpgsqlInsumosConsulta : IInsumosConsulta
    {
        private readonly PostgresConnectionFactory factory;
        public NpgsqlInsumosConsulta(PostgresConnectionFactory factory) { this.factory = factory ?? throw new ArgumentNullException(nameof(factory)); }
        public async Task<IReadOnlyList<InsumoLectura>> InsumosAsync(CancellationToken token = default)
        {
            var lista = new List<InsumoLectura>();
            using (var con = factory.Create()) { await con.OpenAsync(token).ConfigureAwait(false);
                using (var cmd = new NpgsqlCommand(@"SELECT i.id_insumo, i.nombre, i.id_obra, COALESCE(o.nombre, 'Galpón'), i.consumido, i.observaciones FROM public.insumos i LEFT JOIN public.obras o ON i.id_obra = o.id ORDER BY i.nombre ASC", con))
                using (var r = await cmd.ExecuteReaderAsync(token).ConfigureAwait(false))
                    while (await r.ReadAsync(token).ConfigureAwait(false)) lista.Add(new InsumoLectura { Id = r.GetInt32(0), Nombre = r.GetString(1), ObraId = r.IsDBNull(2) ? (int?)null : r.GetInt32(2), Obra = r.GetString(3), Consumido = r.GetBoolean(4), Observaciones = r.IsDBNull(5) ? "" : r.GetString(5) });
            } return lista;
        }
        public async Task<IReadOnlyList<ObraInsumoLectura>> ObrasAsync(CancellationToken token = default)
        {
            var lista = new List<ObraInsumoLectura>();
            using (var con = factory.Create()) { await con.OpenAsync(token).ConfigureAwait(false);
                using (var cmd = new NpgsqlCommand("SELECT id,nombre FROM public.obras", con))
                using (var r = await cmd.ExecuteReaderAsync(token).ConfigureAwait(false))
                    while (await r.ReadAsync(token).ConfigureAwait(false)) lista.Add(new ObraInsumoLectura { Id = r.GetInt32(0), Nombre = r.GetString(1) });
            } return lista;
        }
        public async Task<IReadOnlyList<HistorialInsumoLectura>> HistorialAsync(CancellationToken token = default)
        {
            var lista = new List<HistorialInsumoLectura>();
            using (var con = factory.Create()) { await con.OpenAsync(token).ConfigureAwait(false);
                using (var cmd = new NpgsqlCommand(@"SELECT im.id, COALESCE(NULLIF(im.nombre_insumo,''), i.nombre), im.obra_origen, im.obra_destino, im.fecha, im.id_insumo FROM public.insumo_movimientos im INNER JOIN public.insumos i ON im.id_insumo = i.id_insumo ORDER BY im.fecha DESC", con))
                using (var r = await cmd.ExecuteReaderAsync(token).ConfigureAwait(false))
                    while (await r.ReadAsync(token).ConfigureAwait(false)) lista.Add(new HistorialInsumoLectura { Id = r.GetInt32(0), Insumo = r.GetString(1), Origen = r.GetString(2), Destino = r.GetString(3), Fecha = r.GetDateTime(4), InsumoId = r.GetInt32(5) });
            } return lista;
        }
    }
}
