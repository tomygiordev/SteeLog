using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using SteeLog.Core.Application.Activos;
using SteeLog.Core.Application.Identidad;
using SteeLog.Domain.Activos;
using SteeLog.Infrastructure.Activos;
using SteeLog.Infrastructure.Persistence;
using Xunit;

[Trait("Category", "Integration")]
public sealed class HerramientasIntegrationTests
{
    static string ConnectionString() { var cs=Environment.GetEnvironmentVariable("STEELOG_F7_TEST_CONNECTION"); var db=Environment.GetEnvironmentVariable("STEELOG_F7_TEST_DB"); if(string.IsNullOrWhiteSpace(cs)||!Regex.IsMatch(db??"","^steelog_test_[0-9a-f]{32}$")) throw new InvalidOperationException("Se requiere runner aislado F7."); var b=new NpgsqlConnectionStringBuilder(cs); if(b.Host!="127.0.0.1"||b.Database!=db||b.Port<=0||b.Port==5435) throw new InvalidOperationException("Destino no permitido."); using(var c=new NpgsqlConnection(cs)) { c.Open(); using(var q=new NpgsqlCommand("SELECT current_database()",c)) if((string)q.ExecuteScalar()!=db) throw new InvalidOperationException("DB incorrecta."); } return cs; }
    static PostgresConnectionFactory Factory()=>new PostgresConnectionFactory(ConnectionString());
    static Session Actor(Role role=Role.Galpon) { var s=new Session(); if(role!=Role.None) s.Start(new Identity("test@example.invalid",role)); return s; }
    static async Task<T> Run<T>(Func<GestionarHerramientas,Task<T>> f,Role role=Role.Galpon,Func<IHerramientasEscritura,IHerramientasEscritura> wrap=null) { using(var u=new NpgsqlUnitOfWork(Factory())) { IHerramientasEscritura r=new NpgsqlHerramientas(u); return await f(new GestionarHerramientas(Actor(role),u,wrap==null?r:wrap(r))); } }
    static object Sql(string sql,params NpgsqlParameter[] args) { using(var c=new NpgsqlConnection(ConnectionString())) { c.Open(); using(var q=new NpgsqlCommand(sql,c)) { q.CommandTimeout=10; q.Parameters.AddRange(args); return q.ExecuteScalar(); } } }
    static NpgsqlParameter P(string n,object v)=>new NpgsqlParameter(n,v);
    static int Obra(string n)=>Convert.ToInt32(Sql("INSERT INTO public.obras(nombre) VALUES (@n) RETURNING id",P("n",n)));
    static long Hist(int id)=>Convert.ToInt64(Sql("SELECT count(*) FROM public.herramienta_movimientos WHERE id_herramienta=@id",P("id",id)));
    static object Campo(int id,string campo)=>Sql("SELECT "+campo+" FROM public.herramientas WHERE id_herramienta=@id",P("id",id));
    static Task<int> Crear(int? obra=null)=>Run(x=>x.CrearAsync("Taladro","Óptima",obra,"nota"));
    [Fact] public void DefaultDeEstadoEsOptimaSinMojibake()
    {
        Assert.Equal("'Óptima'::character varying",Sql("SELECT column_default FROM information_schema.columns WHERE table_schema='public' AND table_name='herramientas' AND column_name='estado'"));
    }
    [Fact] public async Task AltaPrefijoHistorialInicialTrasladoEdicionYEliminacion()
    {
        string n="obra-"+Guid.NewGuid().ToString("N"); int obra=Obra(n), id=await Crear(obra);
        Assert.Equal(id+"Taladro",Campo(id,"nombre")); Assert.Equal(1,Hist(id));
        Assert.Equal("Galpón",Sql("SELECT obra_origen FROM public.herramienta_movimientos WHERE id_herramienta=@id",P("id",id))); Assert.Equal(n,Sql("SELECT obra_destino FROM public.herramienta_movimientos WHERE id_herramienta=@id",P("id",id)));
        await Run(x=>x.EditarAsync(id,"nuevo","Regular",obra,"editada")); Assert.Equal(1,Hist(id));
        await Run(x=>x.EnviarAGalponAsync(id)); Assert.Equal(DBNull.Value,Campo(id,"id_obra")); Assert.Equal(2,Hist(id));
        Assert.Equal(n,Sql("SELECT obra_origen FROM public.herramienta_movimientos WHERE id_herramienta=@id ORDER BY id DESC LIMIT 1",P("id",id)));
        Assert.Equal("Galpón",Sql("SELECT obra_destino FROM public.herramienta_movimientos WHERE id_herramienta=@id ORDER BY id DESC LIMIT 1",P("id",id)));
        // Recorrido F9: el historial guarda el nombre vigente en cada movimiento (antes quedaba vacío).
        Assert.Equal(id+"Taladro",Sql("SELECT nombre_herramienta FROM public.herramienta_movimientos WHERE id_herramienta=@id ORDER BY id LIMIT 1",P("id",id)));
        Assert.Equal("nuevo",Sql("SELECT nombre_herramienta FROM public.herramienta_movimientos WHERE id_herramienta=@id ORDER BY id DESC LIMIT 1",P("id",id)));
        var q=new ConsultarHerramientas(Actor(),new NpgsqlHerramientasConsulta(Factory())); Assert.Contains(await q.HerramientasAsync(),x=>x.Id==id && x.Nombre=="nuevo");
        await Run(x=>x.EliminarAsync(id)); Assert.Equal(0,Hist(id)); Assert.Equal(0L,Convert.ToInt64(Sql("SELECT count(*) FROM public.herramientas WHERE id_herramienta=@id",P("id",id))));
        int numerico=await Run(x=>x.CrearAsync("123 taladro","Óptima",null,null)); Assert.Equal("123 taladro",Campo(numerico,"nombre"));
    }
    [Fact] public async Task ValidacionPermisosYFk()
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>Run(x=>x.CrearAsync("x","Óptima",null,null),Role.None));
        await Assert.ThrowsAsync<ArgumentException>(()=>Run(x=>x.CrearAsync(" ","Óptima",null,null)));
        await Assert.ThrowsAsync<ArgumentException>(()=>Run(x=>x.CrearAsync("x","desconocido",null,null)));
        var e=await Assert.ThrowsAsync<PostgresException>(()=>Crear(int.MaxValue)); Assert.Equal("23503",e.SqlState);
    }
    sealed class Interceptor : IHerramientasEscritura
    {
        readonly IHerramientasEscritura r; readonly CancellationTokenSource cts; readonly bool fail;
        public Interceptor(IHerramientasEscritura r,CancellationTokenSource cts,bool fail) { this.r=r;this.cts=cts;this.fail=fail; }
        public Task<Herramienta> BloquearAsync(int id,CancellationToken t=default)=>r.BloquearAsync(id,t);
        public Task<int> CrearAsync(Herramienta h,CancellationToken t=default)=>r.CrearAsync(h,t);
        public async Task ActualizarAsync(Herramienta h,CancellationToken t=default) { await r.ActualizarAsync(h,t); if(fail) throw new InvalidOperationException("Fallo tras update SQL"); cts.Cancel(); }
        public Task RegistrarHistorialAsync(int id,int? a,int? b,CancellationToken t=default)=>r.RegistrarHistorialAsync(id,a,b,t);
        public Task EliminarAsync(int id,CancellationToken t=default)=>r.EliminarAsync(id,t);
    }
    [Theory] [InlineData(true)] [InlineData(false)] public async Task FalloOCancelacionTrasUpdateRealRevierteEstadoYHistorial(bool fail)
    {
        int obra=Obra("rollback-"+Guid.NewGuid().ToString("N")), id=await Crear(obra); using(var cts=new CancellationTokenSource()) {
            if(fail) await Assert.ThrowsAsync<InvalidOperationException>(()=>Run(x=>x.EnviarAGalponAsync(id,cts.Token),wrap:r=>new Interceptor(r,cts,true)));
            else await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>Run(x=>x.EnviarAGalponAsync(id,cts.Token),wrap:r=>new Interceptor(r,cts,false)));
        }
        Assert.Equal(obra,Convert.ToInt32(Campo(id,"id_obra"))); Assert.Equal(1,Hist(id));
    }
    [Fact] public async Task TrasladoEsperaLockRealYConservaHistorial()
    {
        int obra=Obra("lock-"+Guid.NewGuid().ToString("N")), id=await Crear(obra);
        using(var c=new NpgsqlConnection(ConnectionString())) { c.Open(); using(var tx=c.BeginTransaction()) {
            using(var q=new NpgsqlCommand("SELECT id_herramienta FROM public.herramientas WHERE id_herramienta=@id FOR UPDATE",c,tx)) { q.Parameters.AddWithValue("id",id); q.ExecuteScalar(); }
            var trabajo=Run(x=>x.EnviarAGalponAsync(id));
            try { var until=DateTime.UtcNow.AddSeconds(15); int locks=0; do { locks=Convert.ToInt32(Sql("SELECT count(*) FROM pg_stat_activity WHERE datname=current_database() AND wait_event_type='Lock' AND query ILIKE '%herramientas%FOR UPDATE%'")); if(locks>0) break; await Task.Delay(50); } while(DateTime.UtcNow<until); Assert.True(locks>0,"No se observó espera PostgreSQL."); Assert.False(trabajo.IsCompleted); }
            finally { tx.Commit(); }
            await trabajo;
        } }
        Assert.Equal(DBNull.Value,Campo(id,"id_obra")); Assert.Equal(2,Hist(id));
    }
    sealed class FalloSql : IHerramientasEscritura
    {
        readonly IHerramientasEscritura r; readonly bool alta; public int Id;
        public FalloSql(IHerramientasEscritura r,bool alta) { this.r=r; this.alta=alta; }
        public Task<Herramienta> BloquearAsync(int id,CancellationToken t=default)=>r.BloquearAsync(id,t);
        public async Task<int> CrearAsync(Herramienta h,CancellationToken t=default) { Id=await r.CrearAsync(h,t); return Id; }
        public Task ActualizarAsync(Herramienta h,CancellationToken t=default)=>r.ActualizarAsync(h,t);
        public async Task RegistrarHistorialAsync(int id,int? a,int? b,CancellationToken t=default) { await r.RegistrarHistorialAsync(id,a,b,t); if(alta) throw new InvalidOperationException("Fallo luego del historial SQL"); }
        public async Task EliminarAsync(int id,CancellationToken t=default) { await r.EliminarAsync(id,t); if(!alta) throw new InvalidOperationException("Fallo luego del DELETE SQL"); }
    }
    [Fact] public async Task FallosTrasInsertEHistorialYTrasDeleteRevierteTodo()
    {
        FalloSql gate=null;
        await Assert.ThrowsAsync<InvalidOperationException>(()=>Run(x=>x.CrearAsync("fallo-alta","Óptima",null,null),wrap:r=>gate=new FalloSql(r,true)));
        Assert.True(gate.Id>0); Assert.Equal(0,Hist(gate.Id)); Assert.Equal(0L,Convert.ToInt64(Sql("SELECT count(*) FROM public.herramientas WHERE id_herramienta=@id",P("id",gate.Id))));
        int id=await Crear(); Assert.Equal(1,Hist(id));
        await Assert.ThrowsAsync<InvalidOperationException>(()=>Run(x=>x.EliminarAsync(id),wrap:r=>new FalloSql(r,false)));
        Assert.Equal(1,Hist(id)); Assert.Equal(1L,Convert.ToInt64(Sql("SELECT count(*) FROM public.herramientas WHERE id_herramienta=@id",P("id",id))));
    }
    [Fact] public async Task DosRetornosConcurrentesSoloConfirmanUno()
    {
        int obra=Obra("carrera-"+Guid.NewGuid().ToString("N")),id=await Crear(obra);
        using(var c=new NpgsqlConnection(ConnectionString())) { c.Open(); using(var tx=c.BeginTransaction()) {
            using(var q=new NpgsqlCommand("SELECT id_herramienta FROM public.herramientas WHERE id_herramienta=@id FOR UPDATE",c,tx)) { q.Parameters.AddWithValue("id",id); q.ExecuteScalar(); }
            var a=Run(x=>x.EnviarAGalponAsync(id)); var b=Run(x=>x.EnviarAGalponAsync(id));
            try { var until=DateTime.UtcNow.AddSeconds(15); int locks=0; do { locks=Convert.ToInt32(Sql("SELECT count(*) FROM pg_stat_activity WHERE datname=current_database() AND wait_event_type='Lock' AND query ILIKE '%herramientas%FOR UPDATE%'")); if(locks>=2) break; await Task.Delay(50); } while(DateTime.UtcNow<until); Assert.True(locks>=2,"No se observaron ambas esperas."); }
            finally { tx.Commit(); }
            int ok=0, rejected=0; foreach(var task in new[]{a,b}) { try { await task; ok++; } catch(InvalidOperationException) { rejected++; } } Assert.Equal(1,ok); Assert.Equal(1,rejected);
        } }
        Assert.Equal(2,Hist(id)); Assert.Equal(DBNull.Value,Campo(id,"id_obra"));
    }
    [Fact] public async Task LimitesDeNombreYEstadosHistoricos()
    {
        string limite="1"+new string('x',254); int largo=await Run(x=>x.CrearAsync(limite,"Óptima",null,null)); Assert.Equal(limite,Campo(largo,"nombre"));
        string sinPrefijo=new string('z',255); await Assert.ThrowsAsync<ArgumentException>(()=>Run(x=>x.CrearAsync(sinPrefijo,"Óptima",null,null)));
        Assert.Equal(0L,Convert.ToInt64(Sql("SELECT count(*) FROM public.herramientas WHERE nombre=@n",P("n",sinPrefijo))));
        int obra=Obra("legacy-"+Guid.NewGuid().ToString("N")); int id=await Crear(obra); Sql("UPDATE public.herramientas SET estado='histórico' WHERE id_herramienta=@id RETURNING id_herramienta",P("id",id));
        await Run(x=>x.EditarAsync(id,"nombre histórico","histórico",obra,"observación nueva"));
        Assert.Equal("histórico",Campo(id,"estado")); Assert.Equal("observación nueva",Campo(id,"observaciones")); Assert.Equal(1,Hist(id));
        await Run(x=>x.EnviarAGalponAsync(id)); Assert.Equal("histórico",Campo(id,"estado")); Assert.Equal(2,Hist(id)); await Run(x=>x.EliminarAsync(id)); Assert.Equal(0,Hist(id));
    }
}
