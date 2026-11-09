using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Guna.UI2.WinForms;

namespace SteeLog.UI
{
    /// <summary>Variantes visuales de botón.</summary>
    internal enum Variante
    {
        Primario,
        Secundario,
        Peligro,
        Fantasma,
        Tab,
        TabActiva,
        /// <summary>Acción destructiva discreta: contorno rojo y fondo transparente (se rellena al pasar el mouse).</summary>
        PeligroSuave
    }

    /// <summary>
    /// Fuente única de tokens visuales (colores, tipografía, métricas) y helpers que aplican el estilo
    /// en runtime a los controles Guna. Todos los helpers son idempotentes y null-safe.
    /// </summary>
    internal static class Tema
    {
        // ---- Colores ----
        public static readonly Color Fondo = Color.FromArgb(29, 38, 35);
        public static readonly Color Superficie = Color.FromArgb(35, 50, 40);
        public static readonly Color Banda = Color.FromArgb(35, 63, 43);
        public static readonly Color FondoInput = Color.FromArgb(38, 52, 44);
        public static readonly Color Borde = Color.FromArgb(55, 75, 62);
        public static readonly Color BordeFoco = Color.FromArgb(39, 174, 96);
        public static readonly Color Texto = Color.FromArgb(225, 232, 228);
        public static readonly Color TextoSecundario = Color.FromArgb(155, 185, 165);
        public static readonly Color TextoApagado = Color.FromArgb(110, 130, 118);
        public static readonly Color Acento = Color.FromArgb(39, 174, 96);
        public static readonly Color AcentoHover = Color.FromArgb(46, 196, 110);
        public static readonly Color AcentoPressed = Color.FromArgb(25, 135, 84);
        public static readonly Color Secundario = Color.FromArgb(47, 84, 57);
        public static readonly Color SecundarioHover = Color.FromArgb(55, 100, 68);
        public static readonly Color Peligro = Color.FromArgb(180, 55, 48);
        public static readonly Color PeligroHover = Color.FromArgb(198, 70, 60);
        public static readonly Color Advertencia = Color.FromArgb(214, 158, 46);
        public static readonly Color Info = Color.FromArgb(52, 152, 219);
        public static readonly Color DeshabilitadoFill = Color.FromArgb(40, 52, 45);
        public static readonly Color DeshabilitadoTexto = Color.FromArgb(140, 160, 147); // contraste >= 4.5:1 sobre DeshabilitadoFill
        public static readonly Color FilaAlterna = Color.FromArgb(32, 43, 38);
        public static readonly Color Seleccion = Color.FromArgb(47, 84, 57);
        public static readonly Color LineaGrilla = Color.FromArgb(45, 60, 50);

        // ---- Métricas ----
        public const int Radio = 4;
        public const int AltoControl = 36;
        public const int AltoBotonPrincipal = 40;
        public const int AltoFila = 36;
        public const int AltoHeader = 40;
        public const int AltoBanda = 64;
        public const int AltoBarraModal = 40;

        // ---- Tipografía ----
        private const string FamiliaBase = "Bahnschrift";
        private static readonly Dictionary<string, Font> _fuentes = new Dictionary<string, Font>();
        private static HashSet<string> _familias;

        private static bool ExisteFamilia(string nombre)
        {
            if (_familias == null)
            {
                var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                try { foreach (var f in FontFamily.Families) set.Add(f.Name); } catch { }
                _familias = set;
            }
            return _familias.Contains(nombre);
        }

        /// <summary>Fuente Bahnschrift (Light o SemiBold) con fallback a Segoe UI. Instancias compartidas: no disponer.</summary>
        public static Font Fuente(float tamano, bool semiBold = false)
        {
            string fam = FamiliaBase + (semiBold ? " SemiBold" : " Light");
            string key = fam + "|" + tamano.ToString(System.Globalization.CultureInfo.InvariantCulture);
            lock (_fuentes)
            {
                Font f;
                if (_fuentes.TryGetValue(key, out f)) return f;
                if (ExisteFamilia(fam)) f = new Font(fam, tamano, FontStyle.Regular, GraphicsUnit.Point);
                else if (ExisteFamilia(FamiliaBase)) f = new Font(FamiliaBase, tamano, semiBold ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Point);
                else f = new Font("Segoe UI", tamano, semiBold ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Point);
                _fuentes[key] = f;
                return f;
            }
        }

