using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using SteeLog.Presentation;
using SteeLog.Modelos;
using SteeLog.Servicios;
using SteeLog.CompositionRoot;
using SteeLog.Core.Application.Inventario;
using SteeLog.Core.Application.Reportes;
using System.Threading;
using System.IO;
using OfficeOpenXml;
using SteeLog.UI;
using System.Windows.Forms.DataVisualization.Charting;

namespace SteeLog.Vistas
{
    public partial class Vista_ReporteStock : Form
    {
        private FiltroMaterial filtroAbierto = null;

        private Guna.UI2.WinForms.Guna2Button btnTogglePaneles;
        private Guna.UI2.WinForms.Guna2Button btnConfigurarSecciones;
        private bool panelesVisibles = false;
        private int panelDashboardHeight;
        private ReporteStockPresenter presenter;
        private Func<object> identidadActual = () => LocalServices.Current.Session.Current;
        private Func<bool> esAdmin = () => SessionManager.Role == UserRole.Admin;
        private Func<string> elegirPdf = ElegirDestinoPdf;
        private Action<string> abrirPdf = ruta => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(ruta) { UseShellExecute = true });
        private Action<string> avisarError = mensaje => ToastNotification.ShowError(mensaje);
        private Action<string> avisarExito = mensaje => ToastNotification.ShowSuccess(mensaje);
        private readonly CancellationTokenSource vida = new CancellationTokenSource();
        private CancellationTokenSource carga;
        private int version;
        private bool exportando;
        private object identidadDatos;
        private DateTime? fechaDatos;
        private FiltroMaterialParams filtroActual;
        private void CancelarOperaciones()
        {
            if (!vida.IsCancellationRequested)
            {
                try { vida.Cancel(); } catch (ObjectDisposedException) { }
            }
            try { carga?.Cancel(); } catch (ObjectDisposedException) { }
        }

        public Vista_ReporteStock()
        {
            InitializeComponent();
            Tema.SinEscala(this);
            AplicarEstiloUI();
            this.Load += Vista_ReporteStock_Load;
            dgvMateriales.CellPainting += dgvMateriales_CellPainting;
            dgvMateriales.SelectionChanged += dgvMateriales_SelectionChanged;
            FormClosed += (s, e) => CancelarOperaciones();
            Disposed += (s, e) => CancelarOperaciones();
        }

        internal Vista_ReporteStock(ReporteStockPresenter presenter, Func<object> identidadActual, Func<bool> esAdmin, Func<string> elegirPdf, Action<string> abrirPdf, Action<string> avisarError, Action<string> avisarExito) : this()
        {
            this.presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
            this.identidadActual = identidadActual ?? throw new ArgumentNullException(nameof(identidadActual));
            this.esAdmin = esAdmin ?? throw new ArgumentNullException(nameof(esAdmin));
            this.elegirPdf = elegirPdf ?? throw new ArgumentNullException(nameof(elegirPdf));
            this.abrirPdf = abrirPdf ?? throw new ArgumentNullException(nameof(abrirPdf));
            this.avisarError = avisarError ?? throw new ArgumentNullException(nameof(avisarError));
            this.avisarExito = avisarExito ?? throw new ArgumentNullException(nameof(avisarExito));
        }

        private void Vista_ReporteStock_Load(object sender, EventArgs e)
        {
            if (presenter == null) presenter = new ReporteStockPresenter(LocalServices.Current.Executor, LocalServices.Current.ReporteStockPdf);
            CrearBotonTogglePaneles();
            CargarMateriales();
            panelDashboardHeight = panelDashboard.Height;
            panelDashboard.Visible = false;
        }

        private static readonly Color ColorSeccion = Tema.Mezclar(Tema.Fondo, Tema.Acento, 0.22);

