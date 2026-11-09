using System;
using SteeLog.UI;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SteeLog.Presentation;
using SteeLog.CompositionRoot;
using System.Threading;
using SteeLog.Modelos;

namespace SteeLog.Vistas
{
    public partial class Vista_Herramientas : Form, IHerramientasVista
    {
        private int idSeleccionado = -1;
        private readonly CancellationTokenSource cierre = new CancellationTokenSource();
        private readonly CargasHerramientas cargas = new CargasHerramientas();
        private HerramientasPresenter presenter;
        private HerramientasPresenter Presenter => presenter ?? (presenter = LocalServices.Current.CrearHerramientasPresenter(this));
        private List<Herramienta> herramientas = new List<Herramienta>();
        private List<Obra> obras = new List<Obra>();
        private List<HerramientaMovimiento> historial = new List<HerramientaMovimiento>();
        private bool ocupada, leyendo, inicializada, obrasCargadas;
        private Func<string, string, MessageBoxIcon, bool> confirmar = (mensaje, titulo, icono) => GestionUi.Confirmar(mensaje, titulo, icono);
        private Action<string> avisar = mensaje => ToastNotification.ShowError(mensaje);
        private int contexto;
        public void SetOcupada(bool valor) { ocupada=valor; ActualizarControles(); }
        public void MostrarError(Exception error) { if (!IsDisposed && !cierre.IsCancellationRequested) avisar(error.Message); }
        private bool Vigente(int revision) => !IsDisposed && !cierre.IsCancellationRequested && cargas.EsActual(revision);
        private void ActualizarControles()
        {
            if (IsDisposed || cierre.IsCancellationRequested) return;
            bool listo = inicializada && !leyendo && !ocupada;
            btnGuardar.Enabled = listo;
            btnEliminar.Enabled = listo && idSeleccionado != -1;
            btnVerHerramientas.Enabled = obrasCargadas && !leyendo && !ocupada;
            btnVerHistorial.Enabled = obrasCargadas && !leyendo && !ocupada;
            txtNombre.Enabled = listo;
            txtObservaciones.Enabled = listo;
            cboEstado.Enabled = listo;
            cboObra.Enabled = listo;
            dgvHerramientas.Enabled = listo;
        }
        private void CerrarCargas() { cargas.Cerrar(); if(!cierre.IsCancellationRequested) cierre.Cancel(); }
        private PrintDocument printDocument1;
        // Los combos del historial se rellenan con DataSource: durante el llenado los SelectedIndexChanged no deben recargar la grilla
        // (se dispara un repintado con el combo a medio enlazar y WinForms lo marca con la X roja). Se recarga una vez al terminar.
        private bool poblandoFiltros;
        private bool vistaHistorialActiva = false;

        public Vista_Herramientas()
        {
            InitializeComponent();
            GestionUi.Abm(this, panelTitulo, lblTitulo, new[] { panelEntrada, panelEntrada2 }, btnGuardar, btnEliminar, btnPDF);
            GestionUi.Tabs(panelTabs, btnVerHerramientas, btnVerHistorial);
            GestionUi.Tab(btnVerHerramientas, true);
            GestionUi.Tab(btnVerHistorial, false);
            GestionUi.Filtros(panelFiltro);
            panelGrid.Dock = DockStyle.Fill; panelGridHistorial.Dock = DockStyle.Fill;
            btnEliminar.Enabled = false;
            btnGuardar.Enabled = false;
            btnVerHerramientas.Enabled = false;
            btnVerHistorial.Enabled = false;
            txtNombre.Enabled = false; txtObservaciones.Enabled = false; cboEstado.Enabled = false; cboObra.Enabled = false; dgvHerramientas.Enabled = false;
            txtNombre.TextChanged += (s, e) => contexto++;
            txtObservaciones.TextChanged += (s, e) => contexto++;
            cboEstado.SelectedIndexChanged += (s, e) => contexto++;
            cboObra.SelectedIndexChanged += (s, e) => contexto++;
            FormClosed += (s, e) => CerrarCargas();
            Disposed += (s, e) => CerrarCargas();

            printDocument1 = new PrintDocument();
            printDocument1.PrintPage += new PrintPageEventHandler(printDocument1_PrintPage);
        }

