using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Guna.UI2.WinForms;

namespace SteeLog.UI
{
    /// <summary>Tarjeta KPI sutil: etiqueta secundaria, número grande y detalle opcional, centrados verticalmente.</summary>
    internal sealed class TarjetaKpi : Control
    {
        public const int AltoTarjeta = 84;
        private string etiqueta = "", valor = "—", detalle = "";
        private Color colorDetalle = Tema.TextoApagado;

        public TarjetaKpi()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw, true);
            BackColor = Tema.Fondo;
            Height = AltoTarjeta;
            Margin = new Padding(0);
        }

        public string Etiqueta { get { return etiqueta; } set { etiqueta = value ?? ""; Invalidate(); } }
        public string Valor { get { return valor; } set { valor = value ?? ""; Invalidate(); } }
        public string Detalle { get { return detalle; } set { detalle = value ?? ""; Invalidate(); } }
        public Color ColorDetalle { get { return colorDetalle; } set { colorDetalle = value; Invalidate(); } }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent != null ? Parent.BackColor : Tema.Fondo);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new RectangleF(0.5f, 0.5f, Width - 1, Height - 1);
            using (var p = Tema.RectRedondeado(r, 6))
            {
                using (var br = new SolidBrush(Tema.Superficie)) g.FillPath(br, p);
                using (var pen = new Pen(Tema.Borde, 1f)) g.DrawPath(pen, p);
            }
            using (var br = new SolidBrush(Tema.Acento))
                g.FillRectangle(br, 14, 16, 3, Height - 32);

            const TextFormatFlags fl = TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding
                | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine;
            var fe = Tema.Fuente(10f);
            var fv = Tema.Fuente(20f, true);
            int he = TextRenderer.MeasureText(g, "Ag", fe, new Size(400, 40), TextFormatFlags.NoPadding).Height;
            int hv = TextRenderer.MeasureText(g, "0", fv, new Size(400, 60), TextFormatFlags.NoPadding).Height;
            int y0 = Math.Max(4, (Height - (he + hv)) / 2);
            TextRenderer.DrawText(g, etiqueta, fe, new Rectangle(28, y0, Width - 40, he), Tema.TextoSecundario, fl);
            var szv = TextRenderer.MeasureText(g, valor, fv, new Size(Width, 60), TextFormatFlags.NoPadding);
            TextRenderer.DrawText(g, valor, fv, new Rectangle(28, y0 + he, Math.Min(szv.Width + 2, Width - 40), hv), Tema.Texto, fl);
            if (detalle.Length > 0)
            {
                int x = 28 + szv.Width + 10;
                // El detalle se centra con el número, un poco más abajo para alinear las líneas base.
                TextRenderer.DrawText(g, detalle, fe, new Rectangle(x, y0 + he + 2, Math.Max(0, Width - x - 10), hv), colorDetalle, fl);
            }
        }
    }

    /// <summary>CheckBox con el aspecto del tema (cuadrado 18px con borde redondeado y tilde blanca).</summary>
    internal sealed class CheckTema : CheckBox
    {
        public CheckTema()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Cursor = Cursors.Hand;
            Font = Tema.FuenteTexto;
            ForeColor = Tema.Texto;
        }

        public override Size GetPreferredSize(Size proposedSize)
        {
            var t = TextRenderer.MeasureText(Text ?? "", Font);
            return new Size(18 + 8 + t.Width + 4, 28);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent != null ? Parent.BackColor : Tema.Fondo);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            var caja = new RectangleF(0.5f, (Height - 18) / 2f, 17f, 17f);
            using (var ruta = Tema.RectRedondeado(caja, 4))
            {
                if (Checked)
                {
                    using (var b = new SolidBrush(Tema.Acento)) g.FillPath(b, ruta);
                    using (var pen = new Pen(Color.White, 2f) { StartCap = System.Drawing.Drawing2D.LineCap.Round, EndCap = System.Drawing.Drawing2D.LineCap.Round, LineJoin = System.Drawing.Drawing2D.LineJoin.Round })
                        g.DrawLines(pen, new[] { new PointF(caja.X + 4, caja.Y + 9), new PointF(caja.X + 7.5f, caja.Y + 12.5f), new PointF(caja.X + 13, caja.Y + 5) });
                }
                else
                {
                    using (var pen = new Pen(Focused ? Tema.BordeFoco : Tema.Borde, 1.5f)) g.DrawPath(pen, ruta);
                }
            }
            var color = Enabled ? ForeColor : Tema.DeshabilitadoTexto;
            TextRenderer.DrawText(g, Text ?? "", Font, new Rectangle(26, 0, Width - 26, Height), color,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
        }
    }

    /// <summary>CheckedListBox que dibuja casilla redondeada verde, selección y texto con los tokens de <see cref="Tema"/>.</summary>
    internal sealed class ListaChecksTema : CheckedListBox
    {
        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= Items.Count) return;
            bool marcado = GetItemChecked(e.Index);
            bool sel = (e.State & DrawItemState.Selected) != 0;
            var g = e.Graphics;
            using (var br = new SolidBrush(sel ? Tema.Seleccion : Tema.FondoInput)) g.FillRectangle(br, e.Bounds);
            var modo = g.SmoothingMode;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            var r = new RectangleF(e.Bounds.X + 10, e.Bounds.Y + (e.Bounds.Height - 18) / 2f, 18, 18);
            if (marcado)
            {
                using (var p = Tema.RectRedondeado(r, Tema.Radio)) using (var br = new SolidBrush(Tema.Acento)) g.FillPath(br, p);
                using (var pen = new Pen(Color.White, 2f) { StartCap = System.Drawing.Drawing2D.LineCap.Round, EndCap = System.Drawing.Drawing2D.LineCap.Round, LineJoin = System.Drawing.Drawing2D.LineJoin.Round })
                    g.DrawLines(pen, new[] { new PointF(r.X + 4.5f, r.Y + 9.5f), new PointF(r.X + 7.8f, r.Y + 12.8f), new PointF(r.X + 13.5f, r.Y + 5.8f) });
            }
            else
            {
                var rb = new RectangleF(r.X + 0.75f, r.Y + 0.75f, r.Width - 1.5f, r.Height - 1.5f);
                using (var p = Tema.RectRedondeado(rb, Tema.Radio)) using (var pen = new Pen(Color.FromArgb(95, 125, 106), 1.5f)) g.DrawPath(pen, p);
            }
            g.SmoothingMode = modo;
            TextRenderer.DrawText(g, Items[e.Index].ToString(), Font,
                new Rectangle(e.Bounds.X + 38, e.Bounds.Y, e.Bounds.Width - 44, e.Bounds.Height), Tema.Texto,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
        }
    }
}
