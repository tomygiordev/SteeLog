using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Npgsql;
using SteeLog.Core.Application.Catalogos;
using SteeLog.Core.Application.Identidad;
using SteeLog.Infrastructure.Catalogos;
using SteeLog.Infrastructure.Movimientos;
using SteeLog.Infrastructure.Persistence;
using Xunit;

[Trait("Category", "Integration")]
public sealed class CatalogosIntegrationTests
{
    static string ConnectionString()
    {
        var cs=Environment.GetEnvironmentVariable("STEELOG_F7_TEST_CONNECTION"); var db=Environment.GetEnvironmentVariable("STEELOG_F7_TEST_DB");
        if(string.IsNullOrWhiteSpace(cs)||!Regex.IsMatch(db??"", "^steelog_test_[0-9a-f]{32}$")) throw new InvalidOperationException("Se requiere runner aislado F7.");
        var b=new NpgsqlConnectionStringBuilder(cs); if(b.Host!="127.0.0.1"||b.Database!=db||b.Port<=0||b.Port==5435) throw new InvalidOperationException("Destino no permitido.");
        using(var c=new NpgsqlConnection(cs)) { c.Open(); using(var q=new NpgsqlCommand("SELECT current_database()",c)) if((string)q.ExecuteScalar()!=db) throw new InvalidOperationException("DB incorrecta."); } return cs;
    }
    static PostgresConnectionFactory Factory()=>new PostgresConnectionFactory(ConnectionString());
    static Session Actor(Role role=Role.Galpon) { var s=new Session(); if(role!=Role.None) s.Start(new Identity("test@example.invalid",role)); return s; }
    static async Task<T> Run<T>(Func<GestionarCatalogos,Task<T>> f,Role role=Role.Galpon) { using(var u=new NpgsqlUnitOfWork(Factory())) return await f(new GestionarCatalogos(Actor(role),u,new NpgsqlCatalogos(u),new NpgsqlFletes(u))); }
    static ConsultarCatalogos Query(Role role=Role.Galpon)=>new ConsultarCatalogos(Actor(role),new NpgsqlCatalogosConsulta(Factory()));
    static object Sql(string sql,params NpgsqlParameter[] args) { using(var c=new NpgsqlConnection(ConnectionString())) { c.Open(); using(var q=new NpgsqlCommand(sql,c)) { q.Parameters.AddRange(args); return q.ExecuteScalar(); } } }
    static NpgsqlParameter P(string name,object value)=>new NpgsqlParameter(name,value);
    static string Unique()=>Guid.NewGuid().ToString("N");

