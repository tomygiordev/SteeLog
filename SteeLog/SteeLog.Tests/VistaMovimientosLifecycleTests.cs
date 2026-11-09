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
        private sealed class EjecutorIncierto : IUseCaseExecutor
        {
            public int Guardados; public bool FallaCatalogos;
            public Task<TResult> ExecuteAsync<TUseCase,TResult>(Func<TUseCase,Task<TResult>> action) where TUseCase:class
            {
                if (typeof(TResult)==typeof(int)) { Guardados++; throw new SteeLog.Core.Application.Common.ResultadoEscrituraInciertoException(new IOException("commit")); }
                if (typeof(TResult)==typeof(CatalogosMovimiento)) { if (FallaCatalogos) throw new IOException("sin red"); return Task.FromResult((TResult)(object)new CatalogosMovimiento()); }
                if (typeof(TResult)==typeof(IReadOnlyList<MaterialInventarioLectura>)) return Task.FromResult((TResult)(object)new List<MaterialInventarioLectura>());
                if (typeof(TResult)==typeof(IReadOnlyList<CategoriaMaterialLectura>)) return Task.FromResult((TResult)(object)new List<CategoriaMaterialLectura>());
                if (typeof(TResult)==typeof(IReadOnlyList<RecorteLectura>)) return Task.FromResult((TResult)(object)new List<RecorteLectura>());
                throw new InvalidOperationException("Caso inesperado en test.");
            }
        }
        private static void Esperar(Func<bool> condicion)
        {
            var reloj=Stopwatch.StartNew();
            while(!condicion() && reloj.Elapsed<TimeSpan.FromSeconds(5)) { Application.DoEvents(); Thread.Sleep(5); }
        }
        [Fact]
        public void ResultadoInciertoBloqueaReenvioConservaDatosYSoloUnaRecargaExitosaLoLibera()
        {
            Exception error=null;
            var hilo=new Thread(() => {
                try
                {
                    using(var vista=new Vista_Movimientos(new ServiciosVistaDePrueba()))
                    {
                        var handle=vista.Handle;
                        var executor=new EjecutorIncierto();
                        Fijar(vista,"presenter",new MovimientosPresenter(executor,vista));
                        Fijar(vista,"catalogosListos",true); Fijar(vista,"modeloListo",true);
                        Fijar(vista,"ConfirmarGuardado",new Func<DialogResult>(() => DialogResult.Yes));
                        Fijar(vista,"VersionBorrador",new Func<string>(() => null));
                        int borradoresRetirados=0;
                        var persistidos=new List<MovimientoDraft>();
                        Fijar(vista,"GuardarBorradorActual",new Action<MovimientoDraft>(d => persistidos.Add(d)));
                        Fijar(vista,"EliminarBorradorConfirmado",new Action<string>(_ => borradoresRetirados++));
                        var avisos=new List<string>(); var notificaciones=new List<string>(); var errores=new List<string>();
                        Fijar(vista,"AdvertirResultadoIncierto",new Action<string>(m => avisos.Add(m)));
                        Fijar(vista,"AdvertirGuardado",new Action<string>(m => {}));
                        Fijar(vista,"NotificarGuardado",new Action<string>(m => notificaciones.Add(m)));
                        Fijar(vista,"AdvertirErrorCarga",new Action<string>(m => errores.Add(m)));
                        var tipos=new DataTable(); tipos.Columns.Add("id_tipomovimiento",typeof(int)); tipos.Columns.Add("nombre",typeof(string)); tipos.Rows.Add(4,"Salida");
                        var tipo=Campo<ComboBox>(vista,"cmbMovimientos"); tipo.DataSource=tipos; tipo.DisplayMember="nombre"; tipo.ValueMember="id_tipomovimiento"; tipo.SelectedIndex=0;
                        var origen=Campo<ComboBox>(vista,"cmbOrigen"); origen.DataSource=null; origen.Items.Add(new Vista_Movimientos.ItemCombo { IdReal=1,Tipo="UBICACION",TextoDisplay="Galpón" }); origen.SelectedIndex=0;
                        var destino=Campo<ComboBox>(vista,"cmbDestino"); destino.DataSource=null; destino.Items.Add(new Vista_Movimientos.ItemCombo { IdReal=2,Tipo="UBICACION",TextoDisplay="Depósito" }); destino.SelectedIndex=0;
                        Campo<Control>(vista,"nroRemitoTxt").Text="123";
                        var carrito=Campo<List<MovimientoMaterial>>(vista,"materialesLista");
                        carrito.Add(new MovimientoMaterial { id_material=1,NombreMaterial="Material",cantidad=1 });
                        var boton=Campo<Control>(vista,"guna2Button2");
                        var click=vista.GetType().GetMethod("GuardarMovimiento_Click",Flags);
                        click.Invoke(vista,new object[]{vista,EventArgs.Empty});
                        Esperar(() => avisos.Count>0 && !Campo<bool>(vista,"ocupada"));
                        Assert.Equal(1,executor.Guardados); Assert.Single(avisos); Assert.Contains("Resultado incierto",avisos[0]);
                        Assert.Equal("123",Assert.Single(persistidos).ResultadoInciertoRemito); // marcador persistido una sola vez
                        Assert.False(boton.Enabled);
                        Assert.Single(carrito); Assert.Equal("123",Campo<Control>(vista,"nroRemitoTxt").Text); Assert.Equal(0,borradoresRetirados);
                        // Un segundo clic no vuelve a escribir.
                        click.Invoke(vista,new object[]{vista,EventArgs.Empty});
                        Assert.Equal(1,executor.Guardados);
                        // El autoguardado conserva el dato del usuario.
                        var borradores=new List<object>();
                        Fijar(vista,"GuardarBorradorActual",new Action<MovimientoDraft>(d => borradores.Add(d)));
                        vista.GetType().GetMethod("GuardarBorrador",Flags).Invoke(vista,null);
                        Assert.Empty(borradores); // con resultado incierto no se autoguarda hasta resolver
                        // Una recarga fallida no libera.
                        executor.FallaCatalogos=true;
                        vista.GetType().GetMethod("RefrescarDatos",Flags).Invoke(vista,null);
                        Esperar(() => errores.Count>0); Assert.Single(errores);
                        Assert.True(Campo<bool>(vista,"resultadoIncierto")); Assert.False(boton.Enabled);
                        // Una recarga exitosa explícita pregunta y, con Sí, libera conservando el carrito.
                        var preguntas=new List<string>();
                        Fijar(vista,"ConfirmarReenvioIncierto",new Func<string,bool>(r => { preguntas.Add(r); return true; }));
                        executor.FallaCatalogos=false;
                        vista.GetType().GetMethod("RefrescarDatos",Flags).Invoke(vista,null);
                        Esperar(() => notificaciones.Count>0); Assert.Single(notificaciones);
                        Assert.False(Campo<bool>(vista,"resultadoIncierto")); Assert.True(boton.Enabled);
                        Assert.Single(carrito); Assert.Single(avisos); Assert.Equal(new[]{"123"},preguntas);
                        Assert.Single(persistidos); Assert.Null(((MovimientoDraft)Assert.Single(borradores)).ResultadoInciertoRemito); // con Sí se limpia el marcador
                    }
                }
                catch(Exception e) { error=e; }
                finally { ReiniciarIncierto(); }
            });
            hilo.SetApartmentState(ApartmentState.STA); hilo.Start(); Assert.True(hilo.Join(TimeSpan.FromSeconds(20))); Assert.Null(error);
        }
        private static void Sta(Action accion)
        {
            Exception error=null;
            var hilo=new Thread(() => { try { accion(); } catch(Exception e) { error=e; } finally { ReiniciarIncierto(); } });
            hilo.SetApartmentState(ApartmentState.STA); hilo.Start(); Assert.True(hilo.Join(TimeSpan.FromSeconds(20))); Assert.Null(error);
        }
        private static void ReiniciarIncierto() => typeof(Vista_Movimientos).GetMethod("ReiniciarEstadoIncierto",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
        private static void Invocar(object vista,string metodo,params object[] args) => vista.GetType().GetMethod(metodo,Flags).Invoke(vista,args);
        private static List<MovimientoMaterial> PrepararIncierto(Vista_Movimientos vista,IUseCaseExecutor executor)
        {
            var handle=vista.Handle;
            Fijar(vista,"presenter",new MovimientosPresenter(executor,vista));
            Fijar(vista,"catalogosListos",true); Fijar(vista,"modeloListo",true);
            Fijar(vista,"VersionBorrador",new Func<string>(() => null));
            Fijar(vista,"EliminarBorradorConfirmado",new Action<string>(_ => {}));
            Fijar(vista,"GuardarBorradorActual",new Action<MovimientoDraft>(_ => {}));
            Fijar(vista,"AdvertirResultadoIncierto",new Action<string>(m => {}));
            Fijar(vista,"AdvertirGuardado",new Action<string>(m => {}));
            Fijar(vista,"NotificarGuardado",new Action<string>(m => {}));
            Fijar(vista,"AdvertirErrorCarga",new Action<string>(m => {}));
            Campo<Control>(vista,"nroRemitoTxt").Text="123";
            var carrito=Campo<List<MovimientoMaterial>>(vista,"materialesLista");
            carrito.Add(new MovimientoMaterial { id_material=1,NombreMaterial="Material",cantidad=1 });
            ReiniciarIncierto();
            vista.MarcarResultadoIncierto();
            return carrito;
        }
        [Fact]
        public void RecargaExitosaConNoLiberaYDescartaCarritoYBorrador()
        {
            Sta(() => {
                using(var vista=new Vista_Movimientos(new ServiciosVistaDePrueba()))
                {
                    var carrito=PrepararIncierto(vista,new EjecutorIncierto());
                    var preguntas=new List<string>(); var avisos=new List<string>(); var borradores=new List<string>();
                    Fijar(vista,"ConfirmarReenvioIncierto",new Func<string,bool>(r => { preguntas.Add(r); return false; }));
                    Fijar(vista,"AdvertirGuardado",new Action<string>(m => avisos.Add(m)));
                    Fijar(vista,"EliminarBorradorConfirmado",new Action<string>(v => borradores.Add(v)));
                    Invocar(vista,"RefrescarDatos");
                    Esperar(() => avisos.Count>0);
                    Assert.Equal(new[]{"123"},preguntas);
                    Assert.False(Campo<bool>(vista,"resultadoIncierto"));
                    Assert.Empty(carrito); Assert.Equal("",Campo<Control>(vista,"nroRemitoTxt").Text);
                    Assert.Single(borradores); Assert.Single(avisos);
                }
            });
        }
        [Fact]
        public void RecargaFallidaNoPreguntaYMantieneBloqueo()
        {
            Sta(() => {
                using(var vista=new Vista_Movimientos(new ServiciosVistaDePrueba()))
                {
                    var executor=new EjecutorIncierto { FallaCatalogos=true };
                    var carrito=PrepararIncierto(vista,executor);
                    int preguntas=0; var errores=new List<string>();
                    Fijar(vista,"ConfirmarReenvioIncierto",new Func<string,bool>(r => { preguntas++; return true; }));
                    Fijar(vista,"AdvertirErrorCarga",new Action<string>(m => errores.Add(m)));
                    Invocar(vista,"RefrescarDatos");
                    Esperar(() => errores.Count>0);
                    Assert.Single(errores); Assert.Equal(0,preguntas);
                    Assert.True(Campo<bool>(vista,"resultadoIncierto")); Assert.Single(carrito);
                }
            });
        }
        [Fact]
        public void RecargaIniciadaAntesDeLaMarcaNoPregunta()
        {
            Sta(() => {
                using(var vista=new Vista_Movimientos(new ServiciosVistaDePrueba()))
                {
                    var executor=new EjecutorRefresco();
                    var carrito=PrepararIncierto(vista,new EjecutorIncierto());
                    Fijar(vista,"presenter",new MovimientosPresenter(executor,vista));
                    Fijar(vista,"resultadoIncierto",false);
                    int preguntas=0; var notificaciones=new List<string>();
                    Fijar(vista,"ConfirmarReenvioIncierto",new Func<string,bool>(r => { preguntas++; return true; }));
                    Fijar(vista,"NotificarGuardado",new Action<string>(m => notificaciones.Add(m)));
                    Invocar(vista,"RefrescarDatos"); // recarga en curso
                    vista.MarcarResultadoIncierto(); // marca posterior al inicio de la recarga
                    executor.Catalogos.SetResult(new CatalogosMovimiento());
                    executor.Materiales.SetResult(new List<MaterialInventarioLectura>());
                    executor.Categorias.SetResult(new List<CategoriaMaterialLectura>());
                    executor.Recortes.SetResult(new List<RecorteLectura>());
                    Esperar(() => notificaciones.Count>0);
                    Assert.Single(notificaciones); Assert.Equal(0,preguntas);
                    Assert.True(Campo<bool>(vista,"resultadoIncierto")); Assert.Single(carrito);
                }
            });
        }
        private static Vista_Movimientos VistaNuevaConBorradorIncierto(MovimientoDraft draft,List<string> preguntas,bool respuesta,List<MovimientoDraft> escritos,List<string> retirados)
        {
            var vista=new Vista_Movimientos(new ServiciosVistaDePrueba());
            var handle=vista.Handle;
            Fijar(vista,"presenter",new MovimientosPresenter(new EjecutorIncierto(),vista));
            Fijar(vista,"catalogosListos",true); Fijar(vista,"modeloListo",true);
            Campo<ComboBox>(vista,"cmbFlete").Items.Add("Sin flete"); // la restauración selecciona el índice 0
            Fijar(vista,"LeerBorrador",new Func<MovimientoDraft>(() => draft));
            Fijar(vista,"ConfirmarRecuperacion",new Func<MovimientoDraft,bool>(_ => true));
            Fijar(vista,"VersionBorrador",new Func<string>(() => "v-marcado"));
            Fijar(vista,"EliminarBorradorConfirmado",new Action<string>(v => retirados.Add(v)));
            Fijar(vista,"GuardarBorradorActual",new Action<MovimientoDraft>(d => escritos.Add(d)));
            Fijar(vista,"AdvertirResultadoIncierto",new Action<string>(m => {}));
            Fijar(vista,"AdvertirGuardado",new Action<string>(m => {}));
            Fijar(vista,"NotificarGuardado",new Action<string>(m => {}));
            Fijar(vista,"AdvertirErrorCarga",new Action<string>(m => {}));
            Fijar(vista,"ConfirmarReenvioIncierto",new Func<string,bool>(r => { preguntas.Add(r); return respuesta; }));
            ReiniciarIncierto(); // simula un proceso nuevo: no hay estado estático
            return vista;
        }
        private static MovimientoDraft BorradorMarcado()
        {
            var draft=new MovimientoDraft { NroRemito="777",ResultadoInciertoRemito="777",NombreTipoMovimiento="Salida" };
            draft.Materiales.Add(new MovimientoMaterialDraft { IdMaterial=1,NombreMaterial="Material",Cantidad=2 });
            return draft;
        }
        [Fact]
        public void BorradorConMarcadorEnVistaNuevaBloqueaGuardarYConSiLibera()
        {
            Sta(() => {
                var preguntas=new List<string>(); var escritos=new List<MovimientoDraft>(); var retirados=new List<string>();
                using(var vista=VistaNuevaConBorradorIncierto(BorradorMarcado(),preguntas,true,escritos,retirados))
                {
                    var boton=Campo<Control>(vista,"guna2Button2");
                    Invocar(vista,"IntentarRecuperarBorrador");
                    Assert.True(Campo<bool>(vista,"resultadoIncierto")); Assert.False(boton.Enabled);
                    Esperar(() => preguntas.Count>0);
                    Assert.Equal(new[]{"777"},preguntas);
                    Assert.False(Campo<bool>(vista,"resultadoIncierto")); Assert.True(boton.Enabled);
                    Assert.Single(Campo<List<MovimientoMaterial>>(vista,"materialesLista"));
                    Assert.Empty(retirados);
                    Assert.Null(Assert.Single(escritos).ResultadoInciertoRemito); // marcador limpiado en disco
                }
            });
        }
        [Fact]
        public void BorradorConMarcadorEnVistaNuevaConNoDescartaCarritoYBorrador()
        {
            Sta(() => {
                var preguntas=new List<string>(); var escritos=new List<MovimientoDraft>(); var retirados=new List<string>();
                using(var vista=VistaNuevaConBorradorIncierto(BorradorMarcado(),preguntas,false,escritos,retirados))
                {
                    Invocar(vista,"IntentarRecuperarBorrador");
                    Assert.True(Campo<bool>(vista,"resultadoIncierto"));
                    Esperar(() => preguntas.Count>0);
                    Assert.Equal(new[]{"777"},preguntas);
                    Assert.False(Campo<bool>(vista,"resultadoIncierto"));
                    Assert.Empty(Campo<List<MovimientoMaterial>>(vista,"materialesLista"));
                    Assert.Equal(new[]{"v-marcado"},retirados); Assert.Empty(escritos);
                }
            });
        }
        [Fact]
        public void BorradorSinMarcadorNoPideConfirmacionDeIncierto()
        {
            Sta(() => {
                var preguntas=new List<string>(); var draft=BorradorMarcado(); draft.ResultadoInciertoRemito=null;
                using(var vista=VistaNuevaConBorradorIncierto(draft,preguntas,true,new List<MovimientoDraft>(),new List<string>()))
                {
                    Invocar(vista,"IntentarRecuperarBorrador");
                    for(int i=0;i<20;i++) { Application.DoEvents(); Thread.Sleep(5); }
                    Assert.False(Campo<bool>(vista,"resultadoIncierto")); Assert.Empty(preguntas);
                }
            });
        }
        [Fact]
        public void AvisoDeStockMaximoVuelveAAvisarTrasCambiarDeMaterial()
        {
            Sta(() => {
                using(var vista=new Vista_Movimientos(new ServiciosVistaDePrueba()))
                {
                    var handle=vista.Handle;
                    var avisos=new List<string>();
                    Fijar(vista,"AvisarStockMaximo",new Action<string>(m => avisos.Add(m)));
                    var tipos=new DataTable(); tipos.Columns.Add("id_tipomovimiento",typeof(int)); tipos.Columns.Add("nombre",typeof(string)); tipos.Rows.Add(4,"Salida");
                    var tipo=Campo<ComboBox>(vista,"cmbMovimientos"); tipo.DataSource=tipos; tipo.DisplayMember="nombre"; tipo.ValueMember="id_tipomovimiento"; tipo.SelectedIndex=0;
                    var destino=Campo<ComboBox>(vista,"cmbDestino"); destino.DataSource=null; destino.Items.Add(new Vista_Movimientos.ItemCombo { IdReal=2,Tipo="UBICACION",TextoDisplay="Depósito" }); destino.SelectedIndex=0;
                    Fijar(vista,"_materialSeleccionado",new Material { id=1,nombre="M",stockActual=5 });
                    var cant=Campo<Control>(vista,"cantMatMov");
                    cant.Text="9"; Assert.Single(avisos);
                    cant.Text="9"; Assert.Single(avisos); // mismo material y tope: sin repetir
                    Fijar(vista,"_materialSeleccionado",new Material { id=2,nombre="N",stockActual=5 });
                    cant.Text="8"; Assert.Equal(2,avisos.Count); // otro material con el mismo tope vuelve a avisar
                }
            });
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
                    var vista = new Vista_Movimientos(new ServiciosVistaDePrueba());
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
                    using (var vista = new Vista_Movimientos(new ServiciosVistaDePrueba()))
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
                    using(var vista=new Vista_Movimientos(new ServiciosVistaDePrueba()))
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
                    using(var vista=new Vista_Movimientos(new ServiciosVistaDePrueba()))
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
                    var vista=new Vista_Movimientos(new ServiciosVistaDePrueba());
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
                    using(var vista=new Vista_Movimientos(new ServiciosVistaDePrueba()))
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
