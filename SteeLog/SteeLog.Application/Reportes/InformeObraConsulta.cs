using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.Core.Application.Canaletas;
using SteeLog.Core.Application.Identidad;
using SteeLog.Domain.Reportes;

namespace SteeLog.Core.Application.Reportes
{
    public interface IInformeObraConsulta
    {
        Task<IReadOnlyList<InformeMovimientoDetallePdf>> MovimientosAsync(int idObra, CancellationToken t);
        Task<IReadOnlyList<InformeHerramientaDetallePdf>> HerramientasAsync(int idObra, string nombreObra, CancellationToken t);
        Task<IReadOnlyList<InformeInsumoDetallePdf>> InsumosAsync(int idObra, string nombreObra, CancellationToken t);
        Task<DateTime?> PrimeraFechaRemitoAsync(int idObra, CancellationToken t);
    }

    public sealed class InformeObraLecturas
    {
        public List<InformeMovimientoDetallePdf> Movimientos { get; set; }
        public List<InformeHerramientaDetallePdf> Herramientas { get; set; }
        public List<InformeInsumoDetallePdf> Insumos { get; set; }
        public bool MostrarValorizacion { get; set; }
    }

    public static class ValorizacionInformeObra
    {
        private static MovimientoValorizacion Map(InformeMovimientoDetallePdf x) => new MovimientoValorizacion {
            Id = x.id_movimiento, Remito = x.nroremito, Tipo = x.tipo_movimiento, Material = x.material,
            EsRecorte = x.esrecorte, Cantidad = x.cantidad, PrecioUnitario = x.precio_unitario,
            PrecioTotal = x.precio_total, CostoFlete = x.costoflete, EsCompraEventual = x.es_compra_eventual,
            Origen = x.origen, Destino = x.destino, Fecha = x.fecha };
        public static decimal CalcularValor(IEnumerable<InformeMovimientoDetallePdf> items) => ValorizacionObra.CalcularValor((items ?? Enumerable.Empty<InformeMovimientoDetallePdf>()).Select(Map));
        public static decimal ValorDetalle(decimal cantidad, decimal precioUnitario) => ValorizacionObra.ValorDetalle(cantidad, precioUnitario);
        public static IReadOnlyList<decimal> ValoresFilas(IEnumerable<InformeMovimientoDetallePdf> filas, IEnumerable<InformeMovimientoDetallePdf> universo)
            => ValorizacionObra.ValoresFilas((filas ?? Enumerable.Empty<InformeMovimientoDetallePdf>()).Select(Map), (universo ?? Enumerable.Empty<InformeMovimientoDetallePdf>()).Select(Map));
        public static BalanceMaterialObra BalanceMaterial(IEnumerable<InformeMovimientoDetallePdf> items, string obra)
            => ValorizacionObra.BalanceMaterial((items ?? Enumerable.Empty<InformeMovimientoDetallePdf>()).Select(Map), obra);
        public static ResumenValorizacion Resumen(IEnumerable<InformeMovimientoDetallePdf> items, bool mostrar, decimal pendientes)
            => ValorizacionObra.CalcularResumen((items ?? Enumerable.Empty<InformeMovimientoDetallePdf>()).Select(Map), mostrar, pendientes);
    }

