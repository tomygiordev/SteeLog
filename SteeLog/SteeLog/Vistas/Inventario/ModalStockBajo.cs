using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Threading;
using SteeLog.Core.Application.Inventario;
using SteeLog.Presentation;
using SteeLog.CompositionRoot;
using SteeLog.Servicios;
using SteeLog.UI;

namespace SteeLog.Vistas.Inventario
{
    public class ModalStockBajo : Form
    {
        private readonly MaterialesPresenter _presenter = new MaterialesPresenter(LocalServices.Current.Executor);
        private readonly CancellationTokenSource _cierre = new CancellationTokenSource();
        private Guna.UI2.WinForms.Guna2DataGridView dgvMateriales;
        private Guna.UI2.WinForms.Guna2Button btnCerrar;
        private Label lblTitulo;
        private Guna.UI2.WinForms.Guna2Panel panelFondo;

        public ModalStockBajo()
        {
            InitializeComponent();
            ConfigurarFormulario();
            Shown += (s, e) => CargarDatos();
            FormClosed += (s, e) => _cierre.Cancel();
            Disposed += (s, e) => _cierre.Cancel();
        }

        private void InitializeComponent()
        {
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.dgvMateriales = new Guna.UI2.WinForms.Guna2DataGridView();
            this.btnCerrar = new Guna.UI2.WinForms.Guna2Button();
            this.lblTitulo = new System.Windows.Forms.Label();
            this.panelFondo = new Guna.UI2.WinForms.Guna2Panel();

            ((System.ComponentModel.ISupportInitialize)(this.dgvMateriales)).BeginInit();
            this.panelFondo.SuspendLayout();
            this.SuspendLayout();

            // lblTitulo (subtítulo; el título va en la barra del modal)
            this.lblTitulo.AutoSize = false;
            this.lblTitulo.Font = Tema.FuenteTexto;
            this.lblTitulo.ForeColor = Tema.TextoSecundario;
            this.lblTitulo.BackColor = Color.Transparent;
            this.lblTitulo.Location = new System.Drawing.Point(24, 16);
            this.lblTitulo.Name = "lblTitulo";
            this.lblTitulo.Size = new System.Drawing.Size(672, 22);
            this.lblTitulo.TextAlign = ContentAlignment.MiddleLeft;
            this.lblTitulo.Text = "Materiales que alcanzaron o quedaron por debajo del stock mínimo.";

            // dgvMateriales
            this.dgvMateriales.AllowUserToAddRows = false;
            this.dgvMateriales.AllowUserToDeleteRows = false;
            this.dgvMateriales.AllowUserToResizeRows = false;
            this.dgvMateriales.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvMateriales.RowHeadersVisible = false;
            this.dgvMateriales.ReadOnly = true;
            this.dgvMateriales.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            this.dgvMateriales.MultiSelect = false;
            this.dgvMateriales.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            this.dgvMateriales.Location = new System.Drawing.Point(24, 48);
            this.dgvMateriales.Size = new System.Drawing.Size(672, 344);
            this.dgvMateriales.Name = "dgvMateriales";
            Tema.Grilla(this.dgvMateriales);

            // btnCerrar
            this.btnCerrar.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            this.btnCerrar.Location = new System.Drawing.Point(576, 408);
            this.btnCerrar.Size = new System.Drawing.Size(120, Tema.AltoControl);
            this.btnCerrar.Text = "Cerrar";
            Tema.Boton(this.btnCerrar, Variante.Secundario);
            this.btnCerrar.Click += new System.EventHandler(this.btnCerrar_Click);

            // panelFondo
            this.panelFondo.BorderThickness = 0;
            this.panelFondo.FillColor = Tema.Fondo;
            this.panelFondo.Controls.Add(this.lblTitulo);
            this.panelFondo.Controls.Add(this.dgvMateriales);
            this.panelFondo.Controls.Add(this.btnCerrar);
            this.panelFondo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelFondo.Location = new System.Drawing.Point(0, 0);

            // Form
            this.BackColor = Tema.Fondo;
            this.ClientSize = new System.Drawing.Size(720, 460);
            this.Controls.Add(this.panelFondo);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Bajo stock";

            ((System.ComponentModel.ISupportInitialize)(this.dgvMateriales)).EndInit();
            this.panelFondo.ResumeLayout(false);
            this.ResumeLayout(false);

            Tema.ChromeModal(this, "Materiales con bajo stock");
            this.panelFondo.BringToFront();
        }

        private void ConfigurarFormulario()
        {
            // Opcional: sombrear el fondo y permitir drag
        }

        private async void CargarDatos()
        {
            try
            {
                var bajoStock = await _presenter.ListarAsync(new FiltroMaterialLectura { Priorizados = true }, _cierre.Token);
                if (_cierre.IsCancellationRequested || IsDisposed) return;
                bool ocultarCantidades = SessionManager.DebeOcultarCantidadesStockMateriales;

                var source = bajoStock.Select(m => new
                {
                    Codigo = m.Id,
                    Material = m.Nombre,
                    Categoria = m.Categoria,
                    StockActual = ocultarCantidades ? SessionManager.ObtenerEstadoStockMaterial(m.Stock) : m.Stock.ToString("N0"),
                    StockMinimo = ocultarCantidades ? string.Empty : m.Minimo.ToString("N0"),
                    Movimientos = m.CantidadMovimientos
                }).ToList();

                dgvMateriales.DataSource = source;
                
                if (dgvMateriales.Columns.Count > 0)
                {
                    dgvMateriales.Columns["Codigo"].HeaderText = "Código";
                    dgvMateriales.Columns["Codigo"].FillWeight = 12;
                    dgvMateriales.Columns["Categoria"].HeaderText = "Categoría";
                    dgvMateriales.Columns["Material"].FillWeight = 40;
                    foreach (var nombreCol in new[] { "StockActual", "StockMinimo", "Movimientos" })
                        dgvMateriales.Columns[nombreCol].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    dgvMateriales.Columns["StockActual"].HeaderText = ocultarCantidades ? "Disponibilidad" : "Stock Actual";
                    dgvMateriales.Columns["StockActual"].DefaultCellStyle.ForeColor = Color.FromArgb(240, 110, 100);
                    dgvMateriales.Columns["StockMinimo"].Visible = !ocultarCantidades;
                    dgvMateriales.Columns["StockMinimo"].HeaderText = "Stock Mínimo";
                    dgvMateriales.Columns["StockMinimo"].DefaultCellStyle.ForeColor = Tema.Advertencia;
                    dgvMateriales.Columns["Movimientos"].HeaderText = "Usos";
                    dgvMateriales.Columns["Movimientos"].FillWeight = 10;
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { if (!_cierre.IsCancellationRequested) ToastNotification.ShowError("Error cargando vista de bajo stock: " + ex.Message); }
        }

        private void btnCerrar_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        // Drag Form
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }
    }
}
