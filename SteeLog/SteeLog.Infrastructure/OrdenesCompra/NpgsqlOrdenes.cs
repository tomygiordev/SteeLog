using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using SteeLog.Core.Application.OrdenesCompra;
using SteeLog.Domain.OrdenesCompra;
using SteeLog.Infrastructure.Persistence;
using SteeLog.Infrastructure.Inventario;

namespace SteeLog.Infrastructure.OrdenesCompra
{
    public sealed class NpgsqlOrdenes : IOrdenesEscritura
    {
        private readonly NpgsqlUnitOfWork uow;
        private readonly NpgsqlInventario inventario;
        public NpgsqlOrdenes(NpgsqlUnitOfWork uow) { this.uow = uow; inventario = new NpgsqlInventario(uow); }
        private NpgsqlCommand Cmd(string sql) => new NpgsqlCommand(sql, uow.Connection, uow.Transaction);
        private static object Nullable(string value) => string.IsNullOrWhiteSpace(value) ? (object)DBNull.Value : value.Trim();
        private static string Estado(EstadoOrden estado)
        {
            switch (estado) { case EstadoOrden.Pendiente: return "Pendiente"; case EstadoOrden.RecibidaParcial: return "Recibida Parcial"; case EstadoOrden.RecibidaCompleta: return "Recibida Completa"; default: throw new ArgumentOutOfRangeException(nameof(estado)); }
        }
        private static EstadoOrden Estado(string estado)
        {
            switch (estado) { case "Pendiente": return EstadoOrden.Pendiente; case "Recibida Parcial": return EstadoOrden.RecibidaParcial; case "Recibida Completa": return EstadoOrden.RecibidaCompleta; default: throw new InvalidOperationException("Estado de orden desconocido."); }
        }
        public async Task<OrdenCompra> BloquearAsync(int id, CancellationToken cancellationToken = default)
        {
            string estado; Cabecera cabecera;
            using (var cmd = Cmd("SELECT estado,fecha,nro_orden,nro_remito,id_proveedor,costoflete,observaciones FROM public.ordenes_compra WHERE id_orden_compra=@id FOR UPDATE")) {
                cmd.Parameters.AddWithValue("id", id);
                using (var r = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false)) {
                    if (!await r.ReadAsync(cancellationToken).ConfigureAwait(false)) throw new InvalidOperationException("La orden indicada no existe.");
                    estado = r.GetString(0);
                    cabecera = Cabecera.Restaurar(r.GetDateTime(1), r.GetString(2), r.IsDBNull(3) ? null : r.GetString(3), r.IsDBNull(4) ? 0 : r.GetInt32(4), r.GetDecimal(5), r.IsDBNull(6) ? null : r.GetString(6));
                }
            }
            var lineas = new List<LineaOrden>();
            using (var cmd = Cmd("SELECT id_detalle,id_material,cantidad_pedida,cantidad_recibida,stock_despues_recepcion IS NOT NULL FROM public.ordenes_compra_detalle WHERE id_orden_compra=@id ORDER BY id_detalle FOR UPDATE")) {
                cmd.Parameters.AddWithValue("id", id);
                using (var r = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
                    while (await r.ReadAsync(cancellationToken).ConfigureAwait(false)) lineas.Add(new LineaOrden(r.GetInt32(0), r.GetInt32(1), r.GetInt32(2), r.GetInt32(3), r.GetBoolean(4)));
            }
            return new OrdenCompra(id, cabecera, Estado(estado), lineas);
        }
        private async Task<int> DestinoAsync(CancellationToken cancellationToken)
        {
            using (var cmd = Cmd(@"SELECT id_ubicacion FROM public.ubicacion ORDER BY CASE WHEN UPPER(nombre) LIKE '%GALPON%' THEN 0 WHEN UPPER(nombre) LIKE '%DEPÓSITO%' OR UPPER(nombre) LIKE '%DEPOSITO%' THEN 1 ELSE 2 END, nombre LIMIT 1")) {
                var valor = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                if (valor == null || valor == DBNull.Value) throw new InvalidOperationException("No se encontró una ubicación para Galpón.");
                return Convert.ToInt32(valor);
            }
        }
        private static void ParametrosCabecera(NpgsqlCommand cmd, CabeceraOrden h, int destino)
        {
            cmd.Parameters.AddWithValue("fecha", h.Fecha.Date); cmd.Parameters.AddWithValue("numero", h.Numero.Trim());
            cmd.Parameters.AddWithValue("remito", Nullable(h.Remito)); cmd.Parameters.AddWithValue("proveedor", h.ProveedorId);
            cmd.Parameters.AddWithValue("destino", destino); cmd.Parameters.AddWithValue("flete", h.Flete); cmd.Parameters.AddWithValue("observaciones", Nullable(h.Observaciones));
        }
        public async Task<int> CrearAsync(CabeceraOrden cabecera, IReadOnlyList<LineaNueva> lineas, string usuario, CancellationToken cancellationToken = default)
        {
            int destino = await DestinoAsync(cancellationToken).ConfigureAwait(false); int id;
            using (var cmd = Cmd(@"INSERT INTO public.ordenes_compra(fecha,nro_orden,nro_remito,id_proveedor,id_destino,tipo_destino,costoflete,observaciones,estado,usuario_creacion) VALUES(@fecha,@numero,@remito,@proveedor,@destino,'UBICACION',@flete,@observaciones,'Pendiente',@usuario) RETURNING id_orden_compra")) {
                ParametrosCabecera(cmd, cabecera, destino); cmd.Parameters.AddWithValue("usuario", Nullable(usuario));
                id = Convert.ToInt32(await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false));
            }
            await InsertarLineasAsync(id, lineas, cancellationToken).ConfigureAwait(false); return id;
        }
        public async Task EditarCabeceraAsync(int id, CabeceraOrden cabecera, CancellationToken cancellationToken = default)
        {
            int destino = await DestinoAsync(cancellationToken).ConfigureAwait(false);
            using (var cmd = Cmd(@"UPDATE public.ordenes_compra SET fecha=@fecha,nro_orden=@numero,nro_remito=@remito,id_proveedor=@proveedor,id_destino=@destino,tipo_destino='UBICACION',costoflete=@flete,observaciones=@observaciones,updated_at=NOW() WHERE id_orden_compra=@id")) {
                ParametrosCabecera(cmd, cabecera, destino); cmd.Parameters.AddWithValue("id", id); await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        private async Task InsertarLineasAsync(int id, IReadOnlyList<LineaNueva> lineas, CancellationToken cancellationToken)
        {
            var saldos = await inventario.BloquearAsync(lineas.Select(x => x.MaterialId).OrderBy(x => x).ToArray(), cancellationToken).ConfigureAwait(false);
            foreach (var linea in lineas) {
                using (var cmd = Cmd(@"INSERT INTO public.ordenes_compra_detalle(id_orden_compra,id_material,cantidad_pedida,cantidad_recibida,cantidad_faltante,observacion_item,stock_al_crear) VALUES(@id,@material,@pedida,0,@pedida,@observacion,@stock)")) {
                    cmd.Parameters.AddWithValue("id", id); cmd.Parameters.AddWithValue("material", linea.MaterialId); cmd.Parameters.AddWithValue("pedida", CantidadOrden.Entera(linea.Pedida, false)); cmd.Parameters.AddWithValue("observacion", Nullable(linea.Observacion)); cmd.Parameters.AddWithValue("stock", saldos[linea.MaterialId]);
                    await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                }
            }
        }
        public async Task ReemplazarLineasAsync(int id, IReadOnlyList<LineaNueva> lineas, CancellationToken cancellationToken = default)
        {
            using (var cmd = Cmd("DELETE FROM public.ordenes_compra_detalle WHERE id_orden_compra=@id")) { cmd.Parameters.AddWithValue("id", id); await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false); }
            await InsertarLineasAsync(id, lineas, cancellationToken).ConfigureAwait(false);
        }
        public async Task EliminarAsync(int id, CancellationToken cancellationToken = default)
        {
            using (var cmd = Cmd("DELETE FROM public.ordenes_compra_detalle WHERE id_orden_compra=@id")) { cmd.Parameters.AddWithValue("id", id); await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false); }
            using (var cmd = Cmd("DELETE FROM public.ordenes_compra WHERE id_orden_compra=@id")) { cmd.Parameters.AddWithValue("id", id); await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false); }
        }
        public async Task GuardarRecepcionAsync(int id, OrdenCompra orden, IReadOnlyDictionary<int, string> observaciones, IReadOnlyDictionary<int, decimal> snapshots, string usuario, string observacion, CancellationToken cancellationToken = default)
        {
            foreach (var linea in orden.Lineas) {
                string obs = null; if (observaciones != null) observaciones.TryGetValue(linea.Id, out obs);
                decimal snapshot; var tiene = snapshots.TryGetValue(linea.Id, out snapshot);
                using (var cmd = Cmd(@"UPDATE public.ordenes_compra_detalle SET cantidad_recibida=@recibida,cantidad_faltante=@faltante,observacion_item=@observacion,stock_despues_recepcion=CASE WHEN @tiene THEN @snapshot ELSE COALESCE(stock_despues_recepcion,stock_al_crear) END WHERE id_detalle=@linea AND id_orden_compra=@id")) {
                    cmd.Parameters.AddWithValue("recibida", linea.Recibida); cmd.Parameters.AddWithValue("faltante", linea.Faltante); cmd.Parameters.AddWithValue("observacion", Nullable(obs)); cmd.Parameters.AddWithValue("tiene", tiene); cmd.Parameters.AddWithValue("snapshot", snapshot); cmd.Parameters.AddWithValue("linea", linea.Id); cmd.Parameters.AddWithValue("id", id);
                    await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            using (var cmd = Cmd(@"UPDATE public.ordenes_compra SET estado=@estado,usuario_revision=@usuario,fecha_revision=NOW(),observacion_revision=@observacion,updated_at=NOW() WHERE id_orden_compra=@id")) {
                cmd.Parameters.AddWithValue("estado", Estado(orden.Estado)); cmd.Parameters.AddWithValue("usuario", Nullable(usuario)); cmd.Parameters.AddWithValue("observacion", Nullable(observacion)); cmd.Parameters.AddWithValue("id", id); await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        public async Task GuardarMaterialAsync(int id, int lineaId, int materialId, decimal stockAntes, decimal? stockDespues, CancellationToken cancellationToken = default)
        {
            using (var cmd = Cmd(@"UPDATE public.ordenes_compra_detalle SET id_material=@material,stock_al_crear=@antes,stock_despues_recepcion=@despues WHERE id_detalle=@linea AND id_orden_compra=@id")) {
                cmd.Parameters.AddWithValue("material", materialId); cmd.Parameters.AddWithValue("antes", stockAntes); cmd.Parameters.AddWithValue("despues", stockDespues.HasValue ? (object)stockDespues.Value : DBNull.Value); cmd.Parameters.AddWithValue("linea", lineaId); cmd.Parameters.AddWithValue("id", id); await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
            using (var cmd = Cmd("UPDATE public.ordenes_compra SET updated_at=NOW() WHERE id_orden_compra=@id")) { cmd.Parameters.AddWithValue("id", id); await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false); }
        }
    }
}
