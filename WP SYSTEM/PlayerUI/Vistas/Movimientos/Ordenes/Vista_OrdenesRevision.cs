using Guna.UI2.WinForms;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using WP_Inventario.Datos;
using WP_Inventario.Modelos;
using WP_Inventario.Servicios;

namespace WP_Inventario.Vistas.Movimientos.Ordenes
{
    public class Vista_OrdenesRevision : Form
    {
        private readonly OrdenCompraDatos _ordenDatos = new OrdenCompraDatos();
        private readonly BindingList<OrdenCompraDetalle> _detalles = new BindingList<OrdenCompraDetalle>();
        private int _idOrden;

        private Label lblTitulo;
        private Label lblResumen;
        private Label lblAyuda;
        private Label lblMaterialActual;
        private Label lblPendienteActual;
        private Guna2TextBox txtObservacionesRecepcion;
        private Guna2TextBox txtCantidadRapida;
        private Guna2DataGridView dgvDetalles;
        private Guna2Button btnCancelar;
        private Guna2Button btnAplicarSiguiente;
        private Guna2Button btnCompletarActual;
        private Guna2Button btnCompletarTodo;
        private Guna2Button btnConfirmar;
        private KeyPressEventHandler _soloDecimalHandler;
        private int _indiceActual = -1;
        public Action<int> OnRecepcionConfirmada { get; set; }
        public Action OnCancelar { get; set; }
        public int CurrentOrderId => _idOrden;

        public Vista_OrdenesRevision()
        {
            InitializeComponent();
        }

        public void SetOrder(int idOrden)
        {
            _idOrden = idOrden;
            CargarOrden();
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            BackColor = OrdenesUi.FondoForm;

            var root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.RowCount = 4;
            root.ColumnCount = 1;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 70F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 190F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 90F));

            var header = new Panel();
            header.Padding = new Padding(18, 18, 18, 6);
            lblTitulo = new Label();
            lblTitulo.Text = "Revisión de Orden";
            lblTitulo.Font = new Font("Bahnschrift SemiBold", 17F, FontStyle.Bold);
            lblTitulo.ForeColor = Color.White;
            lblTitulo.Dock = DockStyle.Fill;
            header.Controls.Add(lblTitulo);

            var resumenCard = new Panel();
            resumenCard.Dock = DockStyle.Fill;
            resumenCard.Padding = new Padding(18, 10, 18, 10);
            resumenCard.BackColor = OrdenesUi.FondoPanel;
            lblResumen = new Label();
            lblResumen.ForeColor = Color.White;
            lblResumen.Font = new Font("Bahnschrift Light", 10.75F);
            lblResumen.Dock = DockStyle.Top;
            lblResumen.Height = 38;

            lblAyuda = new Label();
            lblAyuda.ForeColor = OrdenesUi.TextoMuted;
            lblAyuda.Font = new Font("Bahnschrift Light", 9.75F);
            lblAyuda.Dock = DockStyle.Top;
            lblAyuda.Height = 20;
            lblAyuda.Text = "Carga rápida: escribí la cantidad a sumar, Enter y seguís con el próximo material.";

            var panelCargaRapida = new Panel();
            panelCargaRapida.Dock = DockStyle.Top;
            panelCargaRapida.Height = 50;

            lblMaterialActual = new Label();
            lblMaterialActual.ForeColor = Color.White;
            lblMaterialActual.Font = new Font("Bahnschrift SemiBold", 10.5F, FontStyle.Bold);
            lblMaterialActual.Location = new Point(0, 4);
            lblMaterialActual.Size = new Size(420, 22);
            lblMaterialActual.Text = "Material actual: -";

            lblPendienteActual = new Label();
            lblPendienteActual.ForeColor = OrdenesUi.TextoMuted;
            lblPendienteActual.Font = new Font("Bahnschrift Light", 9.75F);
            lblPendienteActual.Location = new Point(0, 28);
            lblPendienteActual.Size = new Size(220, 20);
            lblPendienteActual.Text = "Pendiente: -";