        public static Font FuenteTexto { get { return Fuente(10.5f); } }
        public static Font FuenteBoton { get { return Fuente(10.5f, true); } }
        public static Font FuenteTitulo { get { return Fuente(14f, true); } }
        public static Font FuenteSubtitulo { get { return Fuente(10.5f, true); } }
        public static Font FuenteHeaderGrilla { get { return Fuente(10.5f, true); } }

        // ---- Utilidades gráficas ----
        public static GraphicsPath RectRedondeado(RectangleF r, float radio)
        {
            var p = new GraphicsPath();
            float d = Math.Max(0.01f, radio * 2);
            if (radio <= 0) { p.AddRectangle(r); return p; }
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        public static Color Mezclar(Color fondo, Color frente, double alfa)
        {
            return Color.FromArgb(
                (int)(fondo.R + (frente.R - fondo.R) * alfa),
                (int)(fondo.G + (frente.G - fondo.G) * alfa),
                (int)(fondo.B + (frente.B - fondo.B) * alfa));
        }

        // Guna escala su tamaño al cambiar Font (AutoScaleMode=Font): se conserva la geometría original.
        private static Rectangle GuardarGeometria(Control c) { return c.Bounds; }

        private static void RestaurarGeometria(Control c, Rectangle original)
        {
            if (c.Dock != DockStyle.None || c.Bounds == original) return;
            c.Bounds = original;
        }

        /// <summary>
        /// Estrategia única de escala para vistas hijas: el shell usa una fuente mayor que la del diseñador y AutoScaleMode.Font
        /// multiplicaría por ~1.5 todo lo fijado en código (bandas, inputs, botones). Llamar tras InitializeComponent().
        /// </summary>
        public static void SinEscala(Form f)
        {
            if (f == null) return;
            f.AutoScaleMode = AutoScaleMode.None;
        }

        // ---- Botones ----
        public static void Boton(Guna2Button b, Variante v)
        {
            if (b == null) return;
            Color fill, hover, pressed, texto, borde = Color.Transparent;
            int grosor = 0;
            switch (v)
            {
                case Variante.Primario:
                case Variante.TabActiva:
                    fill = Acento; hover = AcentoHover; pressed = AcentoPressed; texto = Color.White; break;
                case Variante.Peligro:
                    fill = Peligro; hover = PeligroHover; pressed = Color.FromArgb(150, 45, 40); texto = Color.White; break;
                case Variante.Fantasma:
                    fill = Color.Transparent; hover = Secundario; pressed = Color.FromArgb(40, 72, 49);
                    texto = TextoSecundario; borde = Borde; grosor = 1; break;
                case Variante.PeligroSuave:
                    fill = Color.Transparent; hover = Peligro; pressed = Color.FromArgb(150, 45, 40);
                    texto = Color.FromArgb(226, 96, 86); borde = Peligro; grosor = 1; break;
                case Variante.Tab:
                    fill = Secundario; hover = SecundarioHover; pressed = Color.FromArgb(40, 72, 49); texto = Texto; break;
                default:
                    fill = Secundario; hover = SecundarioHover; pressed = Color.FromArgb(40, 72, 49); texto = Texto; break;
            }
            var geo = GuardarGeometria(b);
            try
            {
                b.Animated = false;
                b.BorderRadius = Radio;
                b.BorderThickness = grosor;
                b.BorderColor = borde;
                b.FillColor = fill;
                b.ForeColor = texto;
                b.Font = FuenteBoton;
                b.HoverState.FillColor = hover;
                b.HoverState.ForeColor = v == Variante.Fantasma ? Texto : v == Variante.PeligroSuave ? Color.White : texto;
                b.HoverState.BorderColor = borde;
                b.PressedColor = pressed;
                b.DisabledState.FillColor = DeshabilitadoFill;
                b.DisabledState.ForeColor = DeshabilitadoTexto;
                b.DisabledState.BorderColor = DeshabilitadoFill;
                b.ShadowDecoration.Enabled = false;
                b.Cursor = Cursors.Hand;
                if (v == Variante.Peligro || v == Variante.Primario)
                    b.Invalidate();
            }
            catch { }
            RestaurarGeometria(b, geo);
        }

        // ---- Inputs ----
        public static void Input(Guna2TextBox t)
        {
            if (t == null) return;
            var geo = GuardarGeometria(t);
            try
            {
                t.BorderRadius = Radio;
                t.BorderThickness = 1;
                t.FillColor = Tema.FondoInput;
                t.BorderColor = Borde;
                t.ForeColor = Texto;
                t.PlaceholderForeColor = TextoApagado;
                t.Font = FuenteTexto;
                t.FocusedState.BorderColor = BordeFoco;
                t.FocusedState.FillColor = Tema.FondoInput;
                t.HoverState.BorderColor = Color.FromArgb(75, 105, 85);
                t.DisabledState.FillColor = DeshabilitadoFill;
                t.DisabledState.ForeColor = DeshabilitadoTexto;
                t.DisabledState.BorderColor = Borde;
                t.DisabledState.PlaceholderForeColor = DeshabilitadoTexto;
                t.CustomizableEdges.TopLeft = t.CustomizableEdges.TopRight = true;
                t.CustomizableEdges.BottomLeft = t.CustomizableEdges.BottomRight = true;
            }
            catch { }
            RestaurarGeometria(t, geo);
        }

        public static void Combo(Guna2ComboBox c)
        {
            if (c == null) return;
            var geo = GuardarGeometria(c);
            try
            {
                c.BorderRadius = Radio;
                c.BorderThickness = 1;
                c.FillColor = Tema.FondoInput;
                c.BorderColor = Borde;
                c.ForeColor = Texto;
                c.Font = FuenteTexto;
                c.FocusedColor = BordeFoco;
                c.FocusedState.BorderColor = BordeFoco;
                c.HoverState.BorderColor = Color.FromArgb(75, 105, 85);
                c.ItemsAppearance.BackColor = Tema.FondoInput;
                c.ItemsAppearance.ForeColor = Texto;
                c.ItemsAppearance.SelectedBackColor = Seleccion;
                c.ItemsAppearance.SelectedForeColor = Color.White;
                c.ItemsAppearance.Font = FuenteTexto;
            }
            catch { }
            RestaurarGeometria(c, geo);
        }

        public static void Numerico(Guna2NumericUpDown n)
        {
            if (n == null) return;
            var geo = GuardarGeometria(n);
            try
            {
                n.BorderRadius = Radio;
                n.BorderThickness = 1;
                n.FillColor = Tema.FondoInput;
                n.BorderColor = Borde;
                n.ForeColor = Texto;
                n.Font = FuenteTexto;
                n.UpDownButtonFillColor = Secundario;
                n.UpDownButtonForeColor = Texto;
                n.FocusedState.BorderColor = BordeFoco;
                n.DisabledState.FillColor = DeshabilitadoFill;
                n.DisabledState.ForeColor = DeshabilitadoTexto;
                n.DisabledState.BorderColor = Borde;
            }
            catch { }
            RestaurarGeometria(n, geo);
        }

        public static void Fecha(Guna2DateTimePicker d)
        {
            if (d == null) return;
            var geo = GuardarGeometria(d);
            try
            {
                d.BorderRadius = Radio;
                d.BorderThickness = 1;
                d.FillColor = Tema.FondoInput;
                d.BorderColor = Borde;
                d.ForeColor = Texto;
                d.Font = FuenteTexto;
                d.FocusedColor = BordeFoco;
                d.HoverState.BorderColor = Color.FromArgb(75, 105, 85);
                d.CheckedState.FillColor = Tema.FondoInput;
                d.CheckedState.ForeColor = Texto;
                d.CheckedState.BorderColor = BordeFoco;
            }
            catch { }
            RestaurarGeometria(d, geo);
        }

        public static void Check(Guna2CustomCheckBox c)
        {
            if (c == null) return;
            var geo = GuardarGeometria(c);
            try
            {
                c.CheckedState.BorderRadius = Radio;
                c.CheckedState.BorderThickness = 0;
                c.CheckedState.FillColor = Acento;
                c.CheckedState.BorderColor = Acento;
                c.UncheckedState.BorderRadius = Radio;
                c.UncheckedState.BorderThickness = 2;
                c.UncheckedState.FillColor = Color.Transparent;
                c.UncheckedState.BorderColor = Borde;
                c.Cursor = Cursors.Hand;
            }
            catch { }
            RestaurarGeometria(c, geo);
        }

        public static void Toggle(Guna2ToggleSwitch t)
        {
            if (t == null) return;
            var geo = GuardarGeometria(t);
            try
            {
                t.CheckedState.FillColor = Acento;
                t.CheckedState.BorderColor = Acento;
                t.CheckedState.InnerColor = Color.White;
                t.CheckedState.InnerBorderColor = Color.White;
                t.UncheckedState.FillColor = Color.FromArgb(55, 75, 62);
                t.UncheckedState.BorderColor = Borde;
                t.UncheckedState.InnerColor = TextoSecundario;
                t.UncheckedState.InnerBorderColor = TextoSecundario;
                t.Cursor = Cursors.Hand;
            }
            catch { }
            RestaurarGeometria(t, geo);
        }

        // ---- Título de vista ----
        /// <summary>Banda de título de vista (alto 64, texto a la izquierda en mayúsculas). Reutiliza la existente.</summary>
        public static Panel TituloVista(Control contenedor, string texto)
        {
            if (contenedor == null) return null;
            const string NombrePanel = "pnlTituloVista";
            Panel banda = null;
            foreach (Control c in contenedor.Controls)
                if (c.Name == NombrePanel) { banda = c as Panel; break; }

            if (banda == null)
            {
                banda = new Panel
                {
                    Name = NombrePanel,
                    Dock = DockStyle.Top,
                    Height = AltoBanda,
                    BackColor = Banda
                };
                var lbl = new Label
                {
                    Name = "lblTituloVista",
                    AutoSize = false,
                    Dock = DockStyle.Fill,
                    Padding = new Padding(28, 0, 16, 0),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Font = FuenteTitulo,
                    ForeColor = Color.White,
                    BackColor = Color.Transparent,
                    UseCompatibleTextRendering = false
                };
                banda.Controls.Add(lbl);
                contenedor.Controls.Add(banda);
                banda.BringToFront();
            }
            var lblT = banda.Controls["lblTituloVista"] as Label;
            if (lblT != null) lblT.Text = (texto ?? string.Empty).ToUpper(System.Globalization.CultureInfo.CurrentCulture);
            return banda;
        }

        // ---- Scrollbars oscuras ----
        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hwnd, string subAppName, string subIdList);

