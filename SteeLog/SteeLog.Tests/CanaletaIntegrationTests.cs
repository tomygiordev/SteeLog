using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.Core.Application.Canaletas;
using SteeLog.Core.Application.Common;
using SteeLog.Core.Application.Identidad;
using SteeLog.Infrastructure.Canaletas;
using SteeLog.Domain.Canaletas;
using SteeLog.Infrastructure.Files;
using SteeLog.Infrastructure.Persistence;
using System;
using System.Text.RegularExpressions;
using Npgsql;
using Xunit;

[Trait("Category", "Integration")]
public sealed class CanaletaIntegrationTests
{
    static string ConnectionString()
    {
        var cs = Environment.GetEnvironmentVariable("STEELOG_CANALETAS_TEST_CONNECTION");
        var db = Environment.GetEnvironmentVariable("STEELOG_CANALETAS_TEST_DB");
        if (string.IsNullOrWhiteSpace(cs) || !Regex.IsMatch(db ?? "", "^steelog_test_[0-9a-f]{32}$")) throw new InvalidOperationException("Se requiere el runner aislado de canaletas.");
        var b = new NpgsqlConnectionStringBuilder(cs);
        if (b.Host != "127.0.0.1" || b.Database != db || b.Port <= 0 || b.Port == 5435) throw new InvalidOperationException("Destino no permitido.");
        using (var c = new NpgsqlConnection(cs)) { c.Open(); using (var q = new NpgsqlCommand("SELECT current_database()", c)) if ((string)q.ExecuteScalar() != db) throw new InvalidOperationException("DB incorrecta."); }
        return cs;
    }
    static object Sql(string text, params NpgsqlParameter[] args)
    {
        using (var c = new NpgsqlConnection(ConnectionString())) { c.Open(); using (var q = new NpgsqlCommand(text, c)) { q.Parameters.AddRange(args); return q.ExecuteScalar(); } }
    }
    static NpgsqlParameter P(string name, object value) => new NpgsqlParameter(name, value);
    static int Obra() => Convert.ToInt32(Sql("INSERT INTO public.obras(nombre) VALUES (@n) RETURNING id", P("n", Guid.NewGuid().ToString("N"))));
    static int Canaleta(int obra) => Convert.ToInt32(Sql("INSERT INTO public.canaletas_plegados(id_obra,descripcion,precio) VALUES (@o,'prueba',10) RETURNING id", P("o", obra)));
    static object Foto(int id, string bucket, string path, string mime, long size, int orden = 1) => Sql("INSERT INTO public.canaletas_plegados_fotos(canaleta_plegado_id,bucket_id,object_path,nombre_archivo,content_type,tamanio_bytes,orden) VALUES (@id,@b,@p,'foto',@m,@s,@o) RETURNING id", P("id",id), P("b",bucket), P("p",path), P("m",mime), P("s",size), P("o",orden));
    static void Constraint(string state, Action action) => Assert.Equal(state, Assert.Throws<PostgresException>(action).SqlState);

