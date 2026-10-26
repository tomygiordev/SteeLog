using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Guna.UI2.WinForms;
using Microsoft.VisualBasic;
using WP_Inventario.Datos;
using WP_Inventario.Modelos;
using WP_Inventario.Reportes;
using WP_Inventario.Servicios;

namespace WP_Inventario.Vistas.Inventario
{
    public partial class Vista_Conteo : Form, IRefrescable, IMessageFilter
    {
        private readonly ConteoInventarioDatos _datos = new ConteoInventarioDatos();
        private readonly MaterialDatos _materialesDatos = new MaterialDatos();
        private List<Material> _materialesFull = new List<Material>();
        private List<ConteoInventario> _historial = new List<ConteoInventario>();

        private string _tabActual = "entrada";
        private ConteoInventario _conteoActual;
        private ConteoInventario _conteoSeleccionadoHistorial;
        private Material _materialSeleccionado;

        private readonly Color colorFondo = Color.FromArgb(29, 38, 35);
        private readonly Color colorPanelOscuro = Color.FromArgb(20, 30, 25);
        private readonly Color colorVerdeAccent = Color.FromArgb(25, 135, 84);
        private readonly Color colorVerdeOscuro = Color.FromArgb(35, 63, 43);
        private readonly Color colorVerdeBoton = Color.FromArgb(47, 84, 57);
        private readonly Color colorFilaAlt = Color.FromArgb(35, 50, 40);
        private readonly Color colorFila = Color.FromArgb(28, 40, 32);
        private readonly Color colorSeleccion = Color.FromArgb(50, 90, 60);
        private readonly Color colorTexto = Color.White;
        private readonly Color colorBorde = Color.FromArgb(60, 60, 65);

        private Guna2Button btnTabEntrada;
        private Guna2Button btnTabHistorial;
        private Panel panelContenido;

        private bool _suprimirBusqueda = false;

        private Label lblEstadoConteo;
        private Label lblResumenConteo;
        private Guna2Button btnIniciarConteo;
        private Guna2Button btnFinalizarConteo;
        private Guna2Button btnCancelarConteo;
        private Guna2TextBox txtBuscadorMaterial;
        private Guna2TextBox txtCantidad;
        private Guna2Button btnAgregar;
        private Panel panelResultados;
        private Timer timerBusqueda;
        private Guna2DataGridView dgvEntrada;

        private Guna2CheckBox chkDesde;
        private Guna2CheckBox chkHasta;
        private Guna2DateTimePicker dtpDesde;
        private Guna2DateTimePicker dtpHasta;
        private Guna2TextBox txtBuscarHistorial;
        private Guna2DataGridView dgvHistorial;
        private Guna2DataGridView dgvDetalleHistorial;
        private Label lblDetalleHistorial;
        private Guna2Button btnContinuar;
        private Guna2Button btnCancelarHistorial;
        private Guna2Button btnImprimir;
        private Guna2Button btnEliminar;

