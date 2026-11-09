using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using SteeLog.Core.Application.Canaletas;
using SteeLog.Core.Application.Catalogos;
using SteeLog.Core.Application.Activos;
using SteeLog.Presentation;
using SteeLog.CompositionRoot;
using System.Windows.Forms;
using SteeLog.Core.Application.Reportes;
using SteeLog.Modelos;
using SteeLog.Reportes;
using SteeLog.Servicios;
using SteeLog;
using SteeLog.UI;
using SteeLog.Vistas.Inventario;

namespace SteeLog.Vistas.Reportes
{
    public partial class Vista_InformeObra : Form, IRefrescable
    {
        private string tabActual = "movimientos";
        private bool cargando = false;
        private List<InformeMovimientoDetallePdf> movimientosActuales = new List<InformeMovimientoDetallePdf>();
        private List<InformeHerramientaDetallePdf> herramientasActuales = new List<InformeHerramientaDetallePdf>();
        private List<InformeInsumoDetallePdf> insumosActuales = new List<InformeInsumoDetallePdf>();
        private List<CanaletaLectura> canaletasPlegadosActuales = new List<CanaletaLectura>();
        private readonly ContextMenuStrip menuHerramientas = new ContextMenuStrip();
        private readonly ToolStripMenuItem itemEnviarAGalpon = new ToolStripMenuItem("Enviar herramienta al galpón");
        private readonly CancellationTokenSource vida = new CancellationTokenSource();
        private CancellationTokenSource cargaObra;
        private int versionObra;
        private readonly CanaletasPresenter presenterInyectado;
        private readonly Func<int, string, CancellationToken, Task<LecturasObra>> cargarLecturas;
        internal Func<string, int, string, DateTime?, DateTime?, CancellationToken, Task> GenerarInforme;
        internal Action<string> AbrirInforme;
        internal Func<CancellationToken, Task<IReadOnlyList<ObraCatalogo>>> ConsultarObras;
        internal Func<int, CancellationToken, Task<int>> RetornarHerramienta;
        internal Action<string> AvisarRetorno;
        private bool retornandoHerramienta;
        private bool generandoInforme;
        private CanaletasPresenter Presenter => presenterInyectado ?? new CanaletasPresenter(LocalServices.Current.Executor);
        internal sealed class LecturasObra
        {
            internal List<InformeMovimientoDetallePdf> Movimientos;
            internal List<InformeHerramientaDetallePdf> Herramientas;
            internal List<InformeInsumoDetallePdf> Insumos;
            internal bool Mostrar;
        }
        internal Vista_InformeObra(CanaletasPresenter presenter, Func<int, string, CancellationToken, Task<LecturasObra>> cargar) : this()
        { presenterInyectado = presenter; cargarLecturas = cargar; }
        private async Task<LecturasObra> LeerDatosAsync(int id, string nombre, CancellationToken token)
        {
            if (cargarLecturas != null) return await cargarLecturas(id, nombre, token);
            var lecturas = await new InformeObraPresenter(LocalServices.Current.Executor, LocalServices.Current.InformeObraPdf).LecturasAsync(id, nombre, token);
            return new LecturasObra { Movimientos = lecturas.Movimientos, Herramientas = lecturas.Herramientas, Insumos = lecturas.Insumos, Mostrar = lecturas.MostrarValorizacion };
        }
        private void HabilitarAcciones(bool habilitado) { panelTabs.Enabled = habilitado; dgvDatos.Enabled = habilitado; btnDescargar.Enabled = habilitado; }
        private void CancelarOperaciones() { vida.Cancel(); cargaObra?.Cancel(); }
        protected override void OnFormClosing(FormClosingEventArgs e) { CancelarOperaciones(); base.OnFormClosing(e); }
        private Label lblMateriales;
        private bool mostrarValorizacionActual;

        public Vista_InformeObra()
        {
            InitializeComponent();
            Tema.SinEscala(this);
            ConfigurarResumenUI();
            AplicarEstiloUI();
            InicializarMenuHerramientas();
            dgvDatos.CellContentClick += dgvDatos_CellContentClick;
            Load += (s, e) => CargarObras();
        }

        private TarjetaKpi kpiRemitos, kpiMateriales, kpiHerramientas, kpiInsumos;
        private Label lblVacio;

        private void ConfigurarResumenUI()
        {
            lblMateriales = CrearLabelResumen("Materiales: —");

            kpiRemitos = new TarjetaKpi { Etiqueta = "Remitos", Dock = DockStyle.Fill, Margin = new Padding(0, 0, 8, 0) };
            kpiMateriales = new TarjetaKpi { Etiqueta = "Materiales", Dock = DockStyle.Fill, Margin = new Padding(8, 0, 8, 0) };
            kpiHerramientas = new TarjetaKpi { Etiqueta = "Herramientas", Dock = DockStyle.Fill, Margin = new Padding(8, 0, 8, 0) };
            kpiInsumos = new TarjetaKpi { Etiqueta = "Insumos", Dock = DockStyle.Fill, Margin = new Padding(8, 0, 0, 0) };

            panelResumen.SuspendLayout();
            panelResumen.Controls.Clear();
            panelResumen.BackColor = Tema.Fondo;
            panelResumen.ColumnCount = 4;
            panelResumen.ColumnStyles.Clear();
            for (int i = 0; i < 4; i++)
                panelResumen.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

            panelResumen.RowCount = 2;
            panelResumen.RowStyles.Clear();
            panelResumen.RowStyles.Add(new RowStyle(SizeType.Absolute, TarjetaKpi.AltoTarjeta));
            panelResumen.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            panelResumen.Padding = new Padding(28, 12, 28, 0);
            panelResumen.Height = 12 + TarjetaKpi.AltoTarjeta + 28;

            panelResumen.Controls.Add(kpiRemitos, 0, 0);
            panelResumen.Controls.Add(kpiMateriales, 1, 0);
            panelResumen.Controls.Add(kpiHerramientas, 2, 0);
            panelResumen.Controls.Add(kpiInsumos, 3, 0);
            panelResumen.Controls.Add(lblEstado, 0, 1);
            panelResumen.SetColumnSpan(lblEstado, 4);

            lblEstado.Font = Tema.Fuente(10f);
            lblEstado.ForeColor = Tema.TextoSecundario;
            lblEstado.TextAlign = ContentAlignment.MiddleLeft;
            lblEstado.Padding = new Padding(2, 0, 0, 0);
            lblEstado.Dock = DockStyle.Fill;
            panelResumen.ResumeLayout();
        }