        private sealed class EstadoScroll { public readonly HashSet<IntPtr> Aplicados = new HashSet<IntPtr>(); }
        private static readonly ConditionalWeakTable<Control, EstadoScroll> _scrolls = new ConditionalWeakTable<Control, EstadoScroll>();

        private static void AplicarTemaOscuro(Control c, EstadoScroll st)
        {
            try
            {
                if (c.IsHandleCreated && st.Aplicados.Add(c.Handle)) SetWindowTheme(c.Handle, "DarkMode_Explorer", null);
                foreach (Control hijo in c.Controls)
                    if (hijo is ScrollBar && hijo.IsHandleCreated && st.Aplicados.Add(hijo.Handle))
                        SetWindowTheme(hijo.Handle, "DarkMode_Explorer", null);
            }
            catch { }
        }

        /// <summary>
        /// Barras de desplazamiento oscuras (DarkMode_Explorer) en controles nativos y en las ScrollBar hijas de un DataGridView.
        /// Idempotente; se reaplica cuando aparecen barras nuevas (se crean perezosamente al haber más filas que alto).
        /// </summary>
        public static void ScrollbarsOscuros(Control c)
        {
            if (c == null) return;
            EstadoScroll st;
            if (_scrolls.TryGetValue(c, out st)) { AplicarTemaOscuro(c, st); return; }
            st = new EstadoScroll();
            _scrolls.Add(c, st);
            Action aplicar = () => AplicarTemaOscuro(c, st);
            c.HandleCreated += (s, e) => { st.Aplicados.Clear(); aplicar(); };
            c.ControlAdded += (s, e) => { if (e.Control is ScrollBar) e.Control.HandleCreated += (s2, e2) => aplicar(); };
            var dgv = c as DataGridView;
            if (dgv != null)
            {
                dgv.DataBindingComplete += (s, e) => aplicar();
                dgv.RowsAdded += (s, e) => aplicar();
                dgv.SizeChanged += (s, e) => aplicar();
            }
            aplicar();
        }

