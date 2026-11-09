using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Media;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Guna.UI2.WinForms;

namespace SteeLog.UI
{
    /// <summary>
    /// Reemplazo visual de MessageBox con la misma semántica (botones, ícono, botón por defecto).
    /// Se puede invocar desde cualquier hilo y tolera owners ocultos o ya dispuestos.
    /// </summary>
    internal static class Dialogo
    {
        public static DialogResult Mostrar(string mensaje, string titulo,
            MessageBoxButtons botones = MessageBoxButtons.OK, MessageBoxIcon icono = MessageBoxIcon.None,
            MessageBoxDefaultButton def = MessageBoxDefaultButton.Button1)
        {
            return Mostrar(null, mensaje, titulo, botones, icono, def);
        }

        public static DialogResult Mostrar(IWin32Window owner, string mensaje, string titulo,
            MessageBoxButtons botones = MessageBoxButtons.OK, MessageBoxIcon icono = MessageBoxIcon.None,
            MessageBoxDefaultButton def = MessageBoxDefaultButton.Button1)
        {
            return Ejecutar(owner, mensaje, titulo, botones, TipoDe(icono), def, false);
        }

        public static bool Confirmar(string mensaje, string titulo = "Confirmar", bool destructivo = false, IWin32Window owner = null)
        {
            var tipo = destructivo ? TipoDialogo.Advertencia : TipoDialogo.Pregunta;
            // En acciones destructivas el foco inicial queda en "No".
            var def = destructivo ? MessageBoxDefaultButton.Button2 : MessageBoxDefaultButton.Button1;
            return Ejecutar(owner, mensaje, titulo, MessageBoxButtons.YesNo, tipo, def, destructivo) == DialogResult.Yes;
        }

        public static void Advertencia(string mensaje, string titulo = null, IWin32Window owner = null)
        {
            Ejecutar(owner, mensaje, titulo ?? "Atención", MessageBoxButtons.OK, TipoDialogo.Advertencia, MessageBoxDefaultButton.Button1, false);
        }

        public static void Error(string mensaje, string titulo = null, IWin32Window owner = null)
        {
            Ejecutar(owner, mensaje, titulo ?? "Error", MessageBoxButtons.OK, TipoDialogo.Error, MessageBoxDefaultButton.Button1, false);
        }

        /// <summary>
        /// Pide un texto al usuario (reemplazo de InputBox). Devuelve el texto ingresado (puede ser vacío) o null si cancela.
        /// </summary>
        public static string Pedir(string mensaje, string titulo, string valorInicial = "", IWin32Window owner = null)
        {
            return EnUi(owner, () =>
            {
                var dueno = ResolverOwner(owner);
                using (var d = new FormDialogo(mensaje ?? string.Empty, titulo ?? string.Empty, MessageBoxButtons.OKCancel,
                    TipoDialogo.Pregunta, MessageBoxDefaultButton.Button1, false, dueno, true, valorInicial ?? string.Empty))
                {
                    d.Sonar();
                    DialogResult r;
                    try { r = dueno != null ? d.ShowDialog(dueno) : d.ShowDialog(); }
                    catch (InvalidOperationException) { r = d.ShowDialog(); }
                    return r == DialogResult.OK ? d.TextoIngresado : null;
                }
            });
        }

        // ---------------------------------------------------------------

        private enum TipoDialogo { Ninguno, Exito, Info, Pregunta, Advertencia, Error }

        private static TipoDialogo TipoDe(MessageBoxIcon i)
        {
            switch (i)
            {
                case MessageBoxIcon.Information: return TipoDialogo.Info;
                case MessageBoxIcon.Question: return TipoDialogo.Pregunta;
                case MessageBoxIcon.Warning: return TipoDialogo.Advertencia;
                case MessageBoxIcon.Error: return TipoDialogo.Error;
                default: return TipoDialogo.Ninguno;
            }
        }

        private static DialogResult Ejecutar(IWin32Window owner, string mensaje, string titulo, MessageBoxButtons botones,
            TipoDialogo tipo, MessageBoxDefaultButton def, bool destructivo)
        {
            return EnUi(owner, () => EjecutarUi(owner, mensaje, titulo, botones, tipo, def, destructivo));
        }

