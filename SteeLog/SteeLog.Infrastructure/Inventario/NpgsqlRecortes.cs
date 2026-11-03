using System;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using NpgsqlTypes;
using SteeLog.Core.Application.Inventario;
using SteeLog.Domain.Inventario;
using SteeLog.Infrastructure.Persistence;

namespace SteeLog.Infrastructure.Inventario
{
    public sealed class NpgsqlRecortes : IRecortesEscritura
    {
        private readonly NpgsqlUnitOfWork uow;
        public NpgsqlRecortes(NpgsqlUnitOfWork uow) { this.uow = uow; }
        private NpgsqlCommand Crear(string sql, Recorte recorte)
        {
            var cmd = new NpgsqlCommand(sql, uow.Connection, uow.Transaction);
            cmd.Parameters.AddWithValue("material", recorte.MaterialId);
            cmd.Parameters.AddWithValue("ancho", recorte.Ancho);
            cmd.Parameters.AddWithValue("altura", recorte.Altura);
            cmd.Parameters.AddWithValue("medida", recorte.Medida);
            cmd.Parameters.Add("estado", NpgsqlDbType.Text).Value = (object)recorte.Estado ?? DBNull.Value;
            return cmd;
        }
        public async Task<Recorte> BuscarCoincidenteBloqueadoAsync(Recorte clave, CancellationToken token = default)
        {
            using (var cmd = Crear(@"SELECT id, cantidad FROM public.recortes WHERE id_material=@material AND ancho=@ancho AND altura=@altura AND medida=@medida AND estado IS NOT DISTINCT FROM @estado ORDER BY id LIMIT 1 FOR UPDATE", clave))
            using (var reader = await cmd.ExecuteReaderAsync(token).ConfigureAwait(false))
                return await reader.ReadAsync(token).ConfigureAwait(false) ? new Recorte(reader.GetInt32(0), clave.MaterialId, clave.Ancho, clave.Altura, clave.Medida, reader.GetDecimal(1), clave.Estado) : null;
        }
        public async Task<int> InsertarAsync(Recorte recorte, CancellationToken token = default)
        {
            using (var cmd = Crear(@"INSERT INTO public.recortes(id_material, ancho, altura, medida, cantidad, estado) VALUES(@material,@ancho,@altura,@medida,@cantidad,@estado) RETURNING id", recorte))
            {
                cmd.Parameters.AddWithValue("cantidad", recorte.Cantidad);
                return Convert.ToInt32(await cmd.ExecuteScalarAsync(token).ConfigureAwait(false));
            }
        }
        public async Task ActualizarCantidadAsync(int id, decimal cantidad, CancellationToken token = default)
        {
            using (var cmd = new NpgsqlCommand("UPDATE public.recortes SET cantidad=@cantidad WHERE id=@id", uow.Connection, uow.Transaction))
            {
                cmd.Parameters.AddWithValue("id", id); cmd.Parameters.AddWithValue("cantidad", cantidad);
                if (await cmd.ExecuteNonQueryAsync(token).ConfigureAwait(false) != 1) throw new InvalidOperationException("No se encontró el recorte.");
            }
        }        public async Task EliminarAsync(int id, int materialId, CancellationToken token = default)
        {
            using (var cmd = new NpgsqlCommand("SELECT id FROM public.recortes WHERE id=@id AND id_material=@material FOR UPDATE", uow.Connection, uow.Transaction))
            {
                cmd.Parameters.AddWithValue("id", id); cmd.Parameters.AddWithValue("material", materialId);
                if (await cmd.ExecuteScalarAsync(token).ConfigureAwait(false) == null) throw new InvalidOperationException("No se encontró el recorte para el material indicado.");
            }
            using (var cmd = new NpgsqlCommand("DELETE FROM public.recortes r WHERE r.id=@id AND NOT EXISTS (SELECT 1 FROM public.movimientomaterial mm WHERE mm.id_recorte=r.id)", uow.Connection, uow.Transaction))
            {
                cmd.Parameters.AddWithValue("id", id);
                if (await cmd.ExecuteNonQueryAsync(token).ConfigureAwait(false) != 1) throw new InvalidOperationException("El recorte está referenciado por un movimiento.");
            }
        }
    }
}
