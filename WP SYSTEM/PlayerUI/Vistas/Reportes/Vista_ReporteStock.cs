using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WP_Inventario.Datos;
using WP_Inventario.Modelos;
using WP_Inventario.Servicios;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;
using OfficeOpenXml;
using System.Windows.Forms.DataVisualization.Charting;

namespace WP_Inventario.Vistas
{
    public partial class Vista_ReporteStock : Form
    {
        private FiltroMaterial filtroAbierto = null;

        private Guna.UI2.WinForms.Guna2Button btnTogglePaneles;
        private Guna.UI2.WinForms.Guna2Button btnConfigurarSecciones;
        private bool panelesVisibles = false;
        private int panelDashboardHeight;
        private readonly ReporteStockDatos reporteStockDatos = new ReporteStockDatos();

        public Vista_ReporteStock()
        {
            InitializeComponent();
            this.Load += Vista_ReporteStock_Load;
            dgvMateriales.CellPainting += dgvMateriales_CellPainting;
            dgvMateriales.SelectionChanged += dgvMateriales_SelectionChanged;
            CargarMateriales();
            CrearBotonTogglePaneles();
        }

        private void Vista_ReporteStock_Load(object sender, EventArgs e)
        {
            panelDashboardHeight = panelDashboard.Height;
            panelDashboard.Visible = false;
            materiales.Top = panelDashboard.Top;
            materiales.Height = this.ClientSize.Height - materiales.Top;
        }

        private void CrearBotonTogglePaneles()
        {
            btnTogglePaneles = new Guna.UI2.WinForms.Guna2Button();
            btnTogglePaneles.Text = "▼ Mostrar Paneles";
            btnTogglePaneles.Size = new Size(160, 28);
            btnTogglePaneles.FillColor = Color.FromArgb(47, 84, 57);
            btnTogglePaneles.ForeColor = Color.White;
            btnTogglePaneles.Font = new System.Drawing.Font("Bahnschrift Light", 9.5F);
            btnTogglePaneles.BorderRadius = 3;
            btnTogglePaneles.HoverState.FillColor = Color.FromArgb(60, 100, 70);
            btnTogglePaneles.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnTogglePaneles.Click += BtnTogglePaneles_Click;
            // Position it to the right of the subtitle panel
            panelAntiParpadeo6.ColumnCount = 2;
            panelAntiParpadeo6.ColumnStyles.Clear();
            panelAntiParpadeo6.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            panelAntiParpadeo6.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            panelAntiParpadeo6.Controls.Add(btnTogglePaneles, 1, 0);
            btnTogglePaneles.Anchor = AnchorStyles.Right;

            if (SessionManager.Role == UserRole.Admin)
            {
                btnConfigurarSecciones = new Guna.UI2.WinForms.Guna2Button
                {
                    Text = "Configurar secciones",
                    Size = new Size(175, 28),
                    FillColor = Color.FromArgb(35, 63, 43),
                    ForeColor = Color.White,
                    Font = new System.Drawing.Font("Bahnschrift Light", 9.5F),
                    BorderRadius = 3,
                    Anchor = AnchorStyles.Right
                };
                btnConfigurarSecciones.Click += BtnConfigurarSecciones_Click;
                panelAntiParpadeo6.ColumnCount = 3;
                panelAntiParpadeo6.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                panelAntiParpadeo6.Controls.Add(btnConfigurarSecciones, 2, 0);
            }
        }

        private void BtnConfigurarSecciones_Click(object sender, EventArgs e)
        {
            using (var modal = new Reportes.ModalConfigReporteStock())
                modal.ShowDialog(this);
            CargarMateriales(txtFiltroMaterial.Text.Trim(), toggleStockBajo.Checked);
        }

        private void BtnTogglePaneles_Click(object sender, EventArgs e)
        {
            panelesVisibles = !panelesVisibles;
            panelDashboard.Visible = panelesVisibles;

            if (panelesVisibles)
            {
                btnTogglePaneles.Text = "▲ Ocultar Paneles";
                // Restore original position
                materiales.Top = panelDashboard.Bottom + 6;
            }
            else
            {
                btnTogglePaneles.Text = "▼ Mostrar Paneles";
                // Move list up into the space freed by hiding the dashboard
                materiales.Top = panelDashboard.Top;
            }

            // Recalculate height to fill to the bottom
            materiales.Height = this.ClientSize.Height - materiales.Top;
        }

