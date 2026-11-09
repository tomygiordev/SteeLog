using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SteeLog.Core.Application.Catalogos;
using SteeLog.Core.Application.Identidad;
using SteeLog.Core.Application.Inventario;
using SteeLog.Domain.Catalogos;
using SteeLog.Presentation;
using Xunit;

namespace SteeLog.Tests
{
    public sealed class CatalogosEliminacionVistaTests
    {
        private const BindingFlags Privado = BindingFlags.NonPublic | BindingFlags.Instance;
        private sealed class Ejecutor : IUseCaseExecutor
        {
            public int Escrituras; public Exception ErrorEscritura;
            public List<KeyValuePair<int, string>> Items = new List<KeyValuePair<int, string>> { new KeyValuePair<int, string>(1, "Alfa"), new KeyValuePair<int, string>(2, "Beta") };
            public Task<TResult> ExecuteAsync<TUseCase, TResult>(Func<TUseCase, Task<TResult>> accion) where TUseCase : class
            {
                if (typeof(TResult) == typeof(int))
                {
                    Escrituras++;
                    if (ErrorEscritura != null) { var t = new TaskCompletionSource<TResult>(); t.SetException(ErrorEscritura); return t.Task; }
                    return Task.FromResult((TResult)(object)1);
                }
                var dto = typeof(TResult).GetGenericArguments()[0];
                var lista = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(dto));
                foreach (var par in Items) { var x = Activator.CreateInstance(dto); dto.GetProperty("Id").SetValue(x, par.Key); dto.GetProperty("Nombre").SetValue(x, par.Value); lista.Add(x); }
                return Task.FromResult((TResult)lista);
            }
        }
        private static void Sta(Action action)
        {
            Exception error = null;
            var thread = new Thread(() => { try { SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext()); action(); } catch (Exception ex) { error = ex; } });
            thread.SetApartmentState(ApartmentState.STA); thread.Start(); Assert.True(thread.Join(TimeSpan.FromSeconds(30))); Assert.Null(error);
        }
        private static void Esperar(Func<bool> condicion)
        {
            var fin = DateTime.UtcNow.AddSeconds(5);
            while (!condicion() && DateTime.UtcNow < fin) { Application.DoEvents(); Thread.Sleep(5); }
            Assert.True(condicion());
        }
        private sealed class Escenario
        {
            public Type Tipo; public Form Vista; public Ejecutor Ejecutor = new Ejecutor();
            public Control Guardar, Eliminar, Texto; public DataGridView Grilla;
            public List<string> Errores = new List<string>();
            public int Seleccionado { get { return (int)Tipo.GetField("idSeleccionado", Privado).GetValue(Vista); } set { Tipo.GetField("idSeleccionado", Privado).SetValue(Vista, value); } }
            public void Invocar(string metodo, params object[] args) { Tipo.GetMethod(metodo, Privado).Invoke(Vista, args); }
        }
        private static void Con(string plural, string singular, Action<Escenario> prueba)
        {
            Sta(() => {
                var e = new Escenario();
                e.Tipo = Assembly.Load("SteeLog").GetType("SteeLog.Vistas.Vista_" + plural, true);
                using (e.Vista = (Form)Activator.CreateInstance(e.Tipo, Privado, null, new object[] { new CatalogosPresenter(e.Ejecutor) }, null))
                {
                    e.Tipo.GetField("Confirmar", Privado).SetValue(e.Vista, new Func<string, MessageBoxIcon, bool>((_, __) => true));
                    e.Tipo.GetField("AvisarAdvertencia", Privado).SetValue(e.Vista, new Action<string>(m => { }));
                    e.Tipo.GetField("AvisarError", Privado).SetValue(e.Vista, new Action<string>(m => e.Errores.Add(m)));
                    var handle = e.Vista.Handle;
                    e.Guardar = (Control)e.Tipo.GetField("guna2Button1", Privado).GetValue(e.Vista);
                    e.Eliminar = (Control)e.Tipo.GetField("eliminar" + singular + "Boton", Privado).GetValue(e.Vista);
                    e.Texto = (Control)e.Tipo.GetField("txt" + singular, Privado).GetValue(e.Vista);
                    e.Grilla = (DataGridView)e.Tipo.GetField("dgv" + plural, Privado).GetValue(e.Vista);
                    e.Tipo.GetMethod("OnLoad", Privado).Invoke(e.Vista, new object[] { EventArgs.Empty });
                    Esperar(() => e.Guardar.Enabled);
                    prueba(e);
                }
            });
        }
        public static IEnumerable<object[]> Vistas()
        {
            yield return new object[] { "Categorias", "Categoria", "eliminarCategoriaBoton_Click" };
            yield return new object[] { "Obras", "Obra", "eliminarObraBoton_Click_1" };
            yield return new object[] { "Proveedores", "Proveedor", "eliminarProveedorBoton_Click" };
            yield return new object[] { "Ubicaciones", "Ubicacion", "eliminarUbicacionBoton_Click_1" };
        }
        public static IEnumerable<object[]> TodasLasVistas()
        {
            foreach (var v in Vistas()) yield return v;
            yield return new object[] { "Fletes", "Flete", "eliminarFleteBoton_Click" };
        }
        [Theory, MemberData(nameof(Vistas))]
        public void EliminarEntidadEnUsoMuestraMensajeAmigableSinSql(string plural, string singular, string handler)
        {
            Con(plural, singular, e => {
                var causa = new InvalidOperationException("23503: update or delete on table \"x\" violates foreign key constraint \"y\"");
                e.Ejecutor.ErrorEscritura = new CatalogoEnUsoException("la entidad", "movimientos", causa);
                e.Seleccionado = 2;
                e.Invocar(handler, null, EventArgs.Empty);
                Esperar(() => e.Errores.Count == 1 && e.Guardar.Enabled);
                Assert.Equal("No se puede eliminar la entidad porque está en uso (movimientos).", e.Errores[0]);
                Assert.DoesNotContain("23503", e.Errores[0]); Assert.DoesNotContain("violates", e.Errores[0]);
            });
        }
        [Theory, MemberData(nameof(Vistas))]
        public void EliminarConOtroErrorConservaPrefijoYMotivo(string plural, string singular, string handler)
        {
            Con(plural, singular, e => {
                e.Ejecutor.ErrorEscritura = new InvalidOperationException("boom");
                e.Seleccionado = 2;
                e.Invocar(handler, null, EventArgs.Empty);
                Esperar(() => e.Errores.Count == 1 && e.Guardar.Enabled);
                Assert.Equal("No se pudo eliminar: boom", e.Errores[0]);
            });
        }
        [Theory, MemberData(nameof(TodasLasVistas))]
        public void SeleccionDeGrillaYEstadoLogicoCoinciden(string plural, string singular, string handler)
        {
            Con(plural, singular, e => {
                // Tras cargar no hay nada resaltado ni seleccionado.
                Assert.Empty(e.Grilla.SelectedRows); Assert.Equal(-1, e.Seleccionado); Assert.False(e.Eliminar.Enabled);
                // Seleccionar una fila (clic o teclado) la vuelve lógica: carga el texto y habilita Eliminar sin segundo clic.
                e.Grilla.Rows[1].Selected = true;
                Assert.Equal(2, e.Seleccionado); Assert.Equal("Beta", e.Texto.Text); Assert.True(e.Eliminar.Enabled);
                // Re-render (como al volver a una vista en caché): lo resaltado sigue coincidiendo con lo lógico.
                e.Invocar("MostrarItems");
                Assert.Equal(2, e.Seleccionado); Assert.Single(e.Grilla.SelectedRows); Assert.Equal(1, e.Grilla.SelectedRows[0].Index); Assert.True(e.Eliminar.Enabled);
                // Clic en otra fila cambia la selección; clic de nuevo sobre la lógica la limpia.
                e.Invocar("dgv" + plural + "_CellClick", null, new DataGridViewCellEventArgs(0, 0));
                Assert.Equal(1, e.Seleccionado); Assert.Equal("Alfa", e.Texto.Text);
                e.Invocar("dgv" + plural + "_CellClick", null, new DataGridViewCellEventArgs(0, 0));
                Assert.Equal(-1, e.Seleccionado); Assert.Empty(e.Grilla.SelectedRows); Assert.False(e.Eliminar.Enabled); Assert.Equal("", e.Texto.Text);
            });
        }
        [Fact]
        public void GramaticaDelNombreUsaDelYDeLa()
        {
            Assert.Equal("El nombre del proveedor no puede superar los 50 caracteres.", Proveedor.ValidarNombre(new string('x', 51)));
            Assert.Equal("El nombre del flete no puede superar los 50 caracteres.", Flete.ValidarNombre(new string('x', 51)));
            Assert.Equal("El nombre de la categoría no puede superar los 50 caracteres.", Categoria.ValidarNombre(new string('x', 51)));
            Assert.Equal("El nombre de la obra no puede superar los 100 caracteres.", Obra.ValidarNombre(new string('x', 101)));
            Assert.Equal("El nombre de la ubicación no puede superar los 100 caracteres.", Ubicacion.ValidarNombre(new string('x', 101)));
            Assert.DoesNotContain("de el ", NombreCatalogo.Validar(new string('x', 9), 3, "el algo"));
        }
        [Fact]
        public void MensajeEliminacionTraduceEnUsoYConservaOtrosErrores()
        {
            var enUso = new CatalogoEnUsoException("el proveedor", "órdenes de compra");
            Assert.Equal("No se puede eliminar el proveedor porque está en uso (órdenes de compra).", enUso.Message);
            Assert.Equal(enUso.Message, CatalogosPresenter.MensajeEliminacion(new AggregateException(enUso)));
            Assert.Equal("No se puede eliminar la ubicación porque está en uso.", new CatalogoEnUsoException("la ubicación").Message);
            Assert.Equal("No se pudo eliminar: boom", CatalogosPresenter.MensajeEliminacion(new InvalidOperationException("boom")));
        }

        // ---- Materiales ----
        private sealed class ConsultaMateriales : IMaterialesConsulta
        {
            public List<CategoriaMaterialLectura> Categorias = new List<CategoriaMaterialLectura> { new CategoriaMaterialLectura { Id = 1, Nombre = "Chapa" }, new CategoriaMaterialLectura { Id = 2, Nombre = "Perfil" } };
            public Task<IReadOnlyList<MaterialInventarioLectura>> ListarAsync(FiltroMaterialLectura filtro, CancellationToken token = default(CancellationToken)) { return Task.FromResult((IReadOnlyList<MaterialInventarioLectura>)new MaterialInventarioLectura[0]); }
            public Task<IReadOnlyList<CategoriaMaterialLectura>> CategoriasAsync(CancellationToken token = default(CancellationToken)) { return Task.FromResult((IReadOnlyList<CategoriaMaterialLectura>)Categorias.ToArray()); }
            public Task<IReadOnlyList<ProveedorMaterialLectura>> CatalogoProveedoresAsync(CancellationToken token = default(CancellationToken)) { throw new NotImplementedException(); }
            public Task<IReadOnlyList<int>> ProveedoresAsync(int materialId, CancellationToken token = default(CancellationToken)) { throw new NotImplementedException(); }
        }
        private sealed class EjecutorMateriales : IUseCaseExecutor
        {
            private readonly ConsultarMateriales caso; public Exception ErrorEscritura;
            public EjecutorMateriales(ConsultaMateriales consulta) { var s = new Session(); s.Start(new Identity("test@example.test", Role.Admin)); caso = new ConsultarMateriales(s, consulta); }
            public Task<TResult> ExecuteAsync<T, TResult>(Func<T, Task<TResult>> operacion) where T : class
            {
                if (typeof(T) == typeof(ConsultarMateriales)) return operacion((T)(object)caso);
                if (ErrorEscritura != null) { var t = new TaskCompletionSource<TResult>(); t.SetException(ErrorEscritura); return t.Task; }
                return Task.FromResult(default(TResult));
            }
        }
        private static void ConMateriales(Action<Form, ConsultaMateriales, EjecutorMateriales, List<string>, List<string>, ComboBox> prueba)
        {
            Sta(() => {
                var consulta = new ConsultaMateriales(); var ejecutor = new EjecutorMateriales(consulta);
                var tipo = typeof(SteeLog.Vistas.Vista_Materiales);
                using (var vista = (Form)Activator.CreateInstance(tipo, Privado, null, new object[] { new MaterialesPresenter(ejecutor) }, null))
                {
                    var errores = new List<string>(); var avisos = new List<string>();
                    tipo.GetField("AvisarError", Privado).SetValue(vista, new Action<string>(m => errores.Add(m)));
                    tipo.GetField("AvisarAdvertencia", Privado).SetValue(vista, new Action<string>(m => avisos.Add(m)));
                    tipo.GetField("Confirmar", Privado).SetValue(vista, new Func<string, string, MessageBoxIcon, bool>((_, __, ___) => true));
                    var handle = vista.Handle;
                    var combo = (ComboBox)tipo.GetField("cmbCategoria", Privado).GetValue(vista);
                    tipo.GetMethod("Vista_Materiales_Load", Privado).Invoke(vista, new object[] { null, EventArgs.Empty });
                    Esperar(() => combo.Items.Count == 2);
                    prueba(vista, consulta, ejecutor, errores, avisos, combo);
                }
            });
        }
        [Fact]
        public void MaterialesConservaOpcionesDelComboTrasGuardarYPermiteGuardarOtro()
        {
            ConMateriales((vista, consulta, ejecutor, errores, avisos, combo) => {
                var tipo = vista.GetType();
                var nombre = (Control)tipo.GetField("nombreMaterial", Privado).GetValue(vista);
                var boton = (Control)tipo.GetField("guna2Button1", Privado).GetValue(vista);
                nombre.Text = "Uno"; combo.SelectedIndex = 1;
                tipo.GetMethod("guna2Button1_Click", Privado).Invoke(vista, new object[] { null, EventArgs.Empty });
                Esperar(() => boton.Enabled && nombre.Text == "");
                Assert.Equal(2, combo.Items.Count); Assert.NotEqual(-1, combo.SelectedIndex);
                nombre.Text = "Dos";
                tipo.GetMethod("guna2Button1_Click", Privado).Invoke(vista, new object[] { null, EventArgs.Empty });
                Assert.DoesNotContain("Selecciona una categoría válida.", avisos);
                Esperar(() => boton.Enabled);
                Assert.Empty(errores);
            });
        }
        [Fact]
        public void MaterialesRecargaCategoriasNuevasSinPerderLaSeleccionNiLoEscrito()
        {
            ConMateriales((vista, consulta, ejecutor, errores, avisos, combo) => {
                var tipo = vista.GetType();
                var nombre = (Control)tipo.GetField("nombreMaterial", Privado).GetValue(vista);
                nombre.Text = "En edición"; combo.SelectedIndex = 1;
                consulta.Categorias.Add(new CategoriaMaterialLectura { Id = 3, Nombre = "Nueva" });
                var tarea = (Task)tipo.GetMethod("RecargarCategoriasAsync", Privado).Invoke(vista, null);
                Esperar(() => tarea.IsCompleted);
                Assert.Equal(3, combo.Items.Count); Assert.Equal(2, (int)combo.SelectedValue); Assert.Equal("En edición", nombre.Text);
            });
        }
        [Fact]
        public void MaterialesEliminarEnUsoMuestraMensajeAmigable()
        {
            ConMateriales((vista, consulta, ejecutor, errores, avisos, combo) => {
                var tipo = vista.GetType();
                ejecutor.ErrorEscritura = new CatalogoEnUsoException("el material", "movimientos u órdenes de compra", new InvalidOperationException("23503: violates foreign key"));
                tipo.GetField("idMaterialSeleccionado", Privado).SetValue(vista, 7);
                tipo.GetMethod("eliminarMaterialBoton_Click", Privado).Invoke(vista, new object[] { null, EventArgs.Empty });
                Esperar(() => errores.Count == 1);
                Assert.Equal("No se puede eliminar el material porque está en uso (movimientos u órdenes de compra).", errores[0]);
            });
        }
    }
}