    [Fact] public void ConstraintsRealesDeCabeceraYReferencia()
    {
        int obra = Obra();
        Constraint("23514", () => Sql("INSERT INTO public.canaletas_plegados(id_obra,descripcion,precio) VALUES (@o,'   ',0)", P("o", obra)));
        Constraint("23514", () => Sql("INSERT INTO public.canaletas_plegados(id_obra,descripcion,precio) VALUES (@o,'válida',-1)", P("o", obra)));
        Constraint("23503", () => Sql("INSERT INTO public.canaletas_plegados(id_obra,descripcion,precio) VALUES (-1,'válida',0)"));
        int id = Canaleta(obra);
        Constraint("23503", () => Sql("DELETE FROM public.obras WHERE id=@o", P("o", obra)));
        Assert.Equal(1L, Convert.ToInt64(Sql("SELECT count(*) FROM public.canaletas_plegados WHERE id=@id", P("id", id))));
    }
    [Fact] public void ConstraintsRealesDeFotoYEliminacionEnCascada()
    {
        int id = Canaleta(Obra()); string key = Guid.NewGuid().ToString("N");
        foreach (var mime in new[] { "image/jpeg", "image/png", "image/webp" }) Foto(id,"canaletas-fotos",key+mime,mime,8388608);
        Foto(id,"canaletas-fotos",key,"image/png",0);
        Constraint("23514", () => Foto(id,"otro",key+"b","image/png",1));
        Constraint("23514", () => Foto(id,"canaletas-fotos",key+"m","image/gif",1));
        Constraint("23514", () => Foto(id,"canaletas-fotos",key+"s","image/png",8388609));
        Constraint("23514", () => Foto(id,"canaletas-fotos",key+"n","image/png",-1));
        Constraint("23505", () => Foto(id,"canaletas-fotos",key,"image/png",1));
        Constraint("23503", () => Foto(-1,"canaletas-fotos",key+"fk","image/png",1));
        Sql("DELETE FROM public.canaletas_plegados WHERE id=@id",P("id",id));
        Assert.Equal(0L,Convert.ToInt64(Sql("SELECT count(*) FROM public.canaletas_plegados_fotos WHERE canaleta_plegado_id=@id",P("id",id))));
    }
    static Session Actor(Role role = Role.Galpon) { var s = new Session(); if (role != Role.None) s.Start(new Identity("test@example.invalid", role)); return s; }
    static PostgresConnectionFactory Factory() => new PostgresConnectionFactory(ConnectionString());
    static byte[] Png => Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/aXQAAAAASUVORK5CYII=");
    static string Root() { var p = Path.Combine(Path.GetTempPath(), "canaleta-files-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(p); return p; }
    static void RemoveRoot(string root) { var parent = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar; var full = Path.GetFullPath(root); if (!full.StartsWith(parent, StringComparison.OrdinalIgnoreCase) || !Regex.IsMatch(Path.GetFileName(full), "^canaleta-files-[0-9a-f]{32}$")) throw new InvalidOperationException("Directorio de prueba inválido."); Directory.Delete(full,true); }
    static async Task<T> Run<T>(IArchivosCanaleta files, Func<OperacionesCanaleta, Task<T>> action, Func<IUnitOfWork,IUnitOfWork> wrap = null, ICanaletasConsulta query = null, Role role = Role.Galpon, Func<ICanaletasEscritura,ICanaletasEscritura> wrapRepo = null)
    {
        using (var u = new NpgsqlUnitOfWork(Factory())) { var q = query ?? new NpgsqlCanaletasConsulta(Factory()); ICanaletasEscritura repo = new NpgsqlCanaletas(u); return await action(new OperacionesCanaleta(Actor(role), wrap == null ? u : wrap(u), wrapRepo == null ? repo : wrapRepo(repo), q, files)); }
    }
    sealed class CommitIncierto : IUnitOfWork
    {
        readonly IUnitOfWork inner; readonly bool despues;
        public CommitIncierto(IUnitOfWork inner, bool despues) { this.inner=inner; this.despues=despues; }
        public Task BeginAsync(CancellationToken t=default) => inner.BeginAsync(t);
        public async Task CommitAsync(CancellationToken t=default) { if (despues) await inner.CommitAsync(t); throw new InvalidOperationException("Commit interrumpido"); }
        public Task RollbackAsync() => inner.RollbackAsync();
    }
    sealed class ConsultaInterrumpida : ICanaletasConsulta
    {
        readonly ICanaletasConsulta inner;
        public ConsultaInterrumpida(ICanaletasConsulta inner) { this.inner=inner; }
        public Task<IReadOnlyList<CanaletaLectura>> ListarAsync(int? id,CancellationToken t) => inner.ListarAsync(id,t);
        public Task<CanaletaLectura> ObtenerAsync(int id,CancellationToken t) => inner.ObtenerAsync(id,t);
        public Task<IReadOnlyList<FotoCanaletaLectura>> FotosAsync(int id,CancellationToken t) => inner.FotosAsync(id,t);
        public Task<bool> ReferenciadaAsync(string p,CancellationToken t) => throw new IOException("Consulta interrumpida");
        public Task<FotoCanaletaLectura> FotoPorPathAsync(string p,CancellationToken t) => throw new IOException("Consulta interrumpida");
    }
    [Fact] public async Task AltaEdicionAdjuntoLecturaOrdenYEliminacion()
    {
        string root=Root(); try {
            var files=new LocalCanaletaStorage(root); int obra=Obra();
            int id=await Run(files,o=>o.GuardarAsync(null,obra,"  original  ",12.50m,false));
            Assert.Equal(id,await Run(files,o=>o.GuardarAsync(id,obra,"editada",25.25m,true)));
            var query=new ConsultarCanaletas(Actor(),new NpgsqlCanaletasConsulta(Factory()),files);
            var row=await query.ObtenerAsync(id); Assert.Equal("editada",row.Descripcion); Assert.Equal(25.25m,row.Precio); Assert.True(row.Pagado);
            Assert.Contains(await query.ListarAsync(obra),x=>x.Id==id); Assert.DoesNotContain(await query.ListarAsync(Obra()),x=>x.Id==id);
            int f1=await Run(files,o=>o.AdjuntarAsync(id,"primera.png",Png)); int f2=await Run(files,o=>o.AdjuntarAsync(id,"segunda.png",Png));
            var fotos=await query.FotosAsync(id); Assert.Equal(new[]{f1,f2},fotos.Select(x=>x.Id)); Assert.Equal(new[]{1,2},fotos.Select(x=>x.Orden));
            Assert.Equal("primera.png",fotos[0].NombreArchivo); Assert.Equal("image/png",fotos[0].ContentType); Assert.Equal(Png.Length,fotos[0].TamanioBytes); Assert.Equal(Png,await query.LeerFotoAsync(fotos[0].ObjectPath));
            Assert.Equal(2,(await query.ObtenerAsync(id)).CantidadFotos);
            await Run(files,o=>o.EliminarFotoAsync(f1)); Assert.Single(await query.FotosAsync(id));
            await Run(files,o=>o.EliminarAsync(id)); Assert.Null(await query.ObtenerAsync(id)); Assert.Empty(await query.FotosAsync(id));
        } finally { RemoveRoot(root); }
    }
    [Fact] public async Task CommitInciertoAntesYDespuesConsultaConexionNueva()
    {
        string root=Root(); try {
            var files=new LocalCanaletaStorage(root); int id=await Run(files,o=>o.GuardarAsync(null,Obra(),"x",1,false)); var consulta=new NpgsqlCanaletasConsulta(Factory());
            await Assert.ThrowsAsync<CanaletaResultadoInciertoException>(()=>Run(files,o=>o.AdjuntarAsync(id,"antes.png",Png),u=>new CommitIncierto(u,false)));
            Assert.Empty(await consulta.FotosAsync(id,CancellationToken.None)); Assert.Empty(await files.PendientesAsync(CancellationToken.None));
            int confirmado=await Run(files,o=>o.AdjuntarAsync(id,"despues.png",Png),u=>new CommitIncierto(u,true));
            var foto=Assert.Single(await consulta.FotosAsync(id,CancellationToken.None)); Assert.Equal(confirmado,foto.Id); Assert.Equal(Png,await files.LeerAsync(foto.ObjectPath,CancellationToken.None)); Assert.Empty(await files.PendientesAsync(CancellationToken.None));
        } finally { RemoveRoot(root); }
    }
    [Fact] public async Task IncertidumbreIrresolubleConservaArchivoHastaConciliacion()
    {
        string root=Root(); try {
            var files=new LocalCanaletaStorage(root); int id=await Run(files,o=>o.GuardarAsync(null,Obra(),"x",1,false)); var consulta=new NpgsqlCanaletasConsulta(Factory());
            await Assert.ThrowsAsync<CanaletaResultadoInciertoException>(()=>Run(files,o=>o.AdjuntarAsync(id,"x.png",Png),u=>new CommitIncierto(u,true),new ConsultaInterrumpida(consulta))); await Assert.ThrowsAsync<IOException>(()=>Run(files,async o=>{ await o.ConciliarPendientesAsync(); return true; },query:new ConsultaInterrumpida(consulta)));
            var pending=Assert.Single(await files.PendientesAsync(CancellationToken.None)); var foto=Assert.Single(await consulta.FotosAsync(id,CancellationToken.None)); Assert.Equal(foto.ObjectPath,pending); Assert.Equal(Png,await files.LeerAsync(pending,CancellationToken.None));
            await Run(files,async o=>{ await o.ConciliarPendientesAsync(); return true; }); Assert.Empty(await files.PendientesAsync(CancellationToken.None)); Assert.Equal(Png,await files.LeerAsync(pending,CancellationToken.None));
        } finally { RemoveRoot(root); }
    }
    [Fact] public async Task CancelacionTrasSqlRevierteYCompensa()
    {
        string root=Root(); try {
            var files=new LocalCanaletaStorage(root); int id=await Run(files,o=>o.GuardarAsync(null,Obra(),"x",1,false));
            using(var cts=new CancellationTokenSource()) await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>Run(files,o=>o.AdjuntarAsync(id,"foto.png",Png,cts.Token),wrapRepo:r=>new CancelarRepo(r,cts)));
            Assert.Empty(await new NpgsqlCanaletasConsulta(Factory()).FotosAsync(id,CancellationToken.None)); Assert.Empty(await files.PendientesAsync(CancellationToken.None));
        } finally { RemoveRoot(root); }
    }
    [Fact] public async Task AdjuntosConcurrentesSerializanOrdenYPermisos()
    {
        string root=Root(); try {
            var files=new LocalCanaletaStorage(root); int id=await Run(files,o=>o.GuardarAsync(null,Obra(),"x",1,false));
            await Task.WhenAll(Run(files,o=>o.AdjuntarAsync(id,"a.png",Png)),Run(files,o=>o.AdjuntarAsync(id,"b.png",Png)));
            var q=new NpgsqlCanaletasConsulta(Factory()); var fotos=await q.FotosAsync(id,CancellationToken.None); Assert.Equal(new[]{1,2},fotos.Select(x=>x.Orden));
            await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>Run(files,o=>o.GuardarAsync(id,Obra(),"no",1,false),role:Role.None));
            var no=new ConsultarCanaletas(Actor(Role.None),q,files); Assert.Throws<UnauthorizedAccessException>(()=>{ _=no.ListarAsync(null); });
        } finally { RemoveRoot(root); }
    }
    [Fact] public async Task RechazaMimeYTamanoAntesDeEscribir()
    {
        string root=Root(); try {
            var files=new LocalCanaletaStorage(root); int id=await Run(files,o=>o.GuardarAsync(null,Obra(),"x",1,false));
            await Assert.ThrowsAsync<ArgumentException>(()=>Run(files,o=>o.AdjuntarAsync(id,"x.gif",new byte[]{1,2,3})));
            await Assert.ThrowsAsync<ArgumentException>(()=>Run(files,o=>o.AdjuntarAsync(id,"x.png",new byte[8388609])));
            Assert.Empty(await new NpgsqlCanaletasConsulta(Factory()).FotosAsync(id,CancellationToken.None)); Assert.Empty(await files.PendientesAsync(CancellationToken.None));
        } finally { RemoveRoot(root); }
    }
    sealed class CancelarRepo : ICanaletasEscritura
    {
        readonly ICanaletasEscritura inner; readonly CancellationTokenSource cts;
        public CancelarRepo(ICanaletasEscritura inner,CancellationTokenSource cts) { this.inner=inner; this.cts=cts; }
        public Task<CanaletaPlegado> BloquearAsync(int id,CancellationToken t)=>inner.BloquearAsync(id,t);
        public Task<int> GuardarAsync(CanaletaPlegado x,CancellationToken t)=>inner.GuardarAsync(x,t);
        public Task<int> MaximoOrdenAsync(int id,CancellationToken t)=>inner.MaximoOrdenAsync(id,t);
        public async Task<int> AdjuntarAsync(int id,string p,string n,string m,long b,int o,CancellationToken t) { int r=await inner.AdjuntarAsync(id,p,n,m,b,o,t); cts.Cancel(); return r; }
        public Task<FotoCanaletaLectura> BloquearFotoAsync(int id,CancellationToken t)=>inner.BloquearFotoAsync(id,t);
        public Task<IReadOnlyList<FotoCanaletaLectura>> FotosAsync(int id,CancellationToken t)=>inner.FotosAsync(id,t);
        public Task EliminarFotoAsync(int id,CancellationToken t)=>inner.EliminarFotoAsync(id,t);
        public Task EliminarAsync(int id,CancellationToken t)=>inner.EliminarAsync(id,t);
    }
    sealed class FalloLimpieza : IArchivosCanaleta
    {
        readonly IArchivosCanaleta inner;
        public FalloLimpieza(IArchivosCanaleta inner) { this.inner=inner; }
        public Task<string> PrepararAsync(int id,byte[] b,string e,CancellationToken t)=>inner.PrepararAsync(id,b,e,t);
        public Task<byte[]> LeerAsync(string p,CancellationToken t)=>inner.LeerAsync(p,t);
        public Task PendienteAsync(string p,CancellationToken t)=>inner.PendienteAsync(p,t);
        public Task CompletarAsync(string p,bool borrar,CancellationToken t) { if(borrar) throw new IOException("Fallo de disco simulado"); return inner.CompletarAsync(p,borrar,t); }
        public Task<IReadOnlyList<string>> PendientesAsync(CancellationToken t)=>inner.PendientesAsync(t);
        public Task<bool> TomarPendienteAsync(string p,CancellationToken t)=>inner.TomarPendienteAsync(p,t);
        public Task LiberarAsync(string p)=>inner.LiberarAsync(p);
    }
    [Fact] public async Task LimpiezaFallidaQuedaPendienteYLegacyReferenciadaSeConserva()
    {
        string root=Root(); try {
            var files=new LocalCanaletaStorage(root); int id=await Run(files,o=>o.GuardarAsync(null,Obra(),"x",1,false));
            string legacy="legacy/"+Guid.NewGuid().ToString("N")+".png"; string legacyFull=Path.Combine(root,legacy.Replace('/',Path.DirectorySeparatorChar)); Directory.CreateDirectory(Path.GetDirectoryName(legacyFull)); File.WriteAllBytes(legacyFull,Png);
            Foto(id,"canaletas-fotos",legacy,"image/png",Png.Length); int f=await Run(files,o=>o.AdjuntarAsync(id,"nueva.png",Png));
            var q=new ConsultarCanaletas(Actor(),new NpgsqlCanaletasConsulta(Factory()),files); var nueva=(await q.FotosAsync(id)).Single(x=>x.Id==f); Assert.Equal(Png,await q.LeerFotoAsync(legacy));
            var ex=await Assert.ThrowsAsync<CanaletaLimpiezaPendienteException>(()=>Run(new FalloLimpieza(files),o=>o.EliminarFotoAsync(f))); Assert.True(ex.OperacionConfirmada); Assert.Equal(nueva.ObjectPath,ex.ObjectPath);
            Assert.Single(await q.FotosAsync(id)); Assert.Equal(Png,await files.LeerAsync(nueva.ObjectPath,CancellationToken.None)); Assert.Contains(nueva.ObjectPath,await files.PendientesAsync(CancellationToken.None));
            await files.PendienteAsync(legacy,CancellationToken.None); await files.LiberarAsync(legacy);
            await Run(files,async o=>{ await o.ConciliarPendientesAsync(); return true; }); Assert.Empty(await files.PendientesAsync(CancellationToken.None)); Assert.Empty(await files.LeerAsync(nueva.ObjectPath,CancellationToken.None)); Assert.Equal(Png,await q.LeerFotoAsync(legacy));
        } finally { RemoveRoot(root); }
    }
    [Fact]
    public async Task EliminarFotoConAliasReferenciadoPorOtraCanaletaConservaElArchivo()
    {
        string root = Root();
        try
        {
            var files = new LocalCanaletaStorage(root);
            int origen = await Run(files, o => o.GuardarAsync(null, Obra(), "origen", 1m, false));
            int otro = await Run(files, o => o.GuardarAsync(null, Obra(), "otro", 2m, false));
            int fotoId = await Run(files, o => o.AdjuntarAsync(origen, "original.png", Png));
            var consulta = new NpgsqlCanaletasConsulta(Factory());
            var foto = Assert.Single(await consulta.FotosAsync(origen, CancellationToken.None));
            string alias = foto.ObjectPath.ToUpperInvariant().Replace('/', '\\');
            Foto(otro, "canaletas-fotos", alias, "image/png", Png.Length);

            await Run(files, o => o.EliminarFotoAsync(fotoId));

            Assert.Empty(await consulta.FotosAsync(origen, CancellationToken.None));
            Assert.Single(await consulta.FotosAsync(otro, CancellationToken.None));
            Assert.Equal(Png, await files.LeerAsync(foto.ObjectPath, CancellationToken.None));
            var lectura = new ConsultarCanaletas(Actor(), consulta, files);
            Assert.Equal(Png, await lectura.LeerFotoAsync(alias));
            Assert.Empty(await files.PendientesAsync(CancellationToken.None));
        }
        finally { RemoveRoot(root); }
    }