        private void AplicarEstiloUI()
        {
            BackColor = Tema.Fondo;
            SuspendLayout();
            panelAntiParpadeo1.Visible = false;

            // Subtítulo de sección
            panelAntiParpadeo6.BackColor = Tema.Fondo;
            panelAntiParpadeo6.Height = 58;
            panelAntiParpadeo6.Padding = new Padding(28, 12, 28, 6);
            label8.Text = "LISTADO DE MATERIALES";
            label8.Font = Tema.FuenteSubtitulo;
            label8.ForeColor = Tema.TextoSecundario;
            label8.AutoSize = true;
            label8.Anchor = AnchorStyles.Left;

            // Resumen (se muestra/oculta con el botón)
            panelDashboard.BackColor = Tema.Fondo;
            panelDashboard.Height = 116;
            panelDashboard.Padding = new Padding(28, 0, 28, 8);
            foreach (var panel in new[] { panelTotal, panelBajo })
            {
                panel.FillColor = Tema.Superficie;
                panel.BorderColor = Tema.Borde;
                panel.BorderRadius = 6;
                panel.BorderThickness = 1;
                panel.Margin = new Padding(0, 0, 12, 0);
            }
            lblTotalTit.Font = Tema.Fuente(10f); lblTotalTit.ForeColor = Tema.TextoSecundario; lblTotalTit.Text = "Total de materiales";
            lblBajoTit.Font = Tema.Fuente(10f); lblBajoTit.ForeColor = Tema.Advertencia; lblBajoTit.Text = "En stock bajo";
            lblTotalVal.Font = Tema.Fuente(20f, true); lblTotalVal.ForeColor = Tema.Texto;
            lblBajoVal.Font = Tema.Fuente(20f, true); lblBajoVal.ForeColor = Tema.Texto;
            lblTotalTit.Location = new Point(18, 14); lblBajoTit.Location = new Point(18, 14);
            lblTotalVal.Location = new Point(16, 40); lblBajoVal.Location = new Point(16, 40);
            chartCategorias.Legends[0].ForeColor = Tema.Texto;
            chartCategorias.Legends[0].Font = Tema.Fuente(9f);

            // Barra de filtros + grilla
            dgvMateriales.RowTemplate.DefaultCellStyle.Font = Tema.FuenteTexto;
            Tema.Grilla(dgvMateriales);
            materiales.AutoSize = false;
            materiales.CellBorderStyle = TableLayoutPanelCellBorderStyle.None;
            materiales.Dock = DockStyle.Fill;
            materiales.BackColor = Tema.Fondo;
            materiales.ForeColor = Tema.Texto;
            materiales.Padding = new Padding(28, 4, 28, 20);
            materiales.RowStyles.Clear();
            materiales.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
            materiales.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            label9.Text = "Buscar:";
            label9.Font = Tema.FuenteTexto;
            label9.ForeColor = Tema.TextoSecundario;
            label9.Anchor = AnchorStyles.Left;
            label9.Margin = new Padding(0, 0, 12, 0);

            txtFiltroMaterial.Dock = DockStyle.None;
            txtFiltroMaterial.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            txtFiltroMaterial.Height = Tema.AltoControl;
            txtFiltroMaterial.Margin = new Padding(0, 0, 8, 0);
            txtFiltroMaterial.PlaceholderText = "Filtrar por nombre...";
            Tema.Input(txtFiltroMaterial);

            filtrarMaterial.Text = "Filtros";
            filtrarMaterial.ImageAlign = HorizontalAlignment.Left;
            filtrarMaterial.ImageSize = new Size(18, 18);
            filtrarMaterial.TextAlign = HorizontalAlignment.Center;
            filtrarMaterial.Size = new Size(104, Tema.AltoControl);
            filtrarMaterial.Anchor = AnchorStyles.Left;
            filtrarMaterial.Margin = new Padding(0, 0, 8, 0);
            Tema.Boton(filtrarMaterial, Variante.Secundario);

            btnExportarPDF.Size = new Size(84, Tema.AltoControl);
            btnExportarPDF.Anchor = AnchorStyles.Left;
            btnExportarPDF.Margin = new Padding(0, 0, 8, 0);
            Tema.Boton(btnExportarPDF, Variante.Secundario);
            btnExportarExcel.Size = new Size(84, Tema.AltoControl);
            btnExportarExcel.Anchor = AnchorStyles.Left;
            btnExportarExcel.Margin = new Padding(0, 0, 8, 0);
            Tema.Boton(btnExportarExcel, Variante.Primario);

            lblSoloStockBajo.Text = "Solo stock bajo:";
            lblSoloStockBajo.Font = Tema.FuenteTexto;
            lblSoloStockBajo.ForeColor = Tema.TextoSecundario;
            lblSoloStockBajo.Margin = new Padding(16, 0, 8, 0);
            toggleStockBajo.Size = new Size(46, 24);
            toggleStockBajo.Margin = new Padding(0);
            Tema.Toggle(toggleStockBajo);

            materiales.BringToFront();
            var banda = Tema.TituloVista(this, "Reporte de stock");
            if (banda != null) banda.SendToBack();
            ResumeLayout(true);
        }