        private void CargarMateriales(string nombre = null, bool soloStockBajo = false, FiltroMaterialParams filtro = null)
        {
            try
            {
                MaterialDatos datos = new MaterialDatos();
                List<Material> listaMateriales;
                if (reporteStockDatos.TieneConfiguracion())
                {
                    listaMateriales = reporteStockDatos.ObtenerMaterialesReporte(nombre, soloStockBajo);
                    if (filtro != null)
                    {
                        var idsFiltrados = new HashSet<int>(datos.obtenerMateriales(filtro).Select(m => m.id));
                        listaMateriales = listaMateriales.Where(m => idsFiltrados.Contains(m.id)).ToList();
                    }

                    AplicarCabecerasDeSeccion(listaMateriales);
                    listaMateriales = ConstruirFilasConSeparadores(listaMateriales);
                }
                else if (soloStockBajo)
                {
                    listaMateriales = datos.ObtenerMaterialesStockBajoPriorizados();
                    if (!string.IsNullOrWhiteSpace(nombre))
                        listaMateriales = listaMateriales.Where(m => m.nombre.IndexOf(nombre, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
                }
                else
                {
                    listaMateriales = filtro != null ? datos.obtenerMateriales(filtro) : datos.obtenerMateriales(nombre);
                }
                dgvMateriales.DataSource = new SortableBindingList<Material>(listaMateriales);


                ConfigurarGrid();
                AplicarAlturaFilasEspeciales();
                CalcularTotales();
                ActualizarGrafico();
            }
            catch (Exception ex)
            {
                ToastNotification.ShowError("No se pudo cargar los materiales: " + ex.Message);
            }
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
                    row.Height = item.esFilaSeparadora ? 38 : 45;
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
            bool mostrarPrecios = SessionManager.Role == UserRole.Admin;
            bool ocultarCantidades = SessionManager.DebeOcultarCantidadesStockMateriales;

            dgvMateriales.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvMateriales.MultiSelect = false;
            dgvMateriales.ReadOnly = true;
            dgvMateriales.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvMateriales.AllowUserToResizeColumns = false;
            dgvMateriales.AllowUserToResizeRows = false;
            dgvMateriales.EnableHeadersVisualStyles = false;
            dgvMateriales.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgvMateriales.ColumnHeadersHeight = 45;
            dgvMateriales.RowTemplate.Height = 45;

            // Define Green Colors (matching Kardex)
            Color headerColor = Color.FromArgb(35, 63, 43);
            Color rowColor1 = Color.FromArgb(28, 40, 32);
            Color rowColor2 = Color.FromArgb(35, 50, 40);
            Color selectionColor = Color.FromArgb(50, 90, 60);

            // Header Style
            dgvMateriales.ColumnHeadersDefaultCellStyle.BackColor = headerColor;
            dgvMateriales.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvMateriales.ColumnHeadersDefaultCellStyle.SelectionBackColor = headerColor;
            dgvMateriales.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
            dgvMateriales.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dgvMateriales.ColumnHeadersDefaultCellStyle.Font = new System.Drawing.Font("Bahnschrift", 11, FontStyle.Bold);

            // Row Styles
            dgvMateriales.BackgroundColor = Color.FromArgb(20, 30, 25);

            dgvMateriales.DefaultCellStyle.BackColor = rowColor1;
            dgvMateriales.DefaultCellStyle.ForeColor = Color.White;
            dgvMateriales.DefaultCellStyle.Font = new System.Drawing.Font("Bahnschrift Light", 11);
            dgvMateriales.DefaultCellStyle.SelectionBackColor = selectionColor;
            dgvMateriales.DefaultCellStyle.SelectionForeColor = Color.White;
            dgvMateriales.DefaultCellStyle.Padding = new Padding(5);
            dgvMateriales.GridColor = Color.FromArgb(40, 60, 50);

            dgvMateriales.AlternatingRowsDefaultCellStyle.BackColor = rowColor2;
            dgvMateriales.AlternatingRowsDefaultCellStyle.ForeColor = Color.White;
            dgvMateriales.AlternatingRowsDefaultCellStyle.SelectionBackColor = selectionColor;
            dgvMateriales.AlternatingRowsDefaultCellStyle.SelectionForeColor = Color.White;

            // Theme properties for Guna Grid
            dgvMateriales.ThemeStyle.BackColor = Color.FromArgb(20, 30, 25);
            dgvMateriales.ThemeStyle.GridColor = Color.FromArgb(40, 60, 50);
            dgvMateriales.ThemeStyle.HeaderStyle.BackColor = headerColor;
            dgvMateriales.ThemeStyle.HeaderStyle.ForeColor = Color.White;
            dgvMateriales.ThemeStyle.HeaderStyle.Height = 45;
            dgvMateriales.ThemeStyle.RowsStyle.BackColor = rowColor1;
            dgvMateriales.ThemeStyle.RowsStyle.ForeColor = Color.White;
            dgvMateriales.ThemeStyle.RowsStyle.SelectionBackColor = selectionColor;
            dgvMateriales.ThemeStyle.RowsStyle.SelectionForeColor = Color.White;
            dgvMateriales.ThemeStyle.RowsStyle.Height = 45;
            dgvMateriales.ThemeStyle.AlternatingRowsStyle.BackColor = rowColor2;

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
                dgvMateriales.Columns["precioUnitario"].MinimumWidth = 120;
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
                dgvMateriales.Columns["valorTotal"].MinimumWidth = 120;
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
                dgvMateriales.Columns["stockActual"].DefaultCellStyle.Font = new System.Drawing.Font("Bahnschrift", 11, FontStyle.Bold);
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
                        e.CellStyle.BackColor = Color.FromArgb(24, 74, 53);
                        e.CellStyle.SelectionBackColor = Color.FromArgb(24, 74, 53);
                        e.CellStyle.ForeColor = Color.FromArgb(218, 239, 227);
                        e.CellStyle.SelectionForeColor = Color.FromArgb(218, 239, 227);
                        e.CellStyle.Font = new System.Drawing.Font("Bahnschrift SemiBold", 11F, FontStyle.Bold);

                        if (dgvMateriales.Columns[e.ColumnIndex].Name != "nombre")
                            e.Value = string.Empty;

                        return;
                    }

                    if (SessionManager.DebeOcultarCantidadesStockMateriales
                        && dgvMateriales.Columns[e.ColumnIndex].Name == "stockActual")
                    {
                        e.Value = SessionManager.ObtenerEstadoStockMaterial(item.stockActual);
                        e.FormattingApplied = true;
                    }

                    if (item.stockMinimo > 0 && item.stockActual <= item.stockMinimo)
                    {
                        // Balanced vibrant red: brighter than pure red but with enough saturation to look like a warning
                        e.CellStyle.ForeColor = Color.FromArgb(255, 70, 70); 
                        e.CellStyle.SelectionForeColor = Color.FromArgb(255, 120, 120); 
                        e.CellStyle.Font = new System.Drawing.Font(e.CellStyle.Font, FontStyle.Bold);
                    }
                    else
                    {
                        e.CellStyle.ForeColor = Color.White;
                        e.CellStyle.SelectionForeColor = Color.White;
                    }

                    if (dgvMateriales.Columns[e.ColumnIndex].Name == "reporteSeccionCabecera"
                        && !string.IsNullOrWhiteSpace(item.reporteSeccionCabecera))
                    {
                        e.CellStyle.BackColor = Color.FromArgb(35, 63, 43);
                        e.CellStyle.SelectionBackColor = Color.FromArgb(35, 63, 43);
                        e.CellStyle.ForeColor = Color.FromArgb(230, 245, 236);
                        e.CellStyle.SelectionForeColor = Color.FromArgb(230, 245, 236);
                        e.CellStyle.Font = new System.Drawing.Font("Bahnschrift", 10.5F, FontStyle.Bold);
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

            using (var fondo = new SolidBrush(Color.FromArgb(24, 74, 53)))
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
                    new System.Drawing.Font("Bahnschrift SemiBold", 11F, FontStyle.Bold),
                    rectTexto,
                    Color.FromArgb(235, 247, 239),
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);

                using (var lapiz = new Pen(Color.FromArgb(71, 122, 94), 1))
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
             if (dgvMateriales.Rows.Count == 0)
            {
                ToastNotification.ShowWarning("No hay datos para exportar.");
                return;
            }

            SaveFileDialog save = new SaveFileDialog();
            save.Filter = "Excel Files (*.xlsx)|*.xlsx";
            save.FileName = "ReporteStock_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".xlsx";

            if (save.ShowDialog() == DialogResult.OK)
            {
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
                                if (SessionManager.DebeOcultarCantidadesStockMateriales && visibleColumns[j].Name == "stockActual")
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
                this.filtroAbierto = new FiltroMaterial();
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
            CargarMateriales(filtro: filtro);
        }

        private void btnExportarPDF_Click(object sender, EventArgs e)
        {
            if (dgvMateriales.Rows.Count == 0)
            {
                ToastNotification.ShowWarning("No hay datos para exportar.");
                return;
            }

            SaveFileDialog save = new SaveFileDialog();
            save.Filter = "PDF Files (*.pdf)|*.pdf";
            save.FileName = "ReporteStock_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".pdf";

            if (save.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    // Crear documento con los mismos márgenes que MovimientoPDF
                    Document doc = new Document(PageSize.A4, 40, 40, 40, 60);
                    PdfWriter writer = PdfWriter.GetInstance(doc, new FileStream(save.FileName, FileMode.Create));
                    
                    writer.PageEvent = new PieReporteStock();
                    
                    doc.Open();

                    // Encabezado
                    PdfPTable headerTable = new PdfPTable(2);
                    headerTable.WidthPercentage = 100;
                    headerTable.SetWidths(new float[] { 45f, 55f });

                    // ======= IZQUIERDA: LOGO Y DATOS DE EMPRESA =======
                    PdfPCell cellIzq = new PdfPCell() { Border = iTextSharp.text.Rectangle.NO_BORDER, VerticalAlignment = Element.ALIGN_TOP };

                    // Logo cargado desde recurso embebido
                    try
                    {
                        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                        string resourceName = assembly.GetManifestResourceNames().FirstOrDefault(n => n.ToLower().EndsWith("wp_horizontal.png"));
                        if (resourceName != null)
                        {
                            using (var stream = assembly.GetManifestResourceStream(resourceName))
                            {
                                if (stream != null)
                                {
                                    byte[] buffer = new byte[stream.Length];
                                    stream.Read(buffer, 0, buffer.Length);
                                    iTextSharp.text.Image logo = iTextSharp.text.Image.GetInstance(buffer);
                                    logo.ScaleToFit(150, 62);
                                    logo.Alignment = Element.ALIGN_LEFT;
                                    cellIzq.AddElement(logo);
                                }
                            }
                        }
                    }
                    catch { /* Silencioso si falla el logo */ }

                    // Datos de la empresa
                    var fntDir = new iTextSharp.text.Font(iTextSharp.text.Font.FontFamily.HELVETICA, 8, iTextSharp.text.Font.NORMAL, new BaseColor(80, 80, 80));
                    var fntTributo = new iTextSharp.text.Font(iTextSharp.text.Font.FontFamily.HELVETICA, 7, iTextSharp.text.Font.NORMAL, new BaseColor(80, 80, 80));

                    cellIzq.AddElement(new Paragraph("Av. Tecnología 1000 - Parque Tecnológico", fntDir) { SpacingBefore = 8, Alignment = Element.ALIGN_LEFT });
                    string separador = "   \u2022   ";
                    cellIzq.AddElement(new Paragraph($"CUIT: 30-75001234-9{separador}Ingresos Brutos: 30-75001234-9", fntTributo) { SpacingBefore = 2, Alignment = Element.ALIGN_LEFT });
                    cellIzq.AddElement(new Paragraph("SteeLog - Logística y Stock", fntTributo) { SpacingBefore = 1, Alignment = Element.ALIGN_LEFT });

                    headerTable.AddCell(cellIzq);

                    // ======= DERECHA: TÍTULO Y SUBTÍTULO =======
                    PdfPCell cellDer = new PdfPCell() { Border = iTextSharp.text.Rectangle.NO_BORDER, VerticalAlignment = Element.ALIGN_TOP, HorizontalAlignment = Element.ALIGN_RIGHT, PaddingTop = 0f };
                    var fntTitle = new iTextSharp.text.Font(iTextSharp.text.Font.FontFamily.HELVETICA, 20, iTextSharp.text.Font.BOLD, new BaseColor(25, 50, 35));
                    var fntSub = new iTextSharp.text.Font(iTextSharp.text.Font.FontFamily.HELVETICA, 11, iTextSharp.text.Font.NORMAL, new BaseColor(100, 100, 100));
                    
                    cellDer.AddElement(new Paragraph("REPORTE DE STOCK", fntTitle) { Alignment = Element.ALIGN_RIGHT, SpacingBefore = 0 });
                    cellDer.AddElement(new Paragraph("Inventario al " + DateTime.Now.ToString("dd/MM/yyyy HH:mm"), fntSub) { Alignment = Element.ALIGN_RIGHT, SpacingBefore = 4 });

                    headerTable.AddCell(cellDer);

                    doc.Add(headerTable);

                    PdfPTable lineaSep = new PdfPTable(1) { WidthPercentage = 100, SpacingBefore = 5, SpacingAfter = 15 };
                    lineaSep.AddCell(new PdfPCell { Border = iTextSharp.text.Rectangle.BOTTOM_BORDER, BorderColor = new BaseColor(25, 135, 84), BorderWidth = 2, FixedHeight = 3 });
                    doc.Add(lineaSep);

                    // Información global
                    PdfPTable infoTable = new PdfPTable(4) { WidthPercentage = 100, SpacingAfter = 20 };
                    infoTable.SetWidths(new float[] { 22f, 28f, 22f, 28f });
                    var fntEtiq = new iTextSharp.text.Font(iTextSharp.text.Font.FontFamily.HELVETICA, 10, iTextSharp.text.Font.BOLD, new BaseColor(80, 80, 80));
                    var fntVal = new iTextSharp.text.Font(iTextSharp.text.Font.FontFamily.HELVETICA, 10, iTextSharp.text.Font.NORMAL, new BaseColor(33, 37, 41));
                    BaseColor bgFila = new BaseColor(245, 248, 246);
                    BaseColor borderC = new BaseColor(230, 230, 230);

                    infoTable.AddCell(new PdfPCell(new Phrase("Total Materiales:", fntEtiq)) { BackgroundColor = bgFila, BorderColor = borderC, Padding = 8, HorizontalAlignment = Element.ALIGN_RIGHT });
                    infoTable.AddCell(new PdfPCell(new Phrase(lblTotalVal.Text, fntVal)) { BackgroundColor = BaseColor.WHITE, BorderColor = borderC, Padding = 8 });
                    infoTable.AddCell(new PdfPCell(new Phrase("Stock Bajo:", fntEtiq)) { BackgroundColor = bgFila, BorderColor = borderC, Padding = 8, HorizontalAlignment = Element.ALIGN_RIGHT });
                    infoTable.AddCell(new PdfPCell(new Phrase(lblBajoVal.Text, fntVal)) { BackgroundColor = BaseColor.WHITE, BorderColor = borderC, Padding = 8 });
                    doc.Add(infoTable);

                    // Tabla
                    var visibleColumns = dgvMateriales.Columns.Cast<DataGridViewColumn>().Where(c => c.Visible).ToList();
                    PdfPTable table = new PdfPTable(visibleColumns.Count);
                    table.WidthPercentage = 100;
                    
                    // Ajustar anchos proporcionalmente (basado en el grid si es posible o equilibrado)
                    float[] widths = new float[visibleColumns.Count];
                    for (int i = 0; i < visibleColumns.Count; i++) widths[i] = 1f; 
                    table.SetWidths(widths);

                    // Cabeceras
                    foreach (var col in visibleColumns)
                    {
                        PdfPCell cell = new PdfPCell(new Phrase(col.HeaderText, new iTextSharp.text.Font(iTextSharp.text.Font.FontFamily.HELVETICA, 9, iTextSharp.text.Font.BOLD, BaseColor.WHITE)));
                        cell.BackgroundColor = new BaseColor(35, 63, 43);
                        cell.HorizontalAlignment = Element.ALIGN_CENTER;
                        cell.Border = iTextSharp.text.Rectangle.NO_BORDER;
                        cell.Padding = 6;
                        table.AddCell(cell);
                    }

                    // Datos
                    bool alt = false;
                    foreach (DataGridViewRow row in dgvMateriales.Rows)
                    {
                        if (row.IsNewRow) continue;
                        var material = row.DataBoundItem as Material;
                        if (material != null && material.esFilaSeparadora)
                        {
                            var celdaSeccion = new PdfPCell(new Phrase(
                                material.tituloSeparador?.ToUpperInvariant() ?? string.Empty,
                                new iTextSharp.text.Font(iTextSharp.text.Font.FontFamily.HELVETICA, 10, iTextSharp.text.Font.BOLD, BaseColor.WHITE)))
                            {
                                Colspan = visibleColumns.Count,
                                BackgroundColor = new BaseColor(24, 74, 53),
                                Border = iTextSharp.text.Rectangle.NO_BORDER,
                                Padding = 7
                            };
                            table.AddCell(celdaSeccion);
                            alt = false;
                            continue;
                        }
                        BaseColor bgColor = alt ? new BaseColor(245, 248, 246) : BaseColor.WHITE;

                        foreach (var col in visibleColumns)
                        {
                            string cellValue = row.Cells[col.Index].FormattedValue?.ToString() ?? "";
                            PdfPCell cell = new PdfPCell(new Phrase(cellValue, new iTextSharp.text.Font(iTextSharp.text.Font.FontFamily.HELVETICA, 9, iTextSharp.text.Font.NORMAL, new BaseColor(33, 37, 41))));
                            cell.VerticalAlignment = Element.ALIGN_MIDDLE;
                            cell.HorizontalAlignment = col.Name == "precioUnitario" || col.Name == "valorTotal"
                                ? Element.ALIGN_RIGHT
                                : (col.Name.Contains("stock") ? Element.ALIGN_CENTER : Element.ALIGN_LEFT);
                            cell.BackgroundColor = bgColor;
                            cell.Border = iTextSharp.text.Rectangle.BOX;
                            cell.BorderColor = new BaseColor(230, 230, 230);
                            cell.BorderWidth = 0.5f;
                            cell.Padding = 5;
                            
                            // Si el stock es bajo
                            if (col.Name == "stockActual")
                            {
                                if (row.DataBoundItem is Material mat && mat.stockMinimo > 0 && mat.stockActual <= mat.stockMinimo)
                                {
                                    cell.BackgroundColor = new BaseColor(255, 235, 235); // Fondo rojizo suave
                                    cell.Phrase = new Phrase(cellValue, new iTextSharp.text.Font(iTextSharp.text.Font.FontFamily.HELVETICA, 9, iTextSharp.text.Font.BOLD, new BaseColor(200, 30, 30)));
                                }
                            }

                            table.AddCell(cell);
                        }
                        alt = !alt;
                    }

                    doc.Add(table);
                    doc.Close();

                    ToastNotification.ShowSuccess("Reporte exportado correctamente a PDF.");

                    // Autoabrir PDF
                    try {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                        {
                            FileName = save.FileName,
                            UseShellExecute = true
                        });
                    } catch { }
                }
                catch (Exception ex)
                {
                    ToastNotification.ShowError("Error al exportar PDF: " + ex.Message);
                }
            }
        }
    }

    public class PieReporteStock : PdfPageEventHelper
    {
        public override void OnEndPage(PdfWriter writer, Document document)
        {
            PdfContentByte cb = writer.DirectContent;
            BaseFont bf = BaseFont.CreateFont(BaseFont.HELVETICA_OBLIQUE, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);

            // Línea separadora inferior
            cb.SetColorStroke(new BaseColor(25, 135, 84));
            cb.SetLineWidth(1f);
            cb.MoveTo(document.LeftMargin, document.Bottom - 15);
            cb.LineTo(document.PageSize.Width - document.RightMargin, document.Bottom - 15);
            cb.Stroke();

            cb.BeginText();
            cb.SetFontAndSize(bf, 8);
            cb.SetColorFill(new BaseColor(120, 120, 120));
            cb.ShowTextAligned(Element.ALIGN_LEFT, "SteeLog - Sistema de Gestión de Inventario — Generado " + DateTime.Now.ToString("dd/MM/yyyy HH:mm"), document.LeftMargin, document.Bottom - 28, 0);
            cb.ShowTextAligned(Element.ALIGN_RIGHT, "Página " + writer.PageNumber, document.PageSize.Width - document.RightMargin, document.Bottom - 28, 0);
            cb.EndText();
        }
    }
}
