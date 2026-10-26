namespace WP_Inventario.Vistas.Reportes
{
    partial class Vista_InformeObra
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
            this.panelTitulo = new WP_Inventario.PanelAntiParpadeo();
            this.label1 = new System.Windows.Forms.Label();
            this.panelFiltros = new WP_Inventario.PanelAntiParpadeo();
            this.label2 = new System.Windows.Forms.Label();
            this.cmbObras = new Guna.UI2.WinForms.Guna2ComboBox();
            this.btnDescargar = new Guna.UI2.WinForms.Guna2Button();
            this.panelResumen = new WP_Inventario.PanelAntiParpadeo();
            this.lblRemitos = new System.Windows.Forms.Label();
            this.lblHerramientas = new System.Windows.Forms.Label();
            this.lblInsumos = new System.Windows.Forms.Label();
            this.lblEstado = new System.Windows.Forms.Label();
            this.panelTabs = new WP_Inventario.PanelAntiParpadeo();
            this.btnTabMovimientos = new Guna.UI2.WinForms.Guna2Button();
            this.btnTabHerramientas = new Guna.UI2.WinForms.Guna2Button();
            this.btnTabInsumos = new Guna.UI2.WinForms.Guna2Button();
            this.btnTabMateriales = new Guna.UI2.WinForms.Guna2Button();
            this.btnTabEventuales = new Guna.UI2.WinForms.Guna2Button();
            this.btnTabCanaletasPlegados = new Guna.UI2.WinForms.Guna2Button();
            this.dgvDatos = new Guna.UI2.WinForms.Guna2DataGridView();
            this.panelTitulo.SuspendLayout();
            this.panelFiltros.SuspendLayout();
            this.panelResumen.SuspendLayout();
            this.panelTabs.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvDatos)).BeginInit();
            this.SuspendLayout();
            // 
            // panelTitulo
            // 
            this.panelTitulo.ColumnCount = 1;
            this.panelTitulo.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.panelTitulo.Controls.Add(this.label1, 0, 0);
            this.panelTitulo.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTitulo.GrowStyle = System.Windows.Forms.TableLayoutPanelGrowStyle.AddColumns;
            this.panelTitulo.Location = new System.Drawing.Point(0, 0);
            this.panelTitulo.Name = "panelTitulo";
            this.panelTitulo.Padding = new System.Windows.Forms.Padding(40, 0, 40, 0);
            this.panelTitulo.RowCount = 1;
            this.panelTitulo.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.panelTitulo.Size = new System.Drawing.Size(900, 51);
            this.panelTitulo.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Bahnschrift SemiBold", 22F, System.Drawing.FontStyle.Bold);
            this.label1.ForeColor = System.Drawing.SystemColors.Control;
            this.label1.Location = new System.Drawing.Point(43, 7);
            this.label1.Name = "label1";
            this.label1.Padding = new System.Windows.Forms.Padding(40, 0, 40, 0);
            this.label1.Size = new System.Drawing.Size(814, 36);
            this.label1.TabIndex = 3;
            this.label1.Text = "INFORME DE OBRA";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.label1.Click += new System.EventHandler(this.label1_Click);
            // 
            // panelFiltros
            // 
            this.panelFiltros.ColumnCount = 3;
            this.panelFiltros.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.panelFiltros.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 60F));
            this.panelFiltros.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 40F));
            this.panelFiltros.Controls.Add(this.label2, 0, 0);
            this.panelFiltros.Controls.Add(this.cmbObras, 1, 0);
            this.panelFiltros.Controls.Add(this.btnDescargar, 2, 0);
            this.panelFiltros.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelFiltros.GrowStyle = System.Windows.Forms.TableLayoutPanelGrowStyle.AddColumns;
            this.panelFiltros.Location = new System.Drawing.Point(0, 51);
            this.panelFiltros.Name = "panelFiltros";
            this.panelFiltros.Padding = new System.Windows.Forms.Padding(40, 0, 40, 0);
            this.panelFiltros.RowCount = 1;
            this.panelFiltros.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.panelFiltros.Size = new System.Drawing.Size(900, 58);
            this.panelFiltros.TabIndex = 1;
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Bahnschrift Light", 16F);
            this.label2.ForeColor = System.Drawing.SystemColors.Control;
            this.label2.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.label2.Location = new System.Drawing.Point(43, 15);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(64, 27);
            this.label2.TabIndex = 0;
            this.label2.Text = "Obra:";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // cmbObras
            // 
            this.cmbObras.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbObras.BackColor = System.Drawing.Color.Transparent;
            this.cmbObras.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(65)))));
            this.cmbObras.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.cmbObras.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbObras.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(50)))), ((int)(((byte)(40)))));
            this.cmbObras.FocusedColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(135)))), ((int)(((byte)(84)))));
            this.cmbObras.FocusedState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(135)))), ((int)(((byte)(84)))));
            this.cmbObras.Font = new System.Drawing.Font("Segoe UI", 10.75F);
            this.cmbObras.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(230)))), ((int)(((byte)(230)))));
            this.cmbObras.HoverState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(135)))), ((int)(((byte)(84)))));
            this.cmbObras.ItemHeight = 30;
            this.cmbObras.Location = new System.Drawing.Point(113, 11);
            this.cmbObras.Name = "cmbObras";
            this.cmbObras.Size = new System.Drawing.Size(444, 36);
            this.cmbObras.TabIndex = 1;
            this.cmbObras.SelectedIndexChanged += new System.EventHandler(this.cmbObras_SelectedIndexChanged);
            // 
            // btnDescargar
            // 
            this.btnDescargar.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.btnDescargar.DisabledState.BorderColor = System.Drawing.Color.DarkGray;
            this.btnDescargar.DisabledState.CustomBorderColor = System.Drawing.Color.DarkGray;
            this.btnDescargar.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(169)))), ((int)(((byte)(169)))), ((int)(((byte)(169)))));
            this.btnDescargar.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(39)))), ((int)(((byte)(174)))), ((int)(((byte)(96)))));
            this.btnDescargar.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(47)))), ((int)(((byte)(84)))), ((int)(((byte)(57)))));
            this.btnDescargar.Font = new System.Drawing.Font("Bahnschrift Light", 13.75F);
            this.btnDescargar.ForeColor = System.Drawing.Color.White;
            this.btnDescargar.Location = new System.Drawing.Point(563, 12);
            this.btnDescargar.Name = "btnDescargar";
            this.btnDescargar.Size = new System.Drawing.Size(294, 34);
            this.btnDescargar.TabIndex = 2;
            this.btnDescargar.Text = "Descargar Informe PDF";
            this.btnDescargar.Click += new System.EventHandler(this.btnDescargar_Click);
            // 
            // panelResumen
            // 
            this.panelResumen.ColumnCount = 4;
            this.panelResumen.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.panelResumen.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.panelResumen.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.panelResumen.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.panelResumen.Controls.Add(this.lblRemitos, 0, 0);
            this.panelResumen.Controls.Add(this.lblHerramientas, 1, 0);
            this.panelResumen.Controls.Add(this.lblInsumos, 2, 0);
            this.panelResumen.Controls.Add(this.lblEstado, 3, 0);
            this.panelResumen.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelResumen.Location = new System.Drawing.Point(0, 109);
            this.panelResumen.Name = "panelResumen";
            this.panelResumen.Padding = new System.Windows.Forms.Padding(40, 0, 40, 0);
            this.panelResumen.RowCount = 1;
            this.panelResumen.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.panelResumen.Size = new System.Drawing.Size(900, 45);
            this.panelResumen.TabIndex = 2;
            // 
            // lblRemitos
            // 
            this.lblRemitos.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblRemitos.Font = new System.Drawing.Font("Bahnschrift Light", 11F);
            this.lblRemitos.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(135)))), ((int)(((byte)(84)))));
            this.lblRemitos.Location = new System.Drawing.Point(43, 0);
            this.lblRemitos.Name = "lblRemitos";
            this.lblRemitos.Size = new System.Drawing.Size(199, 45);
            this.lblRemitos.TabIndex = 0;
            this.lblRemitos.Text = "Remitos: —";
            this.lblRemitos.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblHerramientas
            // 
            this.lblHerramientas.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblHerramientas.Font = new System.Drawing.Font("Bahnschrift Light", 11F);
            this.lblHerramientas.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(135)))), ((int)(((byte)(84)))));
            this.lblHerramientas.Location = new System.Drawing.Point(248, 0);
            this.lblHerramientas.Name = "lblHerramientas";
            this.lblHerramientas.Size = new System.Drawing.Size(199, 45);
            this.lblHerramientas.TabIndex = 1;
            this.lblHerramientas.Text = "Herramientas: —";
            this.lblHerramientas.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblInsumos
            // 
            this.lblInsumos.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblInsumos.Font = new System.Drawing.Font("Bahnschrift Light", 11F);
            this.lblInsumos.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(135)))), ((int)(((byte)(84)))));
            this.lblInsumos.Location = new System.Drawing.Point(453, 0);
            this.lblInsumos.Name = "lblInsumos";
            this.lblInsumos.Size = new System.Drawing.Size(199, 45);
            this.lblInsumos.TabIndex = 2;
            this.lblInsumos.Text = "Insumos: —";
            this.lblInsumos.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblEstado
            // 
            this.lblEstado.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblEstado.Font = new System.Drawing.Font("Bahnschrift Light", 11F);
            this.lblEstado.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(150)))), ((int)(((byte)(150)))), ((int)(((byte)(150)))));
            this.lblEstado.Location = new System.Drawing.Point(658, 0);
            this.lblEstado.Name = "lblEstado";
            this.lblEstado.Size = new System.Drawing.Size(199, 45);
            this.lblEstado.TabIndex = 3;
            this.lblEstado.Text = "Seleccione una obra";
            this.lblEstado.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // panelTabs
            // 
            this.panelTabs.ColumnCount = 6;
            this.panelTabs.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.panelTabs.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.panelTabs.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.panelTabs.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.panelTabs.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.panelTabs.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.panelTabs.Controls.Add(this.btnTabMovimientos, 0, 0);
            this.panelTabs.Controls.Add(this.btnTabHerramientas, 1, 0);
            this.panelTabs.Controls.Add(this.btnTabInsumos, 2, 0);
            this.panelTabs.Controls.Add(this.btnTabMateriales, 3, 0);
            this.panelTabs.Controls.Add(this.btnTabEventuales, 4, 0);
            this.panelTabs.Controls.Add(this.btnTabCanaletasPlegados, 5, 0);
            this.panelTabs.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTabs.Location = new System.Drawing.Point(0, 154);
            this.panelTabs.Name = "panelTabs";
            this.panelTabs.Padding = new System.Windows.Forms.Padding(40, 0, 40, 0);
            this.panelTabs.RowCount = 1;
            this.panelTabs.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.panelTabs.Size = new System.Drawing.Size(900, 40);
            this.panelTabs.TabIndex = 3;
            // 
            // btnTabMovimientos
            // 
            this.btnTabMovimientos.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.btnTabMovimientos.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(135)))), ((int)(((byte)(84)))));
            this.btnTabMovimientos.Font = new System.Drawing.Font("Bahnschrift Light", 11F);
            this.btnTabMovimientos.ForeColor = System.Drawing.Color.White;
            this.btnTabMovimientos.Location = new System.Drawing.Point(43, 3);
            this.btnTabMovimientos.Name = "btnTabMovimientos";
            this.btnTabMovimientos.Size = new System.Drawing.Size(131, 34);
            this.btnTabMovimientos.TabIndex = 0;
            this.btnTabMovimientos.Text = "📦 Movimientos";
            this.btnTabMovimientos.Click += new System.EventHandler(this.btnTabMovimientos_Click);
            // 
            // btnTabHerramientas
            // 
            this.btnTabHerramientas.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.btnTabHerramientas.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(63)))), ((int)(((byte)(43)))));
            this.btnTabHerramientas.Font = new System.Drawing.Font("Bahnschrift Light", 11F);
            this.btnTabHerramientas.ForeColor = System.Drawing.Color.White;
            this.btnTabHerramientas.Location = new System.Drawing.Point(180, 3);
            this.btnTabHerramientas.Name = "btnTabHerramientas";
            this.btnTabHerramientas.Size = new System.Drawing.Size(131, 34);
            this.btnTabHerramientas.TabIndex = 1;
            this.btnTabHerramientas.Text = "🔧 Herramientas";
            this.btnTabHerramientas.Click += new System.EventHandler(this.btnTabHerramientas_Click);
            // 
            // btnTabInsumos
            // 
            this.btnTabInsumos.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.btnTabInsumos.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(63)))), ((int)(((byte)(43)))));
            this.btnTabInsumos.Font = new System.Drawing.Font("Bahnschrift Light", 11F);
            this.btnTabInsumos.ForeColor = System.Drawing.Color.White;
            this.btnTabInsumos.Location = new System.Drawing.Point(317, 3);
            this.btnTabInsumos.Name = "btnTabInsumos";
            this.btnTabInsumos.Size = new System.Drawing.Size(131, 34);
            this.btnTabInsumos.TabIndex = 2;
            this.btnTabInsumos.Text = "📋 Insumos";
            this.btnTabInsumos.Click += new System.EventHandler(this.btnTabInsumos_Click);
            // 
            // btnTabMateriales
            // 
            this.btnTabMateriales.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.btnTabMateriales.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(63)))), ((int)(((byte)(43)))));
            this.btnTabMateriales.Font = new System.Drawing.Font("Bahnschrift Light", 11F);
            this.btnTabMateriales.ForeColor = System.Drawing.Color.White;
            this.btnTabMateriales.Location = new System.Drawing.Point(454, 3);
            this.btnTabMateriales.Name = "btnTabMateriales";
            this.btnTabMateriales.Size = new System.Drawing.Size(131, 34);
            this.btnTabMateriales.TabIndex = 3;
            this.btnTabMateriales.Text = "🧱 Materiales";
            this.btnTabMateriales.Click += new System.EventHandler(this.btnTabMateriales_Click);
            // 
            // btnTabEventuales
            // 
            this.btnTabEventuales.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.btnTabEventuales.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(63)))), ((int)(((byte)(43)))));
            this.btnTabEventuales.Font = new System.Drawing.Font("Bahnschrift Light", 11F);
            this.btnTabEventuales.ForeColor = System.Drawing.Color.White;
            this.btnTabEventuales.Location = new System.Drawing.Point(591, 3);
            this.btnTabEventuales.Name = "btnTabEventuales";
            this.btnTabEventuales.Size = new System.Drawing.Size(131, 34);
            this.btnTabEventuales.TabIndex = 4;
            this.btnTabEventuales.Text = "📌 Eventuales";
            this.btnTabEventuales.Click += new System.EventHandler(this.btnTabEventuales_Click);
            // 
            // btnTabCanaletasPlegados
            // 
            this.btnTabCanaletasPlegados.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.btnTabCanaletasPlegados.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(63)))), ((int)(((byte)(43)))));
            this.btnTabCanaletasPlegados.Font = new System.Drawing.Font("Bahnschrift Light", 10F);
            this.btnTabCanaletasPlegados.ForeColor = System.Drawing.Color.White;
            this.btnTabCanaletasPlegados.Location = new System.Drawing.Point(728, 3);
            this.btnTabCanaletasPlegados.Name = "btnTabCanaletasPlegados";
            this.btnTabCanaletasPlegados.Size = new System.Drawing.Size(129, 34);
            this.btnTabCanaletasPlegados.TabIndex = 5;
            this.btnTabCanaletasPlegados.Text = "🔩 Canaletas";
            this.btnTabCanaletasPlegados.Click += new System.EventHandler(this.btnTabCanaletasPlegados_Click);
            // 
            // dgvDatos
            // 
            this.dgvDatos.AllowUserToAddRows = false;
            this.dgvDatos.AllowUserToDeleteRows = false;
            this.dgvDatos.AllowUserToResizeRows = false;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(50)))), ((int)(((byte)(40)))));
            dataGridViewCellStyle1.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(90)))), ((int)(((byte)(60)))));
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.Color.White;
            this.dgvDatos.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
            this.dgvDatos.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(30)))), ((int)(((byte)(25)))));
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(63)))), ((int)(((byte)(43)))));
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Bahnschrift", 11F, System.Drawing.FontStyle.Bold);
            dataGridViewCellStyle2.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvDatos.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
            this.dgvDatos.ColumnHeadersHeight = 45;
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(40)))), ((int)(((byte)(32)))));
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Bahnschrift Light", 10.5F);
            dataGridViewCellStyle3.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(90)))), ((int)(((byte)(60)))));
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvDatos.DefaultCellStyle = dataGridViewCellStyle3;
            this.dgvDatos.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvDatos.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(60)))), ((int)(((byte)(50)))));
            this.dgvDatos.Location = new System.Drawing.Point(0, 194);
            this.dgvDatos.Name = "dgvDatos";
            this.dgvDatos.ReadOnly = true;
            this.dgvDatos.RowHeadersVisible = false;
            this.dgvDatos.RowTemplate.Height = 40;
            this.dgvDatos.Size = new System.Drawing.Size(900, 356);
            this.dgvDatos.TabIndex = 4;
            this.dgvDatos.ThemeStyle.AlternatingRowsStyle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(50)))), ((int)(((byte)(40)))));
            this.dgvDatos.ThemeStyle.AlternatingRowsStyle.Font = null;
            this.dgvDatos.ThemeStyle.AlternatingRowsStyle.ForeColor = System.Drawing.Color.White;
            this.dgvDatos.ThemeStyle.AlternatingRowsStyle.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(90)))), ((int)(((byte)(60)))));
            this.dgvDatos.ThemeStyle.AlternatingRowsStyle.SelectionForeColor = System.Drawing.Color.White;
            this.dgvDatos.ThemeStyle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(30)))), ((int)(((byte)(25)))));
            this.dgvDatos.ThemeStyle.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(60)))), ((int)(((byte)(50)))));
            this.dgvDatos.ThemeStyle.HeaderStyle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(63)))), ((int)(((byte)(43)))));
            this.dgvDatos.ThemeStyle.HeaderStyle.BorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            this.dgvDatos.ThemeStyle.HeaderStyle.Font = new System.Drawing.Font("Bahnschrift", 11F, System.Drawing.FontStyle.Bold);
            this.dgvDatos.ThemeStyle.HeaderStyle.ForeColor = System.Drawing.Color.White;
            this.dgvDatos.ThemeStyle.HeaderStyle.HeaightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.dgvDatos.ThemeStyle.HeaderStyle.Height = 45;
            this.dgvDatos.ThemeStyle.ReadOnly = true;
            this.dgvDatos.ThemeStyle.RowsStyle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(40)))), ((int)(((byte)(32)))));
            this.dgvDatos.ThemeStyle.RowsStyle.BorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            this.dgvDatos.ThemeStyle.RowsStyle.Font = new System.Drawing.Font("Bahnschrift Light", 10.5F);
            this.dgvDatos.ThemeStyle.RowsStyle.ForeColor = System.Drawing.Color.White;
            this.dgvDatos.ThemeStyle.RowsStyle.Height = 40;
            this.dgvDatos.ThemeStyle.RowsStyle.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(90)))), ((int)(((byte)(60)))));
            this.dgvDatos.ThemeStyle.RowsStyle.SelectionForeColor = System.Drawing.Color.White;
            // 
            // Vista_InformeObra
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(38)))), ((int)(((byte)(35)))));
            this.ClientSize = new System.Drawing.Size(900, 550);
            this.Controls.Add(this.dgvDatos);
            this.Controls.Add(this.panelTabs);
            this.Controls.Add(this.panelResumen);
            this.Controls.Add(this.panelFiltros);
            this.Controls.Add(this.panelTitulo);
            this.Name = "Vista_InformeObra";
            this.Text = "Vista_InformeObra";
            this.panelTitulo.ResumeLayout(false);
            this.panelTitulo.PerformLayout();
            this.panelFiltros.ResumeLayout(false);
            this.panelFiltros.PerformLayout();
            this.panelResumen.ResumeLayout(false);
            this.panelTabs.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvDatos)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private PanelAntiParpadeo panelTitulo;
        private System.Windows.Forms.Label label1;
        private PanelAntiParpadeo panelFiltros;
        private System.Windows.Forms.Label label2;
        private Guna.UI2.WinForms.Guna2ComboBox cmbObras;
        private Guna.UI2.WinForms.Guna2Button btnDescargar;
        private PanelAntiParpadeo panelResumen;
        private System.Windows.Forms.Label lblRemitos;
        private System.Windows.Forms.Label lblHerramientas;
        private System.Windows.Forms.Label lblInsumos;
        private System.Windows.Forms.Label lblEstado;
        private PanelAntiParpadeo panelTabs;
        private Guna.UI2.WinForms.Guna2Button btnTabMovimientos;
        private Guna.UI2.WinForms.Guna2Button btnTabHerramientas;
        private Guna.UI2.WinForms.Guna2Button btnTabInsumos;
        private Guna.UI2.WinForms.Guna2Button btnTabMateriales;
        private Guna.UI2.WinForms.Guna2Button btnTabEventuales;
        private Guna.UI2.WinForms.Guna2Button btnTabCanaletasPlegados;
        private Guna.UI2.WinForms.Guna2DataGridView dgvDatos;
    }
}
