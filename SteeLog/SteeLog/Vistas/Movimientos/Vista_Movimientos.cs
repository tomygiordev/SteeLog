using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Timer = System.Windows.Forms.Timer;
using SteeLog.Presentation;
using SteeLog.CompositionRoot;
using SteeLog.Core.Application.Movimientos;
using System.Windows.Forms;
using ImpactoStock = SteeLog.Domain.Inventario.ImpactoStock;
using ClasificadorMovimiento = SteeLog.Domain.Inventario.ClasificadorMovimiento;
using SteeLog.Modelos;
using SteeLog.Reportes;
using SteeLog.Servicios;
using System.IO;
// using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace SteeLog.Vistas.Movimientos
{
    public partial class Vista_Movimientos : Form, IMessageFilter, IMovimientosVista, IMovimientosResultadoIncierto
    {
        private List<MovimientoMaterial> materialesLista = new List<MovimientoMaterial>();
        private FlowLayoutPanel flpMateriales;
        private Panel panelSeleccionado = null;
        private bool isProgrammaticChange = false;
        private List<Categoria> _categorias = new List<Categoria>();
        private List<Material> materialesFull = new List<Material>();
        private Recorte _recorteSeleccionado = null;
        private int _idRecorteSeleccionado = 0;
        private decimal _medidaRecorteSeleccionado = 0;
        private decimal _anchoRecorteSeleccionado = 0;
        private decimal _alturaRecorteSeleccionado = 0;
        internal Func<DialogResult> ConfirmarGuardado = () => MessageBox.Show(
            "¿Estás seguro que querés guardar este remito?", "Confirmar guardado",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
        internal Action<string> NotificarGuardado = mensaje => ToastNotification.ShowSuccess(mensaje);
        internal Action<string> AdvertirGuardado = mensaje => ToastNotification.ShowWarning(mensaje);
        internal Func<string> VersionBorrador = () => MovimientoDraftService.VersionActual();
        internal Action<string> EliminarBorradorConfirmado = version => MovimientoDraftService.EliminarSiCoincide(version);
        internal Action<SteeLog.Core.Application.Borradores.MovimientoDraft> GuardarBorradorActual = draft => MovimientoDraftService.Guardar(draft);
        private int _idMovimientoAEditar = 0;
        private bool modeloListo = false;
        private bool catalogosListos;
        private CatalogosMovimiento catalogos = new CatalogosMovimiento();
        private List<Recorte> recortesDisponibles = new List<Recorte>();
        private int versionCatalogos;
        private CancellationTokenSource cargaCatalogos;
        private bool ocupada;
        private bool envioPendiente;
        private bool resultadoIncierto;
        private int versionCatalogosAlMarcarIncierto;
        internal Action<string> AdvertirErrorCarga = mensaje => ToastNotification.ShowError(mensaje);
        internal Action<string> AdvertirResultadoIncierto = mensaje => ToastNotification.ShowWarning(mensaje);
        private int versionCarga;
        private CancellationTokenSource cargaEdicion;
        private readonly CancellationTokenSource cierre = new CancellationTokenSource();
        private MovimientosPresenter presenter;
        private MovimientosPresenter Presenter => presenter ?? (presenter = LocalServices.Current.CrearMovimientosPresenter(this));
        public void SetOcupada(bool valor) { ocupada = valor; if (!IsDisposed && guna2Button2 != null) guna2Button2.Enabled = !valor && modeloListo && catalogosListos && !resultadoIncierto; }
        public void MarcarResultadoIncierto()
        {
            // El movimiento pudo haberse persistido: se bloquean los reenvíos hasta recargar (F5) con éxito.
            // Carrito y borrador se conservan para que el usuario pueda verificar el historial.
            resultadoIncierto = true; versionCatalogosAlMarcarIncierto = versionCatalogos; SetOcupada(ocupada);
            if (!IsDisposed && !cierre.IsCancellationRequested)
                AdvertirResultadoIncierto("Resultado incierto: el movimiento pudo haberse guardado. Verifique el historial antes de reintentar. Pulse F5 para recargar.");
        }
        private bool BloquearPorResultadoIncierto()
        {
            if (!resultadoIncierto) return false;
            if (!IsDisposed && !cierre.IsCancellationRequested)
                AdvertirResultadoIncierto("Hay un resultado incierto pendiente. Verifique el historial y pulse F5 para recargar antes de continuar.");
            return true;
        }
        public void MostrarError(Exception error) { if (!IsDisposed) ToastNotification.ShowError(error.Message); }
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
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
            guna2DateTimePicker1.Value = DateTime.Now;
            
            Application.AddMessageFilter(this);
            FormClosed += (s, e) => CerrarRecursos();
            Disposed += (s, e) => CerrarRecursos();

            SetOcupada(false);
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

            Load += async (s, ev) => {
                await CargarCatalogosAsync();
                if (!IsDisposed && catalogosListos && _idMovimientoAEditar <= 0) IntentarRecuperarBorrador();
                else if (!IsDisposed && catalogosListos && _idMovimientoAEditar > 0) CargarMovimientoParaEdicion();
            };
        }

        private bool recursosCerrados;
        private void CerrarRecursos()
        {
            if (recursosCerrados) return;
            recursosCerrados = true;
            cierre.Cancel(); cargaEdicion?.Cancel(); cargaCatalogos?.Cancel();
            Application.RemoveMessageFilter(this);
            _timerDraft?.Stop(); _timerDraft?.Dispose(); _timerDraft = null;
            _timerDebounce?.Stop(); _timerDebounce?.Dispose(); _timerDebounce = null;
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
                    var puntoMensaje = PuntoPantallaDelMensaje(m);
                    if (!puntoMensaje.HasValue) return false;
                    Point mousePos = puntoMensaje.Value;
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

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ClientToScreen(IntPtr window, ref Point point);

        private static Point? PuntoPantallaDelMensaje(Message mensaje)
        {
            if (mensaje.HWnd == IntPtr.Zero) return null;
            long coordenadas = mensaje.LParam.ToInt64();
            var punto = new Point(unchecked((short)(coordenadas & 0xffff)), unchecked((short)((coordenadas >> 16) & 0xffff)));
            return ClientToScreen(mensaje.HWnd, ref punto) ? punto : (Point?)null;
        }

        private async void RefrescarDatos()
        {
            await CargarCatalogosAsync();
            if (IsDisposed || cierre.IsCancellationRequested || !catalogosListos) return;
            // Solo una recarga completada con éxito y posterior a la marca levanta el bloqueo.
            if (resultadoIncierto && versionCatalogos > versionCatalogosAlMarcarIncierto) { resultadoIncierto = false; SetOcupada(ocupada); }
            if (_idMovimientoAEditar > 0 && !modeloListo) CargarMovimientoParaEdicion();
            NotificarGuardado("Datos actualizados (F5)");
        }

        private async Task CargarCatalogosAsync()
        {
            int version = ++versionCatalogos;
            cargaCatalogos?.Cancel();
            cargaCatalogos = CancellationTokenSource.CreateLinkedTokenSource(cierre.Token);
            var token = cargaCatalogos.Token;
            catalogosListos = false; SetOcupada(ocupada);
            try
            {
                var cat = Presenter.CatalogosAsync(token); var materiales = Presenter.MaterialesAsync(token);
                var categorias = Presenter.CategoriasAsync(token); var recortes = Presenter.RecortesAsync(token);
                await Task.WhenAll(cat, materiales, categorias, recortes);
                if (IsDisposed || token.IsCancellationRequested || version != versionCatalogos) return;
                catalogos = cat.Result;
                materialesFull = materiales.Result.Select(x => new Material { id=x.Id, nombre=x.Nombre, id_categoria=x.CategoriaId, nombre_categoria=x.Categoria, precioUnitario=x.Precio, stockActual=x.Stock, stockMinimo=x.Minimo, cantidadUsada=x.CantidadUsada, cantidadMovimientos=x.CantidadMovimientos }).ToList();
                _categorias = categorias.Result.Select(x => new Categoria { id=x.Id, nombre=x.Nombre, recortable=x.Recortable }).ToList();
                recortesDisponibles = recortes.Result.Select(x => new Recorte { id=x.Id, id_material=x.MaterialId, nombre_material=x.Material, nombre_categoria=x.Categoria, ancho=x.Ancho, altura=x.Altura, medida=x.Medida, cantidad=x.Cantidad, estado=x.Estado, fecha_creacion=x.FechaCreacion }).ToList();
                int? tipoSeleccionado = cmbMovimientos.SelectedValue is int tipoActual ? (int?)tipoActual : null;
                int? obraSeleccionada = cmbObras.SelectedValue is int obraActual ? (int?)obraActual : null;
                int? fleteSeleccionado = cmbFlete.SelectedValue is int fleteActual ? (int?)fleteActual : null;
                var origenSeleccionado = cmbOrigen.SelectedItem as ItemCombo;
                var destinoSeleccionado = cmbDestino.SelectedItem as ItemCombo;
                var tipos = new DataTable(); tipos.Columns.Add("id_tipomovimiento", typeof(int)); tipos.Columns.Add("nombre", typeof(string));
                var disponibles = catalogos.Tipos.Where(x => x.Nombre.IndexOf("ajuste positivo",StringComparison.OrdinalIgnoreCase)<0 && x.Nombre.IndexOf("registro interno",StringComparison.OrdinalIgnoreCase)<0).OrderByDescending(x => x.Nombre.IndexOf("envío",StringComparison.OrdinalIgnoreCase)>=0 || x.Nombre.IndexOf("envio",StringComparison.OrdinalIgnoreCase)>=0);
                foreach(var tipo in disponibles) tipos.Rows.Add(tipo.Id,tipo.Nombre);
                var obras = new DataTable(); obras.Columns.Add("id",typeof(int)); obras.Columns.Add("nombre",typeof(string));
                foreach(var obra in catalogos.Destinos.Where(x => x.Tipo=="OBRA").OrderBy(x=>x.Nombre)) obras.Rows.Add(obra.Id,obra.Nombre);
                bool anteriorProgramatico = isProgrammaticChange; isProgrammaticChange = true;
                try
                {
                    cmbMovimientos.DataSource=tipos; cmbMovimientos.DisplayMember="nombre"; cmbMovimientos.ValueMember="id_tipomovimiento"; cmbMovimientos.SelectedIndex=-1;
                    cmbObras.DataSource=obras; cmbObras.DisplayMember="nombre"; cmbObras.ValueMember="id"; cmbObras.SelectedIndex=-1;
                    EstablecerFletes(fleteSeleccionado); CargarOrigen(); CargarDestino();
                    if (tipoSeleccionado.HasValue) cmbMovimientos.SelectedValue=tipoSeleccionado.Value;
                    if (obraSeleccionada.HasValue) cmbObras.SelectedValue=obraSeleccionada.Value;
                    if (origenSeleccionado != null) cmbOrigen.SelectedItem = cmbOrigen.Items.Cast<ItemCombo>().FirstOrDefault(x => x.IdReal == origenSeleccionado.IdReal && x.Tipo == origenSeleccionado.Tipo);
                    if (destinoSeleccionado != null) cmbDestino.SelectedItem = cmbDestino.Items.Cast<ItemCombo>().FirstOrDefault(x => x.IdReal == destinoSeleccionado.IdReal && x.Tipo == destinoSeleccionado.Tipo);
                }
                finally { isProgrammaticChange=anteriorProgramatico; }
                catalogosListos=true; if (_idMovimientoAEditar<=0) modeloListo=true; SetOcupada(ocupada);
            }
            catch(OperationCanceledException) { }
            catch(Exception ex) { if (!IsDisposed && version==versionCatalogos) AdvertirErrorCarga("Error al cargar catálogos: "+ex.Message); }
        }
        private void EstablecerFletes(int? incluirId)
        {
            var fletes=catalogos.Fletes.Where(x => x.Activo || x.Id==incluirId).Select(x => new Flete { id=x.Id,nombre=x.Nombre,activo=x.Activo }).ToList();
            fletes.Insert(0,new Flete { id=0,nombre="-" });
            cmbFlete.DataSource=fletes; cmbFlete.DisplayMember="nombre"; cmbFlete.ValueMember="id"; cmbFlete.SelectedValue=incluirId ?? 0;
        }

        public void SetEditMode(int idMovimiento)
        {
            LimpiarFormulario();
            _idMovimientoAEditar = idMovimiento;
            modeloListo = false;
            SetOcupada(ocupada);
            ActualizarModoEdicion();
            // Diferir la carga hasta que el handle del formulario exista y tenga tamaño real
            if (this.IsHandleCreated && catalogosListos) CargarMovimientoParaEdicion();
        }

        private async void CargarMovimientoParaEdicion()
        {
            if (_idMovimientoAEditar <= 0) return;
            int id = _idMovimientoAEditar, version = ++versionCarga;
            cargaEdicion?.Cancel();
            cargaEdicion = CancellationTokenSource.CreateLinkedTokenSource(cierre.Token);
            modeloListo = false; SetOcupada(ocupada);
            try
            {
                var edicion = await Presenter.ObtenerEdicionAsync(id, cargaEdicion.Token);
                if (IsDisposed || cierre.IsCancellationRequested || version != versionCarga || _idMovimientoAEditar != id) return;
                var c = edicion?.Cabecera;
                var mov = c == null ? null : new Movimiento { fecha=c.Fecha, nroremito=c.Remito, id_origen=c.OrigenId, tipo_origen=c.TipoOrigen, id_destino=c.DestinoId, tipo_destino=c.TipoDestino, id_tipomovimiento=c.TipoMovimientoId, id_obra=c.ObraId ?? 0, costoflete=c.CostoFlete, id_flete=c.FleteId, observaciones=c.Observaciones, precio_total=c.PrecioTotal };
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
                    EstablecerFletes(mov.id_flete.Value);
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
                foreach (var item in edicion.Lineas)
                {
                    var l = item.Linea;
                    var d = item.InsumoId.HasValue
                        ? new MovimientoMaterial { EsInsumo=true, IdInsumo=item.InsumoId, NombreMaterial=item.Nombre, cantidad=1 }
                        : new MovimientoMaterial { id_material=l.MaterialId, IdRecorte=l.RecorteId, EsRecorte=l.RecorteId.HasValue, MedidaRecorte=l.MedidaRecorte, cantidad=l.Cantidad, NombreMaterial=item.Nombre, NombreMaterialEventual=l.MaterialEventual, EsCompraEventual=l.CompraEventual, precio_unitario=l.PrecioUnitario };
                    materialesLista.Add(d);
                    AgregarItemLista(d);
                }
                modeloListo = true; SetOcupada(ocupada);
                ActualizarModoEdicion();
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                if (!IsDisposed && version == versionCarga && _idMovimientoAEditar == id) ToastNotification.ShowError("Error al cargar para edición: " + ex.Message);
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

        private List<ItemCombo> OpcionesDestino() => catalogos.Destinos.Select(x => new ItemCombo { TextoDisplay=x.Nombre, IdReal=x.Id, Tipo=x.Tipo }).ToList();
        public void CargarOrigen()
        {
            cmbOrigen.DataSource=OpcionesDestino(); cmbOrigen.DisplayMember="TextoDisplay"; cmbOrigen.ValueMember="IdReal"; cmbOrigen.SelectedIndex=-1;
        }
        public void CargarDestino()
        {
            cmbDestino.DataSource=OpcionesDestino(); cmbDestino.DisplayMember="TextoDisplay"; cmbDestino.ValueMember="IdReal"; cmbDestino.SelectedIndex=-1;
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
        private async void GuardarMovimiento_Click(object sender, EventArgs e)
        {
            if (!modeloListo || !catalogosListos || ocupada || envioPendiente || resultadoIncierto) return;
            if (cmbMovimientos.SelectedIndex == -1 || cmbOrigen.SelectedIndex == -1 || cmbDestino.SelectedIndex == -1 || materialesLista.Count == 0)
            {
                AdvertirGuardado("Completa todos los campos obligatorios.");
                return;
            }

            var confirmGuardar = ConfirmarGuardado();
            if (confirmGuardar != DialogResult.Yes) return;

            // Validación de materiales eventuales antes de procesar
            string tipoMovStr = cmbMovimientos.Text.ToUpper();
            if (materialesLista.Any(m => m.EsCompraEventual && string.IsNullOrEmpty(m.NombreMaterialEventual)))
            {
                ToastNotification.ShowError("No se puede guardar: Hay materiales de compra eventual sin nombre.");
                return;
            }

            bool confirmado = false;
            try
            {
                // Separate materials from insumos
                var materialesSolo = materialesLista.Where(m => !m.EsInsumo).ToList();
                var insumosSolo = materialesLista.Where(m => m.EsInsumo).ToList();

                // Determine destino name for insumo movements
                string destinoNombre = "";
                var itemDestino = (ItemCombo)cmbDestino.SelectedItem;
                if (itemDestino.Tipo == "OBRA")
                {
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

                var solicitud = new MovimientoSolicitud {
                    Cabecera = new CabeceraMovimiento { Fecha=movimiento.fecha, Remito=movimiento.nroremito, OrigenId=movimiento.id_origen, TipoOrigen=movimiento.tipo_origen, DestinoId=movimiento.id_destino, TipoDestino=movimiento.tipo_destino, TipoMovimientoId=movimiento.id_tipomovimiento, ObraId=movimiento.id_obra > 0 ? (int?)movimiento.id_obra : null, CostoFlete=movimiento.costoflete, FleteId=movimiento.id_flete, Observaciones=movimiento.observaciones, PrecioTotal=movimiento.precio_total },
                    Lineas = materialesSolo.Select(m => new LineaMovimiento { MaterialId=m.id_material, EsRecorte=m.EsRecorte, RecorteId=m.IdRecorte, AnchoRecorte=m.AnchoRecorte, AlturaRecorte=m.AlturaRecorte, MedidaRecorte=m.MedidaRecorte, Cantidad=m.cantidad, MaterialEventual=m.NombreMaterialEventual, CompraEventual=m.EsCompraEventual, PrecioUnitario=m.precio_unitario }).ToList(),
                    Insumos = insumosSolo.Select(m => m.IdInsumo ?? 0).ToList(), OrigenNombre=origenNombre, DestinoNombre=destinoNombre
                };
                int versionOperacion = versionCarga, idOperacion = _idMovimientoAEditar;
                string versionBorrador = idOperacion <= 0 ? VersionBorrador() : null;
                int? guardado;
                envioPendiente = true;
                try { guardado = await Presenter.GuardarAsync(idOperacion > 0 ? (int?)idOperacion : null, solicitud, cierre.Token); }
                finally { envioPendiente = false; }
                if (!guardado.HasValue) return;
                confirmado = true;
                bool errorAlRetirarBorrador = false;
                try { EliminarBorradorConfirmado(versionBorrador); }
                catch (Exception ex) { errorAlRetirarBorrador = true; System.Diagnostics.Debug.WriteLine("No se pudo retirar el borrador confirmado: " + ex.Message); }
                if (IsDisposed || cierre.IsCancellationRequested || versionOperacion != versionCarga || idOperacion != _idMovimientoAEditar) return;
                int idMovimientoFinal = guardado.Value;

                bool eraEdicion = _idMovimientoAEditar > 0;
                // La operación ya está confirmada: retirar el carrito antes de cualquier
                // espera posterior, para que otro clic no vuelva a registrar lo mismo.
                LimpiarFormulario();
                int versionConfirmada = versionCarga;
                NotificarGuardado(eraEdicion ? "Movimiento actualizado." : "Movimiento guardado.");
                if (errorAlRetirarBorrador)
                    AdvertirGuardado("El movimiento se guardó, pero no se pudo retirar su borrador. No lo recupere ni lo vuelva a guardar.");
                (this.TopLevelControl as VistaGeneral)?.RefrescarHistorialSiAbierto();

                // Consultar el saldo confirmado antes de emitir alertas.
                await RefrescarInventarioAsync(cierre.Token);
                if (IsDisposed || cierre.IsCancellationRequested || versionConfirmada != versionCarga) return;
                VerificarStockMinimoPorMateriales(materialesSolo);

                string archivoPdf = await Presenter.GenerarRemitoAsync(idMovimientoFinal, cierre.Token);
                if (archivoPdf != null && !IsDisposed && !cierre.IsCancellationRequested && versionConfirmada == versionCarga)
                {
                    try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = archivoPdf, UseShellExecute = true }); }
                    catch (Exception exPdf) { ToastNotification.ShowError("Error al abrir PDF: " + exPdf.Message); }
                }
            }
            catch (Exception ex)
            {
                ToastNotification.ShowError((confirmado ? "Movimiento guardado, pero falló una acción posterior: " : "Error al guardar: ") + ex.Message);
            }
        }

        private void LimpiarFormulario()
        {
            versionCarga++; cargaEdicion?.Cancel(); modeloListo = true; SetOcupada(ocupada);
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
            _idRecorteSeleccionado = 0;
            _medidaRecorteSeleccionado = 0;
            _anchoRecorteSeleccionado = 0;
            _alturaRecorteSeleccionado = 0;

            btnRecorte.Enabled = false;

            materialesLista.Clear();
            flpMateriales.Controls.Clear();
            panelSeleccionado = null;
            _idMovimientoAEditar = 0;
            ActualizarModoEdicion();


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
                decimal limitado = LimitarCantidadAStock(valor);
                if (limitado != valor)
                {
                    isProgrammaticChange = true;
                    cantMatMov.Text = limitado.ToString("G29", CultureInfo.InvariantCulture);
                    cantMatMov.SelectionStart = cantMatMov.Text.Length;
                    isProgrammaticChange = false;
                }
            }
        }

        // Aviso de stock máximo: inyectable en tests; no se repite para el mismo tope hasta que la cantidad vuelva a ser válida.
        private Action<string> AvisarStockMaximo = mensaje => ToastNotification.ShowWarning(mensaje);
        private decimal? _ultimoAvisoStockMax;

        /// <summary>
        /// Aplica el tope de stock disponible (solo movimientos que consumen stock) y avisa una vez por tope.
        /// </summary>
        private decimal LimitarCantidadAStock(decimal valor)
        {
            if (_materialSeleccionado == null) return valor;

            // En modo edición, el stockMax considera las cantidades ya cargadas (serán revertidas)
            decimal stockMax = _recorteSeleccionado != null
                ? StockDisponibleRecorte(_recorteSeleccionado.id)
                : StockDisponibleMaterial(_materialSeleccionado.id);

            string tipoDestino = (cmbDestino.SelectedItem as ItemCombo)?.Tipo;
            bool consumeStock = ClasificadorMovimiento.Clasificar(cmbMovimientos.Text, tipoDestino) == ImpactoStock.Salida;

            if (consumeStock && valor > stockMax)
            {
                if (_ultimoAvisoStockMax != stockMax)
                {
                    _ultimoAvisoStockMax = stockMax;
                    AvisarStockMaximo($"Stock máximo disponible: {stockMax.ToString("G29")}");
                }
                return stockMax;
            }
            _ultimoAvisoStockMax = null;
            return valor;
        }

        private void cantMatMov_MouseWheel(object sender, MouseEventArgs e)
        {
            // Evitar que el scroll del padre se active
            if (e is HandledMouseEventArgs hme)
                hme.Handled = true;

            string cantTexto = (cantMatMov.Text ?? "").Replace(',', '.');
            decimal.TryParse(cantTexto, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal valor);

            valor = (e.Delta > 0) ? valor + 1 : valor - 1;
            
            if (valor < 0) valor = 0;
            valor = LimitarCantidadAStock(valor);

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

        private async Task RefrescarInventarioAsync(CancellationToken token)
        {
            try
            {
                var materiales = await Presenter.MaterialesAsync(token);
                if (IsDisposed || token.IsCancellationRequested) return;
                materialesFull = materiales.Select(x => new Material { id=x.Id,nombre=x.Nombre,id_categoria=x.CategoriaId,nombre_categoria=x.Categoria,precioUnitario=x.Precio,stockActual=x.Stock,stockMinimo=x.Minimo }).ToList();
                var recortes = await Presenter.RecortesAsync(token);
                if (IsDisposed || token.IsCancellationRequested) return;
                recortesDisponibles = recortes.Select(x => new Recorte { id=x.Id,id_material=x.MaterialId,nombre_material=x.Material,nombre_categoria=x.Categoria,ancho=x.Ancho,altura=x.Altura,medida=x.Medida,cantidad=x.Cantidad,estado=x.Estado,fecha_creacion=x.FechaCreacion }).ToList();
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { if (!IsDisposed) ToastNotification.ShowError("Movimiento guardado, pero no se pudo actualizar el stock: " + ex.Message); }
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

                var todosLosMateriales = materialesFull;

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
        private async void btnAgregarFlete_Click(object sender, EventArgs e)
        {
            if (BloquearPorResultadoIncierto()) return;
            using (var modal = new ModalAgregarFlete())
            {
                if (modal.ShowDialog() == DialogResult.OK)
                {
                    int nuevoId = modal.IdFleteCreado;
                    
                    // Recargar fletes e incluir el nuevo ID
                    catalogos = await Presenter.CatalogosAsync(cierre.Token);
                    if (!IsDisposed && !cierre.IsCancellationRequested) EstablecerFletes(nuevoId);
                }
            }
        }

        private async void btnEliminarFlete_Click(object sender, EventArgs e)
        {
            if (BloquearPorResultadoIncierto()) return;
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
                    var desactivado = await Presenter.DesactivarFleteAsync(idSeleccionado,cierre.Token);
                    if (!desactivado.HasValue || IsDisposed || cierre.IsCancellationRequested) return;
                    catalogos = await Presenter.CatalogosAsync(cierre.Token);
                    if (IsDisposed || cierre.IsCancellationRequested) return;
                    ToastNotification.ShowSuccess("Flete desactivado (borrado lógico) correctamente.");
                    EstablecerFletes(null);
                }
                catch (Exception ex)
                {
                    ToastNotification.ShowError("Error al desactivar el flete: " + ex.Message);
                }
            }
        }
    }
}
