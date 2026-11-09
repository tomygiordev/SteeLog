using System;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;
using SteeLog.Core.Application.Activos;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SteeLog.Presentation;
using Xunit;
namespace SteeLog.Tests
{
    public class HerramientasPresenterTests
    {
        private sealed class Vista : IHerramientasVista { public int Errores; public void SetOcupada(bool valor) {} public void MostrarError(Exception error) { Errores++; } }
        private sealed class Ejecutor : IUseCaseExecutor
        {
            public int Llamadas; public TaskCompletionSource<int> Pendiente=new TaskCompletionSource<int>();
            public async Task<T> ExecuteAsync<U,T>(Func<U,Task<T>> operacion) where U:class { Llamadas++; return (T)(object)await Pendiente.Task; }
        }
        [Fact] public async Task DobleSubmitSeDescartaMientrasEscrituraPendiente()
        {
            var e=new Ejecutor(); var p=new HerramientasPresenter(e,new Vista());
            var primera=p.GuardarAsync(null,"Taladro","Óptima",null,"");
            Assert.Null(await p.EliminarAsync(4)); Assert.Equal(1,e.Llamadas);
            e.Pendiente.SetResult(4); Assert.Equal(4,await primera);
        }
        [Fact] public async Task ErrorTardioNoAfectaContextoNuevo()
        {
            var e=new Ejecutor(); var v=new Vista(); var p=new HerramientasPresenter(e,v); bool vigente=true;
            var pendiente=p.GuardarAsync(4,"Taladro","Óptima",null,"",default,()=>vigente);
            vigente=false; e.Pendiente.SetException(new InvalidOperationException("obsoleto"));
            Assert.Null(await pendiente); Assert.Equal(0,v.Errores);
        }
        private sealed class EjecutorVista : IUseCaseExecutor
        {
            public readonly TaskCompletionSource<IReadOnlyList<HerramientaLectura>> Lectura = new TaskCompletionSource<IReadOnlyList<HerramientaLectura>>();
            public readonly TaskCompletionSource<int> Escritura = new TaskCompletionSource<int>();
            public int LlamadasLectura, LlamadasEscritura;
            public Task<T> ExecuteAsync<U,T>(Func<U,Task<T>> operacion) where U:class
            {
                if(typeof(U)==typeof(GestionarHerramientas)) { LlamadasEscritura++; return (Task<T>)(object)Escritura.Task; }
                if(typeof(T)==typeof(IReadOnlyList<ObraHerramientaLectura>)) return (Task<T>)(object)Task.FromResult<IReadOnlyList<ObraHerramientaLectura>>(new ObraHerramientaLectura[0]);
                if(typeof(T)==typeof(IReadOnlyList<HerramientaLectura>)) { LlamadasLectura++; return (Task<T>)(object)Lectura.Task; }
                throw new InvalidOperationException("Consulta inesperada");
            }
        }
        private const BindingFlags Privado=BindingFlags.Instance|BindingFlags.NonPublic;
        private static object Campo(Form vista,string nombre) => vista.GetType().GetField(nombre,Privado).GetValue(vista);
        private static void Campo(Form vista,string nombre,object valor) => vista.GetType().GetField(nombre,Privado).SetValue(vista,valor);
        private static void Invocar(Form vista,string nombre,params object[] argumentos) => vista.GetType().GetMethod(nombre,Privado).Invoke(vista,argumentos);
        private static void Esperar(Func<bool> condicion)
        {
            var reloj=Stopwatch.StartNew(); while(!condicion() && reloj.Elapsed < TimeSpan.FromSeconds(5)) { Application.DoEvents(); Thread.Sleep(5); }
            Assert.True(condicion(),"La operación no terminó a tiempo.");
        }
        private static void EnSta(Action accion)
        {
            Exception fallo=null; var hilo=new Thread(()=> { try { SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext()); accion(); } catch(Exception e) { fallo=e; } });
            hilo.SetApartmentState(ApartmentState.STA); hilo.Start(); Assert.True(hilo.Join(TimeSpan.FromSeconds(15))); Assert.Null(fallo);
        }
        private static Form CrearVista(EjecutorVista ejecutor)
        {
            var tipo=Assembly.Load("SteeLog").GetType("SteeLog.Vistas.Vista_Herramientas",true);
            var vista=(Form)Activator.CreateInstance(tipo,Privado,null,new object[]{new ServiciosVistaDePrueba(ejecutor),ejecutor},null);
            Campo(vista,"confirmar",new Func<string,string,MessageBoxIcon,bool>((m,t,i)=>true));
            Campo(vista,"avisar",new Action<string>(m=>{}));
            return vista;
        }
        [Fact] public void CargaRealPendienteYFallidaNoHabilitaEdicion()
        {
            EnSta(()=> {
                var e=new EjecutorVista(); using(var v=CrearVista(e)) {
                    int errores=0; Campo(v,"avisar",new Action<string>(m=>errores++));
                    Invocar(v,"Vista_Herramientas_Load",v,EventArgs.Empty); Esperar(()=>e.LlamadasLectura==1);
                    Assert.False(((Control)Campo(v,"btnGuardar")).Enabled); Assert.False(((Control)Campo(v,"txtNombre")).Enabled);
                    e.Lectura.SetException(new InvalidOperationException("sin conexión")); Esperar(()=>errores==1);
                    Assert.False(((Control)Campo(v,"btnGuardar")).Enabled); Assert.False(((Control)Campo(v,"txtNombre")).Enabled);
                }
            });
        }
        [Fact] public void RespuestaPosteriorADisposeNoAplicaNiAvisa()
        {
            EnSta(()=> {
                var e=new EjecutorVista(); var v=CrearVista(e); int errores=0; Campo(v,"avisar",new Action<string>(m=>errores++));
                Invocar(v,"Vista_Herramientas_Load",v,EventArgs.Empty); Esperar(()=>e.LlamadasLectura==1);
                var cierre=(CancellationTokenSource)Campo(v,"cierre"); v.Dispose(); Assert.True(cierre.IsCancellationRequested);
                e.Lectura.SetException(new InvalidOperationException("respuesta tardía"));
                for(int i=0;i<20;i++) { Application.DoEvents(); Thread.Sleep(5); }
                Assert.Equal(0,errores);
            });
        }
        [Fact] public void EscrituraTardiaNoLimpiaTextoEditadoNiRecarga()
        {
            EnSta(()=> {
                var e=new EjecutorVista(); using(var v=CrearVista(e)) {
                    Invocar(v,"Vista_Herramientas_Load",v,EventArgs.Empty); Esperar(()=>e.LlamadasLectura==1);
                    e.Lectura.SetResult(new HerramientaLectura[0]); Esperar(()=>((Control)Campo(v,"btnGuardar")).Enabled);
                    var nombre=(Control)Campo(v,"txtNombre"); nombre.Text="Taladro";
                    Invocar(v,"btnGuardar_Click",v,EventArgs.Empty); Esperar(()=>e.LlamadasEscritura==1);
                    nombre.Text="Nueva edición"; e.Escritura.SetResult(42);
                    Esperar(()=>!((bool)Campo(v,"ocupada")));
                    Assert.Equal("Nueva edición",nombre.Text); Assert.Equal(1,e.LlamadasLectura);
                }
            });
        }
        [Fact] public void VistaBloqueaCamposYEscrituraHastaDatosYCancelaAlDisponer()
        {
            Exception fallo=null; var hilo=new Thread(()=> {
                try {
                    const BindingFlags f=BindingFlags.Instance|BindingFlags.NonPublic;
                    var tipo=Assembly.Load("SteeLog").GetType("SteeLog.Vistas.Vista_Herramientas",true);
                    CancellationTokenSource cierre;
                    using(var vista=(Form)Activator.CreateInstance(tipo,new ServiciosVistaDePrueba())) {
                        cierre=(CancellationTokenSource)tipo.GetField("cierre",f).GetValue(vista);
                        foreach(var nombre in new[]{"btnGuardar","btnEliminar","txtNombre","txtObservaciones","cboEstado","cboObra"}) Assert.False(((Control)tipo.GetField(nombre,f).GetValue(vista)).Enabled);
                        tipo.GetField("obrasCargadas",f).SetValue(vista,true);
                        tipo.GetField("leyendo",f).SetValue(vista,true);
                        tipo.GetMethod("ActualizarControles",f).Invoke(vista,null);
                        Assert.False(((Control)tipo.GetField("btnGuardar",f).GetValue(vista)).Enabled);
                        Assert.False(((Control)tipo.GetField("txtNombre",f).GetValue(vista)).Enabled);
                        tipo.GetField("leyendo",f).SetValue(vista,false);
                        tipo.GetMethod("ActualizarControles",f).Invoke(vista,null);
                        Assert.False(((Control)tipo.GetField("btnGuardar",f).GetValue(vista)).Enabled);
                    }
                    Assert.True(cierre.IsCancellationRequested);
                } catch(Exception ex) { fallo=ex; }
            }); hilo.SetApartmentState(ApartmentState.STA); hilo.Start(); Assert.True(hilo.Join(TimeSpan.FromSeconds(15))); Assert.Null(fallo);
        }
    }
}