            txtCantidadRapida = OrdenesUi.CrearTextBox();
            txtCantidadRapida.Location = new Point(430, 8);
            txtCantidadRapida.Size = new Size(110, 36);
            txtCantidadRapida.PlaceholderText = "+ Cant.";
            txtCantidadRapida.TextAlign = HorizontalAlignment.Right;
            txtCantidadRapida.KeyPress += TxtCantidadRapida_KeyPress;
            txtCantidadRapida.KeyDown += TxtCantidadRapida_KeyDown;

            btnAplicarSiguiente = OrdenesUi.CrearBoton("Aplicar y Siguiente", true);
            btnAplicarSiguiente.Location = new Point(554, 8);
            btnAplicarSiguiente.Size = new Size(180, 36);
            btnAplicarSiguiente.Click += BtnAplicarSiguiente_Click;

            btnCompletarActual = OrdenesUi.CrearBoton("Completar Actual");
            btnCompletarActual.Location = new Point(746, 8);
            btnCompletarActual.Size = new Size(170, 36);
            btnCompletarActual.Click += BtnCompletarActual_Click;

            panelCargaRapida.Controls.Add(lblMaterialActual);
            panelCargaRapida.Controls.Add(lblPendienteActual);
            panelCargaRapida.Controls.Add(txtCantidadRapida);
            panelCargaRapida.Controls.Add(btnAplicarSiguiente);
            panelCargaRapida.Controls.Add(btnCompletarActual);

            txtObservacionesRecepcion = OrdenesUi.CrearTextBox(true);
            txtObservacionesRecepcion.Dock = DockStyle.Bottom;
            txtObservacionesRecepcion.Height = 44;
            txtObservacionesRecepcion.PlaceholderText = "Observación general de recepción";

            resumenCard.Controls.Add(txtObservacionesRecepcion);
            resumenCard.Controls.Add(panelCargaRapida);
            resumenCard.Controls.Add(lblAyuda);
            resumenCard.Controls.Add(lblResumen);

            dgvDetalles = new Guna2DataGridView();
            dgvDetalles.Dock = DockStyle.Fill;
            dgvDetalles.AutoGenerateColumns = false;
            dgvDetalles.DataSource = _detalles;
            dgvDetalles.EditMode = DataGridViewEditMode.EditOnEnter;
            dgvDetalles.CellClick += DgvDetalles_CellClick;
            dgvDetalles.CellEndEdit += DgvDetalles_CellEndEdit;
            dgvDetalles.CellContentClick += DgvDetalles_CellContentClick;
            dgvDetalles.EditingControlShowing += DgvDetalles_EditingControlShowing;
            dgvDetalles.KeyDown += DgvDetalles_KeyDown;
            dgvDetalles.SelectionChanged += DgvDetalles_SelectionChanged;
            dgvDetalles.RowPrePaint += DgvDetalles_RowPrePaint;
            OrdenesUi.AplicarEstiloGrid(dgvDetalles);

