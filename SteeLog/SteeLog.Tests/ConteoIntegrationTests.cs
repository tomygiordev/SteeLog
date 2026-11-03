using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using SteeLog.Core.Application.Conteos;
using SteeLog.Core.Application.Identidad;
using SteeLog.Core.Application.Inventario;
using SteeLog.Domain.Conteos;
using SteeLog.Infrastructure.Conteos;
using SteeLog.Infrastructure.Inventario;
using SteeLog.Infrastructure.Persistence;
using Xunit;

[Trait("Category", "Integration")]
public sealed class ConteoIntegrationTests
{
    static string ConnectionString() {
        var cs=Environment.GetEnvironmentVariable("STEELOG_CONTEOS_TEST_CONNECTION"); var db=Environment.GetEnvironmentVariable("STEELOG_CONTEOS_TEST_DB");
        if (string.IsNullOrWhiteSpace(cs) || !Regex.IsMatch(db ?? "", "^steelog_test_[0-9a-f]{32}$")) throw new InvalidOperationException("Ejecutar el runner de conteos con DB efímera.");
        var b=new NpgsqlConnectionStringBuilder(cs); if(b.Host!="127.0.0.1" || b.Database!=db || b.Port<=0 || b.Port==5435) throw new InvalidOperationException("Destino no permitido.");
        using(var c=new NpgsqlConnection(cs)) { c.Open(); using(var q=new NpgsqlCommand("SELECT current_database()",c)) if((string)q.ExecuteScalar()!=db) throw new InvalidOperationException("DB incorrecta."); } return cs;
    }
    static object Sql(string text, params NpgsqlParameter[] args) { using(var c=new NpgsqlConnection(ConnectionString())) { c.Open(); using(var q=new NpgsqlCommand(text,c)) { q.CommandTimeout=10; q.Parameters.AddRange(args); return q.ExecuteScalar(); } } }
    static NpgsqlParameter P(string name,object value)=>new NpgsqlParameter(name,value);
    static int Material(decimal stock=10m) => Convert.ToInt32(Sql("INSERT INTO public.materiales(nombre,preciounitario,stockactual,stockminimo,id_categoria) VALUES (@n,0,@s,0,1) RETURNING id",P("n",Guid.NewGuid().ToString("N")),P("s",stock)));
    static object Value(string col,int id) => Sql("SELECT "+col+" FROM public.conteos_inventario WHERE id_conteo=@id",P("id",id));
    static object Detail(string col,int id) => Sql("SELECT "+col+" FROM public.conteos_inventario_detalle WHERE id_conteo=@id",P("id",id));
    static Session Actor(string email="owner@example.invalid",Role role=Role.Galpon) { var s=new Session(); if(role!=Role.None) s.Start(new Identity(email,role)); return s; }
    static PostgresConnectionFactory Factory()=>new PostgresConnectionFactory(ConnectionString());
    static async Task<T> Run<T>(Func<OperacionesConteo,Task<T>> f,string email="owner@example.invalid",Role role=Role.Galpon, Func<IConteosEscritura,IConteosEscritura> wrap=null) { using(var u=new NpgsqlUnitOfWork(Factory())) { IConteosEscritura repo=new NpgsqlConteos(u); return await f(new OperacionesConteo(Actor(email,role),u,wrap==null?repo:wrap(repo),new NpgsqlInventario(u))); } }
    static Task<int> Crear()=>Run(o=>o.CrearAsync());
    static Task<bool> Cargar(int id,int m,decimal n)=>Run(o=>o.CargarAsync(id,m,n));
    static Task<bool> Corregir(int id,int m,decimal n)=>Run(o=>o.CorregirAsync(id,m,n));
    static Task<bool> Quitar(int id,int d)=>Run(o=>o.QuitarAsync(id,d));
    static Task<bool> Finalizar(int id)=>Run(o=>o.FinalizarAsync(id,"observación"));
    static Task<bool> Cancelar(int id)=>Run(o=>o.CancelarAsync(id));
    static Task<bool> Eliminar(int id)=>Run(o=>o.EliminarAsync(id),role:Role.Admin);
    static int Detalle(int id)=>Convert.ToInt32(Detail("id_detalle",id));
    static decimal Cantidad(int id)=>Convert.ToDecimal(Detail("cantidad_contada",id));
    static long Count(string table,int id)=>Convert.ToInt64(Sql("SELECT count(*) FROM public."+table+" WHERE id_conteo=@id",P("id",id)));

