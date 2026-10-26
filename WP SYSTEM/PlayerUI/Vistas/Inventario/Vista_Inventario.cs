using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Guna.UI2.WinForms;
using WP_Inventario.Datos;
using WP_Inventario.Servicios;
using WP_Inventario;

namespace WP_Inventario.Vistas.Inventario
{
    public partial class Vista_Inventario : Form, IRefrescable
    {
        private string tabActual = "materiales";
        private InventarioDatos datos = new InventarioDatos();

        // Filtros dinámicos
        private Guna2TextBox txtBusqueda;
        private Guna2ComboBox cmbCategoria;
        private Guna2ComboBox cmbEstado;
        private Guna2ComboBox cmbObra;
        private Guna2Button btnLimpiar;
        private Guna2Button btnPaquete;
        private bool modoPaquete = false;
        private Label lblCategoria;
        private Label lblEstado;
        private Label lblObra;

        // Resumen
        private Label lblResumen1;
        private Label lblResumen2;
        private Label lblResumen3;
        private Label lblResumen4;

        // Tabs
        private Guna2Button btnTabMateriales;
        private Guna2Button btnTabRecortes;
        private Guna2Button btnTabHerramientas;
        private Guna2Button btnTabInsumos;

        // Grid
        private Guna2DataGridView dgvInventario;

        // Colores
        private readonly Color colorFondo = Color.FromArgb(29, 38, 35);
        private readonly Color colorPanelOscuro = Color.FromArgb(20, 30, 25);
        private readonly Color colorVerdeAccent = Color.FromArgb(25, 135, 84);
        private readonly Color colorVerdeOscuro = Color.FromArgb(35, 63, 43);
        private readonly Color colorVerdeBoton = Color.FromArgb(47, 84, 57);
        private readonly Color colorFilaAlt = Color.FromArgb(35, 50, 40);
        private readonly Color colorFila = Color.FromArgb(28, 40, 32);
        private readonly Color colorSeleccion = Color.FromArgb(50, 90, 60);
        private readonly Color colorTexto = Color.White;
        private readonly Color colorBorde = Color.FromArgb(60, 60, 65);

        public Vista_Inventario()
        {
            InitializeComponent();
            CrearInterfaz();
            CargarFiltros();
            CargarResumen();
            CargarTab("materiales");
        }

        public void Refrescar()
        {
            CargarResumen();
            CargarTab(tabActual);
        }

