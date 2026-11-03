using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using SteeLog.Core.Application.Conteos;
using SteeLog.Domain.Conteos;
using SteeLog.Infrastructure.Persistence;

namespace SteeLog.Infrastructure.Conteos
{
    public sealed class NpgsqlConteos : IConteosEscritura
    {
        private readonly NpgsqlUnitOfWork uow;
        public NpgsqlConteos(NpgsqlUnitOfWork uow) { this.uow = uow; }
        private NpgsqlCommand Cmd(string sql) => new NpgsqlCommand(sql, uow.Connection, uow.Transaction);
        public async Task<int> CrearAsync(string propietario, CancellationToken token)
        {
            using (var cmd = Cmd("INSERT INTO public.conteos_inventario(estado,usuario_inicio) VALUES('abierto',@usuario) RETURNING id_conteo")) {
                cmd.Parameters.AddWithValue("usuario", propietario); return Convert.ToInt32(await cmd.ExecuteScalarAsync(token).ConfigureAwait(false));
            }
        }
        public async Task<Conteo> BloquearAsync(int id, CancellationToken token)
        {
            if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
            string propietario, estado;
            using (var cmd = Cmd("SELECT usuario_inicio,estado FROM public.conteos_inventario WHERE id_conteo=@id FOR UPDATE")) {
                cmd.Parameters.AddWithValue("id", id);
                using (var r = await cmd.ExecuteReaderAsync(token).ConfigureAwait(false)) {
                    if (!await r.ReadAsync(token).ConfigureAwait(false)) throw new InvalidOperationException("El conteo indicado no existe.");
                    propietario = r.IsDBNull(0) ? null : r.GetString(0); estado = r.GetString(1);
                }
            }
            EstadoConteo e;
            switch (estado) { case "abierto": e = EstadoConteo.Abierto; break; case "finalizado": e = EstadoConteo.Finalizado; break; case "cancelado": e = EstadoConteo.Cancelado; break; default: throw new InvalidOperationException("Estado de conteo desconocido."); }
            var detalles = new List<DetalleConteo>();
            using (var cmd = Cmd("SELECT id_detalle,id_material,cantidad_contada FROM public.conteos_inventario_detalle WHERE id_conteo=@id ORDER BY id_detalle")) {
                cmd.Parameters.AddWithValue("id", id);
                using (var r = await cmd.ExecuteReaderAsync(token).ConfigureAwait(false)) while (await r.ReadAsync(token).ConfigureAwait(false)) detalles.Add(new DetalleConteo(r.GetInt32(0), r.GetInt32(1), r.GetDecimal(2)));
            }
            return new Conteo(id, propietario, e, detalles);
        }
        public async Task GuardarCantidadAsync(int id, int materialId, decimal cantidad, CancellationToken token)
        {
            using (var cmd = Cmd(@"INSERT INTO public.conteos_inventario_detalle(id_conteo,id_material,material_nombre_snapshot,material_categoria_snapshot,cantidad_contada)
                SELECT @id,m.id,m.nombre,c.nombre,@cantidad FROM public.materiales m LEFT JOIN public.categorias c ON c.id_categoria=m.id_categoria WHERE m.id=@material
                ON CONFLICT(id_conteo,id_material) DO UPDATE SET cantidad_contada=EXCLUDED.cantidad_contada,updated_at=NOW()")) {
                cmd.Parameters.AddWithValue("id", id); cmd.Parameters.AddWithValue("material", materialId); cmd.Parameters.AddWithValue("cantidad", cantidad);
                if (await cmd.ExecuteNonQueryAsync(token).ConfigureAwait(false) != 1) throw new InvalidOperationException("No se encontró el material seleccionado.");
            }
        }
        public async Task QuitarDetalleAsync(int id, int detalleId, CancellationToken token)
        {
            using (var cmd = Cmd("DELETE FROM public.conteos_inventario_detalle WHERE id_conteo=@id AND id_detalle=@detalle")) {
                cmd.Parameters.AddWithValue("id", id); cmd.Parameters.AddWithValue("detalle", detalleId);
                if (await cmd.ExecuteNonQueryAsync(token).ConfigureAwait(false) != 1) throw new InvalidOperationException("No existe el detalle.");
            }
        }
        public async Task GuardarCierreAsync(int id, string usuario, string observaciones, IDictionary<int, decimal> stock, IReadOnlyDictionary<int, decimal> diferencias, CancellationToken token)
        {
            foreach (var par in diferencias) using (var cmd = Cmd("UPDATE public.conteos_inventario_detalle SET stock_sistema=@stock,diferencia=@diferencia,updated_at=NOW() WHERE id_conteo=@id AND id_material=@material")) {
                cmd.Parameters.AddWithValue("id", id); cmd.Parameters.AddWithValue("material", par.Key); cmd.Parameters.AddWithValue("stock", stock[par.Key]); cmd.Parameters.AddWithValue("diferencia", par.Value);
                if (await cmd.ExecuteNonQueryAsync(token).ConfigureAwait(false) != 1) throw new InvalidOperationException("No existe el detalle.");
            }
            using (var cmd = Cmd("UPDATE public.conteos_inventario SET estado='finalizado',fecha_fin=NOW(),usuario_fin=@usuario,observaciones=@observaciones,updated_at=NOW() WHERE id_conteo=@id")) {
                cmd.Parameters.AddWithValue("id", id); cmd.Parameters.AddWithValue("usuario", usuario); cmd.Parameters.AddWithValue("observaciones", string.IsNullOrWhiteSpace(observaciones) ? (object)DBNull.Value : observaciones.Trim()); await cmd.ExecuteNonQueryAsync(token).ConfigureAwait(false);
            }
        }
        public async Task GuardarCancelacionAsync(int id, CancellationToken token)
        {
            using (var cmd = Cmd("UPDATE public.conteos_inventario SET estado='cancelado',fecha_fin=NOW(),updated_at=NOW() WHERE id_conteo=@id")) { cmd.Parameters.AddWithValue("id", id); await cmd.ExecuteNonQueryAsync(token).ConfigureAwait(false); }
        }
        public async Task EliminarAsync(int id, CancellationToken token)
        {
            using (var cmd = Cmd("DELETE FROM public.conteos_inventario WHERE id_conteo=@id")) { cmd.Parameters.AddWithValue("id", id); await cmd.ExecuteNonQueryAsync(token).ConfigureAwait(false); }
        }
    }
}
