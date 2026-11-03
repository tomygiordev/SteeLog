using System;
using System.Threading;

namespace SteeLog.Presentation
{
    public sealed class CargaVigente : IDisposable
    {
        private CancellationTokenSource actual;
        private long version;
        private bool cerrada;
        public SolicitudCarga Iniciar(IOrdenesVista vista, Func<bool> vistaCerrada, bool mostrarOcupada = true)
        {
            if (cerrada) throw new ObjectDisposedException(nameof(CargaVigente));
            actual?.Cancel();
            var source = new CancellationTokenSource();
            actual = source;
            var id = ++version;
            return new SolicitudCarga(this, id, source, vista, vistaCerrada, mostrarOcupada);
        }
        public void Invalidar()
        {
            version++;
            actual?.Cancel();
            actual = null;
        }
        internal bool EsActual(long id) => !cerrada && version == id;
        internal void Terminar(long id, CancellationTokenSource source)
        {
            if (version == id && ReferenceEquals(actual, source)) actual = null;
        }
        public void Dispose() { if (!cerrada) { Invalidar(); cerrada = true; } }
    }
    public sealed class EstadoModeloOrden
    {
        public int Id { get; private set; }
        public bool Listo { get; private set; }
        public void Seleccionar(int id) { Id = id; Listo = false; }
        public void Nueva() { Id = 0; Listo = true; }
        public bool Aplicar(int id) { if (Id != id) return false; Listo = true; return true; }
    }
    public sealed class SolicitudCarga : IDisposable, IOrdenesVista
    {
        private readonly CargaVigente owner;
        private readonly long version;
        private readonly CancellationTokenSource source;
        private readonly IOrdenesVista vista;
        private readonly Func<bool> vistaCerrada;
        private readonly bool mostrarOcupada;
        internal SolicitudCarga(CargaVigente owner, long version, CancellationTokenSource source, IOrdenesVista vista, Func<bool> vistaCerrada, bool mostrarOcupada)
        { this.owner = owner; this.version = version; this.source = source; this.vista = vista; this.vistaCerrada = vistaCerrada; this.mostrarOcupada = mostrarOcupada; }
        public CancellationToken Token => source.Token;
        public bool EsActual => owner.EsActual(version) && !source.IsCancellationRequested && !vistaCerrada();
        public void SetOcupada(bool ocupada) { if (mostrarOcupada && EsActual) vista.SetOcupada(ocupada); }
        public void MostrarError(Exception error) { if (EsActual) vista.MostrarError(error); }
        public void Dispose() { owner.Terminar(version, source); source.Dispose(); }
    }
}