        private void CrearInterfaz()
        {
            // ============ TÍTULO ============
            var panelTitulo = new PanelAntiParpadeo();
            panelTitulo.Dock = DockStyle.Top;
            panelTitulo.Height = 50;
            panelTitulo.ColumnCount = 1;
            panelTitulo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            panelTitulo.RowCount = 1;
            panelTitulo.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            panelTitulo.Padding = new Padding(40, 0, 40, 0);

            var lblTitulo = new Label
            {
                Text = "INVENTARIO GENERAL",
                Font = new Font("Bahnschrift SemiBold", 22F, FontStyle.Bold),
                ForeColor = colorTexto,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            panelTitulo.Controls.Add(lblTitulo, 0, 0);

            // ============ RESUMEN (4 tarjetas) ============
            var panelResumen = new PanelAntiParpadeo();
            panelResumen.Dock = DockStyle.Top;
            panelResumen.Height = 48;
            panelResumen.ColumnCount = 4;
            panelResumen.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            panelResumen.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            panelResumen.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            panelResumen.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            panelResumen.RowCount = 1;
            panelResumen.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            panelResumen.Padding = new Padding(40, 0, 40, 0);

            Font fntResumen = new Font("Bahnschrift Light", 10.5F);

            lblResumen1 = new Label { Text = "📦 Materiales: —", Font = fntResumen, ForeColor = colorVerdeAccent, TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Fill };
            lblResumen2 = new Label { Text = "✂ Recortes: —", Font = fntResumen, ForeColor = colorVerdeAccent, TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Fill };
            lblResumen3 = new Label { Text = "🔧 Herramientas: —", Font = fntResumen, ForeColor = colorVerdeAccent, TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Fill };
            lblResumen4 = new Label { Text = "📋 Insumos: —", Font = fntResumen, ForeColor = colorVerdeAccent, TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Fill };

            panelResumen.Controls.Add(lblResumen1, 0, 0);
            panelResumen.Controls.Add(lblResumen2, 1, 0);
            panelResumen.Controls.Add(lblResumen3, 2, 0);
            panelResumen.Controls.Add(lblResumen4, 3, 0);

            // ============ TABS ============
            var panelTabs = new PanelAntiParpadeo();
            panelTabs.Dock = DockStyle.Top;
            panelTabs.Height = 42;
            panelTabs.ColumnCount = 4;
            panelTabs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            panelTabs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            panelTabs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            panelTabs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            panelTabs.RowCount = 1;
            panelTabs.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            panelTabs.Padding = new Padding(40, 0, 40, 0);

            Font fntTab = new Font("Bahnschrift Light", 11F);
            btnTabMateriales = CrearBotonTab("📦 Materiales", fntTab); btnTabMateriales.Click += (s, e) => CargarTab("materiales");
            btnTabRecortes = CrearBotonTab("✂ Recortes", fntTab); btnTabRecortes.Click += (s, e) => CargarTab("recortes");
            btnTabHerramientas = CrearBotonTab("🔧 Herramientas", fntTab); btnTabHerramientas.Click += (s, e) => CargarTab("herramientas");
            btnTabInsumos = CrearBotonTab("📋 Insumos", fntTab); btnTabInsumos.Click += (s, e) => CargarTab("insumos");

            panelTabs.Controls.Add(btnTabMateriales, 0, 0);
            panelTabs.Controls.Add(btnTabRecortes, 1, 0);
            panelTabs.Controls.Add(btnTabHerramientas, 2, 0);
            panelTabs.Controls.Add(btnTabInsumos, 3, 0);

            // ============ FILTROS ============
            var panelFiltros = new PanelAntiParpadeo();
            panelFiltros.Dock = DockStyle.Top;
            panelFiltros.Height = 50;
            panelFiltros.ColumnCount = 9;
            panelFiltros.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22)); // Búsqueda
            panelFiltros.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));    // Lbl Categoría
            panelFiltros.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18)); // Cmb Categoría
            panelFiltros.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));    // Lbl Estado
            panelFiltros.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15)); // Cmb Estado
            panelFiltros.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));    // Lbl Obra / spacer
            panelFiltros.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18)); // Cmb Obra / spacer
            panelFiltros.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14)); // Btn Paquete
            panelFiltros.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 13)); // Btn Limpiar
            panelFiltros.RowCount = 1;
            panelFiltros.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            panelFiltros.Padding = new Padding(40, 5, 40, 5);

            Font fntFiltro = new Font("Bahnschrift Light", 10F);
            Font fntLabel = new Font("Bahnschrift SemiLight", 9.5F);

            txtBusqueda = new Guna2TextBox
            {
                PlaceholderText = "🔍  Buscar...",
                Font = fntFiltro,
                ForeColor = Color.FromArgb(230, 230, 230),
                FillColor = Color.FromArgb(35, 50, 40),
                BorderColor = colorBorde,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                BorderRadius = 3,
                Size = new Size(200, 36),
                FocusedState = { BorderColor = Color.FromArgb(39, 174, 96) },
                HoverState = { BorderColor = Color.FromArgb(39, 174, 96) }
            };
            txtBusqueda.TextChanged += (s, e) => AplicarFiltros();

            lblCategoria = new Label { Text = "Cat:", Font = fntLabel, ForeColor = colorTexto, AutoSize = true, Anchor = AnchorStyles.Left | AnchorStyles.Right, TextAlign = ContentAlignment.MiddleRight };
            cmbCategoria = CrearComboFiltro(fntFiltro);
            cmbCategoria.SelectedIndexChanged += (s, e) => AplicarFiltros();

            lblEstado = new Label { Text = "Estado:", Font = fntLabel, ForeColor = colorTexto, AutoSize = true, Anchor = AnchorStyles.Left | AnchorStyles.Right, TextAlign = ContentAlignment.MiddleRight };
            cmbEstado = CrearComboFiltro(fntFiltro);
            cmbEstado.SelectedIndexChanged += (s, e) => AplicarFiltros();

            lblObra = new Label { Text = "Obra:", Font = fntLabel, ForeColor = colorTexto, AutoSize = true, Anchor = AnchorStyles.Left | AnchorStyles.Right, TextAlign = ContentAlignment.MiddleRight };
            cmbObra = CrearComboFiltro(fntFiltro);
            cmbObra.SelectedIndexChanged += (s, e) => AplicarFiltros();

            btnLimpiar = new Guna2Button
            {
                Text = "LIMPIAR",
                Font = fntFiltro,
                FillColor = Color.FromArgb(180, 50, 50),
                ForeColor = colorTexto,
                BorderRadius = 2,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Size = new Size(80, 34)
            };
            btnLimpiar.Click += (s, e) => LimpiarFiltros();

            btnPaquete = new Guna2Button
            {
                Text = "Paquete",
                Font = new Font("Bahnschrift Light", 10F),
                FillColor = colorVerdeBoton,
                ForeColor = colorTexto,
                BorderRadius = 2,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Size = new Size(100, 34),
                Visible = false
            };
            btnPaquete.Click += (s, e) =>
            {
                modoPaquete = !modoPaquete;
                btnPaquete.FillColor = modoPaquete ? colorVerdeAccent : colorVerdeBoton;
                AplicarFiltros();
            };

            panelFiltros.Controls.Add(txtBusqueda, 0, 0);
            panelFiltros.Controls.Add(lblCategoria, 1, 0);
            panelFiltros.Controls.Add(cmbCategoria, 2, 0);
            panelFiltros.Controls.Add(lblEstado, 3, 0);
            panelFiltros.Controls.Add(cmbEstado, 4, 0);
            panelFiltros.Controls.Add(lblObra, 5, 0);
            panelFiltros.Controls.Add(cmbObra, 6, 0);
            panelFiltros.Controls.Add(btnPaquete, 7, 0);
            panelFiltros.Controls.Add(btnLimpiar, 8, 0);

            // ============ GRID ============
            dgvInventario = new Guna2DataGridView();
            dgvInventario.Dock = DockStyle.Fill;
            dgvInventario.AllowUserToAddRows = false;
            dgvInventario.AllowUserToDeleteRows = false;
            dgvInventario.AllowUserToResizeRows = false;
            dgvInventario.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvInventario.ReadOnly = true;
            dgvInventario.RowHeadersVisible = false;
            dgvInventario.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvInventario.MultiSelect = false;
            dgvInventario.BorderStyle = BorderStyle.None;
            dgvInventario.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgvInventario.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgvInventario.ColumnHeadersHeight = 45;
            dgvInventario.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgvInventario.RowTemplate.Height = 40;
            dgvInventario.EnableHeadersVisualStyles = false;
            dgvInventario.BackgroundColor = colorPanelOscuro;
            dgvInventario.GridColor = Color.FromArgb(40, 60, 50);

            // Theme
            dgvInventario.ThemeStyle.BackColor = colorPanelOscuro;
            dgvInventario.ThemeStyle.GridColor = Color.FromArgb(40, 60, 50);
            dgvInventario.ThemeStyle.HeaderStyle.BackColor = colorVerdeOscuro;
            dgvInventario.ThemeStyle.HeaderStyle.ForeColor = colorTexto;
            dgvInventario.ThemeStyle.HeaderStyle.Font = new Font("Bahnschrift", 11F, FontStyle.Bold);
            dgvInventario.ThemeStyle.HeaderStyle.Height = 45;
            dgvInventario.ColumnHeadersDefaultCellStyle.SelectionBackColor = colorVerdeOscuro;
            dgvInventario.ColumnHeadersDefaultCellStyle.SelectionForeColor = colorTexto;
            dgvInventario.ThemeStyle.RowsStyle.BackColor = colorFila;
            dgvInventario.ThemeStyle.RowsStyle.ForeColor = colorTexto;
            dgvInventario.ThemeStyle.RowsStyle.Font = new Font("Bahnschrift Light", 10.5F);
            dgvInventario.ThemeStyle.RowsStyle.Height = 40;
            dgvInventario.ThemeStyle.RowsStyle.SelectionBackColor = colorSeleccion;
            dgvInventario.ThemeStyle.RowsStyle.SelectionForeColor = colorTexto;
            dgvInventario.ThemeStyle.AlternatingRowsStyle.BackColor = colorFilaAlt;
            dgvInventario.ThemeStyle.AlternatingRowsStyle.ForeColor = colorTexto;
            dgvInventario.ThemeStyle.AlternatingRowsStyle.SelectionBackColor = colorSeleccion;
            dgvInventario.ThemeStyle.AlternatingRowsStyle.SelectionForeColor = colorTexto;

            // ============ AGREGAR AL FORM (orden inverso por DockStyle) ============
            this.Controls.Add(dgvInventario);
            this.Controls.Add(panelFiltros);
            this.Controls.Add(panelTabs);
            this.Controls.Add(panelResumen);
            this.Controls.Add(panelTitulo);
        }

        // ======================== HELPERS ========================
        private Guna2Button CrearBotonTab(string text, Font font)
        {
            return new Guna2Button
            {
                Text = text,
                Font = font,
                FillColor = colorVerdeOscuro,
                ForeColor = colorTexto,
                BorderRadius = 0,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Size = new Size(180, 36)
            };
        }

        private Guna2ComboBox CrearComboFiltro(Font font)
        {
            var cmb = new Guna2ComboBox();
            cmb.Font = font;
            cmb.ForeColor = Color.FromArgb(230, 230, 230);
            cmb.FillColor = Color.FromArgb(35, 50, 40);
            cmb.BorderColor = colorBorde;
            cmb.DropDownStyle = ComboBoxStyle.DropDownList;
            cmb.ItemHeight = 26;
            cmb.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            cmb.Size = new Size(150, 34);
            cmb.BorderRadius = 3;
            cmb.FocusedColor = Color.FromArgb(39, 174, 96);
            cmb.FocusedState.BorderColor = Color.FromArgb(39, 174, 96);
            return cmb;
        }

        private void ConfigurarComboBuscable(Guna2ComboBox combo)
        {
            combo.DropDownStyle = ComboBoxStyle.DropDown;
            combo.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            combo.AutoCompleteSource = AutoCompleteSource.ListItems;
        }

        // ======================== CARGA ========================
        private void CargarFiltros()
        {
            try
            {
                var categorias = datos.ObtenerCategorias();
                cmbCategoria.Items.Clear();
                foreach (var c in categorias) cmbCategoria.Items.Add(c);
                ConfigurarComboBuscable(cmbCategoria);
                if (cmbCategoria.Items.Count > 0) cmbCategoria.SelectedIndex = 0;

                CargarObrasFiltro();

                ActualizarFiltrosEstado();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error cargando filtros: " + ex.Message);
            }
        }

        private void ActualizarFiltrosEstado()
        {
            cmbEstado.Items.Clear();
            cmbEstado.Visible = true;
            lblEstado.Visible = true;
            switch (tabActual)
            {
                case "materiales":
                    cmbEstado.Items.AddRange(new object[] { "TODOS", "CON STOCK", "STOCK BAJO", "SIN STOCK" });
                    lblEstado.Text = "Stock:";
                    lblObra.Visible = false;
                    cmbObra.Visible = false;
                    lblCategoria.Visible = true;
                    cmbCategoria.Visible = true;
                    if (btnPaquete != null) btnPaquete.Visible = true;
                    break;
                case "recortes":
                    cmbEstado.Visible = false;
                    lblEstado.Visible = false;
                    lblObra.Visible = false;
                    cmbObra.Visible = false;
                    lblCategoria.Visible = true;
                    cmbCategoria.Visible = true;
                    if (btnPaquete != null) { btnPaquete.Visible = false; modoPaquete = false; btnPaquete.FillColor = colorVerdeBoton; }
                    break;
                case "herramientas":
                    cmbEstado.Items.AddRange(new object[] { "TODOS", "EN OBRA", "DISPONIBLE" });
                    cmbEstado.Visible = true;
                    lblEstado.Visible = true;
                    lblEstado.Text = "Estado:";
                    lblObra.Visible = true;
                    cmbObra.Visible = true;
                    lblCategoria.Visible = false;
                    cmbCategoria.Visible = false;
                    if (btnPaquete != null) { btnPaquete.Visible = false; modoPaquete = false; btnPaquete.FillColor = colorVerdeBoton; }
                    break;
                case "insumos":
                    cmbEstado.Items.AddRange(new object[] { "TODOS", "EN OBRA", "CONSUMIDO", "DISPONIBLE" });
                    cmbEstado.Visible = true;
                    lblEstado.Visible = true;
                    lblEstado.Text = "Estado:";
                    lblObra.Visible = true;
                    cmbObra.Visible = true;
                    lblCategoria.Visible = false;
                    cmbCategoria.Visible = false;
                    if (btnPaquete != null) { btnPaquete.Visible = false; modoPaquete = false; btnPaquete.FillColor = colorVerdeBoton; }
                    break;
            }
            if (cmbEstado.Items.Count > 0)
                cmbEstado.SelectedItem = tabActual == "herramientas" ? "EN OBRA" : "TODOS";
        }

        private void CargarObrasFiltro()
        {
            var obras = tabActual == "herramientas"
                ? datos.ObtenerObrasConHerramientas()
                : datos.ObtenerObrasActivas();

            cmbObra.Items.Clear();
            foreach (var obra in obras)
                cmbObra.Items.Add(obra);

            if (cmbObra.Items.Count > 0)
                cmbObra.SelectedIndex = 0;
        }

        private void CargarResumen()
        {
            try
            {
                var resumen = datos.ObtenerResumen();
                lblResumen1.Text = $"📦 Materiales: {resumen.total_materiales} ({resumen.materiales_sin_stock} sin stock)";
                lblResumen2.Text = $"✂ Recortes: {resumen.total_recortes} disponibles";
                lblResumen3.Text = $"🔧 Herramientas: {resumen.total_herramientas} ({resumen.herramientas_en_obra} en obra)";
                lblResumen4.Text = $"📋 Insumos: {resumen.total_insumos} ({resumen.insumos_en_obra} en obra)";
            }
            catch { }
        }

        // ======================== TABS ========================
        private void CargarTab(string tab)
        {
            tabActual = tab;
            txtBusqueda.Text = "";
            if (cmbCategoria.Items.Count > 0) cmbCategoria.SelectedIndex = 0;
            CargarObrasFiltro();
            ActualizarEstiloTabs();
            ActualizarFiltrosEstado();
            AplicarFiltros();
        }

        private void ActualizarEstiloTabs()
        {
            btnTabMateriales.FillColor = tabActual == "materiales" ? colorVerdeAccent : colorVerdeOscuro;
            btnTabRecortes.FillColor = tabActual == "recortes" ? colorVerdeAccent : colorVerdeOscuro;
            btnTabHerramientas.FillColor = tabActual == "herramientas" ? colorVerdeAccent : colorVerdeOscuro;
            btnTabInsumos.FillColor = tabActual == "insumos" ? colorVerdeAccent : colorVerdeOscuro;
        }

        // ======================== FILTROS ========================
        private void AplicarFiltros()
        {
            try
            {
                string busqueda = txtBusqueda?.Text?.Trim() ?? "";
                string categoria = cmbCategoria?.SelectedItem?.ToString() ?? "";
                string estado = cmbEstado?.SelectedItem?.ToString() ?? "";
                string obra = cmbObra?.SelectedItem?.ToString() ?? "";

                switch (tabActual)
                {
                    case "materiales":
                        if (modoPaquete)
                            MostrarPaquetes();
                        else
                            MostrarMateriales(busqueda, categoria, estado);
                        break;
                    case "recortes":
                        MostrarRecortes(busqueda, categoria);
                        break;
                    case "herramientas":
                        MostrarHerramientas(busqueda, estado, obra);
                        break;
                    case "insumos":
                        MostrarInsumos(busqueda, estado, obra);
                        break;
                }
            }
            catch (Exception ex)
            {
                dgvInventario.DataSource = CrearTablaVaciaSegunTab();
                Console.WriteLine("Error al aplicar filtros del inventario: " + ex.Message);
            }
        }

        private void LimpiarFiltros()
        {
            txtBusqueda.Text = "";
            if (cmbCategoria.Items.Count > 0) cmbCategoria.SelectedIndex = 0;
            if (cmbEstado.Items.Count > 0) cmbEstado.SelectedIndex = 0;
            if (cmbObra.Items.Count > 0) cmbObra.SelectedIndex = 0;
            modoPaquete = false;
            if (btnPaquete != null) btnPaquete.FillColor = colorVerdeBoton;
            AplicarFiltros();
        }

        // ======================== GRILLAS ========================
        private void MostrarMateriales(string busqueda, string categoria, string estado)
        {
            var items = datos.ObtenerMateriales(busqueda, categoria, estado);
            bool mostrarPrecios = SessionManager.Role == UserRole.Admin;
            bool ocultarCantidades = SessionManager.DebeOcultarCantidadesStockMateriales;
            DataTable dt = new DataTable();
            dt.Columns.Add("Nombre", typeof(string));
            dt.Columns.Add("Categoría", typeof(string));
            dt.Columns.Add(ocultarCantidades ? "Disponibilidad" : "Stock Actual", ocultarCantidades ? typeof(string) : typeof(decimal));
            if (!ocultarCantidades)
                dt.Columns.Add("Stock Mínimo", typeof(decimal));
            if (mostrarPrecios)
            {
                dt.Columns.Add("P. Unitario", typeof(decimal));
                dt.Columns.Add("Valor Total", typeof(decimal));
            }
            dt.Columns.Add("Estado", typeof(string));
            dt.Columns.Add("Proveedores", typeof(string));

            foreach (var m in items)
            {
                if (mostrarPrecios)
                {
                    if (ocultarCantidades)
                        dt.Rows.Add(m.nombre, m.categoria, SessionManager.ObtenerEstadoStockMaterial(m.stock_actual), m.precio_unitario, m.stock_actual * m.precio_unitario, m.estado_stock, m.proveedores);
                    else
                        dt.Rows.Add(m.nombre, m.categoria, m.stock_actual, m.stock_minimo, m.precio_unitario, m.stock_actual * m.precio_unitario, m.estado_stock, m.proveedores);
                }
                else
                {
                    if (ocultarCantidades)
                        dt.Rows.Add(m.nombre, m.categoria, SessionManager.ObtenerEstadoStockMaterial(m.stock_actual), m.estado_stock, m.proveedores);
                    else
                        dt.Rows.Add(m.nombre, m.categoria, m.stock_actual, m.stock_minimo, m.estado_stock, m.proveedores);
                }
            }

            dgvInventario.DataSource = dt;

            if (dgvInventario.Columns["Nombre"] != null)
            {
                dgvInventario.Columns["Nombre"].FillWeight = mostrarPrecios ? 21 : 25;
                dgvInventario.Columns["Nombre"].DefaultCellStyle.Padding = new Padding(20, 0, 0, 0);
            }
            if (dgvInventario.Columns["Categoría"] != null) dgvInventario.Columns["Categoría"].FillWeight = mostrarPrecios ? 13 : 15;
            if (dgvInventario.Columns["Stock Actual"] != null)
            {
                dgvInventario.Columns["Stock Actual"].FillWeight = mostrarPrecios ? 11 : 13;
                dgvInventario.Columns["Stock Actual"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                dgvInventario.Columns["Stock Actual"].DefaultCellStyle.Format = "N0";
            }
            if (dgvInventario.Columns["Disponibilidad"] != null)
            {
                dgvInventario.Columns["Disponibilidad"].FillWeight = mostrarPrecios ? 11 : 13;
                dgvInventario.Columns["Disponibilidad"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
            if (dgvInventario.Columns["Stock Mínimo"] != null)
            {
                dgvInventario.Columns["Stock Mínimo"].FillWeight = mostrarPrecios ? 11 : 13;
                dgvInventario.Columns["Stock Mínimo"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                dgvInventario.Columns["Stock Mínimo"].DefaultCellStyle.Format = "N0";
            }
            foreach (string colPrecio in new[] { "P. Unitario", "Valor Total" })
            {
                if (dgvInventario.Columns[colPrecio] != null)
                {
                    dgvInventario.Columns[colPrecio].FillWeight = 11;
                    dgvInventario.Columns[colPrecio].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                    dgvInventario.Columns[colPrecio].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
                    dgvInventario.Columns[colPrecio].DefaultCellStyle.Format = "C2";
                }
            }
            if (dgvInventario.Columns["Estado"] != null)
            {
                dgvInventario.Columns["Estado"].FillWeight = mostrarPrecios ? 12 : 14;
                dgvInventario.Columns["Estado"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
            if (dgvInventario.Columns["Proveedores"] != null) dgvInventario.Columns["Proveedores"].FillWeight = mostrarPrecios ? 20 : 20;
        }

        private void MostrarPaquetes()
        {
            // Definición de materiales con lógica de paquetes: codigo → (unidades por paquete, etiqueta corta)
            var definiciones = new System.Collections.Generic.Dictionary<string, (int porPaquete, string etiqueta)>
            {
                { "EPS196", (25, "EPS 20mm") },
                { "EPS198", (17, "EPS 30mm") },
                { "EPS200", (10, "EPS 50mm") },
                { "MON156", (12, "Montante 35") },
                { "MON157", (12, "Montante 70") },
                { "SOL160", (12, "Solera 35") },
                { "SOL161", (12, "Solera 70") },
            };

            var todosMateriales = datos.ObtenerMateriales();
            var filtrados = todosMateriales.Where(m => !string.IsNullOrEmpty(m.codigo) && definiciones.ContainsKey(m.codigo)).ToList();

            DataTable dt = new DataTable();
            dt.Columns.Add("Material", typeof(string));
            dt.Columns.Add("Unidades", typeof(int));
            dt.Columns.Add("Paq. Completos", typeof(int));
            dt.Columns.Add("Sobrante (u)", typeof(int));

            foreach (var m in filtrados)
            {
                var def = definiciones[m.codigo];
                int stock = (int)m.stock_actual;
                int paquetes = stock / def.porPaquete;
                int sobrante = stock % def.porPaquete;
                dt.Rows.Add(def.etiqueta, stock, paquetes, sobrante);
            }

            dgvInventario.DataSource = dt;

            if (dgvInventario.Columns["Material"] != null)
            {
                dgvInventario.Columns["Material"].FillWeight = 55;
                dgvInventario.Columns["Material"].DefaultCellStyle.Padding = new Padding(20, 0, 0, 0);
                dgvInventario.Columns["Material"].HeaderCell.Style.Padding = new Padding(20, 0, 0, 0);
            }
            foreach (string col in new[] { "Unidades", "Paq. Completos", "Sobrante (u)" })
            {
                if (dgvInventario.Columns[col] != null)
                {
                    dgvInventario.Columns[col].FillWeight = 15;
                    dgvInventario.Columns[col].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    dgvInventario.Columns[col].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    dgvInventario.Columns[col].DefaultCellStyle.Format = "N0";
                }
            }
        }

        private void MostrarRecortes(string busqueda, string categoria)
        {
            var items = datos.ObtenerRecortes(busqueda, categoria);
            DataTable dt = new DataTable();
            dt.Columns.Add("Material", typeof(string));
            dt.Columns.Add("Categoría", typeof(string));
            dt.Columns.Add("Dimensiones", typeof(string));
            dt.Columns.Add("Cantidad", typeof(decimal));
            dt.Columns.Add("Fecha", typeof(string));

            foreach (var r in items)
                dt.Rows.Add(r.material, r.categoria, FormatearDimensionesRecorte(r), r.cantidad, r.fecha_creacion == DateTime.MinValue ? "-" : r.fecha_creacion.ToString("dd/MM/yyyy"));

            dgvInventario.DataSource = dt;

            if (dgvInventario.Columns["Material"] != null)
            {
                dgvInventario.Columns["Material"].FillWeight = 30;
                dgvInventario.Columns["Material"].DefaultCellStyle.Padding = new Padding(20, 0, 0, 0);
            }
            if (dgvInventario.Columns["Categoría"] != null) dgvInventario.Columns["Categoría"].FillWeight = 20;
            if (dgvInventario.Columns["Dimensiones"] != null)
            {
                dgvInventario.Columns["Dimensiones"].FillWeight = 18;
                dgvInventario.Columns["Dimensiones"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
            if (dgvInventario.Columns["Cantidad"] != null)
            {
                dgvInventario.Columns["Cantidad"].FillWeight = 15;
                dgvInventario.Columns["Cantidad"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                dgvInventario.Columns["Cantidad"].DefaultCellStyle.Format = "N0";
            }
            if (dgvInventario.Columns["Fecha"] != null)
            {
                dgvInventario.Columns["Fecha"].FillWeight = 17;
                dgvInventario.Columns["Fecha"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
        }

        private DataTable CrearTablaVaciaSegunTab()
        {
            DataTable dt = new DataTable();

            switch (tabActual)
            {
                case "materiales":
                    bool ocultarCantidades = SessionManager.DebeOcultarCantidadesStockMateriales;
                    dt.Columns.Add("Nombre", typeof(string));
                    dt.Columns.Add("Categoría", typeof(string));
                    dt.Columns.Add(ocultarCantidades ? "Disponibilidad" : "Stock Actual", ocultarCantidades ? typeof(string) : typeof(decimal));
                    if (!ocultarCantidades)
                        dt.Columns.Add("Stock Mínimo", typeof(decimal));
                    if (SessionManager.Role == UserRole.Admin)
                    {
                        dt.Columns.Add("P. Unitario", typeof(decimal));
                        dt.Columns.Add("Valor Total", typeof(decimal));
                    }
                    dt.Columns.Add("Estado", typeof(string));
                    dt.Columns.Add("Proveedores", typeof(string));
                    break;
                case "recortes":
                    dt.Columns.Add("Material", typeof(string));
                    dt.Columns.Add("Categoría", typeof(string));
                    dt.Columns.Add("Dimensiones", typeof(string));
                    dt.Columns.Add("Cantidad", typeof(decimal));
                    dt.Columns.Add("Fecha", typeof(string));
                    break;
                case "herramientas":
                    dt.Columns.Add("Herramienta", typeof(string));
                    dt.Columns.Add("Estado", typeof(string));
                    dt.Columns.Add("Obra", typeof(string));
                    dt.Columns.Add("Observaciones", typeof(string));
                    break;
                case "insumos":
                    dt.Columns.Add("Insumo", typeof(string));
                    dt.Columns.Add("Estado", typeof(string));
                    dt.Columns.Add("Obra", typeof(string));
                    dt.Columns.Add("Observaciones", typeof(string));
                    break;
            }

            return dt;
        }

        private static string FormatearDimensionesRecorte(InventarioRecorte recorte)
        {
            if (recorte == null)
                return "-";

            if (recorte.ancho > 0 && recorte.altura > 0)
                return $"{recorte.ancho} x {recorte.altura}";

            if (recorte.medida > 0)
                return recorte.medida.ToString("0.##");

            if (recorte.ancho > 0)
                return recorte.ancho.ToString("0.##");

            if (recorte.altura > 0)
                return recorte.altura.ToString("0.##");

            return "-";
        }

        private void MostrarHerramientas(string busqueda, string estado, string obra)
        {
            var items = datos.ObtenerHerramientas(busqueda, estado, obra);
            DataTable dt = new DataTable();
            dt.Columns.Add("Herramienta", typeof(string));
            dt.Columns.Add("Estado", typeof(string));
            dt.Columns.Add("Obra", typeof(string));
            dt.Columns.Add("Observaciones", typeof(string));

            foreach (var h in items)
                dt.Rows.Add(h.nombre, h.obra != "-" ? "EN OBRA" : "DISPONIBLE", h.obra, h.observaciones);

            dgvInventario.DataSource = dt;

            if (dgvInventario.Columns["Herramienta"] != null)
            {
                dgvInventario.Columns["Herramienta"].FillWeight = 30;
                dgvInventario.Columns["Herramienta"].DefaultCellStyle.Padding = new Padding(20, 0, 0, 0);
            }
            if (dgvInventario.Columns["Estado"] != null)
            {
                dgvInventario.Columns["Estado"].FillWeight = 15;
                dgvInventario.Columns["Estado"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
            if (dgvInventario.Columns["Obra"] != null) dgvInventario.Columns["Obra"].FillWeight = 25;
            if (dgvInventario.Columns["Observaciones"] != null) dgvInventario.Columns["Observaciones"].FillWeight = 30;
        }

        private void MostrarInsumos(string busqueda, string estado, string obra)
        {
            var items = datos.ObtenerInsumos(busqueda, estado, obra);
            DataTable dt = new DataTable();
            dt.Columns.Add("Insumo", typeof(string));
            dt.Columns.Add("Estado", typeof(string));
            dt.Columns.Add("Obra", typeof(string));
            dt.Columns.Add("Observaciones", typeof(string));

            foreach (var i in items)
                dt.Rows.Add(i.nombre, i.estado, i.obra, i.observaciones);

            dgvInventario.DataSource = dt;

            if (dgvInventario.Columns["Insumo"] != null)
            {
                dgvInventario.Columns["Insumo"].FillWeight = 30;
                dgvInventario.Columns["Insumo"].DefaultCellStyle.Padding = new Padding(20, 0, 0, 0);
            }
            if (dgvInventario.Columns["Estado"] != null)
            {
                dgvInventario.Columns["Estado"].FillWeight = 15;
                dgvInventario.Columns["Estado"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
            if (dgvInventario.Columns["Obra"] != null) dgvInventario.Columns["Obra"].FillWeight = 25;
            if (dgvInventario.Columns["Observaciones"] != null) dgvInventario.Columns["Observaciones"].FillWeight = 30;
        }
    }
}