        private void ActualizarKpis(string remitos, string materiales, string herramientas, string insumos, string detalleHerr = "", string detalleIns = "")
        {
            kpiRemitos.Valor = remitos;
            kpiMateriales.Valor = materiales;
            kpiHerramientas.Valor = herramientas;
            kpiHerramientas.Detalle = detalleHerr;
            kpiInsumos.Valor = insumos;
            kpiInsumos.Detalle = detalleIns;
        }

        private static Label CrearLabelResumen(string texto)
        {
            return new Label
            {
                Text = texto,
                Font = Tema.FuenteTexto,
                ForeColor = Tema.TextoSecundario,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
        }

        private void AplicarEstiloUI()
        {
            BackColor = Tema.Fondo;
            SuspendLayout();

            // Título: banda unificada en lugar del título centrado del diseñador.
            panelTitulo.Visible = false;

            // Filtros: Obra + combo + descarga
            panelFiltros.BackColor = Tema.Fondo;
            panelFiltros.Height = 60;
            panelFiltros.Padding = new Padding(28, 12, 28, 12);
            panelFiltros.ColumnStyles.Clear();
            panelFiltros.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            panelFiltros.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            panelFiltros.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            label2.AutoSize = true;
            label2.Text = "Obra:";
            label2.Font = Tema.FuenteTexto;
            label2.ForeColor = Tema.TextoSecundario;
            label2.Anchor = AnchorStyles.Left;
            label2.Margin = new Padding(0, 0, 12, 0);
            cmbObras.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            cmbObras.Margin = new Padding(0, 0, 12, 0);
            cmbObras.Height = Tema.AltoControl;
            Tema.Combo(cmbObras);
            cmbObras.ItemHeight = 30;
            btnDescargar.Text = "Descargar informe PDF";
            btnDescargar.Anchor = AnchorStyles.Right;
            btnDescargar.Margin = new Padding(0);
            btnDescargar.Size = new Size(210, Tema.AltoControl);
            Tema.Boton(btnDescargar, Variante.Primario);

            // Pestañas
            panelTabs.BackColor = Tema.Fondo;
            panelTabs.Height = 58;
            panelTabs.Padding = new Padding(28, 12, 28, 0);
            panelTabs.ColumnStyles.Clear();
            panelTabs.ColumnCount = 6;
            for (int i = 0; i < 6; i++) panelTabs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.6667F));
            var textos = new[] { "Movimientos", "Herramientas", "Insumos", "Materiales", "Eventuales", "Canaletas" };
            var tabs = new[] { btnTabMovimientos, btnTabHerramientas, btnTabInsumos, btnTabMateriales, btnTabEventuales, btnTabCanaletasPlegados };
            for (int i = 0; i < tabs.Length; i++)
            {
                tabs[i].Text = textos[i];
                tabs[i].Anchor = AnchorStyles.Left | AnchorStyles.Right;
                tabs[i].Margin = new Padding(0, 0, 8, 0);
                tabs[i].Height = Tema.AltoControl;
            }
            ActualizarEstiloTabs();

            // Grilla en panel con márgenes
            dgvDatos.RowTemplate.DefaultCellStyle.Font = Tema.FuenteTexto;
            Tema.Grilla(dgvDatos);
            dgvDatos.AllowUserToAddRows = false;
            dgvDatos.AllowUserToResizeRows = false;
            var panelGrilla = new Panel { Dock = DockStyle.Fill, BackColor = Tema.Fondo, Padding = new Padding(28, 4, 28, 20) };
            Controls.Remove(dgvDatos);
            panelGrilla.Controls.Add(dgvDatos);
            Controls.Add(panelGrilla);
            panelGrilla.BringToFront();

            lblVacio = new Label
            {
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = Tema.Fuente(11f),
                ForeColor = Tema.TextoApagado,
                BackColor = Tema.Fondo,
                Height = 90,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Location = new Point(0, Tema.AltoHeader + 60)
            };
            dgvDatos.Controls.Add(lblVacio);
            lblVacio.Width = Math.Max(100, dgvDatos.ClientSize.Width);
            dgvDatos.DataSourceChanged += (s, e) => ActualizarVacio();
            dgvDatos.RowsAdded += (s, e) => ActualizarVacio();
            dgvDatos.RowsRemoved += (s, e) => ActualizarVacio();

            var banda = Tema.TituloVista(this, "Informe de obra");
            if (banda != null) banda.SendToBack();
            ResumeLayout(true);
            ActualizarVacio();
        }

        private static string NombrePestania(string tab)
        {
            switch (tab)
            {
                case "herramientas": return "herramientas";
                case "insumos": return "insumos";
                case "materiales": return "materiales";
                case "eventuales": return "eventuales";
                case "canaletas_plegados": return "canaletas y plegados";
                default: return "movimientos";
            }
        }

        private void ActualizarVacio()
        {
            if (lblVacio == null || dgvDatos == null || lblVacio.IsDisposed) return;
            bool sinObra = cmbObras == null || cmbObras.SelectedIndex < 0;
            bool vacio = sinObra || dgvDatos.Rows.Count == 0;
            lblVacio.Visible = vacio;
            if (vacio)
                lblVacio.Text = sinObra
                    ? "Seleccioná una obra para ver su informe.\nElegila en la lista de arriba."
                    : "Esta obra no tiene registros en " + NombrePestania(tabActual) + ".";
        }

        public void Refrescar() { CargarObras(); }

