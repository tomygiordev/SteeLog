using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using SteeLog.Core.Application.Canaletas;
using SteeLog.Modelos;
using SteeLog.Vistas;
using Xunit;

namespace SteeLog.Tests
{
    public sealed class UxRecortesCanaletasInformeTests
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        private static void EnSta(Action accion)
        {
            Exception error = null;
            var hilo = new Thread(() => { try { accion(); } catch (Exception e) { error = e; } });
            hilo.SetApartmentState(ApartmentState.STA); hilo.Start(); Assert.True(hilo.Join(TimeSpan.FromSeconds(30))); Assert.Null(error);
        }
        private static T Campo<T>(object o, string nombre) => (T)o.GetType().GetField(nombre, Flags).GetValue(o);

        [Fact]
        public void Recortes_EliminarDeshabilitadoSinSeleccionYHabilitadoConSeleccion()
        {
            EnSta(() => {
                using (var vista = new Vista_Recortes())
                {
                    var btn = Campo<Control>(vista, "btnEliminar");
                    Assert.False(btn.Enabled);
                    vista.GetType().GetField("_recortes", Flags).SetValue(vista, new List<Recorte> {
                        new Recorte { id = 1, id_material = 2, nombre_material = "Chapa", nombre_categoria = "Chapas", ancho = 1.2m, altura = 1m, medida = 1.2m, cantidad = 3m, estado = "Regular" } });
                    vista.GetType().GetMethod("AplicarFiltroBusqueda", Flags).Invoke(vista, null);
                    var grilla = Campo<DataGridView>(vista, "dgvRecortes");
                    Assert.Empty(grilla.SelectedRows.Cast<DataGridViewRow>());
                    Assert.False(btn.Enabled);
                    grilla.Rows[0].Selected = true;
                    Assert.True(btn.Enabled);
                    grilla.ClearSelection();
                    Assert.False(btn.Enabled);
                    // El handler no actua sin seleccion (no abre dialogo ni consulta).
                    vista.GetType().GetMethod("btnEliminar_Click", Flags).Invoke(vista, new object[] { btn, EventArgs.Empty });
                }
            });
        }

        [Fact]
        public void Recortes_GrillaFormateaMedidasConDosDecimales()
        {
            EnSta(() => {
                using (var vista = new Vista_Recortes())
                {
                    vista.GetType().GetField("_recortes", Flags).SetValue(vista, new List<Recorte> {
                        new Recorte { id = 1, id_material = 2, nombre_material = "Chapa", nombre_categoria = "Chapas", ancho = 1.2m, altura = 1m, medida = 1.2m, cantidad = 3m, estado = "Regular" } });
                    vista.GetType().GetMethod("AplicarFiltroBusqueda", Flags).Invoke(vista, null);
                    var grilla = Campo<DataGridView>(vista, "dgvRecortes");
                    foreach (var c in new[] { "ancho", "altura", "medida" })
                        Assert.Equal("N2", grilla.Columns[c].DefaultCellStyle.Format);
                    Assert.Equal(grilla.Rows[0].Cells["ancho"].FormattedValue.ToString().Length, grilla.Rows[0].Cells["altura"].FormattedValue.ToString().Length);
                }
            });
        }

        [Fact]
        public void Canaletas_ResumenFotosIncluyeMotivoReal()
        {
            var motivo = Assert.Throws<ArgumentException>(() => OperacionesCanaleta.ValidarTipoFoto(new byte[] { 1, 2, 3 })).Message;
            var metodo = typeof(Vista_CanaletasPlegados).GetMethod("ResumenFotos", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            var texto = (string)metodo.Invoke(null, new object[] { 0, 1, motivo });
            Assert.StartsWith("0 agregada(s), 1 rechazada(s): ", texto);
            Assert.Contains("JPEG, PNG o WebP", texto);
            Assert.Equal("1 agregada(s), 1 rechazada(s).", (string)metodo.Invoke(null, new object[] { 1, 1, null }));
        }

        private static double Luminancia(Color c)
        {
            Func<byte, double> f = v => { double s = v / 255.0; return s <= 0.03928 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4); };
            return 0.2126 * f(c.R) + 0.7152 * f(c.G) + 0.0722 * f(c.B);
        }
        private static double Contraste(Color a, Color b)
        {
            double la = Luminancia(a), lb = Luminancia(b);
            return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
        }

        [Fact]
        public void InformeObra_BotonesDeshabilitadosTienenContrasteLegible()
        {
            EnSta(() => {
                var tipo = Assembly.Load("SteeLog").GetType("SteeLog.Vistas.Reportes.Vista_InformeObra", true);
                using (var form = (Form)Activator.CreateInstance(tipo))
                {
                    foreach (var n in new[] { "btnDescargar", "btnTabMovimientos", "btnTabHerramientas", "btnTabInsumos", "btnTabMateriales", "btnTabEventuales", "btnTabCanaletasPlegados" })
                    {
                        var b = Campo<Control>(form, n);
                        var d = b.GetType().GetProperty("DisabledState").GetValue(b);
                        Func<string, Color> col = k => (Color)d.GetType().GetProperty(k).GetValue(d);
                        double ratio = Contraste(col("ForeColor"), col("FillColor"));
                        Assert.True(ratio >= 4.5, n + " contraste " + ratio);
                        Assert.NotEqual(Color.Empty, col("BorderColor"));
                    }
                }
            });
        }
    }
}
