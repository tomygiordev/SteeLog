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
using WP_Inventario;
using WP_Inventario.Servicios;
using System.IO;
using System.Diagnostics;

namespace WP_Inventario.Vistas
{
    public partial class Vista_Kardex : Form, IRefrescable
    {
        private Guna.UI2.WinForms.Guna2DateTimePicker dtpInicio;
        private Guna.UI2.WinForms.Guna2DateTimePicker dtpFin;
        private Guna.UI2.WinForms.Guna2Button btnDescargarPDF;
        private Guna.UI2.WinForms.Guna2Button btnDescargarExcel;
        private Guna.UI2.WinForms.Guna2ToggleSwitch chkDesde;
        private Guna.UI2.WinForms.Guna2ToggleSwitch chkHasta;
        
        // --- Búsqueda Inteligente ---
        private Timer _timerDebounce;
        private Panel _panelResultados;
        private List<Material> _materialesFull = new List<Material>();
        private Material _materialSeleccionado = null;

        public Vista_Kardex()
        {
            InitializeComponent();
            InitializeCustomControls();
            this.Load += Vista_Kardex_Load;
            ConfigurarGrid();
            CargarMateriales();
        }

        private void InitializeCustomControls()
        {
            Color themeColor = Color.FromArgb(47, 84, 57);
            Color hoverColor = Color.FromArgb(60, 100, 70);
            Font btnFont = new Font("Bahnschrift Light", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            // --- DateTimePickers ---
            dtpInicio = new Guna.UI2.WinForms.Guna2DateTimePicker();
            dtpInicio.Size = new Size(130, 36);
            dtpInicio.Format = DateTimePickerFormat.Short;
            dtpInicio.Value = DateTime.Now.AddMonths(-1);
            dtpInicio.FillColor = themeColor;
            dtpInicio.ForeColor = Color.White;
            dtpInicio.BorderColor = Color.Gray;
            dtpInicio.BorderThickness = 1;
            dtpInicio.Enabled = false;

            dtpFin = new Guna.UI2.WinForms.Guna2DateTimePicker();
            dtpFin.Size = new Size(130, 36);
            dtpFin.Format = DateTimePickerFormat.Short;
            dtpFin.Value = DateTime.Now;
            dtpFin.FillColor = themeColor;
            dtpFin.ForeColor = Color.White;
            dtpFin.BorderColor = Color.Gray;
            dtpFin.BorderThickness = 1;
            dtpFin.Enabled = false;

            // --- Toggle Switches (Guna2ToggleSwitch) - no text, visual only ---
            chkDesde = new Guna.UI2.WinForms.Guna2ToggleSwitch();
            chkDesde.Checked = false;
            chkDesde.Size = new Size(56, 30);
            chkDesde.Animated = true;
            chkDesde.AutoRoundedCorners = true;
            chkDesde.CheckedState.FillColor = Color.FromArgb(80, 160, 90);      // verde ON
            chkDesde.CheckedState.BorderColor = Color.FromArgb(80, 160, 90);
            chkDesde.CheckedState.InnerColor = Color.White;
            chkDesde.CheckedState.InnerBorderColor = Color.White;
            chkDesde.UncheckedState.FillColor = Color.FromArgb(70, 70, 75);     // gris OFF
            chkDesde.UncheckedState.BorderColor = Color.FromArgb(90, 90, 95);
            chkDesde.UncheckedState.InnerColor = Color.White;
            chkDesde.UncheckedState.InnerBorderColor = Color.White;
            chkDesde.Cursor = Cursors.Hand;
            // Toggle Desde: habilita DTP + filtra
            chkDesde.CheckedChanged += (s, e) =>
            {
                dtpInicio.Enabled = chkDesde.Checked;
                CargarKardex();
            };
            // DatePicker Desde: filtra al cambiar fecha (solo si toggle activo)
            dtpInicio.ValueChanged += (s, e) => { if (chkDesde.Checked) CargarKardex(); };

            // --- Desde group: [Toggle | DatePicker] centrados verticalmente ---
            Panel flowDesde = new Panel();
            flowDesde.BackColor = Color.Transparent;
            flowDesde.Dock = DockStyle.Fill;
            chkDesde.Anchor = AnchorStyles.None;
            dtpInicio.Anchor = AnchorStyles.None;
            flowDesde.Controls.Add(chkDesde);
            flowDesde.Controls.Add(dtpInicio);
            flowDesde.Resize += (s, e) =>
            {
                int gap = 10;
                int totalW = chkDesde.Width + gap + dtpInicio.Width;
                int startX = Math.Max(0, (flowDesde.ClientSize.Width - totalW) / 2);
                int midY = flowDesde.ClientSize.Height / 2;
                chkDesde.Location = new Point(startX, midY - chkDesde.Height / 2);
                dtpInicio.Location = new Point(startX + chkDesde.Width + gap, midY - dtpInicio.Height / 2);
            };

            // Toggle Hasta: habilita DTP + filtra
            chkHasta = new Guna.UI2.WinForms.Guna2ToggleSwitch();
            chkHasta.Checked = false;
            chkHasta.Size = new Size(56, 30);
            chkHasta.Animated = true;
            chkHasta.AutoRoundedCorners = true;
            chkHasta.CheckedState.FillColor = Color.FromArgb(80, 160, 90);
            chkHasta.CheckedState.BorderColor = Color.FromArgb(80, 160, 90);
            chkHasta.CheckedState.InnerColor = Color.White;
            chkHasta.CheckedState.InnerBorderColor = Color.White;
            chkHasta.UncheckedState.FillColor = Color.FromArgb(70, 70, 75);
            chkHasta.UncheckedState.BorderColor = Color.FromArgb(90, 90, 95);
            chkHasta.UncheckedState.InnerColor = Color.White;
            chkHasta.UncheckedState.InnerBorderColor = Color.White;
            chkHasta.Cursor = Cursors.Hand;
            chkHasta.CheckedChanged += (s, e) =>
            {
                dtpFin.Enabled = chkHasta.Checked;
                CargarKardex();
            };
            dtpFin.ValueChanged += (s, e) => { if (chkHasta.Checked) CargarKardex(); };
            
            Panel flowHasta = new Panel();
            flowHasta.BackColor = Color.Transparent;
            flowHasta.Dock = DockStyle.Fill;
            chkHasta.Anchor = AnchorStyles.None;
            dtpFin.Anchor = AnchorStyles.None;
            flowHasta.Controls.Add(chkHasta);
            flowHasta.Controls.Add(dtpFin);
            flowHasta.Resize += (s, e) =>
            {
                int gap = 10;
                int totalW = chkHasta.Width + gap + dtpFin.Width;
                int startX = Math.Max(0, (flowHasta.ClientSize.Width - totalW) / 2);
                int midY = flowHasta.ClientSize.Height / 2;
                chkHasta.Location = new Point(startX, midY - chkHasta.Height / 2);
                dtpFin.Location = new Point(startX + chkHasta.Width + gap, midY - dtpFin.Height / 2);
            };

            // --- Botón PDF ---
            btnDescargarPDF = new Guna.UI2.WinForms.Guna2Button();
            btnDescargarPDF.Text = "PDF";
            btnDescargarPDF.Size = new Size(86, 34);
            btnDescargarPDF.FillColor = themeColor;
            btnDescargarPDF.ForeColor = Color.White;
            btnDescargarPDF.Font = btnFont;
            btnDescargarPDF.BorderRadius = 2;
            btnDescargarPDF.Click += BtnDescargarPDF_Click;
            btnDescargarPDF.HoverState.FillColor = Color.FromArgb(60, 100, 70);

            // --- Botón EXCEL ---
            btnDescargarExcel = new Guna.UI2.WinForms.Guna2Button();
            btnDescargarExcel.Text = "EXCEL";
            btnDescargarExcel.Size = new Size(86, 34);
            btnDescargarExcel.FillColor = Color.FromArgb(30, 120, 60); // excel green
            btnDescargarExcel.ForeColor = Color.White;
            btnDescargarExcel.Font = btnFont;
            btnDescargarExcel.BorderRadius = 2;
            btnDescargarExcel.Click += BtnDescargarExcel_Click;
            btnDescargarExcel.HoverState.FillColor = Color.FromArgb(40, 150, 75);

            Panel wrapPDF = CenterPanel(btnDescargarPDF);
            Panel wrapExcel = CenterPanel(btnDescargarExcel);

            // --- Main Filter TableLayoutPanel ---
            // 7 cols centered: Padding Left (%), Desde (fixed), Hasta (fixed), Spacer (fixed), PDF (fixed), Excel (fixed), Padding Right (%)
            TableLayoutPanel panelFiltros = new TableLayoutPanel();
            panelFiltros.Dock = DockStyle.Top;
            panelFiltros.Height = 58;
            panelFiltros.BackColor = Color.Transparent;
            panelFiltros.Padding = new Padding(0, 10, 0, 8); // remove h-padding as we use spacer cols
            panelFiltros.ColumnCount = 7;
            panelFiltros.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));   // Spacer Left
            panelFiltros.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 206F)); // Desde (56 + 10 gap + 130 + 10 margin approx)
            panelFiltros.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 206F)); // Hasta
            panelFiltros.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40F));  // Spacer between Filters and Buttons
            panelFiltros.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F));  // PDF (+4px padding around via wrapPDF)
            panelFiltros.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F));  // Excel
            panelFiltros.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));   // Spacer Right
            panelFiltros.RowCount = 1;
            panelFiltros.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            panelFiltros.Controls.Add(flowDesde, 1, 0);
            panelFiltros.Controls.Add(flowHasta, 2, 0);
            // 3 is Spacer
            panelFiltros.Controls.Add(wrapPDF, 4, 0);
            panelFiltros.Controls.Add(wrapExcel, 5, 0);

            this.Controls.Add(panelFiltros);

            panelAntiParpadeo1.BringToFront();
            panelFiltros.SendToBack();

            panelFiltros.Location = new Point(0, 80);
            panelFiltros.Width = this.Width;
            panelFiltros.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            // Ajustar posición del panel de materiales (que contiene el buscador y el grid)
            // para que empiece justo debajo de los filtros.
            materiales.Top = 138;
            materiales.Height = this.ClientSize.Height - materiales.Top;

            // --- Inicializar Búsqueda Inteligente ---
            CrearPanelResultados();
            ConfigurarDebounce();

            txtBuscadorMaterial.TextChanged += TxtBuscadorMaterial_TextChanged;
            txtBuscadorMaterial.KeyDown += TxtBuscadorMaterial_KeyDown;
            txtBuscadorMaterial.GotFocus += (s, ev) => { if (string.IsNullOrEmpty(txtBuscadorMaterial.Text)) EjecutarBusqueda(""); };
            txtBuscadorMaterial.Click += (s, ev) => { if (string.IsNullOrEmpty(txtBuscadorMaterial.Text)) EjecutarBusqueda(""); };
            
            // Cerrar resultados al perder foco (con pequeño retraso para permitir clicks)
            txtBuscadorMaterial.LostFocus += (s, ev) =>
            {
                Timer t = new Timer { Interval = 200 };
                t.Tick += (ts, te) =>
                {
                    t.Stop();
                    t.Dispose();
                    if (!txtBuscadorMaterial.Focused && _panelResultados != null && !_panelResultados.ContainsFocus)
                    {
                        OcultarResultados();
                    }
                };
                t.Start();
            };

            this.Resize += (s, e) => { if (_panelResultados != null && _panelResultados.Visible) PosicionarPanelResultados(); };
        }

        private void CrearPanelResultados()
        {
            _panelResultados = new Panel
            {
                Size = new Size(txtBuscadorMaterial.Width, 0),
                BackColor = Color.FromArgb(25, 30, 28),
                Visible = false,
                AutoScroll = true,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(_panelResultados);
            _panelResultados.BringToFront();
        }

        private void PosicionarPanelResultados()
        {
            var screenPoint = txtBuscadorMaterial.PointToScreen(Point.Empty);
            var parentPoint = this.PointToClient(screenPoint);

            _panelResultados.Location = new Point(parentPoint.X, parentPoint.Y + txtBuscadorMaterial.Height);
            _panelResultados.Width = txtBuscadorMaterial.Width;
        }

        private void ConfigurarDebounce()
        {
            _timerDebounce = new Timer { Interval = 250 }; // Un poco más rápido
            _timerDebounce.Tick += (s, e) =>
            {
                _timerDebounce.Stop();
                EjecutarBusqueda(txtBuscadorMaterial.Text.Trim());
            };
        }

        private void TxtBuscadorMaterial_TextChanged(object sender, EventArgs e)
        {
            _timerDebounce.Stop();
            
            // Si el texto cambió y no coincide con el seleccionado, deseleccionamos
            if (_materialSeleccionado != null && txtBuscadorMaterial.Text != _materialSeleccionado.nombre)
            {
                _materialSeleccionado = null;
                CargarKardex(); // Recargar "Todos" si borró el nombre
            }

            _timerDebounce.Start();
        }

        private void TxtBuscadorMaterial_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                OcultarResultados();
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Down && _panelResultados.Visible && _panelResultados.Controls.Count > 0)
            {
                _panelResultados.Controls[0].Focus();
                e.SuppressKeyPress = true;
            }
        }

        private void EjecutarBusqueda(string query)
        {
            string qLower = query.ToLower();
            List<Material> filtrados;

            if (string.IsNullOrEmpty(qLower))
            {
                // Si está vacío, mostramos opción de "TODOS" + algunos materiales recientes/primeros
                filtrados = _materialesFull.Take(20).ToList();
            }
            else
            {
                filtrados = _materialesFull
                    .Where(m => m.nombre.ToLower().Contains(qLower) || m.nombre_categoria.ToLower().Contains(qLower))
                    .OrderBy(m => m.nombre.ToLower().StartsWith(qLower) ? 0 : 1)
                    .ThenBy(m => m.nombre)
                    .Take(30)
                    .ToList();
            }

            MostrarResultados(filtrados, query);
        }

        private void MostrarResultados(List<Material> resultados, string query)
        {
            _panelResultados.SuspendLayout();
            _panelResultados.Controls.Clear();

            const int ITEM_HEIGHT = 45;
            int totalHeight = 0;

            // Opción "TODOS" siempre al principio si no hay búsqueda específica o si coincide
            if (string.IsNullOrEmpty(query) || "todos los materiales".Contains(query.ToLower()))
            {
                var panelTodos = CrearItemResultado(null); // null significa "Todos"
                _panelResultados.Controls.Add(panelTodos);
                totalHeight += ITEM_HEIGHT;
            }

            foreach (var m in resultados)
            {
                var panelProp = CrearItemResultado(m);
                _panelResultados.Controls.Add(panelProp);
                totalHeight += ITEM_HEIGHT;
            }

            _panelResultados.Height = Math.Min(totalHeight + 2, 300);
            PosicionarPanelResultados();
            _panelResultados.Visible = totalHeight > 0;
            _panelResultados.BringToFront();
            _panelResultados.ResumeLayout();
        }

        private Panel CrearItemResultado(Material m)
        {
            var panel = new Panel
            {
                Size = new Size(_panelResultados.Width - 20, 45),
                Dock = DockStyle.Top,
                Cursor = Cursors.Hand,
                Padding = new Padding(10, 0, 0, 0),
                BackColor = Color.FromArgb(25, 30, 28)
            };

            var lbl = new Label
            {
                Text = m == null ? "--- TODOS LOS MATERIALES ---" : m.nombre,
                Font = new Font("Bahnschrift Light", 11f),
                ForeColor = m == null ? Color.FromArgb(39, 174, 96) : Color.White,
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Cursor = Cursors.Hand
            };

            var lblCat = new Label
            {
                Text = m == null ? "" : m.nombre_categoria,
                Font = new Font("Bahnschrift Light", 8f),
                ForeColor = Color.DarkGray,
                AutoSize = false,
                Width = 120,
                Dock = DockStyle.Right,
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(0, 0, 10, 0),
                Cursor = Cursors.Hand
            };

            // Eventos de Hover y Click
            Control[] ctrls = { panel, lbl, lblCat };
            foreach (var c in ctrls)
            {
                c.MouseEnter += (s, e) => panel.BackColor = Color.FromArgb(40, 50, 45);
                c.MouseLeave += (s, e) => panel.BackColor = Color.FromArgb(25, 30, 28);
                c.Click += (s, e) =>
                {
                    SeleccionarMaterial(m);
                };
            }

            panel.Controls.Add(lbl);
            if (m != null) panel.Controls.Add(lblCat);
            return panel;
        }

        private void SeleccionarMaterial(Material m)
        {
            _materialSeleccionado = m;
            txtBuscadorMaterial.Text = m == null ? "" : m.nombre;
            OcultarResultados();
            CargarKardex();
        }

        private void OcultarResultados()
        {
            if (_panelResultados != null) _panelResultados.Visible = false;
        }

        /// <summary>Creates a full-height fill Panel that centers the given control vertically and horizontally.</summary>
        private Panel CenterPanel(Control ctrl)
        {
            Panel p = new Panel();
            p.Dock = DockStyle.Fill;
            p.BackColor = Color.Transparent;
            p.Padding = new Padding(0);
            ctrl.Anchor = AnchorStyles.None;
            p.Controls.Add(ctrl);
            p.Resize += (s, e) =>
            {
                ctrl.Left = (p.ClientSize.Width - ctrl.Width) / 2;
                ctrl.Top = (p.ClientSize.Height - ctrl.Height) / 2;
            };
            return p;
        }

        // Limpiar ya no es necesario como método separado:
        // basta con destildar los toggles, que disparan CargarKardex automáticamente.

        private void BtnDescargarPDF_Click(object sender, EventArgs e)
        {
            try
            {
                if (dgvKardex.DataSource == null) { ToastNotification.ShowWarning("No hay datos para exportar."); return; }
                List<KardexItem> historial = ((SortableBindingList<KardexItem>)dgvKardex.DataSource).ToList();
                if (historial.Count == 0) { ToastNotification.ShowWarning("No hay registros en el filtrado actual."); return; }

                string tempPath = Path.Combine(Path.GetTempPath(), $"Kardex_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
                string materialName = lblMaterialNombre.Text;
                bool mostrarMaterial = (dgvKardex.Columns["nombre_material"] != null && dgvKardex.Columns["nombre_material"].Visible);
                bool mostrarSaldo = !SessionManager.DebeOcultarCantidadesStockMateriales;
                DateTime pdfInicio = chkDesde.Checked ? dtpInicio.Value.Date
                    : (historial.Count > 0 ? historial.Min(h => h.fecha).Date : DateTime.Today);
                DateTime pdfFin = chkHasta.Checked ? dtpFin.Value.Date
                    : (historial.Count > 0 ? historial.Max(h => h.fecha).Date : DateTime.Today);
                WP_Inventario.Reportes.KardexPDF.Generar(tempPath, historial, materialName, pdfInicio, pdfFin, mostrarMaterial, mostrarSaldo);
                Process.Start(new ProcessStartInfo(tempPath) { UseShellExecute = true });
            }
            catch (Exception ex) { ToastNotification.ShowError("Error al generar PDF: " + ex.Message); }
        }

        private void BtnDescargarExcel_Click(object sender, EventArgs e)
        {
            try
            {
                if (dgvKardex.DataSource == null) { ToastNotification.ShowWarning("No hay datos para exportar."); return; }
                List<KardexItem> historial = ((SortableBindingList<KardexItem>)dgvKardex.DataSource).ToList();
                if (historial.Count == 0) { ToastNotification.ShowWarning("No hay registros en el filtrado actual."); return; }

                string tempPath = Path.Combine(Path.GetTempPath(), $"Kardex_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
                bool mostrarSaldo = !SessionManager.DebeOcultarCantidadesStockMateriales;

                using (var pkg = new OfficeOpenXml.ExcelPackage())
                {
                    var ws = pkg.Workbook.Worksheets.Add("Kardex");
                    int totalColumnas = mostrarSaldo ? 8 : 7;

                    // ----- Header title row -----
                    ws.Cells[1, 1].Value = "KARDEX DE MATERIALES";
                    ws.Cells[1, 1, 1, totalColumnas].Merge = true;
                    ws.Cells[1, 1].Style.Font.Bold = true;
                    ws.Cells[1, 1].Style.Font.Size = 16;
                    ws.Cells[1, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    ws.Cells[1, 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    ws.Cells[1, 1].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(35, 63, 43));
                    ws.Cells[1, 1].Style.Font.Color.SetColor(Color.White);

                    ws.Cells[2, 1].Value = "Material: " + lblMaterialNombre.Text;
                    ws.Cells[2, 1, 2, totalColumnas].Merge = true;
                    ws.Cells[2, 1].Style.Font.Italic = true;
                    ws.Cells[2, 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    ws.Cells[2, 1].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(47, 84, 57));
                    ws.Cells[2, 1].Style.Font.Color.SetColor(Color.White);

                    // ----- Column headers -----
                    string[] headers = mostrarSaldo
                        ? new[] { "Fecha", "Material", "Movimiento", "Origen", "Destino", "Cantidad", "Stock Resultante", "ID Movimiento" }
                        : new[] { "Fecha", "Material", "Movimiento", "Origen", "Destino", "Cantidad", "ID Movimiento" };
                    for (int c = 0; c < headers.Length; c++)
                    {
                        var cell = ws.Cells[3, c + 1];
                        cell.Value = headers[c];
                        cell.Style.Font.Bold = true;
                        cell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(28, 50, 35));
                        cell.Style.Font.Color.SetColor(Color.White);
                        cell.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    }

                    // ----- Data rows -----
                    bool mostrarMat = (dgvKardex.Columns["nombre_material"] != null && dgvKardex.Columns["nombre_material"].Visible);
                    for (int r = 0; r < historial.Count; r++)
                    {
                        var item = historial[r];
                        int row = r + 4;
                        ws.Cells[row, 1].Value = item.fecha;
                        ws.Cells[row, 1].Style.Numberformat.Format = "dd/MM/yyyy";
                        ws.Cells[row, 2].Value = mostrarMat ? item.nombre_material : "-";
                        ws.Cells[row, 3].Value = item.tipo_movimiento;
                        ws.Cells[row, 4].Value = item.origen;
                        ws.Cells[row, 5].Value = item.destino;
                        ws.Cells[row, 6].Value = item.cantidad;
                        if (mostrarSaldo)
                        {
                            ws.Cells[row, 7].Value = item.saldo;
                            ws.Cells[row, 8].Value = item.id_movimiento;
                        }
                        else
                        {
                            ws.Cells[row, 7].Value = item.id_movimiento;
                        }

                        // Alternate row color
                        if (r % 2 == 0)
                        {
                            ws.Cells[row, 1, row, totalColumnas].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                            ws.Cells[row, 1, row, totalColumnas].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(28, 40, 32));
                            ws.Cells[row, 1, row, totalColumnas].Style.Font.Color.SetColor(Color.White);
                        }
                        else
                        {
                            ws.Cells[row, 1, row, totalColumnas].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                            ws.Cells[row, 1, row, totalColumnas].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(35, 50, 40));
                            ws.Cells[row, 1, row, totalColumnas].Style.Font.Color.SetColor(Color.White);
                        }
                    }

                    ws.Cells[ws.Dimension.Address].AutoFitColumns();
                    pkg.SaveAs(new System.IO.FileInfo(tempPath));
                }

                Process.Start(new ProcessStartInfo(tempPath) { UseShellExecute = true });
                ToastNotification.ShowSuccess("Excel generado correctamente.");
            }
            catch (Exception ex) { ToastNotification.ShowError("Error al generar Excel: " + ex.Message); }
        }

        private void Vista_Kardex_Load(object sender, EventArgs e)
        {
            // Default load all
            CargarKardex();
        }

        public void Refrescar()
        {
            CargarKardex();
        }

        private void ConfigurarGrid()
        {
             dgvKardex.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvKardex.MultiSelect = false;
            dgvKardex.ReadOnly = true;
            dgvKardex.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvKardex.AllowUserToResizeColumns = false;
            dgvKardex.AllowUserToResizeRows = false;
            dgvKardex.EnableHeadersVisualStyles = false;
            dgvKardex.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgvKardex.ColumnHeadersHeight = 45; 
            dgvKardex.RowTemplate.Height = 45; 
            dgvKardex.ThemeStyle.HeaderStyle.Height = 45;
            dgvKardex.ThemeStyle.RowsStyle.Height = 45;

            // Define Green Colors
            Color headerColor = Color.FromArgb(35, 63, 43); // Dark Green Header
            Color rowColor1 = Color.FromArgb(28, 40, 32); // Very Dark Green/Gray
            Color rowColor2 = Color.FromArgb(35, 50, 40); // Slightly lighter Green
            Color selectionColor = Color.FromArgb(50, 90, 60); // Highlight Green

            // Header Style
            dgvKardex.ColumnHeadersDefaultCellStyle.BackColor = headerColor;
            dgvKardex.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvKardex.ColumnHeadersDefaultCellStyle.SelectionBackColor = headerColor;
            dgvKardex.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
            dgvKardex.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dgvKardex.ColumnHeadersDefaultCellStyle.Font = new Font("Bahnschrift", 11, FontStyle.Bold); 

            // Row Styles
            dgvKardex.BackgroundColor = Color.FromArgb(20, 30, 25); // Main background
            
            dgvKardex.DefaultCellStyle.BackColor = rowColor1;
            dgvKardex.DefaultCellStyle.ForeColor = Color.White;
            dgvKardex.DefaultCellStyle.Font = new Font("Bahnschrift Light", 11);
            dgvKardex.DefaultCellStyle.SelectionBackColor = selectionColor; 
            dgvKardex.DefaultCellStyle.SelectionForeColor = Color.White;
            dgvKardex.DefaultCellStyle.Padding = new Padding(5); 
            dgvKardex.GridColor = Color.FromArgb(40, 60, 50);

            dgvKardex.AlternatingRowsDefaultCellStyle.BackColor = rowColor2;
            dgvKardex.AlternatingRowsDefaultCellStyle.ForeColor = Color.White;
            dgvKardex.AlternatingRowsDefaultCellStyle.SelectionBackColor = selectionColor;
            dgvKardex.AlternatingRowsDefaultCellStyle.SelectionForeColor = Color.White;

            // Theme properties for Guna Grid (often overrides standard styles if not set carefully)
            dgvKardex.ThemeStyle.BackColor = Color.FromArgb(20, 30, 25);
            dgvKardex.ThemeStyle.GridColor = Color.FromArgb(40, 60, 50);
            dgvKardex.ThemeStyle.HeaderStyle.BackColor = headerColor;
            dgvKardex.ThemeStyle.HeaderStyle.ForeColor = Color.White;
            dgvKardex.ThemeStyle.RowsStyle.BackColor = rowColor1;
            dgvKardex.ThemeStyle.RowsStyle.ForeColor = Color.White;
            dgvKardex.ThemeStyle.RowsStyle.SelectionBackColor = selectionColor;
            dgvKardex.ThemeStyle.RowsStyle.SelectionForeColor = Color.White;
            dgvKardex.ThemeStyle.AlternatingRowsStyle.BackColor = rowColor2;

            // Add Delete Button Column
            if (dgvKardex.Columns["btnEliminar"] == null)
            {
                DataGridViewButtonColumn btnEliminar = new DataGridViewButtonColumn();
                btnEliminar.Name = "btnEliminar";
                btnEliminar.HeaderText = "Acciones";
                btnEliminar.Text = "Eliminar";
                btnEliminar.UseColumnTextForButtonValue = true;
                btnEliminar.FillWeight = 15;
                btnEliminar.FlatStyle = FlatStyle.Flat;
                // Style for the button cell
                btnEliminar.DefaultCellStyle.BackColor = Color.FromArgb(35, 63, 43); // Match header/theme
                btnEliminar.DefaultCellStyle.ForeColor = Color.White;
                btnEliminar.DefaultCellStyle.SelectionBackColor = selectionColor;
                btnEliminar.DefaultCellStyle.SelectionForeColor = Color.White;
                
                dgvKardex.Columns.Add(btnEliminar);
            }

            // Important: Wire up the event
            dgvKardex.CellContentClick += DgvKardex_CellContentClick;
        }

        private void DgvKardex_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && dgvKardex.Columns[e.ColumnIndex].Name == "btnEliminar")
            {
                if (MessageBox.Show("¿Está seguro de eliminar este movimiento? Esto revertirá el stock.", "Confirmar Eliminación", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    try
                    {
                        var item = (KardexItem)dgvKardex.Rows[e.RowIndex].DataBoundItem;
                        MovimientoDatos datos = new MovimientoDatos();
                        datos.EliminarMovimiento(item.id_movimiento, item.id_material, item.cantidad, item.tipo_movimiento);
                        
                        ToastNotification.ShowSuccess("Movimiento eliminado correctamente.");
                        CargarKardex(); // Refresh
                    }
                    catch (Exception ex)
                    {
                        ToastNotification.ShowError("Error al eliminar: " + ex.Message);
                    }
                }
            }
        }

        // BtnFiltrar_Click eliminado: los toggles y DatePickers disparan CargarKardex directamente.

        private void CargarMateriales()
        {
            try
            {
                MaterialDatos datos = new MaterialDatos();
                _materialesFull = datos.obtenerMateriales();
            }
            catch (Exception ex)
            {
                ToastNotification.ShowError("Error cargando materiales: " + ex.Message);
            }
        }

        // Ya no usamos este evento, el buscador maneja la selección
        // private void cmbMateriales_SelectedIndexChanged(object sender, EventArgs e) { ... }

        private void CargarKardex(int? idMaterialParam = null) 
        {
            try
            {
                int? idMaterial = _materialSeleccionado?.id;
                lblMaterialNombre.Text = _materialSeleccionado != null ? _materialSeleccionado.nombre : "Todos los Materiales";

                // Apply date filters only if the respective checkbox is checked
                DateTime? inicio = chkDesde.Checked ? (DateTime?)dtpInicio.Value.Date : null;
                DateTime? fin = chkHasta.Checked ? (DateTime?)dtpFin.Value.Date.AddDays(1).AddTicks(-1) : null;

                MovimientoDatos datos = new MovimientoDatos();
                List<KardexItem> historial = datos.ObtenerKardex(idMaterial, inicio ?? DateTime.MinValue, fin ?? DateTime.MaxValue);
                
                dgvKardex.DataSource = new SortableBindingList<KardexItem>(historial);

                // Ensure columns
                if (dgvKardex.Columns["id_movimiento"] != null) dgvKardex.Columns["id_movimiento"].Visible = false;
                if (dgvKardex.Columns["id_material"] != null) dgvKardex.Columns["id_material"].Visible = false;

                if (dgvKardex.Columns["fecha"] != null)
                {
                    dgvKardex.Columns["fecha"].HeaderText = "Fecha";
                    dgvKardex.Columns["fecha"].DefaultCellStyle.Format = "dd/MM/yyyy";
                    dgvKardex.Columns["fecha"].FillWeight = 15;
                    dgvKardex.Columns["fecha"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    dgvKardex.Columns["fecha"].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }
                if (dgvKardex.Columns["tipo_movimiento"] != null)
                {
                    dgvKardex.Columns["tipo_movimiento"].HeaderText = "Movimiento";
                    dgvKardex.Columns["tipo_movimiento"].FillWeight = 20;
                }
                if (dgvKardex.Columns["cantidad"] != null)
                {
                    dgvKardex.Columns["cantidad"].HeaderText = "Cantidad";
                    dgvKardex.Columns["cantidad"].FillWeight = 10;
                    dgvKardex.Columns["cantidad"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    dgvKardex.Columns["cantidad"].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    dgvKardex.Columns["cantidad"].DefaultCellStyle.Format = "G29";
                }
                if (dgvKardex.Columns["origen"] != null)
                {
                    dgvKardex.Columns["origen"].HeaderText = "Origen";
                    dgvKardex.Columns["origen"].FillWeight = 20;
                }
                if (dgvKardex.Columns["destino"] != null)
                {
                    dgvKardex.Columns["destino"].HeaderText = "Destino";
                    dgvKardex.Columns["destino"].FillWeight = 20;
                }
                if (dgvKardex.Columns["saldo"] != null)
                {
                    dgvKardex.Columns["saldo"].Visible = !SessionManager.DebeOcultarCantidadesStockMateriales;
                    dgvKardex.Columns["saldo"].HeaderText = "Stock Resultante";
                    dgvKardex.Columns["saldo"].FillWeight = 15;
                    dgvKardex.Columns["saldo"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    dgvKardex.Columns["saldo"].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    dgvKardex.Columns["saldo"].DefaultCellStyle.Font = new Font("Bahnschrift", 11, FontStyle.Bold);
                    dgvKardex.Columns["saldo"].DefaultCellStyle.Format = "N2";
                }
                
                if (dgvKardex.Columns["nombre_material"] != null)
                {
                    dgvKardex.Columns["nombre_material"].HeaderText = "Material";
                    dgvKardex.Columns["nombre_material"].FillWeight = 25;
                    dgvKardex.Columns["nombre_material"].Visible = !idMaterial.HasValue; // Hide if a specific material is selected
                    dgvKardex.Columns["nombre_material"].DisplayIndex = 1; // Put it after Date
                }
                
                // Re-order delete column to be last
                if (dgvKardex.Columns["btnEliminar"] != null)
                {
                    dgvKardex.Columns["btnEliminar"].DisplayIndex = dgvKardex.Columns.Count - 1;
                }
            }
            catch (Exception ex)
            {
                 ToastNotification.ShowError("Error al cargar historial: " + ex.Message);
            }
        }
    }
}