        // ---- Grilla ----
        private sealed class EstadoGrilla { public int HoverFila = -1, HoverCol = -1; }
        private static readonly ConditionalWeakTable<DataGridView, EstadoGrilla> _grillas =
            new ConditionalWeakTable<DataGridView, EstadoGrilla>();

        public static void Grilla(Guna2DataGridView g)
        {
            if (g == null) return;
            try
            {
                g.EnableHeadersVisualStyles = false;
                g.BackgroundColor = Fondo;
                g.GridColor = LineaGrilla;
                g.BorderStyle = BorderStyle.None;
                g.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
                g.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
                g.RowHeadersVisible = false;
                g.RowTemplate.Height = AltoFila;
                g.RowTemplate.DefaultCellStyle.Padding = new Padding(8, 0, 8, 0);
                g.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
                g.ColumnHeadersHeight = AltoHeader;

                var th = g.ThemeStyle;
                th.BackColor = Fondo;
                th.GridColor = LineaGrilla;
                th.HeaderStyle.BackColor = Banda;
                th.HeaderStyle.ForeColor = Color.White;
                th.HeaderStyle.Font = FuenteHeaderGrilla;
                th.HeaderStyle.BorderStyle = DataGridViewHeaderBorderStyle.None;
                th.HeaderStyle.Height = AltoHeader;
                th.RowsStyle.BackColor = Fondo;
                th.RowsStyle.ForeColor = Texto;
                th.RowsStyle.Font = FuenteTexto;
                th.RowsStyle.SelectionBackColor = Seleccion;
                th.RowsStyle.SelectionForeColor = Color.White;
                th.RowsStyle.Height = AltoFila;
                th.AlternatingRowsStyle.BackColor = FilaAlterna;
                th.AlternatingRowsStyle.ForeColor = Texto;
                th.AlternatingRowsStyle.Font = FuenteTexto;
                th.AlternatingRowsStyle.SelectionBackColor = Seleccion;
                th.AlternatingRowsStyle.SelectionForeColor = Color.White;

                var hs = g.ColumnHeadersDefaultCellStyle;
                hs.BackColor = Banda;
                hs.ForeColor = Color.White;
                hs.Font = FuenteHeaderGrilla;
                hs.SelectionBackColor = Banda;
                hs.SelectionForeColor = Color.White;
                hs.WrapMode = DataGridViewTriState.False;
                hs.Padding = new Padding(12, 0, 8, 0);
                hs.Alignment = DataGridViewContentAlignment.MiddleLeft;

                var ds = g.DefaultCellStyle;
                ds.BackColor = Fondo;
                ds.ForeColor = Texto;
                ds.Font = FuenteTexto;
                ds.SelectionBackColor = Seleccion;
                ds.SelectionForeColor = Color.White;
                ds.Padding = new Padding(12, 0, 8, 0);
                ds.WrapMode = DataGridViewTriState.False;

                var al = g.AlternatingRowsDefaultCellStyle;
                al.BackColor = FilaAlterna;
                al.ForeColor = Texto;
                al.Font = FuenteTexto;
                al.SelectionBackColor = Seleccion;
                al.SelectionForeColor = Color.White;
                al.Padding = new Padding(12, 0, 8, 0);

                g.RowHeadersDefaultCellStyle.BackColor = Banda;
                g.AllowUserToResizeRows = false;
                g.RowsDefaultCellStyle.BackColor = Fondo;
            }
            catch { }

            ScrollbarsOscuros(g);
            EstadoGrilla estado;
            if (_grillas.TryGetValue(g, out estado)) return; // ya enganchada
            estado = new EstadoGrilla();
            _grillas.Add(g, estado);

            g.CellPainting += (s, e) => PintarCelda((DataGridView)s, e, estado);
            g.CellMouseEnter += (s, e) => CambiarHover((DataGridView)s, estado, e.RowIndex, e.ColumnIndex);
            g.CellMouseLeave += (s, e) => CambiarHover((DataGridView)s, estado, -1, -1);
            // Las filas creadas antes de aplicar el tema no toman RowTemplate.Height. El ajuste se hace
            // fuera del binding (BeginInvoke): tocar Row.Height dentro de RowsAdded mientras el DataSource
            // regenera columnas provoca repintados reentrantes (NullReferenceException en PaintPrivate).
            g.DataBindingComplete += (s, e) => AjustarAltoFilasDiferido((DataGridView)s);
            AjustarAltoFilasDiferido(g);
        }