    public sealed class ConsultarInformeObra
    {
        private readonly Session session;
        private readonly IInformeObraConsulta consulta;
        private readonly ICanaletasConsulta canaletas;
        public ConsultarInformeObra(Session session, IInformeObraConsulta consulta, ICanaletasConsulta canaletas)
        { this.session = session ?? throw new ArgumentNullException(nameof(session)); this.consulta = consulta ?? throw new ArgumentNullException(nameof(consulta)); this.canaletas = canaletas ?? throw new ArgumentNullException(nameof(canaletas)); }
        public Task<IReadOnlyList<InformeMovimientoDetallePdf>> MovimientosAsync(int id, CancellationToken t) { session.Require(Role.Galpon); return consulta.MovimientosAsync(id, t); }
        public Task<IReadOnlyList<InformeHerramientaDetallePdf>> HerramientasAsync(int id, string nombre, CancellationToken t) { session.Require(Role.Galpon); return consulta.HerramientasAsync(id, nombre, t); }
        public Task<IReadOnlyList<InformeInsumoDetallePdf>> InsumosAsync(int id, string nombre, CancellationToken t) { session.Require(Role.Galpon); return consulta.InsumosAsync(id, nombre, t); }
        public Task<DateTime?> PrimeraFechaRemitoAsync(int id, CancellationToken t) { session.Require(Role.Galpon); return consulta.PrimeraFechaRemitoAsync(id, t); }
        public bool MostrarValorizacion(string nombre) { session.Require(Role.Galpon); return session.Role == Role.Admin || (!string.IsNullOrEmpty(nombre) && nombre.IndexOf("galpon", StringComparison.OrdinalIgnoreCase) >= 0); }
        public async Task<InformeObraLecturas> LecturasAsync(int id, string nombre, CancellationToken t)
        {
            var mostrar = MostrarValorizacion(nombre);
            var movTask = consulta.MovimientosAsync(id, t);
            var herTask = consulta.HerramientasAsync(id, nombre, t);
            var insTask = consulta.InsumosAsync(id, nombre, t);
            await Task.WhenAll(movTask, herTask, insTask).ConfigureAwait(false);
            t.ThrowIfCancellationRequested();
            return new InformeObraLecturas { MostrarValorizacion = mostrar, Movimientos = movTask.Result.ToList(),
                Herramientas = herTask.Result.ToList(), Insumos = insTask.Result.ToList() };
        }
        public async Task<InformeObraSnapshot> SnapshotAsync(int id, string nombre, DateTime? desde, DateTime? hasta, CancellationToken t)
        {
            session.Require(Role.Galpon);
            var lecturasTask = LecturasAsync(id, nombre, t);
            var canTask = canaletas.ListarAsync(id, t);
            await Task.WhenAll(lecturasTask, canTask).ConfigureAwait(false);
            t.ThrowIfCancellationRequested();
            var lecturas = lecturasTask.Result;
            var movimientos = lecturas.Movimientos.Where(m => (!desde.HasValue || m.fecha.Date >= desde.Value.Date) && (!hasta.HasValue || m.fecha.Date <= hasta.Value.Date)).ToList();
            var filtradas = canTask.Result.Where(c => (!desde.HasValue || c.FechaCreacion.Date >= desde.Value.Date) && (!hasta.HasValue || c.FechaCreacion.Date <= hasta.Value.Date)).ToList();
            return new InformeObraSnapshot { NombreObra = nombre, Desde = desde, Hasta = hasta, MostrarValorizacion = lecturas.MostrarValorizacion,
                Movimientos = movimientos, Herramientas = lecturas.Herramientas, Insumos = lecturas.Insumos,
                Canaletas = filtradas.Select(c => new InformeCanaletaPlegadoDetallePdf { id = c.Id, id_obra = c.ObraId, descripcion = c.Descripcion, pagado = c.Pagado, precio = c.Precio, fecha_creacion = c.FechaCreacion }).ToList(),
                Resumen = Resumir(movimientos, lecturas.MostrarValorizacion, filtradas.Where(c => !c.Pagado).Sum(c => c.Precio)) };
        }
        public static InformeObraResumenPdf Resumir(IEnumerable<InformeMovimientoDetallePdf> movimientos, bool mostrar, decimal pendientes)
        {
            var r = ValorizacionInformeObra.Resumen(movimientos, mostrar, pendientes);
            return new InformeObraResumenPdf { CantRemitos=r.CantRemitos, CantMovimientos=r.CantMovimientos, TotalMateriales=r.TotalMateriales, TotalFletes=r.TotalFletes, TotalPendiente=r.TotalPendiente };
        }
    }
}
