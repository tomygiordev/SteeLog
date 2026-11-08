using System;
using System.Collections.Generic;
using System.Threading;
using System.Reflection;
using System.Windows.Forms;
using System.Threading.Tasks;
using SteeLog.Vistas.Movimientos.Ordenes;
using SteeLog.Presentation;
using SteeLog.Core.Application.Common;
using SteeLog.Core.Application.OrdenesCompra;
using SteeLog.Domain.OrdenesCompra;
using Xunit;

namespace SteeLog.Tests
{
    public sealed class OrdenesPresenterTests
    {
        private sealed class Vista : IOrdenesVista
        {
            public readonly List<bool> Estados = new List<bool>();
            public Exception Error;
            public void SetOcupada(bool ocupada) => Estados.Add(ocupada);
            public void MostrarError(Exception error) => Error = error;
        }
        private sealed class Ejecutor : IUseCaseExecutor
        {
            public int Llamadas;
            public readonly TaskCompletionSource<object> Pendiente = new TaskCompletionSource<object>();
            public async Task<TResult> ExecuteAsync<TUseCase, TResult>(Func<TUseCase, Task<TResult>> operation) where TUseCase : class
            { Llamadas++; return (TResult)await Pendiente.Task; }
        }
        [Fact]
        public async Task CargasFueraDeOrdenSoloAplicanUltimaYSuprimenErroresObsoletos()
        {
            var vista = new Vista(); using (var cargas = new CargaVigente()) {
                var a = cargas.Iniciar(vista, () => false); var esperaA = new TaskCompletionSource<string>();
                var pendienteA = AplicarAlCompletar(a, esperaA.Task);
                var b = cargas.Iniciar(vista, () => false); var esperaB = new TaskCompletionSource<string>();
                var pendienteB = AplicarAlCompletar(b, esperaB.Task);
                a.MostrarError(new InvalidOperationException("obsoleto"));
                esperaB.SetResult("B"); Assert.Equal("B", await pendienteB);
                esperaA.SetResult("A"); Assert.Null(await pendienteA); Assert.Null(vista.Error);
            }
        }
        [Fact]
        public async Task NuevaOrdenYCierreInvalidanCargasPendientes()
        {
            var vista = new Vista(); var cargas = new CargaVigente();
            var espera = new TaskCompletionSource<string>(); var anterior = AplicarAlCompletar(cargas.Iniciar(vista, () => false), espera.Task);
            cargas.Invalidar(); espera.SetResult("A"); Assert.Null(await anterior);
            var esperaCierre = new TaskCompletionSource<string>(); var cierre = AplicarAlCompletar(cargas.Iniciar(vista, () => false), esperaCierre.Task);
            cargas.Dispose(); esperaCierre.SetResult("B"); Assert.Null(await cierre);
        }
        private static async Task<string> AplicarAlCompletar(SolicitudCarga carga, Task<string> operacion)
        {
            using (carga) { var valor = await operacion; return carga.EsActual ? valor : null; }
        }
        [Fact]
        public void VistasBloqueanGuardarYConfirmarMientrasSeCargaNuevoId()
        {
            Exception error = null;
            var hilo = new Thread(() => {
                try {
                    using (var alta = new Vista_OrdenesAlta()) using (var revision = new Vista_OrdenesRevision()) {
                        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                        var altaType = alta.GetType(); var altaEstado = (EstadoModeloOrden)altaType.GetField("_modelo", flags).GetValue(alta);
                        altaType.GetField("_catalogosListos", flags).SetValue(alta, true);
                        var guardar = (Control)altaType.GetField("btnGuardar", flags).GetValue(alta);
                        alta.SetEditMode(1); Assert.True(altaEstado.Aplicar(1)); alta.SetOcupada(false); Assert.True(guardar.Enabled);
                        var a = new TaskCompletionSource<int>(); var b = new TaskCompletionSource<int>();
                        var cargas = (CargaVigente)altaType.GetField("_cargas", flags).GetValue(alta);
                        var cargaA = AplicarModeloAlCompletar(cargas.Iniciar(alta, () => alta.IsDisposed), a.Task, altaEstado);
                        alta.SetEditMode(2); Assert.False(altaEstado.Listo); Assert.False(guardar.Enabled);
                        var cargaB = AplicarModeloAlCompletar(cargas.Iniciar(alta, () => alta.IsDisposed), b.Task, altaEstado);
                        altaType.GetMethod("BtnGuardar_Click", flags).Invoke(alta, new object[] { guardar, EventArgs.Empty });
                        a.SetResult(1); Assert.False(cargaA.GetAwaiter().GetResult()); Assert.False(altaEstado.Listo); Assert.False(guardar.Enabled);
                        b.SetResult(2); Assert.True(cargaB.GetAwaiter().GetResult()); alta.SetOcupada(false); Assert.True(guardar.Enabled);
                        alta.PrepararNuevaOrden(); Assert.True(altaEstado.Listo); Assert.Equal(0, altaEstado.Id);
                        var revType = revision.GetType(); var revEstado = (EstadoModeloOrden)revType.GetField("_modelo", flags).GetValue(revision);
                        var confirmar = (Control)revType.GetField("btnConfirmar", flags).GetValue(revision);
                        revision.SetOrder(1); Assert.True(revEstado.Aplicar(1)); revType.GetField("_permiteRecepcion", flags).SetValue(revision, true);
                        revision.SetOcupada(false); Assert.True(confirmar.Enabled);
                        revision.SetOrder(2); Assert.False(revEstado.Listo); Assert.False(confirmar.Enabled);
                        revType.GetMethod("BtnConfirmar_Click", flags).Invoke(revision, new object[] { confirmar, EventArgs.Empty });
                        Assert.False(revEstado.Aplicar(1)); revision.SetOcupada(false); Assert.False(confirmar.Enabled);
                    }
                } catch (Exception e) { error = e; }
            });
            hilo.SetApartmentState(ApartmentState.STA); hilo.Start(); Assert.True(hilo.Join(TimeSpan.FromSeconds(15))); Assert.Null(error);
        }
        private static async Task<bool> AplicarModeloAlCompletar(SolicitudCarga carga, Task<int> operacion, EstadoModeloOrden estado)
        {
            using (carga) { var id = await operacion; return carga.EsActual && estado.Aplicar(id); }
        }
        [Fact]
        public void ConstructoresYPreparacionNoRequierenBase()
        {
            Exception error = null;
            var hilo = new Thread(() => {
                try {
                    using (var alta = new Vista_OrdenesAlta())
                    using (var listado = new Vista_OrdenesListado())
                    using (var revision = new Vista_OrdenesRevision())
                    using (var modulo = new Vista_OrdenesModulo()) {
                        alta.PrepararNuevaOrden(); alta.SetEditMode(12); revision.SetOrder(12); listado.Refrescar(); modulo.Refrescar();
                    }
                } catch (Exception e) { error = e; }
            });
            hilo.SetApartmentState(ApartmentState.STA); hilo.Start(); Assert.True(hilo.Join(TimeSpan.FromSeconds(15)));
            Assert.Null(error);
        }
        [Fact]
        public void CallbacksFallidosNoReviertenGuardadoNiPermitenReconfirmar()
        {
            Exception error = null;
            var hilo = new Thread(() => {
                try {
                    const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                    var avisos = new List<string>(); Action<string> aviso = avisos.Add;
                    using (var alta = new Vista_OrdenesAlta(aviso, aviso))
                    using (var revision = new Vista_OrdenesRevision(aviso, aviso)) {
                        var altaType = alta.GetType();
                        ((Control)altaType.GetField("txtNroOrden", flags).GetValue(alta)).Text = "OC-123";
                        int idAlta = 0;
                        alta.OnOrdenGuardada = id => {
                            idAlta = id;
                            Assert.Equal("", ((Control)altaType.GetField("txtNroOrden", flags).GetValue(alta)).Text);
                            throw new InvalidOperationException("navegación alta");
                        };
                        altaType.GetMethod("OrdenGuardada", flags).Invoke(alta, new object[] { 123, false });
                        Assert.Equal(123, idAlta);
                        Assert.Contains(avisos, x => x.Contains("se guardó") && x.Contains("navegación alta"));
                        var revType = revision.GetType(); revision.SetOrder(123);
                        var estado = (EstadoModeloOrden)revType.GetField("_modelo", flags).GetValue(revision);
                        Assert.True(estado.Aplicar(123)); revType.GetField("_permiteRecepcion", flags).SetValue(revision, true);
                        revision.SetOcupada(false);
                        int idRecepcion = 0;
                        revision.OnRecepcionConfirmada = id => { idRecepcion = id; throw new InvalidOperationException("navegación recepción"); };
                        revType.GetMethod("RecepcionConfirmada", flags).Invoke(revision, new object[] { 123, "Recibida Parcial" });
                        Assert.Equal(123, idRecepcion);
                        Assert.False((bool)revType.GetField("_permiteRecepcion", flags).GetValue(revision));
                        Assert.False(((Control)revType.GetField("btnConfirmar", flags).GetValue(revision)).Enabled);
                        revType.GetMethod("BtnConfirmar_Click", flags).Invoke(revision, new object[] { null, EventArgs.Empty });
                        Assert.Contains(avisos, x => x.Contains("se confirmó") && x.Contains("navegación recepción"));
                    }
                } catch (Exception ex) { error = ex; }
            });
            hilo.SetApartmentState(ApartmentState.STA); hilo.Start(); Assert.True(hilo.Join(TimeSpan.FromSeconds(15))); Assert.Null(error);
        }
        private sealed class VistaIncierta : IOrdenesVista, IOrdenesResultadoIncierto
        {
            public int Inciertos; public Exception Error;
            public void SetOcupada(bool ocupada) { }
            public void MostrarError(Exception error) => Error = error;
            public void MarcarResultadoIncierto() => Inciertos++;
        }
        private sealed class EjecutorIncierto : IUseCaseExecutor
        {
            public int Escrituras, Lecturas; public bool FallarLecturas;
            public Task<TResult> ExecuteAsync<TUseCase, TResult>(Func<TUseCase, Task<TResult>> operation) where TUseCase : class
            {
                var orden = new OrdenLectura { Id = 7, Numero = "OC-7", Estado = EstadosOrden.Pendiente, Fecha = DateTime.Today, Proveedor = "P", Destino = "D" };
                if (typeof(TResult) == typeof(OrdenLectura)) { Lecturas++; if (FallarLecturas) throw new OperationCanceledException(); return Task.FromResult((TResult)(object)orden); }
                if (typeof(TResult) == typeof(IReadOnlyList<OrdenLectura>)) { Lecturas++; if (FallarLecturas) throw new OperationCanceledException(); return Task.FromResult((TResult)(object)new List<OrdenLectura> { orden }); }
                Escrituras++; throw new ResultadoEscrituraInciertoException(new Exception("respuesta perdida"));
            }
        }
        [Fact]
        public async Task EscrituraInciertaAvisaALaVistaQueImplementaLaInterfaz()
        {
            var vista = new VistaIncierta(); var presenter = new OrdenesPresenter(new EjecutorIncierto(), vista);
            var resultado = await presenter.EliminarAsync(5);
            Assert.False(resultado.Exitoso); Assert.True(resultado.Incierto); Assert.Equal(1, vista.Inciertos); Assert.Null(vista.Error);
        }
        [Fact]
        public async Task EscrituraInciertaMuestraErrorSiLaVistaNoImplementaLaInterfaz()
        {
            var vista = new Vista(); var presenter = new OrdenesPresenter(new EjecutorIncierto(), vista);
            var resultado = await presenter.EliminarAsync(5);
            Assert.False(resultado.Exitoso); Assert.True(resultado.Incierto);
            Assert.IsType<ResultadoEscrituraInciertoException>(vista.Error); Assert.Equal(new[] { true, false }, vista.Estados);
        }
        private static void EnSta(Action accion)
        {
            Exception error = null;
            var hilo = new Thread(() => { try { accion(); } catch (Exception e) { error = e; } });
            hilo.SetApartmentState(ApartmentState.STA); hilo.Start(); Assert.True(hilo.Join(TimeSpan.FromSeconds(15))); Assert.Null(error);
        }
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        private static T Campo<T>(object o, string nombre) => (T)o.GetType().GetField(nombre, Flags).GetValue(o);
        private static void Asignar(object o, string nombre, object valor) => o.GetType().GetField(nombre, Flags).SetValue(o, valor);
        private static void Esperar(object o, string metodo, params object[] args) => ((Task)o.GetType().GetMethod(metodo, Flags).Invoke(o, args)).GetAwaiter().GetResult();
        [Fact]
        public void AltaIncierta_BloqueaGuardarYCambioDeMaterialHastaRecargaExitosa()
        {
            EnSta(() => {
                var avisos = new List<string>(); var ejecutor = new EjecutorIncierto();
                using (var alta = new Vista_OrdenesAlta()) {
                    Asignar(alta, "AdvertirResultadoIncierto", new Action<string>(avisos.Add));
                    var presenter = new OrdenesPresenter(ejecutor, alta); Asignar(alta, "CrearLector", new Func<IOrdenesVista, OrdenesPresenter>(v => new OrdenesPresenter(ejecutor, v)));
                    Asignar(alta, "_presenter", presenter); Asignar(alta, "_catalogosListos", true);
                    var guardar = Campo<Control>(alta, "btnGuardar");
                    alta.SetEditMode(7); Assert.True(Campo<EstadoModeloOrden>(alta, "_modelo").Aplicar(7)); alta.SetOcupada(false); Assert.True(guardar.Enabled);
                    Assert.True(presenter.EditarAsync(7, null, null).GetAwaiter().GetResult().Incierto);
                    Assert.Single(avisos); Assert.False(guardar.Enabled); Assert.Equal(1, ejecutor.Escrituras);
                    alta.GetType().GetMethod("BtnGuardar_Click", Flags).Invoke(alta, new object[] { guardar, EventArgs.Empty });
                    alta.GetType().GetMethod("CambiarMaterialImpactado", Flags).Invoke(alta, new object[] { 0 });
                    Assert.Equal(1, ejecutor.Escrituras); Assert.Equal(2, avisos.Count);
                    ejecutor.FallarLecturas = true; Esperar(alta, "CargarOrden", 7);
                    Assert.True(Campo<bool>(alta, "_resultadoIncierto")); Assert.False(guardar.Enabled);
                    ejecutor.FallarLecturas = false; Esperar(alta, "CargarOrden", 7);
                    Assert.False(Campo<bool>(alta, "_resultadoIncierto")); Assert.True(guardar.Enabled); Assert.Equal(2, avisos.Count); Assert.False(alta.Visible);
                    alta.MarcarResultadoIncierto(); Assert.True(Campo<bool>(alta, "_resultadoIncierto")); Assert.Equal(3, avisos.Count);
                    alta.PrepararNuevaOrden(); Assert.False(Campo<bool>(alta, "_resultadoIncierto"));
                }
            });
        }
        [Fact]
        public void ListadoIncierto_BloqueaEliminarHastaRecargaExitosa()
        {
            EnSta(() => {
                var avisos = new List<string>(); var ejecutor = new EjecutorIncierto(); int confirmaciones = 0;
                using (var listado = new Vista_OrdenesListado()) {
                    Asignar(listado, "AdvertirResultadoIncierto", new Action<string>(avisos.Add)); Asignar(listado, "ConfirmarEliminacion", new Func<string, bool>(m => { confirmaciones++; return false; }));
                    var presenter = new OrdenesPresenter(ejecutor, listado); Asignar(listado, "_presenter", presenter);
                    Asignar(listado, "_rolActual", SteeLog.Servicios.UserRole.Admin);
                    var grilla = Campo<DataGridView>(listado, "dgvOrdenes");
                    Esperar(listado, "CargarOrdenes");
                    Assert.Equal("Eliminar", grilla.Rows[0].Cells["colEliminar"].Value);
                    var clic = new DataGridViewCellEventArgs(grilla.Columns["colEliminar"].Index, 0);
                    var manejador = listado.GetType().GetMethod("DgvOrdenes_CellContentClick", Flags);
                    Assert.True(presenter.EliminarAsync(7).GetAwaiter().GetResult().Incierto);
                    Assert.Single(avisos); Assert.Equal("", grilla.Rows[0].Cells["colEliminar"].Value);
                    manejador.Invoke(listado, new object[] { grilla, clic });
                    Assert.Equal(0, confirmaciones); Assert.Equal(1, ejecutor.Escrituras); Assert.Equal(2, avisos.Count);
                    ejecutor.FallarLecturas = true; Esperar(listado, "CargarOrdenes");
                    Assert.True(Campo<bool>(listado, "_resultadoIncierto"));
                    ejecutor.FallarLecturas = false; Esperar(listado, "CargarOrdenes");
                    Assert.False(Campo<bool>(listado, "_resultadoIncierto")); Assert.Equal("Eliminar", grilla.Rows[0].Cells["colEliminar"].Value);
                    manejador.Invoke(listado, new object[] { grilla, clic });
                    Assert.Equal(1, confirmaciones); Assert.Equal(1, ejecutor.Escrituras); Assert.Equal(2, avisos.Count);
                }
            });
        }
        [Fact]
        public void RevisionIncierta_BloqueaConfirmarHastaRecargaExitosa()
        {
            EnSta(() => {
                SteeLog.Servicios.SessionManager.SetSession("admin@tesis.local");
                try {
                var avisos = new List<string>(); var ejecutor = new EjecutorIncierto();
                using (var revision = new Vista_OrdenesRevision()) {
                    Asignar(revision, "AdvertirResultadoIncierto", new Action<string>(avisos.Add));
                    var presenter = new OrdenesPresenter(ejecutor, revision); Asignar(revision, "CrearLector", new Func<IOrdenesVista, OrdenesPresenter>(v => new OrdenesPresenter(ejecutor, v)));
                    Asignar(revision, "_presenter", presenter);
                    var confirmar = Campo<Control>(revision, "btnConfirmar");
                    revision.SetOrder(7); Assert.True(Campo<EstadoModeloOrden>(revision, "_modelo").Aplicar(7)); Asignar(revision, "_permiteRecepcion", true);
                    revision.SetOcupada(false); Assert.True(confirmar.Enabled);
                    Assert.True(presenter.RecibirAsync(7, new Dictionary<int, decimal>(), new Dictionary<int, string>(), "").GetAwaiter().GetResult().Incierto);
                    Assert.Single(avisos); Assert.False(confirmar.Enabled); Assert.Equal(1, ejecutor.Escrituras);
                    revision.SetOcupada(false); Assert.False(confirmar.Enabled);
                    revision.GetType().GetMethod("BtnConfirmar_Click", Flags).Invoke(revision, new object[] { confirmar, EventArgs.Empty });
                    Assert.Equal(1, ejecutor.Escrituras);
                    ejecutor.FallarLecturas = true; Esperar(revision, "CargarOrden");
                    Assert.True(Campo<bool>(revision, "_resultadoIncierto")); Assert.False(confirmar.Enabled);
                    ejecutor.FallarLecturas = false; Esperar(revision, "CargarOrden");
                    Assert.False(Campo<bool>(revision, "_resultadoIncierto")); Assert.True(confirmar.Enabled); Assert.Single(avisos); Assert.False(revision.Visible);
                }
                } finally { SteeLog.Servicios.SessionManager.Clear(); }
            });
        }
        [Fact]
        public void RevisionCargada_TituloIncluyeNumeroYLabelEntraEnElAncho()
        {
            EnSta(() => {
                SteeLog.Servicios.SessionManager.SetSession("admin@tesis.local");
                try {
                var ejecutor = new EjecutorIncierto();
                using (var revision = new Vista_OrdenesRevision()) {
                    Asignar(revision, "CrearLector", new Func<IOrdenesVista, OrdenesPresenter>(v => new OrdenesPresenter(ejecutor, v)));
                    revision.Size = new System.Drawing.Size(1000, 700);
                    revision.SetOrder(7); Esperar(revision, "CargarOrden");
                    revision.PerformLayout();
                    var titulo = Campo<Label>(revision, "lblTitulo");
                    Assert.Equal("Revisión de Orden OC-7", titulo.Text);
                    var ancho = TextRenderer.MeasureText(titulo.Text, titulo.Font, new System.Drawing.Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding).Width;
                    Assert.True(ancho <= titulo.Width, "ancho texto " + ancho + " > label " + titulo.Width);
                }
                } finally { SteeLog.Servicios.SessionManager.Clear(); }
            });
        }
        [Fact]
        public async Task DeshabilitaMientrasCargaYRechazaDobleEnvio()
        {
            var vista = new Vista(); var executor = new Ejecutor(); var presenter = new OrdenesPresenter(executor, vista);
            var primero = presenter.EliminarAsync(5);
            var segundo = await presenter.EliminarAsync(5);
            Assert.False(segundo.Exitoso); Assert.Equal(1, executor.Llamadas); Assert.Equal(new[] { true }, vista.Estados);
            executor.Pendiente.SetResult(true);
            Assert.True((await primero).Exitoso); Assert.Equal(new[] { true, false }, vista.Estados);
        }
        [Fact]
        public async Task ErrorSeMuestraYDesbloqueaLaVista()
        {
            var vista = new Vista(); var executor = new Ejecutor(); var presenter = new OrdenesPresenter(executor, vista);
            var resultado = presenter.EliminarAsync(5); var error = new InvalidOperationException("fallo");
            executor.Pendiente.SetException(error);
            Assert.False((await resultado).Exitoso); Assert.Same(error, vista.Error); Assert.Equal(new[] { true, false }, vista.Estados);
        }
    }
}