        private static void AjustarAltoFilasDiferido(DataGridView g)
        {
            Action ajustar = () =>
            {
                try
                {
                    if (g.IsDisposed || g.AutoSizeRowsMode != DataGridViewAutoSizeRowsMode.None) return;
                    foreach (DataGridViewRow r in g.Rows)
                        if (!r.IsNewRow && r.Height < AltoFila) r.Height = AltoFila;
                }
                catch (InvalidOperationException) { }
            };
            if (g.IsHandleCreated) g.BeginInvoke(ajustar); else ajustar();
        }

        private static void CambiarHover(DataGridView g, EstadoGrilla st, int fila, int col)
        {
            int of = st.HoverFila, oc = st.HoverCol;
            st.HoverFila = fila; st.HoverCol = col;
            try
            {
                if (of >= 0 && oc >= 0 && of < g.RowCount && oc < g.ColumnCount) g.InvalidateCell(oc, of);
                if (fila >= 0 && col >= 0 && fila < g.RowCount && col < g.ColumnCount) g.InvalidateCell(col, fila);
            }
            catch { }
        }

        private static bool EsCentrada(DataGridViewContentAlignment a)
        {
            return a == DataGridViewContentAlignment.MiddleCenter || a == DataGridViewContentAlignment.TopCenter || a == DataGridViewContentAlignment.BottomCenter;
        }

