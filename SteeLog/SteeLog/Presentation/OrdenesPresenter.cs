using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.Core.Application.OrdenesCompra;
using SteeLog.Core.Application.Common;
using SteeLog.Domain.OrdenesCompra;

namespace SteeLog.Presentation
{
    public interface IOrdenesVista
    {
        void SetOcupada(bool ocupada);
        void MostrarError(Exception error);
    }
    public interface IOrdenesResultadoIncierto { void MarcarResultadoIncierto(); }
    public sealed class ResultadoOrdenes<T>
    {
        public bool Exitoso { get; }
        public T Valor { get; }
        public bool Incierto { get; }
        internal ResultadoOrdenes(bool exitoso, T valor, bool incierto = false) { Exitoso = exitoso; Valor = valor; Incierto = incierto; }
    }
    public sealed class OrdenesPresenter
    {
        private readonly IUseCaseExecutor executor;
        private readonly IOrdenesVista vista;
        private readonly IOrdenesPendientesPdf pdf;
        private int ocupado;
        public OrdenesPresenter(IUseCaseExecutor executor, IOrdenesVista vista, IOrdenesPendientesPdf pdf = null)
        {
            this.executor = executor ?? throw new ArgumentNullException(nameof(executor));
            this.vista = vista ?? throw new ArgumentNullException(nameof(vista));
            this.pdf = pdf;
        }
        private async Task<ResultadoOrdenes<T>> Ejecutar<T>(Func<Task<T>> operacion)
        {
            if (Interlocked.CompareExchange(ref ocupado, 1, 0) != 0) return new ResultadoOrdenes<T>(false, default);
            try {
                vista.SetOcupada(true);
                return new ResultadoOrdenes<T>(true, await operacion());
            }
            catch (ResultadoEscrituraInciertoException e)
            {
                if (vista is IOrdenesResultadoIncierto incierta) incierta.MarcarResultadoIncierto(); else vista.MostrarError(e);
                return new ResultadoOrdenes<T>(false, default, true);
            }
            catch (OperationCanceledException) { return new ResultadoOrdenes<T>(false, default); }
            catch (Exception e) { vista.MostrarError(e); return new ResultadoOrdenes<T>(false, default); }
            finally { Interlocked.Exchange(ref ocupado, 0); vista.SetOcupada(false); }
        }
        public Task<ResultadoOrdenes<IReadOnlyList<OrdenLectura>>> ListarAsync(string estado, bool recepcion, bool historial, CancellationToken token = default)
            => Ejecutar(() => executor.ExecuteAsync<ConsultarOrdenes, IReadOnlyList<OrdenLectura>>(x => x.ListarAsync(estado, recepcion, historial, token)));
        public Task<ResultadoOrdenes<OrdenLectura>> ObtenerAsync(int id, CancellationToken token = default)
            => Ejecutar(() => executor.ExecuteAsync<ConsultarOrdenes, OrdenLectura>(x => x.ObtenerAsync(id, token)));
        public Task<ResultadoOrdenes<IReadOnlyList<ProveedorLectura>>> ProveedoresAsync(CancellationToken token = default)
            => Ejecutar(() => executor.ExecuteAsync<ConsultarOrdenes, IReadOnlyList<ProveedorLectura>>(x => x.ProveedoresAsync(token)));
        public Task<ResultadoOrdenes<IReadOnlyList<MaterialLectura>>> MaterialesAsync(CancellationToken token = default)
            => Ejecutar(() => executor.ExecuteAsync<ConsultarOrdenes, IReadOnlyList<MaterialLectura>>(x => x.MaterialesAsync(token)));
        public Task<ResultadoOrdenes<string>> DestinoAsync(CancellationToken token = default)
            => Ejecutar(() => executor.ExecuteAsync<ConsultarOrdenes, string>(x => x.DestinoAsync(token)));
        public Task<ResultadoOrdenes<IReadOnlyList<PendienteLectura>>> PendientesAsync(CancellationToken token = default)
            => Ejecutar(() => executor.ExecuteAsync<ConsultarOrdenes, IReadOnlyList<PendienteLectura>>(x => x.PendientesAsync(token)));
        public string SugerirNombrePdf() => (pdf ?? throw new InvalidOperationException("No se configuró el generador PDF.")).SugerirNombreArchivo();
        public Task<ResultadoOrdenes<bool>> ExportarPendientesAsync(string ruta, CancellationToken token = default)
            => Ejecutar(async () => {
                var items = await executor.ExecuteAsync<ConsultarOrdenes, IReadOnlyList<PendienteLectura>>(x => x.PendientesAsync(token));
                token.ThrowIfCancellationRequested();
                (pdf ?? throw new InvalidOperationException("No se configuró el generador PDF.")).Generar(ruta, items);
                return true;
            });
        public Task<ResultadoOrdenes<int>> CrearAsync(CabeceraOrden cabecera, IEnumerable<LineaNueva> lineas, CancellationToken token = default)
            => Ejecutar(() => executor.ExecuteAsync<CrearOrden, int>(x => x.EjecutarAsync(cabecera, lineas, token)));
        public Task<ResultadoOrdenes<int>> EditarAsync(int id, CabeceraOrden cabecera, IEnumerable<LineaNueva> lineas, CancellationToken token = default)
            => Ejecutar(() => executor.ExecuteAsync<EditarOrden, int>(x => x.EjecutarAsync(id, cabecera, lineas, token)));
        public Task<ResultadoOrdenes<bool>> CambiarMaterialAsync(int id, int linea, int material, CancellationToken token = default)
            => Ejecutar(() => executor.ExecuteAsync<CambiarMaterialOrden, bool>(x => x.EjecutarAsync(id, linea, material, token)));
        public Task<ResultadoOrdenes<bool>> EliminarAsync(int id, CancellationToken token = default)
            => Ejecutar(() => executor.ExecuteAsync<EliminarOrden, bool>(x => x.EjecutarAsync(id, token)));
        public Task<ResultadoOrdenes<EstadoOrden>> RecibirAsync(int id, IReadOnlyDictionary<int, decimal> cantidades, IReadOnlyDictionary<int, string> observaciones, string observacion, CancellationToken token = default)
            => Ejecutar(() => executor.ExecuteAsync<ConfirmarRecepcion, EstadoOrden>(x => x.EjecutarAsync(id, cantidades, observaciones, observacion, token)));
    }
}
