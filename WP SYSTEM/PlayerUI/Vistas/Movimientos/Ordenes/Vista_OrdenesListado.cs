using Guna.UI2.WinForms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using WP_Inventario.Datos;
using WP_Inventario.Modelos;
using WP_Inventario.Reportes;
using WP_Inventario.Servicios;

namespace WP_Inventario.Vistas.Movimientos.Ordenes
{
    public class Vista_OrdenesListado : Form, IRefrescable
    {
        private readonly OrdenCompraDatos _ordenDatos = new OrdenCompraDatos();
        private readonly UserRole _rolActual = SessionManager.Role;
        private readonly bool _soloRecepcion;
        private readonly bool _soloHistorial;
        private readonly bool _mostrarNuevaOrden;
        private readonly Action _onNuevaOrden;
        private readonly Action<int> _onEditarOrden;
        private readonly Action<int> _onRevisarOrden;

        private Guna2ComboBox cmbEstado;
        private Guna2DataGridView dgvOrdenes;
        private Guna2DataGridView dgvPreviewMateriales;
        private Guna2Button btnNuevaOrden;
        private Label lblPreviewMateriales;
        private bool _actualizandoPreview;

        public Vista_OrdenesListado(
            bool soloRecepcion = false,
            bool soloHistorial = false,
            bool mostrarNuevaOrden = true,
            Action onNuevaOrden = null,
            Action<int> onEditarOrden = null,
            Action<int> onRevisarOrden = null)
        {
            _soloRecepcion = soloRecepcion;
            _soloHistorial = soloHistorial;
            _mostrarNuevaOrden = mostrarNuevaOrden;
            _onNuevaOrden = onNuevaOrden;
            _onEditarOrden = onEditarOrden;
            _onRevisarOrden = onRevisarOrden;
            InitializeComponent();
            CargarEstados();
            Refrescar();
        }

        public void Refrescar()
        {
            CargarOrdenes();
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
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 74F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, MostrarPreviewMateriales() ? 190F : 0F));
            Controls.Add(root);

            var header = new Panel();
            header.Dock = DockStyle.Fill;
            header.Padding = new Padding(18, 18, 18, 6);
            var lblTitulo = new Label();
            lblTitulo.Text = _soloHistorial ? "Historial de Órdenes" : "Órdenes de Compra";
            lblTitulo.Font = new Font("Bahnschrift SemiBold", 17F, FontStyle.Bold);
            lblTitulo.ForeColor = Color.White;
            lblTitulo.Dock = DockStyle.Fill;
            header.Controls.Add(lblTitulo);
            root.Controls.Add(header, 0, 0);

            var filtros = new Panel();
            filtros.Dock = DockStyle.Fill;
            filtros.Padding = new Padding(18, 0, 18, 10);
            filtros.BackColor = OrdenesUi.FondoPanel;
            root.Controls.Add(filtros, 0, 1);

            var lblEstado = OrdenesUi.CrearLabel("Estado");
            lblEstado.Location = new Point(14, 8);
            filtros.Controls.Add(lblEstado);

            cmbEstado = OrdenesUi.CrearCombo();
            cmbEstado.Location = new Point(18, 32);
            cmbEstado.Size = new Size(260, 36);
            cmbEstado.SelectedIndexChanged += (s, e) => CargarOrdenes();
            filtros.Controls.Add(cmbEstado);

            var btnRefrescar = OrdenesUi.CrearBoton("Actualizar");
            btnRefrescar.Location = new Point(294, 32);
            btnRefrescar.Click += (s, e) => CargarOrdenes();
            filtros.Controls.Add(btnRefrescar);

            var btnImprimirPendientes = OrdenesUi.CrearBoton("Imprimir Pendientes");
            btnImprimirPendientes.Location = new Point(484, 32);
            btnImprimirPendientes.Width = 180;
            btnImprimirPendientes.Click += BtnImprimirPendientes_Click;
            btnImprimirPendientes.Visible = !_soloHistorial;
            filtros.Controls.Add(btnImprimirPendientes);

            btnNuevaOrden = OrdenesUi.CrearBoton("Nueva Orden", true);
            btnNuevaOrden.Location = new Point(822, 32);
            btnNuevaOrden.Click += BtnNuevaOrden_Click;
            btnNuevaOrden.Visible = _rolActual == UserRole.Admin && _mostrarNuevaOrden && !_soloRecepcion;
            filtros.Controls.Add(btnNuevaOrden);