        private static bool EsDerecha(DataGridViewContentAlignment a)
        {
            return a == DataGridViewContentAlignment.MiddleRight || a == DataGridViewContentAlignment.TopRight || a == DataGridViewContentAlignment.BottomRight;
        }

        /// <summary>El encabezado sin alineación propia hereda la de las celdas de su columna (centrada o a la derecha).</summary>
        private static void AlinearEncabezado(DataGridView g, int indiceColumna)
        {
            if (indiceColumna < 0 || indiceColumna >= g.Columns.Count) return;
            var col = g.Columns[indiceColumna];
            if (col.HeaderCell.Style.Alignment != DataGridViewContentAlignment.NotSet) return;
            var celda = col.DefaultCellStyle.Alignment;
            if (EsCentrada(celda)) col.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            else if (EsDerecha(celda)) col.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
        }

        private static void PintarCelda(DataGridView g, DataGridViewCellPaintingEventArgs e, EstadoGrilla st)
        {
            if (e.RowIndex == -1 && e.ColumnIndex >= 0) { AlinearEncabezado(g, e.ColumnIndex); return; }
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || e.Graphics == null) return;
            var col = g.Columns[e.ColumnIndex];
            var celda = g.Rows[e.RowIndex].Cells[e.ColumnIndex];
            bool esCheck = celda is DataGridViewCheckBoxCell;
            bool esBoton = celda is DataGridViewButtonCell;
            if (!esCheck && !esBoton) return;

            e.PaintBackground(e.ClipBounds, true);
            var gr = e.Graphics;
            var modo = gr.SmoothingMode;
            gr.SmoothingMode = SmoothingMode.AntiAlias;
            try
            {
                if (esCheck) PintarCheck(gr, e);
                else PintarBoton(gr, e, col, st.HoverFila == e.RowIndex && st.HoverCol == e.ColumnIndex);
            }
            finally { gr.SmoothingMode = modo; }
            e.Handled = true;
        }