        private static T EnUi<T>(IWin32Window owner, Func<T> accion)
        {
            // Marshal al hilo de UI del owner (o del primer form abierto) si hace falta.
            Control marshal = owner as Control;
            if (marshal == null)
            {
                try { if (Application.OpenForms.Count > 0) marshal = Application.OpenForms[0]; } catch { }
            }
            if (marshal != null && !marshal.IsDisposed && marshal.IsHandleCreated && marshal.InvokeRequired)
            {
                try { return (T)marshal.Invoke(accion); }
                catch (ObjectDisposedException) { }
                catch (InvalidOperationException) { }
            }
            return accion();
        }

        private static IWin32Window ResolverOwner(IWin32Window owner)
        {
            try
            {
                var c = owner as Control;
                if (c != null)
                {
                    var f = (c.TopLevelControl as Form) ?? c.FindForm();
                    if (f != null && !f.IsDisposed && f.IsHandleCreated && f.Visible && f.WindowState != FormWindowState.Minimized)
                        return f;
                }
                else if (owner != null && owner.Handle != IntPtr.Zero && IsWindowVisible(owner.Handle))
                {
                    return owner;
                }
                var act = Form.ActiveForm;
                if (OwnerValido(act)) return act;
                // Sin formulario activo (la app perdió el foco): ventana principal abierta.
                foreach (Form f in Application.OpenForms)
                    if (OwnerValido(f)) return f;
            }
            catch { }
            return null;
        }

        private static bool OwnerValido(Form f)
        {
            return f != null && !f.IsDisposed && f.IsHandleCreated && f.Visible && f.TopLevel && !(f is FormDialogo) && !(f is ToastNotification)
                && f.WindowState != FormWindowState.Minimized;
        }

        private static DialogResult EjecutarUi(IWin32Window owner, string mensaje, string titulo, MessageBoxButtons botones,
            TipoDialogo tipo, MessageBoxDefaultButton def, bool destructivo)
        {
            var dueno = ResolverOwner(owner);
            using (var d = new FormDialogo(mensaje ?? string.Empty, titulo ?? string.Empty, botones, tipo, def, destructivo, dueno))
            {
                d.Sonar();
                try
                {
                    return dueno != null ? d.ShowDialog(dueno) : d.ShowDialog();
                }
                catch (InvalidOperationException)
                {
                    return d.ShowDialog();
                }
            }
        }

        [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr h);
        [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr h, out RECT r);
        [StructLayout(LayoutKind.Sequential)] private struct RECT { public int L, T, R, B; }

        // ---------------------------------------------------------------

        private sealed class FormDialogo : Form
        {
            private const int Ancho = 460;
            private const int Pad = 24;
            private const int BarraAcento = 4;
            private const int AltoPie = 68;
            private const int Icono = 40;

            private readonly TipoDialogo _tipo;
            private readonly Color _color;
            private readonly bool _conIcono;
            private readonly Dictionary<Guna2Button, DialogResult> _resultados = new Dictionary<Guna2Button, DialogResult>();
            private readonly DialogResult _escape;
            private readonly Guna2Button _defecto;
            private readonly Rectangle _rectIcono;
            private readonly int _alturaCuerpo;

            private readonly Guna2TextBox _entrada;
            public string TextoIngresado { get { return _entrada == null ? null : _entrada.Text; } }

