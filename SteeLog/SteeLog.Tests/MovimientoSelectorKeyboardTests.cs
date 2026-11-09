using System;
using System.Drawing;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using SteeLog.Modelos;
using SteeLog.Tests;
using SteeLog.Vistas.Movimientos;
using Xunit;

public sealed class MovimientoSelectorKeyboardTests
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    [Theory]
    [InlineData(20, 15)]
    [InlineData(-3, 7)]
    [InlineData(20, -2)]
    public void ClicDelSelectorUsaCoordenadasDelMensajeSinConsultarNiMoverCursor(int x, int y)
    {
        EnSta(() => {
            using (var form = new Form())
            using (var panel = new Panel { Location = new Point(123, 45) })
            {
                form.Controls.Add(panel);
                long lParam = ((long)(ushort)y << 16) | (ushort)x;
                var mensaje = Message.Create(panel.Handle, 0x0201, IntPtr.Zero, new IntPtr(lParam));
                var traducir = typeof(Vista_Movimientos).GetMethod("PuntoPantallaDelMensaje", BindingFlags.Static | BindingFlags.NonPublic);
                Assert.Equal(panel.PointToScreen(new Point(x, y)), (Point)traducir.Invoke(null, new object[] { mensaje }));
                Assert.Null(traducir.Invoke(null, new object[] { Message.Create(IntPtr.Zero, 0x0201, IntPtr.Zero, IntPtr.Zero) }));
                Assert.False(form.Visible);
            }
        });
    }

    [Theory]
    [InlineData(Keys.Enter, true)]
    [InlineData(Keys.Space, true)]
    [InlineData(Keys.A, false)]
    public void ResultadoDeBusquedaSeleccionaElMismoMaterialQueElClic(Keys key, bool selecciona)
    {
        EnSta(() => {
            using (var vista = new Vista_Movimientos(new ServiciosVistaDePrueba()))
            {
                var type = typeof(Vista_Movimientos);
                var itemType = type.GetNestedType("ItemBuscador", BindingFlags.NonPublic);
                var item = Activator.CreateInstance(itemType, true);
                var material = new Material { id = 35, nombre = "Perfil Demo" };
                itemType.GetProperty("EsMaterial").SetValue(item, true);
                itemType.GetProperty("Mat").SetValue(item, material);
                itemType.GetProperty("NombreMostrar").SetValue(item, material.nombre);
                using (var panel = (Panel)type.GetMethod("CrearItemResultado", Private).Invoke(vista, new[] { item }))
                {
                    Assert.True(panel.TabStop);
                    Assert.True(panel.CanSelect);
                    Assert.Equal(material.nombre, panel.AccessibleName);
                    var preview = new PreviewKeyDownEventArgs(key);
                    typeof(Control).GetMethod("OnPreviewKeyDown", Private).Invoke(panel, new object[] { preview });
                    Assert.Equal(selecciona, preview.IsInputKey);
                    var args = new KeyEventArgs(key);
                    typeof(Control).GetMethod("OnKeyDown", Private).Invoke(panel, new object[] { args });
                    Assert.Equal(selecciona, args.SuppressKeyPress);
                    var seleccionado = type.GetField("_materialSeleccionado", Private).GetValue(vista);
                    if (selecciona)
                    {
                        Assert.Same(material, seleccionado);
                        var buscador = (Control)type.GetField("txtBuscadorMaterial", Private).GetValue(vista);
                        Assert.Equal(material.nombre, buscador.Text);
                    }
                    else Assert.Null(seleccionado);
                }
            }
        });
    }
    private static void EnSta(Action test)
    {
        Exception error = null;
        var thread = new Thread(() => { try { test(); } catch (Exception e) { error = e; } });
        thread.SetApartmentState(ApartmentState.STA); thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(15))); Assert.Null(error);
    }
}