    [Fact] public async Task CreacionAcumulacionCorreccionPrecisionYSnapshot() {
        int m=Material(), id=await Crear(); Assert.True(id>0); Assert.Equal("owner@example.invalid",Value("usuario_inicio",id));
        string nombre=Convert.ToString(Sql("SELECT nombre FROM public.materiales WHERE id=@id",P("id",m))), categoria=Convert.ToString(Sql("SELECT nombre FROM public.categorias WHERE id_categoria=1"));
        await Cargar(id,m,0m); await Cargar(id,m,1.25m); await Cargar(id,m,.75m); Assert.Equal(2m,Cantidad(id)); Assert.Equal(1,Count("conteos_inventario_detalle",id));
        Sql("UPDATE public.materiales SET nombre=@n WHERE id=@id RETURNING id",P("n","cambiado"),P("id",m)); await Corregir(id,m,0m); Assert.Equal(0m,Cantidad(id));
        await Corregir(id,m,9999999999999999.99m); Assert.Equal(9999999999999999.99m,Cantidad(id));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(()=>Cargar(id,m,.01m)); await Assert.ThrowsAsync<ArgumentOutOfRangeException>(()=>Corregir(id,m,.001m)); await Assert.ThrowsAsync<ArgumentOutOfRangeException>(()=>Corregir(id,m,10000000000000000m));
        Assert.Equal(9999999999999999.99m,Cantidad(id)); Assert.Equal(nombre,Detail("material_nombre_snapshot",id)); Assert.Equal(categoria,Detail("material_categoria_snapshot",id));
    }
    [Fact] public async Task CierreExactoSinAjusteYSinMutacionPosterior() {
        int m=Material(10.25m),id=await Crear(); await Assert.ThrowsAsync<InvalidOperationException>(()=>Finalizar(id)); await Cargar(id,m,8.75m); int d=Detalle(id);
        long movimientos=Convert.ToInt64(Sql("SELECT count(*) FROM public.movimientos")); await Finalizar(id);
        Assert.Equal("finalizado",Value("estado",id)); Assert.Equal(10.25m,Convert.ToDecimal(Detail("stock_sistema",id))); Assert.Equal(-1.50m,Convert.ToDecimal(Detail("diferencia",id)));
        Assert.Equal(10.25m,Convert.ToDecimal(Sql("SELECT stockactual FROM public.materiales WHERE id=@id",P("id",m)))); Assert.Equal(movimientos,Convert.ToInt64(Sql("SELECT count(*) FROM public.movimientos")));
        await Assert.ThrowsAsync<InvalidOperationException>(()=>Cargar(id,m,1m)); await Assert.ThrowsAsync<InvalidOperationException>(()=>Corregir(id,m,1m)); await Assert.ThrowsAsync<ArgumentOutOfRangeException>(()=>Corregir(id,m,10000000000000000m)); await Assert.ThrowsAsync<InvalidOperationException>(()=>Quitar(id,d)); await Assert.ThrowsAsync<InvalidOperationException>(()=>Finalizar(id));
        Assert.Equal(8.75m,Cantidad(id)); Assert.Equal(1,Count("conteos_inventario_detalle",id));
    }
    [Fact] public async Task CancelacionConservaEstadoYRechazaCambiosYEliminarEsAdmin() {
        int m=Material(),id=await Crear(); await Cargar(id,m,3m); int d=Detalle(id); await Cancelar(id); Assert.Equal("cancelado",Value("estado",id));
        await Assert.ThrowsAsync<InvalidOperationException>(()=>Cargar(id,m,1)); await Assert.ThrowsAsync<InvalidOperationException>(()=>Corregir(id,m,1)); await Assert.ThrowsAsync<InvalidOperationException>(()=>Quitar(id,d)); await Assert.ThrowsAsync<InvalidOperationException>(()=>Cancelar(id));
        Assert.Equal(3m,Cantidad(id)); await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>Run(o=>o.EliminarAsync(id))); await Eliminar(id); Assert.Equal(0,Count("conteos_inventario",id)); await Assert.ThrowsAsync<InvalidOperationException>(()=>Eliminar(id));
    }
    [Fact] public async Task PermisosYPropiedadNoDependenDeSesionGlobal() {
        int m=Material(),id=await Crear(); await Run(o=>o.CargarAsync(id,m,1),"OWNER@EXAMPLE.INVALID");
        await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>Run(o=>o.CargarAsync(id,m,1),"otro@example.invalid")); await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>Run(o=>o.CorregirAsync(id,m,2),"otro@example.invalid",Role.Admin));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>Run(o=>o.CrearAsync(),role:Role.None)); Assert.Equal(1m,Cantidad(id));
    }
    sealed class Interceptar : IConteosEscritura {
        readonly IConteosEscritura inner; readonly string paso; readonly bool fail; readonly CancellationTokenSource cancel;
        public readonly TaskCompletionSource<bool> Entrado=new TaskCompletionSource<bool>(); public readonly TaskCompletionSource<bool> Seguir=new TaskCompletionSource<bool>();
        public Interceptar(IConteosEscritura inner,string paso,bool fail=false,CancellationTokenSource cancel=null) { this.inner=inner; this.paso=paso; this.fail=fail; this.cancel=cancel; }
        async Task Pause(string p) { if(p!=paso)return; Entrado.TrySetResult(true); if(fail)throw new InvalidOperationException("Fallo posterior al SQL real"); if(cancel!=null) { cancel.Cancel(); return; } await Seguir.Task; }
        public Task<int> CrearAsync(string p,CancellationToken t)=>inner.CrearAsync(p,t);
        public Task<Conteo> BloquearAsync(int id,CancellationToken t)=>inner.BloquearAsync(id,t);
        public async Task GuardarCantidadAsync(int id,int m,decimal n,CancellationToken t) { await inner.GuardarCantidadAsync(id,m,n,t); await Pause("cantidad"); }
        public async Task QuitarDetalleAsync(int id,int d,CancellationToken t) { await inner.QuitarDetalleAsync(id,d,t); await Pause("quitar"); }
        public async Task GuardarCierreAsync(int id,string u,string o,IDictionary<int,decimal> s,IReadOnlyDictionary<int,decimal> d,CancellationToken t) { await inner.GuardarCierreAsync(id,u,o,s,d,t); await Pause("cierre"); }
        public async Task GuardarCancelacionAsync(int id,CancellationToken t) { await inner.GuardarCancelacionAsync(id,t); await Pause("cancelar"); }
        public async Task EliminarAsync(int id,CancellationToken t) { await inner.EliminarAsync(id,t); await Pause("eliminar"); }
    }
    static async Task Esperar(Task task) { var fin=await Task.WhenAny(task,Task.Delay(TimeSpan.FromSeconds(15))); Assert.Same(task,fin); await task; }
    static async Task EsperarLock() {
        var until=DateTime.UtcNow.AddSeconds(15); do { var n=Convert.ToInt32(Sql("SELECT count(*) FROM pg_stat_activity WHERE datname=current_database() AND wait_event_type='Lock' AND query ILIKE '%conteos_inventario%'")); if(n>0)return; await Task.Delay(40); } while(DateTime.UtcNow<until);
        throw new Xunit.Sdk.XunitException("La segunda operación no esperó un lock PostgreSQL de conteos.");
    }
    static async Task Carrera(int id,string paso,Func<OperacionesConteo,Task<bool>> primero,Func<Task<bool>> segundo,bool segundoDebeFallar) {
        Interceptar gate=null; var a=Run(primero,role:paso=="eliminar"?Role.Admin:Role.Galpon,wrap:r=>gate=new Interceptar(r,paso));
        while(gate==null) await Task.Delay(10); await Esperar(gate.Entrado.Task); var b=segundo();
        try { await EsperarLock(); } finally { gate.Seguir.TrySetResult(true); }
        Assert.True(await a); if(segundoDebeFallar) await Assert.ThrowsAsync<InvalidOperationException>(()=>b); else Assert.True(await b);
    }
    [Fact] public async Task CarreraCargaAntesCierreYCierreAntesCarga() {
        int m=Material(),id=await Crear(); await Carrera(id,"cantidad",o=>o.CargarAsync(id,m,2),()=>Finalizar(id),false); Assert.Equal(2m,Cantidad(id)); Assert.Equal("finalizado",Value("estado",id));
        int id2=await Crear(); await Cargar(id2,m,4); await Carrera(id2,"cierre",o=>o.FinalizarAsync(id2,null),()=>Cargar(id2,m,3),true); Assert.Equal(4m,Cantidad(id2));
    }
    [Fact] public async Task CarreraCancelacionCorreccionYQuitarCierre() {
        int m=Material(),id=await Crear(); await Cargar(id,m,2); await Carrera(id,"cancelar",o=>o.CancelarAsync(id),()=>Corregir(id,m,9),true); Assert.Equal(2m,Cantidad(id));
        int id2=await Crear(),m2=Material(); await Cargar(id2,m,2); await Cargar(id2,m2,3); int d=Convert.ToInt32(Sql("SELECT id_detalle FROM public.conteos_inventario_detalle WHERE id_conteo=@id AND id_material=@m",P("id",id2),P("m",m)));
        await Carrera(id2,"quitar",o=>o.QuitarAsync(id2,d),()=>Finalizar(id2),false); Assert.Equal(1,Count("conteos_inventario_detalle",id2)); Assert.Equal("finalizado",Value("estado",id2));
    }
    [Fact] public async Task CarreraEliminarContraCargaYCorreccionesSerializan() {
        int m=Material(),id=await Crear(); await Cargar(id,m,2); await Cancelar(id); await Carrera(id,"eliminar",o=>o.EliminarAsync(id),()=>Cargar(id,m,3),true); Assert.Equal(0,Count("conteos_inventario",id));
        int id2=await Crear(); await Cargar(id2,m,1); await Carrera(id2,"cantidad",o=>o.CorregirAsync(id2,m,5),()=>Corregir(id2,m,7),false); Assert.Equal(7m,Cantidad(id2));
    }
    [Fact] public async Task FalloYCancelacionDespuesDeSqlRealHacenRollback() {
        int m=Material(),id=await Crear(); await Cargar(id,m,2);
        await Assert.ThrowsAsync<InvalidOperationException>(()=>Run(o=>o.CorregirAsync(id,m,5),wrap:r=>new Interceptar(r,"cantidad",true))); Assert.Equal(2m,Cantidad(id));
        using(var cts=new CancellationTokenSource()) { await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>Run(o=>o.CargarAsync(id,m,3,cts.Token),wrap:r=>new Interceptar(r,"cantidad",cancel:cts))); } Assert.Equal(2m,Cantidad(id));
    }
    [Fact] public async Task CierreEsperaMaterialYCapturaStockConfirmadoSinAjustarlo() {
        int m=Material(10m),id=await Crear(); await Cargar(id,m,8m);
        using(var c=new NpgsqlConnection(ConnectionString())) { c.Open(); using(var tx=c.BeginTransaction()) {
            using(var q=new NpgsqlCommand("UPDATE public.materiales SET stockactual=12.25 WHERE id=@id",c,tx)) { q.Parameters.AddWithValue("id",m); q.ExecuteNonQuery(); }
            var cierre=Finalizar(id); try {
                var until=DateTime.UtcNow.AddSeconds(15); int waiting=0;
                while(DateTime.UtcNow<until) { waiting=Convert.ToInt32(Sql("SELECT count(*) FROM pg_stat_activity WHERE datname=current_database() AND wait_event_type='Lock' AND query ILIKE '%stockactual%FOR UPDATE%'")); if(waiting>0)break; await Task.Delay(40); }
                Assert.True(waiting>0,"El cierre debe esperar el UPDATE concurrente del material.");
            } finally { tx.Commit(); } await cierre;
        } }
        Assert.Equal(12.25m,Convert.ToDecimal(Detail("stock_sistema",id))); Assert.Equal(-4.25m,Convert.ToDecimal(Detail("diferencia",id))); Assert.Equal(12.25m,Convert.ToDecimal(Sql("SELECT stockactual FROM public.materiales WHERE id=@id",P("id",m))));
    }
    [Fact] public async Task IdentidadesHistoricasSoloMapeanCuentasDemoExplicitas() {
        int m=Material(),id=await Crear(); Sql("UPDATE public.conteos_inventario SET usuario_inicio='  GalPon  ' WHERE id_conteo=@id RETURNING id_conteo",P("id",id));
        var legado=new ConsultarConteos(Actor("GALPON@TESIS.LOCAL"),new NpgsqlConteosConsulta(Factory())); var ajeno=new ConsultarConteos(Actor("galpon@otro.invalid"),new NpgsqlConteosConsulta(Factory())); Assert.Equal(id,(await legado.AbiertoAsync()).Id); Assert.Null(await ajeno.AbiertoAsync());
        await Run(o=>o.CargarAsync(id,m,1),"GALPON@TESIS.LOCAL");
        await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>Run(o=>o.CargarAsync(id,m,1),"galpon@otro.invalid"));
        Assert.Equal(1m,Cantidad(id)); await Run(o=>o.CancelarAsync(id),"admin@tesis.local",Role.Admin); Assert.Equal("cancelado",Value("estado",id));
    }
    [Fact] public async Task DobleCierreYEliminacionConcurrenteSeSerializan() {
        int m=Material(),id=await Crear(); await Cargar(id,m,2);
        await Carrera(id,"cierre",o=>o.FinalizarAsync(id,null),()=>Finalizar(id),true); Assert.Equal("finalizado",Value("estado",id));
        await Carrera(id,"eliminar",o=>o.EliminarAsync(id),()=>Eliminar(id),true); Assert.Equal(0,Count("conteos_inventario",id));
    }
    [Fact] public async Task CierreFallaLuegoDelSqlYRevierteSnapshotsYCabecera() {
        int m=Material(9m),id=await Crear(); await Cargar(id,m,3m);
        await Assert.ThrowsAsync<InvalidOperationException>(()=>Run(o=>o.FinalizarAsync(id,"x"),wrap:r=>new Interceptar(r,"cierre",true)));
        Assert.Equal("abierto",Value("estado",id)); Assert.Equal(DBNull.Value,Detail("stock_sistema",id)); Assert.Equal(DBNull.Value,Detail("diferencia",id)); Assert.Equal(9m,Convert.ToDecimal(Sql("SELECT stockactual FROM public.materiales WHERE id=@id",P("id",m))));
    }
    [Fact] public async Task ConsultasAbiertoHistorialDetallesYPermisosEnConexionesIndependientes() {
        string email="consultas"+Guid.NewGuid().ToString("N")+"@example.invalid"; int m=Material(7m); int id=await Run(o=>o.CrearAsync(),email); await Run(o=>o.CargarAsync(id,m,5.5m),email);
        var q=new ConsultarConteos(Actor(email.ToUpperInvariant()),new NpgsqlConteosConsulta(Factory()));
        Assert.Equal(id,(await q.AbiertoAsync()).Id); Assert.Equal(id,(await q.ObtenerAsync(id)).Id); var antes=Assert.Single(await q.DetallesAsync(id)); Assert.Equal(5.5m,antes.Cantidad); Assert.Null(antes.Stock);
        await Run(o=>o.FinalizarAsync(id,"observacion unica "+email),email); Assert.Null(await q.AbiertoAsync()); var fila=Assert.Single(await q.HistorialAsync(DateTime.Today.AddDays(-1),DateTime.Today.AddDays(1),email),x=>x.Id==id);
        Assert.Equal(1,fila.Materiales); Assert.Equal(1,fila.Diferencias); Assert.Equal("finalizado",fila.Estado); var detalle=Assert.Single(await q.DetallesAsync(id)); Assert.Equal(7m,detalle.Stock); Assert.Equal(-1.5m,detalle.Diferencia);
        Assert.DoesNotContain(await q.HistorialAsync(DateTime.Today.AddDays(2),null,email),x=>x.Id==id); Assert.Contains(await q.HistorialAsync(null,null,antes.Nombre),x=>x.Id==id);
        int seccion=Convert.ToInt32(Sql("INSERT INTO public.reporte_stock_secciones(nombre,orden) VALUES (@n,9) RETURNING id",P("n",email))); Sql("INSERT INTO public.reporte_stock_materiales(id_seccion,id_material,orden) VALUES (@s,@m,1) RETURNING id_material",P("s",seccion),P("m",m)); var secciones=await q.SeccionesAsync(); Assert.Equal(email,secciones[m].Nombre); Assert.Equal(9,secciones[m].Orden);
        var denegada=new ConsultarConteos(Actor(role:Role.None),new NpgsqlConteosConsulta(Factory())); Assert.Throws<UnauthorizedAccessException>(()=>{ _ = denegada.AbiertoAsync(); }); Assert.Throws<UnauthorizedAccessException>(()=>{ _ = denegada.ObtenerAsync(id); }); Assert.Throws<UnauthorizedAccessException>(()=>{ _ = denegada.DetallesAsync(id); }); Assert.Throws<UnauthorizedAccessException>(()=>{ _ = denegada.HistorialAsync(null,null,null); }); Assert.Throws<UnauthorizedAccessException>(()=>{ _ = denegada.SeccionesAsync(); });
    }
    [Fact] public async Task TodasLasMutacionesEsperanLaCabeceraAntesDeEscribir() {
        foreach(string accion in new[]{"carga","correccion","quitar","cierre","cancelar","eliminar"}) {
            int m=Material(),id=await Crear(); await Cargar(id,m,2m); int d=Detalle(id); if(accion=="eliminar") await Cancelar(id);
            using(var c=new NpgsqlConnection(ConnectionString())) { c.Open(); using(var tx=c.BeginTransaction()) {
                using(var q=new NpgsqlCommand("SELECT id_conteo FROM public.conteos_inventario WHERE id_conteo=@id FOR UPDATE",c,tx)) { q.Parameters.AddWithValue("id",id); q.ExecuteScalar(); }
                Task<bool> task=null;
                try {
                    switch(accion) { case "carga":task=Cargar(id,m,1);break; case "correccion":task=Corregir(id,m,1);break; case "quitar":task=Quitar(id,d);break; case "cierre":task=Finalizar(id);break; case "cancelar":task=Cancelar(id);break; default:task=Eliminar(id);break; }
                    await EsperarLock(); Assert.False(task.IsCompleted,"La operación "+accion+" no debe escribir antes del lock de cabecera.");
                } finally { tx.Commit(); }
                Assert.True(await task);
            } }
        }
    }
    [Fact] public async Task LookupMaterialesUtilizadoPorConteosRespetaFiltrosYPermisos() {
        int m=Material(7.25m); string nombre=Convert.ToString(Sql("SELECT nombre FROM public.materiales WHERE id=@id",P("id",m)));
        var q=new ConsultarMateriales(Actor(),new NpgsqlMaterialesConsulta(Factory()));
        var material=Assert.Single(await q.ListarAsync(new FiltroMaterialLectura { Nombre=nombre }),x=>x.Id==m);
        Assert.Equal(nombre,material.Nombre); Assert.Equal(7.25m,material.Stock); Assert.Equal(1,material.CategoriaId); Assert.Contains(await q.CategoriasAsync(),x=>x.Id==1);
        var prohibido=new ConsultarMateriales(Actor(role:Role.None),new NpgsqlMaterialesConsulta(Factory())); Assert.Throws<UnauthorizedAccessException>(()=>{ _=prohibido.ListarAsync(null); });
    }}
