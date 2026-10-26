using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using Guna.UI2.WinForms;
using WP_Inventario.Datos;
using WP_Inventario.Modelos;
using WP_Inventario.Servicios;
using WP_Inventario;

namespace WP_Inventario.Vistas.Movimientos
{
    public partial class Vista_HistorialMovimientos : Form, IMessageFilter, IRefrescable
    {
        private MovimientoDatos movDatos = new MovimientoDatos();
        private List<HistorialMovimiento> rawHistorial = new List<HistorialMovimiento>();
        private List<MovimientoGrupo> listaGrupos = new List<MovimientoGrupo>();
        private Guna.UI2.WinForms.Guna2DataGridView dgvDetalles;
        private Label lblDetalles;
        private CheckBox chkDesde;
        private CheckBox chkHasta;
        private decimal precioUnitarioAnterior;

        public Vista_HistorialMovimientos()
        {
            InitializeComponent();
            Application.AddMessageFilter(this);
            this.FormClosed += (s, e) => Application.RemoveMessageFilter(this);
            this.VisibleChanged += (s, e) => { if (this.Visible) BuscarHistorial(); };
            
            ConfigurarUIAdicional();
            ConfigurarFechas();
            CargarTiposMovimiento();
            ConfigurarGrid();
            CargarHistorial();
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
            lblDetalles.Text = "Materiales del Movimiento Seleccionado";
            lblDetalles.Font = new Font("Bahnschrift SemiBold", 12F, FontStyle.Bold);
            lblDetalles.ForeColor = Color.White;
            lblDetalles.Dock = DockStyle.Fill;
            lblDetalles.TextAlign = ContentAlignment.MiddleLeft;

            dgvDetalles = new Guna.UI2.WinForms.Guna2DataGridView();
            dgvDetalles.EnableHeadersVisualStyles = false;
            dgvDetalles.ThemeStyle.AlternatingRowsStyle.BackColor = Color.FromArgb(29, 38, 35);
            dgvDetalles.ThemeStyle.BackColor = Color.FromArgb(29, 38, 35);
            dgvDetalles.ThemeStyle.GridColor = Color.FromArgb(29, 38, 35);
            dgvDetalles.ThemeStyle.HeaderStyle.BackColor = Color.FromArgb(35, 63, 43);
            dgvDetalles.ThemeStyle.HeaderStyle.ForeColor = Color.White;
            dgvDetalles.ThemeStyle.HeaderStyle.Height = 35;
            dgvDetalles.ThemeStyle.RowsStyle.BackColor = Color.FromArgb(29, 38, 35);
            dgvDetalles.ThemeStyle.RowsStyle.ForeColor = Color.White;
            dgvDetalles.ThemeStyle.RowsStyle.Height = 35;
            dgvDetalles.ThemeStyle.RowsStyle.SelectionBackColor = Color.FromArgb(45, 55, 52);
            dgvDetalles.ThemeStyle.RowsStyle.SelectionForeColor = Color.White;
            dgvDetalles.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(35, 63, 43);
            dgvDetalles.RowTemplate.Height = 35;
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
            bool esAdmin = SessionManager.Role == UserRole.Admin;
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
                    e.CellStyle.ForeColor = Color.Orange;
                    e.CellStyle.SelectionForeColor = Color.Orange;
                }
                else if (det.esinsumo)
                {
                    e.Value = "INSUMO";
                    e.CellStyle.ForeColor = Color.FromArgb(0, 188, 212);
                    e.CellStyle.SelectionForeColor = Color.FromArgb(0, 188, 212);
                }
                else if (det.es_compra_eventual)
                {
                    e.Value = "EVENTUAL";
                    e.CellStyle.ForeColor = Color.FromArgb(255, 165, 0);
                    e.CellStyle.SelectionForeColor = Color.FromArgb(255, 165, 0);
                }
                else
                {
                    e.Value = "—";
                    e.CellStyle.ForeColor = Color.FromArgb(80, 80, 80);
                    e.CellStyle.SelectionForeColor = Color.FromArgb(80, 80, 80);
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
                    ? Color.DarkOrange
                    : Color.FromArgb(140, 140, 140);
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

        private void DgvDetalles_CellEndEdit(object sender, DataGridViewCellEventArgs e)
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
                decimal precioTotal = movDatos.ActualizarPrecioUnitarioCompraEventual(detalle.id_movimientomaterial.Value, precioNuevo);
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
            dtpDesde.Enabled = false;
            dtpHasta.Enabled = false;

            // Checkbox Desde
            chkDesde = new CheckBox();
            chkDesde.Text = "Desde:";
            chkDesde.ForeColor = System.Drawing.Color.White;
            chkDesde.Font = new System.Drawing.Font("Bahnschrift Light", 11.5F);
            chkDesde.AutoSize = true;
            chkDesde.Anchor = AnchorStyles.Left;
            chkDesde.Checked = false;
            chkDesde.CheckedChanged += (s, e) => { dtpDesde.Enabled = chkDesde.Checked; BuscarHistorial(); };
            materiales.Controls.Remove(lblDesde);
            materiales.Controls.Add(chkDesde, 0, 0);

            // Checkbox Hasta
            chkHasta = new CheckBox();
            chkHasta.Text = "Hasta:";
            chkHasta.ForeColor = System.Drawing.Color.White;
            chkHasta.Font = new System.Drawing.Font("Bahnschrift Light", 11.5F);
            chkHasta.AutoSize = true;
            chkHasta.Anchor = AnchorStyles.Left;
            chkHasta.Checked = false;
            chkHasta.CheckedChanged += (s, e) => { dtpHasta.Enabled = chkHasta.Checked; BuscarHistorial(); };
            materiales.Controls.Remove(lblHasta);
            materiales.Controls.Add(chkHasta, 2, 0);
        }

