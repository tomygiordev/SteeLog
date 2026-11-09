using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using Guna.UI2.WinForms;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.Core.Application.Inventario;
using SteeLog.CompositionRoot;
using SteeLog.Presentation;
using SteeLog.Servicios;
using SteeLog.UI;
using SteeLog;

namespace SteeLog.Vistas.Inventario
{
    public partial class Vista_Inventario : Form, IRefrescable
    {
        private string tabActual = "materiales";
        private readonly InventarioPresenter presenter;
        private readonly CancellationTokenSource cierre = new CancellationTokenSource();
        private int versionLista, versionObras, versionResumen;

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
        private TarjetaKpi lblResumen1;
        private TarjetaKpi lblResumen2;
        private TarjetaKpi lblResumen3;
        private TarjetaKpi lblResumen4;

        // Tabs
        private Guna2Button btnTabMateriales;
        private Guna2Button btnTabRecortes;
        private Guna2Button btnTabHerramientas;
        private Guna2Button btnTabInsumos;

        // Grid
        private Guna2DataGridView dgvInventario;

        public Vista_Inventario() : this(LocalServices.Current.Executor) { }

        public Vista_Inventario(IUseCaseExecutor executor)
        {
            presenter = new InventarioPresenter(executor);
            InitializeComponent();
            Tema.SinEscala(this);
            CrearInterfaz();
            Shown += (s, e) => { CargarFiltros(); CargarResumen(); CargarTab("materiales"); };
            FormClosed += (s, e) => cierre.Cancel();
            Disposed += (s, e) => cierre.Cancel();
        }

        public void Refrescar()
        {
            CargarResumen();
            CargarTab(tabActual);
        }

        private void CrearInterfaz()
        {
            BackColor = Tema.Fondo;

            // ============ RESUMEN (4 tarjetas KPI) ============
            var panelResumen = new PanelAntiParpadeo();
            panelResumen.Dock = DockStyle.Top;
            panelResumen.Height = 16 + TarjetaKpi.AltoTarjeta + 4;
            panelResumen.BackColor = Tema.Fondo;
            panelResumen.ColumnCount = 4;
            for (int i = 0; i < 4; i++) panelResumen.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            panelResumen.RowCount = 1;
            panelResumen.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            panelResumen.Padding = new Padding(28, 16, 28, 0);

            lblResumen1 = new TarjetaKpi { Etiqueta = "Materiales", Dock = DockStyle.Fill, Margin = new Padding(0, 0, 8, 0) };
            lblResumen2 = new TarjetaKpi { Etiqueta = "Recortes", Dock = DockStyle.Fill, Margin = new Padding(8, 0, 8, 0) };
            lblResumen3 = new TarjetaKpi { Etiqueta = "Herramientas", Dock = DockStyle.Fill, Margin = new Padding(8, 0, 8, 0) };
            lblResumen4 = new TarjetaKpi { Etiqueta = "Insumos", Dock = DockStyle.Fill, Margin = new Padding(8, 0, 0, 0) };

            panelResumen.Controls.Add(lblResumen1, 0, 0);
            panelResumen.Controls.Add(lblResumen2, 1, 0);
            panelResumen.Controls.Add(lblResumen3, 2, 0);
            panelResumen.Controls.Add(lblResumen4, 3, 0);

            // ============ TABS ============
            var panelTabs = new FlowLayoutPanel();
            panelTabs.Dock = DockStyle.Top;
            panelTabs.Height = 56;
            panelTabs.BackColor = Tema.Fondo;
            panelTabs.Padding = new Padding(28, 14, 28, 0);
            panelTabs.WrapContents = false;

            btnTabMateriales = UiVista.BotonTab("Materiales"); btnTabMateriales.Click += (s, e) => CargarTab("materiales");
            btnTabRecortes = UiVista.BotonTab("Recortes"); btnTabRecortes.Click += (s, e) => CargarTab("recortes");
            btnTabHerramientas = UiVista.BotonTab("Herramientas"); btnTabHerramientas.Click += (s, e) => CargarTab("herramientas");
            btnTabInsumos = UiVista.BotonTab("Insumos"); btnTabInsumos.Click += (s, e) => CargarTab("insumos");

            panelTabs.Controls.Add(btnTabMateriales);
            panelTabs.Controls.Add(btnTabRecortes);
            panelTabs.Controls.Add(btnTabHerramientas);
            panelTabs.Controls.Add(btnTabInsumos);

            // ============ FILTROS ============
            var panelFiltros = new TableLayoutPanel();
            panelFiltros.Dock = DockStyle.Top;
            panelFiltros.Height = 56;
            panelFiltros.BackColor = Tema.Fondo;
            panelFiltros.ColumnCount = 2;
            panelFiltros.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            panelFiltros.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            panelFiltros.RowCount = 1;
            panelFiltros.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            panelFiltros.Padding = new Padding(28, 10, 28, 10);

            txtBusqueda = new Guna2TextBox
            {
                PlaceholderText = "Buscar...",
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 8, 0),
                Height = Tema.AltoControl
            };
            Tema.Input(txtBusqueda);
            txtBusqueda.TextChanged += (s, e) => AplicarFiltros();

