using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WP_Inventario.Datos;
using WP_Inventario.Servicios;

namespace WP_Inventario.Vistas.Inventario
{
    public class ModalStockBajo : Form
    {
        private Guna.UI2.WinForms.Guna2DataGridView dgvMateriales;
        private Guna.UI2.WinForms.Guna2Button btnCerrar;
        private Label lblTitulo;
        private Guna.UI2.WinForms.Guna2Panel panelFondo;

        public ModalStockBajo()
        {
            InitializeComponent();
            ConfigurarFormulario();
            CargarDatos();
        }

        private void InitializeComponent()
        {
            this.dgvMateriales = new Guna.UI2.WinForms.Guna2DataGridView();
            this.btnCerrar = new Guna.UI2.WinForms.Guna2Button();
            this.lblTitulo = new System.Windows.Forms.Label();
            this.panelFondo = new Guna.UI2.WinForms.Guna2Panel();
            
            ((System.ComponentModel.ISupportInitialize)(this.dgvMateriales)).BeginInit();
            this.panelFondo.SuspendLayout();
            this.SuspendLayout();
            
            // lblTitulo
            this.lblTitulo.AutoSize = true;
            this.lblTitulo.Font = new System.Drawing.Font("Bahnschrift SemiBold", 16F, System.Drawing.FontStyle.Bold);
            this.lblTitulo.ForeColor = System.Drawing.Color.FromArgb(230, 230, 230);
            this.lblTitulo.Location = new System.Drawing.Point(20, 20);
            this.lblTitulo.Name = "lblTitulo";
            this.lblTitulo.Size = new System.Drawing.Size(296, 27);
            this.lblTitulo.Text = "Materiales con Bajo Stock";
            
            // dgvMateriales
            this.dgvMateriales.AllowUserToAddRows = false;
            this.dgvMateriales.AllowUserToDeleteRows = false;
            this.dgvMateriales.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvMateriales.BackgroundColor = System.Drawing.Color.FromArgb(30, 30, 35);
            this.dgvMateriales.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dgvMateriales.ColumnHeadersHeight = 35;
            this.dgvMateriales.ThemeStyle.HeaderStyle.BackColor = System.Drawing.Color.FromArgb(47, 84, 57);
            this.dgvMateriales.ThemeStyle.HeaderStyle.ForeColor = System.Drawing.Color.White;
            this.dgvMateriales.ThemeStyle.RowsStyle.BackColor = System.Drawing.Color.FromArgb(40, 45, 40);
            this.dgvMateriales.ThemeStyle.RowsStyle.ForeColor = System.Drawing.Color.White;
            this.dgvMateriales.ThemeStyle.RowsStyle.SelectionBackColor = System.Drawing.Color.FromArgb(60, 90, 70);
            this.dgvMateriales.ThemeStyle.RowsStyle.SelectionForeColor = System.Drawing.Color.White;
            this.dgvMateriales.ThemeStyle.AlternatingRowsStyle.BackColor = System.Drawing.Color.FromArgb(35, 40, 35);
            this.dgvMateriales.RowHeadersVisible = false;
            this.dgvMateriales.ReadOnly = true;
            this.dgvMateriales.Location = new System.Drawing.Point(20, 70);
            this.dgvMateriales.Size = new System.Drawing.Size(660, 320);
            this.dgvMateriales.Name = "dgvMateriales";
            
            // btnCerrar
            this.btnCerrar.BorderRadius = 5;
            this.btnCerrar.FillColor = System.Drawing.Color.FromArgb(192, 57, 43);
            this.btnCerrar.Font = new System.Drawing.Font("Bahnschrift", 11.25F);
            this.btnCerrar.ForeColor = System.Drawing.Color.White;
            this.btnCerrar.Location = new System.Drawing.Point(560, 410);
            this.btnCerrar.Size = new System.Drawing.Size(120, 40);
            this.btnCerrar.Text = "Cerrar";
            this.btnCerrar.Click += new System.EventHandler(this.btnCerrar_Click);
            
            // panelFondo
            this.panelFondo.BorderColor = System.Drawing.Color.FromArgb(80, 160, 90);
            this.panelFondo.BorderThickness = 2;
            this.panelFondo.Controls.Add(this.lblTitulo);
            this.panelFondo.Controls.Add(this.dgvMateriales);
            this.panelFondo.Controls.Add(this.btnCerrar);
            this.panelFondo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelFondo.Location = new System.Drawing.Point(0, 0);
            
            // Form
            this.BackColor = System.Drawing.Color.FromArgb(20, 20, 25);
            this.ClientSize = new System.Drawing.Size(700, 470);
            this.Controls.Add(this.panelFondo);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Bajo Stock";
            
            ((System.ComponentModel.ISupportInitialize)(this.dgvMateriales)).EndInit();
            this.panelFondo.ResumeLayout(false);
            this.panelFondo.PerformLayout();
            this.ResumeLayout(false);
        }

        private void ConfigurarFormulario()
        {
            // Opcional: sombrear el fondo y permitir drag
        }

        private void CargarDatos()
        {
            try
            {
                var bajoStock = new MaterialDatos().ObtenerMaterialesStockBajoPriorizados();
                bool ocultarCantidades = SessionManager.DebeOcultarCantidadesStockMateriales;

                var source = bajoStock.Select(m => new
                {
                    Codigo = m.id,
                    Material = m.nombre,
                    Categoria = m.nombre_categoria,
                    StockActual = ocultarCantidades ? SessionManager.ObtenerEstadoStockMaterial(m.stockActual) : m.stockActual.ToString("N0"),
                    StockMinimo = ocultarCantidades ? string.Empty : m.stockMinimo.ToString("N0"),
                    Movimientos = m.cantidadMovimientos
                }).ToList();

                dgvMateriales.DataSource = source;
                
                if (dgvMateriales.Columns.Count > 0)
                {
                    dgvMateriales.Columns["Codigo"].Width = 60;
                    dgvMateriales.Columns["StockActual"].HeaderText = ocultarCantidades ? "Disponibilidad" : "Stock Actual";
                    dgvMateriales.Columns["StockActual"].DefaultCellStyle.ForeColor = Color.FromArgb(255, 100, 100);
                    dgvMateriales.Columns["StockMinimo"].Visible = !ocultarCantidades;
                    dgvMateriales.Columns["StockMinimo"].HeaderText = "Stock Mínimo";
                    dgvMateriales.Columns["StockMinimo"].DefaultCellStyle.ForeColor = Color.FromArgb(255, 200, 100);
                    dgvMateriales.Columns["Movimientos"].HeaderText = "Usos";
                    dgvMateriales.Columns["Movimientos"].Width = 70;
                }
            }
            catch (Exception ex)
            {
                ToastNotification.ShowError("Error cargando vista de bajo stock: " + ex.Message);
            }
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