            public FormDialogo(string mensaje, string titulo, MessageBoxButtons botones, TipoDialogo tipo,
                MessageBoxDefaultButton def, bool destructivo, IWin32Window owner, bool pedirTexto = false, string valorInicial = "")
            {
                _tipo = tipo;
                _color = ColorDe(tipo);
                _conIcono = tipo != TipoDialogo.Ninguno;
                Name = "FormDialogo";

                AutoScaleMode = AutoScaleMode.None;
                FormBorderStyle = FormBorderStyle.None;
                StartPosition = FormStartPosition.Manual;
                ShowInTaskbar = false;
                ShowIcon = false;
                KeyPreview = true;
                BackColor = Tema.Superficie;
                Text = titulo;
                DoubleBuffered = true;
                TopMost = owner == null;

                var area = Screen.FromPoint(Cursor.Position).WorkingArea;
                int textoX = BarraAcento + Pad + (_conIcono ? Icono + 16 : 0);
                int textoAncho = Ancho - 1 - Pad - textoX;
                int cuerpoY = Pad;

                var fTitulo = Tema.Fuente(13f, true);
                var fMsg = Tema.Fuente(10.5f);
                bool hayTitulo = !string.IsNullOrWhiteSpace(titulo);
                int yTexto = cuerpoY;
                int hTitulo = 0;
                if (hayTitulo)
                {
                    hTitulo = Medir(titulo, fTitulo, textoAncho) ;
                    var lblT = new Label
                    {
                        Text = titulo, Font = fTitulo, ForeColor = Tema.Texto, BackColor = Tema.Superficie,
                        AutoSize = false, UseCompatibleTextRendering = false,
                        Location = new Point(textoX, yTexto + (_conIcono && hTitulo < Icono ? (Icono - hTitulo) / 2 - 2 : 0)),
                        Size = new Size(textoAncho, hTitulo)
                    };
                    Controls.Add(lblT);
                    Tema.HabilitarArrastre(lblT, this);
                    yTexto = lblT.Bottom + 8;
                }
                else if (_conIcono)
                {
                    yTexto = cuerpoY + 8;
                }

                int hMsg = string.IsNullOrEmpty(mensaje) ? 0 : Medir(mensaje, fMsg, textoAncho);
                int maxCuerpo = (int)(area.Height * 0.70) - AltoPie;
                int maxMsg = Math.Max(60, maxCuerpo - yTexto - Pad);
                bool scroll = hMsg > maxMsg;
                if (hMsg > 0)
                {
                    var lblM = new Label
                    {
                        Text = mensaje, Font = fMsg, ForeColor = Tema.Texto, BackColor = Tema.Superficie,
                        AutoSize = false, UseCompatibleTextRendering = false,
                        Size = new Size(scroll ? textoAncho - 18 : textoAncho, hMsg)
                    };
                    if (scroll)
                    {
                        var pnl = new Panel
                        {
                            Location = new Point(textoX, yTexto), Size = new Size(textoAncho, maxMsg),
                            AutoScroll = true, BackColor = Tema.Superficie
                        };
                        lblM.Location = Point.Empty;
                        pnl.Controls.Add(lblM);
                        Controls.Add(pnl);
                        yTexto += maxMsg;
                    }
                    else
                    {
                        lblM.Location = new Point(textoX, yTexto);
                        Controls.Add(lblM);
                        Tema.HabilitarArrastre(lblM, this);
                        yTexto += hMsg;
                    }
                }
                else if (hayTitulo) yTexto -= 8;

                if (pedirTexto)
                {
                    _entrada = new Guna2TextBox
                    {
                        Text = valorInicial ?? string.Empty, Location = new Point(textoX, yTexto + 12),
                        Size = new Size(textoAncho, Tema.AltoControl), TabIndex = 0
                    };
                    Tema.Input(_entrada);
                    Controls.Add(_entrada);
                    yTexto += 12 + Tema.AltoControl;
                }

                _alturaCuerpo = Math.Max(yTexto + Pad, cuerpoY + Icono + Pad);
                _rectIcono = new Rectangle(BarraAcento + Pad, cuerpoY, Icono, Icono);
                ClientSize = new Size(Ancho, _alturaCuerpo + AltoPie);

                _escape = ConstruirBotones(botones, def, tipo, destructivo, out _defecto);

                Tema.HabilitarArrastre(this, this);
                Tema.SombraDwm(this);
                if (owner != null) Load += (s, e) => Centrar(owner);
                else Load += (s, e) => Centrar(null);
                Shown += (s, e) =>
                {
                    try
                    {
                        if (_entrada != null) { _entrada.Focus(); _entrada.SelectAll(); }
                        else if (_defecto != null) _defecto.Focus();
                    }
                    catch { }
                };
            }

            private static int Medir(string texto, Font f, int ancho)
            {
                var sz = TextRenderer.MeasureText(texto, f, new Size(ancho, int.MaxValue),
                    TextFormatFlags.WordBreak | TextFormatFlags.NoPadding | TextFormatFlags.TextBoxControl);
                return sz.Height + 4;
            }

            private static Color ColorDe(TipoDialogo t)
            {
                switch (t)
                {
                    case TipoDialogo.Exito: return Tema.Acento;
                    case TipoDialogo.Advertencia: return Tema.Advertencia;
                    case TipoDialogo.Error: return Tema.Peligro;
                    case TipoDialogo.Info:
                    case TipoDialogo.Pregunta: return Tema.Info;
                    default: return Tema.Borde;
                }
            }

