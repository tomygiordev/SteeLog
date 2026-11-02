using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using System.Threading;
using WP_Inventario.Datos;
using WP_Inventario.Modelos;
class IntegrityTests {
 static int checks;
 static DbConnection Connect() { return (DbConnection)typeof(ConexionDB).GetMethod("GetConnection").Invoke(new ConexionDB(),null); }
 static object Sql(DbConnection c, string sql, DbTransaction t=null) { using(var cmd=c.CreateCommand()) { cmd.CommandText=sql; cmd.Transaction=t; return cmd.ExecuteScalar(); } }
 static bool Delete(int id) { try { new MovimientoDatos().EliminarMovimiento(id,null,0,"entrada falsa"); return true; } catch(InvalidOperationException) { return false; } }
 static void Check(bool ok,string name) { if(!ok) throw new Exception(name); checks++; Console.WriteLine("OK " + name); }
 static void Main() {
  using(var c=(DbConnection)typeof(ConexionDB).GetMethod("GetConnection").Invoke(new ConexionDB(),null)) using(var cmd=c.CreateCommand()) {cmd.CommandText="SELECT current_database()"; Check((string)cmd.ExecuteScalar()==Environment.GetEnvironmentVariable("STEELOG_TEST_DB"),"database identity");}
  var d=new MaterialDatos();
  int a=d.guardarMaterial("Primero A",10,1,1), b=d.guardarMaterial("Primero B",10,1,1);
  using(var start=new ManualResetEventSlim(false)) {
   Task t1=Task.Run(()=>{start.Wait();new MaterialDatos().editarMaterial("Primero A",11,1,1,a,null,10);});
   Task t2=Task.Run(()=>{start.Wait();new MaterialDatos().editarMaterial("Primero B",12,1,1,b,null,10);});
   start.Set();Task.WaitAll(t1,t2);
  }
  using(var c=Connect()) Check(Convert.ToInt32(Sql(c,"SELECT count(*) FROM movimientos WHERE id_tipomovimiento=11 AND date(fecha)=current_date"))==1,"first daily header shared");
  int id=d.guardarMaterial("Prueba de integridad",100.5m,1,1);
  var m=new Movimiento {fecha=DateTime.Now,nroremito="TEST-VENTA-"+id,id_origen=2,tipo_origen="UBICACION",id_destino=4,tipo_destino="UBICACION",id_tipomovimiento=7};
  int mov=new MovimientoDatos().GuardarMovimiento(m,new List<MovimientoMaterial>{new MovimientoMaterial{id_material=id,cantidad=5,NombreMaterial="Prueba"}});
  bool conflict=false; try {d.editarMaterial("Cambio inválido",110.5m,1,1,id,null,100.5m);} catch(DBConcurrencyException){conflict=true;}
  var actual=d.obtenerMateriales().Find(x=>x.id==id); Check(conflict && actual.stockActual==95.5m && actual.nombre=="Prueba de integridad","stale rejected");
  d.editarMaterial("Prueba actualizada",105.5m,1,1,id,null,95.5m); actual=d.obtenerMateriales().Find(x=>x.id==id); Check(actual.stockActual==105.5m,"decimal adjustment");
  conflict=false; try {d.editarMaterial("Cambio inválido",106m,1,1,id);} catch(DBConcurrencyException){conflict=true;} Check(conflict,"snapshot required");
  d.editarMaterial("Metadata",105.5m,1,1,id); Check(d.obtenerMateriales().Find(x=>x.id==id).nombre=="Metadata","metadata without snapshot");
  int countBefore; int providersBefore;
  using(var c=Connect()) {countBefore=Convert.ToInt32(Sql(c,"SELECT count(*) FROM movimientomaterial WHERE id_material="+id)); providersBefore=Convert.ToInt32(Sql(c,"SELECT count(*) FROM materiales_proveedores WHERE id_material="+id));}
  bool rollback=false; try { d.editarMaterial("Proveedor inválido",109m,1,1,id,new List<int>{2147483647},105.5m); } catch(Exception) {rollback=true;}
  actual=d.obtenerMateriales().Find(x=>x.id==id); Check(rollback && actual.stockActual==105.5m && actual.nombre=="Metadata","invalid provider rolls back material");
  using(var c=Connect()) Check(countBefore==Convert.ToInt32(Sql(c,"SELECT count(*) FROM movimientomaterial WHERE id_material="+id)) && providersBefore==Convert.ToInt32(Sql(c,"SELECT count(*) FROM materiales_proveedores WHERE id_material="+id)),"invalid provider rolls back history and providers");
  int race=d.guardarMaterial("Carrera",40,1,1);
  using(var start=new ManualResetEventSlim(false)) {
   Func<decimal,bool> edit=qty=>{start.Wait();try {new MaterialDatos().editarMaterial("Carrera",qty,1,1,race,null,40);return true;} catch(DBConcurrencyException){return false;}};
   Task<bool> t1=Task.Run(()=>edit(41)); Task<bool> t2=Task.Run(()=>edit(42));start.Set();Task.WaitAll(t1,t2);
   Check(t1.Result != t2.Result,"same snapshot permits one edit");
  }
  decimal raceStock=d.obtenerMateriales().Find(x=>x.id==race).stockActual;
  using(var c=Connect()) Check(Convert.ToDecimal(Sql(c,"SELECT COALESCE(sum(cantidad),0) FROM movimientomaterial WHERE id_material="+race))==raceStock-40,"same snapshot history matches stock");
  bool invalid=false; try {d.guardarMaterial("Negativo",-1,0,1);} catch(ArgumentOutOfRangeException){invalid=true;} Check(invalid,"negative rejected");
  invalid=false; try {d.editarMaterial("Ausente",0,0,1,2147483647,null,0);} catch(InvalidOperationException){invalid=true;} Check(invalid,"missing rejected");
  using(var blocker=Connect()) using(var tx=blocker.BeginTransaction()) using(var monitor=Connect()) {
   Sql(blocker,"SELECT stockactual FROM materiales WHERE id="+id+" FOR UPDATE",tx);
   Task<bool> one=Task.Run(()=>Delete(mov)); Task<bool> two=Task.Run(()=>Delete(mov));
   int blocked=0; DateTime limit=DateTime.UtcNow.AddSeconds(15);
   while(DateTime.UtcNow<limit) {
    blocked=Convert.ToInt32(Sql(monitor,"SELECT count(*) FROM pg_stat_activity WHERE datname=current_database() AND wait_event_type='Lock' AND (query LIKE '%UPDATE public.materiales SET stockactual%' OR query LIKE '%SELECT tipo_destino, id_tipomovimiento FROM public.movimientos%')"));
    if(blocked>=2) break; Thread.Sleep(20);
   }
   Check(blocked>=2,"both deletes blocked before release");
   tx.Commit(); Task.WaitAll(one,two);
   Check(one.Result != two.Result,"one concurrent delete succeeds");
   Check(d.obtenerMateriales().Find(x=>x.id==id).stockActual==110.5m,"delete uses persisted type once");
  }
  m.nroremito="TEST-EDIT-DELETE-"+id;
  var details=new List<MovimientoMaterial>{new MovimientoMaterial{id_material=id,cantidad=5,NombreMaterial="Prueba"}};
  int id2=new MovimientoDatos().GuardarMovimiento(m,details);
  using(var blocker=Connect()) using(var tx=blocker.BeginTransaction()) using(var monitor=Connect()) {
   Sql(blocker,"SELECT stockactual FROM materiales WHERE id="+id+" FOR UPDATE",tx);
   Task<bool> del=Task.Run(()=>Delete(id2));
   DateTime limit=DateTime.UtcNow.AddSeconds(15); int waiting=0;
   while(DateTime.UtcNow<limit) {waiting=Convert.ToInt32(Sql(monitor,"SELECT count(*) FROM pg_stat_activity WHERE datname=current_database() AND wait_event_type='Lock' AND query LIKE '%UPDATE public.materiales SET stockactual%'"));if(waiting>=1)break;Thread.Sleep(20);}
   Check(waiting>=1,"delete holds header before edit");
   Task<bool> edit=Task.Run(()=>{try {new MovimientoDatos().ActualizarMovimiento(id2,m,details);return true;} catch(Exception) {return false;}});
   limit=DateTime.UtcNow.AddSeconds(15); waiting=0;
   while(DateTime.UtcNow<limit) {waiting=Convert.ToInt32(Sql(monitor,"SELECT count(*) FROM pg_stat_activity WHERE datname=current_database() AND wait_event_type='Lock' AND (query LIKE '%UPDATE public.materiales SET stockactual%' OR query LIKE '%SELECT tipo_destino, id_tipomovimiento%')"));if(waiting>=2)break;Thread.Sleep(20);}
   Check(waiting>=2,"edit waits for locked header");tx.Commit();Task.WaitAll(del,edit);
   Check(del.Result && !edit.Result && d.obtenerMateriales().Find(x=>x.id==id).stockActual==110.5m,"delete then edit reconciles stock");
  }
  Console.WriteLine("CHECKS="+checks);
 }
}

