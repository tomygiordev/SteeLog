using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Guna.UI2.WinForms;

namespace SteeLog.UI
{
    /// <summary>Utilidades visuales compartidas por las vistas de inventario y reportes.</summary>
    internal static class UiVista
    {
        public static Color ColorEstado(string estado)
        {
            string s = (estado ?? "").ToLowerInvariant();
            if (s.Contains("sin stock") || s.Contains("cancel") || s.Contains("faltante")) return Color.FromArgb(226, 96, 86);
            if (s.Contains("bajo") || s.Contains("abierto") || s.Contains("sobrante")) return Tema.Advertencia;
            if (s.Contains("en obra")) return Tema.Info;
            if (s.Contains("consumido")) return Tema.TextoApagado;
            return Color.FromArgb(70, 200, 120);
        }

        public static string TextoEstado(string estado)
        {
            string s = (estado ?? "").Trim();
            string l = s.ToLowerInvariant();
            if (l.Contains("sin stock")) return "Sin stock";
            if (l.Contains("stock bajo")) return "Stock bajo";
            if (l.Contains("ok")) return "OK";
            if (s.Length == 0) return s;
            return char.ToUpperInvariant(s[0]) + s.Substring(1).ToLowerInvariant();
        }

        /// <summary>Pastilla de estado (punto + texto) en la celda.</summary>
        public static void PintarPildora(DataGridViewCellPaintingEventArgs e, string texto, Color color, bool alinearIzquierda = true)
        {
            e.PaintBackground(e.ClipBounds, e.State.HasFlag(DataGridViewElementStates.Selected));
            var g = e.Graphics;
            var modo = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var f = Tema.Fuente(9.5f, true);
            var sz = TextRenderer.MeasureText(g, texto, f, new Size(400, 30), TextFormatFlags.NoPadding);
            int w = Math.Min(e.CellBounds.Width - 16, sz.Width + 38), h = 24;
            int x = alinearIzquierda ? e.CellBounds.X + 12 : e.CellBounds.X + (e.CellBounds.Width - w) / 2;
            var r = new RectangleF(x, e.CellBounds.Y + (e.CellBounds.Height - h) / 2f, w, h);
            using (var p = Tema.RectRedondeado(r, h / 2f))
            using (var br = new SolidBrush(Tema.Mezclar(Tema.Fondo, color, 0.20)))
                g.FillPath(br, p);
            using (var br = new SolidBrush(color))
                g.FillEllipse(br, r.X + 10, r.Y + h / 2f - 3.5f, 7, 7);
            TextRenderer.DrawText(g, texto, f, Rectangle.Round(new RectangleF(r.X + 24, r.Y, r.Width - 28, r.Height)), color,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
            g.SmoothingMode = modo;
            e.Handled = true;
        }

        /// <summary>Estado "apagado" de un selector de fecha sin deshabilitarlo (Guna lo pinta gris claro al deshabilitar).</summary>
        public static void AtenuarFecha(Guna2DateTimePicker d, bool activo)
        {
            d.FillColor = activo ? Tema.FondoInput : Tema.DeshabilitadoFill;
            d.ForeColor = activo ? Tema.Texto : Tema.DeshabilitadoTexto;
            d.BorderColor = activo ? Tema.Borde : Tema.LineaGrilla;
            d.CheckedState.FillColor = d.FillColor; d.CheckedState.ForeColor = d.ForeColor; d.CheckedState.BorderColor = d.BorderColor;
            d.Invalidate();
        }

        /// <summary>Botón de pestaña interna (Variante.Tab; el estado activo lo fija cada vista con Tema.Boton).</summary>
        public static Guna2Button BotonTab(string texto, int ancho = 150)
        {
            var b = new Guna2Button { Text = texto, Size = new Size(ancho, Tema.AltoControl), Margin = new Padding(0, 0, 8, 0) };
            Tema.Boton(b, Variante.Tab);
            return b;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
        private const int WM_SETREDRAW = 0x000B;

        /// <summary>
        /// Enlaza datos a un Guna2ComboBox (DropDownList) sin pintar estados intermedios. Al asignar DataSource y luego
        /// DisplayMember/ValueMember con el handle creado, WinForms reconstruye la lista (Items.Count == 0) mientras el control
        /// nativo conserva SelectedIndex 0; si Guna pinta en ese instante, Text lanza ArgumentOutOfRangeException ("index 0")
        /// y el combo se dibuja con una X roja. Se fijan primero los miembros (con DataSource nulo), se enlaza una sola vez
        /// y se suspende el redibujado durante la operacion.
        /// </summary>
        public static void AsignarDatos(Guna2ComboBox combo, object origen, string miembroMostrado, string miembroValor)
        {
            if (combo == null) return;
            bool suspender = combo.IsHandleCreated;
            if (suspender) SendMessage(combo.Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
            try
            {
                if (combo.DataSource == null)
                {
                    combo.DisplayMember = miembroMostrado ?? string.Empty;
                    combo.ValueMember = miembroValor ?? string.Empty;
                    combo.DataSource = origen;
                }
                else
                {
                    combo.DataSource = origen;
                    combo.DisplayMember = miembroMostrado ?? string.Empty;
                    combo.ValueMember = miembroValor ?? string.Empty;
                }
            }
            finally
            {
                if (suspender)
                {
                    SendMessage(combo.Handle, WM_SETREDRAW, new IntPtr(1), IntPtr.Zero);
                    combo.Invalidate();
                }
            }
        }

        public static Label Etiqueta(string texto, int alto = 36)
        {
            var f = Tema.Fuente(10.5f);
            int w = TextRenderer.MeasureText(texto, f, new Size(400, alto), TextFormatFlags.NoPadding).Width + 6;
            return new Label
            {
                Text = texto, Font = f, ForeColor = Tema.TextoSecundario, BackColor = Color.Transparent,
                AutoSize = false, Size = new Size(w, alto), TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(14, 0, 4, 0), UseCompatibleTextRendering = false
            };
        }
    }
}