        private async void CargarObras()
        {
            var token = vida.Token;
            int version = ++versionObra;
            cargaObra?.Cancel();
            cargando = true;
            HabilitarAcciones(false);
            try
            {
                var obras = ConsultarObras != null ? await ConsultarObras(token)
                    : await LocalServices.Current.Executor.ExecuteAsync<ConsultarCatalogos, IReadOnlyList<ObraCatalogo>>(x => x.ObrasAsync(token));
                if (token.IsCancellationRequested || IsDisposed || version != versionObra) return;
                var dt = new DataTable();
                dt.Columns.Add("id", typeof(int));
                dt.Columns.Add("nombre", typeof(string));
                foreach (var obra in obras) dt.Rows.Add(obra.Id, obra.Nombre);
                cmbObras.DataSource = dt;
                cmbObras.DisplayMember = "nombre";
                cmbObras.ValueMember = "id";
                cmbObras.SelectedIndex = -1;
                LimpiarDatos();
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { if (!token.IsCancellationRequested && !IsDisposed && version == versionObra) ToastNotification.ShowError("Error al cargar obras:\n" + ex.Message); }
            finally { if (!IsDisposed && version == versionObra) cargando = false; }
        }

        private void cmbObras_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cargando) return;
            versionObra++;
            cargaObra?.Cancel();

            if (cmbObras.SelectedIndex == -1 || cmbObras.SelectedValue == null)
            {
                LimpiarDatos();
                HabilitarAcciones(false);
                return;
            }

            // Proteger contra DataRowView no resuelto
            if (cmbObras.SelectedValue is DataRowView)
                return;