            dgvDetalles.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colMaterial",
                HeaderText = "Material",
                DataPropertyName = "material_nombre",
                ReadOnly = true,
                FillWeight = 30
            });
            dgvDetalles.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colPedida",
                HeaderText = "Pedida",
                DataPropertyName = "cantidad_pedida",
                ReadOnly = true,
                FillWeight = 12,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Format = "0.##",
                    Alignment = DataGridViewContentAlignment.MiddleRight
                }
            });
            dgvDetalles.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colRecibida",
                HeaderText = "Recibida real",
                DataPropertyName = "cantidad_recibida",
                FillWeight = 14,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Format = "0.##",
                    Alignment = DataGridViewContentAlignment.MiddleRight
                }
            });
            dgvDetalles.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colFaltante",
                HeaderText = "Faltante",
                DataPropertyName = "cantidad_faltante",
                ReadOnly = true,
                FillWeight = 12,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Format = "0.##",
                    Alignment = DataGridViewContentAlignment.MiddleRight
                }
            });
            dgvDetalles.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colEstadoItem",
                HeaderText = "Resultado",
                DataPropertyName = "estado_item",
                ReadOnly = true,
                FillWeight = 14
            });
            dgvDetalles.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colObs",
                HeaderText = "Observación ítem",
                DataPropertyName = "observacion_item",
                FillWeight = 20
            });
            dgvDetalles.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "colCompletar",
                HeaderText = "",
                Text = "Completar",
                UseColumnTextForButtonValue = true,
                FillWeight = 12
            });

            var footer = new Panel();
            footer.Dock = DockStyle.Fill;
            footer.Padding = new Padding(18);

            btnCancelar = OrdenesUi.CrearBoton("Cancelar");
            btnCancelar.Location = new Point(18, 18);
            btnCancelar.Click += BtnCancelar_Click;
            footer.Controls.Add(btnCancelar);

            btnCompletarTodo = OrdenesUi.CrearBoton("Completar Todo");
            btnCompletarTodo.Location = new Point(214, 18);
            btnCompletarTodo.Click += BtnCompletarTodo_Click;
            footer.Controls.Add(btnCompletarTodo);

            btnConfirmar = OrdenesUi.CrearBoton("Confirmar Recepción", true);
            btnConfirmar.Location = new Point(820, 18);
            btnConfirmar.Click += BtnConfirmar_Click;
            footer.Controls.Add(btnConfirmar);

            root.Controls.Add(header, 0, 0);
            root.Controls.Add(resumenCard, 0, 1);
            root.Controls.Add(dgvDetalles, 0, 2);
            root.Controls.Add(footer, 0, 3);
            Controls.Add(root);

            ResumeLayout(false);
        }

        private void CargarOrden()
        {
            try
            {
                var orden = _ordenDatos.ObtenerOrdenPorId(_idOrden);
                if (orden == null)
                    throw new Exception("La orden no existe.");

                lblTitulo.Text = "Revisión de Orden " + orden.nro_orden;
                lblResumen.Text =
                    "Proveedor: " + orden.proveedor_nombre +
                    "    |    Fecha: " + orden.fecha.ToString("dd/MM/yyyy") +
                    "    |    Estado actual: " + orden.estado +
                    Environment.NewLine +
                    "Remito: " + (string.IsNullOrWhiteSpace(orden.nro_remito) ? "-" : orden.nro_remito) +
                    "    |    Destino: " + orden.destino_nombre;

                txtObservacionesRecepcion.Text = orden.observacion_revision ?? "";
                _detalles.Clear();
                foreach (var detalle in orden.Detalles)
                    _detalles.Add(detalle);

                bool puedeRevisar = (SessionManager.Role == UserRole.Galpon || SessionManager.Role == UserRole.Admin)
                    && OrdenCompraEstados.PermiteRecepcion(orden.estado);
                dgvDetalles.ReadOnly = !puedeRevisar;
                dgvDetalles.Columns["colMaterial"].ReadOnly = true;
                dgvDetalles.Columns["colPedida"].ReadOnly = true;
                dgvDetalles.Columns["colFaltante"].ReadOnly = true;
                dgvDetalles.Columns["colEstadoItem"].ReadOnly = true;
                dgvDetalles.Columns["colCompletar"].ReadOnly = true;
                // Forzar explícitamente que las columnas editables queden habilitadas
                // cuando se permite la revisión (fix para entregas parciales)
                dgvDetalles.Columns["colRecibida"].ReadOnly = !puedeRevisar;
                dgvDetalles.Columns["colObs"].ReadOnly = !puedeRevisar;
                btnConfirmar.Enabled = puedeRevisar;
                btnAplicarSiguiente.Enabled = puedeRevisar;
                btnCompletarActual.Enabled = puedeRevisar;
                btnCompletarTodo.Enabled = puedeRevisar;
                txtCantidadRapida.Enabled = puedeRevisar;

                SeleccionarSiguientePendiente(0, true);
            }
            catch (Exception ex)
            {
                ToastNotification.ShowError("No se pudo cargar la orden para revisión: " + ex.Message);
            }
        }

        private void DgvDetalles_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            var detalle = dgvDetalles.Rows[e.RowIndex].DataBoundItem as OrdenCompraDetalle;
            if (detalle == null)
                return;

            try
            {
                string nombreColumna = dgvDetalles.Columns[e.ColumnIndex].Name;
                if (nombreColumna == "colRecibida")
                {
                    decimal recibida;
                    if (!decimal.TryParse(Convert.ToString(dgvDetalles.Rows[e.RowIndex].Cells[e.ColumnIndex].Value).Trim().Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out recibida))
                    {
                        // Valor no numérico: revertir al valor actual sin perder la edición
                        dgvDetalles.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = detalle.cantidad_recibida;
                        ToastNotification.ShowWarning("La cantidad recibida debe ser un número.");
                    }
                    else
                    {
                        AplicarCantidadRecibida(detalle, recibida);
                    }
                }
                else if (nombreColumna == "colObs")
                {
                    detalle.observacion_item = Convert.ToString(dgvDetalles.Rows[e.RowIndex].Cells[e.ColumnIndex].Value) ?? "";
                }
            }
            catch (Exception ex)
            {
                ToastNotification.ShowWarning(ex.Message);
                // Sincronizar la celda con el valor vigente del detalle (no fue modificado
                // porque AplicarCantidadRecibida lanzó excepción antes de asignar)
                detalle.cantidad_faltante = detalle.cantidad_pedida - detalle.cantidad_recibida;
                dgvDetalles.Rows[e.RowIndex].Cells["colRecibida"].Value = detalle.cantidad_recibida;
            }

            dgvDetalles.Refresh();
        }

        private void DgvDetalles_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            _indiceActual = e.RowIndex;
            SincronizarCargaRapida();

            if (dgvDetalles.Columns[e.ColumnIndex].Name != "colRecibida")
                return;

            dgvDetalles.CurrentCell = dgvDetalles.Rows[e.RowIndex].Cells[e.ColumnIndex];
            dgvDetalles.BeginEdit(true);

            if (dgvDetalles.EditingControl is TextBox tb)
            {
                tb.SelectAll();
                tb.Focus();
            }
        }

        private void DgvDetalles_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvDetalles.CurrentRow == null || dgvDetalles.CurrentRow.Index < 0)
                return;

            _indiceActual = dgvDetalles.CurrentRow.Index;
            SincronizarCargaRapida();
        }

        private void DgvDetalles_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            if (dgvDetalles.Columns[e.ColumnIndex].Name != "colCompletar")
                return;

            var detalle = dgvDetalles.Rows[e.RowIndex].DataBoundItem as OrdenCompraDetalle;
            if (detalle == null)
                return;

            try
            {
                AplicarCantidadRecibida(detalle, detalle.cantidad_pedida);
                dgvDetalles.Refresh();
            }
            catch (Exception ex)
            {
                ToastNotification.ShowWarning(ex.Message);
            }
        }

        private void DgvDetalles_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if (dgvDetalles.CurrentCell == null)
                return;

            if (dgvDetalles.CurrentCell.OwningColumn.Name != "colRecibida")
                return;

            var tb = e.Control as TextBox;
            if (tb == null)
                return;

            if (_soloDecimalHandler != null)
                tb.KeyPress -= _soloDecimalHandler;

            _soloDecimalHandler = (s, ke) =>
            {
                if (!char.IsControl(ke.KeyChar) && !char.IsDigit(ke.KeyChar) && ke.KeyChar != ',' && ke.KeyChar != '.')
                    ke.Handled = true;
            };

            tb.KeyPress += _soloDecimalHandler;
            tb.TextAlign = HorizontalAlignment.Right;
            BeginInvoke(new Action(() => tb.SelectAll()));
        }

        private void DgvDetalles_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter || dgvDetalles.CurrentCell == null)
                return;

            if (dgvDetalles.CurrentCell.OwningColumn.Name != "colRecibida")
                return;

            e.Handled = true;
            e.SuppressKeyPress = true;

            dgvDetalles.EndEdit();

            int nextRow = dgvDetalles.CurrentCell.RowIndex + 1;
            if (nextRow >= dgvDetalles.Rows.Count)
                return;

            int colIndex = dgvDetalles.Columns["colRecibida"].Index;
            dgvDetalles.CurrentCell = dgvDetalles.Rows[nextRow].Cells[colIndex];
            dgvDetalles.BeginEdit(true);

            if (dgvDetalles.EditingControl is TextBox tb)
            {
                tb.SelectAll();
                tb.Focus();
            }
        }

        private void DgvDetalles_RowPrePaint(object sender, DataGridViewRowPrePaintEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            var detalle = dgvDetalles.Rows[e.RowIndex].DataBoundItem as OrdenCompraDetalle;
            if (detalle == null)
                return;

            if (detalle.cantidad_faltante == 0)
            {
                dgvDetalles.Rows[e.RowIndex].DefaultCellStyle.ForeColor = Color.FromArgb(148, 214, 178);
                dgvDetalles.Rows[e.RowIndex].DefaultCellStyle.SelectionForeColor = Color.FromArgb(148, 214, 178);
            }
            else
            {
                dgvDetalles.Rows[e.RowIndex].DefaultCellStyle.ForeColor = Color.FromArgb(243, 196, 104);
                dgvDetalles.Rows[e.RowIndex].DefaultCellStyle.SelectionForeColor = Color.FromArgb(243, 196, 104);
            }
        }

        private void BtnConfirmar_Click(object sender, EventArgs e)
        {
            if (SessionManager.Role != UserRole.Galpon && SessionManager.Role != UserRole.Admin)
            {
                ToastNotification.ShowWarning("Solo Galpón o Admin pueden confirmar la recepción.");
                return;
            }

            try
            {
                if (!SincronizarEdicionActual())
                    return;

                if (!AplicarCargaRapidaPendiente())
                    return;

                if (!ConfirmarImpactoStock())
                    return;

                string nuevoEstado = _ordenDatos.ConfirmarRecepcion(
                    _idOrden,
                    _detalles.ToList(),
                    SessionManager.Username,
                    txtObservacionesRecepcion.Text.Trim());

                ToastNotification.ShowSuccess("Recepción confirmada: " + nuevoEstado + ".");
                OnRecepcionConfirmada?.Invoke(_idOrden);
                if (OnRecepcionConfirmada != null)
                    return;

                var parent = TopLevelControl as VistaGeneral;
                if (parent != null)
                {
                    parent.RefrescarOrdenesSiAbierto();
                    parent.OpenRevisionOrden(_idOrden);
                }
            }
            catch (Exception ex)
            {
                ToastNotification.ShowError("No se pudo confirmar la recepción: " + ex.Message);
            }
        }

        private bool SincronizarEdicionActual()
        {
            if (dgvDetalles == null)
                return true;

            try
            {
                if (dgvDetalles.IsCurrentCellDirty)
                    dgvDetalles.CommitEdit(DataGridViewDataErrorContexts.Commit);

                if (dgvDetalles.IsCurrentCellInEditMode && !dgvDetalles.EndEdit())
                {
                    ToastNotification.ShowWarning("Terminá de editar la cantidad antes de confirmar.");
                    return false;
                }

                dgvDetalles.EndEdit();
                dgvDetalles.Refresh();
                return true;
            }
            catch (Exception ex)
            {
                ToastNotification.ShowWarning("No se pudo tomar la edición actual: " + ex.Message);
                return false;
            }
        }

        private bool AplicarCargaRapidaPendiente()
        {
            string texto = (txtCantidadRapida.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(texto))
                return true;

            if (!TryObtenerDetalleActual(out var detalle))
                return false;

            decimal cantidadASumar;
            if (!decimal.TryParse(texto.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out cantidadASumar) || cantidadASumar <= 0)
            {
                ToastNotification.ShowWarning("La cantidad rápida pendiente debe ser mayor a cero.");
                txtCantidadRapida.Focus();
                txtCantidadRapida.SelectAll();
                return false;
            }

            try
            {
                decimal nuevoTotal = detalle.cantidad_recibida + cantidadASumar;
                AplicarCantidadRecibida(detalle, nuevoTotal);
                txtCantidadRapida.Text = "";
                dgvDetalles.Refresh();
                return true;
            }
            catch (Exception ex)
            {
                ToastNotification.ShowWarning(ex.Message);
                txtCantidadRapida.Focus();
                txtCantidadRapida.SelectAll();
                return false;
            }
        }

        private bool ConfirmarImpactoStock()
        {
            var impactos = _detalles
                .Select(d => new
                {
                    Detalle = d,
                    Delta = d.cantidad_recibida - d.cantidad_recibida_anterior
                })
                .Where(x => x.Delta > 0)
                .ToList();

            var mensaje = new StringBuilder();
            mensaje.AppendLine("Se va a confirmar la recepción.");
            mensaje.AppendLine();

            if (impactos.Count == 0)
            {
                mensaje.AppendLine("No hay cantidades nuevas para impactar en stock.");
            }
            else
            {
                mensaje.AppendLine("Cantidades nuevas que se sumarán al stock:");
                mensaje.AppendLine();

                foreach (var impacto in impactos.Take(12))
                    mensaje.AppendLine("- " + impacto.Detalle.material_nombre + ": +" + FormatearCantidad(impacto.Delta));

                if (impactos.Count > 12)
                    mensaje.AppendLine("- ... y " + (impactos.Count - 12).ToString("N0") + " materiales más.");
            }

            mensaje.AppendLine();
            mensaje.AppendLine("¿Está seguro de confirmar esta recepción?");

            return MessageBox.Show(
                mensaje.ToString(),
                "Confirmar recepción",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2) == DialogResult.Yes;
        }

        private void BtnCancelar_Click(object sender, EventArgs e)
        {
            OnCancelar?.Invoke();
            if (OnCancelar != null)
                return;

            var parent = TopLevelControl as VistaGeneral;
            if (parent != null)
                parent.openChildForm(new Vista_OrdenesModulo());
        }

        private void BtnCompletarTodo_Click(object sender, EventArgs e)
        {
            try
            {
                foreach (var detalle in _detalles)
                    AplicarCantidadRecibida(detalle, detalle.cantidad_pedida);

                dgvDetalles.Refresh();
                ToastNotification.ShowSuccess("Se completaron todas las cantidades pedidas.");
            }
            catch (Exception ex)
            {
                ToastNotification.ShowWarning("No se pudo completar la recepción: " + ex.Message);
            }
        }

        private void BtnAplicarSiguiente_Click(object sender, EventArgs e)
        {
            AplicarCantidadRapida(true);
        }

        private void BtnCompletarActual_Click(object sender, EventArgs e)
        {
            if (!TryObtenerDetalleActual(out var detalle))
                return;

            try
            {
                AplicarCantidadRecibida(detalle, detalle.cantidad_pedida);
                dgvDetalles.Refresh();
                SeleccionarSiguientePendiente(_indiceActual + 1, true);
            }
            catch (Exception ex)
            {
                ToastNotification.ShowWarning(ex.Message);
            }
        }

        private void TxtCantidadRapida_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != ',' && e.KeyChar != '.')
                e.Handled = true;
        }

        private void TxtCantidadRapida_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter)
                return;

            e.Handled = true;
            e.SuppressKeyPress = true;
            AplicarCantidadRapida(true);
        }

        private void AplicarCantidadRecibida(OrdenCompraDetalle detalle, decimal recibida)
        {
            if (recibida < detalle.cantidad_recibida_anterior)
                throw new Exception("No se puede reducir una cantidad ya recibida.");

            if (recibida < 0)
                throw new Exception("No se permiten cantidades negativas.");

            if (recibida > detalle.cantidad_pedida)
                throw new Exception("La cantidad recibida no puede superar a la pedida.");

            detalle.cantidad_recibida = recibida;
            detalle.cantidad_faltante = detalle.cantidad_pedida - detalle.cantidad_recibida;
        }

        private void AplicarCantidadRapida(bool avanzar)
        {
            if (!TryObtenerDetalleActual(out var detalle))
                return;

            decimal cantidadASumar;
            if (!decimal.TryParse((txtCantidadRapida.Text ?? "").Trim().Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out cantidadASumar) || cantidadASumar <= 0)
            {
                ToastNotification.ShowWarning("Ingresá una cantidad mayor a cero para sumar.");
                txtCantidadRapida.Focus();
                txtCantidadRapida.SelectAll();
                return;
            }

            try
            {
                // Sumar la cantidad ingresada al stock ya recibido (acumulativo)
                decimal nuevoTotal = detalle.cantidad_recibida + cantidadASumar;
                AplicarCantidadRecibida(detalle, nuevoTotal);
                dgvDetalles.Refresh();

                if (avanzar)
                    SeleccionarSiguientePendiente(_indiceActual + 1, true);
                else
                    SincronizarCargaRapida();
            }
            catch (Exception ex)
            {
                ToastNotification.ShowWarning(ex.Message);
                txtCantidadRapida.Focus();
                txtCantidadRapida.SelectAll();
            }
        }

        private bool TryObtenerDetalleActual(out OrdenCompraDetalle detalle)
        {
            detalle = null;
            if (_indiceActual < 0 || _indiceActual >= _detalles.Count)
            {
                ToastNotification.ShowWarning("No hay un material seleccionado para carga rápida.");
                return false;
            }

            detalle = _detalles[_indiceActual];
            return detalle != null;
        }

        private void SeleccionarSiguientePendiente(int indiceDesde, bool enfocarCantidadRapida = false)
        {
            if (_detalles.Count == 0)
            {
                _indiceActual = -1;
                SincronizarCargaRapida();
                return;
            }

            for (int i = Math.Max(0, indiceDesde); i < _detalles.Count; i++)
            {
                if (_detalles[i].cantidad_faltante > 0)
                {
                    SeleccionarFilaTrabajo(i, enfocarCantidadRapida);
                    return;
                }
            }

            SeleccionarFilaTrabajo(Math.Min(Math.Max(0, _detalles.Count - 1), _detalles.Count - 1), enfocarCantidadRapida);
        }

        private void SeleccionarFilaTrabajo(int indice, bool enfocarCantidadRapida = false)
        {
            if (indice < 0 || indice >= dgvDetalles.Rows.Count)
                return;

            _indiceActual = indice;
            dgvDetalles.ClearSelection();
            dgvDetalles.Rows[indice].Selected = true;
            dgvDetalles.CurrentCell = dgvDetalles.Rows[indice].Cells["colRecibida"];
            SincronizarCargaRapida();

            if (enfocarCantidadRapida && txtCantidadRapida.Enabled)
            {
                txtCantidadRapida.Focus();
                txtCantidadRapida.SelectAll();
            }
        }

        private void SincronizarCargaRapida()
        {
            if (_indiceActual < 0 || _indiceActual >= _detalles.Count)
            {
                lblMaterialActual.Text = "Material actual: sin selección";
                lblPendienteActual.Text = "Pendiente: -";
                txtCantidadRapida.Text = "";
                return;
            }

            var detalle = _detalles[_indiceActual];
            lblMaterialActual.Text = "Material actual: " + detalle.material_nombre;
            lblPendienteActual.Text = "Pedida: " + FormatearCantidad(detalle.cantidad_pedida)
                + "  |  Recibida: " + FormatearCantidad(detalle.cantidad_recibida)
                + "  |  Faltante: " + FormatearCantidad(detalle.cantidad_faltante);
            // El input empieza vacío porque ahora es acumulativo (suma al recibido actual)
            txtCantidadRapida.Text = "";
        }

        private static string FormatearCantidad(decimal cantidad)
        {
            return cantidad.ToString("0.##", CultureInfo.CurrentCulture);
        }
    }
}
