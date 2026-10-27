using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using WP_Inventario.Datos;
using WP_Inventario.Modelos;
using WP_Inventario.Reportes;
using WP_Inventario.Servicios;
using WP_Inventario;

namespace WP_Inventario.Vistas.Reportes
{
    public partial class Vista_InformeObra : Form, IRefrescable
    {
        private string tabActual = "movimientos";
        private bool cargando = false;
        private List<InformeMovimientoDetalle> movimientosActuales = new List<InformeMovimientoDetalle>();
        private List<InformeHerramientaDetalle> herramientasActuales = new List<InformeHerramientaDetalle>();
        private List<InformeInsumoDetalle> insumosActuales = new List<InformeInsumoDetalle>();
        private List<InformeCanaletaPlegadoDetalle> canaletasPlegadosActuales = new List<InformeCanaletaPlegadoDetalle>();
        private readonly ContextMenuStrip menuHerramientas = new ContextMenuStrip();
        private readonly ToolStripMenuItem itemEnviarAGalpon = new ToolStripMenuItem("Enviar herramienta al galpón");
        private readonly CanaletaPlegadoFotoDatos canaletaFotoDatos = new CanaletaPlegadoFotoDatos();
        private Label lblMateriales;
        private bool mostrarValorizacionActual;

        public Vista_InformeObra()
        {
            InitializeComponent();
            ConfigurarResumenUI();
            InicializarMenuHerramientas();
            dgvDatos.CellContentClick += dgvDatos_CellContentClick;
            CargarObras();
        }

        private void ConfigurarResumenUI()
        {
            lblMateriales = CrearLabelResumen("Materiales: —");

            panelResumen.SuspendLayout();
            panelResumen.Controls.Clear();
            panelResumen.ColumnCount = 4;
            panelResumen.ColumnStyles.Clear();
            for (int i = 0; i < 4; i++)
                panelResumen.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

            panelResumen.RowCount = 2;
            panelResumen.RowStyles.Clear();
            panelResumen.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
            panelResumen.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            panelResumen.Height = 74;

            panelResumen.Controls.Add(lblRemitos, 0, 0);
            panelResumen.Controls.Add(lblMateriales, 1, 0);
            panelResumen.Controls.Add(lblHerramientas, 2, 0);
            panelResumen.Controls.Add(lblInsumos, 3, 0);
            panelResumen.Controls.Add(lblEstado, 0, 1);
            panelResumen.SetColumnSpan(lblEstado, 4);

            lblEstado.Font = new Font("Bahnschrift Light", 9.5F);
            lblEstado.Dock = DockStyle.Fill;
            panelResumen.ResumeLayout();
        }

        private static Label CrearLabelResumen(string texto)
        {
            return new Label
            {
                Text = texto,
                Font = new Font("Bahnschrift Light", 10F),
                ForeColor = Color.FromArgb(25, 135, 84),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
        }

        public void Refrescar()
        {
            CargarObras();
            if (cmbObras.SelectedIndex >= 0)
                CargarDatosObra();
        }

        private void CargarObras()
        {
            cargando = true;
            ComunDatos datos = new ComunDatos();
            DataTable dt = datos.ObtenerListaGenerica("obras");
            cmbObras.DataSource = dt;
            cmbObras.DisplayMember = "nombre";
            cmbObras.ValueMember = "id";
            cmbObras.SelectedIndex = -1;
            cargando = false;
        }

        private void cmbObras_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cargando) return;

            if (cmbObras.SelectedIndex == -1 || cmbObras.SelectedValue == null)
            {
                LimpiarDatos();
                return;
            }

            // Proteger contra DataRowView no resuelto
            if (cmbObras.SelectedValue is DataRowView)
                return;

            CargarDatosObra();
        }

