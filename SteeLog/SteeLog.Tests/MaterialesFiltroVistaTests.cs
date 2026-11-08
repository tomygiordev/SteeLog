using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SteeLog.Core.Application.Identidad;
using SteeLog.Core.Application.Inventario;
using SteeLog.Modelos;
using SteeLog.Presentation;
using SteeLog.Vistas;
using Xunit;

namespace SteeLog.Tests
{
    public sealed class MaterialesFiltroVistaTests
    {
        private sealed class Consulta : IMaterialesConsulta
        {
            public readonly List<FiltroMaterialLectura> Filtros = new List<FiltroMaterialLectura>();
            public Task<IReadOnlyList<MaterialInventarioLectura>> ListarAsync(FiltroMaterialLectura filtro, CancellationToken token = default)
            { Filtros.Add(filtro); return Task.FromResult((IReadOnlyList<MaterialInventarioLectura>)new MaterialInventarioLectura[0]); }
            public Task<IReadOnlyList<CategoriaMaterialLectura>> CategoriasAsync(CancellationToken token = default) => throw new NotImplementedException();
            public Task<IReadOnlyList<ProveedorMaterialLectura>> CatalogoProveedoresAsync(CancellationToken token = default) => throw new NotImplementedException();
            public Task<IReadOnlyList<int>> ProveedoresAsync(int materialId, CancellationToken token = default) => throw new NotImplementedException();
        }
        private sealed class Executor : IUseCaseExecutor
        {
            private readonly ConsultarMateriales caso;
            public Executor(Consulta consulta)
            {
                var sesion = new Session(); sesion.Start(new Identity("test@example.test", Role.Admin));
                caso = new ConsultarMateriales(sesion, consulta);
            }
            public Task<TResult> ExecuteAsync<T, TResult>(Func<T, Task<TResult>> operacion) where T : class => operacion((T)(object)caso);
        }
        [Fact]
        public void FiltroPorCategoriaSinRangoNoRestringeStockACero()
        {
            Exception error = null;
            var hilo = new Thread(() => {
                try
                {
                    var consulta = new Consulta();
                    using (var vista = (Form)Activator.CreateInstance(typeof(Vista_Materiales), BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { new MaterialesPresenter(new Executor(consulta)) }, null))
                    {
                        var handle = vista.Handle;
                        var recibir = vista.GetType().GetMethod("RecibirFiltro", BindingFlags.Instance | BindingFlags.NonPublic);
                        recibir.Invoke(vista, new object[] { null, new FiltroMaterialParams { DesdeCantidad = 0, HastaCantidad = 0, IdsCategoria = new List<int> { 7 } } });
                        var filtro = consulta.Filtros[consulta.Filtros.Count - 1];
                        Assert.Equal(new[] { 7 }, filtro.Categorias); Assert.Null(filtro.Desde); Assert.Null(filtro.Hasta);
                        recibir.Invoke(vista, new object[] { null, new FiltroMaterialParams { DesdeCantidad = 2, HastaCantidad = 10 } });
                        filtro = consulta.Filtros[consulta.Filtros.Count - 1];
                        Assert.Equal(2m, filtro.Desde); Assert.Equal(10m, filtro.Hasta);
                    }
                }
                catch (Exception ex) { error = ex; }
            });
            hilo.SetApartmentState(ApartmentState.STA); hilo.Start(); Assert.True(hilo.Join(TimeSpan.FromSeconds(15))); Assert.Null(error);
        }
    }
}
