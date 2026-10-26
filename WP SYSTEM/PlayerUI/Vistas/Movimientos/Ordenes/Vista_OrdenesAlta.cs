using Guna.UI2.WinForms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using WP_Inventario.Datos;
using WP_Inventario.Modelos;
using WP_Inventario.Servicios;

namespace WP_Inventario.Vistas.Movimientos.Ordenes
{
    public class Vista_OrdenesAlta : Form
    {
        private readonly OrdenCompraDatos _ordenDatos = new OrdenCompraDatos();
        private readonly ProveedorDatos _proveedorDatos = new ProveedorDatos();
        private readonly MaterialDatos _materialDatos = new MaterialDatos();
        private readonly BindingList<OrdenCompraDetalle> _detalles = new BindingList<OrdenCompraDetalle>();

        private int _idOrdenEdicion;
        private string _estadoOrdenEdicion;
        private List<Material> _materiales = new List<Material>();
        private Material _materialSeleccionado;
        private Panel _panelResultados;
        private Timer _timerDebounce;
        private bool _isProgrammaticChange;

        private Guna2TextBox txtNroOrden;
        private Guna2TextBox txtNroRemito;
        private Guna2DateTimePicker dtpFecha;
        private Guna2ComboBox cmbProveedor;
        private Guna2TextBox txtDestino;
        private Guna2TextBox txtCostoFlete;
        private Guna2TextBox txtObservaciones;
        private Guna2TextBox txtBuscadorMaterial;
        private Guna2TextBox txtCantidad;
        private Guna2TextBox txtObservacionItem;
        private Guna2DataGridView dgvDetalles;
        private Label lblTitulo;
        private Guna2Button btnGuardar;
        private Guna2Button btnAgregar;
        private Guna2Button btnLimpiar;
        private Guna2TextBox txtEstado;
        public Action<int> OnOrdenGuardada { get; set; }

        public Vista_OrdenesAlta()
        {
            InitializeComponent();
            CargarCombos();
            PrepararNuevaOrden();
        }

        public void PrepararNuevaOrden()
        {
            _idOrdenEdicion = 0;
            _estadoOrdenEdicion = OrdenCompraEstados.Pendiente;
            lblTitulo.Text = "Nueva Orden de Compra";
            btnGuardar.Text = "Guardar Orden";
            if (txtEstado != null)
                txtEstado.Text = OrdenCompraEstados.Pendiente;
            dtpFecha.Value = DateTime.Now;
            txtNroOrden.Text = "";
            txtNroRemito.Text = "";
            txtCostoFlete.Text = "";
            txtObservaciones.Text = "";
            txtObservacionItem.Text = "";
            txtCantidad.Text = "";
            cmbProveedor.SelectedIndex = -1;
            _materialSeleccionado = null;
            _isProgrammaticChange = true;
            txtBuscadorMaterial.Text = "";
            _isProgrammaticChange = false;
            txtDestino.Text = _ordenDatos.ObtenerNombreDestinoGalpon();
            _detalles.Clear();
            AplicarModoDetalleEditable(true);
            OcultarResultados();
        }

