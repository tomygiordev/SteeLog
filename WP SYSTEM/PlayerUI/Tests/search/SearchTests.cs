using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using System.Linq.Expressions;
using System.Threading;
using System.Windows.Forms;
class HeldContext : SynchronizationContext { public readonly BlockingCollection<Action> Queue=new BlockingCollection<Action>(); public override void Post(SendOrPostCallback d,object state) { Queue.Add(()=>d(state)); } }
class SearchTests {
 static object fixture; static int calls; static bool fail, gate;
 static ManualResetEventSlim entered=new ManualResetEventSlim(false), release=new ManualResetEventSlim(false);
 public static object Load() { Interlocked.Increment(ref calls); if(fail){fail=false;throw new Exception("fixture failure");} if(gate){entered.Set();if(!release.Wait(10000))throw new Exception("gate timeout");} return fixture; }
 static BindingFlags B=BindingFlags.Instance|BindingFlags.NonPublic;
 static object Get(object o,string n){return o.GetType().GetField(n,B).GetValue(o);}
 static void Set(object o,string n,object v){o.GetType().GetField(n,B).SetValue(o,v);}
 [STAThread] static void Main(){
  Control.CheckForIllegalCrossThreadCalls=true;
  var asm=Assembly.LoadFrom(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"SteeLog.exe"));
  using(var parent=new Form()) using(var top=new Panel{Width=1000,Height=50}) {
   parent.Controls.Add(top); var handle=parent.Handle;
   var type=asm.GetType("WP_Inventario.BusquedaGlobal");
   object search=Activator.CreateInstance(type,new object[]{parent,top,new Action<Form>(f=>f.Dispose())});
   var snap=Activator.CreateInstance(type.GetNestedType("Snapshot",BindingFlags.NonPublic),true);
   foreach(string part in new[]{"Materiales","Recortes","Herramientas","Insumos"}) {
    string model=part=="Materiales"?"Material":part=="Recortes"?"Recorte":part=="Herramientas"?"Herramienta":"Insumo";
    var mt=asm.GetType("WP_Inventario.Modelos."+model); var list=(IList)Activator.CreateInstance(typeof(System.Collections.Generic.List<>).MakeGenericType(mt));
    if(model=="Material"){var m=Activator.CreateInstance(mt);mt.GetProperty("nombre").SetValue(m,"Perfil Prueba");list.Add(m);} Set(snap,part,list);
   }
   Set(search,"_cache",snap);Set(search,"_cacheTimestampUtc",DateTime.UtcNow);
   var input=(Control)Get(search,"_txtBusqueda"); input.Text="perfil";
   var ctx=new HeldContext();SynchronizationContext.SetSynchronizationContext(ctx);
   type.GetMethod("EjecutarBusqueda",B).Invoke(search,new object[]{"perfil"});
   Action continuation;if(!ctx.Queue.TryTake(out continuation,10000))throw new Exception("No continuation");
   int before=(int)Get(search,"_searchVersion");input.Text="";int after=(int)Get(search,"_searchVersion");continuation();
   var results=(Panel)Get(search,"_panelResultados");
   if(after<=before || results.Controls.Count!=0 || (bool)typeof(Control).GetMethod("GetState",B).Invoke(results,new object[]{2})) throw new Exception("Stale result displayed");
   Console.WriteLine("OK clear invalidates held continuation");
   foreach(string mode in new[]{"short","escape"}) {
    input.Text="perfil";type.GetMethod("EjecutarBusqueda",B).Invoke(search,new object[]{"perfil"});
    if(!ctx.Queue.TryTake(out continuation,10000))throw new Exception("No continuation for "+mode);
    if(mode=="short") input.Text="p";
    else type.GetMethod("TxtBusqueda_KeyDown",B).Invoke(search,new object[]{input,new KeyEventArgs(Keys.Escape)});
    continuation();
    if(results.Controls.Count!=0 || (bool)typeof(Control).GetMethod("GetState",B).Invoke(results,new object[]{2})) throw new Exception("Stale result: "+mode);
    Console.WriteLine("OK "+mode+" invalidates held continuation");
   }
   input.Text="perfil";type.GetMethod("EjecutarBusqueda",B).Invoke(search,new object[]{"perfil"});
   if(!ctx.Queue.TryTake(out continuation,10000))throw new Exception("No continuation before dispose");
   ((IDisposable)search).Dispose();continuation();Console.WriteLine("OK dispose invalidates held continuation");
   fixture=snap; ((IList)Get(snap,"Materiales")).Clear();
   var snapshotType=snap.GetType(); var funcType=typeof(Func<>).MakeGenericType(snapshotType);
   var loader=Expression.Lambda(funcType,Expression.Convert(Expression.Call(typeof(SearchTests).GetMethod("Load")),snapshotType)).Compile();
   var ctor=type.GetConstructor(B,null,new[]{typeof(Form),typeof(Panel),typeof(Action<Form>),funcType},null);
   var second=ctor.Invoke(new object[]{parent,top,new Action<Form>(f=>f.Dispose()),loader});
   var secondInput=(Control)Get(second,"_txtBusqueda"); var panel=(Panel)Get(second,"_panelResultados");
   fail=true;secondInput.Text="nada"; type.GetMethod("EjecutarBusqueda",B).Invoke(second,new object[]{"nada"});
   if(!ctx.Queue.TryTake(out continuation,10000))throw new Exception("error continuation missing");continuation();
   if(panel.Controls.Count!=1 || !panel.Controls[0].Text.Contains("No se pudo"))throw new Exception("error not shown");
   type.GetMethod("EjecutarBusqueda",B).Invoke(second,new object[]{"nada"});
   if(!ctx.Queue.TryTake(out continuation,10000))throw new Exception("retry continuation missing");continuation();
   if(panel.Controls.Count!=1 || !panel.Controls[0].Text.Contains("No se encontraron") || calls!=2)throw new Exception("retry/empty mismatch");
   Console.WriteLine("OK error differs from empty and retry reloads");
   Set(second,"_cacheTimestampUtc",DateTime.UtcNow.AddSeconds(-11)); gate=true;
   type.GetMethod("EjecutarBusqueda",B).Invoke(second,new object[]{"nada"});
   if(!entered.Wait(10000))throw new Exception("loader gate not entered");
   type.GetMethod("EjecutarBusqueda",B).Invoke(second,new object[]{"nada"});release.Set();
   Action c1,c2;if(!ctx.Queue.TryTake(out c1,10000)||!ctx.Queue.TryTake(out c2,10000))throw new Exception("gate continuations missing");c1();c2();
   if(calls!=3)throw new Exception("concurrent reload was not shared");
   Console.WriteLine("OK expired cache reload is shared by concurrent workers");
   ((IDisposable)second).Dispose();
   Console.WriteLine("CHECKS=6");

  }
 }
}