            CargarDatosObra();
        }

        private async void CargarDatosObra()
        {
            cargaObra?.Cancel();
            cargaObra = CancellationTokenSource.CreateLinkedTokenSource(vida.Token);
            var token = cargaObra.Token;
            int version = ++versionObra;
            try
            {
                int idObra = Convert.ToInt32(cmbObras.SelectedValue);
                DataRowView drv = (DataRowView)cmbObras.SelectedItem;
                string nombreObra = drv["nombre"].ToString();

                HabilitarAcciones(false);
                var lecturasTask = LeerDatosAsync(idObra, nombreObra, token);
                var canaletasTask = Presenter.ListarAsync(idObra, token);
                await Task.WhenAll(lecturasTask, canaletasTask);
                if (token.IsCancellationRequested || IsDisposed || version != versionObra) return;
                var lecturas = await lecturasTask;
                movimientosActuales = lecturas.Movimientos;
                herramientasActuales = lecturas.Herramientas;
                insumosActuales = lecturas.Insumos;
                canaletasPlegadosActuales = (await canaletasTask).ToList();
                mostrarValorizacionActual = lecturas.Mostrar;
                HabilitarAcciones(true);
                decimal totalCanaletasPendientes = canaletasPlegadosActuales
                    .Where(c => !c.Pagado)
                    .Sum(c => c.Precio);
                InformeObraResumenPdf resumen = ConsultarInformeObra.Resumir(movimientosActuales, mostrarValorizacionActual, totalCanaletasPendientes);

                // Actualizar resumen
                ActualizarKpis(Convert.ToString(resumen.CantRemitos), Convert.ToString(resumen.TotalMateriales),
                    herramientasActuales.Count.ToString(), insumosActuales.Count.ToString(),
                    herramientasActuales.Count(h => h.en_obra) + " en obra", insumosActuales.Count(i => i.en_obra) + " en obra");
                lblEstado.Text = resumen.CantMovimientos + " movimientos y " + canaletasPlegadosActuales.Count + " canaletas/plegados";
                lblEstado.ForeColor = Tema.TextoSecundario;

                // Cargar la tab activa
                CargarTab(tabActual);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                if (token.IsCancellationRequested || IsDisposed || version != versionObra) return;
                lblEstado.ForeColor = Color.FromArgb(226, 96, 86);
                lblEstado.Text = "Error al cargar datos";
                ToastNotification.ShowError("Error al cargar datos de la obra:\n" + ex.Message);
            }
        }

        private void LimpiarDatos()
        {
            movimientosActuales.Clear();
            herramientasActuales.Clear();
            insumosActuales.Clear();
            canaletasPlegadosActuales.Clear();
            dgvDatos.DataSource = null;
            ActualizarKpis("—", "—", "—", "—");
            lblEstado.Text = "";
            lblEstado.ForeColor = Tema.TextoSecundario;
            mostrarValorizacionActual = false;
            ActualizarVacio();
        }

        // ======================== TABS ========================

        private void btnTabMovimientos_Click(object sender, EventArgs e)
        {
            CargarTab("movimientos");
        }

        private void btnTabHerramientas_Click(object sender, EventArgs e)
        {
            CargarTab("herramientas");
        }

        private void btnTabInsumos_Click(object sender, EventArgs e)
        {
            CargarTab("insumos");
        }

        private void btnTabMateriales_Click(object sender, EventArgs e)
        {
            CargarTab("materiales");
        }

        private void btnTabEventuales_Click(object sender, EventArgs e)
        {
            CargarTab("eventuales");
        }

        private void btnTabCanaletasPlegados_Click(object sender, EventArgs e)
        {
            CargarTab("canaletas_plegados");
        }

        private void CargarTab(string tab)
        {
            tabActual = tab;
            ActualizarEstiloTabs();

            switch (tab)
            {
                case "movimientos":
                    MostrarMovimientos();
                    break;
                case "herramientas":
                    MostrarHerramientas();
                    break;
                case "insumos":
                    MostrarInsumos();
                    break;
                case "materiales":
                    MostrarMateriales();
                    break;
                case "eventuales":
                    MostrarEventuales();
                    break;
                case "canaletas_plegados":
                    MostrarCanaletasPlegados();
                    break;
            }
            ActualizarVacio();
        }

        private void ActualizarEstiloTabs()
        {
            Action<Guna.UI2.WinForms.Guna2Button, string> estilo = (b, tab) =>
            {
                if (b == null) return;
                Tema.Boton(b, tabActual == tab ? Variante.TabActiva : Variante.Tab);
            };
            estilo(btnTabMovimientos, "movimientos");
            estilo(btnTabHerramientas, "herramientas");
            estilo(btnTabInsumos, "insumos");
            estilo(btnTabMateriales, "materiales");
            estilo(btnTabEventuales, "eventuales");
            estilo(btnTabCanaletasPlegados, "canaletas_plegados");
        }

        // ======================== GRILLAS ========================

        private void MostrarMovimientos()
        {
            // Crear DataTable para mostrar movimientos
            DataTable dt = new DataTable();
            dt.Columns.Add("Remito", typeof(string));
            dt.Columns.Add("Fecha", typeof(DateTime));
            dt.Columns.Add("Tipo", typeof(string));
            dt.Columns.Add("Material", typeof(string));
            dt.Columns.Add("Cantidad", typeof(decimal));
            dt.Columns.Add("Origen", typeof(string));
            dt.Columns.Add("Destino", typeof(string));
            if (mostrarValorizacionActual)
            {
                dt.Columns.Add("Precio Unit.", typeof(decimal));
                dt.Columns.Add("Total", typeof(decimal));
            }

            // Agrupar: Envío a Obra primero
            var ordenados = movimientosActuales
                .OrderByDescending(m => m.tipo_movimiento.ToUpper().Contains("ENVÍO") || m.tipo_movimiento.ToUpper().Contains("ENVIO"))
                .ThenBy(m => m.tipo_movimiento)
                .ThenBy(m => m.fecha)
                .ToList();

            var totalesFilas = mostrarValorizacionActual ? ValorizacionInformeObra.ValoresFilas(ordenados, movimientosActuales) : null;
            for (int i = 0; i < ordenados.Count; i++)
            {
                var m = ordenados[i];
                string material = m.material;
                if (m.esrecorte && !string.IsNullOrEmpty(m.medida_recorte))
                    material += " [RECORTE: " + m.medida_recorte + "]";
                else if (m.esrecorte)
                    material += " [RECORTE]";

                var valores = new List<object>
                {
                    !string.IsNullOrWhiteSpace(m.nroremito) ? m.nroremito : "-",
                    m.fecha.Date,
                    m.tipo_movimiento,
                    material,
                    m.cantidad,
                    m.origen,
                    m.destino
                };

                if (mostrarValorizacionActual)
                {
                    valores.Add(m.precio_unitario);
                    valores.Add(totalesFilas[i]);
                }

                dt.Rows.Add(valores.ToArray());
            }

            dgvDatos.DataSource = dt;
            ConfigurarColumnasMovimientos();
        }

        private void ConfigurarColumnasMovimientos()
        {
            if (dgvDatos.Columns["Remito"] != null) dgvDatos.Columns["Remito"].FillWeight = 10;
            if (dgvDatos.Columns["Fecha"] != null)
            {
                dgvDatos.Columns["Fecha"].FillWeight = 12;
                dgvDatos.Columns["Fecha"].DefaultCellStyle.Format = "dd/MM/yyyy";
            }
            if (dgvDatos.Columns["Tipo"] != null) dgvDatos.Columns["Tipo"].FillWeight = 18;
            if (dgvDatos.Columns["Material"] != null) dgvDatos.Columns["Material"].FillWeight = 25;
            if (dgvDatos.Columns["Cantidad"] != null)
            {
                dgvDatos.Columns["Cantidad"].FillWeight = 10;
                dgvDatos.Columns["Cantidad"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                dgvDatos.Columns["Cantidad"].DefaultCellStyle.Format = "G29";
            }
            if (dgvDatos.Columns["Origen"] != null) dgvDatos.Columns["Origen"].FillWeight = 15;
            if (dgvDatos.Columns["Destino"] != null) dgvDatos.Columns["Destino"].FillWeight = 15;
            ConfigurarColumnaMoneda("Precio Unit.", 12);
            ConfigurarColumnaMoneda("Total", 13);

            // Centrar Remito y Fecha
            if (dgvDatos.Columns["Remito"] != null)
                dgvDatos.Columns["Remito"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            if (dgvDatos.Columns["Fecha"] != null)
                dgvDatos.Columns["Fecha"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        }

        private void ConfigurarColumnaMoneda(string nombre, float fillWeight)
        {
            if (dgvDatos.Columns[nombre] == null) return;

            dgvDatos.Columns[nombre].FillWeight = fillWeight;
            dgvDatos.Columns[nombre].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgvDatos.Columns[nombre].DefaultCellStyle.Format = "C2";
            dgvDatos.Columns[nombre].DefaultCellStyle.Padding = new Padding(0, 0, 10, 0);
        }

        private void MostrarHerramientas()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("IdHerramienta", typeof(int));
            dt.Columns.Add("Herramienta", typeof(string));
            dt.Columns.Add("Estado", typeof(string));
            dt.Columns.Add("Fecha Ingreso", typeof(string));
            dt.Columns.Add("Fecha Retorno", typeof(string));

            foreach (var h in herramientasActuales)
            {
                dt.Rows.Add(
                    h.id_herramienta,
                    h.nombre,
                    h.en_obra ? "EN OBRA" : "REGRESÓ",
                    h.fecha_ingreso?.ToString("dd/MM/yyyy") ?? "-",
                    h.fecha_retorno?.ToString("dd/MM/yyyy") ?? (h.en_obra ? "EN OBRA" : "-")
                );
            }

            dgvDatos.DataSource = dt;
            ConfigurarColumnasHerramientas();
        }

        private void ConfigurarColumnasHerramientas()
        {
            if (dgvDatos.Columns["IdHerramienta"] != null)
                dgvDatos.Columns["IdHerramienta"].Visible = false;
            if (dgvDatos.Columns["Herramienta"] != null)
            {
                dgvDatos.Columns["Herramienta"].FillWeight = 35;
            }
            if (dgvDatos.Columns["Estado"] != null)
            {
                dgvDatos.Columns["Estado"].FillWeight = 20;
                dgvDatos.Columns["Estado"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
            if (dgvDatos.Columns["Fecha Ingreso"] != null)
            {
                dgvDatos.Columns["Fecha Ingreso"].FillWeight = 22;
                dgvDatos.Columns["Fecha Ingreso"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
            if (dgvDatos.Columns["Fecha Retorno"] != null)
            {
                dgvDatos.Columns["Fecha Retorno"].FillWeight = 23;
                dgvDatos.Columns["Fecha Retorno"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
        }

        private void MostrarInsumos()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("Insumo", typeof(string));
            dt.Columns.Add("Estado", typeof(string));
            dt.Columns.Add("Fecha Ingreso", typeof(string));
            dt.Columns.Add("Fecha Retorno", typeof(string));

            foreach (var ins in insumosActuales)
            {
                string estado = ins.en_obra ? "EN OBRA" : (ins.consumido ? "CONSUMIDO" : "REGRESÓ");
                dt.Rows.Add(
                    ins.nombre,
                    estado,
                    ins.fecha_ingreso?.ToString("dd/MM/yyyy") ?? "-",
                    ins.fecha_retorno?.ToString("dd/MM/yyyy") ?? (ins.en_obra ? "EN OBRA" : "-")
                );
            }

            dgvDatos.DataSource = dt;
            ConfigurarColumnasInsumos();
        }

        private void ConfigurarColumnasInsumos()
        {
            if (dgvDatos.Columns["Insumo"] != null)
            {
                dgvDatos.Columns["Insumo"].FillWeight = 35;
            }
            if (dgvDatos.Columns["Estado"] != null)
            {
                dgvDatos.Columns["Estado"].FillWeight = 20;
                dgvDatos.Columns["Estado"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
            if (dgvDatos.Columns["Fecha Ingreso"] != null)
            {
                dgvDatos.Columns["Fecha Ingreso"].FillWeight = 22;
                dgvDatos.Columns["Fecha Ingreso"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
            if (dgvDatos.Columns["Fecha Retorno"] != null)
            {
                dgvDatos.Columns["Fecha Retorno"].FillWeight = 23;
                dgvDatos.Columns["Fecha Retorno"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
        }

        private void MostrarMateriales()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("Material",        typeof(string));
            dt.Columns.Add("Total Enviado",   typeof(decimal));
            dt.Columns.Add("Total Devuelto",  typeof(decimal));
            dt.Columns.Add("Neto en Obra",    typeof(decimal));
            if (mostrarValorizacionActual)
            {
                dt.Columns.Add("Precio Unit.", typeof(decimal));
                dt.Columns.Add("Valor Neto", typeof(decimal));
            }

            // Obtener el nombre de la obra seleccionada para comparar destino/origen
            string nombreObra = "";
            if (cmbObras.SelectedItem is DataRowView drv)
                nombreObra = drv["nombre"]?.ToString() ?? "";

            var grupos = movimientosActuales
                .Where(m => !m.esrecorte)
                .GroupBy(m => m.material)
                .OrderBy(g => g.Key)
                .ToList();

            foreach (var grupo in grupos)
            {
                var balance = ValorizacionInformeObra.BalanceMaterial(grupo, nombreObra);
                if (mostrarValorizacionActual)
                    dt.Rows.Add(grupo.Key, balance.Enviado, balance.Devuelto, balance.Neto, balance.PrecioUnitario, balance.ValorNeto);
                else
                    dt.Rows.Add(grupo.Key, balance.Enviado, balance.Devuelto, balance.Neto);
            }

            if (mostrarValorizacionActual && dt.Rows.Count > 0)
            {
                dt.Rows.Add(
                    "TOTAL MATERIALES",
                    dt.AsEnumerable().Sum(r => r.Field<decimal>("Total Enviado")),
                    dt.AsEnumerable().Sum(r => r.Field<decimal>("Total Devuelto")),
                    dt.AsEnumerable().Sum(r => r.Field<decimal>("Neto en Obra")),
                    0m,
                    dt.AsEnumerable().Sum(r => r.Field<decimal>("Valor Neto")));
            }

            dgvDatos.DataSource = dt;
            ConfigurarColumnasMateriales();
        }

        private void ConfigurarColumnasMateriales()
        {
            if (dgvDatos.Columns["Material"] != null)
            {
                dgvDatos.Columns["Material"].FillWeight = mostrarValorizacionActual ? 34 : 45;
            }
            if (dgvDatos.Columns["Total Enviado"] != null)  { dgvDatos.Columns["Total Enviado"].FillWeight = 13; dgvDatos.Columns["Total Enviado"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter; }
            if (dgvDatos.Columns["Total Devuelto"] != null) { dgvDatos.Columns["Total Devuelto"].FillWeight = 13; dgvDatos.Columns["Total Devuelto"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter; }
            if (dgvDatos.Columns["Neto en Obra"] != null)   { dgvDatos.Columns["Neto en Obra"].FillWeight = 13; dgvDatos.Columns["Neto en Obra"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter; dgvDatos.Columns["Neto en Obra"].DefaultCellStyle.Font = Tema.Fuente(10.5f, true); }
            ConfigurarColumnaMoneda("Precio Unit.", 13);
            ConfigurarColumnaMoneda("Valor Neto", 14);

            if (mostrarValorizacionActual && dgvDatos.Rows.Count > 0)
            {
                DataGridViewRow filaTotal = dgvDatos.Rows[dgvDatos.Rows.Count - 1];
                filaTotal.DefaultCellStyle.Font = Tema.Fuente(10.5f, true);
                filaTotal.DefaultCellStyle.BackColor = Tema.Banda;
                filaTotal.DefaultCellStyle.ForeColor = Color.White;
                filaTotal.DefaultCellStyle.SelectionBackColor = Tema.Banda;
            }
        }

        private void MostrarEventuales()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("Material / Descripción", typeof(string));
            dt.Columns.Add("Tipo",                   typeof(string));
            dt.Columns.Add("Cantidad",               typeof(decimal));
            dt.Columns.Add("Fecha",                  typeof(string));
            if (mostrarValorizacionActual)
            {
                dt.Columns.Add("Precio Unit.", typeof(decimal));
                dt.Columns.Add("Total", typeof(decimal));
            }

            // Filtramos solo movimientos de tipo Eventual o Aporte
            var eventuales = movimientosActuales
                .Where(m => !m.esrecorte && (m.tipo_movimiento.ToUpper().Contains("EVENTUAL") || m.tipo_movimiento.ToUpper().Contains("APORTE")))
                .OrderByDescending(m => m.fecha)
                .ToList();

            var totalesFilas = mostrarValorizacionActual ? ValorizacionInformeObra.ValoresFilas(eventuales, eventuales) : null;
            for (int i = 0; i < eventuales.Count; i++)
            {
                var m = eventuales[i];
                var valores = new List<object>
                {
                    m.material,
                    m.tipo_movimiento,
                    m.cantidad,
                    m.fecha.ToString("dd/MM/yyyy")
                };

                if (mostrarValorizacionActual)
                {
                    valores.Add(m.precio_unitario);
                    valores.Add(totalesFilas[i]);
                }

                dt.Rows.Add(valores.ToArray());
            }

            dgvDatos.DataSource = dt;
            ConfigurarColumnasEventuales();
        }

        private void ConfigurarColumnasEventuales()
        {
            if (dgvDatos.Columns["Material / Descripción"] != null)
            {
                dgvDatos.Columns["Material / Descripción"].FillWeight = 45;
            }
            if (dgvDatos.Columns["Tipo"] != null)
            {
                dgvDatos.Columns["Tipo"].FillWeight = 25;
            }
            if (dgvDatos.Columns["Cantidad"] != null)
            {
                dgvDatos.Columns["Cantidad"].FillWeight = 15;
                dgvDatos.Columns["Cantidad"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
            if (dgvDatos.Columns["Fecha"] != null)
            {
                dgvDatos.Columns["Fecha"].FillWeight = 15;
                dgvDatos.Columns["Fecha"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
            ConfigurarColumnaMoneda("Precio Unit.", 15);
            ConfigurarColumnaMoneda("Total", 15);
        }

        private void MostrarCanaletasPlegados()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("IdCanaleta", typeof(int));
            dt.Columns.Add("Fecha", typeof(string));
            dt.Columns.Add("Descripción", typeof(string));
            dt.Columns.Add("Pagado", typeof(bool));
            dt.Columns.Add("Fotos", typeof(string));
            if (mostrarValorizacionActual)
                dt.Columns.Add("Precio", typeof(decimal));

            var ordenadas = canaletasPlegadosActuales
                .OrderByDescending(c => c.FechaCreacion)
                .ThenByDescending(c => c.Id)
                .ToList();

            foreach (var item in ordenadas)
            {
                int cantidadFotos = item.CantidadFotos;
                string textoFotos = cantidadFotos == 0 ? "Sin fotos" : "Ver (" + cantidadFotos + ")";

                if (mostrarValorizacionActual)
                    dt.Rows.Add(item.Id, item.FechaCreacion.ToString("dd/MM/yyyy"), item.Descripcion, item.Pagado, textoFotos, item.Precio);
                else
                    dt.Rows.Add(item.Id, item.FechaCreacion.ToString("dd/MM/yyyy"), item.Descripcion, item.Pagado, textoFotos);
            }

            if (mostrarValorizacionActual && dt.Rows.Count > 0)
            {
                dt.Rows.Add(
                    0,
                    "",
                    "TOTAL CANALETAS / PLEGADOS",
                    false,
                    "",
                    canaletasPlegadosActuales.Where(c => !c.Pagado).Sum(c => c.Precio));
            }

            dgvDatos.DataSource = dt;
            ConfigurarColumnasCanaletasPlegados();
        }

        private void ConfigurarColumnasCanaletasPlegados()
        {
            if (dgvDatos.Columns["IdCanaleta"] != null)
                dgvDatos.Columns["IdCanaleta"].Visible = false;

            if (dgvDatos.Columns["Fecha"] != null)
            {
                dgvDatos.Columns["Fecha"].FillWeight = 13;
                dgvDatos.Columns["Fecha"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }

            if (dgvDatos.Columns["Descripción"] != null)
            {
                dgvDatos.Columns["Descripción"].FillWeight = mostrarValorizacionActual ? 44 : 60;
            }

            if (dgvDatos.Columns["Pagado"] != null)
            {
                dgvDatos.Columns["Pagado"].FillWeight = mostrarValorizacionActual ? 12 : 15;
                dgvDatos.Columns["Pagado"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }

            if (dgvDatos.Columns["Fotos"] != null)
            {
                int indice = dgvDatos.Columns["Fotos"].Index;
                dgvDatos.Columns.Remove("Fotos");
                var colFotos = new DataGridViewButtonColumn
                {
                    Name = "Fotos",
                    HeaderText = "Fotos",
                    DataPropertyName = "Fotos",
                    UseColumnTextForButtonValue = false,
                    FlatStyle = FlatStyle.Flat,
                    FillWeight = mostrarValorizacionActual ? 14 : 18
                };
                dgvDatos.Columns.Insert(indice, colFotos);
                dgvDatos.Columns["Fotos"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                dgvDatos.Columns["Fotos"].DefaultCellStyle.Padding = new Padding(4, 0, 4, 0);
            }

            if (mostrarValorizacionActual && dgvDatos.Columns["Precio"] != null)
            {
                dgvDatos.Columns["Precio"].FillWeight = 17;
                dgvDatos.Columns["Precio"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                dgvDatos.Columns["Precio"].DefaultCellStyle.Format = "C2";
                dgvDatos.Columns["Precio"].DefaultCellStyle.Padding = new Padding(0, 0, 10, 0);
            }

            if (mostrarValorizacionActual && dgvDatos.Rows.Count > 0)
            {
                DataGridViewRow filaTotal = dgvDatos.Rows[dgvDatos.Rows.Count - 1];
                filaTotal.DefaultCellStyle.Font = Tema.Fuente(10.5f, true);
                filaTotal.DefaultCellStyle.BackColor = Tema.Banda;
                filaTotal.DefaultCellStyle.ForeColor = Color.White;
                filaTotal.DefaultCellStyle.SelectionBackColor = Tema.Banda;
            }
        }

        private void dgvDatos_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || tabActual != "canaletas_plegados")
                return;

            if (dgvDatos.Columns[e.ColumnIndex].Name != "Fotos")
                return;

            if (dgvDatos.Columns["IdCanaleta"] == null)
                return;

            object idValor = dgvDatos.Rows[e.RowIndex].Cells["IdCanaleta"].Value;
            if (idValor == null || idValor == DBNull.Value)
                return;

            int idCanaleta = Convert.ToInt32(idValor);
            if (idCanaleta <= 0)
                return;

            MostrarFotosCanaleta(idCanaleta);
        }

        private async void MostrarFotosCanaleta(int idCanaleta)
        {
            int version = versionObra;
            var token = vida.Token;
            try
            {
                var fotos = await Presenter.FotosAsync(idCanaleta, token);
                if (token.IsCancellationRequested || IsDisposed || version != versionObra) return;
                if (fotos.Count == 0) { ToastNotification.ShowWarning("Esta canaleta/plegado no tiene fotos cargadas."); return; }
                using (var visor = new VisorFotosCanaletaForm(fotos.ToList(), Presenter)) visor.ShowDialog(this);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { if (!token.IsCancellationRequested && !IsDisposed && version == versionObra) ToastNotification.ShowError("No se pudieron cargar las fotos:\n" + ex.Message); }
        }

        // ======================== PDF ========================

        private async void btnDescargar_Click(object sender, EventArgs e)
        {
            if (generandoInforme) return;
            if (cmbObras.SelectedIndex == -1)
            {
                ToastNotification.ShowWarning("Seleccione una obra.");
                return;
            }

            int idObra = Convert.ToInt32(cmbObras.SelectedValue);
            DataRowView drv = (DataRowView)cmbObras.SelectedItem;
            string nombreObra = drv["nombre"].ToString();

            int version = versionObra;
            using (var modal = new ModalFechasInformeObra(idObra))
            {
                if (modal.ShowDialog(this) != DialogResult.OK || vida.IsCancellationRequested || IsDisposed || version != versionObra)
                    return;

                await GenerarInformeAsync(idObra, nombreObra, modal.FechaDesde, modal.FechaHasta);

            }
        }

        internal async Task GenerarInformeAsync(int idObra, string nombreObra, DateTime? desde, DateTime? hasta)
        {
            if (generandoInforme || vida.IsCancellationRequested) return;
            generandoInforme = true;
            int version = versionObra;
            var token = cargaObra?.Token ?? vida.Token;
            btnDescargar.Enabled = false;
            try
            {
                lblEstado.ForeColor = Color.FromArgb(25, 135, 84);
                lblEstado.Text = "Generando...";
                string ruta = Path.Combine(Path.GetTempPath(), $"InformeObra_{Guid.NewGuid():N}.pdf");
                if (GenerarInforme != null) await GenerarInforme(ruta, idObra, nombreObra, desde, hasta, token);
                else await new InformeObraPDF().GenerarAsync(ruta, idObra, nombreObra, desde, hasta, token);
                if (token.IsCancellationRequested || IsDisposed || version != versionObra) return;
                lblEstado.ForeColor = Color.FromArgb(25, 135, 84);
                lblEstado.Text = "✓ PDF generado";
                if (AbrirInforme != null) AbrirInforme(ruta);
                else Process.Start(new ProcessStartInfo(ruta) { UseShellExecute = true });
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                if (token.IsCancellationRequested || IsDisposed || version != versionObra) return;
                lblEstado.ForeColor = Color.FromArgb(220, 53, 69);
                lblEstado.Text = "Error al generar";
                ToastNotification.ShowError("Error al generar el informe:\n" + ex.Message);
            }
            finally { generandoInforme = false; if (!IsDisposed && !token.IsCancellationRequested && version == versionObra) btnDescargar.Enabled = true; }
        }

        private void InicializarMenuHerramientas()
        {
            menuHerramientas.Items.Add(itemEnviarAGalpon);
            menuHerramientas.Opening += menuHerramientas_Opening;
            itemEnviarAGalpon.Click += itemEnviarAGalpon_Click;

            dgvDatos.ContextMenuStrip = menuHerramientas;
            dgvDatos.MouseDown += dgvDatos_MouseDown;
        }

        private void dgvDatos_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            var hit = dgvDatos.HitTest(e.X, e.Y);
            if (hit.RowIndex < 0)
            {
                dgvDatos.ClearSelection();
                return;
            }

            dgvDatos.ClearSelection();
            dgvDatos.Rows[hit.RowIndex].Selected = true;
            if (hit.ColumnIndex >= 0)
                dgvDatos.CurrentCell = dgvDatos.Rows[hit.RowIndex].Cells[hit.ColumnIndex];
        }

        private void menuHerramientas_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            bool habilitado = !retornandoHerramienta && dgvDatos.Enabled && tabActual == "herramientas"
                && dgvDatos.SelectedRows.Count > 0
                && dgvDatos.Columns["Estado"] != null
                && string.Equals(
                    dgvDatos.SelectedRows[0].Cells["Estado"]?.Value?.ToString(),
                    "EN OBRA",
                    StringComparison.OrdinalIgnoreCase);

            itemEnviarAGalpon.Text = "Enviar herramienta al galpón";
            itemEnviarAGalpon.Enabled = habilitado;
            e.Cancel = !habilitado;
        }

        private async void itemEnviarAGalpon_Click(object sender, EventArgs e)
        {
            if (retornandoHerramienta || !dgvDatos.Enabled || dgvDatos.SelectedRows.Count == 0 || dgvDatos.Columns["IdHerramienta"] == null)
                return;

            var row = dgvDatos.SelectedRows[0];
            if (!(row.Cells["IdHerramienta"]?.Value is int idHerramienta))
                return;

            string nombreHerramienta = row.Cells["Herramienta"]?.Value?.ToString() ?? "la herramienta seleccionada";
            if (!Dialogo.Confirmar($"¿Enviar \"{nombreHerramienta}\" al galpón?", "Confirmar retorno", false, this))
                return;

            await RetornarHerramientaAsync(idHerramienta);
        }

        private async Task RetornarHerramientaAsync(int idHerramienta)
        {
            if (retornandoHerramienta || vida.IsCancellationRequested || !dgvDatos.Enabled) return;
            retornandoHerramienta = true;
            itemEnviarAGalpon.Enabled = false;
            int version = versionObra;
            var token = vida.Token;
            try
            {
                if (RetornarHerramienta != null) await RetornarHerramienta(idHerramienta, token);
                else await LocalServices.Current.Executor.ExecuteAsync<GestionarHerramientas, int>(x => x.EnviarAGalponAsync(idHerramienta, token));
                if (token.IsCancellationRequested || IsDisposed || version != versionObra) return;
                CargarDatosObra();
                if (AvisarRetorno != null) AvisarRetorno("La herramienta fue enviada al galpón.");
                else ToastNotification.ShowSuccess("La herramienta fue enviada al galpón.");
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                if (!token.IsCancellationRequested && !IsDisposed && version == versionObra)
                {
                    if (AvisarRetorno != null) AvisarRetorno(ex.Message);
                    else ToastNotification.ShowError("No se pudo enviar la herramienta al galpón:\n" + ex.Message);
                }
            }
            finally { retornandoHerramienta = false; }
        }

        private class VisorFotosCanaletaForm : Form
        {
            private readonly List<FotoCanaletaLectura> fotos;
            private readonly CanaletasPresenter presenter;
            private readonly CancellationTokenSource vidaVisor = new CancellationTokenSource();
            private int versionFoto;
            private readonly ListBox lstFotos = new ListBox();
            private readonly PictureBox picFoto = new PictureBox();
            private readonly Label lblEstado = new Label();
            private readonly Button btnCerrar = new Button();

            public VisorFotosCanaletaForm(List<FotoCanaletaLectura> fotos, CanaletasPresenter presenter)
            {
                this.presenter = presenter;
                this.fotos = fotos ?? new List<FotoCanaletaLectura>();
                ConfigurarUI();
                CargarLista();
            }

            private void ConfigurarUI()
            {
                Text = "Fotos de canaleta / plegado";
                StartPosition = FormStartPosition.CenterParent;
                MinimumSize = new Size(760, 480);
                Size = new Size(980, 640);
                BackColor = Color.FromArgb(29, 38, 35);
                FormBorderStyle = FormBorderStyle.FixedDialog;
                MaximizeBox = false;
                MinimizeBox = false;

                lblEstado.Dock = DockStyle.Top;
                lblEstado.Height = 42;
                lblEstado.Font = new Font("Bahnschrift Light", 10.5F);
                lblEstado.ForeColor = Color.FromArgb(210, 235, 218);
                lblEstado.TextAlign = ContentAlignment.MiddleLeft;
                lblEstado.Padding = new Padding(14, 0, 14, 0);
                lblEstado.Text = "Seleccione una foto";

                lstFotos.Dock = DockStyle.Left;
                lstFotos.Width = 260;
                lstFotos.BackColor = Color.FromArgb(28, 40, 32);
                lstFotos.ForeColor = Color.White;
                lstFotos.BorderStyle = BorderStyle.FixedSingle;
                lstFotos.Font = new Font("Segoe UI", 10F);
                lstFotos.IntegralHeight = false;
                lstFotos.DisplayMember = nameof(FotoCanaletaLectura.NombreArchivo);
                lstFotos.SelectedIndexChanged += lstFotos_SelectedIndexChanged;

                picFoto.Dock = DockStyle.Fill;
                picFoto.BackColor = Color.FromArgb(20, 30, 25);
                picFoto.BorderStyle = BorderStyle.FixedSingle;
                picFoto.SizeMode = PictureBoxSizeMode.Zoom;

                btnCerrar.Dock = DockStyle.Bottom;
                btnCerrar.Height = 38;
                btnCerrar.Text = "Cerrar";
                btnCerrar.BackColor = Tema.Banda;
                btnCerrar.ForeColor = Color.White;
                btnCerrar.FlatStyle = FlatStyle.Flat;
                btnCerrar.FlatAppearance.BorderColor = Color.FromArgb(55, 85, 65);
                btnCerrar.Click += (s, e) => Close();

                Controls.Add(picFoto);
                Controls.Add(lstFotos);
                Controls.Add(btnCerrar);
                Controls.Add(lblEstado);
            }

            private void CargarLista()
            {
                lstFotos.Items.Clear();
                foreach (var foto in fotos)
                    lstFotos.Items.Add(foto);

                if (lstFotos.Items.Count > 0)
                    lstFotos.SelectedIndex = 0;
            }

            private async void lstFotos_SelectedIndexChanged(object sender, EventArgs e)
            {
                var foto = lstFotos.SelectedItem as FotoCanaletaLectura;
                if (foto == null)
                    return;

                await MostrarFotoAsync(foto);
            }

            private async Task MostrarFotoAsync(FotoCanaletaLectura foto)
            {
                int version = ++versionFoto;
                var token = vidaVisor.Token;
                try
                {
                    lblEstado.Text = "Cargando " + (foto.NombreArchivo ?? "foto") + "...";
                    picFoto.Image?.Dispose();
                    picFoto.Image = null;

                    byte[] bytes = await presenter.LeerFotoAsync(foto.ObjectPath, token);
                    if (token.IsCancellationRequested || IsDisposed || version != versionFoto) return;
                    using (var ms = new MemoryStream(bytes))
                    using (var imagen = Image.FromStream(ms))
                    {
                        picFoto.Image = new Bitmap(imagen);
                    }

                    lblEstado.Text = (foto.NombreArchivo ?? "foto") + " - " + FormatearTamanio(foto.TamanioBytes);
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    if (token.IsCancellationRequested || IsDisposed || version != versionFoto) return;
                    lblEstado.Text = "No se pudo cargar la foto.";
                    ToastNotification.ShowWarning("No se pudo cargar la foto:\n" + ex.Message);
                }
            }

            private static string FormatearTamanio(long bytes)
            {
                if (bytes <= 0)
                    return "tamaño desconocido";

                if (bytes < 1024)
                    return bytes + " B";

                if (bytes < 1024 * 1024)
                    return (bytes / 1024m).ToString("0.#") + " KB";

                return (bytes / 1024m / 1024m).ToString("0.##") + " MB";
            }

            protected override void OnFormClosing(FormClosingEventArgs e) { vidaVisor.Cancel(); base.OnFormClosing(e); }
            protected override void Dispose(bool disposing)
            {
                if (disposing) { vidaVisor.Cancel(); picFoto.Image?.Dispose(); }

                base.Dispose(disposing);
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }
    }
}
