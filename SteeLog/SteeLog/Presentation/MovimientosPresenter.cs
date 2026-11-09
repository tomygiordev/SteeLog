using System;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using SteeLog.Core.Application.Common;
using SteeLog.Core.Application.Movimientos;
using SteeLog.Core.Application.Inventario;
using System.Collections.Generic;

namespace SteeLog.Presentation
{
    public interface IMovimientosVista
    {
        void SetOcupada(bool ocupada);
        void MostrarError(Exception error);
    }
    public interface IMovimientosResultadoIncierto { void MarcarResultadoIncierto(); }
    public sealed class MovimientosPresenter
    {
        private readonly IUseCaseExecutor executor;
        private readonly IMovimientosVista vista;
        private readonly IRemitoPdf pdf;
        private readonly EjecucionExclusiva ejecucion;
        public MovimientosPresenter(IUseCaseExecutor executor, IMovimientosVista vista, IRemitoPdf pdf = null)
        {
            this.executor = executor ?? throw new ArgumentNullException(nameof(executor));
            this.vista = vista ?? throw new ArgumentNullException(nameof(vista));
            this.pdf = pdf;
            ejecucion = new EjecucionExclusiva(vista.SetOcupada, vista.MostrarError);
        }
        private Task<int?> Ejecutar(Func<Task<int>> accion)
            => ejecucion.Ejecutar<int?>(async () => await accion(),
                ex => { if (vista is IMovimientosResultadoIncierto incierto) incierto.MarcarResultadoIncierto(); else vista.MostrarError(ex); });
        public Task<int?> GuardarAsync(int? id, MovimientoSolicitud solicitud, CancellationToken token = default)
            => Ejecutar(() => id.HasValue
                ? executor.ExecuteAsync<EditarMovimiento, int>(x => x.EjecutarAsync(id.Value, solicitud, token))
                : executor.ExecuteAsync<RegistrarMovimiento, int>(x => x.EjecutarAsync(solicitud, token)));
        public Task<IReadOnlyList<HistorialMovimientoLectura>> HistorialAsync(DateTime? desde, DateTime? hasta, string tipo, CancellationToken token=default)
            => executor.ExecuteAsync<ConsultarHistorialMovimientos,IReadOnlyList<HistorialMovimientoLectura>>(x => x.ListarAsync(desde,hasta,tipo,token));
        public Task<CatalogosMovimiento> CatalogosAsync(CancellationToken token=default)
            => executor.ExecuteAsync<ConsultarCatalogosMovimiento,CatalogosMovimiento>(x => x.ObtenerAsync(token));
        public Task<IReadOnlyList<MaterialInventarioLectura>> MaterialesAsync(CancellationToken token=default)
            => executor.ExecuteAsync<ConsultarMateriales,IReadOnlyList<MaterialInventarioLectura>>(x => x.ListarAsync(new FiltroMaterialLectura(),token));
        public Task<IReadOnlyList<CategoriaMaterialLectura>> CategoriasAsync(CancellationToken token=default)
            => executor.ExecuteAsync<ConsultarMateriales,IReadOnlyList<CategoriaMaterialLectura>>(x => x.CategoriasAsync(token));
        public Task<IReadOnlyList<RecorteLectura>> RecortesAsync(CancellationToken token=default)
            => executor.ExecuteAsync<ConsultarRecortes,IReadOnlyList<RecorteLectura>>(x => x.ListarAsync(token:token));
        public Task<MovimientoEdicion> ObtenerEdicionAsync(int id, CancellationToken token = default)
            => executor.ExecuteAsync<ConsultarMovimientoEdicion, MovimientoEdicion>(x => x.ObtenerAsync(id, token));
        public Task<string> GenerarRemitoAsync(int id, CancellationToken token = default)
            => ejecucion.Ejecutar(async () => {
                var remito = await executor.ExecuteAsync<ConsultarRemito, RemitoLectura>(x => x.ObtenerAsync(id, token));
                token.ThrowIfCancellationRequested();
                if (remito == null) throw new InvalidOperationException("El movimiento no tiene detalles para imprimir.");
                var renderer = pdf ?? throw new InvalidOperationException("No se configuró el PDF de remito.");
                string ruta = Path.Combine(Path.GetTempPath(), renderer.SugerirNombreArchivo(remito));
                await Task.Run(() => renderer.Generar(ruta, remito), token);
                return ruta;
            });
        public Task<int?> CrearFleteAsync(string nombre,CancellationToken token=default)
            => Ejecutar(() => executor.ExecuteAsync<GestionarFletes,int>(x => x.CrearAsync(nombre,token)));
        public Task<int?> DesactivarFleteAsync(int id,CancellationToken token=default)
            => Ejecutar(() => executor.ExecuteAsync<GestionarFletes,int>(x => x.DesactivarAsync(id,token)));
        public Task<decimal?> ActualizarPrecioAsync(int id,decimal precio,CancellationToken token=default)
            => ejecucion.Ejecutar<decimal?>(async () => await executor.ExecuteAsync<ActualizarPrecioEventual,decimal>(x => x.EjecutarAsync(id,precio,token)));
        public Task<int?> EliminarAsync(int id, CancellationToken token = default)
            => Ejecutar(() => executor.ExecuteAsync<EliminarMovimiento, int>(x => x.EjecutarAsync(id, token)));
    }
}
