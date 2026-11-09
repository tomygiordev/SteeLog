using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.Core.Application.Activos;
using SteeLog.Core.Application.Common;
using SteeLog.Core.Application.Identidad;
using SteeLog.Presentation;
using Xunit;

namespace SteeLog.Tests
{
    public class InsumosPresenterTests
    {
        private sealed class Lecturas : IInsumosConsulta
        {
            public int Llamadas;
            public CancellationToken Token;
            public Task<IReadOnlyList<InsumoLectura>> InsumosAsync(CancellationToken token = default) { Llamadas++; Token = token; return Task.FromResult<IReadOnlyList<InsumoLectura>>(new[] { new InsumoLectura { Id = 4, Nombre = "Taladro", Obra = "Galpón" } }); }
            public Task<IReadOnlyList<ObraInsumoLectura>> ObrasAsync(CancellationToken token = default) { Llamadas++; return Task.FromResult<IReadOnlyList<ObraInsumoLectura>>(Array.Empty<ObraInsumoLectura>()); }
            public Task<IReadOnlyList<HistorialInsumoLectura>> HistorialAsync(CancellationToken token = default) { Llamadas++; return Task.FromResult<IReadOnlyList<HistorialInsumoLectura>>(Array.Empty<HistorialInsumoLectura>()); }
        }
        private sealed class Ejecutor : IUseCaseExecutor
        {
            private readonly ConsultarInsumos consulta;
            public Ejecutor(ConsultarInsumos consulta) { this.consulta = consulta; }
            public Task<T> ExecuteAsync<U,T>(Func<U,Task<T>> operacion) where U : class => operacion((U)(object)consulta);
        }
        private sealed class Vista : IInsumosVista, IInsumosResultadoIncierto
        {
            public readonly List<bool> Estados = new List<bool>();
            public void SetOcupada(bool valor) { Estados.Add(valor); }
            public void MostrarError(Exception error) { }
            public int Inciertos;
            public void MarcarResultadoIncierto() { Inciertos++; }
        }
        private sealed class EjecutorPendiente : IUseCaseExecutor
        {
            public int Llamadas;
            public readonly TaskCompletionSource<int> Pendiente = new TaskCompletionSource<int>();
            public async Task<T> ExecuteAsync<U,T>(Func<U,Task<T>> operacion) where U : class
            { Llamadas++; return (T)(object)await Pendiente.Task; }
        }
        [Fact]
        public void OpcionesIdentificanHomonimosSinModificarNombresUnicos()
        {
            var opciones = InsumosPresenter.OpcionesFiltro(new[] {
                new InsumoLectura { Id = 1, Nombre = "Disco" },
                new InsumoLectura { Id = 2, Nombre = "Disco" },
                new InsumoLectura { Id = 3, Nombre = "Broca" }
            });
            Assert.Equal("Disco (#1)", opciones[0].Etiqueta);
            Assert.Equal("Disco (#2)", opciones[1].Etiqueta);
            Assert.Equal("Broca", opciones[2].Etiqueta);
            Assert.Equal(2, opciones[1].Id);
        }
        [Fact]
        public void FiltroDeHistorialDistingueInsumosConMismoNombre()
        {
            var datos = new[] {
                new HistorialInsumoLectura { Id = 11, InsumoId = 1, Insumo = "Disco" },
                new HistorialInsumoLectura { Id = 12, InsumoId = 2, Insumo = "Disco" }
            };
            Assert.Equal(11, Assert.Single(InsumosPresenter.FiltrarHistorial(datos, 1)).Id);
            Assert.Equal(12, Assert.Single(InsumosPresenter.FiltrarHistorial(datos, 2)).Id);
            Assert.Equal(2, InsumosPresenter.FiltrarHistorial(datos, null).Count);
            Assert.Empty(InsumosPresenter.FiltrarHistorial(datos, 3));
        }
        [Fact]
        public async Task TodasLasLecturasExigenSesionAntesDeAccederAlPuerto()
        {
            var session = new Session(); var repo = new Lecturas(); var caso = new ConsultarInsumos(session, repo);
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Task.Run(async () => await caso.InsumosAsync()));
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Task.Run(async () => await caso.ObrasAsync()));
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Task.Run(async () => await caso.HistorialAsync()));
            Assert.Equal(0, repo.Llamadas);
            session.Start(new Identity("deposito@test", Role.Galpon));
            var presenter = new InsumosPresenter(new Ejecutor(caso), new Vista());
            using (var cierre = new CancellationTokenSource())
            {
                Assert.Equal("Taladro", Assert.Single(await presenter.InsumosAsync(cierre.Token)).Nombre);
                Assert.Equal(cierre.Token, repo.Token);
            }
            Assert.Empty(await presenter.ObrasAsync());
            Assert.Empty(await presenter.HistorialAsync());
            Assert.Equal(3, repo.Llamadas);
        }
        [Fact]
        public async Task CargasFueraDeOrdenYTrasCierreNoAplicanDatosNiErrores()
        {
            var cargas = new CargasInsumos();
            int a = cargas.Iniciar();
            var pendienteA = new TaskCompletionSource<string>();
            var aplicacionA = Aplicar(cargas, a, pendienteA.Task);
            int b = cargas.Iniciar();
            var pendienteB = new TaskCompletionSource<string>();
            var aplicacionB = Aplicar(cargas, b, pendienteB.Task);
            pendienteB.SetResult("B");
            Assert.Equal("B", await aplicacionB);
            pendienteA.SetResult("A");
            Assert.Null(await aplicacionA);
            int c = cargas.Iniciar();
            var pendienteC = new TaskCompletionSource<string>();
            var aplicacionC = Aplicar(cargas, c, pendienteC.Task);
            cargas.Cerrar();
            pendienteC.SetException(new InvalidOperationException("obsoleto"));
            Assert.Null(await aplicacionC);
            Assert.False(cargas.EsActual(cargas.Iniciar()));
        }
        private static async Task<string> Aplicar(CargasInsumos cargas, int revision, Task<string> tarea)
        {
            try { var valor = await tarea; return cargas.EsActual(revision) ? valor : null; }
            catch { if (cargas.EsActual(revision)) throw; return null; }
        }
        private sealed class EjecutorIncierto : IUseCaseExecutor
        {
            public int Escrituras, Lecturas;
            public Task<T> ExecuteAsync<U, T>(Func<U, Task<T>> operacion) where U : class
            {
                if (typeof(T) == typeof(int)) { Escrituras++; throw new ResultadoEscrituraInciertoException(new Exception("respuesta perdida")); }
                if (typeof(T) == typeof(IReadOnlyList<InsumoLectura>)) { Lecturas++; return Task.FromResult((T)(object)Array.Empty<InsumoLectura>()); }
                throw new InvalidOperationException();
            }
        }
        [Fact]
        public void ResultadoInciertoBloqueaEscrituraHastaRecargaExitosaSinMostrarVentana()
        {
            Exception error = null;
            var hilo = new Thread(() => {
                try {
                    var tipo = Assembly.Load("SteeLog").GetType("SteeLog.Vistas.Vista_Insumos", true);
                    const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                    using (var vista = (Form)Activator.CreateInstance(tipo, new ServiciosVistaDePrueba())) {
                        var avisos = new List<string>();
                        tipo.GetField("AdvertirResultadoIncierto", flags).SetValue(vista, new Action<string>(avisos.Add));
                        var ejecutor = new EjecutorIncierto(); var presenter = new InsumosPresenter(ejecutor, (IInsumosVista)vista);
                        tipo.GetField("presenter", flags).SetValue(vista, presenter);
                        Assert.Null(presenter.GuardarAsync(null, "Disco", null, false, "").GetAwaiter().GetResult());
                        Assert.Single(avisos); Assert.True((bool)tipo.GetField("resultadoIncierto", flags).GetValue(vista));
                        Assert.False(((Control)tipo.GetField("btnGuardar", flags).GetValue(vista)).Enabled);
                        ((Task)tipo.GetMethod("CargarInsumos", flags).Invoke(vista, null)).GetAwaiter().GetResult();
                        Assert.False((bool)tipo.GetField("resultadoIncierto", flags).GetValue(vista));
                        Assert.Equal(1, ejecutor.Escrituras); Assert.Equal(1, ejecutor.Lecturas); Assert.False(vista.Visible);
                    }
                } catch (Exception ex) { error = ex; }
            });
            hilo.SetApartmentState(ApartmentState.STA); hilo.Start(); Assert.True(hilo.Join(TimeSpan.FromSeconds(15))); Assert.Null(error);
        }
        [Fact]
        public async Task EscrituraPendienteBloqueaDobleAccionYLiberaBusy()
        {
            var vista = new Vista(); var ejecutor = new EjecutorPendiente();
            var presenter = new InsumosPresenter(ejecutor, vista);
            var primero = presenter.GuardarAsync(7, "Taladro", null, false, "");
            Assert.Null(await presenter.EliminarAsync(7));
            Assert.Equal(1, ejecutor.Llamadas);
            Assert.Equal(new[] { true }, vista.Estados);
            ejecutor.Pendiente.SetResult(7);
            Assert.Equal(7, await primero);
            Assert.Equal(new[] { true, false }, vista.Estados);
        }
        [Fact]
        public void VistasDeshabilitanAccionesYDescartanSeleccionMientrasCarga()
        {
            Exception error = null;
            var hilo = new Thread(() => {
                try
                {
                    var host = Assembly.Load("SteeLog");
                    const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                    var tipo = host.GetType("SteeLog.Vistas.Vista_Insumos", true);
                    CancellationTokenSource cancelacionVista;
                    using (var vista = (Form)Activator.CreateInstance(tipo, new ServiciosVistaDePrueba()))
                    {
                        cancelacionVista = (CancellationTokenSource)tipo.GetField("cierre", flags).GetValue(vista);
                        tipo.GetField("idSeleccionado", flags).SetValue(vista, 7);
                        tipo.GetMethod("CambiarLectura", flags).Invoke(vista, new object[] { true });
                        Assert.False(((Control)tipo.GetField("btnGuardar", flags).GetValue(vista)).Enabled);
                        Assert.False(((Control)tipo.GetField("btnEliminar", flags).GetValue(vista)).Enabled);
                        tipo.GetMethod("LimpiarSeleccion", flags).Invoke(vista, null);
                        Assert.Equal(-1, (int)tipo.GetField("idSeleccionado", flags).GetValue(vista));
                    }
                    Assert.True(cancelacionVista.IsCancellationRequested);
                    var tipoModal = host.GetType("SteeLog.Vistas.Movimientos.ModalInsumos", true);
                    CancellationTokenSource cancelacionModal;
                    using (var modal = (Form)Activator.CreateInstance(tipoModal, new object[] { new ServiciosVistaDePrueba() }))
                    {
                        cancelacionModal = (CancellationTokenSource)tipoModal.GetField("cierre", flags).GetValue(modal);
                        Assert.False(((Control)tipoModal.GetField("btnConfirmar", flags).GetValue(modal)).Enabled);
                    }
                    Assert.True(cancelacionModal.IsCancellationRequested);
                }
                catch (Exception ex) { error = ex; }
            });
            hilo.SetApartmentState(ApartmentState.STA); hilo.Start();
            Assert.True(hilo.Join(TimeSpan.FromSeconds(15)));
            Assert.Null(error);
        }
    }
}
