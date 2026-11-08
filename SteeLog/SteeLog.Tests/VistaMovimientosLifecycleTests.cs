using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using SteeLog.Core.Application.Borradores;
using SteeLog.Infrastructure.Files;
using SteeLog.Core.Application.Movimientos;
using SteeLog.Core.Application.Inventario;
using SteeLog.Modelos;
using SteeLog.Presentation;
using SteeLog.Vistas.Movimientos;
using Xunit;

namespace SteeLog.Tests
{
    public class VistaMovimientosLifecycleTests
    {
        private const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
        private sealed class Ejecutor : IUseCaseExecutor
        {
            public readonly TaskCompletionSource<int> Guardado=new TaskCompletionSource<int>();
            public int Llamadas;
            public async Task<TResult> ExecuteAsync<TUseCase,TResult>(Func<TUseCase,Task<TResult>> action) where TUseCase:class
            { Llamadas++; return (TResult)(object)await Guardado.Task; }
        }
        private sealed class EjecutorEdicion : IUseCaseExecutor
        {
            public readonly TaskCompletionSource<MovimientoEdicion> A = new TaskCompletionSource<MovimientoEdicion>();
            public readonly TaskCompletionSource<MovimientoEdicion> B = new TaskCompletionSource<MovimientoEdicion>();
            public int Llamadas;
            public async Task<TResult> ExecuteAsync<TUseCase,TResult>(Func<TUseCase,Task<TResult>> action) where TUseCase:class
            { var pendiente = ++Llamadas == 1 ? A : B; return (TResult)(object)await pendiente.Task; }
        }
        private sealed class EjecutorGuardadoYRefresco : IUseCaseExecutor
        {
            public readonly TaskCompletionSource<int> Guardado = new TaskCompletionSource<int>();
            public readonly TaskCompletionSource<IReadOnlyList<MaterialInventarioLectura>> Materiales = new TaskCompletionSource<IReadOnlyList<MaterialInventarioLectura>>();
            public int Guardados;
            public async Task<TResult> ExecuteAsync<TUseCase,TResult>(Func<TUseCase,Task<TResult>> action) where TUseCase : class
            {
                if (typeof(TResult) == typeof(int)) { Guardados++; return (TResult)(object)await Guardado.Task; }
                if (typeof(TResult) == typeof(IReadOnlyList<MaterialInventarioLectura>)) return (TResult)(object)await Materiales.Task;
                throw new InvalidOperationException("Caso inesperado en test.");
            }
        }
        private sealed class EjecutorRefresco : IUseCaseExecutor
        {
            public readonly TaskCompletionSource<CatalogosMovimiento> Catalogos = new TaskCompletionSource<CatalogosMovimiento>();
            public readonly TaskCompletionSource<IReadOnlyList<MaterialInventarioLectura>> Materiales = new TaskCompletionSource<IReadOnlyList<MaterialInventarioLectura>>();
            public readonly TaskCompletionSource<IReadOnlyList<CategoriaMaterialLectura>> Categorias = new TaskCompletionSource<IReadOnlyList<CategoriaMaterialLectura>>();
            public readonly TaskCompletionSource<IReadOnlyList<RecorteLectura>> Recortes = new TaskCompletionSource<IReadOnlyList<RecorteLectura>>();
            public readonly TaskCompletionSource<MovimientoEdicion> Edicion = new TaskCompletionSource<MovimientoEdicion>();
            public int Ediciones;
            public async Task<TResult> ExecuteAsync<TUseCase,TResult>(Func<TUseCase,Task<TResult>> action) where TUseCase:class
            {
                object resultado;
                if (typeof(TResult)==typeof(CatalogosMovimiento)) resultado=await Catalogos.Task;
                else if (typeof(TResult)==typeof(IReadOnlyList<MaterialInventarioLectura>)) resultado=await Materiales.Task;
                else if (typeof(TResult)==typeof(IReadOnlyList<CategoriaMaterialLectura>)) resultado=await Categorias.Task;
                else if (typeof(TResult)==typeof(IReadOnlyList<RecorteLectura>)) resultado=await Recortes.Task;
                else if (typeof(TResult)==typeof(MovimientoEdicion)) { Ediciones++; resultado=await Edicion.Task; }
                else throw new InvalidOperationException("Caso inesperado en test.");
                return (TResult)resultado;
            }
        }
        private static T Campo<T>(object target,string nombre) => (T)target.GetType().GetField(nombre,Flags).GetValue(target);
        private static void Fijar(object target,string nombre,object value) => target.GetType().GetField(nombre,Flags).SetValue(target,value);
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void CierreAntesDeConfirmacionRetiraSoloElBorradorOriginal(bool crearBorradorNuevo)
        {
            Exception error = null;
            var directorio = Path.Combine(Path.GetTempPath(), "steelog_draft_lifecycle_" + Guid.NewGuid().ToString("N"));
            var hilo = new Thread(() => {
                try
                {
                    var servicio = new BorradorMovimiento(new LocalDraftStore(Path.Combine(directorio,"draft.json")), () => new DateTime(2026,1,2));
                    servicio.Guardar(new MovimientoDraft { NroRemito = "original" });
                    var versionOriginal = servicio.VersionActual();
                    var vista = new Vista_Movimientos();
                    var handle = vista.Handle;
                    var executor = new Ejecutor();
                    Fijar(vista,"presenter",new MovimientosPresenter(executor,vista));
                    Fijar(vista,"catalogosListos",true); Fijar(vista,"modeloListo",true);
                    Fijar(vista,"ConfirmarGuardado",new Func<DialogResult>(() => DialogResult.Yes));
                    Fijar(vista,"VersionBorrador",new Func<string>(servicio.VersionActual));
                    Fijar(vista,"GuardarBorradorActual",new Action<MovimientoDraft>(servicio.Guardar));
                    int callbacks = 0;
                    Fijar(vista,"EliminarBorradorConfirmado",new Action<string>(version => { callbacks++; servicio.EliminarSiCoincide(version); }));
                    var tipos=new DataTable(); tipos.Columns.Add("id_tipomovimiento",typeof(int)); tipos.Columns.Add("nombre",typeof(string)); tipos.Rows.Add(4,"Salida");
                    var tipo=Campo<ComboBox>(vista,"cmbMovimientos"); tipo.DataSource=tipos; tipo.DisplayMember="nombre"; tipo.ValueMember="id_tipomovimiento"; tipo.SelectedIndex=0;
                    var origen=Campo<ComboBox>(vista,"cmbOrigen"); origen.DataSource=null; origen.Items.Add(new Vista_Movimientos.ItemCombo { IdReal=1,Tipo="UBICACION",TextoDisplay="Galpón" }); origen.SelectedIndex=0;
                    var destino=Campo<ComboBox>(vista,"cmbDestino"); destino.DataSource=null; destino.Items.Add(new Vista_Movimientos.ItemCombo { IdReal=2,Tipo="UBICACION",TextoDisplay="Depósito" }); destino.SelectedIndex=0;
                    Campo<Control>(vista,"nroRemitoTxt").Text="123";
                    Campo<List<MovimientoMaterial>>(vista,"materialesLista").Add(new MovimientoMaterial { id_material=1,NombreMaterial="Material",cantidad=1 });
                    vista.GetType().GetMethod("GuardarMovimiento_Click",Flags).Invoke(vista,new object[]{vista,EventArgs.Empty});
                    Assert.True(Campo<bool>(vista,"envioPendiente"));
                    // Simula la liberación del presenter antes de la continuación del guardado.
                    Fijar(vista,"ocupada",false);
                    vista.GetType().GetMethod("GuardarMovimiento_Click",Flags).Invoke(vista,new object[]{vista,EventArgs.Empty});
                    Assert.Equal(1,executor.Llamadas);
                    vista.GetType().GetMethod("GuardarBorrador",Flags).Invoke(vista,null);
                    Assert.Equal(versionOriginal, servicio.VersionActual());
                    vista.Dispose();
                    if (crearBorradorNuevo) servicio.Guardar(new MovimientoDraft { NroRemito = "nuevo" });
                    executor.Guardado.SetResult(91);
                    var reloj=Stopwatch.StartNew();
                    while(callbacks == 0 && reloj.Elapsed<TimeSpan.FromSeconds(5)) { Application.DoEvents(); Thread.Sleep(5); }
                    Assert.Equal(1,callbacks);
                    if (crearBorradorNuevo) Assert.Equal("nuevo", servicio.Leer().NroRemito);
                    else Assert.Null(servicio.Leer());
                }
                catch(Exception e) { error=e; }
            });
            hilo.SetApartmentState(ApartmentState.STA); hilo.Start(); Assert.True(hilo.Join(TimeSpan.FromSeconds(15)));
            try { Assert.Null(error); }
            finally { if (Directory.Exists(directorio)) Directory.Delete(directorio, true); }
        }
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GuardadoConfirmadoRetiraCarritoAntesDeEsperarRefresco(bool fallaEliminarBorrador)
        {
            Exception error = null;
            var hilo = new Thread(() => {
                try
                {
                    using (var vista = new Vista_Movimientos())
                    {
                        var handle = vista.Handle;
                        var executor = new EjecutorGuardadoYRefresco();
                        Fijar(vista,"presenter",new MovimientosPresenter(executor,vista));
                        Fijar(vista,"catalogosListos",true); Fijar(vista,"modeloListo",true);
                        Fijar(vista,"ConfirmarGuardado",new Func<DialogResult>(() => DialogResult.Yes));
                        Fijar(vista,"NotificarGuardado",new Action<string>(_ => {}));
                        var avisos = new List<string>();
                        Fijar(vista,"AdvertirGuardado",new Action<string>(mensaje => avisos.Add(mensaje)));
                        Fijar(vista,"VersionBorrador",new Func<string>(() => null));
                        Fijar(vista,"EliminarBorradorConfirmado",new Action<string>(_ => { if (fallaEliminarBorrador) throw new IOException("archivo bloqueado"); }));
                        var tipos=new DataTable(); tipos.Columns.Add("id_tipomovimiento",typeof(int)); tipos.Columns.Add("nombre",typeof(string)); tipos.Rows.Add(4,"Salida");
                        var tipo=Campo<ComboBox>(vista,"cmbMovimientos"); tipo.DataSource=tipos; tipo.DisplayMember="nombre"; tipo.ValueMember="id_tipomovimiento"; tipo.SelectedIndex=0;
                        var origen=Campo<ComboBox>(vista,"cmbOrigen"); origen.DataSource=null; origen.Items.Add(new Vista_Movimientos.ItemCombo { IdReal=1,Tipo="UBICACION",TextoDisplay="Galpón" }); origen.SelectedIndex=0;
                        var destino=Campo<ComboBox>(vista,"cmbDestino"); destino.DataSource=null; destino.Items.Add(new Vista_Movimientos.ItemCombo { IdReal=2,Tipo="UBICACION",TextoDisplay="Depósito" }); destino.SelectedIndex=0;
                        Campo<Control>(vista,"nroRemitoTxt").Text="123";
                        Campo<List<MovimientoMaterial>>(vista,"materialesLista").Add(new MovimientoMaterial { id_material=1,NombreMaterial="Material",cantidad=1 });
                        var click = vista.GetType().GetMethod("GuardarMovimiento_Click",Flags);
                        click.Invoke(vista,new object[]{vista,EventArgs.Empty});
                        executor.Guardado.SetResult(91);
                        var reloj=Stopwatch.StartNew();
                        while(Campo<List<MovimientoMaterial>>(vista,"materialesLista").Count != 0 && reloj.Elapsed<TimeSpan.FromSeconds(5)) { Application.DoEvents(); Thread.Sleep(5); }
                        Assert.Empty(Campo<List<MovimientoMaterial>>(vista,"materialesLista"));
                        Assert.False(executor.Materiales.Task.IsCompleted);
                        if (fallaEliminarBorrador) Assert.Contains(avisos, mensaje => mensaje.Contains("movimiento se guardó") && mensaje.Contains("borrador"));
                        else Assert.Empty(avisos);
                        click.Invoke(vista,new object[]{vista,EventArgs.Empty});
                        Assert.Equal(1,executor.Guardados);
                        vista.Dispose();
                        executor.Materiales.SetResult(new List<MaterialInventarioLectura>());
                        for(int i=0;i<10;i++) { Application.DoEvents(); Thread.Sleep(2); }
                    }
                }
                catch(Exception e) { error=e; }
            });
            hilo.SetApartmentState(ApartmentState.STA); hilo.Start(); Assert.True(hilo.Join(TimeSpan.FromSeconds(15))); Assert.Null(error);
        }
        [Fact]
        public void GuardadoAnteriorNoLimpiaElMovimientoRecienSeleccionado()
        {
            Exception error=null;
            var hilo=new Thread(() => {
                try
                {
                    using(var vista=new Vista_Movimientos())
                    {
                        var handle=vista.Handle;
                        var executor=new Ejecutor();
                        Fijar(vista,"presenter",new MovimientosPresenter(executor,vista));
                        Fijar(vista,"catalogosListos",true); Fijar(vista,"modeloListo",true);
                        Fijar(vista,"ConfirmarGuardado",new Func<DialogResult>(()=>DialogResult.Yes));
                        Fijar(vista,"VersionBorrador",new Func<string>(() => null));
                        Fijar(vista,"EliminarBorradorConfirmado",new Action<string>(_ => {}));
                        var tipos=new DataTable(); tipos.Columns.Add("id_tipomovimiento",typeof(int)); tipos.Columns.Add("nombre",typeof(string)); tipos.Rows.Add(4,"Salida");
                        var cmbTipo=Campo<ComboBox>(vista,"cmbMovimientos"); cmbTipo.DataSource=tipos; cmbTipo.DisplayMember="nombre"; cmbTipo.ValueMember="id_tipomovimiento"; cmbTipo.SelectedIndex=0;
                        var origen=Campo<ComboBox>(vista,"cmbOrigen"); origen.DataSource=null; origen.Items.Add(new Vista_Movimientos.ItemCombo { IdReal=1,Tipo="UBICACION",TextoDisplay="Galpón" }); origen.SelectedIndex=0;
                        var destino=Campo<ComboBox>(vista,"cmbDestino"); destino.DataSource=null; destino.Items.Add(new Vista_Movimientos.ItemCombo { IdReal=2,Tipo="UBICACION",TextoDisplay="Depósito" }); destino.SelectedIndex=0;
                        Campo<Control>(vista,"nroRemitoTxt").Text="123";
                        Campo<List<MovimientoMaterial>>(vista,"materialesLista").Add(new MovimientoMaterial { id_material=1,NombreMaterial="Material",cantidad=1 });
                        vista.GetType().GetMethod("GuardarMovimiento_Click",Flags).Invoke(vista,new object[]{vista,EventArgs.Empty});
                        Assert.Equal(1,executor.Llamadas); Assert.True(Campo<bool>(vista,"ocupada"));
                        Fijar(vista,"catalogosListos",false);
                        vista.SetEditMode(2);
                        executor.Guardado.SetResult(91);
                        var reloj=Stopwatch.StartNew();
                        while(Campo<bool>(vista,"ocupada") && reloj.Elapsed<TimeSpan.FromSeconds(5)) { Application.DoEvents(); Thread.Sleep(5); }
                        Assert.False(Campo<bool>(vista,"ocupada"));
                        Assert.Equal(2,Campo<int>(vista,"_idMovimientoAEditar"));
                        Assert.Equal(1,executor.Llamadas);
                    }
                }
                catch(Exception e) { error=e; }
            });
            hilo.SetApartmentState(ApartmentState.STA); hilo.Start(); Assert.True(hilo.Join(TimeSpan.FromSeconds(15))); Assert.Null(error);
        }
        [Fact]
        public void VistaRealAplicaSoloLaUltimaCargaDeEdicion()
        {
            Exception error=null;
            var hilo=new Thread(() => {
                try
                {
                    using(var vista=new Vista_Movimientos())
                    {
                        var handle=vista.Handle;
                        var executor=new EjecutorEdicion();
                        Fijar(vista,"presenter",new MovimientosPresenter(executor,vista));
                        Fijar(vista,"catalogosListos",true);
                        Campo<ComboBox>(vista,"cmbFlete").Items.Add("-");
                        vista.SetEditMode(1); vista.SetEditMode(2);
                        Assert.Equal(2,executor.Llamadas);
                        executor.A.SetResult(new MovimientoEdicion { Cabecera=new CabeceraMovimiento { Fecha=DateTime.Today,Remito="ANTERIOR" } });
                        for(int i=0;i<20;i++) { Application.DoEvents(); Thread.Sleep(2); }
                        Assert.False(Campo<bool>(vista,"modeloListo"));
                        executor.B.SetResult(new MovimientoEdicion { Cabecera=new CabeceraMovimiento { Fecha=DateTime.Today,Remito="VIGENTE" } });
                        var reloj=Stopwatch.StartNew();
                        while(!Campo<bool>(vista,"modeloListo") && reloj.Elapsed<TimeSpan.FromSeconds(5)) { Application.DoEvents(); Thread.Sleep(5); }
                        Assert.True(Campo<bool>(vista,"modeloListo"));
                        Assert.Equal(2,Campo<int>(vista,"_idMovimientoAEditar"));
                        Assert.Equal("VIGENTE",Campo<Control>(vista,"nroRemitoTxt").Text);
                    }
                }
                catch(Exception e) { error=e; }
            });
            hilo.SetApartmentState(ApartmentState.STA); hilo.Start(); Assert.True(hilo.Join(TimeSpan.FromSeconds(15))); Assert.Null(error);
        }
        [Fact]
        public void DisposeDeVistaNoMostradaCancelaCargaEnCurso()
        {
            Exception error=null;
            var hilo=new Thread(() => {
                try
                {
                    var vista=new Vista_Movimientos();
                    var handle=vista.Handle;
                    var executor=new EjecutorEdicion();
                    Fijar(vista,"presenter",new MovimientosPresenter(executor,vista));
                    Fijar(vista,"catalogosListos",true);
                    vista.SetEditMode(8);
                    Assert.Equal(1,executor.Llamadas);
                    var carga=Campo<CancellationTokenSource>(vista,"cargaEdicion");
                    vista.Dispose();
                    Assert.True(carga.IsCancellationRequested);
                    Assert.True(Campo<CancellationTokenSource>(vista,"cierre").IsCancellationRequested);
                    executor.A.SetResult(new MovimientoEdicion { Cabecera=new CabeceraMovimiento { Fecha=DateTime.Today,Remito="TARDE" } });
                    for(int j=0;j<20;j++) { Application.DoEvents(); Thread.Sleep(2); }
                    Assert.False(Campo<bool>(vista,"modeloListo"));
                }
                catch(Exception e) { error=e; }
            });
            hilo.SetApartmentState(ApartmentState.STA); hilo.Start(); Assert.True(hilo.Join(TimeSpan.FromSeconds(15))); Assert.Null(error);
        }
        [Fact]
        public void F5PendienteReanudaElIdSeleccionadoDuranteLaCarga()
        {
            Exception error=null;
            var hilo=new Thread(() => {
                try
                {
                    using(var vista=new Vista_Movimientos())
                    {
                        var handle=vista.Handle;
                        var executor=new EjecutorRefresco();
                        Fijar(vista,"presenter",new MovimientosPresenter(executor,vista));
                        Fijar(vista,"catalogosListos",true);
                        vista.GetType().GetMethod("RefrescarDatos",Flags).Invoke(vista,null);
                        vista.SetEditMode(12);
                        Assert.False(Campo<bool>(vista,"modeloListo")); Assert.Equal(0,executor.Ediciones);
                        executor.Catalogos.SetResult(new CatalogosMovimiento());
                        executor.Materiales.SetResult(new List<MaterialInventarioLectura>());
                        executor.Categorias.SetResult(new List<CategoriaMaterialLectura>());
                        executor.Recortes.SetResult(new List<RecorteLectura>());
                        var reloj=Stopwatch.StartNew();
                        while(executor.Ediciones==0 && reloj.Elapsed<TimeSpan.FromSeconds(5)) { Application.DoEvents(); Thread.Sleep(5); }
                        Assert.Equal(1,executor.Ediciones);
                        executor.Edicion.SetResult(new MovimientoEdicion { Cabecera=new CabeceraMovimiento { Fecha=DateTime.Today,Remito="RETOMADO" } });
                        reloj.Restart();
                        while(!Campo<bool>(vista,"modeloListo") && reloj.Elapsed<TimeSpan.FromSeconds(5)) { Application.DoEvents(); Thread.Sleep(5); }
                        Assert.True(Campo<bool>(vista,"modeloListo"));
                        Assert.Equal(12,Campo<int>(vista,"_idMovimientoAEditar"));
                        Assert.Equal("RETOMADO",Campo<Control>(vista,"nroRemitoTxt").Text);
                    }
                }
                catch(Exception e) { error=e; }
            });
            hilo.SetApartmentState(ApartmentState.STA); hilo.Start(); Assert.True(hilo.Join(TimeSpan.FromSeconds(15))); Assert.Null(error);
        }
    }
}
