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
using System.Threading;
using System.Threading.Tasks;
using Timer = System.Windows.Forms.Timer;
using SteeLog.Core.Application.Conteos;
using SteeLog.Core.Application.Inventario;
using SteeLog.CompositionRoot;
using SteeLog.Presentation;


using SteeLog.Servicios;

namespace SteeLog.Vistas.Inventario
{
    public partial class Vista_Conteo : Form, IRefrescable, IMessageFilter
    {
        private ConteosPresenter presenter;
        private IConteoPdf pdf;
        private readonly Action<string> abrirPdf;
        private readonly Action<string> mostrarError;
        private readonly Action<string> mostrarExito;
        private readonly CancellationTokenSource cierre = new CancellationTokenSource();
        private int versionEntrada, versionHistorial, versionDetalle, versionMateriales;
        private bool ocupada, entradaLista;
        private List<MaterialInventarioLectura> _materialesFull = new List<MaterialInventarioLectura>();
        private List<ConteoLectura> _historial = new List<ConteoLectura>();

        private string _tabActual = "entrada";
        private ConteoLectura _conteoActual;
        private ConteoLectura _conteoSeleccionadoHistorial;
        private MaterialInventarioLectura _materialSeleccionado;

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

        public Vista_Conteo() : this(null, null, null) { }
        public Vista_Conteo(IUseCaseExecutor executor, IConteoPdf renderer) : this(executor, renderer, null) { }
        public Vista_Conteo(IUseCaseExecutor executor, IConteoPdf renderer, Action<string> abrirPdf) : this(executor, renderer, abrirPdf, null, null) { }
        public Vista_Conteo(IUseCaseExecutor executor, IConteoPdf renderer, Action<string> abrirPdf, Action<string> mostrarError, Action<string> mostrarExito)
        {
            this.mostrarError = mostrarError ?? (mensaje => ToastNotification.ShowError(mensaje));
            this.mostrarExito = mostrarExito ?? (mensaje => ToastNotification.ShowSuccess(mensaje));
            this.abrirPdf = abrirPdf ?? (ruta => Process.Start(new ProcessStartInfo(ruta) { UseShellExecute = true }));
            if (executor != null) presenter = new ConteosPresenter(executor);
            pdf = renderer;
            InitializeComponent();
            CrearInterfaz(); ActualizarEstadoEntrada();
            Shown += async (s, e) => {
                if (presenter == null) presenter = new ConteosPresenter(LocalServices.Current.Executor);
                if (pdf == null) pdf = LocalServices.Current.ConteoPdf;
                Application.AddMessageFilter(this);
                await CargarMateriales(); if (Vigente && _tabActual == "entrada") await CargarConteoActual();
            };
            FormClosed += (s, e) => Cerrar();
            Disposed += (s, e) => Cerrar();
        }
        private void Cerrar()
        {
            if (!cierre.IsCancellationRequested) cierre.Cancel();
            Application.RemoveMessageFilter(this);
            timerBusqueda?.Stop(); timerBusqueda?.Dispose();
        }
        private bool Vigente => !cierre.IsCancellationRequested && !IsDisposed;
        private bool Comenzar() { if (ocupada || !Vigente) return false; ocupada = true; btnTabEntrada.Enabled = false; btnTabHistorial.Enabled = false; ActualizarEstadoEntrada(); ActualizarBotonesHistorial(); return true; }
        private void Terminar() { ocupada = false; if (Vigente) { btnTabEntrada.Enabled = true; btnTabHistorial.Enabled = true; ActualizarEstadoEntrada(); ActualizarBotonesHistorial(); } }

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

        public async void Refrescar()
        {
            if (presenter == null || !Vigente || ocupada) return;
            await CargarMateriales();
            if (!Vigente || ocupada) return;
            if (_tabActual == "entrada") await CargarConteoActual(); else await CargarHistorial();
        }

