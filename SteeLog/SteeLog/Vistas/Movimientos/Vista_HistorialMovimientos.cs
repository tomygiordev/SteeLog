using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using System.Threading;
using System.ComponentModel;
using SteeLog.Presentation;
using Guna.UI2.WinForms;
using SteeLog.Modelos;
using SteeLog.Servicios;
using SteeLog;
using SteeLog.UI;
using SteeLog.Vistas.Movimientos.Ordenes;

namespace SteeLog.Vistas.Movimientos
{
    public partial class Vista_HistorialMovimientos : Form, IMessageFilter, IRefrescable, IMovimientosVista, IMovimientosResultadoIncierto
    {
        private readonly IServiciosVista servicios;
        private readonly CancellationTokenSource cierre = new CancellationTokenSource();
        private MovimientosPresenter presenter;
        private MovimientosPresenter Presenter => presenter ?? (presenter = servicios.CrearMovimientosPresenter(this));
        public void SetOcupada(bool ocupada) { if (!IsDisposed && dgvHistorial != null) dgvHistorial.Enabled = !ocupada; }
        public void MostrarError(Exception error) { if (!IsDisposed) ToastNotification.ShowError(error.Message); }
        private bool resultadoIncierto;
        private int versionHistorialAlMarcarIncierto;
        internal Action<string> AdvertirResultadoIncierto = mensaje => ToastNotification.ShowWarning(mensaje);
        public void MarcarResultadoIncierto()
        {
            // La eliminación pudo haberse confirmado: no permitir otra hasta releer el historial con éxito.
            resultadoIncierto = true; versionHistorialAlMarcarIncierto = versionHistorial;
            if (!IsDisposed && !cierre.IsCancellationRequested)
                AdvertirResultadoIncierto("Resultado incierto: la eliminación pudo haberse realizado. Verifique el historial antes de reintentar. Pulse F5 para recargar.");
        }
        private List<HistorialMovimiento> rawHistorial = new List<HistorialMovimiento>();
        private List<MovimientoGrupo> listaGrupos = new List<MovimientoGrupo>();
        private Guna.UI2.WinForms.Guna2DataGridView dgvDetalles;
        private Label lblDetalles;
        private CheckTema chkDesde;
        private CheckTema chkHasta;
        private decimal precioUnitarioAnterior;

        public Vista_HistorialMovimientos(IServiciosVista servicios)
        {
            this.servicios = servicios ?? throw new ArgumentNullException(nameof(servicios));

            InitializeComponent();
            Tema.SinEscala(this);
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
            Application.AddMessageFilter(this);
            FormClosed += (s, e) => CerrarRecursos();
            Disposed += (s, e) => CerrarRecursos();
            this.VisibleChanged += (s, e) => { if (this.Visible) BuscarHistorial(); };
            
            ConfigurarUIAdicional();
            ConfigurarFechas();
            ConfigurarGrid();
            Load += (s, e) => AplicarTemaHistorial();
            Load += (s, e) => { CargarTiposMovimiento(); CargarHistorial(); };
        }

