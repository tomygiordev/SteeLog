using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SteeLog.Core.Application.Catalogos;
using SteeLog.Presentation;
using Xunit;

namespace SteeLog.Tests
{
    public sealed class CatalogosValidacionVistaTests
    {
        private const BindingFlags Privado = BindingFlags.NonPublic | BindingFlags.Instance;
        private sealed class Ejecutor : IUseCaseExecutor
        {
            public int Escrituras; public Exception ErrorEscritura;
            public Task<TResult> ExecuteAsync<TUseCase, TResult>(Func<TUseCase, Task<TResult>> accion) where TUseCase : class
            {
                if (typeof(TResult) == typeof(int))
                {
                    Escrituras++;
                    if (ErrorEscritura != null) { var t = new TaskCompletionSource<TResult>(); t.SetException(ErrorEscritura); return t.Task; }
                    return Task.FromResult((TResult)(object)1);
                }
                var dto = typeof(TResult).GetGenericArguments()[0];
                return Task.FromResult((TResult)Activator.CreateInstance(typeof(List<>).MakeGenericType(dto)));
            }
        }
        private static void Sta(Action action)
        {
            Exception error = null;
            var thread = new Thread(() => { try { SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext()); action(); } catch (Exception ex) { error = ex; } });
            thread.SetApartmentState(ApartmentState.STA); thread.Start(); Assert.True(thread.Join(TimeSpan.FromSeconds(20))); Assert.Null(error);
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
            public Control Boton, Texto; public List<string> Advertencias = new List<string>(), Errores = new List<string>(); public int Confirmaciones;
            public Action OnConfirmar;
            public void Guardar(string handler) => Tipo.GetMethod(handler, Privado).Invoke(Vista, new object[] { null, EventArgs.Empty });
        }
        private static void Con(string plural, string singular, Action<Escenario> prueba)
        {
            Sta(() => {
                var e = new Escenario();
                e.Tipo = Assembly.Load("SteeLog").GetType("SteeLog.Vistas.Vista_" + plural, true);
                using (e.Vista = (Form)Activator.CreateInstance(e.Tipo, Privado, null, new object[] { new ServiciosVistaDePrueba(), new CatalogosPresenter(e.Ejecutor) }, null))
                {
                    e.Tipo.GetField("Confirmar", Privado).SetValue(e.Vista, new Func<string, MessageBoxIcon, bool>((_, __) => { e.Confirmaciones++; e.OnConfirmar?.Invoke(); return true; }));
                    e.Tipo.GetField("AvisarAdvertencia", Privado).SetValue(e.Vista, new Action<string>(m => e.Advertencias.Add(m)));
                    e.Tipo.GetField("AvisarError", Privado).SetValue(e.Vista, new Action<string>(m => e.Errores.Add(m)));
                    var handle = e.Vista.Handle;
                    e.Boton = (Control)e.Tipo.GetField("guna2Button1", Privado).GetValue(e.Vista);
                    e.Texto = (Control)e.Tipo.GetField("txt" + singular, Privado).GetValue(e.Vista);
                    e.Tipo.GetMethod("OnLoad", Privado).Invoke(e.Vista, new object[] { EventArgs.Empty });
                    Esperar(() => e.Boton.Enabled);
                    prueba(e);
                }
            });
        }
        public static IEnumerable<object[]> Vistas()
        {
            yield return new object[] { "Categorias", "Categoria", "guna2Button1_Click_1", 50, "una categoría" };
            yield return new object[] { "Obras", "Obra", "guna2Button1_Click", 100, "una obra" };
            yield return new object[] { "Proveedores", "Proveedor", "guna2Button1_Click", 50, "un proveedor" };
            yield return new object[] { "Ubicaciones", "Ubicacion", "guna2Button1_Click", 100, "una ubicación" };
            yield return new object[] { "Fletes", "Flete", "guna2Button1_Click", 50, "un flete" };
        }
        [Theory, MemberData(nameof(Vistas))]
        public void DobleInvocacionDelGuardadoEscribeUnaSolaVez(string plural, string singular, string handler, int max, string articulo)
        {
            Con(plural, singular, e => {
                e.Texto.Text = "Nuevo";
                e.OnConfirmar = () => e.Guardar(handler); // segundo Enter mientras la confirmación sigue abierta
                e.Guardar(handler);
                Assert.Equal(1, e.Confirmaciones);
                Esperar(() => e.Boton.Enabled);
                Assert.Equal(1, e.Ejecutor.Escrituras);
                Assert.Empty(e.Errores);
            });
        }
        [Theory, MemberData(nameof(Vistas))]
        public void NombreMasLargoQueLaColumnaSeRechazaSinConfirmarNiEscribir(string plural, string singular, string handler, int max, string articulo)
        {
            Con(plural, singular, e => {
                e.Texto.Text = new string('x', max + 1);
                e.Guardar(handler);
                Assert.Equal(0, e.Confirmaciones); Assert.Equal(0, e.Ejecutor.Escrituras);
                Assert.Single(e.Advertencias); Assert.Contains(max.ToString(), e.Advertencias[0]); Assert.DoesNotContain("parámetro", e.Advertencias[0]);
                Assert.True(e.Boton.Enabled);
            });
        }
        [Theory, MemberData(nameof(Vistas))]
        public void MaxLengthCoincideConLaColumna(string plural, string singular, string handler, int max, string articulo)
        {
            Con(plural, singular, e => Assert.Equal(max, (int)e.Texto.GetType().GetProperty("MaxLength").GetValue(e.Texto)));
        }
        [Theory, MemberData(nameof(Vistas))]
        public void UnicidadMuestraMensajeAmigable(string plural, string singular, string handler, int max, string articulo)
        {
            Con(plural, singular, e => {
                e.Ejecutor.ErrorEscritura = new CatalogoDuplicadoException(articulo);
                e.Texto.Text = "Repetido";
                e.Guardar(handler);
                Esperar(() => e.Errores.Count == 1 && e.Boton.Enabled);
                Assert.Equal("Ya existe " + articulo + " con ese nombre.", e.Errores[0]);
            });
        }
        [Theory, MemberData(nameof(Vistas))]
        public void ArgumentExceptionMuestraMensajeAmigableSinNombreDelParametro(string plural, string singular, string handler, int max, string articulo)
        {
            Con(plural, singular, e => {
                e.Ejecutor.ErrorEscritura = new ArgumentException("Nombre inválido.", "nombre");
                e.Texto.Text = "Algo";
                e.Guardar(handler);
                Esperar(() => e.Errores.Count == 1 && e.Boton.Enabled);
                Assert.Equal("Nombre inválido.", e.Errores[0]);
                Assert.DoesNotContain("parámetro", e.Errores[0]);
            });
        }
        [Fact]
        public void MensajeGuardadoTraduceSoloErroresConocidos()
        {
            Assert.Equal("Ya existe un material con ese nombre.", CatalogosPresenter.MensajeGuardado(new CatalogoDuplicadoException("un material")));
            Assert.Equal("Nombre inválido.", CatalogosPresenter.MensajeGuardado(new ArgumentException("Nombre inválido.", "nombre")));
            Assert.Equal("Error al guardar: boom", CatalogosPresenter.MensajeGuardado(new InvalidOperationException("boom")));
        }
        [Fact]
        public void TextoSinResultadosDelBuscadorTieneContrasteSuficiente()
        {
            var tipo = Assembly.Load("SteeLog").GetType("SteeLog.BusquedaGlobal", true);
            var texto = (Color)tipo.GetField("ColorSinResultados", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            var fondo = (Color)tipo.GetField("ColorFondoPanel", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            Assert.True(Contraste(texto, fondo) >= 4.5, "Contraste " + Contraste(texto, fondo));
        }
        private static double Luminancia(Color c)
        {
            Func<byte, double> f = v => { var s = v / 255.0; return s <= 0.03928 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4); };
            return 0.2126 * f(c.R) + 0.7152 * f(c.G) + 0.0722 * f(c.B);
        }
        private static double Contraste(Color a, Color b)
        {
            double x = Luminancia(a), y = Luminancia(b);
            return (Math.Max(x, y) + 0.05) / (Math.Min(x, y) + 0.05);
        }
    }
}
