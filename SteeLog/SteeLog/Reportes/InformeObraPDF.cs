using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.CompositionRoot;
using SteeLog.Core.Application.Canaletas;
using SteeLog.Core.Application.Reportes;
using SteeLog.Datos;
using SteeLog.Presentation;
using SteeLog.Servicios;

namespace SteeLog.Reportes
{
    public sealed class InformeObraPDF
    {
        public async Task GenerarAsync(string ruta, int idObra, string nombreObra, DateTime? desde, DateTime? hasta, CancellationToken token)
        {
            bool mostrar = SessionManager.Role == UserRole.Admin || (!string.IsNullOrEmpty(nombreObra) && nombreObra.IndexOf("galpon", StringComparison.OrdinalIgnoreCase) >= 0);
            var presenter = new CanaletasPresenter(LocalServices.Current.Executor);
            var canaletas = await presenter.ListarAsync(idObra, token).ConfigureAwait(false);
            await Task.Run(() => {
                token.ThrowIfCancellationRequested();
                var datos = new InformeObraDatos();
                var movimientos = datos.ObtenerMovimientosObra(idObra);
                var herramientas = datos.ObtenerHerramientasObra(idObra, nombreObra);
                var insumos = datos.ObtenerInsumosObra(idObra, nombreObra);
                var filtradas = canaletas.Where(c => (!desde.HasValue || c.FechaCreacion.Date >= desde.Value.Date) && (!hasta.HasValue || c.FechaCreacion.Date <= hasta.Value.Date)).ToList();
                if (desde.HasValue) movimientos = movimientos.Where(m => m.fecha.Date >= desde.Value.Date).ToList();
                if (hasta.HasValue) movimientos = movimientos.Where(m => m.fecha.Date <= hasta.Value.Date).ToList();
                var resumen = InformeObraDatos.CalcularResumen(movimientos, mostrar, filtradas.Where(c => !c.Pagado).Sum(c => c.Precio));
                var snapshot = new InformeObraSnapshot {
                    NombreObra = nombreObra, Desde = desde, Hasta = hasta, MostrarValorizacion = mostrar,
                    Resumen = Map(resumen), Movimientos = movimientos.Select(Map).ToList(), Herramientas = herramientas.Select(Map).ToList(), Insumos = insumos.Select(Map).ToList(),
                    Canaletas = filtradas.Select(c => new InformeCanaletaPlegadoDetallePdf { id = c.Id, id_obra = c.ObraId, descripcion = c.Descripcion, pagado = c.Pagado, precio = c.Precio, fecha_creacion = c.FechaCreacion }).ToList()
                };
                token.ThrowIfCancellationRequested();
                LocalServices.Current.InformeObraPdf.Generar(ruta, snapshot);
            }, token).ConfigureAwait(false);
        }
        private static InformeMovimientoDetallePdf Map(InformeMovimientoDetalle x) => new InformeMovimientoDetallePdf { id_movimiento = x.id_movimiento, fecha = x.fecha, nroremito = x.nroremito, tipo_movimiento = x.tipo_movimiento, material = x.material, cantidad = x.cantidad, origen = x.origen, destino = x.destino, observaciones = x.observaciones, costoflete = x.costoflete, esrecorte = x.esrecorte, medida_recorte = x.medida_recorte, esinsumo = x.esinsumo, es_compra_eventual = x.es_compra_eventual, precio_unitario = x.precio_unitario, precio_total = x.precio_total };
        private static InformeObraResumenPdf Map(InformeObraResumen x) => new InformeObraResumenPdf { CantRemitos = x.CantRemitos, CantMovimientos = x.CantMovimientos, TotalMateriales = x.TotalMateriales, TotalFletes = x.TotalFletes, TotalPendiente = x.TotalPendiente };
        private static InformeHerramientaDetallePdf Map(InformeHerramientaDetalle x) => new InformeHerramientaDetallePdf { id_herramienta = x.id_herramienta, nombre = x.nombre, estado = x.estado, fecha_ingreso = x.fecha_ingreso, fecha_retorno = x.fecha_retorno, en_obra = x.en_obra };
        private static InformeInsumoDetallePdf Map(InformeInsumoDetalle x) => new InformeInsumoDetallePdf { nombre = x.nombre, consumido = x.consumido, fecha_ingreso = x.fecha_ingreso, fecha_retorno = x.fecha_retorno, en_obra = x.en_obra };
    }
}