            dgvOrdenes = new Guna2DataGridView();
            dgvOrdenes.Dock = DockStyle.Fill;
            dgvOrdenes.AutoGenerateColumns = false;
            dgvOrdenes.ReadOnly = true;
            dgvOrdenes.CellContentClick += DgvOrdenes_CellContentClick;
            dgvOrdenes.DataBindingComplete += DgvOrdenes_DataBindingComplete;
            dgvOrdenes.SelectionChanged += DgvOrdenes_SelectionChanged;
            OrdenesUi.AplicarEstiloGrid(dgvOrdenes);

            dgvOrdenes.Columns.Add(new DataGridViewTextBoxColumn { Name = "colNroOrden", HeaderText = "Nro. Orden", DataPropertyName = "nro_orden", FillWeight = 16 });
            dgvOrdenes.Columns.Add(new DataGridViewTextBoxColumn { Name = "colFecha", HeaderText = "Fecha", DataPropertyName = "fecha", FillWeight = 12, DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy" } });
            dgvOrdenes.Columns.Add(new DataGridViewTextBoxColumn { Name = "colProveedor", HeaderText = "Proveedor", DataPropertyName = "proveedor_nombre", FillWeight = 20 });
            dgvOrdenes.Columns.Add(new DataGridViewTextBoxColumn { Name = "colRemito", HeaderText = "Remito", DataPropertyName = "nro_remito", FillWeight = 14 });
            dgvOrdenes.Columns.Add(new DataGridViewTextBoxColumn { Name = "colEstado", HeaderText = "Estado", DataPropertyName = "estado", FillWeight = 18 });
            dgvOrdenes.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPedida", HeaderText = "Pedida", DataPropertyName = "total_cantidad_pedida", FillWeight = 10, DefaultCellStyle = CrearEstiloCantidad() });
            dgvOrdenes.Columns.Add(new DataGridViewTextBoxColumn { Name = "colRecibida", HeaderText = "Recibida", DataPropertyName = "total_cantidad_recibida", FillWeight = 10, DefaultCellStyle = CrearEstiloCantidad() });

            var colAbrir = CrearColumnaAccion(
                "colAbrir",
                12,
                Color.FromArgb(31, 84, 58),
                Color.FromArgb(112, 212, 159),
                Color.FromArgb(42, 120, 81));
            colAbrir.Visible = _soloRecepcion || _rolActual == UserRole.Galpon || _rolActual == UserRole.Admin;
            dgvOrdenes.Columns.Add(colAbrir);

            var colEditar = CrearColumnaAccion(
                "colEditar",
                10,
                Color.FromArgb(84, 71, 31),
                Color.FromArgb(244, 213, 128),
                Color.FromArgb(120, 102, 45));
            colEditar.Visible = _rolActual == UserRole.Admin;
            dgvOrdenes.Columns.Add(colEditar);

            var colEliminar = CrearColumnaAccion(
                "colEliminar",
                10,
                Color.FromArgb(92, 40, 40),
                Color.FromArgb(255, 191, 191),
                Color.FromArgb(130, 58, 58));
            colEliminar.Visible = _rolActual == UserRole.Admin;
            dgvOrdenes.Columns.Add(colEliminar);

            root.Controls.Add(dgvOrdenes, 0, 2);

            var preview = CrearPreviewMateriales();
            preview.Visible = MostrarPreviewMateriales();
            root.Controls.Add(preview, 0, 3);
            ResumeLayout(false);
        }

        private bool MostrarPreviewMateriales()
        {
            return _soloRecepcion || _soloHistorial || _rolActual != UserRole.Admin;
        }

        private static DataGridViewCellStyle CrearEstiloCantidad()
        {
            return new DataGridViewCellStyle
            {
                Format = "0.##",
                FormatProvider = CultureInfo.CurrentCulture,
                Alignment = DataGridViewContentAlignment.MiddleRight
            };
        }

        private Control CrearPreviewMateriales()
        {
            var panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.Padding = new Padding(18, 8, 18, 12);
            panel.BackColor = OrdenesUi.FondoPanel;

            lblPreviewMateriales = new Label();
            lblPreviewMateriales.Dock = DockStyle.Top;
            lblPreviewMateriales.Height = 24;
            lblPreviewMateriales.Text = "Materiales de la orden seleccionada";
            lblPreviewMateriales.Font = new Font("Bahnschrift SemiBold", 10.5F, FontStyle.Bold);
            lblPreviewMateriales.ForeColor = Color.White;

            dgvPreviewMateriales = new Guna2DataGridView();
            dgvPreviewMateriales.Dock = DockStyle.Fill;
            dgvPreviewMateriales.AutoGenerateColumns = false;
            dgvPreviewMateriales.ReadOnly = true;
            OrdenesUi.AplicarEstiloGrid(dgvPreviewMateriales);
            dgvPreviewMateriales.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPreviewMaterial", HeaderText = "Material", DataPropertyName = "material_nombre", FillWeight = 42 });
            dgvPreviewMateriales.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPreviewPedida", HeaderText = "Pedida", DataPropertyName = "cantidad_pedida", FillWeight = 14, DefaultCellStyle = CrearEstiloCantidad() });
            dgvPreviewMateriales.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPreviewRecibida", HeaderText = "Recibida", DataPropertyName = "cantidad_recibida", FillWeight = 14, DefaultCellStyle = CrearEstiloCantidad() });
            dgvPreviewMateriales.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPreviewFaltante", HeaderText = "Faltante", DataPropertyName = "cantidad_faltante", FillWeight = 14, DefaultCellStyle = CrearEstiloCantidad() });
            dgvPreviewMateriales.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPreviewObs", HeaderText = "Obs.", DataPropertyName = "observacion_item", FillWeight = 24 });

            panel.Controls.Add(dgvPreviewMateriales);
            panel.Controls.Add(lblPreviewMateriales);
            return panel;
        }

        private void CargarEstados()
        {
            var items = new List<string> { "Todas" };
            items.AddRange(OrdenCompraEstados.Todos);

            if (_soloHistorial)
                items = new List<string> { "Todas", OrdenCompraEstados.RecibidaParcial, OrdenCompraEstados.RecibidaCompleta };
            else if (_soloRecepcion || _rolActual != UserRole.Admin)
                items = new List<string> { "Todas", OrdenCompraEstados.Pendiente, OrdenCompraEstados.RecibidaParcial };
            else
                items = new List<string> { "Todas", OrdenCompraEstados.Pendiente, OrdenCompraEstados.RecibidaParcial };

            cmbEstado.DataSource = items;
            cmbEstado.SelectedIndex = 0;
        }

        private void CargarOrdenes()
        {
            try
            {
                string estado = cmbEstado.SelectedItem as string;
                if (estado == "Todas")
                    estado = null;

                bool soloAbiertasGalpon = _soloRecepcion || _rolActual != UserRole.Admin;
                bool excluirCompletas = !_soloHistorial;
                var lista = _ordenDatos.ObtenerOrdenes(estado, soloAbiertasGalpon, _soloHistorial, excluirCompletas);
                _actualizandoPreview = true;
                dgvOrdenes.DataSource = lista;
                _actualizandoPreview = false;
                AplicarAccionesGrilla();
                ActualizarPreviewMateriales();
            }
            catch (Exception ex)
            {
                _actualizandoPreview = false;
                ToastNotification.ShowError("No se pudo cargar el listado de órdenes: " + ex.Message);
            }
        }

        private void DgvOrdenes_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            AplicarAccionesGrilla();
        }