        private void CargarTiposMovimiento()
        {
            try
            {
                ComunDatos datos = new ComunDatos();
                var dt = datos.ObtenerListaGenerica("tipo_movimiento");

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

                cmbTipoMov.DataSource = dtOrdenado;
                cmbTipoMov.DisplayMember = "nombre";
                cmbTipoMov.ValueMember = "nombre";
                cmbTipoMov.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error cargando tipos: " + ex.Message);
                cmbTipoMov.DataSource = null;
                cmbTipoMov.Items.Clear();
                cmbTipoMov.Items.Add("TODOS");
                cmbTipoMov.SelectedIndex = 0;
            }
        }

        private void ConfigurarGrid()
        {
            dgvHistorial.EnableHeadersVisualStyles = false;
            dgvHistorial.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvHistorial.MultiSelect = false;
            dgvHistorial.ReadOnly = true;
            dgvHistorial.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgvHistorial.ColumnHeadersHeight = 35;

            // Colores de gestión estándar
            dgvHistorial.ThemeStyle.HeaderStyle.BackColor = Color.FromArgb(35, 63, 43);
            dgvHistorial.ThemeStyle.HeaderStyle.ForeColor = Color.White;
            dgvHistorial.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(35, 63, 43);
            dgvHistorial.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvHistorial.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(35, 63, 43);
            dgvHistorial.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;

            dgvHistorial.ThemeStyle.RowsStyle.BackColor = Color.FromArgb(29, 38, 35);
            dgvHistorial.ThemeStyle.RowsStyle.ForeColor = Color.White;
            dgvHistorial.ThemeStyle.RowsStyle.SelectionBackColor = Color.FromArgb(45, 55, 52);
            dgvHistorial.ThemeStyle.RowsStyle.SelectionForeColor = Color.White;
            dgvHistorial.ThemeStyle.AlternatingRowsStyle.BackColor = Color.FromArgb(29, 38, 35);
            dgvHistorial.RowTemplate.Height = 40;

            dgvHistorial.AutoGenerateColumns = false;
            dgvHistorial.Columns.Clear();

            dgvHistorial.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colFecha",
                HeaderText = "Fecha",
                DataPropertyName = "fecha",
                FillWeight = 12
            });
            dgvHistorial.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colIdMov",
                HeaderText = "Nº Mov",
                DataPropertyName = "id_movimiento",
                FillWeight = 8
            });
            dgvHistorial.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colRemito",
                HeaderText = "Remito",
                DataPropertyName = "nroremito",
                FillWeight = 8
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
            colPdf.Width = 45;
            colPdf.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            colPdf.FlatStyle = FlatStyle.Flat;
            colPdf.DefaultCellStyle.BackColor = Color.FromArgb(29, 38, 35);
            colPdf.DefaultCellStyle.ForeColor = Color.FromArgb(100, 200, 100); // Verde suave en lugar de azul
            colPdf.DefaultCellStyle.SelectionBackColor = Color.FromArgb(45, 55, 52);
            colPdf.DefaultCellStyle.SelectionForeColor = Color.FromArgb(100, 200, 100);
            colPdf.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvHistorial.Columns.Add(colPdf);

            var colEditar = new DataGridViewButtonColumn();
            colEditar.Name = "colEditar";
            colEditar.HeaderText = "";
            colEditar.Text = "Editar";
            colEditar.UseColumnTextForButtonValue = true;
            colEditar.Width = 55;
            colEditar.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            colEditar.FlatStyle = FlatStyle.Flat;
            colEditar.DefaultCellStyle.BackColor = Color.FromArgb(29, 38, 35);
            colEditar.DefaultCellStyle.ForeColor = Color.DarkOrange; 
            colEditar.DefaultCellStyle.SelectionBackColor = Color.FromArgb(45, 55, 52);
            colEditar.DefaultCellStyle.SelectionForeColor = Color.DarkOrange;
            colEditar.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvHistorial.Columns.Add(colEditar);

            // Ocultar Editar para rol Galpon
            if (WP_Inventario.Servicios.SessionManager.Role == WP_Inventario.Servicios.UserRole.Galpon)
            {
                colEditar.Visible = false;
                colEditar.Width = 0;
            }

            var colEliminar = new DataGridViewButtonColumn();
            colEliminar.Name = "colEliminar";
            colEliminar.HeaderText = "";
            colEliminar.Text = "X";
            colEliminar.UseColumnTextForButtonValue = true;
            colEliminar.Width = 35;
            colEliminar.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            colEliminar.FlatStyle = FlatStyle.Flat;
            colEliminar.DefaultCellStyle.BackColor = Color.FromArgb(29, 38, 35);
            colEliminar.DefaultCellStyle.ForeColor = Color.IndianRed;
            colEliminar.DefaultCellStyle.SelectionBackColor = Color.FromArgb(45, 55, 52);
            colEliminar.DefaultCellStyle.SelectionForeColor = Color.IndianRed;
            colEliminar.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
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

        private void GenerarPDF(MovimientoGrupo mov)
        {
            try
            {
                WP_Inventario.Reportes.MovimientoPDF generador = new WP_Inventario.Reportes.MovimientoPDF();
                string tempFileName = System.IO.Path.Combine(System.IO.Path.GetTempPath(), generador.SugerirNombreArchivo(mov.id_movimiento));
                
                generador.Generar(tempFileName, mov.id_movimiento);
                
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = tempFileName,
                    UseShellExecute = true
                });
            }
            catch (Exception exPdf)
            {
                ToastNotification.ShowError("Error al generar PDF: " + exPdf.Message);
            }
        }

        private void ConfirmarEliminacion(MovimientoGrupo row)
        {
            var result = MessageBox.Show(
                $"¿Está seguro de que desea eliminar el movimiento completo?\r\n\r\n" +
                $"Movimiento: {row.id_movimiento}\r\n" +
                $"Origen: {row.origen} | Destino: {row.destino}\r\n\r\n" +
                "El stock de todos sus materiales se revertirá automáticamente.",
                "Confirmar Eliminación",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (result == DialogResult.Yes)
            {
                try
                {
                    movDatos.EliminarMovimiento(row.id_movimiento, null, 0, row.tipo_movimiento);
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
                    e.CellStyle.ForeColor = Color.FromArgb(100, 200, 100);
                }
                else if (tipo.Contains("SALIDA") || tipo.Contains("EGRESO") || tipo.Contains("ENVIO") || tipo.Contains("ENVÍO") || tipo.Contains("AJUSTE NEGATIVO") || tipo.Contains("RECORTE"))
                {
                    e.CellStyle.ForeColor = Color.FromArgb(200, 100, 100);
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

        private void BuscarHistorial()
        {
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

                rawHistorial = movDatos.ObtenerHistorial(fechaDesde, fechaHasta, tipoFiltro);

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
            }
            catch (Exception ex)
            {
                ToastNotification.ShowError($"Error al cargar historial: {ex.Message}");
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