        private async Task CargarMateriales()
        {
            int version = ++versionMateriales;
            try { var datos = await presenter.MaterialesAsync(cierre.Token); if (Vigente && version == versionMateriales) _materialesFull = datos.ToList(); }
            catch (OperationCanceledException) { }
            catch (Exception ex) { if (Vigente && version == versionMateriales) ToastNotification.ShowError("Error al cargar materiales: " + ex.Message); }
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

        private async void CambiarTab(string tab, int continuarId = 0)
        {
            _tabActual = tab; versionEntrada++; versionHistorial++; versionDetalle++; entradaLista = false;
            ActualizarTabs(); panelContenido.Controls.Clear();
            if (tab == "entrada") {
                _conteoActual = null;
                ConstruirEntrada(); ActualizarEstadoEntrada();
                if (continuarId == 0) await CargarConteoActual();
                else {
                    int version = ++versionEntrada; entradaLista = false; ActualizarEstadoEntrada();
                    try { var elegido = await presenter.ObtenerAsync(continuarId, cierre.Token);
                        if (!Vigente || version != versionEntrada || _tabActual != "entrada") return;
                        if (elegido == null || !string.Equals(elegido.Estado, "abierto", StringComparison.OrdinalIgnoreCase)) { await CargarConteoActual(); return; }
                        _conteoActual = elegido; ActualizarEstadoEntrada(); await CargarDetallesEntrada();
                    } catch (OperationCanceledException) { } catch (Exception ex) { if (Vigente && version == versionEntrada) ToastNotification.ShowError("Error al continuar: " + ex.Message); }
                }
            }
            else { ConstruirHistorial(); await CargarHistorial(); if (Vigente && _tabActual == "historial" && continuarId > 0) SeleccionarConteoEnGrid(continuarId); }
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
            dgvEntrada.Columns.Add(new DataGridViewTextBoxColumn { Name = "colMaterial", HeaderText = "Material", DataPropertyName = "Nombre", FillWeight = 48 });
            dgvEntrada.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCategoria", HeaderText = "Categoria", DataPropertyName = "Categoria", FillWeight = 24 });
            dgvEntrada.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCantidad", HeaderText = "Contado", DataPropertyName = "Cantidad", FillWeight = 12 });
            dgvEntrada.Columns.Add(new DataGridViewTextBoxColumn { Name = "colHora", HeaderText = "Hora", DataPropertyName = "FechaCarga", FillWeight = 10 });
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
            timerBusqueda?.Stop(); timerBusqueda?.Dispose();
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
                .Where(m => !string.IsNullOrWhiteSpace(m.Nombre) && m.Nombre.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderByDescending(m => m.Nombre.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                .ThenBy(m => m.Nombre)
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

        private Control CrearResultadoMaterial(MaterialInventarioLectura material)
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
                Text = material.Nombre,
                Dock = DockStyle.Top,
                Height = 24,
                ForeColor = colorTexto,
                Font = new Font("Bahnschrift", 10.5F, FontStyle.Bold),
                AutoEllipsis = true
            };
            var categoria = new Label
            {
                Text = string.IsNullOrWhiteSpace(material.Categoria) ? "Material" : material.Categoria,
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

        private void SeleccionarMaterial(MaterialInventarioLectura material)
        {
            _suprimirBusqueda = true;
            _materialSeleccionado = material;
            txtBuscadorMaterial.Text = material.Nombre;
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

        private async Task CargarConteoActual()
        {
            if (presenter == null || !Vigente) return;
            int version = ++versionEntrada;
            entradaLista = false; _conteoActual = null; ActualizarEstadoEntrada();
            if (dgvEntrada != null) dgvEntrada.DataSource = null;
            try {
                var conteo = await presenter.AbiertoAsync(cierre.Token);
                if (!Vigente || version != versionEntrada || _tabActual != "entrada") return;
                _conteoActual = conteo; ActualizarEstadoEntrada(); await CargarDetallesEntrada();
            } catch (OperationCanceledException) { }
            catch (Exception ex) { if (Vigente && version == versionEntrada) ToastNotification.ShowError("Error al cargar conteo: " + ex.Message); }
        }

        private void ActualizarEstadoEntrada()
        {
            bool activo = entradaLista && _conteoActual != null;
            if (lblEstadoConteo != null)
            {
                lblEstadoConteo.Text = activo
                    ? $"Conteo #{_conteoActual.Id} abierto"
                    : "Sin conteo abierto";
            }
            if (lblResumenConteo != null)
            {
                lblResumenConteo.Text = activo
                    ? $"Usuario: {MostrarUsuario(_conteoActual.UsuarioInicio)}  |  Inicio: {_conteoActual.Inicio:dd/MM/yyyy HH:mm}"
                    : "Presiona Iniciar Conteo para habilitar la carga.";
            }

            if (btnIniciarConteo != null) btnIniciarConteo.Enabled = entradaLista && !ocupada && !activo;
            if (btnFinalizarConteo != null) btnFinalizarConteo.Enabled = !ocupada && activo;
            if (btnCancelarConteo != null) btnCancelarConteo.Enabled = !ocupada && activo;
            if (txtBuscadorMaterial != null) txtBuscadorMaterial.Enabled = activo;
            if (txtCantidad != null) txtCantidad.Enabled = activo;
            if (btnAgregar != null) btnAgregar.Enabled = !ocupada && activo;
        }

        private async Task CargarDetallesEntrada()
        {
            if (!Vigente || dgvEntrada == null) return;
            int version = ++versionEntrada; int id = _conteoActual?.Id ?? 0;
            entradaLista = false; ActualizarEstadoEntrada();
            if (id == 0) { dgvEntrada.DataSource = new List<DetalleConteoLectura>(); entradaLista = true; ActualizarEstadoEntrada(); return; }
            try {
                var detalles = await presenter.DetallesAsync(id, cierre.Token);
                if (!Vigente || _tabActual != "entrada" || version != versionEntrada || _conteoActual?.Id != id) return;
                dgvEntrada.DataSource = detalles.ToList(); entradaLista = true; ActualizarEstadoEntrada();
            } catch (OperationCanceledException) { }
            catch (Exception ex) { if (Vigente && version == versionEntrada) ToastNotification.ShowError("Error al cargar detalles: " + ex.Message); }
        }

        private async void BtnIniciarConteo_Click(object sender, EventArgs e)
        {
            if (!entradaLista || _conteoActual != null || !Comenzar()) return; int version = versionEntrada;
            bool creado = false;
            try { int id = await presenter.CrearAsync(cierre.Token); creado = true; var conteo = await presenter.ObtenerAsync(id, cierre.Token);
                if (!Vigente || _tabActual != "entrada" || version != versionEntrada) return;
                if (conteo == null) throw new InvalidOperationException("No se encontró el conteo creado.");
                _conteoActual = conteo; ActualizarEstadoEntrada(); await CargarDetallesEntrada(); mostrarExito("Conteo iniciado.");
            } catch (OperationCanceledException) { } catch (Exception ex) {
                if (Vigente && _tabActual == "entrada" && version == versionEntrada) {
                    entradaLista = false; ActualizarEstadoEntrada();
                    mostrarError((creado ? "El conteo se creó, pero no se pudo recuperar. " : "No se pudo confirmar si se creó el conteo. ") + "Actualizá con F5 antes de volver a intentar. " + ex.Message);
                }
            } finally { Terminar(); }
        }

        private async void BtnAgregar_Click(object sender, EventArgs e)
        {
            if (!entradaLista) return;
            if (_conteoActual == null || _materialSeleccionado == null) { ToastNotification.ShowWarning("Inicia un conteo y selecciona un material."); return; }
            if (!TryParseCantidad(txtCantidad.Text, out decimal cantidad)) { ToastNotification.ShowWarning("Ingresa una cantidad valida mayor o igual a cero."); return; }
            if (!Comenzar()) return; int id = _conteoActual.Id, material = _materialSeleccionado.Id; int version = versionEntrada;
            try { await presenter.CargarAsync(id, material, cantidad, cierre.Token);
                if (!Vigente || _tabActual != "entrada" || _conteoActual?.Id != id || version != versionEntrada) return;
                _materialSeleccionado = null; txtBuscadorMaterial.Clear(); txtCantidad.Clear(); OcultarResultados(); await CargarDetallesEntrada();
            } catch (OperationCanceledException) { } catch (Exception ex) {
                if (Vigente && _tabActual == "entrada" && _conteoActual?.Id == id && version == versionEntrada) {
                    entradaLista = false; ActualizarEstadoEntrada();
                    mostrarError("No se pudo confirmar si se agregó la cantidad. Actualizá con F5 y verificá el conteo antes de volver a cargarla. " + ex.Message);
                }
            } finally { Terminar(); }
        }

        private async void BtnFinalizar_Click(object sender, EventArgs e)
        {
            if (!entradaLista || _conteoActual == null || ocupada) return;
            string obs = Interaction.InputBox("Observaciones (opcional):", "Finalizar conteo", "");
            int id = _conteoActual.Id;
            if (MessageBox.Show($"Finalizar conteo #{id}?\n\nSe calcularan las diferencias contra el stock actual.", "Confirmar finalizacion", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes || !Comenzar()) return;
            int version = versionEntrada;
            try { await presenter.FinalizarAsync(id, obs, cierre.Token); if (!Vigente || _tabActual != "entrada" || _conteoActual?.Id != id || version != versionEntrada) return;
                _conteoActual = null; ToastNotification.ShowSuccess("Conteo finalizado."); CambiarTab("historial", id);
            } catch (OperationCanceledException) { } catch (Exception ex) { if (Vigente) ToastNotification.ShowError("No se pudo finalizar: " + ex.Message); } finally { Terminar(); }
        }

        private async void BtnCancelarConteo_Click(object sender, EventArgs e)
        {
            if (!entradaLista || _conteoActual == null || ocupada) return; int id = _conteoActual.Id;
            if (MessageBox.Show($"Cancelar conteo #{id}?", "Confirmar cancelacion", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes || !Comenzar()) return;
            int version = versionEntrada;
            try { await presenter.CancelarAsync(id, cierre.Token); if (!Vigente || _tabActual != "entrada" || _conteoActual?.Id != id || version != versionEntrada) return;
                _conteoActual = null; ActualizarEstadoEntrada(); await CargarDetallesEntrada(); ToastNotification.ShowSuccess("Conteo cancelado.");
            } catch (OperationCanceledException) { } catch (Exception ex) { if (Vigente) ToastNotification.ShowError("No se pudo cancelar: " + ex.Message); } finally { Terminar(); }
        }

        private void DgvEntrada_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || dgvEntrada.Rows[e.RowIndex].DataBoundItem == null) return;
            var det = dgvEntrada.Rows[e.RowIndex].DataBoundItem as DetalleConteoLectura;
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

        private async void EditarDetalle(DetalleConteoLectura det)
        {
            if (!entradaLista || ocupada) return; string input = Interaction.InputBox($"Nueva cantidad contada para:\n\n{det.Nombre}", "Editar cantidad", det.Cantidad.ToString("0.##", CultureInfo.CurrentCulture));
            if (string.IsNullOrWhiteSpace(input)) return; if (!TryParseCantidad(input, out decimal cantidad)) { ToastNotification.ShowWarning("Cantidad invalida."); return; }
            int id = det.ConteoId; if (_conteoActual?.Id != id || !Comenzar()) return; int version = versionEntrada;
            try { await presenter.CorregirAsync(id, det.MaterialId, cantidad, cierre.Token); if (Vigente && _tabActual == "entrada" && _conteoActual?.Id == id && version == versionEntrada) await CargarDetallesEntrada(); }
            catch (OperationCanceledException) { } catch (Exception ex) { if (Vigente) ToastNotification.ShowError("No se pudo editar: " + ex.Message); } finally { Terminar(); }
        }

        private async void QuitarDetalle(DetalleConteoLectura det)
        {
            if (!entradaLista || ocupada || MessageBox.Show($"Quitar {det.Nombre} del conteo?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            int id = det.ConteoId; if (_conteoActual?.Id != id || !Comenzar()) return; int version = versionEntrada;
            try { await presenter.QuitarAsync(id, det.Id, cierre.Token); if (Vigente && _tabActual == "entrada" && _conteoActual?.Id == id && version == versionEntrada) await CargarDetallesEntrada(); }
            catch (OperationCanceledException) { } catch (Exception ex) { if (Vigente) ToastNotification.ShowError("No se pudo quitar: " + ex.Message); } finally { Terminar(); }
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

            chkDesde.CheckedChanged += (s, e) => { dtpDesde.Enabled = chkDesde.Checked; _ = CargarHistorial(); };
            chkHasta.CheckedChanged += (s, e) => { dtpHasta.Enabled = chkHasta.Checked; _ = CargarHistorial(); };
            dtpDesde.ValueChanged += (s, e) => _ = CargarHistorial();
            dtpHasta.ValueChanged += (s, e) => _ = CargarHistorial();
            txtBuscarHistorial.TextChanged += (s, e) => _ = CargarHistorial();

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
            dgvHistorial.Columns.Add(new DataGridViewTextBoxColumn { Name = "colId", HeaderText = "ID", DataPropertyName = "Id", FillWeight = 6 });
            dgvHistorial.Columns.Add(new DataGridViewTextBoxColumn { Name = "colFecha", HeaderText = "Fecha inicio", DataPropertyName = "Inicio", FillWeight = 16 });
            dgvHistorial.Columns.Add(new DataGridViewTextBoxColumn { Name = "colUsuario", HeaderText = "Usuario", DataPropertyName = "UsuarioInicio", FillWeight = 16 });
            dgvHistorial.Columns.Add(new DataGridViewTextBoxColumn { Name = "colEstado", HeaderText = "Estado", DataPropertyName = "Estado", FillWeight = 12 });
            dgvHistorial.Columns.Add(new DataGridViewTextBoxColumn { Name = "colItems", HeaderText = "Items", DataPropertyName = "Materiales", FillWeight = 8 });
            dgvHistorial.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDif", HeaderText = "Diferencias", DataPropertyName = "Diferencias", FillWeight = 10 });
            dgvHistorial.Columns.Add(new DataGridViewTextBoxColumn { Name = "colObs", HeaderText = "Observaciones", DataPropertyName = "Observaciones", FillWeight = 28 });
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
            dgvDetalleHistorial.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDMat", HeaderText = "Material", DataPropertyName = "Nombre", FillWeight = 42 });
            dgvDetalleHistorial.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDCat", HeaderText = "Categoria", DataPropertyName = "Categoria", FillWeight = 20 });
            dgvDetalleHistorial.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDCont", HeaderText = "Contado", DataPropertyName = "Cantidad", FillWeight = 12 });
            dgvDetalleHistorial.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDStk", HeaderText = "Stock sistema", DataPropertyName = "Stock", FillWeight = 14 });
            dgvDetalleHistorial.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDDif", HeaderText = "Diferencia", DataPropertyName = "Diferencia", FillWeight = 12 });
            dgvDetalleHistorial.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDEst", HeaderText = "Estado", DataPropertyName = "EstadoDiferencia", FillWeight = 14 });
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

        private async Task CargarHistorial()
        {
            if (dgvHistorial == null || presenter == null) return; int version = ++versionHistorial;
            try {
                DateTime? desde = chkDesde != null && chkDesde.Checked ? (DateTime?)dtpDesde.Value.Date : null;
                DateTime? hasta = chkHasta != null && chkHasta.Checked ? (DateTime?)dtpHasta.Value.Date : null;
                var datos = await presenter.HistorialAsync(desde, hasta, txtBuscarHistorial?.Text?.Trim(), cierre.Token);
                if (!Vigente || _tabActual != "historial" || version != versionHistorial) return;
                _historial = datos.ToList(); dgvHistorial.DataSource = null; dgvHistorial.DataSource = _historial;
                if (_historial.Count > 0) { _conteoSeleccionadoHistorial = _historial[0]; SeleccionarFilaGrid(0); await CargarDetalleHistorial(_historial[0]); }
                else { _conteoSeleccionadoHistorial = null; dgvDetalleHistorial.DataSource = null; lblDetalleHistorial.Text = "Detalle del conteo seleccionado"; }
                ActualizarBotonesHistorial();
            } catch (OperationCanceledException) { } catch (Exception ex) { if (Vigente && version == versionHistorial) ToastNotification.ShowError("Error al cargar historial: " + ex.Message); }
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
            ConteoLectura conteoSel = null;
            int idxSel = -1;
            for (int i = 0; i < dgvHistorial.Rows.Count; i++)
            {
                if (dgvHistorial.Rows[i].DataBoundItem is ConteoLectura c && c.Id == idConteo)
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
                _ = CargarDetalleHistorial(conteoSel);
                ActualizarBotonesHistorial();
            }
        }

        private void DgvHistorial_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvHistorial.CurrentRow != null && dgvHistorial.CurrentRow.DataBoundItem is ConteoLectura conteo)
            {
                _conteoSeleccionadoHistorial = conteo;
                _ = CargarDetalleHistorial(conteo);
            }
            else
            {
                _conteoSeleccionadoHistorial = null;
                dgvDetalleHistorial.DataSource = null;
            }
            ActualizarBotonesHistorial();
        }

        private async Task CargarDetalleHistorial(ConteoLectura conteo)
        {
            if (conteo == null || dgvDetalleHistorial == null) return; int version = ++versionDetalle; int id = conteo.Id;
            try { var detalles = await presenter.DetallesAsync(id, cierre.Token);
                if (!Vigente || _tabActual != "historial" || version != versionDetalle || _conteoSeleccionadoHistorial?.Id != id) return;
                dgvDetalleHistorial.DataSource = detalles.ToList();
                lblDetalleHistorial.Text = $"{(conteo.Estado == "finalizado" ? "Diferencias del" : "Materiales cargados en")} conteo #{id} - {detalles.Count} material(es)";
            } catch (OperationCanceledException) { } catch (Exception ex) { if (Vigente && version == versionDetalle) ToastNotification.ShowError("Error al cargar detalle: " + ex.Message); }
        }

        private void ActualizarBotonesHistorial()
        {
            bool hay = _conteoSeleccionadoHistorial != null;
            string estado = hay ? (_conteoSeleccionadoHistorial.Estado ?? "").ToLower() : "";
            bool esAdmin = SessionManager.Role == UserRole.Admin;
            string propietario = _conteoSeleccionadoHistorial?.UsuarioInicio?.Trim();
            string email = SessionManager.Email?.Trim();
            bool esCreador = hay && !string.IsNullOrWhiteSpace(email) && (string.Equals(propietario, email, StringComparison.OrdinalIgnoreCase)
                || (string.Equals(propietario, "admin", StringComparison.OrdinalIgnoreCase) && string.Equals(email, "admin@tesis.local", StringComparison.OrdinalIgnoreCase))
                || (string.Equals(propietario, "galpon", StringComparison.OrdinalIgnoreCase) && string.Equals(email, "galpon@tesis.local", StringComparison.OrdinalIgnoreCase)));

            if (btnContinuar != null) btnContinuar.Enabled = !ocupada && hay && estado == "abierto" && esCreador;
            if (btnCancelarHistorial != null) btnCancelarHistorial.Enabled = !ocupada && hay && estado == "abierto" && (esAdmin || esCreador);
            if (btnImprimir != null) btnImprimir.Enabled = !ocupada && hay;
            if (btnEliminar != null)
            {
                bool esFinalizadoOCancelado = estado == "finalizado" || estado == "cancelado";
                btnEliminar.Visible = esAdmin;
                btnEliminar.Enabled = !ocupada && esAdmin && hay && esFinalizadoOCancelado;
            }
        }

        private void BtnContinuar_Click(object sender, EventArgs e)
        {
            if (_conteoSeleccionadoHistorial == null) return;
            CambiarTab("entrada", _conteoSeleccionadoHistorial.Id);
        }

        private async void BtnCancelarHistorial_Click(object sender, EventArgs e)
        {
            if (_conteoSeleccionadoHistorial == null || ocupada) return; int id = _conteoSeleccionadoHistorial.Id;
            if (MessageBox.Show($"Cancelar conteo #{id}?", "Confirmar cancelacion", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes || !Comenzar()) return;
            int version = versionHistorial;
            try { await presenter.CancelarAsync(id, cierre.Token); if (Vigente && _tabActual == "historial" && version == versionHistorial && _conteoSeleccionadoHistorial?.Id == id) await CargarHistorial(); }
            catch (OperationCanceledException) { } catch (Exception ex) { if (Vigente) ToastNotification.ShowError("No se pudo cancelar: " + ex.Message); } finally { Terminar(); }
        }

        private async void BtnImprimir_Click(object sender, EventArgs e)
        {
            var conteo = _conteoSeleccionadoHistorial; if (conteo == null || !Comenzar()) return; int id = conteo.Id, version = versionHistorial;
            bool esAdmin = SessionManager.Role == UserRole.Admin;
            try { var detalles = await presenter.DetallesAsync(id, cierre.Token); var secciones = await presenter.SeccionesAsync(cierre.Token);
                if (!Vigente || _tabActual != "historial" || version != versionHistorial || _conteoSeleccionadoHistorial?.Id != id) return;
                string ruta = Path.Combine(Path.GetTempPath(), pdf.SugerirNombreArchivo(conteo));
                await Task.Run(() => { cierre.Token.ThrowIfCancellationRequested(); pdf.Generar(ruta, conteo, detalles, secciones, esAdmin); }, cierre.Token);
                if (!Vigente || _tabActual != "historial" || version != versionHistorial || _conteoSeleccionadoHistorial?.Id != id) return;
                abrirPdf(ruta); ToastNotification.ShowSuccess("PDF generado correctamente.");
            } catch (OperationCanceledException) { } catch (Exception ex) { if (Vigente) ToastNotification.ShowError("No se pudo generar el PDF: " + ex.Message); } finally { Terminar(); }
        }

        private async void BtnEliminarHistorial_Click(object sender, EventArgs e)
        {
            var conteo = _conteoSeleccionadoHistorial; if (conteo == null || ocupada) return;
            if (SessionManager.Role != UserRole.Admin) { ToastNotification.ShowWarning("Solo los administradores pueden eliminar conteos."); return; }
            int id = conteo.Id; if (MessageBox.Show($"Eliminar definitivamente el conteo #{id}?\n\nSe borrarán la cabecera y todos los detalles asociados.", "Confirmar eliminación", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes || !Comenzar()) return;
            int version = versionHistorial;
            try { await presenter.EliminarAsync(id, cierre.Token); if (Vigente && _tabActual == "historial" && version == versionHistorial && _conteoSeleccionadoHistorial?.Id == id) await CargarHistorial(); }
            catch (OperationCanceledException) { } catch (Exception ex) { if (Vigente) ToastNotification.ShowError("No se pudo eliminar: " + ex.Message); } finally { Terminar(); }
        }

        private static string MostrarUsuario(string usuario)
        {
            if (string.Equals(usuario, "admin@tesis.local", StringComparison.OrdinalIgnoreCase)) return "admin";
            if (string.Equals(usuario, "galpon@tesis.local", StringComparison.OrdinalIgnoreCase)) return "galpon";
            return usuario;
        }

        private void DgvHistorial_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || !(dgvHistorial.Rows[e.RowIndex].DataBoundItem is ConteoLectura conteo)) return;
            string col = dgvHistorial.Columns[e.ColumnIndex].Name;
            if (col == "colUsuario") { e.Value = MostrarUsuario(conteo.UsuarioInicio); e.FormattingApplied = true; }
            else if (col == "colFecha")
            {
                e.Value = conteo.Inicio.ToString("dd/MM/yyyy HH:mm");
                e.FormattingApplied = true;
            }
            else if (col == "colEstado")
            {
                string estado = (conteo.Estado ?? "").ToLower();
                if (estado == "abierto") e.CellStyle.ForeColor = Color.FromArgb(255, 200, 100);
                else if (estado == "finalizado") e.CellStyle.ForeColor = Color.FromArgb(115, 210, 130);
                else if (estado == "cancelado") e.CellStyle.ForeColor = Color.FromArgb(230, 110, 100);
                e.CellStyle.SelectionForeColor = e.CellStyle.ForeColor;
            }
        }

        private void DgvDetalleHistorial_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || !(dgvDetalleHistorial.Rows[e.RowIndex].DataBoundItem is DetalleConteoLectura det)) return;
            string col = dgvDetalleHistorial.Columns[e.ColumnIndex].Name;
            if (col == "colDEst")
            {
                string estado = det.EstadoDiferencia;
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