        internal Vista_Herramientas(IUseCaseExecutor executor) : this()
        {
            presenter = new HerramientasPresenter(executor ?? throw new ArgumentNullException(nameof(executor)), this);
        }

        private async void Vista_Herramientas_Load(object sender, EventArgs e)
        {
            leyendo = true; ActualizarControles();
            try
            {
                CargarComboEstado();
                var datosObras = await Presenter.ObrasAsync(cierre.Token);
                if (IsDisposed || cierre.IsCancellationRequested) return;
                obras = datosObras.Select(x => new Obra { id = x.Id, nombre = x.Nombre }).ToList();
                CargarComboObras();
                obrasCargadas = true;
                await CargarHerramientas();
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { if (!IsDisposed && !cierre.IsCancellationRequested) MostrarError(ex); }
            finally { if (!obrasCargadas && !IsDisposed && !cierre.IsCancellationRequested) { leyendo = false; ActualizarControles(); } }
        }

        // ===================== COMBOS =====================

        private void CargarComboEstado()
        {
            cboEstado.Items.Clear();
            cboEstado.Items.Add("Óptima");
            cboEstado.Items.Add("Regular");
            cboEstado.Items.Add("Mal estado");
            cboEstado.SelectedIndex = 0;
        }

        private void CargarComboObras()
        {
            cboObra.BeginUpdate(); // sin repintado mientras el combo queda a medio enlazar
            try
            {
                cboObra.Items.Clear();
                cboObra.DataSource = null;


                var listaObras = new List<Obra>();
                listaObras.Add(new Obra { id = 0, nombre = "Galpón" });
                listaObras.AddRange(obras);

                cboObra.DataSource = listaObras;
                cboObra.DisplayMember = "nombre";
                cboObra.ValueMember = "id";
                cboObra.SelectedIndex = 0;
            }
            finally { cboObra.EndUpdate(); }
        }

        private void CargarComboFiltroHerramienta()
        {
            poblandoFiltros = true;
            try { CargarComboFiltroHerramientaCore(); }
            finally { poblandoFiltros = false; }
        }

        private void CargarComboFiltroHerramientaCore()
        {
            cboFiltroHerramienta.BeginUpdate(); // sin repintado mientras el combo queda a medio enlazar
            try
            {
                cboFiltroHerramienta.DataSource = null;


                var lista = new List<Herramienta>();
                lista.Add(new Herramienta { id = 0, nombre = "— Todas —" });
                foreach (var h in herramientas)
                    lista.Add(h);

                cboFiltroHerramienta.DataSource = lista;
                cboFiltroHerramienta.DisplayMember = "nombre";
                cboFiltroHerramienta.ValueMember = "id";
                cboFiltroHerramienta.SelectedIndex = 0;
            }
            finally { cboFiltroHerramienta.EndUpdate(); }
        }

        private void CargarFiltrosHistorial()
        {
            poblandoFiltros = true;
            try { CargarFiltrosHistorialCore(); }
            finally { poblandoFiltros = false; }
        }

        private void CargarFiltrosHistorialCore()
        {
            // Cargar estados para el filtro
            cboFiltroEstado.Items.Clear();
            cboFiltroEstado.Items.Add("Todos");
            cboFiltroEstado.Items.Add("Óptima");
            cboFiltroEstado.Items.Add("Regular");
            cboFiltroEstado.Items.Add("Mal estado");
            cboFiltroEstado.SelectedIndex = 0;

            // Cargar obras para el filtro
            cboFiltroObra.BeginUpdate(); // sin repintado mientras el combo queda a medio enlazar
            try
            {
                cboFiltroObra.DataSource = null;

                var listaObras = new List<Obra>();
                listaObras.Add(new Obra { id = 0, nombre = "Todas" });
                listaObras.Add(new Obra { id = -1, nombre = "Galpón" });
                listaObras.AddRange(obras);
            
                cboFiltroObra.DataSource = listaObras;
                cboFiltroObra.DisplayMember = "nombre";
                cboFiltroObra.ValueMember = "id";
                cboFiltroObra.SelectedIndex = 0;
            }
            finally { cboFiltroObra.EndUpdate(); }
        }

        // ===================== GRILLA HERRAMIENTAS =====================

        private async Task CargarHerramientas()
        {
            int revision=cargas.Iniciar(); leyendo=true; inicializada=false; ActualizarControles();
            try
            {
                var datos=await Presenter.HerramientasAsync(cierre.Token);
                if(!Vigente(revision)) return;
                herramientas=datos.Select(x=>new Herramienta { id=x.Id,nombre=x.Nombre,estado=x.Estado,id_obra=x.ObraId,nombre_obra=x.Obra,observaciones=x.Observaciones }).ToList();
                inicializada=true;
                var lista = herramientas;
                
                // Ordenar por nombre que contiene ID
                lista = lista.OrderBy(h => {
                    // Extraer número del nombre si existe
                    string nombre = h.nombre ?? "";
                    // Buscar dígitos al inicio del nombre
                    var match = System.Text.RegularExpressions.Regex.Match(nombre, @"^(\d+)");
                    if (match.Success)
                    {
                        if (int.TryParse(match.Value, out int num))
                        {
                            return num;
                        }
                    }
                    // Si no tiene número al inicio, poner al final
                    return int.MaxValue;
                }).ThenBy(h => h.nombre).ToList();

                dgvHerramientas.DataSource = lista;

                dgvHerramientas.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                dgvHerramientas.RowHeadersVisible = false;
                dgvHerramientas.AllowUserToResizeRows = false;
                dgvHerramientas.MultiSelect = false;
                dgvHerramientas.ReadOnly = true;
                dgvHerramientas.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
                dgvHerramientas.AllowUserToResizeColumns = false;
                dgvHerramientas.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;

                if (dgvHerramientas.Columns["id"] != null)
                    dgvHerramientas.Columns["id"].Visible = false;
                if (dgvHerramientas.Columns["id_obra"] != null)
                    dgvHerramientas.Columns["id_obra"].Visible = false;

                if (dgvHerramientas.Columns["nombre"] != null)
                {
                    dgvHerramientas.Columns["nombre"].HeaderText = "Nombre";
                    dgvHerramientas.Columns["nombre"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    dgvHerramientas.Columns["nombre"].FillWeight = 30;
                }
                if (dgvHerramientas.Columns["estado"] != null)
                {
                    dgvHerramientas.Columns["estado"].HeaderText = "Estado";
                    dgvHerramientas.Columns["estado"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    dgvHerramientas.Columns["estado"].FillWeight = 20;
                }
                if (dgvHerramientas.Columns["nombre_obra"] != null)
                {
                    dgvHerramientas.Columns["nombre_obra"].HeaderText = "Ubicación (Obra)";
                    dgvHerramientas.Columns["nombre_obra"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    dgvHerramientas.Columns["nombre_obra"].FillWeight = 25;
                }
                if (dgvHerramientas.Columns["observaciones"] != null)
                {
                    dgvHerramientas.Columns["observaciones"].HeaderText = "Observaciones";
                    dgvHerramientas.Columns["observaciones"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    dgvHerramientas.Columns["observaciones"].FillWeight = 25;
                }
                GestionUi.AjustarColumnas(dgvHerramientas);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { if(Vigente(revision)) MostrarError(ex); }
            finally { if(Vigente(revision)) { leyendo=false; ActualizarControles(); } }
        }

        // ===================== GRILLA HISTORIAL =====================

        private void CargarHistorial()
        {
            try
            {
                var lista = historial.ToList();

                // Filtro por herramienta específica (comparación por id, no por nombre)
                if (cboFiltroHerramienta.SelectedIndex > 0 && cboFiltroHerramienta.SelectedItem is Herramienta seleccionada && seleccionada.id > 0)
                {
                    lista = lista.Where(m => m.id_herramienta == seleccionada.id).ToList();
                }

                // Filtro por estado (ahora sí: usa el campo estado devuelto por GetHistorial)
                if (cboFiltroEstado.SelectedIndex > 0)
                {
                    string estadoSeleccionado = cboFiltroEstado.SelectedItem.ToString();
                    lista = lista.Where(m => string.Equals(m.estado, estadoSeleccionado, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                // Filtro por obra (incluye origen o destino)
                if (cboFiltroObra.SelectedIndex > 0 && cboFiltroObra.SelectedItem is Obra obraSeleccionada)
                {
                    string nombreObra = obraSeleccionada.id == -1 ? "Galpón" : obraSeleccionada.nombre;
                    lista = lista.Where(m =>
                        string.Equals(m.obra_origen, nombreObra, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(m.obra_destino, nombreObra, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                // Filtro por texto de búsqueda (insensible a mayúsculas)
                string textoBusqueda = txtBuscar.Text.Trim();
                if (!string.IsNullOrEmpty(textoBusqueda))
                {
                    lista = lista.Where(m =>
                        (m.nombre_herramienta ?? "").IndexOf(textoBusqueda, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        (m.obra_origen ?? "").IndexOf(textoBusqueda, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        (m.obra_destino ?? "").IndexOf(textoBusqueda, StringComparison.OrdinalIgnoreCase) >= 0
                    ).ToList();
                }

                dgvHistorial.DataSource = lista;

                dgvHistorial.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                dgvHistorial.RowHeadersVisible = false;
                dgvHistorial.AllowUserToResizeRows = false;
                dgvHistorial.MultiSelect = false;
                dgvHistorial.ReadOnly = true;
                dgvHistorial.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
                dgvHistorial.AllowUserToResizeColumns = false;
                dgvHistorial.ScrollBars = ScrollBars.Vertical;

                if (dgvHistorial.Columns["id"] != null)
                    dgvHistorial.Columns["id"].Visible = false;
                if (dgvHistorial.Columns["id_herramienta"] != null)
                    dgvHistorial.Columns["id_herramienta"].Visible = false;

                dgvHistorial.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

                if (dgvHistorial.Columns["nombre_herramienta"] != null)
                {
                    dgvHistorial.Columns["nombre_herramienta"].HeaderText = "Herramienta";
                    dgvHistorial.Columns["nombre_herramienta"].FillWeight = 28;
                    dgvHistorial.Columns["nombre_herramienta"].DisplayIndex = 0;
                }
                if (dgvHistorial.Columns["estado"] != null)
                {
                    // El esquema no guarda el estado de cada movimiento: se muestra el estado vigente, rotulado como tal.
                    dgvHistorial.Columns["estado"].HeaderText = "Estado actual";
                    dgvHistorial.Columns["estado"].ToolTipText = "Estado vigente de la herramienta; el historial no registra el estado de cada movimiento.";
                    dgvHistorial.Columns["estado"].FillWeight = 14;
                    dgvHistorial.Columns["estado"].DisplayIndex = 1;
                }
                if (dgvHistorial.Columns["obra_origen"] != null)
                {
                    dgvHistorial.Columns["obra_origen"].HeaderText = "Origen";
                    dgvHistorial.Columns["obra_origen"].FillWeight = 20;
                    dgvHistorial.Columns["obra_origen"].DisplayIndex = 2;
                }
                if (dgvHistorial.Columns["obra_destino"] != null)
                {
                    dgvHistorial.Columns["obra_destino"].HeaderText = "Destino";
                    dgvHistorial.Columns["obra_destino"].FillWeight = 20;
                    dgvHistorial.Columns["obra_destino"].DisplayIndex = 3;
                }
                if (dgvHistorial.Columns["fecha"] != null)
                {
                    dgvHistorial.Columns["fecha"].HeaderText = "Fecha";
                    dgvHistorial.Columns["fecha"].FillWeight = 18;
                    dgvHistorial.Columns["fecha"].DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
                    dgvHistorial.Columns["fecha"].DisplayIndex = 4;
                }
                GestionUi.AjustarColumnas(dgvHistorial);
            }
            catch (Exception ex)
            {
                ToastNotification.ShowError("Error al cargar historial: " + ex.Message);
            }
        }

        // ===================== FILTRO HISTORIAL =====================

        private void cboFiltroHerramienta_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (vistaHistorialActiva && !poblandoFiltros)
                CargarHistorial();
        }

        private void txtBuscar_TextChanged(object sender, EventArgs e)
        {
            if (vistaHistorialActiva)
                CargarHistorial();
        }

        private void cboFiltroEstado_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (vistaHistorialActiva && !poblandoFiltros)
                CargarHistorial();
        }

        private void cboFiltroObra_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (vistaHistorialActiva && !poblandoFiltros)
                CargarHistorial();
        }

        // ===================== TABS =====================

        private async void btnVerHerramientas_Click(object sender, EventArgs e)
        {
            if (!obrasCargadas || leyendo || ocupada) return;
            vistaHistorialActiva = false;
            panelGrid.Visible = true;
            panelGridHistorial.Visible = false;
            panelFiltro.Visible = false;
            GestionUi.Tab(btnVerHerramientas, true);
            GestionUi.Tab(btnVerHistorial, false);
            await CargarHerramientas();
        }

        private async void btnVerHistorial_Click(object sender, EventArgs e)
        {
            if (!obrasCargadas || leyendo || ocupada) return;
            vistaHistorialActiva = true;
            panelGrid.Visible = false;
            panelGridHistorial.Visible = true;
            panelFiltro.Visible = true;
            GestionUi.Tab(btnVerHerramientas, false);
            GestionUi.Tab(btnVerHistorial, true);
            int revision=cargas.Iniciar(); leyendo=true; ActualizarControles();
            try
            {
                var datos=await Presenter.HistorialAsync(cierre.Token);
                if(!Vigente(revision) || !vistaHistorialActiva) return;
                historial=datos.Select(x=>new HerramientaMovimiento { id=x.Id,id_herramienta=x.HerramientaId,nombre_herramienta=x.Nombre,estado=x.Estado,obra_origen=x.Origen,obra_destino=x.Destino,fecha=x.Fecha }).ToList();
                CargarComboFiltroHerramienta(); CargarFiltrosHistorial(); CargarHistorial();
            }
            catch(OperationCanceledException) { }
            catch(Exception ex) { if(Vigente(revision)) MostrarError(ex); }
            finally { if(Vigente(revision)) { leyendo=false; ActualizarControles(); } }
        }

        // ===================== SELECTION =====================

        private void LimpiarSeleccion()
        {
            contexto++;
            idSeleccionado = -1;
            txtNombre.Clear();
            txtObservaciones.Clear();
            CargarComboEstado();
            cboObra.SelectedIndex = 0;
            btnGuardar.Text = "Guardar";
            btnEliminar.Enabled = false;
            dgvHerramientas.ClearSelection();
        }

        private void dgvHerramientas_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (!inicializada || leyendo || ocupada) return;
            if (e.RowIndex < 0) return;

            var fila = dgvHerramientas.Rows[e.RowIndex];
            if (fila.Cells["id"].Value == null) return;

            int clicID = Convert.ToInt32(fila.Cells["id"].Value);
            contexto++;

            if (idSeleccionado == clicID)
            {
                LimpiarSeleccion();
            }
            else
            {
                idSeleccionado = clicID;
                txtNombre.Text = fila.Cells["nombre"].Value.ToString();
                txtObservaciones.Text = fila.Cells["observaciones"].Value?.ToString() ?? "";

                CargarComboEstado();
                string estado = fila.Cells["estado"].Value?.ToString() ?? "";
                if (!string.IsNullOrEmpty(estado))
                {
                    if (!cboEstado.Items.Contains(estado)) cboEstado.Items.Add(estado);
                    cboEstado.SelectedItem = estado;
                }

                var idObraVal = fila.Cells["id_obra"].Value;
                if (idObraVal == null || idObraVal == DBNull.Value)
                {
                    cboObra.SelectedIndex = 0;
                }
                else
                {
                    int idObra = Convert.ToInt32(idObraVal);
                    for (int i = 0; i < cboObra.Items.Count; i++)
                    {
                        var obra = (Obra)cboObra.Items[i];
                        if (obra.id == idObra)
                        {
                            cboObra.SelectedIndex = i;
                            break;
                        }
                    }
                }

                btnGuardar.Text = "Actualizar";
                ActualizarControles();
            }
        }

        // ===================== CRUD =====================

        private async void btnGuardar_Click(object sender, EventArgs e)
        {
            if(!inicializada || leyendo || ocupada) return;
            string nombre = txtNombre.Text.Trim();
            if (string.IsNullOrEmpty(nombre))
            {
                ToastNotification.ShowWarning("Debes escribir un nombre para la herramienta.");
                return;
            }

            string estado = cboEstado.SelectedItem?.ToString() ?? "";
            var obraSeleccionada = cboObra.SelectedItem as Obra;
            int? idObra = (obraSeleccionada == null || obraSeleccionada.id == 0) ? (int?)null : obraSeleccionada.id;
            string observaciones = txtObservaciones.Text.Trim();

            string _msg = idSeleccionado == -1 ? "¿Crear esta herramienta?" : "¿Actualizar esta herramienta?";
            if (!confirmar(_msg, "Confirmar", MessageBoxIcon.Question)) return;

            int revisionContexto=contexto;
            int? id=idSeleccionado == -1 ? (int?)null : idSeleccionado;
            var resultado=await Presenter.GuardarAsync(id,nombre,estado,idObra,observaciones,cierre.Token,()=>!IsDisposed && !cierre.IsCancellationRequested && revisionContexto==contexto);
            if(resultado.HasValue && !IsDisposed && !cierre.IsCancellationRequested && revisionContexto==contexto)
            { LimpiarSeleccion(); await CargarHerramientas(); }
        }

        private async void btnEliminar_Click(object sender, EventArgs e)
        {
            if(idSeleccionado==-1 || leyendo || ocupada) return;
            if(!confirmar("¿Estás seguro de eliminar esta herramienta?","Confirmar",MessageBoxIcon.Warning)) return;
            int id=idSeleccionado, revisionContexto=contexto;
            var resultado=await Presenter.EliminarAsync(id,cierre.Token,()=>!IsDisposed && !cierre.IsCancellationRequested && revisionContexto==contexto);
            if(resultado.HasValue && !IsDisposed && !cierre.IsCancellationRequested && revisionContexto==contexto)
            { LimpiarSeleccion(); await CargarHerramientas(); }
        }

        // ===================== PDF =====================

        private void btnPDF_Click(object sender, EventArgs e)
        {
            try
            {
                string printerName = "Microsoft Print to PDF";
                bool printerFound = false;
                foreach (string printer in PrinterSettings.InstalledPrinters)
                {
                    if (printer.Equals(printerName, StringComparison.OrdinalIgnoreCase))
                    {
                        printerFound = true;
                        break;
                    }
                }

                if (printerFound)
                {
                    printDocument1.PrinterSettings.PrinterName = printerName;
                    printDocument1.PrintController = new StandardPrintController();
                    printDocument1.Print();
                }
                else
                {
                    PrintDialog pd = new PrintDialog();
                    pd.Document = printDocument1;
                    if (pd.ShowDialog() == DialogResult.OK)
                    {
                        printDocument1.Print();
                    }
                }
            }
            catch (Exception ex)
            {
                ToastNotification.ShowError("Error al generar PDF: " + ex.Message);
            }
        }

        private void printDocument1_PrintPage(object sender, PrintPageEventArgs e)
        {
            if (vistaHistorialActiva)
                ImprimirHistorial(e);
            else
                ImprimirHerramientas(e);
        }

        private void ImprimirHerramientas(PrintPageEventArgs e)
        {
            Font fontHeader = new Font("Arial", 14, FontStyle.Bold);
            Font fontSubHeader = new Font("Arial", 10, FontStyle.Bold);
            Font fontBody = new Font("Arial", 9, FontStyle.Regular);

            Brush brushText = Brushes.Black;
            Pen penLine = Pens.Black;

            float x = 40;
            float y = 40;
            float offset = 28;

            e.Graphics.DrawString("INFORME DE HERRAMIENTAS", fontHeader, brushText, x, y);
            y += offset;
            e.Graphics.DrawString("Fecha: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm"), fontBody, brushText, x, y);
            y += offset + 10;

            float[] colWidths = new float[] { 180, 100, 150, 180 };
            string[] headers = new string[] { "Nombre", "Estado", "Ubicación", "Observaciones" };

            float currentX = x;
            for (int i = 0; i < headers.Length; i++)
            {
                e.Graphics.FillRectangle(Brushes.DarkGreen, currentX, y, colWidths[i], 25);
                e.Graphics.DrawRectangle(penLine, currentX, y, colWidths[i], 25);
                e.Graphics.DrawString(headers[i], fontSubHeader, Brushes.White, currentX + 4, y + 4);
                currentX += colWidths[i];
            }
            y += 25;

            foreach (DataGridViewRow row in dgvHerramientas.Rows)
            {
                if (row.IsNewRow) continue;
                if (y > e.MarginBounds.Bottom) break;

                currentX = x;
                string[] cells = new string[]
                {
                    row.Cells["nombre"].Value?.ToString() ?? "",
                    row.Cells["estado"].Value?.ToString() ?? "",
                    row.Cells["nombre_obra"].Value?.ToString() ?? "",
                    row.Cells["observaciones"].Value?.ToString() ?? ""
                };

                for (int i = 0; i < cells.Length; i++)
                {
                    e.Graphics.DrawRectangle(penLine, currentX, y, colWidths[i], 22);
                    e.Graphics.DrawString(cells[i], fontBody, brushText, new RectangleF(currentX + 3, y + 3, colWidths[i] - 4, 22));
                    currentX += colWidths[i];
                }
                y += 22;
            }

            y += 15;
            e.Graphics.DrawString($"Total herramientas: {dgvHerramientas.Rows.Count}", fontSubHeader, brushText, x, y);
        }

        private void ImprimirHistorial(PrintPageEventArgs e)
        {
            Font fontHeader = new Font("Arial", 14, FontStyle.Bold);
            Font fontSubHeader = new Font("Arial", 10, FontStyle.Bold);
            Font fontBody = new Font("Arial", 9, FontStyle.Regular);

            Brush brushText = Brushes.Black;
            Pen penLine = Pens.Black;

            float x = 40;
            float y = 40;
            float offset = 28;

            e.Graphics.DrawString("HISTORIAL DE MOVIMIENTOS - HERRAMIENTAS", fontHeader, brushText, x, y);
            y += offset;

            string filtro = cboFiltroHerramienta.SelectedIndex > 0
                ? ((Herramienta)cboFiltroHerramienta.SelectedItem).nombre
                : "Todas";
            e.Graphics.DrawString("Filtro: " + filtro, fontBody, brushText, x, y);
            y += 18;
            e.Graphics.DrawString("Fecha: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm"), fontBody, brushText, x, y);
            y += offset + 10;

            float[] colWidths = new float[] { 170, 150, 150, 140 };
            string[] headers = new string[] { "Herramienta", "Origen", "Destino", "Fecha" };

            float currentX = x;
            for (int i = 0; i < headers.Length; i++)
            {
                e.Graphics.FillRectangle(Brushes.DarkGreen, currentX, y, colWidths[i], 25);
                e.Graphics.DrawRectangle(penLine, currentX, y, colWidths[i], 25);
                e.Graphics.DrawString(headers[i], fontSubHeader, Brushes.White, currentX + 4, y + 4);
                currentX += colWidths[i];
            }
            y += 25;

            foreach (DataGridViewRow row in dgvHistorial.Rows)
            {
                if (row.IsNewRow) continue;
                if (y > e.MarginBounds.Bottom) break;

                currentX = x;
                string fechaStr = "";
                if (row.Cells["fecha"].Value != null && DateTime.TryParse(row.Cells["fecha"].Value.ToString(), out DateTime dt))
                    fechaStr = dt.ToString("dd/MM/yyyy HH:mm");

                string[] cells = new string[]
                {
                    row.Cells["nombre_herramienta"].Value?.ToString() ?? "",
                    row.Cells["obra_origen"].Value?.ToString() ?? "",
                    row.Cells["obra_destino"].Value?.ToString() ?? "",
                    fechaStr
                };

                for (int i = 0; i < cells.Length; i++)
                {
                    e.Graphics.DrawRectangle(penLine, currentX, y, colWidths[i], 22);
                    e.Graphics.DrawString(cells[i], fontBody, brushText, new RectangleF(currentX + 3, y + 3, colWidths[i] - 4, 22));
                    currentX += colWidths[i];
                }
                y += 22;
            }

            y += 15;
            e.Graphics.DrawString($"Total movimientos: {dgvHistorial.Rows.Count}", fontSubHeader, brushText, x, y);
        }
    }
}