        private void CrearBotonTogglePaneles()
        {
            btnTogglePaneles = new Guna.UI2.WinForms.Guna2Button();
            btnTogglePaneles.Text = "Mostrar resumen";
            btnTogglePaneles.Size = new Size(150, Tema.AltoControl);
            btnTogglePaneles.Anchor = AnchorStyles.Right;
            btnTogglePaneles.Margin = new Padding(8, 0, 0, 0);
            Tema.Boton(btnTogglePaneles, Variante.Fantasma);
            btnTogglePaneles.Click += BtnTogglePaneles_Click;
            panelAntiParpadeo6.ColumnCount = 2;
            panelAntiParpadeo6.ColumnStyles.Clear();
            panelAntiParpadeo6.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            panelAntiParpadeo6.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            panelAntiParpadeo6.Controls.Add(btnTogglePaneles, 1, 0);

            if (esAdmin())
            {
                btnConfigurarSecciones = new Guna.UI2.WinForms.Guna2Button
                {
                    Text = "Configurar secciones",
                    Size = new Size(170, Tema.AltoControl),
                    Anchor = AnchorStyles.Right,
                    Margin = new Padding(8, 0, 0, 0)
                };
                Tema.Boton(btnConfigurarSecciones, Variante.Secundario);
                btnConfigurarSecciones.Click += BtnConfigurarSecciones_Click;
                panelAntiParpadeo6.ColumnCount = 3;
                panelAntiParpadeo6.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                panelAntiParpadeo6.Controls.Add(btnConfigurarSecciones, 2, 0);
            }
        }

        private void BtnConfigurarSecciones_Click(object sender, EventArgs e)
        {
            using (var modal = new Reportes.ModalConfigReporteStock(presenter))
                modal.ShowDialog(this);
            CargarMateriales(txtFiltroMaterial.Text.Trim(), toggleStockBajo.Checked);
        }

        private void BtnTogglePaneles_Click(object sender, EventArgs e)
        {
            panelesVisibles = !panelesVisibles;
            panelDashboard.Visible = panelesVisibles;

            if (panelesVisibles)
            {
                btnTogglePaneles.Text = "Ocultar resumen";
            }
            else
            {
                btnTogglePaneles.Text = "Mostrar resumen";
            }
        }