        private void AplicarAccionesGrilla()
        {
            foreach (DataGridViewRow row in dgvOrdenes.Rows)
            {
                var orden = row.DataBoundItem as OrdenCompra;
                if (orden == null)
                    continue;

                row.Cells["colAbrir"].Value = OrdenCompraEstados.PermiteRecepcion(orden.estado) ? "Revisar" : "Ver";
                row.Cells["colEditar"].Value = OrdenCompraEstados.PermiteEditarCabecera(orden.estado) ? "Editar" : "";
                row.Cells["colEliminar"].Value = OrdenCompraEstados.EsEditable(orden.estado) ? "Eliminar" : "";
            }

            dgvOrdenes.Invalidate();
            dgvOrdenes.Refresh();
        }

        private void DgvOrdenes_SelectionChanged(object sender, EventArgs e)
        {
            if (_actualizandoPreview)
                return;

            ActualizarPreviewMateriales();
        }

        private void ActualizarPreviewMateriales()
        {
            if (dgvPreviewMateriales == null || !dgvPreviewMateriales.Visible)
                return;

            var ordenResumen = dgvOrdenes.CurrentRow == null ? null : dgvOrdenes.CurrentRow.DataBoundItem as OrdenCompra;
            if (ordenResumen == null)
            {
                dgvPreviewMateriales.DataSource = null;
                lblPreviewMateriales.Text = "Materiales de la orden seleccionada";
                return;
            }

            try
            {
                var orden = _ordenDatos.ObtenerOrdenPorId(ordenResumen.id_orden_compra);
                dgvPreviewMateriales.DataSource = orden == null ? null : orden.Detalles;
                lblPreviewMateriales.Text = "Materiales de " + ordenResumen.nro_orden + " - " + ordenResumen.proveedor_nombre;
            }
            catch (Exception ex)
            {
                dgvPreviewMateriales.DataSource = null;
                lblPreviewMateriales.Text = "No se pudo cargar la vista previa: " + ex.Message;
            }
        }