    [Fact] public async Task AdjuntoEsperaBloqueoDeCabeceraEnPostgres()
    {
        string root=Root(); try {
            var files=new LocalCanaletaStorage(root); int id=await Run(files,o=>o.GuardarAsync(null,Obra(),"x",1,false));
            using(var c=new NpgsqlConnection(ConnectionString())) { c.Open(); using(var tx=c.BeginTransaction()) {
                using(var q=new NpgsqlCommand("SELECT id FROM public.canaletas_plegados WHERE id=@id FOR UPDATE",c,tx)) { q.Parameters.AddWithValue("id",id); q.ExecuteScalar(); }
                var adjunto=Run(files,o=>o.AdjuntarAsync(id,"espera.png",Png));
                try { var hasta=DateTime.UtcNow.AddSeconds(15); int espera=0; do { espera=Convert.ToInt32(Sql("SELECT count(*) FROM pg_stat_activity WHERE datname=current_database() AND wait_event_type='Lock' AND query ILIKE '%canaletas_plegados%FOR UPDATE%'")); if(espera>0) break; await Task.Delay(40); } while(DateTime.UtcNow<hasta); Assert.True(espera>0,"El adjunto no esperó el bloqueo de cabecera."); Assert.False(adjunto.IsCompleted); } finally { tx.Commit(); }
                Assert.True(await adjunto>0);
            } }
        } finally { RemoveRoot(root); }
    }
    [Fact] public async Task EliminacionesConCommitInciertoConservanReferenciasYArchivosCorrectos()
    {
        foreach (bool canaleta in new[] { false, true }) foreach (bool despues in new[] { false, true })
        {
            string root = Root();
            try
            {
                var files = new LocalCanaletaStorage(root);
                int obra = Obra();
                int id = await Run(files, o => o.GuardarAsync(null, obra, "principal", 1, false));
                int otro = await Run(files, o => o.GuardarAsync(null, obra, "otra", 1, false));
                int fotoId = await Run(files, o => o.AdjuntarAsync(id, "principal.png", Png));
                await Run(files, o => o.AdjuntarAsync(otro, "otra.png", Png));
                var consulta = new NpgsqlCanaletasConsulta(Factory());
                var foto = Assert.Single(await consulta.FotosAsync(id, CancellationToken.None));
                var ajena = Assert.Single(await consulta.FotosAsync(otro, CancellationToken.None));
                await Assert.ThrowsAsync<CanaletaResultadoInciertoException>(() => Run(files,
                    o => canaleta ? o.EliminarAsync(id) : o.EliminarFotoAsync(fotoId),
                    u => new CommitIncierto(u, despues)));
                var filas = await consulta.FotosAsync(id, CancellationToken.None);
                if (despues)
                {
                    Assert.Empty(filas);
                    Assert.Empty(await files.LeerAsync(foto.ObjectPath, CancellationToken.None));
                    Assert.Empty(await files.PendientesAsync(CancellationToken.None));
                }
                else
                {
                    Assert.Single(filas);
                    Assert.Equal(Png, await files.LeerAsync(foto.ObjectPath, CancellationToken.None));
                    Assert.Contains(foto.ObjectPath, await files.PendientesAsync(CancellationToken.None));
                }
                Assert.Equal(!canaleta || !despues, (await consulta.ObtenerAsync(id, CancellationToken.None)) != null);
                Assert.Single(await consulta.FotosAsync(otro, CancellationToken.None));
                Assert.Equal(Png, await files.LeerAsync(ajena.ObjectPath, CancellationToken.None));
                await Run(files, async o => { await o.ConciliarPendientesAsync(); return true; });
                Assert.Empty(await files.PendientesAsync(CancellationToken.None));
                Assert.Equal(Png, await files.LeerAsync(ajena.ObjectPath, CancellationToken.None));
                if (!despues) Assert.Equal(Png, await files.LeerAsync(foto.ObjectPath, CancellationToken.None));
            }
            finally { RemoveRoot(root); }
        }
    }
    sealed class InterceptarAdjunto : ICanaletasEscritura
    {
        readonly ICanaletasEscritura inner; readonly bool fallar;
        public readonly TaskCompletionSource<bool> Entrado = new TaskCompletionSource<bool>();
        public readonly TaskCompletionSource<bool> Seguir = new TaskCompletionSource<bool>();
        public InterceptarAdjunto(ICanaletasEscritura inner, bool fallar) { this.inner = inner; this.fallar = fallar; }
        public Task<CanaletaPlegado> BloquearAsync(int id, CancellationToken t) => inner.BloquearAsync(id,t);
        public Task<int> GuardarAsync(CanaletaPlegado x, CancellationToken t) => inner.GuardarAsync(x,t);
        public Task<int> MaximoOrdenAsync(int id, CancellationToken t) => inner.MaximoOrdenAsync(id,t);
        public async Task<int> AdjuntarAsync(int id, string p, string n, string m, long b, int o, CancellationToken t)
        {
            var result = await inner.AdjuntarAsync(id,p,n,m,b,o,t);
            Entrado.TrySetResult(true);
            if (fallar) throw new IOException("Fallo después del INSERT SQL");
            await Seguir.Task;
            return result;
        }
        public Task<FotoCanaletaLectura> BloquearFotoAsync(int id, CancellationToken t) => inner.BloquearFotoAsync(id,t);
        public Task<IReadOnlyList<FotoCanaletaLectura>> FotosAsync(int id, CancellationToken t) => inner.FotosAsync(id,t);
        public Task EliminarFotoAsync(int id, CancellationToken t) => inner.EliminarFotoAsync(id,t);
        public Task EliminarAsync(int id, CancellationToken t) => inner.EliminarAsync(id,t);
    }
    [Fact] public async Task FalloDespuesDeInsertRealRevierteMetadataYArchivo()
    {
        string root = Root();
        try
        {
            var files = new LocalCanaletaStorage(root);
            int id = await Run(files, o => o.GuardarAsync(null, Obra(), "x", 1, false));
            await Assert.ThrowsAsync<IOException>(() => Run(files, o => o.AdjuntarAsync(id, "fallo.png", Png),
                wrapRepo: r => new InterceptarAdjunto(r, true)));
            Assert.Empty(await new NpgsqlCanaletasConsulta(Factory()).FotosAsync(id, CancellationToken.None));
            Assert.Empty(await files.PendientesAsync(CancellationToken.None));
            Assert.Empty(Directory.GetFiles(root, "*.png", SearchOption.AllDirectories));
        }
        finally { RemoveRoot(root); }
    }
    [Fact] public async Task ConciliacionConcurrenteNoTocaArchivoDeAdjuntoActivo()
    {
        string root = Root();
        try
        {
            var files = new LocalCanaletaStorage(root);
            int id = await Run(files, o => o.GuardarAsync(null, Obra(), "x", 1, false));
            InterceptarAdjunto gate = null;
            var adjunto = Run(files, o => o.AdjuntarAsync(id, "activa.png", Png),
                wrapRepo: r => gate = new InterceptarAdjunto(r, false));
            var limite = DateTime.UtcNow.AddSeconds(15);
            while (gate == null && DateTime.UtcNow < limite) await Task.Delay(10);
            Assert.NotNull(gate);
            try
            {
                Assert.Same(gate.Entrado.Task, await Task.WhenAny(gate.Entrado.Task, Task.Delay(15000)));
                var path = Assert.Single(await files.PendientesAsync(CancellationToken.None));
                Assert.Equal(Png, await files.LeerAsync(path, CancellationToken.None));
                foreach (var storage in new IArchivosCanaleta[] { files, new LocalCanaletaStorage(root) })
                {
                    await Run(storage, async o => { await o.ConciliarPendientesAsync(); return true; });
                    Assert.Contains(path, await files.PendientesAsync(CancellationToken.None));
                    Assert.Equal(Png, await files.LeerAsync(path, CancellationToken.None));
                }
            }
            finally { gate.Seguir.TrySetResult(true); }
            await adjunto;
            var foto = Assert.Single(await new NpgsqlCanaletasConsulta(Factory()).FotosAsync(id, CancellationToken.None));
            Assert.Equal(Png, await files.LeerAsync(foto.ObjectPath, CancellationToken.None));
            Assert.Empty(await files.PendientesAsync(CancellationToken.None));
        }
        finally { RemoveRoot(root); }
    }
}