        private void CargarDatosObra()
        {
            try
            {
                int idObra = Convert.ToInt32(cmbObras.SelectedValue);
                DataRowView drv = (DataRowView)cmbObras.SelectedItem;
                string nombreObra = drv["nombre"].ToString();

                var datos = new InformeObraDatos();

                movimientosActuales = datos.ObtenerMovimientosObra(idObra);
                herramientasActuales = datos.ObtenerHerramientasObra(idObra, nombreObra);
                insumosActuales = datos.ObtenerInsumosObra(idObra, nombreObra);
                canaletasPlegadosActuales = datos.ObtenerCanaletasPlegadosObra(idObra);
                mostrarValorizacionActual = SessionManager.Role == UserRole.Admin ||
                    nombreObra.IndexOf("galpon", StringComparison.OrdinalIgnoreCase) >= 0;
                decimal totalCanaletasPendientes = canaletasPlegadosActuales
                    .Where(c => !c.pagado)
                    .Sum(c => c.precio);
                InformeObraResumen resumen = InformeObraDatos.CalcularResumen(movimientosActuales, mostrarValorizacionActual, totalCanaletasPendientes);

                // Actualizar resumen
                lblRemitos.Text = "Remitos: " + resumen.CantRemitos;
                lblMateriales.Text = "Materiales: " + resumen.TotalMateriales;
                lblHerramientas.Text = "Herramientas: " + herramientasActuales.Count;
                lblInsumos.Text = "Insumos: " + insumosActuales.Count;
                lblEstado.Text = resumen.CantMovimientos + " movimientos y " + canaletasPlegadosActuales.Count + " canaletas/plegados";
                lblEstado.ForeColor = Color.FromArgb(25, 135, 84);

                // Cargar la tab activa
                CargarTab(tabActual);
            }
            catch (Exception ex)
            {
                lblEstado.ForeColor = Color.FromArgb(220, 53, 69);
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
            lblRemitos.Text = "Remitos: —";
            lblMateriales.Text = "Materiales: —";
            lblHerramientas.Text = "Herramientas: —";
            lblInsumos.Text = "Insumos: —";
            lblEstado.Text = "Seleccione una obra";
            lblEstado.ForeColor = Color.FromArgb(150, 150, 150);
            mostrarValorizacionActual = false;
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
        }

        private void ActualizarEstiloTabs()
        {
            Color activo = Color.FromArgb(25, 135, 84);
            Color inactivo = Color.FromArgb(35, 63, 43);

            btnTabMovimientos.FillColor  = tabActual == "movimientos"  ? activo : inactivo;
            btnTabHerramientas.FillColor = tabActual == "herramientas" ? activo : inactivo;
            btnTabInsumos.FillColor      = tabActual == "insumos"      ? activo : inactivo;
            if (btnTabMateriales != null)
                btnTabMateriales.FillColor = tabActual == "materiales" ? activo : inactivo;
            if (btnTabEventuales != null)
                btnTabEventuales.FillColor = tabActual == "eventuales" ? activo : inactivo;
            if (btnTabCanaletasPlegados != null)
                btnTabCanaletasPlegados.FillColor = tabActual == "canaletas_plegados" ? activo : inactivo;
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

            var comprasEventualesMostradas = new HashSet<int>();
            var comprasEventualesConPrecioIndividual = new HashSet<int>(
                movimientosActuales.Where(m => m.es_compra_eventual && m.precio_unitario > 0).Select(m => m.id_movimiento));
            foreach (var m in ordenados)
            {
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
                    bool esCompraEventual = (m.tipo_movimiento ?? "").IndexOf("COMPRA EVENTUAL", StringComparison.OrdinalIgnoreCase) >= 0;
                    decimal totalFila = m.cantidad * m.precio_unitario;
                    if (esCompraEventual && m.precio_total > 0 && !comprasEventualesConPrecioIndividual.Contains(m.id_movimiento))
                        totalFila = comprasEventualesMostradas.Add(m.id_movimiento) ? m.precio_total : 0;

                    valores.Add(m.precio_unitario);
                    valores.Add(totalFila);
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
                dgvDatos.Columns["Herramienta"].DefaultCellStyle.Padding = new Padding(10, 0, 0, 0);
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
                dgvDatos.Columns["Insumo"].DefaultCellStyle.Padding = new Padding(10, 0, 0, 0);
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
                decimal enviado  = grupo
                    .Where(m => m.destino != null && m.destino.Equals(nombreObra, StringComparison.OrdinalIgnoreCase))
                    .Sum(m => m.cantidad);
                decimal devuelto = grupo
                    .Where(m => m.origen != null && m.origen.Equals(nombreObra, StringComparison.OrdinalIgnoreCase))
                    .Sum(m => m.cantidad);
                decimal neto = enviado - devuelto;
                decimal precioUnitario = grupo
                    .OrderByDescending(m => m.fecha)
                    .Select(m => m.precio_unitario)
                    .FirstOrDefault();
                decimal valorEnviado = grupo
                    .Where(m => m.destino != null && m.destino.Equals(nombreObra, StringComparison.OrdinalIgnoreCase))
                    .Sum(m => m.cantidad * m.precio_unitario);
                decimal valorDevuelto = grupo
                    .Where(m => m.origen != null && m.origen.Equals(nombreObra, StringComparison.OrdinalIgnoreCase))
                    .Sum(m => m.cantidad * m.precio_unitario);

                if (mostrarValorizacionActual)
                    dt.Rows.Add(grupo.Key, enviado, devuelto, neto, precioUnitario, valorEnviado - valorDevuelto);
                else
                    dt.Rows.Add(grupo.Key, enviado, devuelto, neto);
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
                dgvDatos.Columns["Material"].DefaultCellStyle.Padding = new Padding(10, 0, 0, 0);
            }
            if (dgvDatos.Columns["Total Enviado"] != null)  { dgvDatos.Columns["Total Enviado"].FillWeight = 13; dgvDatos.Columns["Total Enviado"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter; }
            if (dgvDatos.Columns["Total Devuelto"] != null) { dgvDatos.Columns["Total Devuelto"].FillWeight = 13; dgvDatos.Columns["Total Devuelto"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter; }
            if (dgvDatos.Columns["Neto en Obra"] != null)   { dgvDatos.Columns["Neto en Obra"].FillWeight = 13; dgvDatos.Columns["Neto en Obra"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter; dgvDatos.Columns["Neto en Obra"].DefaultCellStyle.Font = new Font("Bahnschrift", 11, FontStyle.Bold); }
            ConfigurarColumnaMoneda("Precio Unit.", 13);
            ConfigurarColumnaMoneda("Valor Neto", 14);

            if (mostrarValorizacionActual && dgvDatos.Rows.Count > 0)
            {
                DataGridViewRow filaTotal = dgvDatos.Rows[dgvDatos.Rows.Count - 1];
                filaTotal.DefaultCellStyle.Font = new Font("Bahnschrift", 11, FontStyle.Bold);
                filaTotal.DefaultCellStyle.BackColor = Color.FromArgb(35, 63, 43);
                filaTotal.DefaultCellStyle.ForeColor = Color.White;
                filaTotal.DefaultCellStyle.SelectionBackColor = Color.FromArgb(35, 63, 43);
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

            var comprasEventualesMostradas = new HashSet<int>();
            var comprasEventualesConPrecioIndividual = new HashSet<int>(
                eventuales.Where(m => m.es_compra_eventual && m.precio_unitario > 0).Select(m => m.id_movimiento));
            foreach (var m in eventuales)
            {
                var valores = new List<object>
                {
                    m.material,
                    m.tipo_movimiento,
                    m.cantidad,
                    m.fecha.ToString("dd/MM/yyyy")
                };

                if (mostrarValorizacionActual)
                {
                    bool esCompraEventual = (m.tipo_movimiento ?? "").IndexOf("COMPRA EVENTUAL", StringComparison.OrdinalIgnoreCase) >= 0;
                    decimal totalFila = m.cantidad * m.precio_unitario;
                    if (esCompraEventual && m.precio_total > 0 && !comprasEventualesConPrecioIndividual.Contains(m.id_movimiento))
                        totalFila = comprasEventualesMostradas.Add(m.id_movimiento) ? m.precio_total : 0;

                    valores.Add(m.precio_unitario);
                    valores.Add(totalFila);
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
                dgvDatos.Columns["Material / Descripción"].DefaultCellStyle.Padding = new Padding(10, 0, 0, 0);
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
                .OrderByDescending(c => c.fecha_creacion)
                .ThenByDescending(c => c.id)
                .ToList();
            var conteosFotos = canaletaFotoDatos.ObtenerConteosPorCanaletas(ordenadas.Select(c => c.id));

            foreach (var item in ordenadas)
            {
                int cantidadFotos = conteosFotos.ContainsKey(item.id) ? conteosFotos[item.id] : 0;
                string textoFotos = cantidadFotos == 0 ? "Sin fotos" : "Ver (" + cantidadFotos + ")";

                if (mostrarValorizacionActual)
                    dt.Rows.Add(item.id, item.fecha_creacion.ToString("dd/MM/yyyy"), item.descripcion, item.pagado, textoFotos, item.precio);
                else
                    dt.Rows.Add(item.id, item.fecha_creacion.ToString("dd/MM/yyyy"), item.descripcion, item.pagado, textoFotos);
            }

            if (mostrarValorizacionActual && dt.Rows.Count > 0)
            {
                dt.Rows.Add(
                    0,
                    "",
                    "TOTAL CANALETAS / PLEGADOS",
                    false,
                    "",
                    canaletasPlegadosActuales.Where(c => !c.pagado).Sum(c => c.precio));
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
                dgvDatos.Columns["Descripción"].DefaultCellStyle.Padding = new Padding(10, 0, 0, 0);
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
                filaTotal.DefaultCellStyle.Font = new Font("Bahnschrift", 11, FontStyle.Bold);
                filaTotal.DefaultCellStyle.BackColor = Color.FromArgb(35, 63, 43);
                filaTotal.DefaultCellStyle.ForeColor = Color.White;
                filaTotal.DefaultCellStyle.SelectionBackColor = Color.FromArgb(35, 63, 43);
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

        private void MostrarFotosCanaleta(int idCanaleta)
        {
            try
            {
                var fotos = canaletaFotoDatos.ObtenerPorCanaleta(idCanaleta);
                if (fotos.Count == 0)
                {
                    ToastNotification.ShowWarning("Esta canaleta/plegado no tiene fotos cargadas.");
                    return;
                }

                using (var visor = new VisorFotosCanaletaForm(fotos))
                    visor.ShowDialog(this);
            }
            catch (Exception ex)
            {
                ToastNotification.ShowError("No se pudieron cargar las fotos:\n" + ex.Message);
            }
        }

        // ======================== PDF ========================

        private void btnDescargar_Click(object sender, EventArgs e)
        {
            if (cmbObras.SelectedIndex == -1)
            {
                ToastNotification.ShowWarning("Seleccione una obra.");
                return;
            }

            int idObra = Convert.ToInt32(cmbObras.SelectedValue);
            DataRowView drv = (DataRowView)cmbObras.SelectedItem;
            string nombreObra = drv["nombre"].ToString();

            using (var modal = new ModalFechasInformeObra(idObra))
            {
                if (modal.ShowDialog() != DialogResult.OK)
                    return;

                try
                {
                    lblEstado.ForeColor = Color.FromArgb(25, 135, 84);
                    lblEstado.Text = "Generando...";
                    Application.DoEvents();

                    string tempPath = Path.Combine(
                        Path.GetTempPath(),
                        $"InformeObra_{nombreObra.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

                    InformeObraPDF generador = new InformeObraPDF();
                    generador.Generar(tempPath, idObra, nombreObra, modal.FechaDesde, modal.FechaHasta);

                    lblEstado.ForeColor = Color.FromArgb(25, 135, 84);
                    lblEstado.Text = "✓ PDF generado";

                    Process.Start(new ProcessStartInfo(tempPath) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    lblEstado.ForeColor = Color.FromArgb(220, 53, 69);
                    lblEstado.Text = "Error al generar";
                    ToastNotification.ShowError("Error al generar el informe:\n" + ex.Message);
                }
            }
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
            bool habilitado = tabActual == "herramientas"
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

        private void itemEnviarAGalpon_Click(object sender, EventArgs e)
        {
            if (dgvDatos.SelectedRows.Count == 0 || dgvDatos.Columns["IdHerramienta"] == null)
                return;

            var row = dgvDatos.SelectedRows[0];
            if (!(row.Cells["IdHerramienta"]?.Value is int idHerramienta))
                return;

            string nombreHerramienta = row.Cells["Herramienta"]?.Value?.ToString() ?? "la herramienta seleccionada";
            DialogResult confirmacion = MessageBox.Show(
                $"¿Enviar \"{nombreHerramienta}\" al galpón?",
                "Confirmar retorno",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirmacion != DialogResult.Yes)
                return;

            try
            {
                new HerramientaDatos().EnviarAGalpon(idHerramienta);
                CargarDatosObra();
                ToastNotification.ShowSuccess("La herramienta fue enviada al galpón.");
            }
            catch (Exception ex)
            {
                ToastNotification.ShowError("No se pudo enviar la herramienta al galpón:\n" + ex.Message);
            }
        }

        private class VisorFotosCanaletaForm : Form
        {
            private readonly List<CanaletaPlegadoFoto> fotos;
            private readonly ListBox lstFotos = new ListBox();
            private readonly PictureBox picFoto = new PictureBox();
            private readonly Label lblEstado = new Label();
            private readonly Button btnCerrar = new Button();

            public VisorFotosCanaletaForm(List<CanaletaPlegadoFoto> fotos)
            {
                this.fotos = fotos ?? new List<CanaletaPlegadoFoto>();
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
                lstFotos.DisplayMember = nameof(CanaletaPlegadoFoto.nombre_archivo);
                lstFotos.SelectedIndexChanged += lstFotos_SelectedIndexChanged;

                picFoto.Dock = DockStyle.Fill;
                picFoto.BackColor = Color.FromArgb(20, 30, 25);
                picFoto.BorderStyle = BorderStyle.FixedSingle;
                picFoto.SizeMode = PictureBoxSizeMode.Zoom;

                btnCerrar.Dock = DockStyle.Bottom;
                btnCerrar.Height = 38;
                btnCerrar.Text = "Cerrar";
                btnCerrar.BackColor = Color.FromArgb(35, 63, 43);
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
                var foto = lstFotos.SelectedItem as CanaletaPlegadoFoto;
                if (foto == null)
                    return;

                await MostrarFotoAsync(foto);
            }

            private async Task MostrarFotoAsync(CanaletaPlegadoFoto foto)
            {
                if (!SupabaseStorageService.TokenDisponible())
                {
                    lblEstado.Text = "No hay sesión activa para ver fotos. Reinicie sesión.";
                    return;
                }

                try
                {
                    lblEstado.Text = "Cargando " + (foto.nombre_archivo ?? "foto") + "...";
                    picFoto.Image?.Dispose();
                    picFoto.Image = null;

                    byte[] bytes = await SupabaseStorageService.DescargarAsync(foto.object_path);
                    using (var ms = new MemoryStream(bytes))
                    using (var imagen = Image.FromStream(ms))
                    {
                        picFoto.Image = new Bitmap(imagen);
                    }

                    lblEstado.Text = (foto.nombre_archivo ?? "foto") + " - " + FormatearTamanio(foto.tamanio_bytes);
                }
                catch (Exception ex)
                {
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

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                    picFoto.Image?.Dispose();

                base.Dispose(disposing);
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }
    }
}