        private bool recursosCerrados;
        private void CerrarRecursos()
        {
            if (recursosCerrados) return;
            recursosCerrados = true;
            cierre.Cancel(); cargaHistorial?.Cancel();
            Application.RemoveMessageFilter(this);
        }

        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg == 0x0100) // WM_KEYDOWN
            {
                Keys key = (Keys)m.WParam.ToInt32();
                if (key == Keys.F5 && this.Visible)
                {
                    BuscarHistorial();
                    return true;
                }
            }
            return false;
        }

        private void ConfigurarUIAdicional()
        {
            // Modify TableLayoutPanel 'materiales' layout
            materiales.RowCount = 4;
            materiales.RowStyles.Clear();
            materiales.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            materiales.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            materiales.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            materiales.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

            lblDetalles = new Label();
            lblDetalles.Text = "MATERIALES DEL MOVIMIENTO SELECCIONADO";
            lblDetalles.Font = Tema.FuenteSubtitulo;
            lblDetalles.ForeColor = Tema.TextoSecundario;
            lblDetalles.Padding = new Padding(4, 0, 0, 0);
            lblDetalles.Dock = DockStyle.Fill;
            lblDetalles.TextAlign = ContentAlignment.MiddleLeft;

            dgvDetalles = new Guna.UI2.WinForms.Guna2DataGridView();
            dgvDetalles.AllowUserToAddRows = false;
            dgvDetalles.AllowUserToDeleteRows = false;
            dgvDetalles.ReadOnly = true;
            dgvDetalles.RowHeadersVisible = false;
            dgvDetalles.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvDetalles.Dock = DockStyle.Fill;
            dgvDetalles.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            dgvHistorial.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            materiales.Controls.Add(lblDetalles, 0, 2);
            materiales.SetColumnSpan(lblDetalles, 8);
            materiales.Controls.Add(dgvDetalles, 0, 3);
            materiales.SetColumnSpan(dgvDetalles, 8);

            dgvDetalles.AutoGenerateColumns = false;
            bool esAdmin = servicios.Sesion.Role == UserRole.Admin;
            dgvDetalles.ReadOnly = !esAdmin;
            dgvDetalles.EditMode = DataGridViewEditMode.EditOnEnter;
            
            dgvDetalles.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDetMaterial", HeaderText = "Material", DataPropertyName = "material", FillWeight = esAdmin ? 44 : 60 });
            dgvDetalles.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDetTipo", HeaderText = "Tipo", DataPropertyName = "esrecorte", FillWeight = esAdmin ? 14 : 20 });
            dgvDetalles.Columns["colDetTipo"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvDetalles.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDetCantidad", HeaderText = "Cantidad", DataPropertyName = "cantidad", FillWeight = esAdmin ? 14 : 20 });
            dgvDetalles.Columns["colDetCantidad"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            if (esAdmin)
            {
                var colPrecio = new DataGridViewTextBoxColumn
                {
                    Name = "colDetPrecio",
                    HeaderText = "Precio U.",
                    DataPropertyName = "precio_unitario",
                    FillWeight = 14,
                    ReadOnly = false,
                    ToolTipText = "Editable únicamente para compras eventuales."
                };
                colPrecio.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                colPrecio.DefaultCellStyle.Format = "C2";
                dgvDetalles.Columns.Add(colPrecio);

                var colTotal = new DataGridViewTextBoxColumn { Name = "colDetTotal", HeaderText = "Total", DataPropertyName = "total", FillWeight = 14 };
                colTotal.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                colTotal.DefaultCellStyle.Format = "C2";
                dgvDetalles.Columns.Add(colTotal);
            }

            foreach (DataGridViewColumn columna in dgvDetalles.Columns)
                columna.ReadOnly = columna.Name != "colDetPrecio";

            dgvDetalles.CellFormatting += DgvDetalles_CellFormatting;
            dgvDetalles.CellBeginEdit += DgvDetalles_CellBeginEdit;
            dgvDetalles.CellValidating += DgvDetalles_CellValidating;
            dgvDetalles.CellEndEdit += DgvDetalles_CellEndEdit;
            dgvHistorial.SelectionChanged += DgvHistorial_SelectionChanged;
            txtBuscarMaterial.PlaceholderText = "Remito";
            txtBuscarMaterial.TextChanged += btnBuscar_Click;
            
            // Filtros en tiempo real
            cmbTipoMov.SelectedIndexChanged += btnBuscar_Click;
            dtpDesde.ValueChanged += btnBuscar_Click;
            dtpHasta.ValueChanged += btnBuscar_Click;
        }

        private void DgvDetalles_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var det = dgvDetalles.Rows[e.RowIndex].DataBoundItem as HistorialMovimiento;
            if (det == null) return;

            string colName = dgvDetalles.Columns[e.ColumnIndex].Name;

            if (colName == "colDetTipo")
            {
                if (det.esrecorte)
                {
                    e.Value = "RECORTE" + (det.medida_recorte.HasValue ? $" ({det.medida_recorte.Value})" : "");
                    e.CellStyle.ForeColor = Tema.Advertencia;
                    e.CellStyle.SelectionForeColor = Tema.Advertencia;
                }
                else if (det.esinsumo)
                {
                    e.Value = "INSUMO";
                    e.CellStyle.ForeColor = Tema.Info;
                    e.CellStyle.SelectionForeColor = Tema.Info;
                }
                else if (det.es_compra_eventual)
                {
                    e.Value = "EVENTUAL";
                    e.CellStyle.ForeColor = Tema.Advertencia;
                    e.CellStyle.SelectionForeColor = Tema.Advertencia;
                }
                else
                {
                    e.Value = "—";
                    e.CellStyle.ForeColor = Tema.TextoApagado;
                    e.CellStyle.SelectionForeColor = Tema.TextoApagado;
                }
                e.FormattingApplied = true;
            }
            else if (colName == "colDetCantidad")
            {
                e.Value = det.cantidad.ToString("G29", CultureInfo.CurrentCulture);
                e.FormattingApplied = true;
            }
            else if (colName == "colDetPrecio")
            {
                e.CellStyle.ForeColor = det.es_compra_eventual
                    ? Tema.Advertencia
                    : Tema.TextoApagado;
                e.CellStyle.SelectionForeColor = e.CellStyle.ForeColor;
            }
        }

        private void DgvDetalles_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            if (e.RowIndex < 0 || dgvDetalles.Columns[e.ColumnIndex].Name != "colDetPrecio")
            {
                e.Cancel = true;
                return;
            }

            var detalle = dgvDetalles.Rows[e.RowIndex].DataBoundItem as HistorialMovimiento;
            if (detalle == null || !detalle.es_compra_eventual || !detalle.id_movimientomaterial.HasValue)
            {
                e.Cancel = true;
                return;
            }

            precioUnitarioAnterior = detalle.precio_unitario;
        }

        private void DgvDetalles_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (e.RowIndex < 0 || dgvDetalles.Columns[e.ColumnIndex].Name != "colDetPrecio")
                return;

            if (!TryParsePrecio(e.FormattedValue, out decimal precio) || precio < 0)
            {
                e.Cancel = true;
                ToastNotification.ShowWarning("Ingresá un precio unitario válido, mayor o igual a cero.");
            }
        }

        private async void DgvDetalles_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || dgvDetalles.Columns[e.ColumnIndex].Name != "colDetPrecio")
                return;

            var detalle = dgvDetalles.Rows[e.RowIndex].DataBoundItem as HistorialMovimiento;
            if (detalle == null || !detalle.id_movimientomaterial.HasValue ||
                !TryParsePrecio(dgvDetalles.Rows[e.RowIndex].Cells[e.ColumnIndex].Value, out decimal precioNuevo))
                return;

            if (precioNuevo == precioUnitarioAnterior)
                return;

            try
            {
                decimal precioAnterior = precioUnitarioAnterior;
                var resultado = await Presenter.ActualizarPrecioAsync(detalle.id_movimientomaterial.Value, precioNuevo, cierre.Token);
                if (IsDisposed || cierre.IsCancellationRequested) return;
                if (!resultado.HasValue) { detalle.precio_unitario=precioAnterior; dgvDetalles.Refresh(); return; }
                decimal precioTotal=resultado.Value;
                detalle.precio_unitario = precioNuevo;
                detalle.precio_total = precioTotal;

                var grupo = listaGrupos.FirstOrDefault(g => g.id_movimiento == detalle.id_movimiento);
                if (grupo != null)
                {
                    grupo.precio_total = precioTotal;
                    foreach (var item in grupo.Detalles)
                        item.precio_total = precioTotal;
                }

                dgvDetalles.Refresh();
                dgvHistorial.Refresh();
                ToastNotification.ShowSuccess("Precio unitario actualizado y total recalculado.");
            }
            catch (Exception ex)
            {
                detalle.precio_unitario = precioUnitarioAnterior;
                dgvDetalles.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = precioUnitarioAnterior;
                ToastNotification.ShowError("No se pudo actualizar el precio: " + ex.Message);
            }
        }

        private void DgvHistorial_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvHistorial.CurrentRow != null && dgvHistorial.CurrentRow.DataBoundItem is MovimientoGrupo grupo)
            {
                dgvDetalles.DataSource = null;
                dgvDetalles.DataSource = grupo.Detalles;
            }
            else
            {
                dgvDetalles.DataSource = null;
            }
        }

        private void ConfigurarFechas()
        {
            dtpDesde.Value = DateTime.Now.AddMonths(-1);
            dtpHasta.Value = DateTime.Now;
            UiVista.AtenuarFecha(dtpDesde, false);
            UiVista.AtenuarFecha(dtpHasta, false);
            dtpDesde.ValueChanged += (s, e) => { if (chkDesde != null && !chkDesde.Checked) chkDesde.Checked = true; };
            dtpHasta.ValueChanged += (s, e) => { if (chkHasta != null && !chkHasta.Checked) chkHasta.Checked = true; };

            // Checkbox Desde
            chkDesde = new CheckTema();
            chkDesde.Text = "Desde:";
            chkDesde.ForeColor = Tema.TextoSecundario;
            chkDesde.AutoSize = true;
            chkDesde.Anchor = AnchorStyles.Left;
            chkDesde.Checked = false;
            chkDesde.CheckedChanged += (s, e) => { UiVista.AtenuarFecha(dtpDesde, chkDesde.Checked); BuscarHistorial(); };
            materiales.Controls.Remove(lblDesde);
            materiales.Controls.Add(chkDesde, 0, 0);

            // Checkbox Hasta
            chkHasta = new CheckTema();
            chkHasta.Text = "Hasta:";
            chkHasta.ForeColor = Tema.TextoSecundario;
            chkHasta.AutoSize = true;
            chkHasta.Anchor = AnchorStyles.Left;
            chkHasta.Checked = false;
            chkHasta.CheckedChanged += (s, e) => { UiVista.AtenuarFecha(dtpHasta, chkHasta.Checked); BuscarHistorial(); };
            materiales.Controls.Remove(lblHasta);
            materiales.Controls.Add(chkHasta, 2, 0);
        }

        private async void CargarTiposMovimiento()
        {
            try
            {
                var catalogos = await Presenter.CatalogosAsync(cierre.Token);
                if (IsDisposed || cierre.IsCancellationRequested) return;
                var dt = new DataTable(); dt.Columns.Add("id_tipomovimiento", typeof(int)); dt.Columns.Add("nombre", typeof(string));
                foreach (var tipo in catalogos.Tipos) dt.Rows.Add(tipo.Id, tipo.Nombre);

                // Reordenar: primero TODOS, luego Envío a Obra, luego el resto
                DataTable dtOrdenado = dt.Clone();
                DataRow allRow = dtOrdenado.NewRow();
                allRow["nombre"] = "TODOS";
                dtOrdenado.Rows.Add(allRow);

                // Buscar "Envío a Obra" (o similar) y agregarlo primero
                DataRow envioRow = null;
                foreach (DataRow row in dt.Rows)
                {
                    string nombre = row["nombre"]?.ToString() ?? "";
                    if (nombre.IndexOf("Envío", StringComparison.OrdinalIgnoreCase) >= 0 
                        || nombre.IndexOf("Envio", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        envioRow = row;
                        break;
                    }
                }

                if (envioRow != null)
                {
                    dtOrdenado.ImportRow(envioRow);
                }

                // Agregar el resto
                foreach (DataRow row in dt.Rows)
                {
                    if (row != envioRow)
                    {
                        dtOrdenado.ImportRow(row);
                    }
                }

                UiVista.AsignarDatos(cmbTipoMov, dtOrdenado, "nombre", "nombre");
                cmbTipoMov.SelectedIndex = 0;
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                if (IsDisposed || cierre.IsCancellationRequested) return;
                Console.WriteLine("Error cargando tipos: " + ex.Message);
                cmbTipoMov.DataSource = null;
                cmbTipoMov.Items.Clear();
                cmbTipoMov.Items.Add("TODOS");
                cmbTipoMov.SelectedIndex = 0;
            }
        }

        private void AplicarTemaHistorial()
        {
            BackColor = Tema.Fondo;
            Padding = new Padding(0);

            // Banda estandar; el contador pasa a texto secundario a la derecha.
            panelTitulo.Visible = false;
            var banda = Tema.TituloVista(this, "Historial de movimientos");
            lblSubtitulo.AutoSize = false;
            lblSubtitulo.Anchor = AnchorStyles.None;
            lblSubtitulo.Dock = DockStyle.Right;
            lblSubtitulo.Width = 320;
            lblSubtitulo.Padding = new Padding(0, 0, 28, 0);
            lblSubtitulo.TextAlign = ContentAlignment.MiddleRight;
            lblSubtitulo.Font = Tema.FuenteTexto;
            lblSubtitulo.ForeColor = Tema.TextoSecundario;
            lblSubtitulo.BackColor = Color.Transparent;
            panelTitulo.Controls.Remove(lblSubtitulo);
            banda.Controls.Add(lblSubtitulo);

            materiales.AutoSize = false;
            materiales.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            materiales.Dock = DockStyle.Fill;
            materiales.BringToFront();
            materiales.CellBorderStyle = TableLayoutPanelCellBorderStyle.None;
            materiales.BackColor = Tema.Fondo;
            materiales.Padding = new Padding(16, 12, 16, 12);
            materiales.RowStyles[0] = new RowStyle(SizeType.Absolute, 52F);
            materiales.RowStyles[1] = new RowStyle(SizeType.Percent, 58F);
            materiales.RowStyles[2] = new RowStyle(SizeType.Absolute, 40F);
            materiales.RowStyles[3] = new RowStyle(SizeType.Percent, 42F);

            foreach (var chk in new[] { chkDesde, chkHasta }) { chk.Margin = new Padding(3, 3, 6, 3); }
            lblTipo.Font = Tema.FuenteTexto; lblTipo.ForeColor = Tema.TextoSecundario; lblTipo.Margin = new Padding(14, 0, 6, 0);
            foreach (var dtp in new[] { dtpDesde, dtpHasta })
            {
                Tema.Fecha(dtp);
                dtp.Size = new Size(140, Tema.AltoControl);
                dtp.Anchor = AnchorStyles.Left;
                dtp.Margin = new Padding(0, 0, 8, 0);
            }
            Tema.Combo(cmbTipoMov);
            cmbTipoMov.ItemHeight = 28;
            cmbTipoMov.Size = new Size(190, Tema.AltoControl);
            cmbTipoMov.Anchor = AnchorStyles.Left;
            cmbTipoMov.Margin = new Padding(0, 0, 12, 0);
            Tema.Input(txtBuscarMaterial);
            txtBuscarMaterial.Dock = DockStyle.None;
            txtBuscarMaterial.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            txtBuscarMaterial.Height = Tema.AltoControl;
            txtBuscarMaterial.Margin = new Padding(0, 0, 12, 0);
            Tema.Boton(btnExportar, Variante.Primario);
            btnExportar.Text = "Exportar Excel";
            btnExportar.Size = new Size(130, Tema.AltoControl);
            btnExportar.Anchor = AnchorStyles.Left;
            btnExportar.Margin = new Padding(0);

            UiVista.AtenuarFecha(dtpDesde, chkDesde.Checked);
            UiVista.AtenuarFecha(dtpHasta, chkHasta.Checked);

            Tema.Grilla(dgvHistorial);
            dgvHistorial.RowTemplate.DefaultCellStyle.Font = Tema.FuenteTexto;
            dgvHistorial.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            Tema.Grilla(dgvDetalles);
            dgvDetalles.Margin = new Padding(0);
            dgvHistorial.Margin = new Padding(0);
        }

        private void ConfigurarGrid()
        {
            dgvHistorial.EnableHeadersVisualStyles = false;
            dgvHistorial.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvHistorial.MultiSelect = false;
            dgvHistorial.ReadOnly = true;
            dgvHistorial.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            dgvHistorial.RowTemplate.Height = Tema.AltoFila;

            dgvHistorial.AutoGenerateColumns = false;
            dgvHistorial.Columns.Clear();

            dgvHistorial.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colFecha",
                HeaderText = "Fecha",
                DataPropertyName = "fecha",
                FillWeight = 10
            });
            dgvHistorial.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colIdMov",
                HeaderText = "Nº Mov",
                DataPropertyName = "id_movimiento",
                FillWeight = 7
            });
            dgvHistorial.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colRemito",
                HeaderText = "Remito",
                DataPropertyName = "nroremito",
                FillWeight = 15
            });
            dgvHistorial.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colTipo",
                HeaderText = "Tipo",
                DataPropertyName = "tipo_movimiento",
                FillWeight = 16
            });
            dgvHistorial.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colOrigen",
                HeaderText = "Origen",
                DataPropertyName = "origen",
                FillWeight = 16
            });
            dgvHistorial.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colDestino",
                HeaderText = "Destino",
                DataPropertyName = "destino",
                FillWeight = 16
            });

            var colPdf = new DataGridViewButtonColumn();
            colPdf.Name = "colPdf";
            colPdf.HeaderText = "";
            colPdf.Text = "PDF";
            colPdf.UseColumnTextForButtonValue = true;
            colPdf.Width = 80;
            colPdf.AutoSizeMode = DataGridViewAutoSizeColumnMode.None; // Verde suave en lugar de azul
                colPdf.Tag = Variante.Primario;
            dgvHistorial.Columns.Add(colPdf);

            var colEditar = new DataGridViewButtonColumn();
            colEditar.Name = "colEditar";
            colEditar.HeaderText = "";
            colEditar.Text = "Editar";
            colEditar.UseColumnTextForButtonValue = true;
            colEditar.Width = 90;
            colEditar.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                colEditar.Tag = Variante.Secundario;
            dgvHistorial.Columns.Add(colEditar);

            // Ocultar Editar para rol Galpon
            if (servicios.Sesion.Role != SteeLog.Servicios.UserRole.Admin)
            {
                colEditar.Visible = false;
            }

            var colEliminar = new DataGridViewButtonColumn();
            colEliminar.Name = "colEliminar";
            colEliminar.HeaderText = "";
            colEliminar.Text = "Eliminar";
            colEliminar.UseColumnTextForButtonValue = true;
            colEliminar.Width = 100;
            colEliminar.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                colEliminar.Tag = Variante.Peligro;
            dgvHistorial.Columns.Add(colEliminar);

            dgvHistorial.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            dgvHistorial.Columns["colIdMov"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvHistorial.Columns["colRemito"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            dgvHistorial.CellFormatting += DgvHistorial_CellFormatting;
            dgvHistorial.CellContentClick += DgvHistorial_CellContentClick;
        }

        private static bool TryParsePrecio(object valor, out decimal precio)
        {
            if (valor is decimal valorDecimal)
            {
                precio = valorDecimal;
                return true;
            }

            return decimal.TryParse(
                Convert.ToString(valor, CultureInfo.CurrentCulture),
                NumberStyles.Currency,
                CultureInfo.CurrentCulture,
                out precio);
        }

        private void DgvHistorial_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var row = dgvHistorial.Rows[e.RowIndex].DataBoundItem as MovimientoGrupo;
            if (row == null) return;

            if (dgvHistorial.Columns[e.ColumnIndex].Name == "colEliminar")
            {
                ConfirmarEliminacion(row);
            }
            else if (dgvHistorial.Columns[e.ColumnIndex].Name == "colPdf")
            {
                GenerarPDF(row);
            }
            else if (dgvHistorial.Columns[e.ColumnIndex].Name == "colEditar")
            {
                EditarMovimiento(row);
            }
        }

        private void EditarMovimiento(MovimientoGrupo mov)
        {
            if (!string.IsNullOrEmpty(mov.tipo_movimiento) &&
                mov.tipo_movimiento.IndexOf("registro interno", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                ToastNotification.ShowWarning("Los ajustes de Registro Interno se gestionan desde el módulo de Materiales.");
                return;
            }

            var parent = this.TopLevelControl as VistaGeneral;
            if (parent != null)
            {
                // Abrir la vista de movimientos en modo edición reutilizando la caché de formularios
                parent.OpenEditMovimiento(mov.id_movimiento);
            }
            else
            {
                ToastNotification.ShowError("No se pudo encontrar el contenedor principal.");
            }
        }

        private async void GenerarPDF(MovimientoGrupo mov)
        {
            string ruta = await Presenter.GenerarRemitoAsync(mov.id_movimiento, cierre.Token);
            if (ruta == null || IsDisposed || cierre.IsCancellationRequested) return;
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = ruta, UseShellExecute = true }); }
            catch (Exception ex) { ToastNotification.ShowError("Error al abrir PDF: " + ex.Message); }
        }

        private async void ConfirmarEliminacion(MovimientoGrupo row)
        {
            if (resultadoIncierto)
            {
                if (!IsDisposed && !cierre.IsCancellationRequested)
                    AdvertirResultadoIncierto("Hay un resultado incierto pendiente. Recargue el historial (F5) y verifique antes de eliminar.");
                return;
            }
            bool confirmado = Dialogo.Confirmar(
                $"¿Está seguro de que desea eliminar el movimiento completo?\r\n\r\n" +
                $"Movimiento: {row.id_movimiento}\r\n" +
                $"Origen: {row.origen} | Destino: {row.destino}\r\n\r\n" +
                "El stock de todos sus materiales se revertirá automáticamente.",
                "Confirmar eliminación", true, this.FindForm());

            if (confirmado)
            {
                try
                {
                    var eliminado = await Presenter.EliminarAsync(row.id_movimiento, cierre.Token);
                    if (!eliminado.HasValue || IsDisposed || cierre.IsCancellationRequested) return;
                    ToastNotification.ShowSuccess("Registro eliminado correctamente.");
                    BuscarHistorial();
                }
                catch (Exception ex)
                {
                    ToastNotification.ShowError("Error al eliminar: " + ex.Message);
                }
            }
        }

        private void DgvHistorial_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;

            // Format date
            if (dgvHistorial.Columns[e.ColumnIndex].Name == "colFecha" && e.Value is DateTime dt)
            {
                e.Value = dt.ToString("dd/MM/yyyy");
                e.FormattingApplied = true;
            }

            // Hide remito 0
            if (dgvHistorial.Columns[e.ColumnIndex].Name == "colRemito" && e.Value != null)
            {
                if (e.Value.ToString() == "0")
                {
                    e.Value = "-";
                    e.FormattingApplied = true;
                }
            }

            // Color-code movement type
            if (dgvHistorial.Columns[e.ColumnIndex].Name == "colTipo" && e.Value != null)
            {
                string tipo = e.Value.ToString().ToUpper();
                if (tipo.Contains("ENTRADA") || tipo.Contains("INGRESO") || tipo.Contains("COMPRA") || tipo.Contains("AJUSTE POSITIVO") || tipo.Contains("APORTE"))
                {
                    e.CellStyle.ForeColor = Tema.AcentoHover; e.CellStyle.SelectionForeColor = e.CellStyle.ForeColor;
                }
                else if (tipo.Contains("SALIDA") || tipo.Contains("EGRESO") || tipo.Contains("ENVIO") || tipo.Contains("ENVÍO") || tipo.Contains("AJUSTE NEGATIVO") || tipo.Contains("RECORTE"))
                {
                    e.CellStyle.ForeColor = Color.FromArgb(222, 110, 100); e.CellStyle.SelectionForeColor = e.CellStyle.ForeColor;
                }
            }

        }

        private void CargarHistorial()
        {
            BuscarHistorial();
        }

        public void Refrescar()
        {
            BuscarHistorial();
        }

        private void btnBuscar_Click(object sender, EventArgs e)
        {
            BuscarHistorial();
        }

        private void btnExportar_Click(object sender, EventArgs e)
        {
            ExportarExcel();
        }

        private int versionHistorial;
        private CancellationTokenSource cargaHistorial;
        private async void BuscarHistorial()
        {
            int version = ++versionHistorial;
            cargaHistorial?.Cancel();
            cargaHistorial = CancellationTokenSource.CreateLinkedTokenSource(cierre.Token);
            try
            {
                DateTime? fechaDesde = (chkDesde != null && chkDesde.Checked) ? (DateTime?)dtpDesde.Value.Date : (DateTime?)null;
                DateTime? fechaHasta = (chkHasta != null && chkHasta.Checked) ? (DateTime?)dtpHasta.Value.Date.AddDays(1).AddSeconds(-1) : (DateTime?)null;

                string tipoFiltro = null;
                if (cmbTipoMov.SelectedItem != null)
                {
                    if (cmbTipoMov.SelectedItem is DataRowView drv)
                        tipoFiltro = drv["nombre"].ToString();
                    else
                        tipoFiltro = cmbTipoMov.SelectedItem.ToString();
                }

                if (tipoFiltro == "TODOS") tipoFiltro = null;

                var leidos = await Presenter.HistorialAsync(fechaDesde, fechaHasta, tipoFiltro, cargaHistorial.Token);
                if (IsDisposed || cierre.IsCancellationRequested || version != versionHistorial) return;
                rawHistorial = leidos.Select(x => new HistorialMovimiento { id_movimiento=x.IdMovimiento, id_movimientomaterial=x.IdLinea, fecha=x.Fecha, nroremito=x.Remito, tipo_movimiento=x.Tipo, material=x.Material, cantidad=x.Cantidad, origen=x.Origen, destino=x.Destino, observaciones=x.Observaciones, esrecorte=x.EsRecorte, medida_recorte=x.Medida, esinsumo=x.EsInsumo, es_compra_eventual=x.Eventual, precio_unitario=x.PrecioUnitario, precio_total=x.PrecioTotal }).ToList();

                var grupos = rawHistorial.GroupBy(m => m.id_movimiento).Select(g => new MovimientoGrupo
                {
                    id_movimiento = g.Key,
                    fecha = g.First().fecha,
                    nroremito = g.First().nroremito,
                    tipo_movimiento = g.First().tipo_movimiento,
                    origen = g.First().origen,
                    destino = g.First().destino,
                    observaciones = g.First().observaciones,
                    precio_total = g.First().precio_total,
                    Detalles = g.ToList()
                }).ToList();

                string busqueda = txtBuscarMaterial.Text.Trim();
                if (!string.IsNullOrEmpty(busqueda))
                {
                    grupos = grupos.Where(h =>
                        (h.nroremito ?? "").IndexOf(busqueda, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        h.origen.IndexOf(busqueda, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        h.destino.IndexOf(busqueda, StringComparison.OrdinalIgnoreCase) >= 0
                    ).ToList();
                }

                listaGrupos = grupos;

                dgvHistorial.DataSource = null;
                dgvHistorial.DataSource = listaGrupos;

                if (listaGrupos.Count > 0 && dgvHistorial.SelectedRows.Count == 0 && dgvHistorial.Rows.Count > 0)
                {
                    dgvHistorial.Rows[0].Selected = true;
                }

                lblSubtitulo.Text = $"{listaGrupos.Count} movimiento(s) encontrado(s)";
                if (resultadoIncierto && version > versionHistorialAlMarcarIncierto) resultadoIncierto = false;
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                if (!IsDisposed && version == versionHistorial) ToastNotification.ShowError($"Error al cargar historial: {ex.Message}");
            }
        }

        private void ExportarExcel()
        {
            if (listaGrupos.Count == 0)
            {
                ToastNotification.ShowWarning("No hay datos para exportar.");
                return;
            }

            SaveFileDialog save = new SaveFileDialog();
            save.Filter = "Excel Files (*.xlsx)|*.xlsx";
            save.FileName = "HistorialMovimientos_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".xlsx";

            if (save.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    using (var excel = new OfficeOpenXml.ExcelPackage())
                    {
                        var ws = excel.Workbook.Worksheets.Add("Historial");

                        string[] headers = { "Fecha", "Nº Mov", "Remito", "Tipo", "Origen", "Destino", "Material", "Cantidad", "Observaciones" };
                        for (int i = 0; i < headers.Length; i++)
                        {
                            ws.Cells[1, i + 1].Value = headers[i];
                            ws.Cells[1, i + 1].Style.Font.Bold = true;
                            ws.Cells[1, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                            ws.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(35, 63, 43));
                            ws.Cells[1, i + 1].Style.Font.Color.SetColor(Color.White);
                        }

                        int row = 2;
                        foreach (var grupo in listaGrupos)
                        {
                            foreach (var det in grupo.Detalles)
                            {
                                ws.Cells[row, 1].Value = grupo.fecha.ToString("dd/MM/yyyy");
                                ws.Cells[row, 2].Value = grupo.id_movimiento;
                                ws.Cells[row, 3].Value = string.IsNullOrWhiteSpace(grupo.nroremito) ? "-" : grupo.nroremito;
                                ws.Cells[row, 4].Value = grupo.tipo_movimiento;
                                ws.Cells[row, 5].Value = grupo.origen;
                                ws.Cells[row, 6].Value = grupo.destino;
                                string res_mat = det.material;
                                if (det.esrecorte && det.medida_recorte.HasValue) res_mat += $" ({det.medida_recorte.Value})";
                                else if (det.es_compra_eventual) res_mat += " (EVENTUAL)";
                                ws.Cells[row, 7].Value = res_mat;
                                ws.Cells[row, 8].Value = det.cantidad;
                                ws.Cells[row, 9].Value = grupo.observaciones;
                                row++;
                            }
                        }

                        ws.Cells.AutoFitColumns();
                        excel.SaveAs(new System.IO.FileInfo(save.FileName));
                    }
                    ToastNotification.ShowSuccess("Exportación exitosa.");
                }
                catch (Exception ex)
                {
                    ToastNotification.ShowError("Error al exportar: " + ex.Message);
                }
            }
        }
    }
}