            private DialogResult ConstruirBotones(MessageBoxButtons botones, MessageBoxDefaultButton def, TipoDialogo tipo,
                bool destructivo, out Guna2Button defecto)
            {
                DialogResult[] res;
                string[] txt;
                DialogResult esc;
                switch (botones)
                {
                    case MessageBoxButtons.OKCancel:
                        res = new[] { DialogResult.OK, DialogResult.Cancel }; txt = new[] { "Aceptar", "Cancelar" }; esc = DialogResult.Cancel; break;
                    case MessageBoxButtons.YesNo:
                        res = new[] { DialogResult.Yes, DialogResult.No }; txt = new[] { "Sí", "No" }; esc = DialogResult.No; break;
                    case MessageBoxButtons.YesNoCancel:
                        res = new[] { DialogResult.Yes, DialogResult.No, DialogResult.Cancel }; txt = new[] { "Sí", "No", "Cancelar" }; esc = DialogResult.Cancel; break;
                    case MessageBoxButtons.RetryCancel:
                        res = new[] { DialogResult.Retry, DialogResult.Cancel }; txt = new[] { "Reintentar", "Cancelar" }; esc = DialogResult.Cancel; break;
                    case MessageBoxButtons.AbortRetryIgnore:
                        res = new[] { DialogResult.Abort, DialogResult.Retry, DialogResult.Ignore }; txt = new[] { "Anular", "Reintentar", "Omitir" }; esc = DialogResult.None; break;
                    default:
                        res = new[] { DialogResult.OK }; txt = new[] { "Aceptar" }; esc = DialogResult.OK; break;
                }

                int idxDef = def == MessageBoxDefaultButton.Button2 ? 1 : def == MessageBoxDefaultButton.Button3 ? 2 : 0;
                if (idxDef >= res.Length) idxDef = 0;
                bool critico = destructivo || tipo == TipoDialogo.Advertencia || tipo == TipoDialogo.Error;
                bool confirmatorio = res.Length > 1 && critico;

                var fuente = Tema.FuenteBoton;
                var anchos = new int[res.Length];
                int total = 0;
                for (int i = 0; i < res.Length; i++)
                {
                    anchos[i] = Math.Max(110, TextRenderer.MeasureText(txt[i], fuente).Width + 44);
                    total += anchos[i] + (i > 0 ? 8 : 0);
                }
                // Si no entran, se reduce al mínimo.
                int x = Ancho - 1 - Pad - total;
                int y = _alturaCuerpo + (AltoPie - Tema.AltoControl) / 2;
                defecto = null;
                for (int i = 0; i < res.Length; i++)
                {
                    bool afirmativo = res[i] == DialogResult.Yes || res[i] == DialogResult.OK || res[i] == DialogResult.Retry;
                    var b = new Guna2Button
                    {
                        Text = txt[i], Size = new Size(anchos[i], Tema.AltoControl), Location = new Point(x, y),
                        DialogResult = res[i], TabIndex = i + 1
                    };
                    var v = afirmativo ? (confirmatorio ? Variante.Peligro : Variante.Primario) : Variante.Secundario;
                    Tema.Boton(b, v);
                    if (i == idxDef)
                    {
                        defecto = b;
                        if (v == Variante.Secundario)
                        {
                            b.BorderThickness = 1;
                            b.BorderColor = Tema.BordeFoco;
                            b.HoverState.BorderColor = Tema.BordeFoco;
                        }
                    }
                    var resultado = res[i];
                    b.Click += (s, e) => DialogResult = resultado;
                    _resultados[b] = res[i];
                    Controls.Add(b);
                    x += anchos[i] + 8;
                }
                if (defecto != null) AcceptButton = defecto;
                return esc;
            }

            protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
            {
                if (keyData == Keys.Escape && _escape != DialogResult.None)
                {
                    DialogResult = _escape;
                    return true;
                }
                if (keyData == Keys.Enter)
                {
                    var boton = (ActiveControl as Guna2Button) ?? _defecto;
                    if (boton != null)
                    {
                        boton.PerformClick();
                        return true;
                    }
                }
                return base.ProcessCmdKey(ref msg, keyData);
            }

