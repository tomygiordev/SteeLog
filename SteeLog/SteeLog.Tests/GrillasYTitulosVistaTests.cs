using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using SteeLog.Modelos;
using SteeLog.Vistas;
using SteeLog.Vistas.Movimientos;
using SteeLog.Vistas.Reportes;
using Xunit;

namespace SteeLog.Tests
{
    public sealed class GrillasYTitulosVistaTests
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        private static void EnSta(Action accion)
        {
            Exception error = null;
            var hilo = new Thread(() => { try { accion(); } catch (Exception e) { error = e; } });
            hilo.SetApartmentState(ApartmentState.STA); hilo.Start(); Assert.True(hilo.Join(TimeSpan.FromSeconds(30))); Assert.Null(error);
        }

        [Fact]
        public void GrillaRecortes_NoDuplicaColumnasYUsaEncabezadosConfigurados()
        {
            EnSta(() => {
                using (var vista = new Vista_Recortes(new ServiciosVistaDePrueba()))
                {
                    vista.GetType().GetField("_recortes", Flags).SetValue(vista, new List<Recorte> {
                        new Recorte { id = 1, id_material = 2, nombre_material = "Chapa", nombre_categoria = "Chapas", ancho = 1m, altura = 2m, medida = 2m, cantidad = 3m, estado = "Regular", fecha_creacion = DateTime.Today } });
                    vista.GetType().GetMethod("AplicarFiltroBusqueda", Flags).Invoke(vista, null);
                    var grilla = (DataGridView)vista.GetType().GetField("dgvRecortes", Flags).GetValue(vista);
                    var visibles = grilla.Columns.Cast<DataGridViewColumn>().Where(c => c.Visible).ToList();
                    Assert.Equal(10, grilla.Columns.Count);
                    Assert.Equal(new[] { "Material", "Categoría", "Ancho (m)", "Altura (m)", "Medida (m)", "Stock Recorte", "Estado" }, visibles.Select(c => c.HeaderText).ToArray());
                    Assert.Equal(visibles.Count, visibles.Select(c => c.HeaderText.ToLowerInvariant()).Distinct().Count());
                }
            });
        }

        [Fact]
        public void Fletes_ChkActivoEsCheckBoxPersonalizadoConEtiquetaSeparada()
        {
            EnSta(() => {
                using (var vista = new Vista_Fletes(new ServiciosVistaDePrueba()))
                {
                    var chk = vista.GetType().GetField("chkActivo", Flags).GetValue(vista);
                    var lbl = (Label)vista.GetType().GetField("lblActivo", Flags).GetValue(vista);
                    Assert.Equal("Guna2CustomCheckBox", chk.GetType().Name);
                    var custom = (Control)chk;
                    Assert.Equal(new System.Drawing.Size(22, 22), custom.Size);
                    Assert.Equal(AnchorStyles.Left, custom.Anchor);
                    Assert.Equal("Activo:", lbl.Text);
                    Assert.True((bool)chk.GetType().GetProperty("Checked").GetValue(chk));
                    Assert.Same(custom.Parent, lbl.Parent);
                }
            });
        }

        [Fact]
        public void VistasDeGestion_SeInstancianYDisponenSinLanzarExcepcionEnEntornoDiseniador()
        {
            EnSta(() => {
                var tipos = new[] {
                    typeof(Vista_Insumos),
                    typeof(Vista_Herramientas)
                };
                foreach (var tipo in tipos)
                {
                    using (var form = (Form)Activator.CreateInstance(tipo, new ServiciosVistaDePrueba()))
                    {
                        Assert.NotNull(form);
                        Assert.False(form.IsDisposed);
                        var handle = form.Handle;
                        Assert.NotEqual(IntPtr.Zero, handle);
                    }
                }
            });
        }

        [Fact]
        public void VistasPrincipales_SeInstancianYDisponenSinLanzarExcepcionEnEntornoDiseniador()
        {
            EnSta(() => {
                var tipos = new[] {
                    typeof(Vista_Login),
                    typeof(SteeLog.Vistas.Inventario.Vista_Conteo),
                    typeof(SteeLog.Vistas.Inventario.Vista_Inventario),
                    typeof(Vista_Kardex),
                    typeof(Vista_ReporteStock),
                    typeof(Vista_InformeObra),
                };
                foreach (var tipo in tipos)
                {
                    using (var form = (Form)Activator.CreateInstance(tipo, tipo.GetConstructor(new[] { typeof(SteeLog.Presentation.IServiciosVista) }) != null ? new object[] { new ServiciosVistaDePrueba() } : new object[0]))
                    {
                        Assert.NotNull(form);
                        Assert.False(form.IsDisposed);
                        var handle = form.Handle;
                        Assert.NotEqual(IntPtr.Zero, handle);
                    }
                }
            });
        }
    }
}