        private DataGridViewButtonColumn CrearColumnaAccion(string name, float fillWeight, Color backColor, Color foreColor, Color selectionColor)
        {
            return new DataGridViewButtonColumn
            {
                Name = name,
                HeaderText = "",
                FillWeight = fillWeight,
                FlatStyle = FlatStyle.Flat,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = backColor,
                    ForeColor = foreColor,
                    SelectionBackColor = selectionColor,
                    SelectionForeColor = Color.White,
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Padding = new Padding(6, 3, 6, 3),
                    Font = new Font("Bahnschrift SemiBold", 9.25F, FontStyle.Bold)
                }
            };
        }

        private void BtnNuevaOrden_Click(object sender, EventArgs e)
        {
            if (_onNuevaOrden != null)
            {
                _onNuevaOrden();
                return;
            }

            var parent = TopLevelControl as VistaGeneral;
            if (parent != null)
                parent.OpenOrdenForm(0);
        }

        private void BtnImprimirPendientes_Click(object sender, EventArgs e)
        {
            try
            {
                var pdf = new OrdenesPendientesPDF();
                
                string directorio = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SteeLog", "Reportes");
                if (!Directory.Exists(directorio))
                    Directory.CreateDirectory(directorio);

                string rutaDestino = Path.Combine(directorio, pdf.SugerirNombreArchivo());
                
                pdf.Generar(rutaDestino);
                
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                {
                    FileName = rutaDestino,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                ToastNotification.ShowError("Error al generar el reporte: " + ex.Message);
            }
        }

        private void DgvOrdenes_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;

            var orden = dgvOrdenes.Rows[e.RowIndex].DataBoundItem as OrdenCompra;
            if (orden == null)
                return;

            string columnName = dgvOrdenes.Columns[e.ColumnIndex].Name;

            if (columnName == "colAbrir")
            {
                if (_soloRecepcion || _rolActual == UserRole.Galpon || _rolActual == UserRole.Admin)
                {
                    if (!OrdenCompraEstados.PermiteVerRevision(orden.estado))
                    {
                        ToastNotification.ShowWarning("La orden seleccionada no permite revisión.");
                        return;
                    }

                    if (_onRevisarOrden != null)
                    {
                        _onRevisarOrden(orden.id_orden_compra);
                        return;
                    }

                    var parent = TopLevelControl as VistaGeneral;
                    if (parent == null)
                    {
                        ToastNotification.ShowWarning("No se pudo abrir la revisión desde esta vista.");
                        return;
                    }

                    parent.OpenRevisionOrden(orden.id_orden_compra);
                }
            }
            else if (columnName == "colEditar" && _rolActual == UserRole.Admin)
            {
                if (!OrdenCompraEstados.PermiteEditarCabecera(orden.estado))
                {
                    ToastNotification.ShowWarning("La orden seleccionada no permite edición.");
                    return;
                }

                if (_onEditarOrden != null)
                {
                    _onEditarOrden(orden.id_orden_compra);
                    return;
                }

                var parent = TopLevelControl as VistaGeneral;
                if (parent == null)
                {
                    ToastNotification.ShowWarning("No se pudo abrir la edición desde esta vista.");
                    return;
                }

                parent.OpenOrdenForm(orden.id_orden_compra);
            }
            else if (columnName == "colEliminar" && _rolActual == UserRole.Admin)
            {
                if (!OrdenCompraEstados.EsEditable(orden.estado))
                {
                    ToastNotification.ShowWarning("Solo se pueden eliminar órdenes en estado Pendiente.");
                    return;
                }

                var confirm = MessageBox.Show(
                    "¿Querés eliminar esta orden pendiente?",
                    "Eliminar orden",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2);

                if (confirm != DialogResult.Yes)
                    return;

                try
                {
                    _ordenDatos.EliminarOrden(orden.id_orden_compra);
                    ToastNotification.ShowSuccess("Orden eliminada.");
                    CargarOrdenes();
                }
                catch (Exception ex)
                {
                    ToastNotification.ShowError("No se pudo eliminar la orden: " + ex.Message);
                }
            }
        }
    }
}