            private void Centrar(IWin32Window owner)
            {
                try
                {
                    Rectangle refRect;
                    if (owner != null)
                    {
                        var f = owner as Form;
                        if (f != null) refRect = f.Bounds;
                        else
                        {
                            RECT r; GetWindowRect(owner.Handle, out r);
                            refRect = Rectangle.FromLTRB(r.L, r.T, r.R, r.B);
                        }
                    }
                    else refRect = Screen.FromPoint(Cursor.Position).WorkingArea;

                    var area = Screen.FromRectangle(refRect).WorkingArea;
                    int x = refRect.Left + (refRect.Width - Width) / 2;
                    int y = refRect.Top + (refRect.Height - Height) / 2;
                    x = Math.Max(area.Left, Math.Min(x, area.Right - Width));
                    y = Math.Max(area.Top, Math.Min(y, area.Bottom - Height));
                    Location = new Point(x, y);
                }
                catch { }
            }

            public void Sonar()
            {
                try
                {
                    switch (_tipo)
                    {
                        case TipoDialogo.Error: SystemSounds.Hand.Play(); break;
                        case TipoDialogo.Advertencia: SystemSounds.Exclamation.Play(); break;
                        case TipoDialogo.Pregunta: SystemSounds.Question.Play(); break;
                        case TipoDialogo.Info:
                        case TipoDialogo.Exito: SystemSounds.Asterisk.Play(); break;
                    }
                }
                catch { }
            }

            protected override CreateParams CreateParams
            {
                get
                {
                    var cp = base.CreateParams;
                    cp.ClassStyle |= 0x00020000; // CS_DROPSHADOW
                    return cp;
                }
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                // Pie de botones
                using (var br = new SolidBrush(Tema.Fondo))
                    g.FillRectangle(br, BarraAcento, _alturaCuerpo, Width - BarraAcento, Height - _alturaCuerpo);
                using (var pen = new Pen(Tema.Borde))
                    g.DrawLine(pen, BarraAcento, _alturaCuerpo, Width, _alturaCuerpo);

                // Barra de acento
                using (var br = new SolidBrush(_color))
                    g.FillRectangle(br, 0, 0, BarraAcento, Height);

                // Borde 1px del color del tipo
                using (var pen = new Pen(_color, 1f))
                    g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);

                if (_conIcono) DibujarIcono(g, _rectIcono);
            }

            private void DibujarIcono(Graphics g, Rectangle r)
            {
                var rf = new RectangleF(r.X + 0.5f, r.Y + 0.5f, r.Width - 1, r.Height - 1);
                using (var br = new SolidBrush(Color.FromArgb(Tema.Mezclar(Tema.Superficie, _color, 0.20).ToArgb())))
                    g.FillEllipse(br, rf);
                using (var pen = new Pen(Tema.Mezclar(Tema.Superficie, _color, 0.55), 1.5f))
                    g.DrawEllipse(pen, rf);

                float cx = r.X + r.Width / 2f, cy = r.Y + r.Height / 2f;
                using (var pen = new Pen(_color, 3f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                using (var br = new SolidBrush(_color))
                {
                    switch (_tipo)
                    {
                        case TipoDialogo.Exito:
                            g.DrawLines(pen, new[] { new PointF(cx - 8, cy + 0.5f), new PointF(cx - 2.5f, cy + 6), new PointF(cx + 8.5f, cy - 6) });
                            break;
                        case TipoDialogo.Error:
                            g.DrawLine(pen, cx - 6.5f, cy - 6.5f, cx + 6.5f, cy + 6.5f);
                            g.DrawLine(pen, cx - 6.5f, cy + 6.5f, cx + 6.5f, cy - 6.5f);
                            break;
                        case TipoDialogo.Advertencia:
                            g.DrawLine(pen, cx, cy - 9, cx, cy + 2.5f);
                            g.FillEllipse(br, cx - 2f, cy + 6.5f, 4f, 4f);
                            break;
                        case TipoDialogo.Info:
                            g.FillEllipse(br, cx - 2f, cy - 10.5f, 4f, 4f);
                            g.DrawLine(pen, cx, cy - 3.5f, cx, cy + 9);
                            break;
                        case TipoDialogo.Pregunta:
                            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                            var f = Tema.Fuente(19f, true);
                            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                                g.DrawString("?", f, br, new RectangleF(r.X, r.Y - 1, r.Width, r.Height), sf);
                            break;
                    }
                }
            }
        }
    }
}
