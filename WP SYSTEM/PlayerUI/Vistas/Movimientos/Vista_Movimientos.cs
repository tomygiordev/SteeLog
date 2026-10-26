using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WP_Inventario.Datos;
using WP_Inventario.Modelos;
using WP_Inventario.Reportes;
using WP_Inventario.Servicios;
using System.IO;
// using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace WP_Inventario.Vistas.Movimientos
{
    public partial class Vista_Movimientos : Form, IMessageFilter
    {
        private List<MovimientoMaterial> materialesLista = new List<MovimientoMaterial>();
        private FlowLayoutPanel flpMateriales;
        private Panel panelSeleccionado = null;
        private bool isProgrammaticChange = false;
        private List<Categoria> _categorias = new List<Categoria>();
        private List<Material> materialesFull = new List<Material>();
        private Recorte _recorteSeleccionado = null;
        private int _idMovimientoAEditar = 0;
        private Label lblPrecioMaterial;
        private Guna.UI2.WinForms.Guna2TextBox txtPrecioMaterial;
        private Label lblPrecioGeneral;
        private Guna.UI2.WinForms.Guna2TextBox txtPrecioGeneral;
        private FlowLayoutPanel panelAccionesMovimiento;
        private Guna.UI2.WinForms.Guna2Button btnCancelarEdicion;

        private Timer _timerDebounce;
        private Panel _panelResultados;
        private Material _materialSeleccionado = null;

        // ── Borrador / Autoguardado ──────────────────────────────
        private Timer _timerDraft;
        private bool _guardadoExitoso = false; // true = el form cerró con éxito

        public class ItemCombo
        {
            public string TextoDisplay { get; set; }
            public int IdReal { get; set; }
            public string Tipo { get; set; }

            public override string ToString()
            {
                return TextoDisplay;
            }
        }


        public Vista_Movimientos()
        {
            InitializeComponent();
            guna2DateTimePicker1.Value = DateTime.Now;
            
            Application.AddMessageFilter(this);
            this.FormClosed += (s, e) =>
            {
                Application.RemoveMessageFilter(this);
                // Limpiar timer de borrador
                _timerDraft?.Stop();
                _timerDraft?.Dispose();
                _timerDraft = null;
                // Solo eliminar el borrador si el guardado fue exitoso
                if (_guardadoExitoso)
                    MovimientoDraftService.Eliminar();
            };

            ComunDatos datos = new ComunDatos();
            var dtTipos = datos.ObtenerListaGenerica("tipo_movimiento");
            // Reordenar: Envío a Obra primero, excluir Ajuste Positivo y Registro Interno
            DataTable dtTiposOrden = dtTipos.Clone();
            DataRow envioRow = null;

            // Tipos que NO se muestran en el selector manual
            var tiposOcultos = new[] { "ajuste positivo", "registro interno" };

            foreach (DataRow row in dtTipos.Rows)
            {
                string nombre = row["nombre"]?.ToString() ?? "";
                if (tiposOcultos.Any(t => nombre.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0))
                    continue; // Saltar los ocultos

                if (nombre.IndexOf("Envío", StringComparison.OrdinalIgnoreCase) >= 0
                    || nombre.IndexOf("Envio", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    envioRow = row;
                    // No importar todavía, lo ponemos primero luego
                }
            }

            if (envioRow != null) dtTiposOrden.ImportRow(envioRow);
            foreach (DataRow row in dtTipos.Rows)
            {
                string nombre = row["nombre"]?.ToString() ?? "";
                if (tiposOcultos.Any(t => nombre.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0))
                    continue;
                if (row != envioRow) dtTiposOrden.ImportRow(row);
            }

            // cmbMovimientos
            cmbMovimientos.DataSource = dtTiposOrden;
            cmbMovimientos.DisplayMember = "nombre";
            cmbMovimientos.ValueMember = "id_tipomovimiento";
            cmbMovimientos.SelectedIndex = -1;
            cmbMovimientos.SelectedIndex = -1;

            // cmbObras
            DataTable dtObras = datos.ObtenerListaGenerica("obras", "nombre", true);
            cmbObras.DataSource = dtObras;
            cmbObras.DisplayMember = "nombre";
            cmbObras.ValueMember = "id";
            cmbObras.SelectedIndex = -1;
            cmbObras.SelectedIndex = -1;

            // cmbFlete
            List<Flete> fletes = new FleteDatos().GetFletes(soloActivos: true);
            fletes.Insert(0, new Flete { id = 0, nombre = "-" });
            cmbFlete.DataSource = fletes;
            cmbFlete.DisplayMember = "nombre";
            cmbFlete.ValueMember = "id";
            cmbFlete.SelectedIndex = 0;

            materialesFull = new MaterialDatos().obtenerMateriales();

            cmbMovimientos.SelectedIndex = -1;
            cmbMovimientos.SelectedIndex = -1;
            cmbObras.SelectedIndex = -1;
            cmbObras.SelectedIndex = -1;

            _categorias = new CategoriaDatos().GetCategorias();

            CrearPanelResultados();
            ConfigurarDebounce();

            txtBuscadorMaterial.TextChanged += TxtBuscadorMaterial_TextChanged;
            txtBuscadorMaterial.KeyDown += TxtBuscadorMaterial_KeyDown;
            // Mostrar todos los materiales al hacer click/foco
            txtBuscadorMaterial.GotFocus += (s, ev) => EjecutarBusqueda(txtBuscadorMaterial.Text.Trim());
            txtBuscadorMaterial.Click += (s, ev) => EjecutarBusqueda(txtBuscadorMaterial.Text.Trim());
            txtBuscadorMaterial.LostFocus += (s, ev) =>
            {
                var timer = new Timer { Interval = 200 };
                timer.Tick += (ts, te) =>
                {
                    timer.Stop();
                    timer.Dispose();
                    if (!txtBuscadorMaterial.Focused && _panelResultados != null && !_panelResultados.ContainsFocus)
                    {
                        // Evitar cerrar si el mouse está sobre el panel (incluso si no tiene el foco)
                        Point mousePos = Control.MousePosition;
                        Rectangle pnlRect = _panelResultados.RectangleToScreen(_panelResultados.ClientRectangle);
                        if (!pnlRect.Contains(mousePos))
                            OcultarResultados();
                    }
                };
                timer.Start();
            };


            CargarOrigen();
            CargarDestino();
            InitializeUI();
            ConfigurarPreciosUI();
            ConfigurarAccionesMovimiento();
            ActualizarVisibilidadPrecios();

            guna2Button2.Click += GuardarMovimiento_Click;
            eliminarMaterial.Click += EliminarMaterial_Click;

            cmbObras.SelectedIndexChanged += CmbObras_SelectedIndexChanged;
            cmbDestino.SelectedIndexChanged += CmbDestino_SelectedIndexChanged;
            cmbMovimientos.SelectedIndexChanged += CmbMovimientos_SelectedIndexChanged;

            cantMatMov.MouseWheel += cantMatMov_MouseWheel;
            cantMatMov.Click += (s, ev) => cantMatMov.SelectAll();
            cantMatMov.TextChanged += cantMatMov_TextChanged;

            costoFleteTxt.MouseWheel += costoFleteTxt_MouseWheel;
            costoFleteTxt.Click += (s, ev) => costoFleteTxt.SelectAll();

            // ── Timer de autoguardado de borrador (cada 30 segundos) ──
            _timerDraft = new Timer { Interval = 30000 };
            _timerDraft.Tick += (s, ev) => GuardarBorrador();
            _timerDraft.Start();

            // ── Verificar borrador al iniciar (solo si no es modo edición) ──
            if (_idMovimientoAEditar <= 0)
            {
                this.Load += (s, ev) => IntentarRecuperarBorrador();
            }
        }

        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg == 0x0100) // WM_KEYDOWN (F5 handling)
            {
                Keys key = (Keys)m.WParam.ToInt32();
                if (key == Keys.F5 && this.Visible)
                {
                    RefrescarDatos();
                    return true;
                }
            }

            // Detect WM_LBUTTONDOWN (0x0201) and WM_RBUTTONDOWN (0x0204)
            if (m.Msg == 0x0201 || m.Msg == 0x0204)
            {
                if (_panelResultados != null && _panelResultados.Visible && txtBuscadorMaterial != null && !txtBuscadorMaterial.IsDisposed)
                {
                    Point mousePos = Control.MousePosition;
                    Rectangle txtRect = txtBuscadorMaterial.RectangleToScreen(txtBuscadorMaterial.ClientRectangle);
                    Rectangle pnlRect = _panelResultados.RectangleToScreen(_panelResultados.ClientRectangle);

                    if (!txtRect.Contains(mousePos) && !pnlRect.Contains(mousePos))
                    {
                        if (this.IsHandleCreated)
                        {
                            this.BeginInvoke(new Action(() => OcultarResultados()));
                        }
                    }
                }
            }
            return false;
        }

        private void RefrescarDatos()
        {
            try
            {
                isProgrammaticChange = true;
                var origenSel = cmbOrigen.SelectedItem as ItemCombo;
                var destinoSel = cmbDestino.SelectedItem as ItemCombo;
                int idTipoMov = cmbMovimientos.SelectedIndex;
                int idObra = cmbObras.SelectedIndex >= 0 ? Convert.ToInt32(cmbObras.SelectedValue) : -1;
                int idFlete = cmbFlete.SelectedValue != null ? Convert.ToInt32(cmbFlete.SelectedValue) : 0;
                materialesFull = new MaterialDatos().obtenerMateriales();
                CargarOrigen();
                CargarDestino();

                List<Flete> fletesRef = new FleteDatos().GetFletes(soloActivos: true, incluirId: idFlete);
                fletesRef.Insert(0, new Flete { id = 0, nombre = "-" });
                cmbFlete.DataSource = fletesRef;
                cmbFlete.DisplayMember = "nombre";
                cmbFlete.ValueMember = "id";
                if (idFlete > 0) cmbFlete.SelectedValue = idFlete;
                else cmbFlete.SelectedIndex = 0;
                cmbMovimientos.SelectedIndex = idTipoMov;
                if (origenSel != null)
                {
                    foreach (var item in cmbOrigen.Items)
                    {
                        if (item is ItemCombo ic && ic.IdReal == origenSel.IdReal && ic.Tipo == origenSel.Tipo)
                        { cmbOrigen.SelectedItem = item; break; }
                    }
                }
                if (destinoSel != null)
                {
                    foreach (var item in cmbDestino.Items)
                    {
                        if (item is ItemCombo ic && ic.IdReal == destinoSel.IdReal && ic.Tipo == destinoSel.Tipo)
                        { cmbDestino.SelectedItem = item; break; }
                    }
                }
                if (idObra > 0) cmbObras.SelectedValue = idObra;
                ToastNotification.ShowSuccess("Datos actualizados (F5)");
            }
            catch (Exception ex)
            {
                ToastNotification.ShowError("Error al refrescar: " + ex.Message);
            }
            finally
            {
                isProgrammaticChange = false;
            }
        }

        public void SetEditMode(int idMovimiento)
        {
            LimpiarFormulario();
            _idMovimientoAEditar = idMovimiento;
            ActualizarModoEdicion();
            // Diferir la carga hasta que el handle del formulario exista y tenga tamaño real
            if (this.IsHandleCreated)
                CargarMovimientoParaEdicion();
            else
                this.HandleCreated += (s, e) => CargarMovimientoParaEdicion();
        }

        private void CargarMovimientoParaEdicion()
        {
            if (_idMovimientoAEditar <= 0) return;

            try
            {
                MovimientoDatos datos = new MovimientoDatos();
                var mov = datos.ObtenerMovimientoPorId(_idMovimientoAEditar);
                if (mov == null)
                {
                    ToastNotification.ShowWarning("Movimiento no encontrado.");
                    return;
                }

                // Header
                guna2DateTimePicker1.Value = mov.fecha;
                nroRemitoTxt.Text = mov.nroremito ?? "";
                cmbMovimientos.SelectedValue = mov.id_tipomovimiento;

                // Trigger events might have changed origen/destino logic, let's wait a bit or set directly
                foreach (ItemCombo item in cmbOrigen.Items)
                {
                    if (item.IdReal == mov.id_origen && item.Tipo == mov.tipo_origen)
                    {
                        cmbOrigen.SelectedItem = item;
                        break;
                    }
                }

                foreach (ItemCombo item in cmbDestino.Items)
                {
                    if (item.IdReal == mov.id_destino && item.Tipo == mov.tipo_destino)
                    {
                        cmbDestino.SelectedItem = item;
                        break;
                    }
                }

                if (mov.id_obra > 0)
                {
                    cmbObras.SelectedValue = mov.id_obra;
                }

                if (mov.id_flete.HasValue && mov.id_flete.Value > 0)
                {
                    List<Flete> fletesEdicion = new FleteDatos().GetFletes(soloActivos: true, incluirId: mov.id_flete.Value);
                    fletesEdicion.Insert(0, new Flete { id = 0, nombre = "-" });
                    cmbFlete.DataSource = fletesEdicion;
                    cmbFlete.SelectedValue = mov.id_flete.Value;
                }
                else
                {
                    cmbFlete.SelectedIndex = 0;
                }

                costoFleteTxt.Text = mov.costoflete > 0 ? mov.costoflete.ToString("0.##").Replace('.', ',') : "";
                if (txtPrecioGeneral != null)
                {
                    txtPrecioGeneral.Text = mov.precio_total > 0 ? mov.precio_total.ToString("0.##").Replace('.', ',') : "";
                }
                txtCategoria.Text = mov.observaciones;

                // Detalles
                var detalles = datos.ObtenerDetallesPorMovimiento(_idMovimientoAEditar);
                foreach (var d in detalles)
                {
                    materialesLista.Add(d);
                    AgregarItemLista(d);
                }

                ActualizarModoEdicion();
            }
            catch (Exception ex)
            {
                ToastNotification.ShowError("Error al cargar para edición: " + ex.Message);
            }
        }

        private void InitializeUI()
        {
            Panel pnlContenedorMateriales = new Panel();
            pnlContenedorMateriales.BackColor = Color.FromArgb(40, 60, 50);
            pnlContenedorMateriales.Margin = new Padding(28, 0, 28, 0);
            pnlContenedorMateriales.Dock = DockStyle.Fill;

            flpMateriales = new FlowLayoutPanel();
            flpMateriales.Dock = DockStyle.Fill;
            flpMateriales.AutoScroll = true;
            flpMateriales.FlowDirection = FlowDirection.TopDown;
            flpMateriales.WrapContents = false;
            flpMateriales.BackColor = Color.Transparent;
            flpMateriales.Padding = new Padding(0, 0, 0, 30);

            pnlContenedorMateriales.Controls.Add(flpMateriales);

            panelAntiParpadeo4.Controls.Add(pnlContenedorMateriales, 0, 1);
            panelAntiParpadeo4.SetColumnSpan(pnlContenedorMateriales, 11);

            // Cuando el FlowLayoutPanel cambia de tamaño, ajustamos el ancho de cada ítem
            flpMateriales.Resize += (s, e) =>
            {
                int targetW = flpMateriales.ClientSize.Width - 5;
                if (targetW <= 0) return;
                foreach (Control ctrl in flpMateriales.Controls)
                    ctrl.Width = targetW;
            };
        }

        private void ConfigurarPreciosUI()
        {
            bool esAdmin = SessionManager.Role == UserRole.Admin;

            // 1. Inicializar lblPrecioMaterial y txtPrecioMaterial
            lblPrecioMaterial = new Label
            {
                Text = "Precio U.:",
                Font = new Font("Bahnschrift Light", 10F),
                ForeColor = Color.FromArgb(155, 185, 165),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                Visible = false
            };

            txtPrecioMaterial = new Guna.UI2.WinForms.Guna2TextBox
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Animated = true,
                BackColor = Color.Transparent,
                BorderColor = Color.FromArgb(55, 75, 62),
                Cursor = Cursors.IBeam,
                FillColor = Color.FromArgb(35, 50, 40),
                Font = new Font("Segoe UI", 10.5F),
                ForeColor = Color.FromArgb(225, 225, 225),
                PlaceholderText = "0",
                Size = new Size(60, 30),
                Visible = false
            };
            txtPrecioMaterial.FocusedState.BorderColor = Color.FromArgb(39, 174, 96);
            txtPrecioMaterial.HoverState.BorderColor = Color.FromArgb(60, 120, 80);
            
            txtPrecioMaterial.KeyPress += (s, e) =>
            {
                if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != ',' && e.KeyChar != '.')
                {
                    e.Handled = true;
                }
                if (e.KeyChar == '.')
                {
                    e.KeyChar = ',';
                }
                if (e.KeyChar == ',' && txtPrecioMaterial.Text.Contains(","))
                {
                    e.Handled = true;
                }
            };
            txtPrecioMaterial.Click += (s, ev) => txtPrecioMaterial.SelectAll();

            // 2. Inicializar lblPrecioGeneral y txtPrecioGeneral
            lblPrecioGeneral = new Label
            {
                Text = "Precio total:",
                Font = new Font("Bahnschrift Light", 10.5F),
                ForeColor = Color.FromArgb(155, 185, 165),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Visible = false
            };

            txtPrecioGeneral = new Guna.UI2.WinForms.Guna2TextBox
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Animated = true,
                BackColor = Color.Transparent,
                BorderColor = Color.FromArgb(55, 75, 62),
                Cursor = Cursors.IBeam,
                FillColor = Color.FromArgb(35, 50, 40),
                Font = new Font("Segoe UI", 10.5F),
                ForeColor = Color.FromArgb(225, 225, 225),
                PlaceholderText = "0,00",
                Size = new Size(200, 30),
                Visible = false
            };
            txtPrecioGeneral.FocusedState.BorderColor = Color.FromArgb(39, 174, 96);
            txtPrecioGeneral.HoverState.BorderColor = Color.FromArgb(60, 120, 80);
            
            txtPrecioGeneral.KeyPress += (s, e) =>
            {
                if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != ',' && e.KeyChar != '.')
                {
                    e.Handled = true;
                }
                if (e.KeyChar == '.')
                {
                    e.KeyChar = ',';
                }
                if (e.KeyChar == ',' && txtPrecioGeneral.Text.Contains(","))
                {
                    e.Handled = true;
                }
            };
            txtPrecioGeneral.Click += (s, ev) => txtPrecioGeneral.SelectAll();

            // 3. Modificar panelAntiParpadeo4 (11 columnas)
            panelAntiParpadeo4.ColumnCount = 11;
            panelAntiParpadeo4.ColumnStyles.Clear();
            panelAntiParpadeo4.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 75F));  // 0: Material Label
            panelAntiParpadeo4.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));  // 1: Buscador
            panelAntiParpadeo4.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40F));  // 2: Cant Label
            panelAntiParpadeo4.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50F));  // 3: Cant Input
            panelAntiParpadeo4.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 58F));  // 4: Eventual Label
            panelAntiParpadeo4.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 30F));  // 5: Eventual Check
            panelAntiParpadeo4.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 0F));   // 6: Precio Label (hidden by default)
            panelAntiParpadeo4.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 0F));   // 7: Precio Input (hidden by default)
            panelAntiParpadeo4.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40F));  // 8: Recorte Button
            panelAntiParpadeo4.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40F));  // 9: Insumo Button
            panelAntiParpadeo4.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 135F)); // 10: Agregar Button

            // Mover controles existentes a sus nuevas columnas
            panelAntiParpadeo4.Controls.Add(btnRecorte, 8, 0);
            panelAntiParpadeo4.Controls.Add(btnInsumo, 9, 0);
            panelAntiParpadeo4.Controls.Add(guna2Button1, 10, 0);

            // Agregar controles nuevos de precio a panelAntiParpadeo4
            panelAntiParpadeo4.Controls.Add(lblPrecioMaterial, 6, 0);
            panelAntiParpadeo4.Controls.Add(txtPrecioMaterial, 7, 0);

            // Agregar precio general de cabecera a panelAntiParpadeo2 (Row 3, Col 2 y 3)
            panelAntiParpadeo2.Controls.Add(lblPrecioGeneral, 2, 3);
            panelAntiParpadeo2.Controls.Add(txtPrecioGeneral, 3, 3);
        }

        private void ConfigurarAccionesMovimiento()
        {
            panelAccionesMovimiento = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };

            panelAntiParpadeo4.Controls.Remove(guna2Button2);
            guna2Button2.Anchor = AnchorStyles.None;
            guna2Button2.Width = 190;
            guna2Button2.Height = 34;
            guna2Button2.Margin = new Padding(4, 5, 14, 5);

            btnCancelarEdicion = new Guna.UI2.WinForms.Guna2Button
            {
                Text = "CANCELAR EDICIÓN",
                Width = 150,
                Height = 34,
                Anchor = AnchorStyles.None,
                Margin = new Padding(4, 5, 4, 5),
                FillColor = Color.FromArgb(90, 90, 90),
                ForeColor = Color.White,
                Font = new Font("Bahnschrift Light", 11F),
                Visible = false
            };
            btnCancelarEdicion.HoverState.FillColor = Color.FromArgb(115, 115, 115);
            btnCancelarEdicion.Click += CancelarEdicion_Click;

            panelAccionesMovimiento.Controls.Add(guna2Button2);
            panelAccionesMovimiento.Controls.Add(btnCancelarEdicion);
            panelAntiParpadeo4.Controls.Add(panelAccionesMovimiento, 1, 2);
            panelAntiParpadeo4.SetColumnSpan(panelAccionesMovimiento, 10);
        }

        private void ActualizarModoEdicion()
        {
            bool editando = _idMovimientoAEditar > 0;
            guna2Button2.Text = editando ? "ACTUALIZAR MOVIMIENTO" : "GUARDAR MOVIMIENTO";
            if (btnCancelarEdicion != null)
                btnCancelarEdicion.Visible = editando;
        }

        private void CancelarEdicion_Click(object sender, EventArgs e)
        {
            LimpiarFormulario();
            (TopLevelControl as VistaGeneral)?.OpenHistorialMovimientos();
        }

        private void ActualizarVisibilidadPrecios()
        {
            bool esAdmin = SessionManager.Role == UserRole.Admin;
            string tipoMovStr = cmbMovimientos.Text.ToUpper();
            // El total del movimiento siempre se calcula desde sus detalles.
            bool mostrarPrecioGeneral = false;
            if (lblPrecioGeneral != null && txtPrecioGeneral != null)
            {
                lblPrecioGeneral.Visible = mostrarPrecioGeneral;
                txtPrecioGeneral.Visible = mostrarPrecioGeneral;
                if (!mostrarPrecioGeneral)
                {
                    txtPrecioGeneral.Text = "";
                }
            }

            // 2. Mostrar/Ocultar precio unitario de material eventual
            bool mostrarPrecioMaterial = esAdmin && chkCompraEventual.Checked;
            if (lblPrecioMaterial != null && txtPrecioMaterial != null)
            {
                lblPrecioMaterial.Visible = mostrarPrecioMaterial;
                txtPrecioMaterial.Visible = mostrarPrecioMaterial;
                
                if (mostrarPrecioMaterial)
                {
                    panelAntiParpadeo4.ColumnStyles[6].Width = 70F;
                    panelAntiParpadeo4.ColumnStyles[7].Width = 75F;
                }
                else
                {
                    panelAntiParpadeo4.ColumnStyles[6].Width = 0F;
                    panelAntiParpadeo4.ColumnStyles[7].Width = 0F;
                    txtPrecioMaterial.Text = "";
                }
            }
        }

        public void CargarOrigen()
        {
            ComunDatos datos = new ComunDatos();
            List<ItemCombo> listaUnificada = new List<ItemCombo>();

            DataTable dtObras = datos.ObtenerListaGenerica("obras", "nombre", true);
            foreach (DataRow row in dtObras.Rows)
            {
                listaUnificada.Add(new ItemCombo
                {
                    TextoDisplay = row["nombre"].ToString(),
                    IdReal = Convert.ToInt32(row["id"]),
                    Tipo = "OBRA"
                });
            }

            DataTable dtUbis = datos.ObtenerListaGenerica("ubicacion");
            foreach (DataRow row in dtUbis.Rows)
            {
                listaUnificada.Add(new ItemCombo
                {
                    TextoDisplay = row["nombre"].ToString(),
                    IdReal = Convert.ToInt32(row["id_ubicacion"]),
                    Tipo = "UBICACION"
                });
            }

            DataTable dtProv = datos.ObtenerListaGenerica("proveedores");
            foreach (DataRow row in dtProv.Rows)
            {
                listaUnificada.Add(new ItemCombo
                {
                    TextoDisplay = row["nombre"].ToString(),
                    IdReal = Convert.ToInt32(row["id_proveedores"]),
                    Tipo = "PROVEEDOR"
                });
            }

            cmbOrigen.DataSource = listaUnificada;
            cmbOrigen.DisplayMember = "TextoDisplay";
            cmbOrigen.ValueMember = "IdReal";
            cmbOrigen.SelectedIndex = -1;
            cmbOrigen.SelectedIndex = -1;
        }

        public void CargarDestino()
        {
            ComunDatos datos = new ComunDatos();
            List<ItemCombo> listaUnificada = new List<ItemCombo>();

            DataTable dtObras = datos.ObtenerListaGenerica("obras", "nombre", true);
            foreach (DataRow row in dtObras.Rows)
            {
                listaUnificada.Add(new ItemCombo
                {
                    TextoDisplay = row["nombre"].ToString(),
                    IdReal = Convert.ToInt32(row["id"]),
                    Tipo = "OBRA"
                });
            }

            DataTable dtUbis = datos.ObtenerListaGenerica("ubicacion");
            foreach (DataRow row in dtUbis.Rows)
            {
                listaUnificada.Add(new ItemCombo
                {
                    TextoDisplay = row["nombre"].ToString(),
                    IdReal = Convert.ToInt32(row["id_ubicacion"]),
                    Tipo = "UBICACION"
                });
            }

            DataTable dtProv = datos.ObtenerListaGenerica("proveedores");
            foreach (DataRow row in dtProv.Rows)
            {
                listaUnificada.Add(new ItemCombo
                {
                    TextoDisplay = row["nombre"].ToString(),
                    IdReal = Convert.ToInt32(row["id_proveedores"]),
                    Tipo = "PROVEEDOR"
                });
            }

            // Fix for WinForms CurrencyManager auto-selecting index 0 on bound lists
            cmbDestino.DataSource = listaUnificada;
            cmbDestino.DisplayMember = "TextoDisplay";
            cmbDestino.ValueMember = "IdReal";
            cmbDestino.SelectedIndex = -1;
            cmbDestino.SelectedIndex = -1;
        }

        private void CmbObras_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (isProgrammaticChange) return;
            if (cmbObras.SelectedIndex == -1) return;

            try
            {
                isProgrammaticChange = true;
                int idObra = Convert.ToInt32(cmbObras.SelectedValue);

                foreach (var item in cmbDestino.Items)
                {
                    if (item is ItemCombo itemCombo && itemCombo.Tipo == "OBRA" && itemCombo.IdReal == idObra)
                    {
                        cmbDestino.SelectedItem = item;
                        break;
                    }
                }
            }
            finally
            {
                isProgrammaticChange = false;
            }
        }

        private void CmbMovimientos_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (isProgrammaticChange) return;
            if (cmbMovimientos.SelectedIndex == -1) return;

            string selectedTipo = cmbMovimientos.Text.ToUpper();

            string tipoMovStr = selectedTipo.ToUpper();
            bool permiteEventuales = tipoMovStr.Contains("COMPRA EVENTUAL") || tipoMovStr.Contains("APORTE DE CLIENTE") || tipoMovStr.Contains("APORTE DEL CLIENTE");
            if (tipoMovStr.Contains("COMPRA EVENTUAL"))
                chkCompraEventual.Checked = true;

            // ⚠️ VALIDACIÓN DE MATERIALES EVENTUALES:
            // Si el nuevo tipo NO permite eventuales, removemos cualquier material que no tenga ID (eventuales)
            if (!permiteEventuales && materialesLista.Any(m => !m.EsInsumo && !m.id_material.HasValue))
            {
                var eventuales = materialesLista.Where(m => !m.EsInsumo && !m.id_material.HasValue).ToList();
                foreach (var item in eventuales)
                {
                    materialesLista.Remove(item);
                    var pnl = flpMateriales.Controls.OfType<Panel>().FirstOrDefault(p => p.Tag == item);
                    if (pnl != null)
                    {
                        flpMateriales.Controls.Remove(pnl);
                        pnl.Dispose();
                    }
                }
                ToastNotification.ShowWarning("Se removieron materiales eventuales no permitidos en este tipo de movimiento.");
            }

            try
            {
                isProgrammaticChange = true;
                cmbObras.SelectedIndex = -1;
                cmbObras.SelectedIndex = -1;

                // 1. Carga inicial: Origen "DEPOSITO CENTRAL", Destino "DEPOSITO CENTRAL"
                if (selectedTipo.Contains("CARGA INICIAL"))
                {
                    SeleccionarEnCombo(cmbOrigen, "UBICACION", "DEPÓSITO", "DEPOSITO", "GALPON");
                    SeleccionarEnCombo(cmbDestino, "UBICACION", "DEPÓSITO", "DEPOSITO", "GALPON");
                }
                // 2. Compra: Origen "-", Destino "DEPOSITO CENTRAL"
                else if (selectedTipo.Contains("COMPRA") && !selectedTipo.Contains("EVENTUAL"))
                {
                    SeleccionarEnCombo(cmbOrigen, null, null);
                    SeleccionarEnCombo(cmbDestino, "UBICACION", "DEPÓSITO", "DEPOSITO", "GALPON");
                }
                // 3. Compra eventual: Origen "PROVEEDOR", Destino "OBRA"
                else if (selectedTipo.Contains("COMPRA EVENTUAL"))
                {
                    SeleccionarEnCombo(cmbOrigen, "PROVEEDOR", null);
                    SeleccionarEnCombo(cmbDestino, "OBRA", null);
                }
                // 4. Envio a Obra: Origen "DEPOSITO CENTRAL", Destino "OBRA" (Usuario elige)
                else if (selectedTipo.Contains("ENVIO A OBRA") || selectedTipo.Contains("ENVÍO A OBRA"))
                {
                    SeleccionarEnCombo(cmbOrigen, "UBICACION", "DEPÓSITO", "DEPOSITO", "GALPON");
                    SeleccionarEnCombo(cmbDestino, "OBRA", null);
                }
                // 5. Devolucion de obra: Origen "OBRA" (Usuario elige), Destino "DEPOSITO CENTRAL"
                else if (selectedTipo.Contains("DEVOLUCION DE OBRA") || selectedTipo.Contains("DEVOLUCIÓN DE OBRA"))
                {
                    SeleccionarEnCombo(cmbOrigen, "OBRA", null);
                    SeleccionarEnCombo(cmbDestino, "UBICACION", "DEPÓSITO", "DEPOSITO", "GALPON");
                }
                // 6. Aporte de cliente: Origen OBRA, Destino OBRA (Usuario elige)
                else if (selectedTipo.Contains("APORTE DE CLIENTE") || selectedTipo.Contains("APORTE DEL CLIENTE"))
                {
                    SeleccionarEnCombo(cmbOrigen, "OBRA", null);
                    SeleccionarEnCombo(cmbDestino, "OBRA", null);
                }
                // 7. Venta de material: Origen DEPOSITO CENTRAL, Destino "SE ELIGE" -> -1
                else if (selectedTipo.Contains("VENTA DE MATERIAL"))
                {
                    SeleccionarEnCombo(cmbOrigen, "UBICACION", "DEPÓSITO", "DEPOSITO", "GALPON");
                    SeleccionarEnCombo(cmbDestino, null, null); 
                }
                // 8. Uso interno: Origen DEPOSITO CENTRAL, Destino DEPOSITO CENTRAL
                else if (selectedTipo.Contains("USO INTERNO"))
                {
                    SeleccionarEnCombo(cmbOrigen, "UBICACION", "DEPÓSITO", "DEPOSITO", "GALPON");
                    SeleccionarEnCombo(cmbDestino, "UBICACION", "DEPÓSITO", "DEPOSITO", "GALPON");
                }
            }
            finally
            {
                isProgrammaticChange = false;
                ActualizarVisibilidadPrecios();
            }
        }

        private void SeleccionarEnCombo(Guna.UI2.WinForms.Guna2ComboBox combo, string tipo, params string[] busquedas)
        {
            if (busquedas == null || busquedas.Length == 0 || (busquedas.Length == 1 && busquedas[0] == null))
            {
                // Si no hay búsqueda, deseleccionamos para evitar que tome el primero por defecto (ej: Alero)
                combo.SelectedIndex = -1;
                combo.SelectedIndex = -1;
                return;
            }

            foreach (string busqueda in busquedas)
            {
                if (busqueda == null) continue;

                foreach (var item in combo.Items)
                {
                    if (item is ItemCombo itemCombo)
                    {
                        bool matchTipo = (tipo == null || itemCombo.Tipo == tipo.ToUpper());
                        bool matchNombre = (itemCombo.TextoDisplay.ToUpper().Contains(busqueda.ToUpper()));

                        if (matchTipo && matchNombre)
                        {
                            combo.SelectedItem = item;
                            return;
                        }
                    }
                }
            }
            
            // Si no encontró ninguno de los términos, deseleccionamos
            combo.SelectedIndex = -1;
            combo.SelectedIndex = -1;
        }

        private void CmbDestino_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (isProgrammaticChange) return;
            if (cmbDestino.SelectedIndex == -1) return;

            if (cmbDestino.SelectedItem is ItemCombo itemCombo)
            {
                if (itemCombo.Tipo == "OBRA")
                {
                    try
                    {
                        isProgrammaticChange = true;
                        cmbObras.SelectedValue = itemCombo.IdReal;
                        
                        // Si es Aporte de Cliente, sincronizar origen
                        if (cmbMovimientos.Text.ToUpper().Contains("APORTE DE CLIENTE"))
                        {
                            cmbOrigen.SelectedItem = cmbDestino.SelectedItem;
                        }
                    }
                    finally
                    {
                        isProgrammaticChange = false;
                    }
                }
            }
        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void label10_Click(object sender, EventArgs e)
        {

        }

        private void panelAntiParpadeo5_Paint(object sender, PaintEventArgs e)
        {

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

            this.Controls.Add(_panelResultados);
            _panelResultados.BringToFront();
        }

        private void PosicionarPanelResultados()
        {
            var screenPoint = txtBuscadorMaterial.PointToScreen(Point.Empty);
            var parentPoint = this.PointToClient(screenPoint);

            _panelResultados.Location = new Point(
                parentPoint.X,
                parentPoint.Y + txtBuscadorMaterial.Height + 5
            );
        }

        private void ConfigurarDebounce()
        {
            _timerDebounce = new Timer { Interval = 300 };
            _timerDebounce.Tick += (s, e) =>
            {
                _timerDebounce.Stop();
                EjecutarBusqueda(txtBuscadorMaterial.Text.Trim());
            };
        }

        private void TxtBuscadorMaterial_TextChanged(object sender, EventArgs e)
        {
            if (isProgrammaticChange) return;

            _timerDebounce.Stop();
            string nombreEsperado = _materialSeleccionado != null ? _materialSeleccionado.nombre : "";
            if (_recorteSeleccionado != null)
            {
                nombreEsperado = $"{_recorteSeleccionado.nombre_material} (Recorte: {_recorteSeleccionado.ancho}x{_recorteSeleccionado.altura})";
            }

            // Si estaba editando pero borro algo, deselecciono el material/recorte
            if (_materialSeleccionado != null && txtBuscadorMaterial.Text != nombreEsperado)
            {
                _materialSeleccionado = null;
                _recorteSeleccionado = null;
                btnRecorte.Enabled = false;
            }

            if (txtBuscadorMaterial.Text.Trim().Length < 2)
            {
                // Mostrar indicación en vez de buscar si hay menos de 2 caracteres
                EjecutarBusqueda("");
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
                cantMatMov.Focus();
                e.SuppressKeyPress = true;
            }
        }

        private class ItemBuscador
        {
            public bool EsMaterial { get; set; }
            public Material Mat { get; set; }
            public Recorte Rec { get; set; }
            public string NombreMostrar { get; set; }
            public string InfoMostrar { get; set; }
            public Color ColorAcento { get; set; }
            public string Icono { get; set; }
        }

        private void EjecutarBusqueda(string query)
        {
            string qLower = query?.ToLower() ?? "";

            if (qLower.Length < 2)
            {
                // Menos de 2 caracteres: pasar un mensaje especial a MostrarResultados
                var vacio = new List<ItemBuscador>();
                MostrarResultados(vacio, "MIN_CHARS");
                return;
            }

            var itemsCandidatos = new List<ItemBuscador>();
            var recDatos = new RecorteDatos();
            var listaRec = recDatos.ObtenerRecortes();

            // Filtrar materiales (requiere al menos 2 caracteres minimo)
            var mats = materialesFull
                .Where(m => m.nombre != null && m.nombre.ToLower().Contains(qLower))
                .OrderByDescending(m => m.nombre.ToLower().StartsWith(qLower))
                .ThenBy(m => m.nombre)
                .Take(50) // Limitar a 50 resultados para evitar colapso de UI
                .ToList();

            foreach (var m in mats)
            {
                string infoStockMaterial = SessionManager.DebeOcultarCantidadesStockMateriales
                    ? $"Material • {SessionManager.ObtenerEstadoStockMaterial(m.stockActual)}"
                    : $"Material • Stock: {m.stockActual:N0}";

                itemsCandidatos.Add(new ItemBuscador
                {
                    EsMaterial = true,
                    Mat = m,
                    NombreMostrar = m.nombre,
                    InfoMostrar = infoStockMaterial,
                    ColorAcento = Color.FromArgb(39, 174, 96),
                    Icono = "📦"
                });

                if (listaRec != null)
                {
                    var recortesHijos = listaRec.Where(r => r.id_material == m.id && r.cantidad > 0).ToList();
                    foreach (var r in recortesHijos)
                    {
                        itemsCandidatos.Add(new ItemBuscador
                        {
                            EsMaterial = false,
                            Rec = r,
                            NombreMostrar = $"{r.nombre_material} (Recorte: {r.ancho}x{r.altura})",
                            InfoMostrar = $"Recorte • Cantidad disponible: {r.cantidad:N0}",
                            ColorAcento = Color.Orange,
                            Icono = "✂️"
                        });
                    }
                }
            }

            MostrarResultados(itemsCandidatos, query);
        }

        private void MostrarResultados(List<ItemBuscador> resultados, string query)
        {
            _panelResultados.Controls.Clear();

            const int MAX_PANEL_HEIGHT = 260;
            const int ITEM_HEIGHT = 48;

            if (resultados.Count == 0)
            {
                string mensaje = "";
                if (query == "MIN_CHARS")
                    mensaje = "Escribí al menos 2 letras para buscar...";
                else if (string.IsNullOrEmpty(query))
                    mensaje = "No hay materiales disponibles.";
                else
                    mensaje = $"Sin resultados para \"{query}\"";

                var lblNoResults = new Label
                {
                    Text = mensaje,
                    Font = new Font("Bahnschrift Light", 10f),
                    ForeColor = Color.FromArgb(120, 120, 120),
                    Size = new Size(516, 45),
                    TextAlign = ContentAlignment.MiddleCenter,
                    BackColor = Color.Transparent,
                    Dock = DockStyle.Top
                };
                _panelResultados.Controls.Add(lblNoResults);
                _panelResultados.Size = new Size(516, 50);
                PosicionarPanelResultados();
                _panelResultados.BringToFront();
                _panelResultados.Visible = true;
                return;
            }

            // Sin limite de items - el scroll se encarga
            int totalContentHeight = resultados.Count * ITEM_HEIGHT;
            int panelHeight = Math.Min(totalContentHeight, MAX_PANEL_HEIGHT);

            // Agregar items en orden (Dock Top apila al revés, invertimos)
            for (int i = resultados.Count - 1; i >= 0; i--)
            {
                var item = resultados[i];
                var panel = CrearItemResultado(item);
                _panelResultados.Controls.Add(panel);
            }

            _panelResultados.Size = new Size(516, panelHeight);
            PosicionarPanelResultados();
            _panelResultados.BringToFront();
            _panelResultados.Visible = true;
        }

        private Panel CrearItemResultado(ItemBuscador item)
        {
            var panel = new Panel
            {
                Size = new Size(496, 48),
                BackColor = Color.FromArgb(25, 30, 28),
                Dock = DockStyle.Top,
                Cursor = Cursors.Hand,
                Padding = new Padding(10, 0, 10, 0)
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
                Size = new Size(400, 28),
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
                Size = new Size(400, 18),
                Location = new Point(55, 28),
                TextAlign = ContentAlignment.TopLeft,
                BackColor = Color.Transparent
            };

            var allControls = new Control[] { panel, lblIcon, lblName, lblType, accentBar };
            foreach (var ctrl in allControls)
            {
                ctrl.MouseEnter += (s, e) => panel.BackColor = Color.FromArgb(35, 45, 40);
                ctrl.MouseLeave += (s, e) => panel.BackColor = Color.FromArgb(25, 30, 28);
                ctrl.Click += (s, e) => ItemClicked(item);
            }

            panel.Controls.Add(accentBar);
            panel.Controls.Add(lblIcon);
            panel.Controls.Add(lblName);
            panel.Controls.Add(lblType);

            return panel;
        }

        private void ItemClicked(ItemBuscador item)
        {
            OcultarResultados();

            isProgrammaticChange = true;
            if (item.EsMaterial)
            {
                _materialSeleccionado = item.Mat;
                txtBuscadorMaterial.Text = item.Mat.nombre;
                _recorteSeleccionado = null;

                int idCat = item.Mat.id_categoria;
                // Refrescar categoría desde DB en tiempo real para reflejar cambios inmediatos
                var cat = new CategoriaDatos().GetCategorias().FirstOrDefault(c => c.id == idCat);
                btnRecorte.Enabled = (cat != null && cat.recortable);
            }
            else
            {
                _materialSeleccionado = materialesFull.FirstOrDefault(m => m.id == item.Rec.id_material);
                txtBuscadorMaterial.Text = $"{item.Rec.nombre_material} (Recorte: {item.Rec.ancho}x{item.Rec.altura})";
                _recorteSeleccionado = item.Rec;
                btnRecorte.Enabled = false;
            }
            isProgrammaticChange = false;

            cantMatMov.Focus();
        }

        private void OcultarResultados()
        {
            if (_panelResultados != null)
            {
                _panelResultados.Visible = false;
                _panelResultados.Controls.Clear();
            }
        }

        private void btnRecorte_Click(object sender, EventArgs e)
        {
            if (_materialSeleccionado == null) return;

            int idMat = _materialSeleccionado.id;
            string nomMat = _materialSeleccionado.nombre;

            using (var modal = new ModalRecortes(idMat, nomMat))
            {
                if (modal.ShowDialog() == DialogResult.OK)
                {
                    // Extraer selección del modal
                    var recorte = modal.RecorteSeleccionado;
                    decimal cantidad = modal.CantidadSeleccionada;

                    // Verificar si ya existe para acumular
                    var existente = materialesLista.FirstOrDefault(m =>
                        m.id_material == idMat &&
                        m.EsRecorte == true &&
                        m.IdRecorte == recorte.id);

                    if (existente != null)
                    {
                        existente.cantidad += cantidad;
                        ActualizarCantidadEnUI(existente);
                    }
                    else
                    {
                        MovimientoMaterial nuevoItem = new MovimientoMaterial
                        {
                            id_material = idMat,
                            NombreMaterial = nomMat + $" (R: {recorte.ancho}x{recorte.altura})",
                            cantidad = cantidad,
                            EsRecorte = true,
                            IdRecorte = recorte.id,
                            MedidaRecorte = recorte.ancho
                        };

                        materialesLista.Add(nuevoItem);
                        AgregarItemLista(nuevoItem);
                    }

                    // Limpiar selección de material y cantidad
                    _materialSeleccionado = null;
                    isProgrammaticChange = true;
                    txtBuscadorMaterial.Text = "";
                    isProgrammaticChange = false;
                    cantMatMov.Text = "";
                    _recorteSeleccionado = null;
                    btnRecorte.Enabled = false;
                }
            }
        }

        private void btnInsumo_Click(object sender, EventArgs e)
        {
            using (var modal = new ModalInsumos())
            {
                if (modal.ShowDialog() == DialogResult.OK)
                {
                    var insumo = modal.InsumoSeleccionado;

                    // Buscar si ya existe para acumular
                    var existente = materialesLista.FirstOrDefault(m => m.EsInsumo && m.IdInsumo == insumo.id);
                    if (existente != null)
                    {
                        existente.cantidad += 1;
                        ActualizarCantidadEnUI(existente);
                    }
                    else
                    {
                        MovimientoMaterial nuevoItem = new MovimientoMaterial
                        {
                            id_material = 0,
                            NombreMaterial = insumo.nombre,
                            cantidad = 1,
                            EsInsumo = true,
                            IdInsumo = insumo.id
                        };

                        materialesLista.Add(nuevoItem);
                        AgregarItemLista(nuevoItem);
                    }
                }
            }
        }

        private void ActualizarCantidadEnUI(MovimientoMaterial item)
        {
            foreach (Control ctrl in flpMateriales.Controls)
            {
                if (ctrl is Panel p && p.Tag == item)
                {
                    TableLayoutPanel tbl = p.Controls.OfType<TableLayoutPanel>().FirstOrDefault();
                    if (tbl != null)
                    {
                        Label lblCant = tbl.GetControlFromPosition(2, 0) as Label;
                        if (lblCant != null) lblCant.Text = item.cantidad.ToString("G29", CultureInfo.InvariantCulture);
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Calcula el stock disponible real para agregar al movimiento actual.
        /// En modo edición, se suman las cantidades ya cargadas en la lista porque
        /// esas cantidades serán revertidas al guardar (el movimiento original se elimina primero).
        /// </summary>
        private decimal StockDisponibleMaterial(int idMaterial)
        {
            decimal stockBase = _materialSeleccionado?.stockActual ?? 0;
            if (_idMovimientoAEditar > 0)
            {
                // Las cantidades ya en la lista se van a revertir → las sumamos al disponible
                decimal yaEnLista = materialesLista
                    .Where(m => m.id_material == idMaterial && !m.EsRecorte)
                    .Sum(m => m.cantidad);
                stockBase += yaEnLista;
            }
            return stockBase;
        }

        private decimal StockDisponibleRecorte(int idRecorte)
        {
            decimal stockBase = _recorteSeleccionado?.cantidad ?? 0;
            if (_idMovimientoAEditar > 0)
            {
                decimal yaEnLista = materialesLista
                    .Where(m => m.EsRecorte && m.IdRecorte == idRecorte)
                    .Sum(m => m.cantidad);
                stockBase += yaEnLista;
            }
            return stockBase;
        }

        private void chkCompraEventual_CheckedChanged(object sender, EventArgs e)
        {
            if (chkCompraEventual.Checked)
            {
                // Visual feedback for Eventual Purchase
                txtBuscadorMaterial.BorderColor = Color.FromArgb(255, 165, 0); // Orange/Amber
                txtBuscadorMaterial.PlaceholderText = "Escribir nombre de material eventual...";
                txtBuscadorMaterial.FillColor = Color.FromArgb(45, 40, 30);
                txtBuscadorMaterial.ForeColor = Color.FromArgb(255, 200, 100);
                
                // Disable search behavior (if implemented via autocomplete or similar)
                if (!isProgrammaticChange)
                {
                    _materialSeleccionado = null;
                    _recorteSeleccionado = null;
                }
                btnRecorte.Enabled = false;
            }
            else
            {
                // Restore standard look
                txtBuscadorMaterial.BorderColor = Color.FromArgb(60, 60, 65);
                txtBuscadorMaterial.PlaceholderText = "🔍 Buscar material...";
                txtBuscadorMaterial.FillColor = Color.FromArgb(25, 30, 28);
                txtBuscadorMaterial.ForeColor = Color.FromArgb(200, 200, 200);
            }

            if (!isProgrammaticChange && chkCompraEventual.Checked && _idMovimientoAEditar == 0 && panelSeleccionado != null) 
                DeseleccionarItem();

            ActualizarVisibilidadPrecios();
        }

        private void btnAgregarMaterial_Click(object sender, EventArgs e)
        {
            decimal cantidadVal = 0;
            // Acepta tanto "1.5" como "1,5" normalizando a InvariantCulture
            string cantTexto = (cantMatMov.Text ?? "").Trim().Replace(',', '.');
            decimal.TryParse(cantTexto, NumberStyles.Any, CultureInfo.InvariantCulture, out cantidadVal);

            bool esCompraEventual = chkCompraEventual.Checked;
            string materialTexto = txtBuscadorMaterial.Text.Trim();

            if (cantidadVal <= 0)
            {
                ToastNotification.ShowWarning("Ingrese una cantidad válida.");
                return;
            }

            // Evitar pérdida silenciosa: la columna cantidad es entera en la base para materiales normales.
            // Si el usuario ingresa un decimal (ej: 2,75) se perdería la parte fraccional.
            // Los recortes y compras eventuales aceptan decimales (ej: 0.5, 1.25).
            if (_recorteSeleccionado == null && !esCompraEventual && cantidadVal != Math.Floor(cantidadVal))
            {
                var resp = MessageBox.Show(
                    $"La cantidad '{cantidadVal.ToString(CultureInfo.InvariantCulture)}' tiene decimales y se guardará redondeada a {Math.Round(cantidadVal, MidpointRounding.AwayFromZero)}.\n\n¿Continuar?",
                    "Cantidad con decimales",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2);
                if (resp != DialogResult.Yes) return;
                cantidadVal = Math.Round(cantidadVal, MidpointRounding.AwayFromZero);
            }

            if (esCompraEventual && string.IsNullOrEmpty(materialTexto))
            {
                ToastNotification.ShowWarning("Ingrese el nombre del material eventual.");
                return;
            }
            if (!esCompraEventual && _materialSeleccionado == null)
            {
                ToastNotification.ShowWarning("Seleccione material de la lista desde el buscador.");
                return;
            }

            int? idMaterial = _materialSeleccionado?.id;
            Material materialInfo = _materialSeleccionado;
            decimal cantidad = cantidadVal;

            MovimientoMaterial itemAnterior = null;
            if (panelSeleccionado != null && panelSeleccionado.Tag is MovimientoMaterial)
            {
                itemAnterior = (MovimientoMaterial)panelSeleccionado.Tag;
                var confirmEdit = MessageBox.Show(
                    "¿Estás seguro que querés editar este material?",
                    "Confirmar edición",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2);
                if (confirmEdit != DialogResult.Yes) return;
            }

            if (_recorteSeleccionado == null && materialInfo != null)
            {
                // En modo edición el stock disponible incluye lo ya cargado (será revertido)
                decimal stockDisponible = idMaterial.HasValue ? StockDisponibleMaterial(idMaterial.Value) : 0;
                decimal yaEnLista = materialesLista.Where(m => m.id_material == idMaterial && !m.EsRecorte).Sum(m => m.cantidad);

                // Si estamos modificando el mismo ítem, restamos su cantidad anterior para no duplicarla en la suma
                if (itemAnterior != null && itemAnterior.id_material == idMaterial && !itemAnterior.EsRecorte)
                {
                    yaEnLista -= itemAnterior.cantidad;
                }

                decimal totalSolicitado = yaEnLista + cantidad;

                string tipoDestino = (cmbDestino.SelectedItem as ItemCombo)?.Tipo;
                bool consumeStock = MovimientoDatos.TipoMovimientoConsumeStock(cmbMovimientos.Text, tipoDestino);

                if (consumeStock && totalSolicitado > stockDisponible)
                {
                    string mensaje = SessionManager.DebeOcultarCantidadesStockMateriales
                        ? "Stock insuficiente para agregar ese material."
                        : $"Stock insuficiente. Disponible: {stockDisponible}";
                    ToastNotification.ShowWarning(mensaje);
                    return;
                }
            }
            else if (_recorteSeleccionado != null)
            {
                decimal stockDisponibleRec = StockDisponibleRecorte(_recorteSeleccionado.id);
                decimal yaEnLista = materialesLista.Where(m => m.EsRecorte && m.IdRecorte == _recorteSeleccionado.id).Sum(m => m.cantidad);

                if (itemAnterior != null && itemAnterior.EsRecorte && itemAnterior.IdRecorte == _recorteSeleccionado.id)
                {
                    yaEnLista -= itemAnterior.cantidad;
                }

                decimal totalSolicitado = yaEnLista + cantidad;

                string tipoDestino = (cmbDestino.SelectedItem as ItemCombo)?.Tipo;
                bool consumeStock = MovimientoDatos.TipoMovimientoConsumeStock(cmbMovimientos.Text, tipoDestino);
                if (consumeStock && totalSolicitado > stockDisponibleRec)
                {
                    string mensaje = SessionManager.DebeOcultarCantidadesStockMateriales
                        ? "Stock de recorte insuficiente."
                        : $"Stock de recorte insuficiente. Disponible: {stockDisponibleRec.ToString("G29")}";
                    ToastNotification.ShowWarning(mensaje);
                    return;
                }
            }

            idMaterial = esCompraEventual ? (int?)null : _materialSeleccionado?.id;
            string nombreMaterial = materialInfo?.nombre ?? materialTexto;

            decimal precioUnitario = 0;
            if (esCompraEventual && txtPrecioMaterial != null)
            {
                decimal.TryParse(txtPrecioMaterial.Text.Trim().Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out precioUnitario);
            }

            MovimientoMaterial nuevoItem = new MovimientoMaterial
            {
                id_material = idMaterial,
                NombreMaterial = nombreMaterial + (_recorteSeleccionado != null ? $" (R: {_recorteSeleccionado.ancho}x{_recorteSeleccionado.altura})" : ""),
                NombreMaterialEventual = esCompraEventual ? materialTexto : null,
                cantidad = cantidad,
                EsRecorte = _recorteSeleccionado != null,
                IdRecorte = _recorteSeleccionado?.id,
                MedidaRecorte = _recorteSeleccionado?.ancho,
                EsCompraEventual = esCompraEventual,
                precio_unitario = precioUnitario
            };

            if (itemAnterior != null)
            {
                // Estamos editando un material existente en la lista
                int idx = materialesLista.IndexOf(itemAnterior);
                if (idx >= 0) materialesLista[idx] = nuevoItem;

                panelSeleccionado.Tag = nuevoItem;
                TableLayoutPanel tbl = panelSeleccionado.Controls.OfType<TableLayoutPanel>().FirstOrDefault();
                if (tbl != null)
                {
                    Label lblMat = tbl.GetControlFromPosition(0, 0) as Label;
                    if (lblMat != null)
                    {
                        string textoPrecio = "";
                        if (nuevoItem.EsCompraEventual && nuevoItem.precio_unitario > 0 && SessionManager.Role == UserRole.Admin)
                        {
                            textoPrecio = $" (${nuevoItem.precio_unitario:N2})";
                        }
                        lblMat.Text = nuevoItem.NombreMaterial + textoPrecio;
                    }

                    Label lblTip = tbl.GetControlFromPosition(1, 0) as Label;
                    if (lblTip != null)
                    {
                        if (nuevoItem.EsRecorte) { lblTip.Text = "RECORTE"; lblTip.ForeColor = Color.Orange; }
                        else if (nuevoItem.EsInsumo) { lblTip.Text = "INSUMO"; lblTip.ForeColor = Color.FromArgb(0, 188, 212); }
                        else if (nuevoItem.EsCompraEventual) { lblTip.Text = "EVENTUAL"; lblTip.ForeColor = Color.FromArgb(255, 165, 0); }
                        else { lblTip.Text = ""; }
                    }

                    Label lblCant = tbl.GetControlFromPosition(2, 0) as Label;
                    if (lblCant != null) lblCant.Text = nuevoItem.cantidad.ToString("G29", CultureInfo.InvariantCulture);
                }
                
                DeseleccionarItem();
            }
            else
            {
                // Es uno nuevo - Verificar si ya existe en la lista para acumular
                var existente = materialesLista.FirstOrDefault(m => 
                    m.id_material == nuevoItem.id_material && 
                    m.NombreMaterialEventual == nuevoItem.NombreMaterialEventual &&
                    m.EsRecorte == nuevoItem.EsRecorte && 
                    m.IdRecorte == nuevoItem.IdRecorte &&
                    m.EsInsumo == nuevoItem.EsInsumo &&
                    m.EsCompraEventual == nuevoItem.EsCompraEventual &&
                    m.precio_unitario == nuevoItem.precio_unitario);

                if (existente != null)
                {
                    existente.cantidad += nuevoItem.cantidad;
                    ActualizarCantidadEnUI(existente);
                }
                else
                {
                    materialesLista.Add(nuevoItem);
                    AgregarItemLista(nuevoItem);
                }

                DeseleccionarItem();
                GuardarBorrador();
            }
        }

        private void DeseleccionarItem()
        {
            if (panelSeleccionado != null)
                panelSeleccionado.BackColor = Color.Transparent;
            panelSeleccionado = null;

            isProgrammaticChange = true;
            _materialSeleccionado = null;
            _recorteSeleccionado = null;
            txtBuscadorMaterial.Text = "";
            cantMatMov.Text = "";
            chkCompraEventual.Checked = cmbMovimientos.Text.ToUpper().Contains("COMPRA EVENTUAL");
            btnRecorte.Enabled = false;
            guna2Button1.Text = "Agregar Material";
            isProgrammaticChange = false;
            
            ActualizarVisibilidadPrecios();
            
            // Regresar foco al buscador de materiales para agilizar el flujo de entrada
            txtBuscadorMaterial.Focus();
        }

        private void AgregarItemLista(MovimientoMaterial item)
        {
            // ⚠️ Los hijos de FlowLayoutPanel NO deben usar Dock: el FLP controla su posición.
            // Usamos Width explícito y un TableLayoutPanel interno para el layout de columnas.
            Panel pnlItem = new Panel();
            pnlItem.Margin = new Padding(0, 0, 0, 4);
            // Tomar el ancho actual del FLP; si aún es 0 (carga inicial), usamos 600 como fallback
            pnlItem.Width = flpMateriales.ClientSize.Width > 5
                ? flpMateriales.ClientSize.Width - 5
                : 600;
            pnlItem.Height = 40;
            pnlItem.Tag = item;
            pnlItem.Cursor = Cursors.Hand;
            pnlItem.BackColor = Color.Transparent;

            // Tabla interna: [nombre | etiqueta tipo | cantidad]
            TableLayoutPanel tbl = new TableLayoutPanel();
            tbl.Dock = DockStyle.Fill;
            tbl.ColumnCount = 3;
            tbl.RowCount = 1;
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f)); // nombre
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));       // etiqueta RECORTE/INSUMO
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80f));  // cantidad
            tbl.Padding = new Padding(8, 0, 8, 0);
            tbl.BackColor = Color.Transparent;

            Label lblMaterial = new Label();
            string textoPrecio = "";
            if (item.EsCompraEventual && item.precio_unitario > 0 && SessionManager.Role == UserRole.Admin)
            {
                textoPrecio = $" (${item.precio_unitario:N2})";
            }
            lblMaterial.Text = item.NombreMaterial + textoPrecio;
            lblMaterial.Font = new Font("Bahnschrift SemiLight", 12.5f);
            lblMaterial.ForeColor = Color.White;
            lblMaterial.Dock = DockStyle.Fill;
            lblMaterial.TextAlign = ContentAlignment.MiddleLeft;
            lblMaterial.AutoEllipsis = true;
            tbl.Controls.Add(lblMaterial, 0, 0);

            Label lblTipo = new Label();
            lblTipo.Dock = DockStyle.Fill;
            lblTipo.TextAlign = ContentAlignment.MiddleCenter;
            lblTipo.Font = new Font("Bahnschrift SemiBold", 9.5f);
            lblTipo.Padding = new Padding(4, 0, 4, 0);
            if (item.EsRecorte)       { lblTipo.Text = "RECORTE"; lblTipo.ForeColor = Color.Orange; }
            else if (item.EsInsumo)   { lblTipo.Text = "INSUMO";  lblTipo.ForeColor = Color.FromArgb(0, 188, 212); }
            else if (item.EsCompraEventual) { lblTipo.Text = "EVENTUAL"; lblTipo.ForeColor = Color.FromArgb(255, 165, 0); }
            tbl.Controls.Add(lblTipo, 1, 0);

            Label lblCantidad = new Label();
            lblCantidad.Text = item.cantidad.ToString("G29", CultureInfo.InvariantCulture);
            lblCantidad.Font = new Font("Bahnschrift SemiBold", 12.5f);
            lblCantidad.ForeColor = Color.FromArgb(150, 220, 150);
            lblCantidad.Dock = DockStyle.Fill;
            lblCantidad.TextAlign = ContentAlignment.MiddleRight;
            tbl.Controls.Add(lblCantidad, 2, 0);

            pnlItem.Controls.Add(tbl);

            pnlItem.Click += (s, ev) => Item_Click(pnlItem, ev);
            tbl.Click      += (s, ev) => Item_Click(pnlItem, ev);
            lblMaterial.Click += (s, ev) => Item_Click(pnlItem, ev);
            lblTipo.Click     += (s, ev) => Item_Click(pnlItem, ev);
            lblCantidad.Click += (s, ev) => Item_Click(pnlItem, ev);

            flpMateriales.Controls.Add(pnlItem);
        }

        private void Item_Click(object sender, EventArgs e)
        {
            Panel clickedPanel = (Panel)sender;

            // Si clicamos el mismo que ya está seleccionado, lo deseleccionamos (cancelamos edición)
            if (panelSeleccionado == clickedPanel)
            {
                DeseleccionarItem();
                return;
            }

            // Deseleccionar el previo
            if (panelSeleccionado != null)
                panelSeleccionado.BackColor = Color.Transparent;

            panelSeleccionado = clickedPanel;
            panelSeleccionado.BackColor = Color.FromArgb(75, 145, 90);
            guna2Button1.Text = "Editar Material";

            // Cargar el material y cantidad en los campos del selector
            if (panelSeleccionado.Tag is MovimientoMaterial itemSel)
            {
                isProgrammaticChange = true;
                chkCompraEventual.Checked = itemSel.EsCompraEventual;
                
                // Buscar el material en la lista completa para tener el objeto completo
                var mat = itemSel.id_material.HasValue ? materialesFull.FirstOrDefault(m => m.id == itemSel.id_material) : null;
                
                _materialSeleccionado = mat;

                if (itemSel.EsCompraEventual)
                {
                    txtBuscadorMaterial.Text = itemSel.NombreMaterialEventual ?? itemSel.NombreMaterial;
                    btnRecorte.Enabled = false;
                    _recorteSeleccionado = null;
                }
                else if (mat != null)
                {

                    if (itemSel.EsRecorte && itemSel.IdRecorte.HasValue)
                    {
                        // Para recortes mostramos el nombre con medida
                        txtBuscadorMaterial.Text = itemSel.NombreMaterial;
                    }
                    else
                    {
                        txtBuscadorMaterial.Text = mat.nombre;
                        // Refrescar categoría desde DB en tiempo real para reflejar cambios inmediatos
                        var cat = new CategoriaDatos().GetCategorias().FirstOrDefault(c => c.id == mat.id_categoria);
                        btnRecorte.Enabled = (cat != null && cat.recortable);
                    }

                }
                else
                {
                    // Caso material eventual antiguo sin boolean
                    txtBuscadorMaterial.Text = itemSel.NombreMaterial;
                    btnRecorte.Enabled = false;
                    _recorteSeleccionado = null;
                }

                cantMatMov.Text = itemSel.cantidad.ToString("G29", CultureInfo.InvariantCulture);

                if (itemSel.EsCompraEventual && txtPrecioMaterial != null)
                {
                    txtPrecioMaterial.Text = itemSel.precio_unitario > 0 ? itemSel.precio_unitario.ToString("0.##").Replace('.', ',') : "";
                }

                isProgrammaticChange = false;

                // Enfocar el campo de cantidad para edición inmediata
                cantMatMov.Focus();
                cantMatMov.SelectAll();
            }
        }

        private void EliminarMaterial_Click(object sender, EventArgs e)
        {
            if (panelSeleccionado != null)
            {
                MovimientoMaterial item = (MovimientoMaterial)panelSeleccionado.Tag;
                materialesLista.Remove(item);
                flpMateriales.Controls.Remove(panelSeleccionado);
                panelSeleccionado.Dispose();
                
                DeseleccionarItem();
                GuardarBorrador();
            }
            else
            {
                ToastNotification.ShowWarning("Seleccione un ítem para eliminar.");
            }
        }

        private decimal CalcularPrecioTotalMateriales()
        {
            decimal total = 0;
            foreach (var detalle in materialesLista.Where(m => !m.EsInsumo))
            {
                decimal precioUnitario = detalle.EsCompraEventual
                    ? detalle.precio_unitario
                    : materialesFull.FirstOrDefault(m => detalle.id_material.HasValue && m.id == detalle.id_material.Value)?.precioUnitario ?? 0;

                total += detalle.cantidad * precioUnitario;
            }

            return total;
        }

        private void GuardarMovimiento_Click(object sender, EventArgs e)
        {
            if (cmbMovimientos.SelectedIndex == -1 || cmbOrigen.SelectedIndex == -1 || cmbDestino.SelectedIndex == -1 || materialesLista.Count == 0)
            {
                ToastNotification.ShowWarning("Completa todos los campos obligatorios.");
                return;
            }

            var confirmGuardar = MessageBox.Show(
                "¿Estás seguro que querés guardar este remito?",
                "Confirmar guardado",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);
            if (confirmGuardar != DialogResult.Yes) return;

            // Validación de materiales eventuales antes de procesar
            string tipoMovStr = cmbMovimientos.Text.ToUpper();
            if (materialesLista.Any(m => m.EsCompraEventual && string.IsNullOrEmpty(m.NombreMaterialEventual)))
            {
                ToastNotification.ShowError("No se puede guardar: Hay materiales de compra eventual sin nombre.");
                return;
            }

            try
            {
                // Separate materials from insumos
                var materialesSolo = materialesLista.Where(m => !m.EsInsumo).ToList();
                var insumosSolo = materialesLista.Where(m => m.EsInsumo).ToList();

                // Determine destino name for insumo movements
                string destinoNombre = "";
                int? destinoObraId = null;
                var itemDestino = (ItemCombo)cmbDestino.SelectedItem;
                if (itemDestino.Tipo == "OBRA")
                {
                    destinoObraId = itemDestino.IdReal;
                    destinoNombre = itemDestino.TextoDisplay;
                }
                else
                {
                    destinoNombre = itemDestino.TextoDisplay;
                }

                string origenNombre = ((ItemCombo)cmbOrigen.SelectedItem).TextoDisplay;

                decimal precioTotal = CalcularPrecioTotalMateriales();

                Movimiento movimiento = new Movimiento
                {
                    fecha = guna2DateTimePicker1.Value,
                    nroremito = nroRemitoTxt.Text.Trim(),
                    id_origen = ((ItemCombo)cmbOrigen.SelectedItem).IdReal,
                    tipo_origen = ((ItemCombo)cmbOrigen.SelectedItem).Tipo,
                    id_destino = itemDestino.IdReal,
                    tipo_destino = itemDestino.Tipo,
                    id_tipomovimiento = Convert.ToInt32(cmbMovimientos.SelectedValue),
                    id_obra = cmbObras.SelectedIndex != -1 ? Convert.ToInt32(cmbObras.SelectedValue) : 0, 
                    costoflete = decimal.TryParse((costoFleteTxt.Text ?? "").Trim().Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal costo) ? costo : 0,
                    id_flete = (cmbFlete.SelectedValue != null && Convert.ToInt32(cmbFlete.SelectedValue) > 0) ? Convert.ToInt32(cmbFlete.SelectedValue) : (int?)null,
                    observaciones = txtCategoria.Text,
                    precio_total = precioTotal
                };

                int idMovimientoFinal = 0;
                MovimientoDatos movimientoDatos = new MovimientoDatos();

                if (_idMovimientoAEditar > 0)
                {
                    idMovimientoFinal = movimientoDatos.ActualizarMovimiento(_idMovimientoAEditar, movimiento, materialesSolo);
                }
                else
                {
                    idMovimientoFinal = movimientoDatos.GuardarMovimiento(movimiento, materialesSolo);
                }

                // Handle insumo movements separately
                if (insumosSolo.Count > 0)
                {
                    InsumoDatos insumoDatos = new InsumoDatos();
                    foreach (var insumoItem in insumosSolo)
                    {
                        if (insumoItem.IdInsumo.HasValue)
                        {
                            insumoDatos.MoverInsumo(insumoItem.IdInsumo.Value, destinoObraId, origenNombre, destinoNombre, idMovimientoFinal);
                        }
                    }
                }

                ToastNotification.ShowSuccess(_idMovimientoAEditar > 0 ? "Movimiento actualizado." : "Movimiento guardado.");
                _guardadoExitoso = true;
                MovimientoDraftService.Eliminar(); // Borrador ya no necesario

                // Verificar stock mínimo de los materiales consumidos
                VerificarStockMinimoPorMateriales(materialesSolo);

                // PDF GENERATION
                if (idMovimientoFinal > 0)
                {
                    try
                    {
                        MovimientoPDF generador = new MovimientoPDF();
                        string tempFileName = Path.Combine(Path.GetTempPath(), generador.SugerirNombreArchivo(idMovimientoFinal));
                        
                        generador.Generar(tempFileName, idMovimientoFinal);
                        
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

                LimpiarFormulario();
                _guardadoExitoso = true; // Permitir cierre limpio
                (this.TopLevelControl as VistaGeneral)?.RefrescarHistorialSiAbierto();
            }
            catch (Exception ex)
            {
                ToastNotification.ShowError("Error al guardar: " + ex.Message);
            }
        }

        private void LimpiarFormulario()
        {
            isProgrammaticChange = true;
            try
            {
                cmbMovimientos.SelectedIndex = -1;
                cmbMovimientos.SelectedIndex = -1;
                cmbObras.SelectedIndex = -1;
                cmbObras.SelectedIndex = -1;
                cmbOrigen.SelectedIndex = -1;
                cmbOrigen.SelectedIndex = -1;
                cmbDestino.SelectedIndex = -1;
                cmbDestino.SelectedIndex = -1;
                txtBuscadorMaterial.Text = "";
                _materialSeleccionado = null;
            }
            finally
            {
                isProgrammaticChange = false;
            }

            nroRemitoTxt.Text = "";
            costoFleteTxt.Text = "";
            if (cmbFlete != null && cmbFlete.Items.Count > 0) cmbFlete.SelectedIndex = 0;
            cantMatMov.Text = "";
            guna2DateTimePicker1.Value = DateTime.Now;

            txtCategoria.Text = string.Empty;
            _recorteSeleccionado = null;

            btnRecorte.Enabled = false;

            materialesLista.Clear();
            flpMateriales.Controls.Clear();
            panelSeleccionado = null;
            _idMovimientoAEditar = 0;
            ActualizarModoEdicion();

            // Refrescar Stock
            materialesFull = new MaterialDatos().obtenerMateriales();
        }

        private void cmbMovimientos_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void Vista_Movimientos_Load(object sender, EventArgs e)
        {

        }

        private void limpiarForm_Click(object sender, EventArgs e)
        {
            LimpiarFormulario();
        }

        private void cmbOrigen_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (isProgrammaticChange) return;
            if (cmbOrigen.SelectedIndex == -1) return;

            // Si es Aporte de Cliente, sincronizar destino
            if (cmbMovimientos.Text.ToUpper().Contains("APORTE DE CLIENTE"))
            {
                try
                {
                    isProgrammaticChange = true;
                    cmbDestino.SelectedItem = cmbOrigen.SelectedItem;
                }
                finally
                {
                    isProgrammaticChange = false;
                }
            }
        }

        private void label8_Click(object sender, EventArgs e)
        {

        }

        private void panelAntiParpadeo3_Paint(object sender, PaintEventArgs e)
        {

        }

        private void guna2CheckBox1_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void guna2ImageCheckBox1_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void cantMatMov_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                e.Handled = true;
                btnAgregarMaterial_Click(guna2Button1, EventArgs.Empty);
                return;
            }

            // Permitir: dígitos, teclas de control (backspace, etc.) y UN separador decimal (. o ,)
            bool esSeparador = e.KeyChar == '.' || e.KeyChar == ',';
            bool yaTieneSeparador = (cantMatMov.Text ?? "").IndexOfAny(new[] { '.', ',' }) >= 0;

            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && !esSeparador)
            {
                e.Handled = true;
            }
            else if (esSeparador && yaTieneSeparador)
            {
                e.Handled = true;
            }
        }
        private void cantMatMov_TextChanged(object sender, EventArgs e)
        {
            if (isProgrammaticChange) return;
            if (_materialSeleccionado == null) return;

            string cantTexto = (cantMatMov.Text ?? "").Trim().Replace(',', '.');
            if (decimal.TryParse(cantTexto, System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture, out decimal valor))
            {
                // En modo edición, el stockMax considera las cantidades ya cargadas (serán revertidas)
                decimal stockMax = _recorteSeleccionado != null
                    ? StockDisponibleRecorte(_recorteSeleccionado.id)
                    : (_materialSeleccionado != null ? StockDisponibleMaterial(_materialSeleccionado.id) : decimal.MaxValue);
                
                // Clamp automático: solo para tipos de movimiento que CONSUMEN stock.
                string tipoDestino = (cmbDestino.SelectedItem as ItemCombo)?.Tipo;
                bool consumeStock = MovimientoDatos.TipoMovimientoConsumeStock(cmbMovimientos.Text, tipoDestino);

                if (consumeStock && valor > stockMax)
                {
                    isProgrammaticChange = true;
                    cantMatMov.Text = stockMax.ToString("G29", CultureInfo.InvariantCulture);
                    cantMatMov.SelectionStart = cantMatMov.Text.Length;
                    isProgrammaticChange = false;
                    ToastNotification.ShowWarning($"Stock máximo disponible: {stockMax.ToString("G29")}");
                }
            }
        }

        private void cantMatMov_MouseWheel(object sender, MouseEventArgs e)
        {
            // Evitar que el scroll del padre se active
            if (e is HandledMouseEventArgs hme)
                hme.Handled = true;

            string cantTexto = (cantMatMov.Text ?? "").Replace(',', '.');
            decimal.TryParse(cantTexto, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal valor);

            // En modo edición el stockMax considera lo ya cargado (será revertido)
            decimal stockMax;
            if (_recorteSeleccionado != null)
                stockMax = StockDisponibleRecorte(_recorteSeleccionado.id);
            else if (_materialSeleccionado != null)
                stockMax = StockDisponibleMaterial(_materialSeleccionado.id);
            else
                stockMax = decimal.MaxValue;

            valor = (e.Delta > 0) ? valor + 1 : valor - 1;
            
            if (valor < 0) valor = 0;

            cantMatMov.Text = valor.ToString("G29");
        }
        private void costoFleteTxt_MouseWheel(object sender, MouseEventArgs e)
        {
            if (e is HandledMouseEventArgs hme)
                hme.Handled = true;

            decimal valor = 0;
            decimal.TryParse(costoFleteTxt.Text.Replace(',', ','), System.Globalization.NumberStyles.Any,
                new System.Globalization.CultureInfo("es-AR"), out valor);

            valor = (e.Delta > 0) ? valor + 1 : valor - 1;
            if (valor < 0) valor = 0;
            costoFleteTxt.Text = valor.ToString("0.##").Replace('.', ',');
        }
        private void nroRemitoTxt_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Permitir letras, dígitos, espacios y teclas de control
            if (!char.IsControl(e.KeyChar) && !char.IsLetterOrDigit(e.KeyChar) && e.KeyChar != ' ' && e.KeyChar != '-')
            {
                e.Handled = true;
            }
        }

        private void costoFleteTxt_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Permitir: dígitos, teclas de control (backspace, etc.) y coma decimal
            if (char.IsControl(e.KeyChar))
                return;

            if (char.IsDigit(e.KeyChar))
            {
                // Si ya hay coma, no permitir más de 2 decimales
                if (costoFleteTxt.Text.Contains(","))
                {
                    int posicionComa = costoFleteTxt.Text.IndexOf(',');
                    string decimales = costoFleteTxt.Text.Substring(posicionComa + 1);
                    if (decimales.Length >= 2 && costoFleteTxt.SelectionStart > posicionComa)
                    {
                        e.Handled = true;
                    }
                }
                return;
            }

            if (e.KeyChar == ',')
            {
                // Solo permitir una coma
                if (costoFleteTxt.Text.Contains(","))
                    e.Handled = true;
                return;
            }

            // Bloquear todo lo demás
            e.Handled = true;
        }

        private void VerificarStockMinimoPorMateriales(List<MovimientoMaterial> materialesUsados)
        {
            try
            {
                // Solo verificar materiales reales (no recortes ni insumos)
                var idsAVerificar = materialesUsados
                    .Where(m => !m.EsRecorte && !m.EsInsumo && m.id_material.HasValue && m.id_material.Value > 0)
                    .Select(m => m.id_material.Value)
                    .Distinct()
                    .ToList();

                if (idsAVerificar.Count == 0) return;

                var datos = new MaterialDatos();
                var todosLosMateriales = datos.obtenerMateriales();

                var bajoStock = todosLosMateriales
                    .Where(m => idsAVerificar.Contains(m.id)
                             && m.stockMinimo > 0
                             && m.stockActual <= m.stockMinimo)
                    .ToList();

                if (bajoStock.Count == 0) return;

                int delay = 600; // empieza después del toast de "guardado"
                foreach (var mat in bajoStock)
                {
                    int capturedDelay = delay;
                    var capturedMat = mat;
                    var t = new Timer();
                    t.Interval = capturedDelay;
                    t.Tick += (s, e) =>
                    {
                        t.Stop();
                        t.Dispose();
                        string detalle = SessionManager.DebeOcultarCantidadesStockMateriales
                            ? $"Estado: {SessionManager.ObtenerEstadoStockMaterial(capturedMat.stockActual)}"
                            : $"Actual: {capturedMat.stockActual} | Mínimo: {capturedMat.stockMinimo}";
                        ToastNotification.ShowWarning(
                            $"⚠ Stock bajo: {capturedMat.nombre}\n" +
                            detalle
                        );
                    };
                    t.Start();
                    delay += 600;
                }
            }
            catch { /* silencioso */ }
        }

        // ─────────────────────────────────────────────────────────────────────
        // BORRADOR / AUTOGUARDADO
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Captura el estado actual del formulario y lo persiste como borrador en disco.
        /// Se llama desde el timer cada 30 segundos. No lanza excepciones.
        /// </summary>
        private void GuardarBorrador()
        {
            try
            {
                // Si no hay nada cargado, no guardar un borrador vacío
                if (cmbMovimientos.SelectedIndex < 0 && materialesLista.Count == 0) return;

                var origen = cmbOrigen.SelectedItem as ItemCombo;
                var destino = cmbDestino.SelectedItem as ItemCombo;

                decimal ptDraft = CalcularPrecioTotalMateriales();

                var draft = MovimientoDraftService.CrearDesde(
                    fecha: guna2DateTimePicker1.Value,
                    nroRemito: nroRemitoTxt.Text.Trim(),
                    idTipoMov: cmbMovimientos.SelectedIndex >= 0 ? Convert.ToInt32(cmbMovimientos.SelectedValue) : 0,
                    nombreTipoMov: cmbMovimientos.Text,
                    idOrigen: origen?.IdReal ?? 0,
                    tipoOrigen: origen?.Tipo ?? "",
                    idDestino: destino?.IdReal ?? 0,
                    tipoDestino: destino?.Tipo ?? "",
                    idObra: cmbObras.SelectedIndex >= 0 ? Convert.ToInt32(cmbObras.SelectedValue) : 0,
                    costoFlete: decimal.TryParse(costoFleteTxt.Text.Replace(',', '.'),
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out decimal cf) ? cf : 0,
                    idFlete: (cmbFlete.SelectedValue != null && Convert.ToInt32(cmbFlete.SelectedValue) > 0) ? Convert.ToInt32(cmbFlete.SelectedValue) : (int?)null,
                    precioTotal: ptDraft,
                    observaciones: txtCategoria.Text,
                    materiales: materialesLista
                );

                MovimientoDraftService.Guardar(draft);
            }
            catch { /* silencioso */ }
        }

        /// <summary>
        /// Verifica si existe un borrador válido al abrir la vista y, si el usuario acepta,
        /// restaura todos los campos y la lista de materiales.
        /// </summary>
        private void IntentarRecuperarBorrador()
        {
            var draft = MovimientoDraftService.Leer();
            if (draft == null || (draft.Materiales.Count == 0 && draft.IdTipoMovimiento == 0)) return;

            string hace = "";
            var edad = DateTime.Now - draft.FechaGuardado;
            if (edad.TotalMinutes < 60)
                hace = $"hace {(int)edad.TotalMinutes} min";
            else
                hace = $"hace {(int)edad.TotalHours}h";

            var resultado = MessageBox.Show(
                $"Se encontró un movimiento sin guardar ({hace}).\n\n" +
                $"Tipo: {draft.NombreTipoMovimiento}\n" +
                $"Materiales: {draft.Materiales.Count}\n\n" +
                "¿Desea recuperarlo?",
                "Borrador encontrado",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button1);

            if (resultado != DialogResult.Yes)
            {
                MovimientoDraftService.Eliminar();
                return;
            }

            try
            {
                isProgrammaticChange = true;

                // Fecha y remito
                guna2DateTimePicker1.Value = draft.Fecha == default ? DateTime.Now : draft.Fecha;
                nroRemitoTxt.Text = draft.NroRemito ?? "";

                // Tipo de movimiento
                if (draft.IdTipoMovimiento > 0)
                    cmbMovimientos.SelectedValue = draft.IdTipoMovimiento;

                // Origen
                if (draft.IdOrigen > 0)
                {
                    foreach (var item in cmbOrigen.Items)
                    {
                        if (item is ItemCombo ic && ic.IdReal == draft.IdOrigen && ic.Tipo == draft.TipoOrigen)
                        {
                            cmbOrigen.SelectedItem = item;
                            break;
                        }
                    }
                }

                // Destino
                if (draft.IdDestino > 0)
                {
                    foreach (var item in cmbDestino.Items)
                    {
                        if (item is ItemCombo ic && ic.IdReal == draft.IdDestino && ic.Tipo == draft.TipoDestino)
                        {
                            cmbDestino.SelectedItem = item;
                            break;
                        }
                    }
                }

                // Obra
                if (draft.IdObra > 0)
                    cmbObras.SelectedValue = draft.IdObra;

                // Flete
                if (draft.IdFlete.HasValue && draft.IdFlete.Value > 0)
                {
                    List<Flete> fletesBorrador = new FleteDatos().GetFletes(soloActivos: true, incluirId: draft.IdFlete.Value);
                    fletesBorrador.Insert(0, new Flete { id = 0, nombre = "-" });
                    cmbFlete.DataSource = fletesBorrador;
                    cmbFlete.SelectedValue = draft.IdFlete.Value;
                }
                else
                {
                    cmbFlete.SelectedIndex = 0;
                }

                // Costo flete y observaciones
                costoFleteTxt.Text = draft.CostoFlete > 0 ? draft.CostoFlete.ToString("0.##").Replace('.', ',') : "";
                if (txtPrecioGeneral != null)
                {
                    txtPrecioGeneral.Text = draft.PrecioTotal > 0 ? draft.PrecioTotal.ToString("0.##").Replace('.', ',') : "";
                }
                txtCategoria.Text = draft.Observaciones ?? "";

                // Materiales
                foreach (var md in draft.Materiales)
                {
                    var item = new MovimientoMaterial
                    {
                        id_material = md.IdMaterial,
                        NombreMaterial = md.NombreMaterial,
                        NombreMaterialEventual = md.NombreMaterialEventual,
                        cantidad = md.Cantidad,
                        EsRecorte = md.EsRecorte,
                        IdRecorte = md.IdRecorte,
                        MedidaRecorte = md.MedidaRecorte,
                        EsInsumo = md.EsInsumo,
                        IdInsumo = md.IdInsumo,
                        EsCompraEventual = md.EsCompraEventual,
                        precio_unitario = md.PrecioUnitario
                    };
                    materialesLista.Add(item);
                    AgregarItemLista(item);
                }

                ToastNotification.ShowSuccess("Borrador recuperado correctamente.");
            }
            catch
            {
                ToastNotification.ShowError("No se pudo restaurar el borrador completamente.");
            }
            finally
            {
                isProgrammaticChange = false;
            }
        }

        private void btnAgregarFlete_Click(object sender, EventArgs e)
        {
            using (var modal = new ModalAgregarFlete())
            {
                if (modal.ShowDialog() == DialogResult.OK)
                {
                    int nuevoId = modal.IdFleteCreado;
                    
                    // Recargar fletes e incluir el nuevo ID
                    List<Flete> fletes = new FleteDatos().GetFletes(soloActivos: true, incluirId: nuevoId);
                    fletes.Insert(0, new Flete { id = 0, nombre = "-" });
                    
                    cmbFlete.DataSource = fletes;
                    cmbFlete.DisplayMember = "nombre";
                    cmbFlete.ValueMember = "id";
                    cmbFlete.SelectedValue = nuevoId;
                }
            }
        }

        private void btnEliminarFlete_Click(object sender, EventArgs e)
        {
            if (cmbFlete.SelectedValue == null || Convert.ToInt32(cmbFlete.SelectedValue) == 0)
            {
                ToastNotification.ShowWarning("Seleccione un flete válido para eliminar.");
                return;
            }

            int idSeleccionado = Convert.ToInt32(cmbFlete.SelectedValue);
            string nombreFlete = cmbFlete.Text;

            string msg = $"¿Está seguro de que desea eliminar el flete '{nombreFlete}'?\nSe realizará un borrado lógico (no se mostrará más para nuevos registros, pero se mantendrá el historial).";
            if (MessageBox.Show(msg, "Confirmar Eliminación", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try
                {
                    ComunDatos datos = new ComunDatos();
                    var valores = new System.Collections.Generic.Dictionary<string, object>();
                    valores.Add("activo", false);
                    datos.ActualizarEntidadGenerica("fletes", "id_flete", idSeleccionado, valores);

                    ToastNotification.ShowSuccess("Flete desactivado (borrado lógico) correctamente.");

                    // Recargar fletes
                    List<Flete> fletes = new FleteDatos().GetFletes(soloActivos: true);
                    fletes.Insert(0, new Flete { id = 0, nombre = "-" });
                    
                    cmbFlete.DataSource = fletes;
                    cmbFlete.DisplayMember = "nombre";
                    cmbFlete.ValueMember = "id";
                    cmbFlete.SelectedIndex = 0;
                }
                catch (Exception ex)
                {
                    ToastNotification.ShowError("Error al desactivar el flete: " + ex.Message);
                }
            }
        }
    }
}