        private async void CargarMateriales(string nombre = null, bool soloStockBajo = false, FiltroMaterialParams filtro = null)
        {
            if (presenter == null || vida.IsCancellationRequested) return;
            if (filtro != null) filtroActual = filtro;
            carga?.Cancel();
            var anterior = carga;
            carga = CancellationTokenSource.CreateLinkedTokenSource(vida.Token);
            anterior?.Dispose();
            var token = carga.Token;
            int solicitud = ++version;
            var identidad = identidadActual();
            dgvMateriales.DataSource = null;
            identidadDatos = null; fechaDatos = null;
            var f = filtroActual;
            var consulta = new FiltroMaterialLectura { Nombre = nombre ?? f?.Nombre, SoloStockBajo = soloStockBajo || (f?.SoloStockBajo ?? false), Desde = f?.DesdeCantidad == 0 ? null : f?.DesdeCantidad, Hasta = f?.HastaCantidad == 0 ? null : f?.HastaCantidad, Categorias = f?.IdsCategoria, Proveedores = f?.IdsProveedor };
            try
            {
                var items = await presenter.ListarAsync(consulta, token);
                if (token.IsCancellationRequested || IsDisposed || solicitud != version || !ReferenceEquals(identidad, identidadActual())) return;
                var lista = items.Select(i => new Material { id = i.Material.Id, nombre = i.Material.Nombre, id_categoria = i.Material.CategoriaId, nombre_categoria = i.Material.Categoria, precioUnitario = i.Material.Precio, stockActual = i.Material.Stock, stockMinimo = i.Material.Minimo, cantidadUsada = i.Material.CantidadUsada, cantidadMovimientos = i.Material.CantidadMovimientos, reporteSeccion = i.Seccion, reporteOrdenSeccion = i.OrdenSeccion, reporteOrdenMaterial = i.OrdenMaterial }).ToList();
                AplicarCabecerasDeSeccion(lista);
                dgvMateriales.DataSource = new SortableBindingList<Material>(ConstruirFilasConSeparadores(lista));
                ConfigurarGrid(); AplicarAlturaFilasEspeciales(); CalcularTotales(); ActualizarGrafico();
                identidadDatos = identidad; fechaDatos = DateTime.Now;
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { if (!token.IsCancellationRequested && !vida.IsCancellationRequested && !IsDisposed && solicitud == version && ReferenceEquals(identidad, identidadActual())) avisarError("No se pudo cargar los materiales: " + ex.Message); }
        }

        private List<Material> ConstruirFilasConSeparadores(List<Material> materiales)
        {
            var filas = new List<Material>();
            string seccionAnterior = null;

            foreach (var material in materiales)
            {
                if (!string.IsNullOrWhiteSpace(material.reporteSeccion)
                    && !string.Equals(material.reporteSeccion, seccionAnterior, StringComparison.Ordinal))
                {
                    filas.Add(new Material
                    {
                        esFilaSeparadora = true,
                        tituloSeparador = material.reporteSeccion,
                        reporteSeccion = material.reporteSeccion,
                        reporteOrdenSeccion = material.reporteOrdenSeccion,
                        reporteOrdenMaterial = material.reporteOrdenMaterial
                    });
                }

                filas.Add(material);
                seccionAnterior = material.reporteSeccion;
            }

            return filas;
        }

        private void AplicarAlturaFilasEspeciales()
        {
            foreach (DataGridViewRow row in dgvMateriales.Rows)
            {
                if (row.DataBoundItem is Material item)
                    row.Height = item.esFilaSeparadora ? 34 : Tema.AltoFila;
            }
        }

        private void AplicarCabecerasDeSeccion(List<Material> materiales)
        {
            string seccionAnterior = null;
            foreach (var material in materiales)
            {
                bool esNuevaSeccion = !string.Equals(material.reporteSeccion, seccionAnterior, StringComparison.Ordinal);
                material.reporteSeccionCabecera = esNuevaSeccion ? material.reporteSeccion : string.Empty;
                seccionAnterior = material.reporteSeccion;
            }
        }

        private void ConfigurarGrid()
        {
            bool mostrarPrecios = esAdmin();
            bool ocultarCantidades = !esAdmin();

            dgvMateriales.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvMateriales.MultiSelect = false;
            dgvMateriales.ReadOnly = true;
            dgvMateriales.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvMateriales.AllowUserToResizeColumns = false;
            dgvMateriales.AllowUserToResizeRows = false;
            Tema.Grilla(dgvMateriales);

            // Columns Configuration
            if (dgvMateriales.Columns["id"] != null) dgvMateriales.Columns["id"].Visible = false;
            if (dgvMateriales.Columns["id_categoria"] != null) dgvMateriales.Columns["id_categoria"].Visible = false;
            if (dgvMateriales.Columns["unidad"] != null) dgvMateriales.Columns["unidad"].Visible = false;
            if (dgvMateriales.Columns["id_proveedor"] != null) dgvMateriales.Columns["id_proveedor"].Visible = false;
            if (dgvMateriales.Columns["nombre_proveedor"] != null) dgvMateriales.Columns["nombre_proveedor"].Visible = false;
            if (dgvMateriales.Columns["cantidadUsada"] != null) dgvMateriales.Columns["cantidadUsada"].Visible = false;
            if (dgvMateriales.Columns["cantidadMovimientos"] != null) dgvMateriales.Columns["cantidadMovimientos"].Visible = false;
            if (dgvMateriales.Columns["reporteSeccion"] != null) dgvMateriales.Columns["reporteSeccion"].Visible = false;
            if (dgvMateriales.Columns["reporteOrdenSeccion"] != null) dgvMateriales.Columns["reporteOrdenSeccion"].Visible = false;
            if (dgvMateriales.Columns["reporteOrdenMaterial"] != null) dgvMateriales.Columns["reporteOrdenMaterial"].Visible = false;
            if (dgvMateriales.Columns["esFilaSeparadora"] != null) dgvMateriales.Columns["esFilaSeparadora"].Visible = false;
            if (dgvMateriales.Columns["tituloSeparador"] != null) dgvMateriales.Columns["tituloSeparador"].Visible = false;

            if (dgvMateriales.Columns["reporteSeccionCabecera"] != null)
            {
                dgvMateriales.Columns["reporteSeccionCabecera"].Visible = false;
            }

            if (dgvMateriales.Columns["nombre"] != null)
            {
                dgvMateriales.Columns["nombre"].HeaderText = "Nombre del Material";
                dgvMateriales.Columns["nombre"].FillWeight = 58;
                dgvMateriales.Columns["nombre"].MinimumWidth = 320;
                dgvMateriales.Columns["nombre"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                dgvMateriales.Columns["nombre"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                dgvMateriales.Columns["nombre"].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft;
                dgvMateriales.Columns["nombre"].DisplayIndex = 0;
            }

            if (dgvMateriales.Columns["nombre_categoria"] != null)
            {
                dgvMateriales.Columns["nombre_categoria"].Visible = true;
                dgvMateriales.Columns["nombre_categoria"].HeaderText = "Categoría";
                dgvMateriales.Columns["nombre_categoria"].DataPropertyName = "nombre_categoria";
                dgvMateriales.Columns["nombre_categoria"].FillWeight = 22;
                dgvMateriales.Columns["nombre_categoria"].MinimumWidth = 150;
                dgvMateriales.Columns["nombre_categoria"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                dgvMateriales.Columns["nombre_categoria"].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft;
                dgvMateriales.Columns["nombre_categoria"].DisplayIndex = 1;
            }

            if (dgvMateriales.Columns["precioUnitario"] != null)
            {
                dgvMateriales.Columns["precioUnitario"].Visible = mostrarPrecios;
                dgvMateriales.Columns["precioUnitario"].HeaderText = "Precio Unitario";
                dgvMateriales.Columns["precioUnitario"].DefaultCellStyle.Format = "C2";
                dgvMateriales.Columns["precioUnitario"].FillWeight = 12;
                dgvMateriales.Columns["precioUnitario"].MinimumWidth = 150;
                dgvMateriales.Columns["precioUnitario"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                dgvMateriales.Columns["precioUnitario"].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
                if (mostrarPrecios) dgvMateriales.Columns["precioUnitario"].DisplayIndex = 4;
            }

            if (dgvMateriales.Columns["valorTotal"] != null)
            {
                dgvMateriales.Columns["valorTotal"].Visible = mostrarPrecios;
                dgvMateriales.Columns["valorTotal"].HeaderText = "Valor Total";
                dgvMateriales.Columns["valorTotal"].DefaultCellStyle.Format = "C2";
                dgvMateriales.Columns["valorTotal"].FillWeight = 12;
                dgvMateriales.Columns["valorTotal"].MinimumWidth = 150;
                dgvMateriales.Columns["valorTotal"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                dgvMateriales.Columns["valorTotal"].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
                if (mostrarPrecios) dgvMateriales.Columns["valorTotal"].DisplayIndex = 5;
            }

            if (dgvMateriales.Columns["stockActual"] != null)
            {
                dgvMateriales.Columns["stockActual"].HeaderText = ocultarCantidades ? "Disponibilidad" : "Stock Actual";
                dgvMateriales.Columns["stockActual"].DefaultCellStyle.Format = ocultarCantidades ? string.Empty : "N0";
                dgvMateriales.Columns["stockActual"].FillWeight = mostrarPrecios ? 10 : 12;
                dgvMateriales.Columns["stockActual"].MinimumWidth = 110;
                dgvMateriales.Columns["stockActual"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                dgvMateriales.Columns["stockActual"].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                dgvMateriales.Columns["stockActual"].DefaultCellStyle.Font = Tema.Fuente(10.5f, true);
                dgvMateriales.Columns["stockActual"].DisplayIndex = 2;
            }

            if (dgvMateriales.Columns["stockMinimo"] != null)
            {
                dgvMateriales.Columns["stockMinimo"].Visible = !ocultarCantidades;
                dgvMateriales.Columns["stockMinimo"].HeaderText = "Mínimo";
                dgvMateriales.Columns["stockMinimo"].DefaultCellStyle.Format = "N0";
                dgvMateriales.Columns["stockMinimo"].FillWeight = mostrarPrecios ? 8 : 10;
                dgvMateriales.Columns["stockMinimo"].MinimumWidth = 95;
                dgvMateriales.Columns["stockMinimo"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                dgvMateriales.Columns["stockMinimo"].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                dgvMateriales.Columns["stockMinimo"].DisplayIndex = 3;
            }
        }

        private void dgvMateriales_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex >= 0 && e.RowIndex < dgvMateriales.Rows.Count)
            {
                var row = dgvMateriales.Rows[e.RowIndex];
                if (row.DataBoundItem is Material item)
                {
                    if (item.esFilaSeparadora)
                    {
                        e.CellStyle.BackColor = ColorSeccion;
                        e.CellStyle.SelectionBackColor = ColorSeccion;
                        e.CellStyle.ForeColor = Tema.Texto;
                        e.CellStyle.SelectionForeColor = Tema.Texto;
                        e.CellStyle.Font = Tema.Fuente(10.5f, true);

                        if (dgvMateriales.Columns[e.ColumnIndex].Name != "nombre")
                            e.Value = string.Empty;

                        return;
                    }

                    if (!esAdmin()
                        && dgvMateriales.Columns[e.ColumnIndex].Name == "stockActual")
                    {
                        e.Value = SessionManager.ObtenerEstadoStockMaterial(item.stockActual);
                        e.FormattingApplied = true;
                    }

                    if (item.stockMinimo > 0 && item.stockActual <= item.stockMinimo)
                    {
                        // Balanced vibrant red: brighter than pure red but with enough saturation to look like a warning
                        e.CellStyle.ForeColor = Color.FromArgb(240, 110, 100); 
                        e.CellStyle.SelectionForeColor = Color.FromArgb(255, 140, 130); 
                        e.CellStyle.Font = Tema.Fuente(10.5f, true);
                    }
                    else
                    {
                        e.CellStyle.ForeColor = Tema.Texto;
                        e.CellStyle.SelectionForeColor = Color.White;
                    }

                    if (dgvMateriales.Columns[e.ColumnIndex].Name == "reporteSeccionCabecera"
                        && !string.IsNullOrWhiteSpace(item.reporteSeccionCabecera))
                    {
                        e.CellStyle.BackColor = Tema.Banda;
                        e.CellStyle.SelectionBackColor = Tema.Banda;
                        e.CellStyle.ForeColor = Tema.Texto;
                        e.CellStyle.SelectionForeColor = Tema.Texto;
                        e.CellStyle.Font = Tema.Fuente(10.5f, true);
                    }
                }
            }
        }

        private void dgvMateriales_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= dgvMateriales.Rows.Count)
                return;

            if (!(dgvMateriales.Rows[e.RowIndex].DataBoundItem is Material item) || !item.esFilaSeparadora)
                return;

            e.Handled = true;
            e.PaintBackground(e.CellBounds, false);

            using (var fondo = new SolidBrush(ColorSeccion))
            {
                e.Graphics.FillRectangle(fondo, e.CellBounds);
            }

            if (dgvMateriales.Columns[e.ColumnIndex].Visible
                && dgvMateriales.Columns[e.ColumnIndex].DisplayIndex == 0)
            {
                int anchoFila = dgvMateriales.RowHeadersVisible
                    ? dgvMateriales.RowHeadersWidth + dgvMateriales.Columns.GetColumnsWidth(DataGridViewElementStates.Visible)
                    : dgvMateriales.Columns.GetColumnsWidth(DataGridViewElementStates.Visible);

                var rectTexto = new System.Drawing.Rectangle(
                    e.CellBounds.Left + 12,
                    e.CellBounds.Top,
                    Math.Max(0, anchoFila - 24),
                    e.CellBounds.Height);

                TextRenderer.DrawText(
                    e.Graphics,
                    item.tituloSeparador?.ToUpperInvariant() ?? string.Empty,
                    Tema.Fuente(10.5f, true),
                    rectTexto,
                    Tema.Texto,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);

                using (var lapiz = new Pen(Tema.Borde, 1))
                {
                    e.Graphics.DrawLine(lapiz, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Left + anchoFila, e.CellBounds.Bottom - 1);
                }
            }
        }

        private void dgvMateriales_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvMateriales.CurrentRow?.DataBoundItem is Material item && item.esFilaSeparadora)
            {
                dgvMateriales.ClearSelection();
                dgvMateriales.CurrentCell = null;
            }
        }

        private void txtFiltroMaterial_TextChanged(object sender, EventArgs e)
        {
            string nombre = txtFiltroMaterial.Text.Trim();
            CargarMateriales(string.IsNullOrEmpty(nombre) ? null : nombre, toggleStockBajo.Checked);
        }

        private void toggleStockBajo_CheckedChanged(object sender, EventArgs e)
        {
            txtFiltroMaterial_TextChanged(sender, e);
        }
        
        private void CalcularTotales()
        {
            int totalMateriales = 0;
            int stockBajo = 0;
            decimal valorTotal = 0;

            foreach (DataGridViewRow row in dgvMateriales.Rows)
            {
                if (row.DataBoundItem is Material mat)
                {
                    if (mat.esFilaSeparadora)
                        continue;

                    totalMateriales++;
                    if (mat.stockMinimo > 0 && mat.stockActual <= mat.stockMinimo) stockBajo++;
                    valorTotal += mat.stockActual * mat.precioUnitario;
                }
            }

            lblTotalVal.Text = totalMateriales.ToString("N0");
            lblBajoVal.Text = stockBajo.ToString("N0");
        }

        private void ActualizarGrafico()
        {
            chartCategorias.Series[0].Points.Clear();
            
            // Agrupar por categoría
            var conteoCategorias = new Dictionary<string, int>();
            foreach (DataGridViewRow row in dgvMateriales.Rows)
            {
                if (row.DataBoundItem is Material mat)
                {
                    if (mat.esFilaSeparadora)
                        continue;

                    if (conteoCategorias.ContainsKey(mat.nombre_categoria))
                        conteoCategorias[mat.nombre_categoria]++;
                    else
                        conteoCategorias.Add(mat.nombre_categoria, 1);
                }
            }

            foreach (var kvp in conteoCategorias)
            {
                chartCategorias.Series[0].Points.AddXY(kvp.Key, kvp.Value);
            }
            
            // Opcional: Ocultar leyenda si hay muchas categorías o ajustar estilo
            chartCategorias.Series[0]["PieLabelStyle"] = "Disabled"; 
        }

        private void btnExportarExcel_Click(object sender, EventArgs e)
        {
            if (identidadDatos == null || !ReferenceEquals(identidadDatos, identidadActual())) return;
             if (dgvMateriales.Rows.Count == 0)
            {
                ToastNotification.ShowWarning("No hay datos para exportar.");
                return;
            }

            int versionExportacion = version;
            SaveFileDialog save = new SaveFileDialog();
            save.Filter = "Excel Files (*.xlsx)|*.xlsx";
            save.FileName = "ReporteStock_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".xlsx";

            if (save.ShowDialog() == DialogResult.OK)
            {
                if (IsDisposed || vida.IsCancellationRequested || versionExportacion != version || !ReferenceEquals(identidadDatos, identidadActual())) return;
                try
                {
                    using (ExcelPackage excel = new ExcelPackage())
                    {
                        excel.Workbook.Worksheets.Add("Stock");
                        var headerRow = new List<string[]>();

                        // Obtener nombres de columnas visibles
                        var visibleColumns = dgvMateriales.Columns.Cast<DataGridViewColumn>().Where(c => c.Visible).ToList();

                        var ws = excel.Workbook.Worksheets["Stock"];

                        // Encabezados
                        for (int i = 0; i < visibleColumns.Count; i++)
                        {
                            ws.Cells[1, i + 1].Value = visibleColumns[i].HeaderText;
                            ws.Cells[1, i + 1].Style.Font.Bold = true;
                            ws.Cells[1, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                            ws.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(35, 63, 43));
                            ws.Cells[1, i + 1].Style.Font.Color.SetColor(System.Drawing.Color.White);
                        }

                        // Datos
                        for (int i = 0; i < dgvMateriales.Rows.Count; i++)
                        {
                            var material = dgvMateriales.Rows[i].DataBoundItem as Material;

                            if (material != null && material.esFilaSeparadora)
                            {
                                ws.Cells[i + 2, 1].Value = material.tituloSeparador?.ToUpperInvariant();
                                ws.Cells[i + 2, 1, i + 2, visibleColumns.Count].Merge = true;
                                ws.Cells[i + 2, 1].Style.Font.Bold = true;
                                ws.Cells[i + 2, 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                                ws.Cells[i + 2, 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(24, 74, 53));
                                ws.Cells[i + 2, 1].Style.Font.Color.SetColor(System.Drawing.Color.White);
                                continue;
                            }

                            for (int j = 0; j < visibleColumns.Count; j++)
                            {
                                object cellValue = dgvMateriales.Rows[i].Cells[visibleColumns[j].Index].Value;
                                if (!esAdmin() && visibleColumns[j].Name == "stockActual")
                                    cellValue = SessionManager.ObtenerEstadoStockMaterial(material.stockActual);
                                ws.Cells[i + 2, j + 1].Value = cellValue;

                                // Formato moneda si es precio
                                if (visibleColumns[j].Name == "precioUnitario" || visibleColumns[j].Name == "valorTotal") 
                                    ws.Cells[i + 2, j + 1].Style.Numberformat.Format = "$#,##0.00";
                            }
                        }

                        ws.Cells.AutoFitColumns();
                        FileInfo excelFile = new FileInfo(save.FileName);
                        excel.SaveAs(excelFile);
                    }
                    ToastNotification.ShowSuccess("Reporte exportado correctamente a Excel.");
                }
                catch (Exception ex)
                {
                    ToastNotification.ShowError("Error al exportar Excel: " + ex.Message);
                }
            }
        }

        private void filtrarMaterial_Click(object sender, EventArgs e)
        {
            if (this.filtroAbierto == null || this.filtroAbierto.IsDisposed)
            {
                this.filtroAbierto = new FiltroMaterial(LocalServices.Current);
                this.filtroAbierto.onBuscar += RecibirFiltro;
            }
            AbrirFiltros(this.filtroAbierto);
        }

        private void AbrirFiltros(FiltroMaterial filtroForm)
        {
            filtroForm.StartPosition = FormStartPosition.Manual;

            System.Drawing.Rectangle areaTrabajo = Screen.FromControl(this).WorkingArea;
            Point botonScreen = filtrarMaterial.PointToScreen(Point.Empty);

            int x = botonScreen.X - filtroForm.Width - 10;
            int y = botonScreen.Y - (filtroForm.Height / 2) + (filtrarMaterial.Height / 2);

            if (x < areaTrabajo.Left) x = botonScreen.X + filtrarMaterial.Width + 10;
            if (y < areaTrabajo.Top) y = areaTrabajo.Top + 10;
            else if (y + filtroForm.Height > areaTrabajo.Bottom) y = areaTrabajo.Bottom - filtroForm.Height - 10;
            if (x + filtroForm.Width > areaTrabajo.Right) x = areaTrabajo.Right - filtroForm.Width - 10;

            filtroForm.Location = new Point(x, y);
            this.filtroAbierto = filtroForm;
            filtroForm.Show(this);
        }

        private void RecibirFiltro(object sender, FiltroMaterialParams filtro)
        {
            CargarMateriales(txtFiltroMaterial.Text.Trim(), toggleStockBajo.Checked, filtro);
        }

        private async void btnExportarPDF_Click(object sender, EventArgs e)
        {
            if (exportando || identidadDatos == null || !ReferenceEquals(identidadDatos, identidadActual()) || dgvMateriales.DataSource == null || dgvMateriales.Rows.Count == 0) return;
            int versionExportacion = version;
            string destino = elegirPdf();
            if (string.IsNullOrEmpty(destino)) return;
            if (IsDisposed || vida.IsCancellationRequested || versionExportacion != version || !ReferenceEquals(identidadDatos, identidadActual())) return;
            {
                var columnas = dgvMateriales.Columns.Cast<DataGridViewColumn>().Where(c => c.Visible).ToList();
                var snapshot = new ReporteStockSnapshot { FechaDatos = fechaDatos, TotalMateriales = lblTotalVal.Text, TotalStockBajo = lblBajoVal.Text,
                    Columnas = columnas.Select(c => new ReporteStockColumnaPdf { Nombre = c.Name, Titulo = c.HeaderText }).ToList() };
                foreach (DataGridViewRow row in dgvMateriales.Rows)
                {
                    if (row.IsNewRow) continue;
                    var mat = row.DataBoundItem as Material;
                    if (mat == null) continue;
                    snapshot.Filas.Add(mat.esFilaSeparadora ? new ReporteStockFilaPdf { Seccion = mat.tituloSeparador ?? "" } : new ReporteStockFilaPdf {
                        StockBajo = mat.stockMinimo > 0 && mat.stockActual <= mat.stockMinimo,
                        Valores = columnas.Select(c => row.Cells[c.Index].FormattedValue?.ToString() ?? "").ToList() });
                }
                int solicitud = version;
                var identidad = identidadActual();
                var temporal = Path.Combine(Path.GetTempPath(), "ReporteStock_" + Guid.NewGuid().ToString("N") + ".pdf");
                exportando = true;
                try
                {
                    await presenter.GenerarAsync(temporal, snapshot, vida.Token);
                    if (vida.IsCancellationRequested || IsDisposed || solicitud != version || !ReferenceEquals(identidad, identidadActual())) return;
                    File.Copy(temporal, destino, true);
                    avisarExito("Reporte exportado correctamente a PDF.");
                    try { abrirPdf(destino); } catch { }
                }
                catch (OperationCanceledException) { }
                catch (Exception ex) { if (!vida.IsCancellationRequested && !IsDisposed && solicitud == version && ReferenceEquals(identidad, identidadActual())) avisarError("Error al exportar PDF: " + ex.Message); }
                finally { exportando = false; try { if (File.Exists(temporal)) File.Delete(temporal); } catch { } }
            }
        }
        private static string ElegirDestinoPdf()
        {
            using (var save = new SaveFileDialog { Filter = "PDF Files (*.pdf)|*.pdf", FileName = "ReporteStock_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".pdf" })
                return save.ShowDialog() == DialogResult.OK ? save.FileName : null;
        }
    }
}
