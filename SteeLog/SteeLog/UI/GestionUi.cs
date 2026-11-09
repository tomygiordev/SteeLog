namespace SteeLog.UI
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Windows.Forms;
    using Guna.UI2.WinForms;

    /// <summary>
    /// Estilo común de los ABM de Gestión: banda de título, bloque de formulario, subtítulo de listado y grilla.
    /// Aplica los tokens de <see cref="Tema"/> en runtime sin tocar la lógica de cada vista.
    /// </summary>
    internal static class GestionUi
    {
        internal const int Pad = 24;
        internal const int AnchoBoton = 140;
        private const int EtiquetaPrimera = 112;

        /// <summary>Confirmación estándar (las eliminaciones usan Warning y salen como destructivas).</summary>
        internal static bool Confirmar(string mensaje, string titulo, MessageBoxIcon icono)
        {
            bool destructivo = icono == MessageBoxIcon.Warning || icono == MessageBoxIcon.Error;
            return Dialogo.Confirmar(mensaje, string.IsNullOrEmpty(titulo) ? "Confirmar" : titulo, destructivo);
        }

        // ---- Controles ----
        internal static void Aplicar(Control raiz)
        {
            if (raiz == null) return;
            foreach (Control c in raiz.Controls)
            {
                Estilizar(c);
                if (c.HasChildren) Aplicar(c);
            }
        }

        private static void Estilizar(Control c)
        {
            var txt = c as Guna2TextBox;
            if (txt != null)
            {
                Tema.Input(txt);
                if (!txt.Multiline && txt.Dock == DockStyle.None) txt.Height = Tema.AltoControl;
                return;
            }
            var cmb = c as Guna2ComboBox;
            if (cmb != null)
            {
                Tema.Combo(cmb);
                try
                {
                    cmb.DisabledState.FillColor = Tema.DeshabilitadoFill;
                    cmb.DisabledState.ForeColor = Tema.DeshabilitadoTexto;
                    cmb.DisabledState.BorderColor = Tema.Borde;
                }
                catch { }
                cmb.ItemHeight = 30;
                if (cmb.Dock == DockStyle.None) cmb.Height = Tema.AltoControl;
                return;
            }
            var num = c as Guna2NumericUpDown;
            if (num != null)
            {
                Tema.Numerico(num);
                num.BackColor = Color.Transparent;
                if (num.Dock == DockStyle.None) num.Height = Tema.AltoControl;
                return;
            }
            var fec = c as Guna2DateTimePicker;
            if (fec != null)
            {
                Tema.Fecha(fec);
                if (fec.Dock == DockStyle.None) fec.Height = Tema.AltoControl;
                return;
            }
            var chk = c as Guna2CustomCheckBox;
            if (chk != null) { Tema.Check(chk); return; }
            var grilla = c as Guna2DataGridView;
            if (grilla != null) { Tema.Grilla(grilla); return; }
            var sb = c as Guna2VScrollBar;
            if (sb != null)
            {
                sb.FillColor = Tema.Fondo;
                sb.ThumbColor = Color.FromArgb(70, 100, 82);
                return;
            }
            var lbl = c as Label;
            if (lbl != null && lbl.Font != null && !lbl.Font.Name.StartsWith("Bahnschrift", System.StringComparison.OrdinalIgnoreCase))
                lbl.Font = Tema.Fuente(10.5f);
        }

        /// <summary>Columnas de checkbox con el encabezado centrado sobre la casilla y primera columna alineada al margen de 24.</summary>
        internal static void AjustarColumnas(DataGridView g)
        {
            if (g == null) return;
            bool primera = true;
            foreach (DataGridViewColumn col in g.Columns)
            {
                if (!col.Visible) continue;
                if (col is DataGridViewCheckBoxColumn)
                {
                    col.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }
                if (primera)
                {
                    col.HeaderCell.Style.Padding = new Padding(Pad, 0, 8, 0);
                    col.DefaultCellStyle.Padding = new Padding(Pad, 0, 8, 0);
                    primera = false;
                }
            }
        }

        /// <summary>Lista con casillas del tema (ver <see cref="ListaChecksTema"/>): sin bordes nativos y con barras oscuras.</summary>
        internal static void ListaChecks(CheckedListBox l)
        {
            if (l == null) return;
            l.ItemHeight = 30;
            l.BackColor = Tema.FondoInput;
            l.ForeColor = Tema.Texto;
            l.BorderStyle = BorderStyle.None;
            l.Font = Tema.Fuente(10.5f);
            l.IntegralHeight = false;
            Tema.ScrollbarsOscuros(l);
        }

        // ---- Bloques ----
        /// <summary>Banda de título: alto 64, texto en mayúsculas a la izquierda.</summary>
        internal static void Banda(Control panel, Label titulo)
        {
            if (panel == null) return;
            panel.Height = Tema.AltoBanda;
            panel.BackColor = Tema.Banda;
            panel.Padding = new Padding(28, 0, 16, 0);
            if (titulo == null) return;
            titulo.Margin = Padding.Empty;
            titulo.Padding = Padding.Empty;
            titulo.Font = Tema.FuenteTitulo;
            titulo.ForeColor = Color.White;
            titulo.BackColor = Color.Transparent;
            titulo.Text = (titulo.Text ?? string.Empty).ToUpper(System.Globalization.CultureInfo.CurrentCulture);
        }

        /// <summary>Franja "LISTADO DE X" (opcionalmente con buscador a la derecha).</summary>
        internal static void Subtitulo(Control panel, Label etiqueta)
        {
            if (panel == null) return;
            panel.BackColor = Tema.Fondo;
            panel.Height = 52;
            panel.Padding = new Padding(Pad, 0, Pad, 0);
            if (etiqueta == null) return;
            etiqueta.Margin = new Padding(0, 0, 12, 0);
            etiqueta.Font = Tema.FuenteSubtitulo;
            etiqueta.ForeColor = Tema.TextoSecundario;
            etiqueta.BackColor = Color.Transparent;
        }

        /// <summary>Fila única de formulario: etiqueta 112, campo que se estira, botones de 140 a la derecha.</summary>
        internal static void FormularioFila(TableLayoutPanel t, int arriba = 14, int abajo = 14, bool superficie = true)
        {
            if (t == null) return;
            t.BackColor = superficie ? Tema.Superficie : Tema.Fondo;
            t.Padding = new Padding(Pad, arriba, Pad, abajo);
            int filas = Math.Max(1, t.RowStyles.Count);
            int altoFila = filas > 1 ? Tema.AltoControl + 12 : Tema.AltoControl;
            t.Height = altoFila * filas + arriba + abajo;
            for (int r = 0; r < t.RowStyles.Count; r++)
            {
                if (filas == 1) { t.RowStyles[r].SizeType = SizeType.Percent; t.RowStyles[r].Height = 100F; }
                else { t.RowStyles[r].SizeType = SizeType.Absolute; t.RowStyles[r].Height = altoFila; }
            }
            for (int col = 0; col < t.ColumnStyles.Count; col++)
            {
                var cs = t.ColumnStyles[col];
                var enCol = new List<Control>();
                foreach (Control c in t.Controls) if (t.GetColumn(c) == col) enCol.Add(c);
                if (enCol.Count == 0) continue; // columnas separadoras: se respetan
                bool hayBoton = enCol.Count > 0 && enCol.All(c => c is Guna2Button);
                bool soloEtiquetas = enCol.Count > 0 && enCol.All(c => c is Label);
                bool hayCheck = enCol.Any(c => c is Guna2CustomCheckBox);
                if (hayBoton)
                {
                    cs.SizeType = SizeType.Absolute; cs.Width = AnchoBoton + 8;
                    foreach (var c in enCol) { c.Margin = new Padding(4, 0, col == t.ColumnStyles.Count - 1 ? 0 : 4, 0); c.Height = Tema.AltoControl; }
                }
                else if (soloEtiquetas)
                {
                    int ancho = 0;
                    cs.SizeType = SizeType.Absolute;
                    foreach (Label l in enCol)
                    {
                        if (t.GetColumnSpan(l) > 1) continue; // títulos que abarcan varias columnas
                        l.Font = Tema.Fuente(10.5f);
                        l.ForeColor = Tema.TextoSecundario;
                        l.AutoSize = false; l.Dock = DockStyle.Fill;
                        l.TextAlign = ContentAlignment.MiddleLeft;
                        if (col == 0) { l.Padding = Padding.Empty; l.Margin = Padding.Empty; }
                        else l.Padding = new Padding(16, 0, 6, 0);
                        ancho = Math.Max(ancho, TextRenderer.MeasureText(l.Text, l.Font).Width + (col == 0 ? 8 : 32));
                    }
                    cs.Width = col == 0 ? Math.Max(EtiquetaPrimera, ancho) : ancho;
                }
                else if (hayCheck)
                {
                    cs.SizeType = SizeType.Absolute; cs.Width = 30;
                    foreach (var c in enCol) c.Margin = new Padding(0, 0, 8, 0);
                }
                else
                {
                    if (cs.SizeType == SizeType.AutoSize) { cs.SizeType = SizeType.Percent; cs.Width = 100F; }
                    foreach (var c in enCol) c.Margin = new Padding(0, 0, col == t.ColumnStyles.Count - 1 ? 0 : 8, 0);
                }
            }
        }

        /// <summary>Pestaña interna ("Ver X" / "Ver historial"): Tab o TabActiva, nunca gris claro al deshabilitarse.</summary>
        internal static void Tab(Guna2Button b, bool activa)
        {
            if (b == null) return;
            Tema.Boton(b, activa ? Variante.TabActiva : Variante.Tab);
            if (activa)
            {
                b.DisabledState.FillColor = Tema.Mezclar(Tema.Fondo, Tema.Acento, 0.45);
                b.DisabledState.BorderColor = b.DisabledState.FillColor;
                b.DisabledState.ForeColor = Tema.Mezclar(Tema.Fondo, Color.White, 0.55);
            }
        }

        /// <summary>Franja de pestañas internas alineadas a la izquierda.</summary>
        internal static void Tabs(TableLayoutPanel panel, params Guna2Button[] botones)
        {
            if (panel == null) return;
            panel.BackColor = Tema.Fondo;
            panel.Padding = new Padding(Pad, 12, Pad, 4);
            panel.Height = Tema.AltoControl + 16;
            if (panel.RowStyles.Count > 0) { panel.RowStyles[0].SizeType = SizeType.Percent; panel.RowStyles[0].Height = 100F; }
            panel.ColumnStyles.Clear();
            panel.ColumnCount = botones.Length + 1;
            for (int i = 0; i < botones.Length; i++)
            {
                panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 176));
                panel.Controls.Add(botones[i], i, 0);
                botones[i].Anchor = AnchorStyles.Left | AnchorStyles.Right;
                botones[i].Margin = new Padding(0, 0, 8, 0);
                botones[i].Height = Tema.AltoControl;
            }
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        }

        /// <summary>Banda + bloques de formulario apilados (de 1 a N filas) + botones de acción; el resto lo resuelve cada vista.</summary>
        internal static void Abm(Form f, Control banda, Label titulo, TableLayoutPanel[] bloques, Guna2Button guardar, Guna2Button eliminar, Guna2Button pdf = null)
        {
            Tema.SinEscala(f);
            f.BackColor = Tema.Fondo;
            Banda(banda, titulo);
            Aplicar(f);
            Tema.Boton(guardar, Variante.Primario);
            Tema.Boton(eliminar, Variante.Peligro);
            if (pdf != null) Tema.Boton(pdf, Variante.Secundario);
            for (int i = 0; i < bloques.Length; i++)
                FormularioFila(bloques[i], i == 0 ? 16 : 4, i == bloques.Length - 1 ? 16 : 4);
        }

        /// <summary>Fila de filtros (etiqueta + campo, repetido) debajo de las pestañas.</summary>
        internal static void Filtros(TableLayoutPanel t)
        {
            FormularioFila(t, 4, 12, false);
        }

        /// <summary>Fila única de formulario + banda + subtítulo + grilla a pantalla completa (Categorías, Proveedores, Obras, Ubicaciones, Fletes).</summary>
        internal static void AbmSimple(Form f, Control banda, Label titulo, TableLayoutPanel formulario, Control franja, Label subtitulo,
            Control contenedorGrilla, Guna2Button guardar, Guna2Button eliminar)
        {
            Tema.SinEscala(f);
            f.BackColor = Tema.Fondo;
            Banda(banda, titulo);
            Aplicar(f);
            Tema.Boton(guardar, Variante.Primario);
            Tema.Boton(eliminar, Variante.Peligro);
            FormularioFila(formulario);
            Subtitulo(franja, subtitulo);
            contenedorGrilla.Dock = DockStyle.Fill;
            contenedorGrilla.BackColor = Tema.Fondo;
        }
    }
}

