using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CancellationTokenSource = System.Threading.CancellationTokenSource;
using SteeLog.CompositionRoot;
using SteeLog.Core.Application.Reportes;
using SteeLog.Presentation;

using SteeLog;
using SteeLog.Servicios;
using SteeLog.UI;
using SteeLog.Vistas.Inventario;
using System.IO;
using System.Diagnostics;

namespace SteeLog.Vistas
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
        private List<KardexMaterial> _materialesFull = new List<KardexMaterial>();
        private KardexMaterial _materialSeleccionado = null;

        private KardexPresenter presenter;
        private readonly IServiciosVista servicios;
        private Func<object> identidadActual;
        private Func<bool> ocultarSaldo;
        private Action<string> abrirPdf = ruta => Process.Start(new ProcessStartInfo(ruta) { UseShellExecute = true });
        private Action<string> avisarPdfFallido = mensaje => ToastNotification.ShowError(mensaje);
        private readonly CancellationTokenSource vida = new CancellationTokenSource();
        private CancellationTokenSource carga;
        private int version;
        private object identidadDatos;
        private bool exportando;
        private bool eliminando;
        private Func<bool> confirmarEliminacion = () => Dialogo.Confirmar("¿Seguro que querés eliminar este movimiento?\nEsto revertirá el stock.", "Confirmar eliminación", true);
        private Action<string> avisarEliminacionExitosa = mensaje => ToastNotification.ShowSuccess(mensaje);
        private Action<string> avisarEliminacionFallida = mensaje => ToastNotification.ShowError(mensaje);
        private void CancelarOperaciones()
        {
            if (!vida.IsCancellationRequested) vida.Cancel();
            if (carga != null) carga.Cancel();
            _timerDebounce?.Stop();
        }

        public Vista_Kardex(IServiciosVista servicios)
        {
            this.servicios = servicios ?? throw new ArgumentNullException(nameof(servicios));
            identidadActual = () => this.servicios.Sesion.Email;
            ocultarSaldo = () => this.servicios.Sesion.DebeOcultarCantidadesStockMateriales;
            InitializeComponent();
            Tema.SinEscala(this);
            InitializeCustomControls();
            this.Load += Vista_Kardex_Load;
            ConfigurarGrid();
            FormClosed += (s, e) => CancelarOperaciones();
        }

        internal Vista_Kardex(IServiciosVista servicios, KardexPresenter presenter, Func<object> identidadActual, Func<bool> ocultarSaldo, Action<string> abrirPdf) : this(servicios)
        {
            this.presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
            this.identidadActual = identidadActual ?? throw new ArgumentNullException(nameof(identidadActual));
            this.ocultarSaldo = ocultarSaldo ?? throw new ArgumentNullException(nameof(ocultarSaldo));
            this.abrirPdf = abrirPdf ?? throw new ArgumentNullException(nameof(abrirPdf));
        }

        private void InitializeCustomControls()
        {
            BackColor = Tema.Fondo;

            // --- DateTimePickers ---
            dtpInicio = new Guna.UI2.WinForms.Guna2DateTimePicker();
            dtpInicio.Size = new Size(138, Tema.AltoControl);
            dtpInicio.Format = DateTimePickerFormat.Short;
            dtpInicio.Value = DateTime.Now.AddMonths(-1);
            UiVista.AtenuarFecha(dtpInicio, false);
            Tema.Fecha(dtpInicio); UiVista.AtenuarFecha(dtpInicio, false);

            dtpFin = new Guna.UI2.WinForms.Guna2DateTimePicker();
            dtpFin.Size = new Size(138, Tema.AltoControl);
            dtpFin.Format = DateTimePickerFormat.Short;
            dtpFin.Value = DateTime.Now;
            UiVista.AtenuarFecha(dtpFin, false);
            Tema.Fecha(dtpFin); UiVista.AtenuarFecha(dtpFin, false);

            // --- Toggle Switches ---
            chkDesde = new Guna.UI2.WinForms.Guna2ToggleSwitch();
            chkDesde.Checked = false;
            chkDesde.Size = new Size(46, 24);
            chkDesde.Animated = true;
            chkDesde.AutoRoundedCorners = true;
            Tema.Toggle(chkDesde);
            chkDesde.CheckedChanged += (s, e) =>
            {
                UiVista.AtenuarFecha(dtpInicio, chkDesde.Checked);
                CargarKardex();
            };
            dtpInicio.ValueChanged += (s, e) => { if (chkDesde.Checked) CargarKardex(); else chkDesde.Checked = true; };

            chkHasta = new Guna.UI2.WinForms.Guna2ToggleSwitch();
            chkHasta.Checked = false;
            chkHasta.Size = new Size(46, 24);
            chkHasta.Animated = true;
            chkHasta.AutoRoundedCorners = true;
            Tema.Toggle(chkHasta);
            chkHasta.CheckedChanged += (s, e) =>
            {
                UiVista.AtenuarFecha(dtpFin, chkHasta.Checked);
                CargarKardex();
            };
            dtpFin.ValueChanged += (s, e) => { if (chkHasta.Checked) CargarKardex(); else chkHasta.Checked = true; };

            // --- Botones de exportación ---
            btnDescargarPDF = new Guna.UI2.WinForms.Guna2Button();
            btnDescargarPDF.Text = "PDF";
            btnDescargarPDF.Size = new Size(90, Tema.AltoControl);
            btnDescargarPDF.Click += BtnDescargarPDF_Click;
            Tema.Boton(btnDescargarPDF, Variante.Secundario);

            btnDescargarExcel = new Guna.UI2.WinForms.Guna2Button();
            btnDescargarExcel.Text = "Excel";
            btnDescargarExcel.Size = new Size(90, Tema.AltoControl);
            btnDescargarExcel.Click += BtnDescargarExcel_Click;
            Tema.Boton(btnDescargarExcel, Variante.Primario);

            // --- Barra de filtros: material seleccionado a la izquierda, rango y exportación a la derecha ---
            TableLayoutPanel panelFiltros = new TableLayoutPanel();
            panelFiltros.Dock = DockStyle.Top;
            panelFiltros.Height = 64;
            panelFiltros.BackColor = Tema.Fondo;
            panelFiltros.Padding = new Padding(28, 14, 28, 6);
            panelFiltros.ColumnCount = 2;
            panelFiltros.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            panelFiltros.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            panelFiltros.RowCount = 1;
            panelFiltros.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // lblMaterialNombre alimenta PDF/Excel: se conserva, ahora como título de la selección.
            panelAntiParpadeo1.Controls.Remove(lblMaterialNombre);
            lblMaterialNombre.AutoSize = false;
            lblMaterialNombre.Dock = DockStyle.Fill;
            lblMaterialNombre.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;
            lblMaterialNombre.Font = Tema.Fuente(11f, true);
            lblMaterialNombre.ForeColor = Tema.Texto;
            lblMaterialNombre.TextAlign = ContentAlignment.MiddleLeft;
            lblMaterialNombre.AutoEllipsis = true;
            lblMaterialNombre.Margin = new Padding(0);
            panelFiltros.Controls.Add(lblMaterialNombre, 0, 0);

            var flujo = new FlowLayoutPanel
            {
                AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false,
                Dock = DockStyle.Fill, Margin = new Padding(0), BackColor = Tema.Fondo
            };
            var lblDesde = UiVista.Etiqueta("Desde", Tema.AltoControl);
            lblDesde.Margin = new Padding(0, 0, 8, 0);
            chkDesde.Margin = new Padding(0, 6, 8, 6);
            var lblHasta = UiVista.Etiqueta("Hasta", Tema.AltoControl);
            lblHasta.Margin = new Padding(20, 0, 8, 0);
            chkHasta.Margin = new Padding(0, 6, 8, 6);
            dtpInicio.Margin = new Padding(0);
            dtpFin.Margin = new Padding(0);
            btnDescargarPDF.Margin = new Padding(24, 0, 0, 0);
            btnDescargarExcel.Margin = new Padding(8, 0, 0, 0);
            flujo.Controls.Add(lblDesde);
            flujo.Controls.Add(chkDesde);
            flujo.Controls.Add(dtpInicio);
            flujo.Controls.Add(lblHasta);
            flujo.Controls.Add(chkHasta);
            flujo.Controls.Add(dtpFin);
            flujo.Controls.Add(btnDescargarPDF);
            flujo.Controls.Add(btnDescargarExcel);
            panelFiltros.Controls.Add(flujo, 1, 0);

            // --- Buscador de material + grilla ---
            panelAntiParpadeo1.Visible = false;
            this.Padding = Padding.Empty;
            materiales.AutoSize = false;
            materiales.CellBorderStyle = TableLayoutPanelCellBorderStyle.None;
            materiales.Dock = DockStyle.Fill;
            materiales.BackColor = Tema.Fondo;
            materiales.ForeColor = Tema.Texto;
            materiales.Padding = new Padding(28, 4, 28, 20);
            materiales.RowStyles[0] = new RowStyle(SizeType.Absolute, 52F);
            labelSelect.Font = Tema.FuenteTexto;
            labelSelect.ForeColor = Tema.TextoSecundario;
            labelSelect.Text = "Material:";
            labelSelect.Margin = new Padding(0, 0, 12, 0);
            txtBuscadorMaterial.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            txtBuscadorMaterial.Height = Tema.AltoControl;
            txtBuscadorMaterial.Margin = new Padding(0, 0, 0, 0);
            txtBuscadorMaterial.PlaceholderText = "Buscar material por nombre o categoría...";
            Tema.Input(txtBuscadorMaterial);
            txtBuscadorMaterial.Cursor = Cursors.IBeam;

            this.Controls.Add(panelFiltros);
            materiales.BringToFront();
            panelFiltros.SendToBack();
            var banda = Tema.TituloVista(this, "Kardex de materiales");
            if (banda != null) banda.SendToBack();

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
                BackColor = Tema.FondoInput,
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
            List<KardexMaterial> filtrados;

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

        private void MostrarResultados(List<KardexMaterial> resultados, string query)
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

        private Panel CrearItemResultado(KardexMaterial m)
        {
            var panel = new Panel
            {
                Size = new Size(_panelResultados.Width - 20, 45),
                Dock = DockStyle.Top,
                Cursor = Cursors.Hand,
                Padding = new Padding(10, 0, 0, 0),
                BackColor = Tema.FondoInput
            };

            var lbl = new Label
            {
                Text = m == null ? "Todos los materiales" : m.nombre,
                Font = m == null ? Tema.Fuente(10.5f, true) : Tema.FuenteTexto,
                ForeColor = m == null ? Tema.Acento : Tema.Texto,
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Cursor = Cursors.Hand
            };

            var lblCat = new Label
            {
                Text = m == null ? "" : m.nombre_categoria,
                Font = Tema.Fuente(9f),
                ForeColor = Tema.TextoSecundario,
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
                c.MouseEnter += (s, e) => panel.BackColor = Tema.Seleccion;
                c.MouseLeave += (s, e) => panel.BackColor = Tema.FondoInput;
                c.Click += (s, e) =>
                {
                    SeleccionarMaterial(m);
                };
            }

            panel.Controls.Add(lbl);
            if (m != null) panel.Controls.Add(lblCat);
            return panel;
        }

        private void SeleccionarMaterial(KardexMaterial m)
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

        // Limpiar ya no es necesario como método separado:
        // basta con destildar los toggles, que disparan CargarKardex automáticamente.

        private async void BtnDescargarPDF_Click(object sender, EventArgs e)
        {
            if (exportando || presenter == null || vida.IsCancellationRequested || identidadDatos == null || !ReferenceEquals(identidadDatos, identidadActual())) return;
            string ruta = null;
            bool abierto = false;
            int solicitud = version;
            var identidad = identidadActual();
            try
            {
                if (dgvKardex.DataSource == null) { ToastNotification.ShowWarning("No hay datos para exportar."); return; }
                var historial = ((SortableBindingList<KardexItem>)dgvKardex.DataSource).ToList();
                if (historial.Count == 0) { ToastNotification.ShowWarning("No hay registros en el filtrado actual."); return; }
                ruta = Path.Combine(Path.GetTempPath(), $"Kardex_{Guid.NewGuid():N}.pdf");
                var snapshot = new KardexSnapshot {
                    Historial = historial, MaterialNombre = lblMaterialNombre.Text,
                    MostrarColumnaMaterial = dgvKardex.Columns["nombre_material"] != null && dgvKardex.Columns["nombre_material"].Visible,
                    MostrarColumnaSaldo = !ocultarSaldo(),
                    FechaInicio = chkDesde.Checked ? dtpInicio.Value.Date : historial.Min(h => h.fecha).Date,
                    FechaFin = chkHasta.Checked ? dtpFin.Value.Date : historial.Max(h => h.fecha).Date
                };
                exportando = true; btnDescargarPDF.Enabled = false;
                await presenter.GenerarAsync(ruta, snapshot, vida.Token);
                if (!vida.IsCancellationRequested && !IsDisposed && solicitud == version && ReferenceEquals(identidad, identidadActual()))
                {
                    abrirPdf(ruta);
                    abierto = true;
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { if (ContextoVigente(solicitud, identidad)) avisarPdfFallido("Error al generar PDF: " + ex.Message); }
            finally { if (!abierto && ruta != null) { try { if (File.Exists(ruta)) File.Delete(ruta); } catch (IOException) { } catch (UnauthorizedAccessException) { } } exportando = false; if (!IsDisposed && !vida.IsCancellationRequested) btnDescargarPDF.Enabled = true; }
        }

        private void BtnDescargarExcel_Click(object sender, EventArgs e)
        {
            if (identidadDatos == null || !ReferenceEquals(identidadDatos, identidadActual())) return;
            try
            {
                if (dgvKardex.DataSource == null) { ToastNotification.ShowWarning("No hay datos para exportar."); return; }
                List<KardexItem> historial = ((SortableBindingList<KardexItem>)dgvKardex.DataSource).ToList();
                if (historial.Count == 0) { ToastNotification.ShowWarning("No hay registros en el filtrado actual."); return; }

                string tempPath = Path.Combine(Path.GetTempPath(), $"Kardex_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
                bool mostrarSaldo = !ocultarSaldo();

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
            if (presenter == null) presenter = new KardexPresenter(servicios.Executor, servicios.KardexPdf);
            CargarMateriales();
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
            dgvKardex.RowTemplate.DefaultCellStyle.Font = Tema.FuenteTexto;
            Tema.Grilla(dgvKardex);

            // Add Delete Button Column
            if (dgvKardex.Columns["btnEliminar"] == null)
            {
                DataGridViewButtonColumn btnEliminar = new DataGridViewButtonColumn();
                btnEliminar.Name = "btnEliminar";
                btnEliminar.HeaderText = "Acciones";
                btnEliminar.Text = "Eliminar";
                btnEliminar.UseColumnTextForButtonValue = true;
                btnEliminar.FillWeight = 15;
                btnEliminar.Tag = Variante.Peligro;
                dgvKardex.Columns.Add(btnEliminar);
            }

            // Important: Wire up the event
            dgvKardex.CellContentClick += DgvKardex_CellContentClick;
            dgvKardex.CellFormatting += DgvKardex_CellFormatting;
        }

        // Sólo presentación: los tipos de movimiento vienen de la base sin tildes.
        private void DgvKardex_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || dgvKardex.Columns[e.ColumnIndex].Name != "tipo_movimiento") return;
            var texto = e.Value as string;
            if (string.IsNullOrEmpty(texto)) return;
            string visible = texto.Replace("Devolucion", "Devolución").Replace("Reparacion", "Reparación").Replace("Envio", "Envío");
            if (!ReferenceEquals(visible, texto) && visible != texto) { e.Value = visible; e.FormattingApplied = true; }
        }

        private async void DgvKardex_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || dgvKardex.Columns[e.ColumnIndex].Name != "btnEliminar") return;
            var item = dgvKardex.Rows[e.RowIndex].DataBoundItem as KardexItem;
            if (item != null) await EliminarAsync(item);
        }

        private async Task EliminarAsync(KardexItem item)
        {
            if (eliminando || presenter == null || vida.IsCancellationRequested || IsDisposed || identidadDatos == null || !ReferenceEquals(identidadDatos, identidadActual())) return;
            eliminando = true;
            int solicitud = version;
            object identidad = identidadActual();
            int id = item.id_movimiento;
            try
            {
                if (!confirmarEliminacion() || !ContextoVigente(solicitud, identidad)) return;
                await presenter.EliminarAsync(id, vida.Token);
                if (!ContextoVigente(solicitud, identidad)) return;
                avisarEliminacionExitosa("Movimiento eliminado correctamente.");
                CargarKardex();
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                if (ContextoVigente(solicitud, identidad)) avisarEliminacionFallida("Error al eliminar: " + ex.Message);
            }
            finally { eliminando = false; }
        }

        private bool ContextoVigente(int solicitud, object identidad)
        {
            return !vida.IsCancellationRequested && !IsDisposed && solicitud == version && ReferenceEquals(identidad, identidadActual());
        }
        // BtnFiltrar_Click eliminado: los toggles y DatePickers disparan CargarKardex directamente.

        private async void CargarMateriales()
        {
            var identidad = identidadActual();
            try { var materiales = await presenter.MaterialesAsync(vida.Token); if (!vida.IsCancellationRequested && !IsDisposed && ReferenceEquals(identidad, identidadActual())) _materialesFull = materiales; }
            catch (OperationCanceledException) { }
            catch (Exception ex) { if (!vida.IsCancellationRequested && !IsDisposed && ReferenceEquals(identidad, identidadActual())) ToastNotification.ShowError("Error cargando materiales: " + ex.Message); }
        }

        // Ya no usamos este evento, el buscador maneja la selección
        // private void cmbMateriales_SelectedIndexChanged(object sender, EventArgs e) { ... }

        private async void CargarKardex(int? idMaterialParam = null)
        {
            if (presenter == null || vida.IsCancellationRequested) return;
            if (carga != null) { carga.Cancel(); carga.Dispose(); }
            carga = CancellationTokenSource.CreateLinkedTokenSource(vida.Token);
            var token = carga.Token;
            int solicitud = ++version;
            dgvKardex.DataSource = null;
            identidadDatos = null;
            var identidad = identidadActual();
            try
            {
                int? idMaterial = _materialSeleccionado?.id;
                lblMaterialNombre.Text = _materialSeleccionado != null ? _materialSeleccionado.nombre : "Todos los Materiales";

                // Apply date filters only if the respective checkbox is checked
                DateTime? inicio = chkDesde.Checked ? (DateTime?)dtpInicio.Value.Date : null;
                DateTime? fin = chkHasta.Checked ? (DateTime?)dtpFin.Value.Date.AddDays(1).AddTicks(-1) : null;

                List<KardexItem> historial = await presenter.HistorialAsync(idMaterial, inicio ?? DateTime.MinValue, fin ?? DateTime.MaxValue, token);
                if (token.IsCancellationRequested || IsDisposed || solicitud != version || !ReferenceEquals(identidad, identidadActual())) return;
                
                dgvKardex.DataSource = new SortableBindingList<KardexItem>(historial);
                identidadDatos = identidad;

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
                    dgvKardex.Columns["saldo"].Visible = !ocultarSaldo();
                    dgvKardex.Columns["saldo"].HeaderText = "Stock Resultante";
                    dgvKardex.Columns["saldo"].FillWeight = 15;
                    dgvKardex.Columns["saldo"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    dgvKardex.Columns["saldo"].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    dgvKardex.Columns["saldo"].DefaultCellStyle.Font = Tema.Fuente(10.5f, true);
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
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                 if (!token.IsCancellationRequested && solicitud == version && !IsDisposed && ReferenceEquals(identidad, identidadActual())) ToastNotification.ShowError("Error al cargar historial: " + ex.Message);
            }
        }
    }
}