    [Fact] public async Task CrudTipadoDeTodosLosCatalogos()
    {
        var n=Unique(); int c=await Run(x=>x.CrearCategoriaAsync(n,true)); int o=await Run(x=>x.CrearObraAsync(n,true)); int p=await Run(x=>x.CrearProveedorAsync(n)); int f=await Run(x=>x.CrearFleteAsync(n,true)); int u=await Run(x=>x.CrearUbicacionAsync(n));
        await Run(x=>x.EditarCategoriaAsync(c,n+"c",false)); await Run(x=>x.EditarObraAsync(o,n+"o",false)); await Run(x=>x.EditarProveedorAsync(p,n+"p")); await Run(x=>x.EditarFleteAsync(f,n+"f",false)); await Run(x=>x.EditarUbicacionAsync(u,n+"u"));
        var q=Query(); var cat=Assert.Single((await q.CategoriasAsync()).Where(x=>x.Id==c)); Assert.Equal(n+"c",cat.Nombre); Assert.False(cat.Recortable);
        var obra=Assert.Single((await q.ObrasAsync()).Where(x=>x.Id==o)); Assert.Equal(n+"o",obra.Nombre); Assert.False(obra.Activa);
        Assert.Equal(n+"p",Assert.Single((await q.ProveedoresAsync()).Where(x=>x.Id==p)).Nombre);
        var flete=Assert.Single((await q.FletesAsync()).Where(x=>x.Id==f)); Assert.Equal(n+"f",flete.Nombre); Assert.False(flete.Activo);
        Assert.Equal(n+"u",Assert.Single((await q.UbicacionesAsync()).Where(x=>x.Id==u)).Nombre);
        await Run(x=>x.EliminarCategoriaAsync(c)); await Run(x=>x.EliminarObraAsync(o)); await Run(x=>x.EliminarProveedorAsync(p)); await Run(x=>x.DesactivarFleteAsync(f)); await Run(x=>x.EliminarUbicacionAsync(u));
        Assert.DoesNotContain(await q.CategoriasAsync(),x=>x.Id==c); Assert.DoesNotContain(await q.ObrasAsync(),x=>x.Id==o); Assert.DoesNotContain(await q.ProveedoresAsync(),x=>x.Id==p); Assert.DoesNotContain(await q.UbicacionesAsync(),x=>x.Id==u);
        Assert.False(Convert.ToBoolean(Sql("SELECT activo FROM public.fletes WHERE id_flete=@id",P("id",f))));
    }
    [Fact] public async Task PermisosYLimitesValidanAntesDeGuardar()
    {
        Assert.Throws<UnauthorizedAccessException>(()=>{ _=Query(Role.None).CategoriasAsync(); });
        await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>Run(x=>x.CrearCategoriaAsync("x",false),Role.None));
        await Assert.ThrowsAsync<ArgumentException>(()=>Run(x=>x.CrearCategoriaAsync(new string('x',51),false)));
        await Assert.ThrowsAsync<ArgumentException>(()=>Run(x=>x.CrearObraAsync(new string('x',101),true)));
        await Assert.ThrowsAsync<ArgumentException>(()=>Run(x=>x.CrearProveedorAsync(" ")));
        await Assert.ThrowsAsync<ArgumentException>(()=>Run(x=>x.CrearFleteAsync(new string('x',51),true)));
        await Assert.ThrowsAsync<ArgumentException>(()=>Run(x=>x.CrearUbicacionAsync(new string('x',101))));
        int id=await Run(x=>x.CrearCategoriaAsync(new string('a',50),true));
        // Recorrido F9: el duplicado se traduce desde la UNIQUE real (23505) a un mensaje de negocio, sin SQL crudo.
        var dup=await Assert.ThrowsAsync<CatalogoDuplicadoException>(()=>Run(x=>x.CrearCategoriaAsync(new string('a',50),false)));
        Assert.Equal("23505",Assert.IsType<PostgresException>(dup.InnerException).SqlState); Assert.DoesNotContain("duplicate key",dup.Message);
        await Run(x=>x.EliminarCategoriaAsync(id));
    }
    [Fact] public async Task FkRealImpideEliminarCategoriaReferenciadaYConservaDatos()
    {
        int c=await Run(x=>x.CrearCategoriaAsync(Unique(),false));
        int m=Convert.ToInt32(Sql("INSERT INTO public.materiales(nombre,preciounitario,stockactual,stockminimo,id_categoria) VALUES (@n,0,0,0,@c) RETURNING id",P("n",Unique()),P("c",c)));
        // La FK real (23503) se traduce a un mensaje de negocio, sin SQL crudo.
        var error=await Assert.ThrowsAsync<CatalogoEnUsoException>(()=>Run(x=>x.EliminarCategoriaAsync(c))); Assert.Equal("23503",Assert.IsType<PostgresException>(error.InnerException).SqlState);
        Assert.Equal("No se puede eliminar la categoría porque está en uso (materiales).",error.Message); Assert.DoesNotContain("violates",error.Message);
        Assert.Equal(1L,Convert.ToInt64(Sql("SELECT count(*) FROM public.categorias WHERE id_categoria=@id",P("id",c))));
        Sql("DELETE FROM public.materiales WHERE id=@id RETURNING id",P("id",m)); await Run(x=>x.EliminarCategoriaAsync(c));
    }
    [Fact] public async Task UbicacionUsadaEnMovimientoNoSePuedeEliminar()
    {
        int u=await Run(x=>x.CrearUbicacionAsync(Unique()));
        int mov=Convert.ToInt32(Sql("INSERT INTO public.movimientos(fecha,nroremito,id_origen,tipo_origen,id_destino,tipo_destino,precio_total,id_tipomovimiento) VALUES (CURRENT_DATE,@r,@u,'UBICACION',@u,'UBICACION',0,2) RETURNING id_movimiento",P("r",Unique()),P("u",u)));
        try
        {
            var error=await Assert.ThrowsAsync<CatalogoEnUsoException>(()=>Run(x=>x.EliminarUbicacionAsync(u)));
            Assert.Equal("No se puede eliminar la ubicación porque está en uso (movimientos).",error.Message);
            Assert.Equal(1L,Convert.ToInt64(Sql("SELECT count(*) FROM public.ubicacion WHERE id_ubicacion=@id",P("id",u))));
        }
        finally { Sql("DELETE FROM public.movimientos WHERE id_movimiento=@id RETURNING id_movimiento",P("id",mov)); }
        await Run(x=>x.EliminarUbicacionAsync(u));
        Assert.Equal(0L,Convert.ToInt64(Sql("SELECT count(*) FROM public.ubicacion WHERE id_ubicacion=@id",P("id",u))));
    }
    [Fact] public async Task AltaFleteInactivoPersisteAmbasOperaciones()
    {
        string n=Unique(); int id=await Run(x=>x.CrearFleteAsync(n,false));
        Assert.Equal(n,Sql("SELECT nombre FROM public.fletes WHERE id_flete=@id",P("id",id)));
        Assert.False(Convert.ToBoolean(Sql("SELECT activo FROM public.fletes WHERE id_flete=@id",P("id",id))));
    }}