        public Vista_Conteo()
        {
            InitializeComponent();
            Application.AddMessageFilter(this);
            FormClosed += (s, e) =>
            {
                Application.RemoveMessageFilter(this);
                timerBusqueda?.Stop();
                timerBusqueda?.Dispose();
            };

            CargarMateriales();
            CrearInterfaz();
            CargarConteoActual();
        }

        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg == 0x0100 && this.Visible)
            {
                Keys key = (Keys)m.WParam.ToInt32();
                if (key == Keys.F5)
                {
                    Refrescar();
                    return true;
                }
                if (key == Keys.Escape && panelResultados != null && panelResultados.Visible)
                {
                    OcultarResultados();
                    return true;
                }
            }
            return false;
        }

        public void Refrescar()
        {
            CargarMateriales();
            if (_tabActual == "entrada") CargarConteoActual();
            else CargarHistorial();
        }

        private void CargarMateriales()
        {
            try
            {
                _materialesFull = _materialesDatos.obtenerMateriales();
            }
            catch (Exception ex)
            {
                _materialesFull = new List<Material>();
                ToastNotification.ShowError("Error al cargar materiales: " + ex.Message);
            }
        }

        private void CrearInterfaz()
        {
            BackColor = colorFondo;
            Font = new Font("Bahnschrift", 9.5F);

            var panelTitulo = new PanelAntiParpadeo
            {
                Dock = DockStyle.Top,
                Height = 50,
                ColumnCount = 1,
                Padding = new Padding(40, 0, 40, 0)
            };
            panelTitulo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            panelTitulo.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var lblTitulo = new Label
            {
                Text = "CONTEO FISICO DE INVENTARIO",
                Font = new Font("Bahnschrift SemiBold", 21F, FontStyle.Bold),
                ForeColor = colorTexto,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            panelTitulo.Controls.Add(lblTitulo, 0, 0);

            var panelTabs = new PanelAntiParpadeo
            {
                Dock = DockStyle.Top,
                Height = 42,
                ColumnCount = 2,
                Padding = new Padding(40, 0, 40, 0)
            };
            panelTabs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            panelTabs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            panelTabs.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            btnTabEntrada = CrearBotonTab("Entrada");
            btnTabHistorial = CrearBotonTab("Historial");
            btnTabEntrada.Click += (s, e) => CambiarTab("entrada");
            btnTabHistorial.Click += (s, e) => CambiarTab("historial");
            panelTabs.Controls.Add(btnTabEntrada, 0, 0);
            panelTabs.Controls.Add(btnTabHistorial, 1, 0);

            panelContenido = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = colorFondo
            };

            Controls.Add(panelContenido);
            Controls.Add(panelTabs);
            Controls.Add(panelTitulo);

            ActualizarTabs();
            ConstruirEntrada();
        }

        private Guna2Button CrearBotonTab(string texto)
        {
            return new Guna2Button
            {
                Text = texto,
                Font = new Font("Bahnschrift Light", 11F),
                FillColor = colorVerdeOscuro,
                ForeColor = colorTexto,
                BorderRadius = 0,
                Dock = DockStyle.Fill,
                Cursor = Cursors.Hand
            };
        }

        private void CambiarTab(string tab)
        {
            _tabActual = tab;
            ActualizarTabs();
            panelContenido.Controls.Clear();
            if (tab == "entrada")
            {
                ConstruirEntrada();
                CargarConteoActual();
            }
            else
            {
                ConstruirHistorial();
                CargarHistorial();
            }
        }

        private void ActualizarTabs()
        {
            if (btnTabEntrada == null || btnTabHistorial == null) return;
            btnTabEntrada.FillColor = _tabActual == "entrada" ? colorVerdeAccent : colorVerdeOscuro;
            btnTabHistorial.FillColor = _tabActual == "historial" ? colorVerdeAccent : colorVerdeOscuro;
        }

        private void ConstruirEntrada()
        {
            panelContenido.Controls.Clear();

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = colorFondo,
                Padding = new Padding(40, 10, 40, 12)
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));

            root.Controls.Add(CrearPanelEstadoEntrada(), 0, 0);
            root.Controls.Add(CrearPanelCargaEntrada(), 0, 1);
            root.Controls.Add(CrearGridEntrada(), 0, 2);
            root.Controls.Add(CrearPanelAccionesEntrada(), 0, 3);

            panelContenido.Controls.Add(root);
        }

        private Control CrearPanelEstadoEntrada()
        {
            var panel = new PanelAntiParpadeo
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                BackColor = colorPanelOscuro,
                Padding = new Padding(12, 8, 12, 8),
                Margin = new Padding(0, 0, 0, 8)
            };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            lblEstadoConteo = new Label
            {
                Text = "Sin conteo abierto",
                Font = new Font("Bahnschrift SemiBold", 12F, FontStyle.Bold),
                ForeColor = colorTexto,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };
            lblResumenConteo = new Label
            {
                Text = "Inicia un conteo para cargar materiales.",
                Font = new Font("Bahnschrift Light", 10F),
                ForeColor = Color.FromArgb(175, 200, 180),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };
            btnIniciarConteo = CrearBotonAccion("INICIAR CONTEO", colorVerdeAccent);
            btnIniciarConteo.Click += BtnIniciarConteo_Click;
            btnFinalizarConteo = CrearBotonAccion("FINALIZAR", colorVerdeAccent);
            btnFinalizarConteo.Click += BtnFinalizar_Click;

            panel.Controls.Add(lblEstadoConteo, 0, 0);
            panel.Controls.Add(lblResumenConteo, 1, 0);
            panel.Controls.Add(btnIniciarConteo, 2, 0);
            panel.Controls.Add(btnFinalizarConteo, 3, 0);
            return panel;
        }

        private Control CrearPanelCargaEntrada()
        {
            var panel = new PanelAntiParpadeo
            {
                Dock = DockStyle.Fill,
                ColumnCount = 6,
                BackColor = colorFondo,
                Padding = new Padding(0, 4, 0, 8),
                Margin = new Padding(0)
            };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 56));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var lblMaterial = CrearEtiquetaCampo("Material:");
            var lblCantidad = CrearEtiquetaCampo("Cant.:");

            txtBuscadorMaterial = CrearTextBox("Buscar material...");
            txtCantidad = CrearTextBox("0");
            txtCantidad.TextAlign = HorizontalAlignment.Center;
            txtCantidad.KeyPress += SoloDecimal_KeyPress;
            txtCantidad.KeyDown += TxtCantidad_KeyDown;
            txtCantidad.Click += (s, e) => txtCantidad.SelectAll();
            txtCantidad.Margin = new Padding(3, 4, 8, 4);

            btnAgregar = CrearBotonAccion("AGREGAR", colorVerdeBoton);
            btnAgregar.Click += BtnAgregar_Click;
            btnCancelarConteo = CrearBotonAccion("CANCELAR", Color.FromArgb(150, 55, 55));
            btnCancelarConteo.Click += BtnCancelarConteo_Click;

            panel.Controls.Add(lblMaterial, 0, 0);
            panel.Controls.Add(txtBuscadorMaterial, 1, 0);
            panel.Controls.Add(lblCantidad, 2, 0);
            panel.Controls.Add(txtCantidad, 3, 0);
            panel.Controls.Add(btnAgregar, 4, 0);
            panel.Controls.Add(btnCancelarConteo, 5, 0);

            CrearPanelResultados();
            ConfigurarBusqueda();
            return panel;
        }

        private Control CrearGridEntrada()
        {
            dgvEntrada = CrearGridBase(40);
            dgvEntrada.AutoGenerateColumns = false;
            dgvEntrada.Columns.Clear();
            dgvEntrada.Columns.Add(new DataGridViewTextBoxColumn { Name = "colMaterial", HeaderText = "Material", DataPropertyName = "material_nombre_snapshot", FillWeight = 48 });
            dgvEntrada.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCategoria", HeaderText = "Categoria", DataPropertyName = "material_categoria_snapshot", FillWeight = 24 });
            dgvEntrada.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCantidad", HeaderText = "Contado", DataPropertyName = "cantidad_contada", FillWeight = 12 });
            dgvEntrada.Columns.Add(new DataGridViewTextBoxColumn { Name = "colHora", HeaderText = "Hora", DataPropertyName = "fecha_carga", FillWeight = 10 });
            dgvEntrada.Columns.Add(new DataGridViewButtonColumn { Name = "colEditar", HeaderText = "", Text = "Editar", UseColumnTextForButtonValue = true, FillWeight = 9 });
            dgvEntrada.Columns.Add(new DataGridViewButtonColumn { Name = "colQuitar", HeaderText = "", Text = "Quitar", UseColumnTextForButtonValue = true, FillWeight = 9 });
            dgvEntrada.Columns["colCantidad"].DefaultCellStyle.Format = "N2";
            dgvEntrada.Columns["colCantidad"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvEntrada.Columns["colHora"].DefaultCellStyle.Format = "HH:mm";
            dgvEntrada.Columns["colHora"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvEntrada.CellContentClick += DgvEntrada_CellContentClick;
            dgvEntrada.CellFormatting += DgvEntrada_CellFormatting;
            return dgvEntrada;
        }

        private Control CrearPanelAccionesEntrada()
        {
            var panel = new Label
            {
                Dock = DockStyle.Fill,
                Text = "El stock del sistema se oculta durante la carga. Las diferencias se calculan al finalizar.",
                Font = new Font("Bahnschrift Light", 10F),
                ForeColor = Color.FromArgb(155, 180, 160),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(4, 0, 0, 0)
            };
            return panel;
        }

        private void ConfigurarBusqueda()
        {
            timerBusqueda = new Timer { Interval = 180 };
            timerBusqueda.Tick += (s, e) =>
            {
                timerBusqueda.Stop();
                EjecutarBusqueda(txtBuscadorMaterial.Text.Trim());
            };
            txtBuscadorMaterial.TextChanged += (s, e) =>
            {
                if (_suprimirBusqueda) return;
                _materialSeleccionado = null;
                timerBusqueda.Stop();
                timerBusqueda.Start();
            };
            txtBuscadorMaterial.GotFocus += (s, e) => { if (!_suprimirBusqueda) EjecutarBusqueda(txtBuscadorMaterial.Text.Trim()); };
            txtBuscadorMaterial.Click += (s, e) => { if (!_suprimirBusqueda) EjecutarBusqueda(txtBuscadorMaterial.Text.Trim()); };
            txtBuscadorMaterial.KeyDown += TxtBuscadorMaterial_KeyDown;
            txtBuscadorMaterial.LostFocus += (s, e) =>
            {
                var t = new Timer { Interval = 180 };
                t.Tick += (ts, te) =>
                {
                    t.Stop();
                    t.Dispose();
                    if (!txtBuscadorMaterial.Focused && (panelResultados == null || !panelResultados.ContainsFocus))
                        OcultarResultados();
                };
                t.Start();
            };
        }

        private void CrearPanelResultados()
        {
            if (panelResultados != null && !panelResultados.IsDisposed)
            {
                Controls.Remove(panelResultados);
                panelResultados.Dispose();
            }

            panelResultados = new Panel
            {
                Visible = false,
                BackColor = Color.FromArgb(18, 28, 22),
                BorderStyle = BorderStyle.FixedSingle,
                AutoScroll = true,
                Width = 520,
                Height = 220
            };
            Controls.Add(panelResultados);
            panelResultados.BringToFront();
        }

        private void EjecutarBusqueda(string query)
        {
            if (panelResultados == null || txtBuscadorMaterial == null || !_txtActivo()) return;
            panelResultados.Controls.Clear();

            if (query.Length < 2)
            {
                AgregarMensajeBusqueda("Escribi al menos 2 letras para buscar materiales.");
                return;
            }

            var items = _materialesFull
                .Where(m => !string.IsNullOrWhiteSpace(m.nombre) && m.nombre.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderByDescending(m => m.nombre.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                .ThenBy(m => m.nombre)
                .Take(35)
                .ToList();

            if (items.Count == 0)
            {
                AgregarMensajeBusqueda("Sin resultados.");
                return;
            }

            foreach (var material in items.AsEnumerable().Reverse())
                panelResultados.Controls.Add(CrearResultadoMaterial(material));

            PosicionarPanelResultados();
            panelResultados.Visible = true;
            panelResultados.BringToFront();
        }

        private bool _txtActivo()
        {
            return txtBuscadorMaterial != null && !txtBuscadorMaterial.IsDisposed;
        }

        private void AgregarMensajeBusqueda(string texto)
        {
            var lbl = new Label
            {
                Text = texto,
                Dock = DockStyle.Top,
                Height = 42,
                ForeColor = Color.FromArgb(150, 170, 155),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Bahnschrift Light", 10F)
            };
            panelResultados.Controls.Add(lbl);
            PosicionarPanelResultados();
            panelResultados.Visible = true;
            panelResultados.BringToFront();
        }

        private Control CrearResultadoMaterial(Material material)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 46,
                BackColor = Color.FromArgb(24, 36, 29),
                Cursor = Cursors.Hand,
                Padding = new Padding(12, 2, 12, 2)
            };
            var nombre = new Label
            {
                Text = material.nombre,
                Dock = DockStyle.Top,
                Height = 24,
                ForeColor = colorTexto,
                Font = new Font("Bahnschrift", 10.5F, FontStyle.Bold),
                AutoEllipsis = true
            };
            var categoria = new Label
            {
                Text = string.IsNullOrWhiteSpace(material.nombre_categoria) ? "Material" : material.nombre_categoria,
                Dock = DockStyle.Top,
                Height = 18,
                ForeColor = Color.FromArgb(135, 190, 150),
                Font = new Font("Bahnschrift Light", 8.8F),
                AutoEllipsis = true
            };

            panel.Controls.Add(categoria);
            panel.Controls.Add(nombre);

            foreach (Control c in new Control[] { panel, nombre, categoria })
            {
                c.MouseEnter += (s, e) => panel.BackColor = Color.FromArgb(35, 52, 42);
                c.MouseLeave += (s, e) => panel.BackColor = Color.FromArgb(24, 36, 29);
                c.Click += (s, e) => SeleccionarMaterial(material);
            }
            return panel;
        }

        private void SeleccionarMaterial(Material material)
        {
            _suprimirBusqueda = true;
            _materialSeleccionado = material;
            txtBuscadorMaterial.Text = material.nombre;
            txtBuscadorMaterial.Focus();
            txtBuscadorMaterial.SelectionStart = txtBuscadorMaterial.Text.Length;
            _suprimirBusqueda = false;
            OcultarResultados();
        }

        private void PosicionarPanelResultados()
        {
            if (txtBuscadorMaterial == null || panelResultados == null) return;
            var punto = txtBuscadorMaterial.PointToScreen(new Point(0, txtBuscadorMaterial.Height + 2));
            punto = PointToClient(punto);
            panelResultados.Location = punto;
            panelResultados.Width = Math.Max(420, txtBuscadorMaterial.Width);
            panelResultados.Height = 220;
        }

        private void OcultarResultados()
        {
            if (panelResultados == null) return;
            panelResultados.Visible = false;
            panelResultados.Controls.Clear();
        }

        private void CargarConteoActual()
        {
            string usuario = SessionManager.Username ?? SessionManager.Email ?? Environment.UserName;
            try
            {
                _conteoActual = _datos.ObtenerConteoAbiertoPorUsuario(usuario);
                if (dgvEntrada == null) ConstruirEntrada();
                ActualizarEstadoEntrada();
                CargarDetallesEntrada();
            }
            catch (Exception ex)
            {
                ToastNotification.ShowError("Error al cargar conteo: " + ex.Message);
            }
        }

        private void ActualizarEstadoEntrada()
        {
            bool activo = _conteoActual != null;
            if (lblEstadoConteo != null)
            {
                lblEstadoConteo.Text = activo
                    ? $"Conteo #{_conteoActual.id_conteo} abierto"
                    : "Sin conteo abierto";
            }
            if (lblResumenConteo != null)
            {
                lblResumenConteo.Text = activo
                    ? $"Usuario: {_conteoActual.usuario_inicio}  |  Inicio: {_conteoActual.fecha_inicio:dd/MM/yyyy HH:mm}"
                    : "Presiona Iniciar Conteo para habilitar la carga.";
            }

            if (btnIniciarConteo != null) btnIniciarConteo.Enabled = !activo;
            if (btnFinalizarConteo != null) btnFinalizarConteo.Enabled = activo;
            if (btnCancelarConteo != null) btnCancelarConteo.Enabled = activo;
            if (txtBuscadorMaterial != null) txtBuscadorMaterial.Enabled = activo;
            if (txtCantidad != null) txtCantidad.Enabled = activo;
            if (btnAgregar != null) btnAgregar.Enabled = activo;
        }

        private void CargarDetallesEntrada()
        {
            if (dgvEntrada == null) return;
            if (_conteoActual == null)
            {
                dgvEntrada.DataSource = new List<ConteoInventarioDetalle>();
                return;
            }

            dgvEntrada.DataSource = null;
            dgvEntrada.DataSource = _datos.ObtenerDetallesCarga(_conteoActual.id_conteo);
        }

        private void BtnIniciarConteo_Click(object sender, EventArgs e)
        {
            try
            {
                string usuario = SessionManager.Username ?? SessionManager.Email ?? Environment.UserName;
                int id = _datos.CrearConteo(usuario);
                _conteoActual = _datos.ObtenerConteoPorId(id);
                ToastNotification.ShowSuccess("Conteo iniciado.");
                ActualizarEstadoEntrada();
                CargarDetallesEntrada();
                txtBuscadorMaterial.Focus();
            }
            catch (Exception ex)
            {
                ToastNotification.ShowError("No se pudo iniciar: " + ex.Message);
            }
        }

        private void BtnAgregar_Click(object sender, EventArgs e)
        {
            if (_conteoActual == null)
            {
                ToastNotification.ShowWarning("Primero inicia un conteo.");
                return;
            }
            if (_materialSeleccionado == null)
            {
                ToastNotification.ShowWarning("Selecciona un material del buscador.");
                return;
            }
            if (!TryParseCantidad(txtCantidad.Text, out decimal cantidad))
            {
                ToastNotification.ShowWarning("Ingresa una cantidad valida mayor o igual a cero.");
                txtCantidad.Focus();
                return;
            }

            try
            {
                _datos.AgregarOActualizarDetalle(_conteoActual.id_conteo, _materialSeleccionado.id, cantidad);
                _materialSeleccionado = null;
                txtBuscadorMaterial.Clear();
                txtCantidad.Clear();
                OcultarResultados();
                CargarDetallesEntrada();
                txtBuscadorMaterial.Focus();
            }
            catch (Exception ex)
            {
                ToastNotification.ShowError("No se pudo agregar: " + ex.Message);
            }
        }

        private void BtnFinalizar_Click(object sender, EventArgs e)
        {
            if (_conteoActual == null) return;
            if (!_datos.ConteoTieneDetalles(_conteoActual.id_conteo))
            {
                ToastNotification.ShowWarning("No se puede finalizar un conteo sin materiales.");
                return;
            }

            string obs = Interaction.InputBox("Observaciones (opcional):", "Finalizar conteo", "");
            var r = MessageBox.Show(
                $"Finalizar conteo #{_conteoActual.id_conteo}?\n\nSe calcularan las diferencias contra el stock actual.",
                "Confirmar finalizacion",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (r != DialogResult.Yes) return;

            try
            {
                string usuario = SessionManager.Username ?? SessionManager.Email ?? Environment.UserName;
                int idConteoFinalizado = _conteoActual.id_conteo;
                _datos.FinalizarConteo(idConteoFinalizado, usuario, string.IsNullOrWhiteSpace(obs) ? null : obs);
                ToastNotification.ShowSuccess("Conteo finalizado.");
                _conteoActual = null;
                CambiarTab("historial");
                SeleccionarConteoEnGrid(idConteoFinalizado);
            }
            catch (Exception ex)
            {
                ToastNotification.ShowError("No se pudo finalizar: " + ex.Message);
            }
        }

        private void BtnCancelarConteo_Click(object sender, EventArgs e)
        {
            if (_conteoActual == null) return;
            var r = MessageBox.Show(
                $"Cancelar conteo #{_conteoActual.id_conteo}?",
                "Confirmar cancelacion",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            if (r != DialogResult.Yes) return;

            try
            {
                _datos.CancelarConteo(_conteoActual.id_conteo);
                _conteoActual = null;
                ToastNotification.ShowSuccess("Conteo cancelado.");
                ActualizarEstadoEntrada();
                CargarDetallesEntrada();
            }
            catch (Exception ex)
            {
                ToastNotification.ShowError("No se pudo cancelar: " + ex.Message);
            }
        }

        private void DgvEntrada_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || dgvEntrada.Rows[e.RowIndex].DataBoundItem == null) return;
            var det = dgvEntrada.Rows[e.RowIndex].DataBoundItem as ConteoInventarioDetalle;
            if (det == null) return;

            string col = dgvEntrada.Columns[e.ColumnIndex].Name;
            if (col == "colEditar") EditarDetalle(det);
            else if (col == "colQuitar") QuitarDetalle(det);
        }

        private void DgvEntrada_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            string col = dgvEntrada.Columns[e.ColumnIndex].Name;
            if (col == "colHora" && e.Value is DateTime fecha)
            {
                e.Value = fecha.ToString("HH:mm");
                e.FormattingApplied = true;
            }
        }

        private void EditarDetalle(ConteoInventarioDetalle det)
        {
            string input = Interaction.InputBox(
                $"Nueva cantidad contada para:\n\n{det.material_nombre_snapshot}",
                "Editar cantidad",
                det.cantidad_contada.ToString("0.##", CultureInfo.CurrentCulture));
            if (string.IsNullOrWhiteSpace(input)) return;
            if (!TryParseCantidad(input, out decimal cantidad))
            {
                ToastNotification.ShowWarning("Cantidad invalida.");
                return;
            }

            try
            {
                _datos.ReemplazarCantidadDetalle(det.id_conteo, det.id_material, cantidad);
                CargarDetallesEntrada();
            }
            catch (Exception ex)
            {
                ToastNotification.ShowError("No se pudo editar: " + ex.Message);
            }
        }

        private void QuitarDetalle(ConteoInventarioDetalle det)
        {
            var r = MessageBox.Show(
                $"Quitar {det.material_nombre_snapshot} del conteo?",
                "Confirmar",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (r != DialogResult.Yes) return;

            try
            {
                _datos.EliminarDetalle(det.id_detalle);
                CargarDetallesEntrada();
            }
            catch (Exception ex)
            {
                ToastNotification.ShowError("No se pudo quitar: " + ex.Message);
            }
        }

        private void ConstruirHistorial()
        {
            panelContenido.Controls.Clear();

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = colorFondo,
                Padding = new Padding(40, 10, 40, 12)
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 48));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 52));

            root.Controls.Add(CrearPanelFiltrosHistorial(), 0, 0);
            root.Controls.Add(CrearGridHistorial(), 0, 1);

            lblDetalleHistorial = new Label
            {
                Text = "Detalle del conteo seleccionado",
                Font = new Font("Bahnschrift SemiBold", 11F, FontStyle.Bold),
                ForeColor = colorTexto,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.BottomLeft
            };
            root.Controls.Add(lblDetalleHistorial, 0, 2);
            root.Controls.Add(CrearDetalleHistorial(), 0, 3);

            panelContenido.Controls.Add(root);
        }

        private Control CrearPanelFiltrosHistorial()
        {
            var panel = new PanelAntiParpadeo
            {
                Dock = DockStyle.Fill,
                ColumnCount = 7,
                BackColor = Color.FromArgb(26, 34, 30),
                Padding = new Padding(0, 4, 0, 6)
            };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 4));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            chkDesde = CrearCheck("Desde:");
            chkHasta = CrearCheck("Hasta:");
            dtpDesde = CrearFecha(false);
            dtpHasta = CrearFecha(false);
            dtpDesde.Value = DateTime.Now.AddMonths(-1);
            dtpHasta.Value = DateTime.Now;
            txtBuscarHistorial = CrearTextBox("Buscar usuario o material...");

            chkDesde.CheckedChanged += (s, e) => { dtpDesde.Enabled = chkDesde.Checked; CargarHistorial(); };
            chkHasta.CheckedChanged += (s, e) => { dtpHasta.Enabled = chkHasta.Checked; CargarHistorial(); };
            dtpDesde.ValueChanged += (s, e) => CargarHistorial();
            dtpHasta.ValueChanged += (s, e) => CargarHistorial();
            txtBuscarHistorial.TextChanged += (s, e) => CargarHistorial();

            panel.Controls.Add(chkDesde, 0, 0);
            panel.Controls.Add(dtpDesde, 1, 0);
            panel.Controls.Add(chkHasta, 2, 0);
            panel.Controls.Add(dtpHasta, 3, 0);
            panel.Controls.Add(new Panel { BackColor = Color.FromArgb(26, 34, 30) }, 4, 0);
            panel.Controls.Add(txtBuscarHistorial, 5, 0);
            return panel;
        }

        private Control CrearGridHistorial()
        {
            dgvHistorial = CrearGridBase(38);
            dgvHistorial.AutoGenerateColumns = false;
            dgvHistorial.Columns.Clear();
            dgvHistorial.Columns.Add(new DataGridViewTextBoxColumn { Name = "colId", HeaderText = "ID", DataPropertyName = "id_conteo", FillWeight = 6 });
            dgvHistorial.Columns.Add(new DataGridViewTextBoxColumn { Name = "colFecha", HeaderText = "Fecha inicio", DataPropertyName = "fecha_inicio", FillWeight = 16 });
            dgvHistorial.Columns.Add(new DataGridViewTextBoxColumn { Name = "colUsuario", HeaderText = "Usuario", DataPropertyName = "usuario_inicio", FillWeight = 16 });
            dgvHistorial.Columns.Add(new DataGridViewTextBoxColumn { Name = "colEstado", HeaderText = "Estado", DataPropertyName = "estado", FillWeight = 12 });
            dgvHistorial.Columns.Add(new DataGridViewTextBoxColumn { Name = "colItems", HeaderText = "Items", DataPropertyName = "cantidad_materiales", FillWeight = 8 });
            dgvHistorial.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDif", HeaderText = "Diferencias", DataPropertyName = "cantidad_diferencias", FillWeight = 10 });
            dgvHistorial.Columns.Add(new DataGridViewTextBoxColumn { Name = "colObs", HeaderText = "Observaciones", DataPropertyName = "observaciones", FillWeight = 28 });
            dgvHistorial.Columns["colFecha"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvHistorial.Columns["colItems"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvHistorial.Columns["colDif"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            if (SessionManager.DebeOcultarCantidadesStockMateriales)
                dgvHistorial.Columns["colDif"].Visible = false;
            dgvHistorial.CellFormatting += DgvHistorial_CellFormatting;
            dgvHistorial.SelectionChanged += DgvHistorial_SelectionChanged;
            return dgvHistorial;
        }

        private Control CrearDetalleHistorial()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = colorFondo
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));

            dgvDetalleHistorial = CrearGridBase(36);
            dgvDetalleHistorial.AutoGenerateColumns = false;
            dgvDetalleHistorial.Columns.Clear();
            dgvDetalleHistorial.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDMat", HeaderText = "Material", DataPropertyName = "material_nombre_snapshot", FillWeight = 42 });
            dgvDetalleHistorial.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDCat", HeaderText = "Categoria", DataPropertyName = "material_categoria_snapshot", FillWeight = 20 });
            dgvDetalleHistorial.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDCont", HeaderText = "Contado", DataPropertyName = "cantidad_contada", FillWeight = 12 });
            dgvDetalleHistorial.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDStk", HeaderText = "Stock sistema", DataPropertyName = "stock_sistema", FillWeight = 14 });
            dgvDetalleHistorial.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDDif", HeaderText = "Diferencia", DataPropertyName = "diferencia", FillWeight = 12 });
            dgvDetalleHistorial.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDEst", HeaderText = "Estado", DataPropertyName = "estado_diferencia", FillWeight = 14 });
            dgvDetalleHistorial.Columns["colDCont"].DefaultCellStyle.Format = "N2";
            dgvDetalleHistorial.Columns["colDStk"].DefaultCellStyle.Format = "N2";
            dgvDetalleHistorial.Columns["colDDif"].DefaultCellStyle.Format = "N2";
            if (SessionManager.DebeOcultarCantidadesStockMateriales)
            {
                dgvDetalleHistorial.Columns["colDStk"].Visible = false;
                dgvDetalleHistorial.Columns["colDDif"].Visible = false;
                dgvDetalleHistorial.Columns["colDEst"].Visible = false;
            }
            dgvDetalleHistorial.CellFormatting += DgvDetalleHistorial_CellFormatting;

            var botones = new PanelAntiParpadeo
            {
                Dock = DockStyle.Fill,
                ColumnCount = 5,
                BackColor = colorFondo,
                Padding = new Padding(0, 8, 0, 0)
            };
            botones.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            botones.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            botones.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            botones.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            botones.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            botones.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            btnImprimir = CrearBotonAccion("IMPRIMIR", Color.FromArgb(80, 130, 90));
            btnContinuar = CrearBotonAccion("CONTINUAR", Color.FromArgb(60, 120, 180));
            btnCancelarHistorial = CrearBotonAccion("CANCELAR", Color.FromArgb(150, 55, 55));
            btnEliminar = CrearBotonAccion("ELIMINAR", Color.FromArgb(180, 60, 60));
            btnImprimir.Click += BtnImprimir_Click;
            btnContinuar.Click += BtnContinuar_Click;
            btnCancelarHistorial.Click += BtnCancelarHistorial_Click;
            btnEliminar.Click += BtnEliminarHistorial_Click;

            botones.Controls.Add(new Panel { BackColor = colorFondo }, 0, 0);
            botones.Controls.Add(btnImprimir, 1, 0);
            botones.Controls.Add(btnContinuar, 2, 0);
            botones.Controls.Add(btnCancelarHistorial, 3, 0);
            botones.Controls.Add(btnEliminar, 4, 0);

            root.Controls.Add(dgvDetalleHistorial, 0, 0);
            root.Controls.Add(botones, 0, 1);
            return root;
        }

        private void CargarHistorial()
        {
            if (dgvHistorial == null) return;
            try
            {
                DateTime? desde = chkDesde != null && chkDesde.Checked ? (DateTime?)dtpDesde.Value.Date : null;
                DateTime? hasta = chkHasta != null && chkHasta.Checked ? (DateTime?)dtpHasta.Value.Date.AddDays(1).AddSeconds(-1) : null;
                string busqueda = txtBuscarHistorial?.Text?.Trim();
                _historial = _datos.ObtenerHistorial(desde, hasta, string.IsNullOrWhiteSpace(busqueda) ? null : busqueda);
                dgvHistorial.DataSource = null;
                dgvHistorial.DataSource = _historial;

                if (_historial.Count > 0)
                {
                    var primerConteo = _historial[0];
                    _conteoSeleccionadoHistorial = primerConteo;
                    SeleccionarFilaGrid(0);
                    CargarDetalleHistorial(primerConteo);
                }
                else
                {
                    _conteoSeleccionadoHistorial = null;
                    if (dgvDetalleHistorial != null) dgvDetalleHistorial.DataSource = null;
                    if (lblDetalleHistorial != null) lblDetalleHistorial.Text = "Detalle del conteo seleccionado";
                }
                ActualizarBotonesHistorial();
            }
            catch (Exception ex)
            {
                ToastNotification.ShowError("Error al cargar historial: " + ex.Message);
            }
        }

        private void SeleccionarFilaGrid(int rowIndex)
        {
            if (dgvHistorial == null || rowIndex < 0 || rowIndex >= dgvHistorial.Rows.Count) return;
            try
            {
                dgvHistorial.ClearSelection();
                dgvHistorial.Rows[rowIndex].Selected = true;
                dgvHistorial.CurrentCell = dgvHistorial.Rows[rowIndex].Cells[0];
                dgvHistorial.FirstDisplayedScrollingRowIndex = rowIndex;
            }
            catch { }
        }

        private void SeleccionarConteoEnGrid(int idConteo)
        {
            if (dgvHistorial == null || dgvHistorial.Rows.Count == 0) return;
            ConteoInventario conteoSel = null;
            int idxSel = -1;
            for (int i = 0; i < dgvHistorial.Rows.Count; i++)
            {
                if (dgvHistorial.Rows[i].DataBoundItem is ConteoInventario c && c.id_conteo == idConteo)
                {
                    conteoSel = c;
                    idxSel = i;
                    break;
                }
            }
            if (conteoSel != null)
            {
                _conteoSeleccionadoHistorial = conteoSel;
                SeleccionarFilaGrid(idxSel);
                CargarDetalleHistorial(conteoSel);
                ActualizarBotonesHistorial();
            }
        }

        private void DgvHistorial_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvHistorial.CurrentRow != null && dgvHistorial.CurrentRow.DataBoundItem is ConteoInventario conteo)
            {
                _conteoSeleccionadoHistorial = conteo;
                CargarDetalleHistorial(conteo);
            }
            else
            {
                _conteoSeleccionadoHistorial = null;
                dgvDetalleHistorial.DataSource = null;
            }
            ActualizarBotonesHistorial();
        }

        private void CargarDetalleHistorial(ConteoInventario conteo)
        {
            if (conteo == null || dgvDetalleHistorial == null) return;
            try
            {
                if ((conteo.estado ?? "").Equals("finalizado", StringComparison.OrdinalIgnoreCase))
                {
                    var difs = _datos.ObtenerDiferencias(conteo.id_conteo);
                    dgvDetalleHistorial.DataSource = difs;
                    lblDetalleHistorial.Text = $"Diferencias del conteo #{conteo.id_conteo} - {difs.Count} material(es)";
                }
                else
                {
                    var dets = _datos.ObtenerDetallesCarga(conteo.id_conteo)
                        .Select(d => new ConteoInventarioDiferencia
                        {
                            id_detalle = d.id_detalle,
                            id_conteo = d.id_conteo,
                            id_material = d.id_material,
                            material_nombre_snapshot = d.material_nombre_snapshot,
                            material_categoria_snapshot = d.material_categoria_snapshot,
                            cantidad_contada = d.cantidad_contada,
                            fecha_carga = d.fecha_carga
                        })
                        .ToList();
                    dgvDetalleHistorial.DataSource = dets;
                    lblDetalleHistorial.Text = $"Materiales cargados en conteo #{conteo.id_conteo} - {dets.Count} material(es)";
                }
            }
            catch (Exception ex)
            {
                ToastNotification.ShowError("Error al cargar detalle: " + ex.Message);
            }
        }

        private void ActualizarBotonesHistorial()
        {
            bool hay = _conteoSeleccionadoHistorial != null;
            string estado = hay ? (_conteoSeleccionadoHistorial.estado ?? "").ToLower() : "";
            bool esAdmin = SessionManager.Role == UserRole.Admin;
            bool esCreador = hay && string.Equals(
                _conteoSeleccionadoHistorial.usuario_inicio,
                SessionManager.Username ?? SessionManager.Email,
                StringComparison.OrdinalIgnoreCase);

            if (btnContinuar != null) btnContinuar.Enabled = hay && estado == "abierto" && esCreador;
            if (btnCancelarHistorial != null) btnCancelarHistorial.Enabled = hay && estado == "abierto" && (esAdmin || esCreador);
            if (btnImprimir != null) btnImprimir.Enabled = hay;
            if (btnEliminar != null)
            {
                bool esFinalizadoOCancelado = estado == "finalizado" || estado == "cancelado";
                btnEliminar.Visible = esAdmin;
                btnEliminar.Enabled = esAdmin && hay && esFinalizadoOCancelado;
            }
        }

        private void BtnContinuar_Click(object sender, EventArgs e)
        {
            if (_conteoSeleccionadoHistorial == null) return;
            _conteoActual = _conteoSeleccionadoHistorial;
            CambiarTab("entrada");
        }

        private void BtnCancelarHistorial_Click(object sender, EventArgs e)
        {
            if (_conteoSeleccionadoHistorial == null) return;
            var r = MessageBox.Show(
                $"Cancelar conteo #{_conteoSeleccionadoHistorial.id_conteo}?",
                "Confirmar cancelacion",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            if (r != DialogResult.Yes) return;

            try
            {
                _datos.CancelarConteo(_conteoSeleccionadoHistorial.id_conteo);
                CargarHistorial();
            }
            catch (Exception ex)
            {
                ToastNotification.ShowError("No se pudo cancelar: " + ex.Message);
            }
        }

        private void BtnImprimir_Click(object sender, EventArgs e)
        {
            if (_conteoSeleccionadoHistorial == null) return;

            try
            {
                List<ConteoInventarioDiferencia> detalle;
                string estado = (_conteoSeleccionadoHistorial.estado ?? "").ToLower();
                if (estado == "finalizado")
                {
                    detalle = _datos.ObtenerDiferencias(_conteoSeleccionadoHistorial.id_conteo);
                }
                else
                {
                    detalle = _datos.ObtenerDetallesCarga(_conteoSeleccionadoHistorial.id_conteo)
                        .Select(d => new ConteoInventarioDiferencia
                        {
                            id_detalle = d.id_detalle,
                            id_conteo = d.id_conteo,
                            id_material = d.id_material,
                            material_nombre_snapshot = d.material_nombre_snapshot,
                            material_categoria_snapshot = d.material_categoria_snapshot,
                            cantidad_contada = d.cantidad_contada,
                            fecha_carga = d.fecha_carga
                        })
                        .ToList();
                }

                string nombre = ConteoInventarioPDF.SugerirNombreArchivo(_conteoSeleccionadoHistorial);
                string ruta = Path.Combine(Path.GetTempPath(), nombre);

                if (File.Exists(ruta)) File.Delete(ruta);

                ConteoInventarioPDF.Generar(ruta, _conteoSeleccionadoHistorial, detalle);
                Process.Start(new ProcessStartInfo(ruta) { UseShellExecute = true });
                ToastNotification.ShowSuccess("PDF generado correctamente.");
            }
            catch (Exception ex)
            {
                ToastNotification.ShowError("No se pudo generar el PDF: " + ex.Message);
            }
        }

        private void BtnEliminarHistorial_Click(object sender, EventArgs e)
        {
            if (_conteoSeleccionadoHistorial == null) return;
            if (SessionManager.Role != UserRole.Admin)
            {
                ToastNotification.ShowWarning("Solo los administradores pueden eliminar conteos.");
                return;
            }

            string estado = (_conteoSeleccionadoHistorial.estado ?? "").ToLower();
            if (estado == "abierto")
            {
                ToastNotification.ShowWarning("El conteo está abierto. Cancelalo antes de eliminarlo.");
                return;
            }

            var r = MessageBox.Show(
                $"Eliminar definitivamente el conteo #{_conteoSeleccionadoHistorial.id_conteo}?\n\n" +
                "Se borrarán la cabecera y todos los detalles asociados. Esta acción no se puede deshacer.",
                "Confirmar eliminación",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            if (r != DialogResult.Yes) return;

            try
            {
                _datos.EliminarConteo(_conteoSeleccionadoHistorial.id_conteo);
                ToastNotification.ShowSuccess("Conteo eliminado correctamente.");
                CargarHistorial();
            }
            catch (Exception ex)
            {
                ToastNotification.ShowError("No se pudo eliminar: " + ex.Message);
            }
        }

        private void DgvHistorial_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || !(dgvHistorial.Rows[e.RowIndex].DataBoundItem is ConteoInventario conteo)) return;
            string col = dgvHistorial.Columns[e.ColumnIndex].Name;
            if (col == "colFecha")
            {
                e.Value = conteo.fecha_inicio.ToString("dd/MM/yyyy HH:mm");
                e.FormattingApplied = true;
            }
            else if (col == "colEstado")
            {
                string estado = (conteo.estado ?? "").ToLower();
                if (estado == "abierto") e.CellStyle.ForeColor = Color.FromArgb(255, 200, 100);
                else if (estado == "finalizado") e.CellStyle.ForeColor = Color.FromArgb(115, 210, 130);
                else if (estado == "cancelado") e.CellStyle.ForeColor = Color.FromArgb(230, 110, 100);
                e.CellStyle.SelectionForeColor = e.CellStyle.ForeColor;
            }
        }

        private void DgvDetalleHistorial_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || !(dgvDetalleHistorial.Rows[e.RowIndex].DataBoundItem is ConteoInventarioDiferencia det)) return;
            string col = dgvDetalleHistorial.Columns[e.ColumnIndex].Name;
            if (col == "colDEst")
            {
                string estado = det.estado_diferencia;
                if (estado == "Sobrante") e.CellStyle.ForeColor = Color.FromArgb(255, 200, 100);
                else if (estado == "Faltante") e.CellStyle.ForeColor = Color.FromArgb(255, 100, 100);
                else e.CellStyle.ForeColor = Color.FromArgb(170, 185, 175);
                e.CellStyle.SelectionForeColor = e.CellStyle.ForeColor;
            }
        }

        private Guna2DataGridView CrearGridBase(int altoFila)
        {
            var grid = new Guna2DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                ColumnHeadersHeight = 40,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                RowTemplate = { Height = altoFila },
                EnableHeadersVisualStyles = false,
                BackgroundColor = colorPanelOscuro,
                GridColor = Color.FromArgb(40, 60, 50),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            grid.ThemeStyle.BackColor = colorPanelOscuro;
            grid.ThemeStyle.GridColor = Color.FromArgb(40, 60, 50);
            grid.ThemeStyle.HeaderStyle.BackColor = colorVerdeOscuro;
            grid.ThemeStyle.HeaderStyle.ForeColor = colorTexto;
            grid.ThemeStyle.HeaderStyle.Font = new Font("Bahnschrift", 10.5F, FontStyle.Bold);
            grid.ThemeStyle.HeaderStyle.Height = 40;
            grid.ThemeStyle.RowsStyle.BackColor = colorFila;
            grid.ThemeStyle.RowsStyle.ForeColor = colorTexto;
            grid.ThemeStyle.RowsStyle.Font = new Font("Bahnschrift Light", 10F);
            grid.ThemeStyle.RowsStyle.Height = altoFila;
            grid.ThemeStyle.RowsStyle.SelectionBackColor = colorSeleccion;
            grid.ThemeStyle.RowsStyle.SelectionForeColor = colorTexto;
            grid.ThemeStyle.AlternatingRowsStyle.BackColor = colorFilaAlt;
            grid.ThemeStyle.AlternatingRowsStyle.ForeColor = colorTexto;
            grid.ThemeStyle.AlternatingRowsStyle.SelectionBackColor = colorSeleccion;
            grid.ThemeStyle.AlternatingRowsStyle.SelectionForeColor = colorTexto;
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = colorVerdeOscuro;
            grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = colorTexto;
            return grid;
        }

        private Guna2TextBox CrearTextBox(string placeholder)
        {
            return new Guna2TextBox
            {
                PlaceholderText = placeholder,
                Font = new Font("Segoe UI", 10.5F),
                ForeColor = Color.FromArgb(230, 230, 230),
                FillColor = Color.FromArgb(35, 50, 40),
                BorderColor = Color.FromArgb(55, 75, 62),
                BorderRadius = 0,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 4, 8, 4),
                FocusedState = { BorderColor = Color.FromArgb(39, 174, 96) },
                HoverState = { BorderColor = Color.FromArgb(60, 120, 80) }
            };
        }

        private Guna2Button CrearBotonAccion(string texto, Color color)
        {
            return new Guna2Button
            {
                Text = texto,
                Font = new Font("Bahnschrift Light", 11F),
                FillColor = color,
                ForeColor = colorTexto,
                BorderRadius = 0,
                Dock = DockStyle.Fill,
                Margin = new Padding(4),
                Cursor = Cursors.Hand
            };
        }

        private Label CrearEtiquetaCampo(string texto)
        {
            return new Label
            {
                Text = texto,
                Dock = DockStyle.Fill,
                Font = new Font("Bahnschrift Light", 10.5F),
                ForeColor = Color.FromArgb(155, 185, 165),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(14, 0, 6, 0)
            };
        }

        private Guna2CheckBox CrearCheck(string texto)
        {
            return new Guna2CheckBox
            {
                Text = texto,
                Font = new Font("Bahnschrift Light", 10.5F),
                ForeColor = Color.FromArgb(155, 185, 165),
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                CheckedState =
                {
                    BorderColor = Color.FromArgb(39, 174, 96),
                    BorderRadius = 0,
                    BorderThickness = 0,
                    FillColor = Color.FromArgb(39, 174, 96)
                },
                UncheckedState =
                {
                    BorderColor = Color.FromArgb(80, 100, 90),
                    BorderRadius = 0,
                    BorderThickness = 2,
                    FillColor = Color.FromArgb(35, 50, 40)
                }
            };
        }

        private Guna2DateTimePicker CrearFecha(bool enabled)
        {
            return new Guna2DateTimePicker
            {
                BorderColor = Color.FromArgb(55, 75, 62),
                Checked = true,
                FillColor = Color.FromArgb(35, 50, 40),
                Font = new Font("Segoe UI", 10F),
                ForeColor = colorTexto,
                Format = DateTimePickerFormat.Short,
                HoverState = { BorderColor = Color.FromArgb(39, 174, 96) },
                FocusedColor = Color.FromArgb(39, 174, 96),
                MaxDate = new DateTime(9998, 12, 31, 0, 0, 0, 0),
                MinDate = new DateTime(1753, 1, 1, 0, 0, 0, 0),
                Enabled = enabled,
                Dock = DockStyle.Fill,
                Margin = new Padding(4)
            };
        }

        private void SoloDecimal_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != ',' && e.KeyChar != '.')
                e.Handled = true;
            if (e.KeyChar == '.') e.KeyChar = ',';
            if (e.KeyChar == ',' && ((TextBoxBase)sender).Text.Contains(",")) e.Handled = true;
        }

        private bool TryParseCantidad(string texto, out decimal cantidad)
        {
            texto = (texto ?? "").Trim().Replace('.', ',');
            return decimal.TryParse(texto, NumberStyles.Any, CultureInfo.CurrentCulture, out cantidad) && cantidad >= 0;
        }

        private void TxtCantidad_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                BtnAgregar_Click(btnAgregar, EventArgs.Empty);
                e.SuppressKeyPress = true;
            }
        }

        private void TxtBuscadorMaterial_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Down && panelResultados != null && panelResultados.Visible && panelResultados.Controls.Count > 0)
            {
                panelResultados.Controls[0].Focus();
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Enter && _materialSeleccionado != null)
            {
                txtCantidad.Focus();
                e.SuppressKeyPress = true;
            }
        }
    }
}