            var flujo = new FlowLayoutPanel
            {
                AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false,
                Dock = DockStyle.Fill, Margin = new Padding(0), BackColor = Tema.Fondo
            };

            lblCategoria = UiVista.Etiqueta("Categoría:");
            cmbCategoria = CrearComboFiltro(190);
            cmbCategoria.SelectedIndexChanged += (s, e) => AplicarFiltros();

            lblEstado = UiVista.Etiqueta("Estado:");
            cmbEstado = CrearComboFiltro(160);
            cmbEstado.SelectedIndexChanged += (s, e) => AplicarFiltros();

            lblObra = UiVista.Etiqueta("Obra:");
            cmbObra = CrearComboFiltro(200);
            cmbObra.SelectedIndexChanged += (s, e) => AplicarFiltros();

            btnPaquete = new Guna2Button
            {
                Text = "Paquete",
                Size = new Size(100, Tema.AltoControl),
                Margin = new Padding(14, 0, 0, 0),
                Visible = false
            };
            Tema.Boton(btnPaquete, Variante.Secundario);
            btnPaquete.Click += (s, e) =>
            {
                modoPaquete = !modoPaquete;
                Tema.Boton(btnPaquete, modoPaquete ? Variante.Primario : Variante.Secundario);
                AplicarFiltros();
            };

            btnLimpiar = new Guna2Button
            {
                Text = "Limpiar",
                Size = new Size(90, Tema.AltoControl),
                Margin = new Padding(8, 0, 0, 0)
            };
            Tema.Boton(btnLimpiar, Variante.Fantasma);
            btnLimpiar.Click += (s, e) => LimpiarFiltros();

            flujo.Controls.Add(lblCategoria);
            flujo.Controls.Add(cmbCategoria);
            flujo.Controls.Add(lblEstado);
            flujo.Controls.Add(cmbEstado);
            flujo.Controls.Add(lblObra);
            flujo.Controls.Add(cmbObra);
            flujo.Controls.Add(btnPaquete);
            flujo.Controls.Add(btnLimpiar);

            panelFiltros.Controls.Add(txtBusqueda, 0, 0);
            panelFiltros.Controls.Add(flujo, 1, 0);

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
            Tema.Grilla(dgvInventario);
            dgvInventario.CellPainting += DgvInventario_CellPainting;

            var panelGrilla = new Panel { Dock = DockStyle.Fill, BackColor = Tema.Fondo, Padding = new Padding(28, 4, 28, 20) };
            panelGrilla.Controls.Add(dgvInventario);

            // ============ AGREGAR AL FORM (orden inverso por DockStyle) ============
            this.Controls.Add(panelGrilla);
            this.Controls.Add(panelFiltros);
            this.Controls.Add(panelTabs);
            this.Controls.Add(panelResumen);
            var banda = Tema.TituloVista(this, "Inventario general");
            if (banda != null) banda.SendToBack();
        }