        private static void PintarCheck(Graphics gr, DataGridViewCellPaintingEventArgs e)
        {
            bool marcado = false;
            var v = e.FormattedValue;
            if (v is bool) marcado = (bool)v;
            else if (v is CheckState) marcado = (CheckState)v != CheckState.Unchecked;
            bool soloLectura = e.CellStyle != null && e.State.HasFlag(DataGridViewElementStates.ReadOnly);

            const int lado = 18;
            var r = new RectangleF(
                e.CellBounds.X + (e.CellBounds.Width - lado) / 2f,
                e.CellBounds.Y + (e.CellBounds.Height - lado) / 2f, lado, lado);

            if (marcado)
            {
                using (var p = RectRedondeado(r, Radio))
                using (var br = new SolidBrush(soloLectura ? Mezclar(Fondo, Acento, 0.55) : Acento))
                    gr.FillPath(br, p);
                using (var pen = new Pen(Color.White, 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                {
                    gr.DrawLines(pen, new[]
                    {
                        new PointF(r.X + 4.5f, r.Y + 9.5f),
                        new PointF(r.X + 7.8f, r.Y + 12.8f),
                        new PointF(r.X + 13.5f, r.Y + 5.8f)
                    });
                }
            }
            else
            {
                var rb = new RectangleF(r.X + 0.75f, r.Y + 0.75f, r.Width - 1.5f, r.Height - 1.5f);
                using (var p = RectRedondeado(rb, Radio))
                using (var pen = new Pen(soloLectura ? Color.FromArgb(50, 66, 57) : Color.FromArgb(95, 125, 106), 1.5f))
                    gr.DrawPath(pen, p);
            }
        }

        private static void PintarBoton(Graphics gr, DataGridViewCellPaintingEventArgs e, DataGridViewColumn col, bool hover)
        {
            var variante = col.Tag is Variante ? (Variante)col.Tag : Variante.Secundario;
            Color fill, fillHover;
            switch (variante)
            {
                case Variante.Primario: fill = Acento; fillHover = AcentoHover; break;
                case Variante.Peligro: fill = Peligro; fillHover = PeligroHover; break;
                case Variante.Fantasma: fill = Color.FromArgb(38, 52, 44); fillHover = Secundario; break;
                // Más claro que Seleccion para que la pastilla no se pierda en filas seleccionadas.
                default: fill = Color.FromArgb(62, 104, 76); fillHover = Color.FromArgb(76, 122, 90); break;
            }
            var r = new RectangleF(e.CellBounds.X + 6, e.CellBounds.Y + 4, e.CellBounds.Width - 12, e.CellBounds.Height - 9);
            if (r.Width < 8 || r.Height < 8) return;
            using (var p = RectRedondeado(r, Radio))
            using (var br = new SolidBrush(hover ? fillHover : fill))
                gr.FillPath(br, p);

            string texto = e.FormattedValue as string ?? (e.Value != null ? e.Value.ToString() : string.Empty);
            if (string.IsNullOrEmpty(texto) && col is DataGridViewButtonColumn)
                texto = ((DataGridViewButtonColumn)col).Text ?? string.Empty;
            TextRenderer.DrawText(gr, texto, Fuente(9.5f, true), Rectangle.Round(r), Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
        }

        // ---- Chrome de modales ----
        [DllImport("user32.dll")] private static extern bool ReleaseCapture();
        [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
        [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int val, int size);
        [DllImport("dwmapi.dll")] private static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref Margins m);
        [StructLayout(LayoutKind.Sequential)] private struct Margins { public int L, R, T, B; }

        /// <summary>Arrastre de ventana borderless desde cualquier control.</summary>
        internal static void HabilitarArrastre(Control origen, Form ventana)
        {
            if (origen == null || ventana == null) return;
            origen.MouseDown += (s, e) =>
            {
                if (e.Button != MouseButtons.Left || !ventana.IsHandleCreated) return;
                ReleaseCapture();
                SendMessage(ventana.Handle, 0xA1 /*WM_NCLBUTTONDOWN*/, (IntPtr)2 /*HTCAPTION*/, IntPtr.Zero);
            };
        }

        /// <summary>Sombra del sistema para ventanas sin borde (DWM).</summary>
        internal static void SombraDwm(Form f)
        {
            if (f == null) return;
            Action aplicar = () =>
            {
                try
                {
                    int politica = 2;
                    DwmSetWindowAttribute(f.Handle, 2 /*DWMWA_NCRENDERING_POLICY*/, ref politica, 4);
                    var m = new Margins { L = 1, R = 1, T = 1, B = 1 };
                    DwmExtendFrameIntoClientArea(f.Handle, ref m);
                }
                catch { }
            };
            if (f.IsHandleCreated) aplicar(); else f.HandleCreated += (s, e) => aplicar();
        }

        private sealed class BotonCerrar : Control
        {
            private bool _hover;
            public BotonCerrar()
            {
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
                Cursor = Cursors.Hand;
                TabStop = false;
            }
            protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
            protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }
            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.Clear(_hover ? Peligro : Banda);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                float cx = Width / 2f, cy = Height / 2f;
                using (var pen = new Pen(_hover ? Color.White : TextoSecundario, 1.6f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                {
                    g.DrawLine(pen, cx - 5, cy - 5, cx + 5, cy + 5);
                    g.DrawLine(pen, cx - 5, cy + 5, cx + 5, cy - 5);
                }
            }
        }

        /// <summary>Corrige texto UTF-8 leído como Latin-1 ("SelecciÃ³n" -> "Selección").</summary>
        internal static string ArreglarMojibake(string s)
        {
            if (string.IsNullOrEmpty(s) || (s.IndexOf('Ã') < 0 && s.IndexOf('Â') < 0)) return s;
            try
            {
                var bytes = System.Text.Encoding.GetEncoding(1252).GetBytes(s);
                var corregido = new System.Text.UTF8Encoding(false, true).GetString(bytes);
                return corregido;
            }
            catch { return s; }
        }

        /// <summary>
        /// Convierte un form modal en ventana borderless con barra de título propia (alto 40), borde 1px,
        /// sombra, arrastre y Esc para cancelar. Llamar tras InitializeComponent(). Idempotente.
        /// </summary>
        public static void ChromeModal(Form f, string titulo)
        {
            if (f == null) return;
            const string NombreBarra = "pnlChromeModal";
            if (f.Controls.ContainsKey(NombreBarra))
            {
                var existente = f.Controls[NombreBarra].Controls["lblChromeTitulo"] as Label;
                if (existente != null && titulo != null) existente.Text = ArreglarMojibake(titulo);
                return;
            }

            string texto = ArreglarMojibake(titulo ?? f.Text);
            f.Text = texto;
            int clientH = f.ClientSize.Height, clientW = f.ClientSize.Width;
            f.SuspendLayout();
            try
            {
                f.FormBorderStyle = FormBorderStyle.None;
                f.MaximizeBox = false;
                f.MinimizeBox = false;
                f.ControlBox = false;
                f.ShowIcon = false;
                f.ShowInTaskbar = false;
                f.BackColor = Fondo;
                f.KeyPreview = true;
                f.ClientSize = new Size(clientW, clientH + AltoBarraModal);

                // Desplaza lo que está posicionado a mano para dejar sitio a la barra.
                foreach (Control c in f.Controls)
                    if (c.Dock == DockStyle.None) c.Top += AltoBarraModal;

                var barra = new Panel { Name = NombreBarra, Dock = DockStyle.Top, Height = AltoBarraModal, BackColor = Banda };
                var lbl = new Label
                {
                    Name = "lblChromeTitulo",
                    Text = texto,
                    Dock = DockStyle.Fill,
                    AutoSize = false,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(20, 0, 0, 0),
                    Font = Fuente(11f, true),
                    ForeColor = Color.White,
                    BackColor = Color.Transparent,
                    UseCompatibleTextRendering = false
                };
                var cerrar = new BotonCerrar { Name = "btnChromeCerrar", Dock = DockStyle.Right, Width = 48 };
                cerrar.Click += (s, e) =>
                {
                    if (f.Modal) f.DialogResult = DialogResult.Cancel; else f.Close();
                };
                barra.Controls.Add(lbl);
                barra.Controls.Add(cerrar);
                f.Controls.Add(barra);
                barra.BringToFront();
                HabilitarArrastre(barra, f);
                HabilitarArrastre(lbl, f);

                f.Paint += (s, e) =>
                {
                    using (var pen = new Pen(Borde, 1f))
                        e.Graphics.DrawRectangle(pen, 0, 0, f.ClientSize.Width - 1, f.ClientSize.Height - 1);
                };
                f.KeyDown += (s, e) =>
                {
                    if (e.KeyCode != Keys.Escape || e.Handled) return;
                    e.Handled = true;
                    if (f.Modal) f.DialogResult = DialogResult.Cancel; else f.Close();
                };
                SombraDwm(f);
            }
            finally { f.ResumeLayout(true); }
        }
    }
}