        public void SetEditMode(int idOrden)
        {
            _idOrdenEdicion = idOrden;
            CargarOrden(idOrden);
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            BackColor = OrdenesUi.FondoForm;

            var scroll = new Panel();
            scroll.Dock = DockStyle.Fill;
            scroll.AutoScroll = true;
            scroll.BackColor = OrdenesUi.FondoForm;

            var root = new FlowLayoutPanel();
            root.Dock = DockStyle.Top;
            root.FlowDirection = FlowDirection.TopDown;
            root.WrapContents = false;
            root.AutoSize = true;
            root.Padding = new Padding(18, 18, 18, 24);

            var header = new Panel();
            header.Width = 1020;
            header.Height = 58;
            header.Margin = new Padding(0, 0, 0, 10);

            lblTitulo = new Label();
            lblTitulo.Text = "Nueva Orden de Compra";
            lblTitulo.Font = new Font("Bahnschrift SemiBold", 17F, FontStyle.Bold);
            lblTitulo.ForeColor = Color.White;
            lblTitulo.Dock = DockStyle.Fill;
            lblTitulo.TextAlign = ContentAlignment.MiddleLeft;
            header.Controls.Add(lblTitulo);

            var cardCabecera = new Panel();
            cardCabecera.BackColor = OrdenesUi.FondoPanel;
            cardCabecera.Width = 1020;
            cardCabecera.Height = 300;
            cardCabecera.Padding = new Padding(18);
            cardCabecera.Margin = new Padding(0, 0, 0, 10);

            var tlCabecera = new TableLayoutPanel();
            tlCabecera.Dock = DockStyle.Fill;
            tlCabecera.ColumnCount = 4;
            tlCabecera.RowCount = 3;
            tlCabecera.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlCabecera.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlCabecera.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlCabecera.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlCabecera.RowStyles.Add(new RowStyle(SizeType.Absolute, 74F));
            tlCabecera.RowStyles.Add(new RowStyle(SizeType.Absolute, 74F));
            tlCabecera.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            cardCabecera.Controls.Add(tlCabecera);

            txtNroOrden = OrdenesUi.CrearTextBox();
            txtNroRemito = OrdenesUi.CrearTextBox();
            txtCostoFlete = OrdenesUi.CrearTextBox();
            txtObservaciones = OrdenesUi.CrearTextBox(true);
            txtDestino = OrdenesUi.CrearTextBox(false, true);
            txtDestino.PlaceholderText = "Galpón";
            cmbProveedor = OrdenesUi.CrearCombo();
            txtBuscadorMaterial = OrdenesUi.CrearTextBox();
            dtpFecha = new Guna2DateTimePicker();
            dtpFecha.FillColor = OrdenesUi.FondoInput;
            dtpFecha.BorderColor = OrdenesUi.Borde;
            dtpFecha.Checked = true;
            dtpFecha.Format = DateTimePickerFormat.Short;
            dtpFecha.Font = new Font("Segoe UI", 10.5F);
            dtpFecha.ForeColor = Color.White;
            dtpFecha.Dock = DockStyle.Top;
            dtpFecha.Height = 36;

            txtCostoFlete.TextAlign = HorizontalAlignment.Right;
            txtCostoFlete.PlaceholderText = "0,00";
            txtObservaciones.PlaceholderText = "Observaciones generales de la orden";
            txtObservaciones.ScrollBars = ScrollBars.Vertical;
            txtObservaciones.Height = 96;
            txtNroOrden.PlaceholderText = "Ej. OC-1024";
            txtNroRemito.PlaceholderText = "Opcional";
            cmbProveedor.AutoCompleteSource = AutoCompleteSource.ListItems;
            cmbProveedor.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            txtBuscadorMaterial.PlaceholderText = "Buscar material...";

            AgregarCampoCabecera(tlCabecera, 0, "Nro. Orden", txtNroOrden);
            AgregarCampoCabecera(tlCabecera, 1, "Fecha", dtpFecha);
            AgregarCampoCabecera(tlCabecera, 2, "Proveedor", cmbProveedor);
            AgregarCampoCabecera(tlCabecera, 3, "Destino", txtDestino);
            AgregarCampoCabecera(tlCabecera, 4, "Nro. Remito", txtNroRemito);
            AgregarCampoCabecera(tlCabecera, 5, "Costo Flete", txtCostoFlete);
            AgregarCampoCabecera(tlCabecera, 6, "Estado", CrearEstadoSoloLectura());
            AgregarCampoCabeceraSpan(tlCabecera, 2, 0, 4, "Observaciones", txtObservaciones);

            var cardDetalle = new Panel();
            cardDetalle.BackColor = OrdenesUi.FondoPanel;
            cardDetalle.Width = 1020;
            cardDetalle.Height = 114;
            cardDetalle.Padding = new Padding(18);
            cardDetalle.Margin = new Padding(0, 0, 0, 10);

            var tlDetalle = new TableLayoutPanel();
            tlDetalle.Dock = DockStyle.Fill;
            tlDetalle.ColumnCount = 4;
            tlDetalle.RowCount = 2;
            tlDetalle.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 56F));
            tlDetalle.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 115F));
            tlDetalle.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24F));
            tlDetalle.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170F));
            tlDetalle.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            tlDetalle.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            cardDetalle.Controls.Add(tlDetalle);

            txtCantidad = OrdenesUi.CrearTextBox();
            txtCantidad.PlaceholderText = "0";
            txtCantidad.TextAlign = HorizontalAlignment.Right;
            txtObservacionItem = OrdenesUi.CrearTextBox();
            txtObservacionItem.PlaceholderText = "Observación opcional por material";

            btnAgregar = OrdenesUi.CrearBoton("Agregar Material", true);
            btnAgregar.Dock = DockStyle.Fill;
            btnAgregar.Click += BtnAgregar_Click;

            tlDetalle.Controls.Add(OrdenesUi.CrearLabel("Material"), 0, 0);
            tlDetalle.Controls.Add(OrdenesUi.CrearLabel("Cantidad pedida"), 1, 0);
            tlDetalle.Controls.Add(OrdenesUi.CrearLabel("Observación ítem"), 2, 0);
            tlDetalle.Controls.Add(new Label(), 3, 0);
            tlDetalle.Controls.Add(txtBuscadorMaterial, 0, 1);
            tlDetalle.Controls.Add(txtCantidad, 1, 1);
            tlDetalle.Controls.Add(txtObservacionItem, 2, 1);
            tlDetalle.Controls.Add(btnAgregar, 3, 1);

            var cardGrid = new Panel();
            cardGrid.BackColor = OrdenesUi.FondoPanel;
            cardGrid.Width = 1020;
            cardGrid.Height = 340;
            cardGrid.Padding = new Padding(18);
            cardGrid.Margin = new Padding(0, 0, 0, 10);

            dgvDetalles = new Guna2DataGridView();
            dgvDetalles.Dock = DockStyle.Fill;
            dgvDetalles.AutoGenerateColumns = false;
            OrdenesUi.AplicarEstiloGrid(dgvDetalles);
            dgvDetalles.DataSource = _detalles;
            dgvDetalles.CellEndEdit += DgvDetalles_CellEndEdit;
            dgvDetalles.CellContentClick += DgvDetalles_CellContentClick;

            dgvDetalles.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colMaterial",
                HeaderText = "Material",
                DataPropertyName = "material_nombre",
                ReadOnly = true,
                FillWeight = 42
            });
            dgvDetalles.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colCantidadPedida",
                HeaderText = "Cantidad pedida",
                DataPropertyName = "cantidad_pedida",
                FillWeight = 16,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Format = "0.##",
                    Alignment = DataGridViewContentAlignment.MiddleRight
                }
            });
            dgvDetalles.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colObservacion",
                HeaderText = "Observación",
                DataPropertyName = "observacion_item",
                FillWeight = 30
            });
            dgvDetalles.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "colCambiarMaterial",
                HeaderText = "",
                Text = "Cambiar",
                UseColumnTextForButtonValue = true,
                FillWeight = 12
            });
            dgvDetalles.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "colEliminar",
                HeaderText = "",
                Text = "Quitar",
                UseColumnTextForButtonValue = true,
                FillWeight = 12
            });
            cardGrid.Controls.Add(dgvDetalles);

            var footer = new Panel();
            footer.Width = 1020;
            footer.Height = 46;
            footer.Margin = new Padding(0);

            btnLimpiar = OrdenesUi.CrearBoton("Limpiar");
            btnLimpiar.Location = new Point(0, 5);
            btnLimpiar.Click += (s, e) => PrepararNuevaOrden();

            btnGuardar = OrdenesUi.CrearBoton("Guardar Orden", true);
            btnGuardar.Location = new Point(840, 5);
            btnGuardar.Click += BtnGuardar_Click;

            footer.Controls.Add(btnLimpiar);
            footer.Controls.Add(btnGuardar);

            root.Controls.Add(header);
            root.Controls.Add(cardCabecera);
            root.Controls.Add(cardDetalle);
            root.Controls.Add(cardGrid);
            root.Controls.Add(footer);
            scroll.Controls.Add(root);
            Controls.Add(scroll);

            CrearPanelResultados();
            ConfigurarBusquedaMaterial();
            ResumeLayout(false);
        }

        private Control CrearEstadoSoloLectura()
        {
            txtEstado = OrdenesUi.CrearTextBox(false, true);
            txtEstado.Text = OrdenCompraEstados.Pendiente;
            return txtEstado;
        }

        private void AgregarCampoCabecera(TableLayoutPanel tl, int indice, string etiqueta, Control control)
        {
            int col = indice % 4;
            int row = indice / 4;

            var pnl = new Panel();
            pnl.Dock = DockStyle.Fill;
            pnl.Margin = new Padding(8, 0, 8, 0);

            var lbl = OrdenesUi.CrearLabel(etiqueta);
            lbl.Dock = DockStyle.Top;
            control.Dock = DockStyle.Top;
            pnl.Controls.Add(control);
            pnl.Controls.Add(lbl);

            tl.Controls.Add(pnl, col, row);
        }

        private void AgregarCampoCabeceraSpan(TableLayoutPanel tl, int row, int col, int colSpan, string etiqueta, Control control)
        {
            var pnl = new Panel();
            pnl.Dock = DockStyle.Fill;
            pnl.Margin = new Padding(8, 0, 8, 0);

            var lbl = OrdenesUi.CrearLabel(etiqueta);
            lbl.Dock = DockStyle.Top;
            control.Dock = DockStyle.Fill;
            pnl.Controls.Add(control);
            pnl.Controls.Add(lbl);

            tl.Controls.Add(pnl, col, row);
            tl.SetColumnSpan(pnl, colSpan);
        }

        private void CargarCombos()
        {
            cmbProveedor.DataSource = _proveedorDatos.GetProveedor();
            cmbProveedor.DisplayMember = "nombre";
            cmbProveedor.ValueMember = "id";
            cmbProveedor.SelectedIndex = -1;

            _materiales = _materialDatos.obtenerMateriales();
        }

        private void BtnAgregar_Click(object sender, EventArgs e)
        {
            try
            {
                var material = _materialSeleccionado;
                if (material == null)
                    throw new Exception("Seleccione un material.");

                decimal cantidad = ParseCantidad(txtCantidad.Text, "La cantidad pedida");

                if (_detalles.Any(x => x.id_material == material.id))
                    throw new Exception("Ese material ya fue agregado a la orden.");

                _detalles.Add(new OrdenCompraDetalle
                {
                    id_material = material.id,
                    material_nombre = material.nombre,
                    cantidad_pedida = cantidad,
                    cantidad_recibida = 0,
                    cantidad_faltante = cantidad,
                    observacion_item = txtObservacionItem.Text.Trim()
                });

                _materialSeleccionado = null;
                _isProgrammaticChange = true;
                txtBuscadorMaterial.Text = "";
                _isProgrammaticChange = false;
                txtCantidad.Text = "";
                txtObservacionItem.Text = "";
                OcultarResultados();
            }
            catch (Exception ex)
            {
                ToastNotification.ShowWarning(ex.Message);
            }
        }

        private void DgvDetalles_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            string nombreColumna = dgvDetalles.Columns[e.ColumnIndex].Name;
            if (!OrdenCompraEstados.EsEditable(_estadoOrdenEdicion))
            {
                if (nombreColumna == "colCambiarMaterial")
                {
                    CambiarMaterialImpactado(e.RowIndex);
                    return;
                }

                if (nombreColumna == "colEliminar")
                    ToastNotification.ShowWarning("Los materiales de una orden recibida se revisan desde Recepción.");

                return;
            }

            if (nombreColumna != "colEliminar")
                return;

            var detalle = dgvDetalles.Rows[e.RowIndex].DataBoundItem as OrdenCompraDetalle;
            if (detalle != null)
                _detalles.Remove(detalle);
        }

        private void CambiarMaterialImpactado(int rowIndex)
        {
            var detalle = dgvDetalles.Rows[rowIndex].DataBoundItem as OrdenCompraDetalle;
            if (detalle == null)
                return;

            using (var modal = new ModalCambioMaterialOrden(_materiales, detalle.id_material))
            {
                if (modal.ShowDialog(this) != DialogResult.OK || modal.MaterialSeleccionado == null)
                    return;

                var nuevoMaterial = modal.MaterialSeleccionado;
                if (nuevoMaterial.id == detalle.id_material)
                    return;

                string impacto = detalle.cantidad_recibida > 0
                    ? "Se descontará -" + FormatearCantidad(detalle.cantidad_recibida) + " de:\n" +
                      detalle.material_nombre + "\n\n" +
                      "Y se sumará +" + FormatearCantidad(detalle.cantidad_recibida) + " a:\n" +
                      nuevoMaterial.nombre
                    : "La línea todavía no tiene cantidades recibidas. Solo se cambiará el material de la orden.";

                var confirmar = MessageBox.Show(
                    impacto + "\n\n¿Está seguro de corregir este material?",
                    "Cambiar material de orden",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2);

                if (confirmar != DialogResult.Yes)
                    return;

                try
                {
                    _ordenDatos.CambiarMaterialDetalle(_idOrdenEdicion, detalle.id_detalle, nuevoMaterial.id);
                    ToastNotification.ShowSuccess("Material corregido y stock ajustado.");
                    _materiales = _materialDatos.obtenerMateriales();
                    CargarOrden(_idOrdenEdicion);
                }
                catch (Exception ex)
                {
                    ToastNotification.ShowError("No se pudo cambiar el material: " + ex.Message);
                }
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
                if (!OrdenCompraEstados.EsEditable(_estadoOrdenEdicion))
                {
                    dgvDetalles.CancelEdit();
                    return;
                }

                if (dgvDetalles.Columns[e.ColumnIndex].Name == "colCantidadPedida")
                {
                    detalle.cantidad_pedida = ParseCantidad(Convert.ToString(dgvDetalles.Rows[e.RowIndex].Cells[e.ColumnIndex].Value), "La cantidad pedida");
                    detalle.cantidad_faltante = detalle.cantidad_pedida;
                }
                else if (dgvDetalles.Columns[e.ColumnIndex].Name == "colObservacion")
                {
                    detalle.observacion_item = Convert.ToString(dgvDetalles.Rows[e.RowIndex].Cells[e.ColumnIndex].Value) ?? "";
                }
            }
            catch (Exception ex)
            {
                ToastNotification.ShowWarning(ex.Message);
                dgvDetalles.CancelEdit();
            }

            dgvDetalles.Refresh();
        }

        private void BtnGuardar_Click(object sender, EventArgs e)
        {
            if (SessionManager.Role != UserRole.Admin)
            {
                ToastNotification.ShowWarning("Solo un usuario Admin puede guardar órdenes.");
                return;
            }

            try
            {
                var proveedor = cmbProveedor.SelectedItem as Proveedor;
                if (proveedor == null)
                    throw new Exception("Seleccione un proveedor.");

                if (_detalles.Count == 0)
                    throw new Exception("Agregue al menos un material.");

                decimal costoFlete = ParseDecimal(txtCostoFlete.Text);
                var orden = new OrdenCompra
                {
                    id_orden_compra = _idOrdenEdicion,
                    nro_orden = txtNroOrden.Text.Trim(),
                    nro_remito = txtNroRemito.Text.Trim(),
                    fecha = dtpFecha.Value.Date,
                    id_proveedor = proveedor.id,
                    costoflete = costoFlete,
                    observaciones = txtObservaciones.Text.Trim(),
                    usuario_creacion = SessionManager.Username,
                    Detalles = _detalles.ToList()
                };

                int idOrden = OrdenCompraEstados.EsEditable(_estadoOrdenEdicion)
                    ? _ordenDatos.GuardarOrden(orden)
                    : _ordenDatos.ActualizarCabeceraOrden(orden);

                ToastNotification.ShowSuccess(_idOrdenEdicion > 0 ? "Orden actualizada." : "Orden creada en estado Pendiente.");
                OnOrdenGuardada?.Invoke(idOrden);
                if (OnOrdenGuardada != null)
                {
                    PrepararNuevaOrden();
                    return;
                }

                PrepararNuevaOrden();
                var parent = TopLevelControl as VistaGeneral;
                if (parent != null)
                {
                    parent.RefrescarOrdenesSiAbierto();
                    parent.OpenOrdenForm(idOrden);
                }
            }
            catch (Exception ex)
            {
                ToastNotification.ShowError("No se pudo guardar la orden: " + ex.Message);
            }
        }

        private void CargarOrden(int idOrden)
        {
            try
            {
                var orden = _ordenDatos.ObtenerOrdenPorId(idOrden);
                if (orden == null)
                    throw new Exception("La orden no existe.");

                if (!OrdenCompraEstados.PermiteEditarCabecera(orden.estado))
                    throw new Exception("La orden seleccionada no permite edición.");

                _estadoOrdenEdicion = orden.estado;
                bool detalleEditable = OrdenCompraEstados.EsEditable(orden.estado);

                lblTitulo.Text = detalleEditable ? "Editar Orden de Compra" : "Editar Cabecera de Orden";
                btnGuardar.Text = detalleEditable ? "Actualizar Orden" : "Actualizar Cabecera";
                if (txtEstado != null)
                    txtEstado.Text = orden.estado;
                dtpFecha.Value = orden.fecha;
                txtNroOrden.Text = orden.nro_orden;
                txtNroRemito.Text = orden.nro_remito;
                txtDestino.Text = orden.destino_nombre;
                txtCostoFlete.Text = orden.costoflete == 0 ? "" : orden.costoflete.ToString("0.##", CultureInfo.InvariantCulture).Replace('.', ',');
                txtObservaciones.Text = orden.observaciones;
                cmbProveedor.SelectedValue = orden.id_proveedor;
                _materialSeleccionado = null;
                _isProgrammaticChange = true;
                txtBuscadorMaterial.Text = "";
                _isProgrammaticChange = false;
                _detalles.Clear();
                foreach (var detalle in orden.Detalles)
                    _detalles.Add(detalle);

                AplicarModoDetalleEditable(detalleEditable);
            }
            catch (Exception ex)
            {
                ToastNotification.ShowError("No se pudo cargar la orden: " + ex.Message);
            }
        }

        private void AplicarModoDetalleEditable(bool editable)
        {
            txtBuscadorMaterial.Enabled = editable;
            txtCantidad.Enabled = editable;
            txtObservacionItem.Enabled = editable;
            if (btnAgregar != null)
                btnAgregar.Enabled = editable;

            if (dgvDetalles == null || dgvDetalles.Columns.Count == 0)
                return;

            dgvDetalles.ReadOnly = !editable;
            dgvDetalles.Columns["colMaterial"].ReadOnly = true;
            dgvDetalles.Columns["colCantidadPedida"].ReadOnly = !editable;
            dgvDetalles.Columns["colObservacion"].ReadOnly = !editable;
            dgvDetalles.Columns["colCambiarMaterial"].ReadOnly = editable;
            dgvDetalles.Columns["colCambiarMaterial"].Visible = !editable;
            dgvDetalles.Columns["colEliminar"].ReadOnly = !editable;
        }

        private decimal ParseCantidad(string texto, string nombreCampo)
        {
            decimal valor;
            if (!decimal.TryParse((texto ?? "").Trim().Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out valor) || valor <= 0)
                throw new Exception(nombreCampo + " debe ser un número mayor a cero.");

            return valor;
        }

        private static string FormatearCantidad(decimal cantidad)
        {
            return cantidad.ToString("0.##", CultureInfo.CurrentCulture);
        }

        private decimal ParseDecimal(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return 0;

            decimal valor;
            if (!decimal.TryParse(texto.Trim().Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out valor))
                throw new Exception("El costo de flete no tiene un formato válido.");

            if (valor < 0)
                throw new Exception("El costo de flete no puede ser negativo.");

            return valor;
        }

        private void CrearPanelResultados()
        {
            _panelResultados = new Panel
            {
                Size = new Size(516, 0),
                BackColor = Color.FromArgb(25, 30, 28),
                Visible = false,
                AutoScroll = true
            };

            Controls.Add(_panelResultados);
            _panelResultados.BringToFront();
        }

        private void ConfigurarBusquedaMaterial()
        {
            _timerDebounce = new Timer { Interval = 300 };
            _timerDebounce.Tick += (s, e) =>
            {
                _timerDebounce.Stop();
                EjecutarBusquedaMaterial(txtBuscadorMaterial.Text.Trim());
            };

            txtBuscadorMaterial.TextChanged += TxtBuscadorMaterial_TextChanged;
            txtBuscadorMaterial.KeyDown += TxtBuscadorMaterial_KeyDown;
            txtBuscadorMaterial.GotFocus += (s, e) => EjecutarBusquedaMaterial(txtBuscadorMaterial.Text.Trim());
            txtBuscadorMaterial.Click += (s, e) => EjecutarBusquedaMaterial(txtBuscadorMaterial.Text.Trim());
            txtBuscadorMaterial.LostFocus += (s, e) =>
            {
                var timer = new Timer { Interval = 200 };
                timer.Tick += (ts, te) =>
                {
                    timer.Stop();
                    timer.Dispose();
                    if (!txtBuscadorMaterial.Focused && _panelResultados != null && !_panelResultados.ContainsFocus)
                    {
                        Point mousePos = Control.MousePosition;
                        Rectangle pnlRect = _panelResultados.RectangleToScreen(_panelResultados.ClientRectangle);
                        if (!pnlRect.Contains(mousePos))
                            OcultarResultados();
                    }
                };
                timer.Start();
            };
        }

        private void PosicionarPanelResultados()
        {
            var screenPoint = txtBuscadorMaterial.PointToScreen(Point.Empty);
            var parentPoint = PointToClient(screenPoint);
            _panelResultados.Location = new Point(parentPoint.X, parentPoint.Y + txtBuscadorMaterial.Height + 5);
        }

        private void TxtBuscadorMaterial_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticChange)
                return;

            _timerDebounce.Stop();
            string nombreEsperado = _materialSeleccionado != null ? _materialSeleccionado.nombre : "";
            if (_materialSeleccionado != null && txtBuscadorMaterial.Text != nombreEsperado)
                _materialSeleccionado = null;

            if (txtBuscadorMaterial.Text.Trim().Length < 2)
            {
                EjecutarBusquedaMaterial("");
                return;
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
            else if (e.KeyCode == Keys.Enter && _materialSeleccionado != null)
            {
                txtCantidad.Focus();
                e.SuppressKeyPress = true;
            }
        }

        private class ItemBuscador
        {
            public Material Mat { get; set; }
            public string NombreMostrar { get; set; }
            public string InfoMostrar { get; set; }
            public Color ColorAcento { get; set; }
            public string Icono { get; set; }
        }

        private void EjecutarBusquedaMaterial(string query)
        {
            string qLower = query?.ToLower() ?? "";
            if (qLower.Length < 2)
            {
                MostrarResultados(new List<ItemBuscador>(), "MIN_CHARS");
                return;
            }

            var items = _materiales
                .Where(m => m.nombre != null && m.nombre.ToLower().Contains(qLower))
                .OrderByDescending(m => m.nombre.ToLower().StartsWith(qLower))
                .ThenBy(m => m.nombre)
                .Take(50)
                .Select(m => new ItemBuscador
                {
                    Mat = m,
                    NombreMostrar = m.nombre,
                    InfoMostrar = SessionManager.DebeOcultarCantidadesStockMateriales
                        ? $"Material • {SessionManager.ObtenerEstadoStockMaterial(m.stockActual)}"
                        : $"Material • Stock: {m.stockActual:N0}",
                    ColorAcento = Color.FromArgb(39, 174, 96),
                    Icono = "📦"
                })
                .ToList();

            MostrarResultados(items, query);
        }

        private void MostrarResultados(List<ItemBuscador> resultados, string query)
        {
            _panelResultados.Controls.Clear();

            const int maxPanelHeight = 260;
            const int itemHeight = 48;
            int panelWidth = txtBuscadorMaterial.Width;

            if (resultados.Count == 0)
            {
                string mensaje = query == "MIN_CHARS"
                    ? "Escribí al menos 2 letras para buscar..."
                    : string.IsNullOrEmpty(query)
                        ? "No hay materiales disponibles."
                        : $"Sin resultados para \"{query}\"";

                var lblNoResults = new Label
                {
                    Text = mensaje,
                    Font = new Font("Bahnschrift Light", 10f),
                    ForeColor = Color.FromArgb(120, 120, 120),
                    Size = new Size(panelWidth, 45),
                    TextAlign = ContentAlignment.MiddleCenter,
                    BackColor = Color.Transparent,
                    Dock = DockStyle.Top
                };

                _panelResultados.Controls.Add(lblNoResults);
                _panelResultados.Size = new Size(panelWidth, 50);
                PosicionarPanelResultados();
                _panelResultados.BringToFront();
                _panelResultados.Visible = true;
                return;
            }

            int totalContentHeight = resultados.Count * itemHeight;
            int panelHeight = Math.Min(totalContentHeight, maxPanelHeight);

            for (int i = resultados.Count - 1; i >= 0; i--)
            {
                var panel = CrearItemResultado(resultados[i], panelWidth);
                _panelResultados.Controls.Add(panel);
            }

            _panelResultados.Size = new Size(panelWidth, panelHeight);
            PosicionarPanelResultados();
            _panelResultados.BringToFront();
            _panelResultados.Visible = true;
        }

        private Panel CrearItemResultado(ItemBuscador item, int panelWidth)
        {
            var panel = new Panel
            {
                Size = new Size(panelWidth - 20, 48),
                BackColor = Color.FromArgb(25, 30, 28),
                Dock = DockStyle.Top,
                Cursor = Cursors.Hand,
                Padding = new Padding(10, 0, 10, 0),
                TabStop = true
            };

            var accentBar = new Panel
            {
                Size = new Size(3, 30),
                BackColor = item.ColorAcento,
                Location = new Point(5, 9)
            };

            var lblIcon = new Label
            {
                Text = item.Icono,
                Font = new Font("Segoe UI", 14f),
                Size = new Size(35, 48),
                Location = new Point(15, 0),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };

            var lblName = new Label
            {
                Text = item.NombreMostrar,
                Font = new Font("Bahnschrift", 11f),
                ForeColor = Color.FromArgb(220, 220, 220),
                Size = new Size(panelWidth - 95, 28),
                Location = new Point(55, 3),
                TextAlign = ContentAlignment.BottomLeft,
                BackColor = Color.Transparent,
                AutoEllipsis = true
            };

            var lblType = new Label
            {
                Text = item.InfoMostrar,
                Font = new Font("Bahnschrift Light", 8.5f),
                ForeColor = item.ColorAcento,
                Size = new Size(panelWidth - 95, 18),
                Location = new Point(55, 28),
                TextAlign = ContentAlignment.TopLeft,
                BackColor = Color.Transparent
            };

            panel.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    SeleccionarMaterial(item);
                    e.SuppressKeyPress = true;
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    OcultarResultados();
                    txtBuscadorMaterial.Focus();
                    e.SuppressKeyPress = true;
                }
            };

            foreach (var ctrl in new Control[] { panel, lblIcon, lblName, lblType, accentBar })
            {
                ctrl.MouseEnter += (s, e) => panel.BackColor = Color.FromArgb(35, 45, 40);
                ctrl.MouseLeave += (s, e) => panel.BackColor = Color.FromArgb(25, 30, 28);
                ctrl.Click += (s, e) => SeleccionarMaterial(item);
            }

            panel.Controls.Add(accentBar);
            panel.Controls.Add(lblIcon);
            panel.Controls.Add(lblName);
            panel.Controls.Add(lblType);
            return panel;
        }

        private void SeleccionarMaterial(ItemBuscador item)
        {
            OcultarResultados();
            _isProgrammaticChange = true;
            _materialSeleccionado = item.Mat;
            txtBuscadorMaterial.Text = item.Mat.nombre;
            _isProgrammaticChange = false;
            txtCantidad.Focus();
        }

        private void OcultarResultados()
        {
            if (_panelResultados != null)
            {
                _panelResultados.Visible = false;
                _panelResultados.Controls.Clear();
            }
        }

        private class ModalCambioMaterialOrden : Form
        {
            private readonly Guna2ComboBox cmbMateriales;
            private readonly Guna2Button btnAceptar;
            private readonly Guna2Button btnCancelar;

            public Material MaterialSeleccionado { get; private set; }

            public ModalCambioMaterialOrden(List<Material> materiales, int idMaterialActual)
            {
                Text = "Cambiar material";
                StartPosition = FormStartPosition.CenterParent;
                FormBorderStyle = FormBorderStyle.FixedDialog;
                MaximizeBox = false;
                MinimizeBox = false;
                ClientSize = new Size(560, 170);
                BackColor = OrdenesUi.FondoForm;
                Padding = new Padding(18);

                var lbl = OrdenesUi.CrearLabel("Material correcto");
                lbl.Location = new Point(18, 18);
                lbl.Size = new Size(520, 24);
                Controls.Add(lbl);

                cmbMateriales = OrdenesUi.CrearCombo();
                cmbMateriales.Location = new Point(18, 48);
                cmbMateriales.Size = new Size(524, 36);
                cmbMateriales.AutoCompleteSource = AutoCompleteSource.ListItems;
                cmbMateriales.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                cmbMateriales.DataSource = materiales == null ? new List<Material>() : materiales.ToList();
                cmbMateriales.DisplayMember = "nombre";
                cmbMateriales.ValueMember = "id";
                cmbMateriales.SelectedValue = idMaterialActual;
                Controls.Add(cmbMateriales);

                btnCancelar = OrdenesUi.CrearBoton("Cancelar");
                btnCancelar.Location = new Point(314, 110);
                btnCancelar.Click += (s, e) => DialogResult = DialogResult.Cancel;
                Controls.Add(btnCancelar);

                btnAceptar = OrdenesUi.CrearBoton("Aceptar", true);
                btnAceptar.Location = new Point(440, 110);
                btnAceptar.Click += BtnAceptar_Click;
                Controls.Add(btnAceptar);

                AcceptButton = btnAceptar;
                CancelButton = btnCancelar;
            }

            private void BtnAceptar_Click(object sender, EventArgs e)
            {
                MaterialSeleccionado = cmbMateriales.SelectedItem as Material;
                if (MaterialSeleccionado == null)
                {
                    ToastNotification.ShowWarning("Seleccione el material correcto.");
                    return;
                }

                DialogResult = DialogResult.OK;
            }
        }
    }
}