        private void DgvInventario_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            try
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0 || e.ColumnIndex >= dgvInventario.Columns.Count) return;
                if (dgvInventario.Columns[e.ColumnIndex].Name != "Estado") return;
                string texto = Convert.ToString(e.Value);
                if (string.IsNullOrWhiteSpace(texto)) return;
                UiVista.PintarPildora(e, UiVista.TextoEstado(texto), UiVista.ColorEstado(texto), false);
            }
            catch (Exception) { }
        }

        // ======================== HELPERS ========================
        private Guna2ComboBox CrearComboFiltro(int ancho)
        {
            var cmb = new Guna2ComboBox();
            cmb.DropDownStyle = ComboBoxStyle.DropDownList;
            cmb.Size = new Size(ancho, Tema.AltoControl);
            cmb.Margin = new Padding(0, 7, 0, 0);
            Tema.Combo(cmb);
            cmb.ItemHeight = 30; cmb.Height = Tema.AltoControl;
            return cmb;
        }

        // Los combos muestran "Todas"/"Todos" y sentence case; la consulta usa los valores de dominio.
        private static string ValorFiltro(object seleccion, bool mayusculas)
        {
            string s = seleccion == null ? "" : seleccion.ToString();
            if (string.Equals(s, "Todas", StringComparison.OrdinalIgnoreCase)) return "TODAS";
            if (string.Equals(s, "Todos", StringComparison.OrdinalIgnoreCase)) return "TODOS";
            return mayusculas ? s.ToUpperInvariant() : s;
        }

        private static string MostrarFiltro(string valor)
        {
            if (valor == "TODAS") return "Todas";
            if (valor == "TODOS") return "Todos";
            return valor;
        }

        private void ConfigurarComboBuscable(Guna2ComboBox combo)
        {
            combo.DropDownStyle = ComboBoxStyle.DropDown;
            // La fuente debe fijarse antes del modo: Guna2ComboBox conserva DropDownList y WinForms
            // solo admite autocompletar en ese estilo con AutoCompleteSource.ListItems.
            combo.AutoCompleteSource = AutoCompleteSource.ListItems;
            combo.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
        }

        // ======================== CARGA ========================
        private async void CargarFiltros()
        {
            try
            {
                var categorias = await presenter.CategoriasAsync(cierre.Token);
                if (cierre.IsCancellationRequested || IsDisposed) return;
                cmbCategoria.Items.Clear();
                foreach (var c in categorias) cmbCategoria.Items.Add(MostrarFiltro(c));
                ConfigurarComboBuscable(cmbCategoria);
                if (cmbCategoria.Items.Count > 0) cmbCategoria.SelectedIndex = 0;

                CargarObrasFiltro();

                ActualizarFiltrosEstado();
            }
            catch (OperationCanceledException) { }
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
                    cmbEstado.Items.AddRange(new object[] { "Todos", "Con stock", "Stock bajo", "Sin stock" });
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
                    if (btnPaquete != null) { btnPaquete.Visible = false; modoPaquete = false; Tema.Boton(btnPaquete, Variante.Secundario); }
                    break;
                case "herramientas":
                    cmbEstado.Items.AddRange(new object[] { "Todos", "En obra", "Disponible" });
                    cmbEstado.Visible = true;
                    lblEstado.Visible = true;
                    lblEstado.Text = "Estado:";
                    lblObra.Visible = true;
                    cmbObra.Visible = true;
                    lblCategoria.Visible = false;
                    cmbCategoria.Visible = false;
                    if (btnPaquete != null) { btnPaquete.Visible = false; modoPaquete = false; Tema.Boton(btnPaquete, Variante.Secundario); }
                    break;
                case "insumos":
                    cmbEstado.Items.AddRange(new object[] { "Todos", "En obra", "Consumido", "Disponible" });
                    cmbEstado.Visible = true;
                    lblEstado.Visible = true;
                    lblEstado.Text = "Estado:";
                    lblObra.Visible = true;
                    cmbObra.Visible = true;
                    lblCategoria.Visible = false;
                    cmbCategoria.Visible = false;
                    if (btnPaquete != null) { btnPaquete.Visible = false; modoPaquete = false; Tema.Boton(btnPaquete, Variante.Secundario); }
                    break;
            }
            if (cmbEstado.Items.Count > 0)
                cmbEstado.SelectedItem = tabActual == "herramientas" ? "En obra" : "Todos";
        }

        private async void CargarObrasFiltro()
        {
            int version = ++versionObras;
            bool herramientas = tabActual == "herramientas";
            try
            {
            var obras = await presenter.ObrasAsync(herramientas, cierre.Token);
            if (cierre.IsCancellationRequested || IsDisposed || version != versionObras) return;

            cmbObra.Items.Clear();
            foreach (var obra in obras)
                cmbObra.Items.Add(MostrarFiltro(obra));

            if (cmbObra.Items.Count > 0)
                cmbObra.SelectedIndex = 0;
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { if (!cierre.IsCancellationRequested && version == versionObras) ToastNotification.ShowError("Error cargando obras: " + ex.Message); }
        }

        private async void CargarResumen()
        {
            int version = ++versionResumen;
            try
            {
                var resumen = await presenter.ResumenAsync(cierre.Token);
                if (cierre.IsCancellationRequested || IsDisposed || version != versionResumen) return;
                lblResumen1.Valor = resumen.total_materiales.ToString("N0");
                lblResumen1.Detalle = resumen.materiales_sin_stock + " sin stock";
                lblResumen1.ColorDetalle = resumen.materiales_sin_stock > 0 ? Tema.Advertencia : Tema.TextoApagado;
                lblResumen2.Valor = resumen.total_recortes.ToString("N0");
                lblResumen2.Detalle = "disponibles";
                lblResumen3.Valor = resumen.total_herramientas.ToString("N0");
                lblResumen3.Detalle = resumen.herramientas_en_obra + " en obra";
                lblResumen4.Valor = resumen.total_insumos.ToString("N0");
                lblResumen4.Detalle = resumen.insumos_en_obra + " en obra";
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { if (!cierre.IsCancellationRequested && version == versionResumen) ToastNotification.ShowError("Error cargando resumen: " + ex.Message); }
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
            Tema.Boton(btnTabMateriales, tabActual == "materiales" ? Variante.TabActiva : Variante.Tab);
            Tema.Boton(btnTabRecortes, tabActual == "recortes" ? Variante.TabActiva : Variante.Tab);
            Tema.Boton(btnTabHerramientas, tabActual == "herramientas" ? Variante.TabActiva : Variante.Tab);
            Tema.Boton(btnTabInsumos, tabActual == "insumos" ? Variante.TabActiva : Variante.Tab);
        }

        // ======================== FILTROS ========================
        private async void AplicarFiltros()
        {
            int version = ++versionLista;
            try
            {
                string busqueda = txtBusqueda?.Text?.Trim() ?? "";
                string categoria = ValorFiltro(cmbCategoria?.SelectedItem, false);
                string estado = ValorFiltro(cmbEstado?.SelectedItem, true);
                string obra = ValorFiltro(cmbObra?.SelectedItem, false);

                switch (tabActual)
                {
                    case "materiales":
                        if (modoPaquete)
                            await MostrarPaquetes(version);
                        else
                            await MostrarMateriales(busqueda, categoria, estado, version);
                        break;
                    case "recortes":
                        await MostrarRecortes(busqueda, categoria, version);
                        break;
                    case "herramientas":
                        await MostrarHerramientas(busqueda, estado, obra, version);
                        break;
                    case "insumos":
                        await MostrarInsumos(busqueda, estado, obra, version);
                        break;
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                if (cierre.IsCancellationRequested || IsDisposed || version != versionLista) return;
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
            if (btnPaquete != null) Tema.Boton(btnPaquete, Variante.Secundario);
            AplicarFiltros();
        }

        // ======================== GRILLAS ========================
        private async Task MostrarMateriales(string busqueda, string categoria, string estado, int version)
        {
            var items = await presenter.MaterialesAsync(busqueda, categoria, estado, cierre.Token);
            if (cierre.IsCancellationRequested || IsDisposed || version != versionLista) return;
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

        private async Task MostrarPaquetes(int version)
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

            var todosMateriales = await presenter.MaterialesAsync("", "", "", cierre.Token);
            if (cierre.IsCancellationRequested || IsDisposed || version != versionLista) return;
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

        private async Task MostrarRecortes(string busqueda, string categoria, int version)
        {
            var items = await presenter.RecortesAsync(busqueda, categoria, cierre.Token);
            if (cierre.IsCancellationRequested || IsDisposed || version != versionLista) return;
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

        private async Task MostrarHerramientas(string busqueda, string estado, string obra, int version)
        {
            var items = await presenter.HerramientasAsync(busqueda, estado, obra, cierre.Token);
            if (cierre.IsCancellationRequested || IsDisposed || version != versionLista) return;
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
            }
            if (dgvInventario.Columns["Estado"] != null)
            {
                dgvInventario.Columns["Estado"].FillWeight = 15;
                dgvInventario.Columns["Estado"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
            if (dgvInventario.Columns["Obra"] != null) dgvInventario.Columns["Obra"].FillWeight = 25;
            if (dgvInventario.Columns["Observaciones"] != null) dgvInventario.Columns["Observaciones"].FillWeight = 30;
        }

        private async Task MostrarInsumos(string busqueda, string estado, string obra, int version)
        {
            var items = await presenter.InsumosAsync(busqueda, estado, obra, cierre.Token);
            if (cierre.IsCancellationRequested || IsDisposed || version != versionLista) return;
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
