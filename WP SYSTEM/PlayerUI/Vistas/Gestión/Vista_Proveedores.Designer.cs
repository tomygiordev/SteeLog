namespace WP_Inventario.Vistas
{
    partial class Vista_Proveedores
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();

            this.panelAntiParpadeo1 = new WP_Inventario.PanelAntiParpadeo();
            this.label1 = new System.Windows.Forms.Label();

            this.panelAntiParpadeo2 = new WP_Inventario.PanelAntiParpadeo();
            this.label2 = new System.Windows.Forms.Label();
            this.txtProveedor = new Guna.UI2.WinForms.Guna2TextBox();
            this.guna2Button1 = new Guna.UI2.WinForms.Guna2Button();
            this.eliminarProveedorBoton = new Guna.UI2.WinForms.Guna2Button();

            this.panelAntiParpadeo4 = new WP_Inventario.PanelAntiParpadeo();
            this.label3 = new System.Windows.Forms.Label();

            this.guna2Panel1 = new Guna.UI2.WinForms.Guna2Panel();
            this.guna2VScrollBar1 = new Guna.UI2.WinForms.Guna2VScrollBar();
            this.dgvProveedores = new Guna.UI2.WinForms.Guna2DataGridView();

            this.panelAntiParpadeo1.SuspendLayout();
            this.panelAntiParpadeo2.SuspendLayout();
            this.panelAntiParpadeo4.SuspendLayout();
            this.guna2Panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvProveedores)).BeginInit();
            this.SuspendLayout();

            // -------------------------------------------------------
            // panelAntiParpadeo1 — Encabezado
            // -------------------------------------------------------
            this.panelAntiParpadeo1.BackColor = System.Drawing.Color.FromArgb(32, 55, 40);
            this.panelAntiParpadeo1.ColumnCount = 1;
            this.panelAntiParpadeo1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.panelAntiParpadeo1.Controls.Add(this.label1, 0, 0);
            this.panelAntiParpadeo1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelAntiParpadeo1.GrowStyle = System.Windows.Forms.TableLayoutPanelGrowStyle.AddColumns;
            this.panelAntiParpadeo1.Location = new System.Drawing.Point(0, 0);
            this.panelAntiParpadeo1.Name = "panelAntiParpadeo1";
            this.panelAntiParpadeo1.Padding = new System.Windows.Forms.Padding(14, 0, 14, 0);
            this.panelAntiParpadeo1.RowCount = 1;
            this.panelAntiParpadeo1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.panelAntiParpadeo1.Size = new System.Drawing.Size(800, 42);
            this.panelAntiParpadeo1.TabIndex = 22;

            // label1
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right));
            this.label1.AutoSize = false;
            this.label1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label1.Font = new System.Drawing.Font("Bahnschrift SemiBold", 13F, System.Drawing.FontStyle.Bold);
            this.label1.ForeColor = System.Drawing.Color.FromArgb(210, 235, 218);
            this.label1.Location = new System.Drawing.Point(14, 0);
            this.label1.Name = "label1";
            this.label1.TabIndex = 3;
            this.label1.Text = "PROVEEDORES";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // -------------------------------------------------------
            // panelAntiParpadeo2 — Formulario (fila única)
            // Col0: label 90px | Col1: input 60% | Col2: Guardar 20% | Col3: Eliminar 20%
            // -------------------------------------------------------
            this.panelAntiParpadeo2.ColumnCount = 4;
            this.panelAntiParpadeo2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 90F));
            this.panelAntiParpadeo2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 60F));
            this.panelAntiParpadeo2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.panelAntiParpadeo2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.panelAntiParpadeo2.Controls.Add(this.label2, 0, 0);
            this.panelAntiParpadeo2.Controls.Add(this.txtProveedor, 1, 0);
            this.panelAntiParpadeo2.Controls.Add(this.guna2Button1, 2, 0);
            this.panelAntiParpadeo2.Controls.Add(this.eliminarProveedorBoton, 3, 0);
            this.panelAntiParpadeo2.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelAntiParpadeo2.GrowStyle = System.Windows.Forms.TableLayoutPanelGrowStyle.AddColumns;
            this.panelAntiParpadeo2.Location = new System.Drawing.Point(0, 42);
            this.panelAntiParpadeo2.Name = "panelAntiParpadeo2";
            this.panelAntiParpadeo2.Padding = new System.Windows.Forms.Padding(0, 8, 4, 8);
            this.panelAntiParpadeo2.RowCount = 1;
            this.panelAntiParpadeo2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.panelAntiParpadeo2.Size = new System.Drawing.Size(800, 52);
            this.panelAntiParpadeo2.TabIndex = 23;

            // label2 — "Nombre:"
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right));
            this.label2.AutoSize = false;
            this.label2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label2.Font = new System.Drawing.Font("Bahnschrift Light", 10.5F);
            this.label2.ForeColor = System.Drawing.Color.FromArgb(155, 185, 165);
            this.label2.Name = "label2";
            this.label2.Padding = new System.Windows.Forms.Padding(14, 0, 6, 0);
            this.label2.TabIndex = 0;
            this.label2.Text = "Nombre:";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // txtProveedor
            this.txtProveedor.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtProveedor.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.txtProveedor.BorderColor = System.Drawing.Color.FromArgb(55, 75, 62);
            this.txtProveedor.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txtProveedor.DefaultText = "";
            this.txtProveedor.DisabledState.BorderColor = System.Drawing.Color.FromArgb(60, 60, 60);
            this.txtProveedor.DisabledState.FillColor = System.Drawing.Color.FromArgb(40, 55, 44);
            this.txtProveedor.DisabledState.ForeColor = System.Drawing.Color.FromArgb(100, 100, 100);
            this.txtProveedor.DisabledState.PlaceholderForeColor = System.Drawing.Color.FromArgb(100, 100, 100);
            this.txtProveedor.FillColor = System.Drawing.Color.FromArgb(35, 50, 40);
            this.txtProveedor.FocusedState.BorderColor = System.Drawing.Color.FromArgb(39, 174, 96);
            this.txtProveedor.Font = new System.Drawing.Font("Segoe UI", 10.5F);
            this.txtProveedor.ForeColor = System.Drawing.Color.FromArgb(225, 225, 225);
            this.txtProveedor.HoverState.BorderColor = System.Drawing.Color.FromArgb(60, 120, 80);
            this.txtProveedor.Location = new System.Drawing.Point(93, 10);
            this.txtProveedor.Margin = new System.Windows.Forms.Padding(3, 2, 8, 2);
            this.txtProveedor.Name = "txtProveedor";
            this.txtProveedor.PlaceholderText = "Ej: Corralón Alem";
            this.txtProveedor.SelectedText = "";
            this.txtProveedor.Size = new System.Drawing.Size(300, 30);
            this.txtProveedor.TabIndex = 1;
            this.txtProveedor.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtProveedor_KeyDown);

            // guna2Button1 — "Guardar" (primario)
            this.guna2Button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.guna2Button1.BorderRadius = 0;
            this.guna2Button1.DisabledState.BorderColor = System.Drawing.Color.FromArgb(50, 65, 55);
            this.guna2Button1.DisabledState.CustomBorderColor = System.Drawing.Color.FromArgb(50, 65, 55);
            this.guna2Button1.DisabledState.FillColor = System.Drawing.Color.FromArgb(45, 60, 50);
            this.guna2Button1.DisabledState.ForeColor = System.Drawing.Color.FromArgb(100, 120, 108);
            this.guna2Button1.FillColor = System.Drawing.Color.FromArgb(39, 174, 96);
            this.guna2Button1.Font = new System.Drawing.Font("Bahnschrift Light", 11F);
            this.guna2Button1.ForeColor = System.Drawing.Color.White;
            this.guna2Button1.HoverState.FillColor = System.Drawing.Color.FromArgb(46, 196, 110);
            this.guna2Button1.Location = new System.Drawing.Point(254, 9);
            this.guna2Button1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.guna2Button1.Name = "guna2Button1";
            this.guna2Button1.Size = new System.Drawing.Size(130, 34);
            this.guna2Button1.TabIndex = 2;
            this.guna2Button1.Text = "Guardar";
            this.guna2Button1.Click += new System.EventHandler(this.guna2Button1_Click);

            // eliminarProveedorBoton — "Eliminar" (destructivo)
            this.eliminarProveedorBoton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.eliminarProveedorBoton.BorderRadius = 0;
            this.eliminarProveedorBoton.DisabledState.BorderColor = System.Drawing.Color.FromArgb(50, 50, 50);
            this.eliminarProveedorBoton.DisabledState.CustomBorderColor = System.Drawing.Color.FromArgb(50, 50, 50);
            this.eliminarProveedorBoton.DisabledState.FillColor = System.Drawing.Color.FromArgb(40, 40, 40);
            this.eliminarProveedorBoton.DisabledState.ForeColor = System.Drawing.Color.FromArgb(80, 80, 80);
            this.eliminarProveedorBoton.FillColor = System.Drawing.Color.FromArgb(158, 45, 40);
            this.eliminarProveedorBoton.Font = new System.Drawing.Font("Bahnschrift Light", 11F);
            this.eliminarProveedorBoton.ForeColor = System.Drawing.Color.White;
            this.eliminarProveedorBoton.HoverState.FillColor = System.Drawing.Color.FromArgb(180, 55, 48);
            this.eliminarProveedorBoton.Location = new System.Drawing.Point(392, 9);
            this.eliminarProveedorBoton.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.eliminarProveedorBoton.Name = "eliminarProveedorBoton";
            this.eliminarProveedorBoton.Size = new System.Drawing.Size(130, 34);
            this.eliminarProveedorBoton.TabIndex = 3;
            this.eliminarProveedorBoton.Text = "Eliminar";
            this.eliminarProveedorBoton.Click += new System.EventHandler(this.eliminarProveedorBoton_Click);

            // -------------------------------------------------------
            // panelAntiParpadeo4 — Encabezado del listado
            // -------------------------------------------------------
            this.panelAntiParpadeo4.BackColor = System.Drawing.Color.FromArgb(26, 34, 30);
            this.panelAntiParpadeo4.ColumnCount = 1;
            this.panelAntiParpadeo4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.panelAntiParpadeo4.Controls.Add(this.label3, 0, 0);
            this.panelAntiParpadeo4.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelAntiParpadeo4.GrowStyle = System.Windows.Forms.TableLayoutPanelGrowStyle.AddColumns;
            this.panelAntiParpadeo4.Location = new System.Drawing.Point(0, 94);
            this.panelAntiParpadeo4.Margin = new System.Windows.Forms.Padding(0);
            this.panelAntiParpadeo4.Name = "panelAntiParpadeo4";
            this.panelAntiParpadeo4.Padding = new System.Windows.Forms.Padding(14, 0, 14, 0);
            this.panelAntiParpadeo4.RowCount = 1;
            this.panelAntiParpadeo4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.panelAntiParpadeo4.Size = new System.Drawing.Size(800, 36);
            this.panelAntiParpadeo4.TabIndex = 26;

            // label3
            this.label3.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Bahnschrift SemiBold", 10F, System.Drawing.FontStyle.Bold);
            this.label3.ForeColor = System.Drawing.Color.FromArgb(145, 175, 158);
            this.label3.Location = new System.Drawing.Point(17, 9);
            this.label3.Name = "label3";
            this.label3.TabIndex = 3;
            this.label3.Text = "LISTADO DE PROVEEDORES";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // -------------------------------------------------------
            // guna2Panel1 — Contenedor del DataGridView
            // -------------------------------------------------------
            this.guna2Panel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.guna2Panel1.BorderColor = System.Drawing.Color.FromArgb(38, 52, 44);
            this.guna2Panel1.BorderRadius = 0;
            this.guna2Panel1.BorderThickness = 0;
            this.guna2Panel1.Controls.Add(this.guna2VScrollBar1);
            this.guna2Panel1.Controls.Add(this.dgvProveedores);
            this.guna2Panel1.Location = new System.Drawing.Point(0, 130);
            this.guna2Panel1.Name = "guna2Panel1";
            this.guna2Panel1.Padding = new System.Windows.Forms.Padding(0);
            this.guna2Panel1.Size = new System.Drawing.Size(800, 320);
            this.guna2Panel1.TabIndex = 25;

            // guna2VScrollBar1
            this.guna2VScrollBar1.BindingContainer = this.dgvProveedores;
            this.guna2VScrollBar1.Dock = System.Windows.Forms.DockStyle.Right;
            this.guna2VScrollBar1.FillColor = System.Drawing.Color.FromArgb(35, 50, 40);
            this.guna2VScrollBar1.InUpdate = false;
            this.guna2VScrollBar1.LargeChange = 10;
            this.guna2VScrollBar1.Location = new System.Drawing.Point(784, 0);
            this.guna2VScrollBar1.Minimum = 1;
            this.guna2VScrollBar1.Name = "guna2VScrollBar1";
            this.guna2VScrollBar1.ScrollbarSize = 16;
            this.guna2VScrollBar1.Size = new System.Drawing.Size(16, 320);
            this.guna2VScrollBar1.TabIndex = 21;
            this.guna2VScrollBar1.ThumbColor = System.Drawing.Color.FromArgb(55, 85, 65);
            this.guna2VScrollBar1.Value = 1;

            // -------------------------------------------------------
            // dgvProveedores
            // -------------------------------------------------------
            this.dgvProveedores.AllowUserToAddRows = false;
            this.dgvProveedores.AllowUserToDeleteRows = false;
            this.dgvProveedores.AllowUserToResizeColumns = false;
            this.dgvProveedores.AllowUserToResizeRows = false;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(29, 38, 35);
            this.dgvProveedores.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
            this.dgvProveedores.BackgroundColor = System.Drawing.Color.FromArgb(29, 38, 35);
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.FromArgb(32, 55, 40);
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Bahnschrift Light", 10.5F);
            dataGridViewCellStyle2.ForeColor = System.Drawing.Color.FromArgb(195, 215, 202);
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.Color.FromArgb(32, 55, 40);
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.Color.FromArgb(195, 215, 202);
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvProveedores.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
            this.dgvProveedores.ColumnHeadersHeight = 32;
            this.dgvProveedores.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = System.Drawing.Color.FromArgb(29, 38, 35);
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Bahnschrift Light", 11F);
            dataGridViewCellStyle3.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle3.Padding = new System.Windows.Forms.Padding(14, 0, 14, 0);
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.Color.FromArgb(47, 84, 57);
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvProveedores.DefaultCellStyle = dataGridViewCellStyle3;
            this.dgvProveedores.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvProveedores.GridColor = System.Drawing.Color.FromArgb(38, 50, 43);
            this.dgvProveedores.Location = new System.Drawing.Point(0, 0);
            this.dgvProveedores.Name = "dgvProveedores";
            this.dgvProveedores.ReadOnly = true;
            this.dgvProveedores.RowHeadersVisible = false;
            this.dgvProveedores.RowTemplate.DefaultCellStyle.Font = new System.Drawing.Font("Bahnschrift Light", 11F);
            this.dgvProveedores.RowTemplate.Height = 36;
            this.dgvProveedores.Size = new System.Drawing.Size(784, 320);
            this.dgvProveedores.TabIndex = 19;
            this.dgvProveedores.Theme = Guna.UI2.WinForms.Enums.DataGridViewPresetThemes.Dark;
            this.dgvProveedores.ThemeStyle.AlternatingRowsStyle.BackColor = System.Drawing.Color.FromArgb(29, 38, 35);
            this.dgvProveedores.ThemeStyle.AlternatingRowsStyle.Font = null;
            this.dgvProveedores.ThemeStyle.AlternatingRowsStyle.ForeColor = System.Drawing.Color.Empty;
            this.dgvProveedores.ThemeStyle.AlternatingRowsStyle.SelectionBackColor = System.Drawing.Color.Empty;
            this.dgvProveedores.ThemeStyle.AlternatingRowsStyle.SelectionForeColor = System.Drawing.Color.Empty;
            this.dgvProveedores.ThemeStyle.BackColor = System.Drawing.Color.FromArgb(29, 38, 35);
            this.dgvProveedores.ThemeStyle.GridColor = System.Drawing.Color.FromArgb(38, 50, 43);
            this.dgvProveedores.ThemeStyle.HeaderStyle.BackColor = System.Drawing.Color.FromArgb(32, 55, 40);
            this.dgvProveedores.ThemeStyle.HeaderStyle.BorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            this.dgvProveedores.ThemeStyle.HeaderStyle.Font = new System.Drawing.Font("Bahnschrift Light", 10.5F);
            this.dgvProveedores.ThemeStyle.HeaderStyle.ForeColor = System.Drawing.Color.FromArgb(195, 215, 202);
            this.dgvProveedores.ThemeStyle.HeaderStyle.HeaightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.dgvProveedores.ThemeStyle.HeaderStyle.Height = 32;
            this.dgvProveedores.ThemeStyle.ReadOnly = true;
            this.dgvProveedores.ThemeStyle.RowsStyle.BackColor = System.Drawing.Color.FromArgb(29, 38, 35);
            this.dgvProveedores.ThemeStyle.RowsStyle.BorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            this.dgvProveedores.ThemeStyle.RowsStyle.Font = new System.Drawing.Font("Bahnschrift Light", 11F);
            this.dgvProveedores.ThemeStyle.RowsStyle.ForeColor = System.Drawing.Color.White;
            this.dgvProveedores.ThemeStyle.RowsStyle.Height = 36;
            this.dgvProveedores.ThemeStyle.RowsStyle.SelectionBackColor = System.Drawing.Color.FromArgb(47, 84, 57);
            this.dgvProveedores.ThemeStyle.RowsStyle.SelectionForeColor = System.Drawing.Color.White;
            this.dgvProveedores.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvProveedores_CellClick);

            // -------------------------------------------------------
            // Vista_Proveedores — Form
            // -------------------------------------------------------
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(29, 38, 35);
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.guna2Panel1);
            this.Controls.Add(this.panelAntiParpadeo4);
            this.Controls.Add(this.panelAntiParpadeo2);
            this.Controls.Add(this.panelAntiParpadeo1);
            this.Name = "Vista_Proveedores";
            this.Text = "Gestión de Proveedores";

            this.panelAntiParpadeo1.ResumeLayout(false);
            this.panelAntiParpadeo2.ResumeLayout(false);
            this.panelAntiParpadeo2.PerformLayout();
            this.panelAntiParpadeo4.ResumeLayout(false);
            this.panelAntiParpadeo4.PerformLayout();
            this.guna2Panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvProveedores)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private PanelAntiParpadeo panelAntiParpadeo1;
        private System.Windows.Forms.Label label1;
        private PanelAntiParpadeo panelAntiParpadeo2;
        private System.Windows.Forms.Label label2;
        private Guna.UI2.WinForms.Guna2TextBox txtProveedor;
        private Guna.UI2.WinForms.Guna2Button guna2Button1;
        private Guna.UI2.WinForms.Guna2Button eliminarProveedorBoton;
        private PanelAntiParpadeo panelAntiParpadeo4;
        private System.Windows.Forms.Label label3;
        private Guna.UI2.WinForms.Guna2Panel guna2Panel1;
        private Guna.UI2.WinForms.Guna2VScrollBar guna2VScrollBar1;
        private Guna.UI2.WinForms.Guna2DataGridView dgvProveedores;
    }
}
