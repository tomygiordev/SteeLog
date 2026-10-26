namespace WP_Inventario.Vistas
{
    partial class Vista_CanaletasPlegados
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
            this.guna2Panel1 = new Guna.UI2.WinForms.Guna2Panel();
            this.guna2VScrollBar1 = new Guna.UI2.WinForms.Guna2VScrollBar();
            this.dgvCanaletas = new Guna.UI2.WinForms.Guna2DataGridView();
            this.panelFotos = new System.Windows.Forms.Panel();
            this.picPreview = new System.Windows.Forms.PictureBox();
            this.lstFotos = new System.Windows.Forms.ListBox();
            this.lblContadorFotos = new System.Windows.Forms.Label();
            this.panelFotosBotones = new System.Windows.Forms.Panel();
            this.btnQuitarFoto = new Guna.UI2.WinForms.Guna2Button();
            this.btnAgregarFoto = new Guna.UI2.WinForms.Guna2Button();
            this.lblFotosTitulo = new System.Windows.Forms.Label();
            this.panelAntiParpadeo3 = new WP_Inventario.PanelAntiParpadeo();
            this.lblSubtituloFiltro = new System.Windows.Forms.Label();
            this.lblFiltroObra = new System.Windows.Forms.Label();
            this.cmbFiltroObra = new Guna.UI2.WinForms.Guna2ComboBox();
            this.lblFiltroPrecio = new System.Windows.Forms.Label();
            this.txtFiltroPrecio = new Guna.UI2.WinForms.Guna2TextBox();
            this.lblFiltroPagado = new System.Windows.Forms.Label();
            this.cmbFiltroPagado = new Guna.UI2.WinForms.Guna2ComboBox();
            this.lblListado = new System.Windows.Forms.Label();
            this.btnLimpiar = new Guna.UI2.WinForms.Guna2Button();
            this.txtBusqueda = new Guna.UI2.WinForms.Guna2TextBox();
            this.panelAntiParpadeo2 = new WP_Inventario.PanelAntiParpadeo();
            this.lblObra = new System.Windows.Forms.Label();
            this.cmbObra = new Guna.UI2.WinForms.Guna2ComboBox();
            this.lblPrecio = new System.Windows.Forms.Label();
            this.txtPrecio = new Guna.UI2.WinForms.Guna2TextBox();
            this.chkPagado = new Guna.UI2.WinForms.Guna2CheckBox();
            this.lblDescripcion = new System.Windows.Forms.Label();
            this.txtDescripcion = new Guna.UI2.WinForms.Guna2TextBox();
            this.btnGuardar = new Guna.UI2.WinForms.Guna2Button();
            this.btnEliminar = new Guna.UI2.WinForms.Guna2Button();
            this.panelAntiParpadeo1 = new WP_Inventario.PanelAntiParpadeo();
            this.label1 = new System.Windows.Forms.Label();
            this.guna2Panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvCanaletas)).BeginInit();
            this.panelFotos.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picPreview)).BeginInit();
            this.panelFotosBotones.SuspendLayout();
            this.panelAntiParpadeo3.SuspendLayout();
            this.panelAntiParpadeo2.SuspendLayout();
            this.panelAntiParpadeo1.SuspendLayout();
            this.SuspendLayout();
            // 
            // guna2Panel1
            // 
            this.guna2Panel1.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(52)))), ((int)(((byte)(44)))));
            this.guna2Panel1.Controls.Add(this.guna2VScrollBar1);
            this.guna2Panel1.Controls.Add(this.dgvCanaletas);
            this.guna2Panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.guna2Panel1.Location = new System.Drawing.Point(0, 220);
            this.guna2Panel1.Name = "guna2Panel1";
            this.guna2Panel1.Size = new System.Drawing.Size(890, 360);
            this.guna2Panel1.TabIndex = 3;
            // 
            // guna2VScrollBar1
            // 
            this.guna2VScrollBar1.BindingContainer = this.dgvCanaletas;
            this.guna2VScrollBar1.Dock = System.Windows.Forms.DockStyle.Right;
            this.guna2VScrollBar1.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(50)))), ((int)(((byte)(40)))));
            this.guna2VScrollBar1.InUpdate = false;
            this.guna2VScrollBar1.LargeChange = 10;
            this.guna2VScrollBar1.Location = new System.Drawing.Point(872, 0);
            this.guna2VScrollBar1.Minimum = 1;
            this.guna2VScrollBar1.Name = "guna2VScrollBar1";
            this.guna2VScrollBar1.ScrollbarSize = 18;
            this.guna2VScrollBar1.Size = new System.Drawing.Size(18, 360);
            this.guna2VScrollBar1.TabIndex = 1;
            this.guna2VScrollBar1.ThumbColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(85)))), ((int)(((byte)(65)))));
            this.guna2VScrollBar1.Value = 1;
            // 
            // dgvCanaletas
            // 
            this.dgvCanaletas.AllowUserToAddRows = false;
            this.dgvCanaletas.AllowUserToDeleteRows = false;
            this.dgvCanaletas.AllowUserToResizeColumns = false;
            this.dgvCanaletas.AllowUserToResizeRows = false;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(38)))), ((int)(((byte)(35)))));
            this.dgvCanaletas.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
            this.dgvCanaletas.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(38)))), ((int)(((byte)(35)))));
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(55)))), ((int)(((byte)(40)))));
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Bahnschrift Light", 10.5F);
            dataGridViewCellStyle2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(195)))), ((int)(((byte)(215)))), ((int)(((byte)(202)))));
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(55)))), ((int)(((byte)(40)))));
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(195)))), ((int)(((byte)(215)))), ((int)(((byte)(202)))));
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvCanaletas.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
            this.dgvCanaletas.ColumnHeadersHeight = 32;
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(38)))), ((int)(((byte)(35)))));
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Bahnschrift Light", 11F);
            dataGridViewCellStyle3.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle3.Padding = new System.Windows.Forms.Padding(14, 0, 14, 0);
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(47)))), ((int)(((byte)(84)))), ((int)(((byte)(57)))));
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvCanaletas.DefaultCellStyle = dataGridViewCellStyle3;
            this.dgvCanaletas.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvCanaletas.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(50)))), ((int)(((byte)(43)))));
            this.dgvCanaletas.Location = new System.Drawing.Point(0, 0);
            this.dgvCanaletas.Name = "dgvCanaletas";
            this.dgvCanaletas.ReadOnly = true;
            this.dgvCanaletas.RowHeadersVisible = false;
            this.dgvCanaletas.RowTemplate.DefaultCellStyle.Font = new System.Drawing.Font("Bahnschrift Light", 11F);
            this.dgvCanaletas.RowTemplate.Height = 36;
            this.dgvCanaletas.Size = new System.Drawing.Size(890, 360);
            this.dgvCanaletas.TabIndex = 0;
            this.dgvCanaletas.Theme = Guna.UI2.WinForms.Enums.DataGridViewPresetThemes.Dark;
            this.dgvCanaletas.ThemeStyle.AlternatingRowsStyle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(38)))), ((int)(((byte)(35)))));
            this.dgvCanaletas.ThemeStyle.AlternatingRowsStyle.Font = null;
            this.dgvCanaletas.ThemeStyle.AlternatingRowsStyle.ForeColor = System.Drawing.Color.Empty;
            this.dgvCanaletas.ThemeStyle.AlternatingRowsStyle.SelectionBackColor = System.Drawing.Color.Empty;
            this.dgvCanaletas.ThemeStyle.AlternatingRowsStyle.SelectionForeColor = System.Drawing.Color.Empty;
            this.dgvCanaletas.ThemeStyle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(38)))), ((int)(((byte)(35)))));
            this.dgvCanaletas.ThemeStyle.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(50)))), ((int)(((byte)(43)))));
            this.dgvCanaletas.ThemeStyle.HeaderStyle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(55)))), ((int)(((byte)(40)))));
            this.dgvCanaletas.ThemeStyle.HeaderStyle.BorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            this.dgvCanaletas.ThemeStyle.HeaderStyle.Font = new System.Drawing.Font("Bahnschrift Light", 10.5F);
            this.dgvCanaletas.ThemeStyle.HeaderStyle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(195)))), ((int)(((byte)(215)))), ((int)(((byte)(202)))));
            this.dgvCanaletas.ThemeStyle.HeaderStyle.HeaightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.dgvCanaletas.ThemeStyle.HeaderStyle.Height = 32;
            this.dgvCanaletas.ThemeStyle.ReadOnly = true;
            this.dgvCanaletas.ThemeStyle.RowsStyle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(38)))), ((int)(((byte)(35)))));
            this.dgvCanaletas.ThemeStyle.RowsStyle.BorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            this.dgvCanaletas.ThemeStyle.RowsStyle.Font = new System.Drawing.Font("Bahnschrift Light", 11F);
            this.dgvCanaletas.ThemeStyle.RowsStyle.ForeColor = System.Drawing.Color.White;
            this.dgvCanaletas.ThemeStyle.RowsStyle.Height = 36;
            this.dgvCanaletas.ThemeStyle.RowsStyle.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(47)))), ((int)(((byte)(84)))), ((int)(((byte)(57)))));
            this.dgvCanaletas.ThemeStyle.RowsStyle.SelectionForeColor = System.Drawing.Color.White;
            this.dgvCanaletas.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvCanaletas_CellClick);
            // 
            // panelFotos
            // 
            this.panelFotos.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(34)))), ((int)(((byte)(30)))));
            this.panelFotos.Controls.Add(this.picPreview);
            this.panelFotos.Controls.Add(this.lstFotos);
            this.panelFotos.Controls.Add(this.lblContadorFotos);
            this.panelFotos.Controls.Add(this.panelFotosBotones);
            this.panelFotos.Controls.Add(this.lblFotosTitulo);
            this.panelFotos.Dock = System.Windows.Forms.DockStyle.Right;
            this.panelFotos.Location = new System.Drawing.Point(890, 220);
            this.panelFotos.Name = "panelFotos";
            this.panelFotos.Padding = new System.Windows.Forms.Padding(8);
            this.panelFotos.Size = new System.Drawing.Size(290, 360);
            this.panelFotos.TabIndex = 10;
            // 
            // picPreview
            // 
            this.picPreview.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(22)))), ((int)(((byte)(28)))), ((int)(((byte)(25)))));
            this.picPreview.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.picPreview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.picPreview.Location = new System.Drawing.Point(8, 198);
            this.picPreview.Name = "picPreview";
            this.picPreview.Size = new System.Drawing.Size(274, 154);
            this.picPreview.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picPreview.TabIndex = 4;
            this.picPreview.TabStop = false;
            // 
            // lstFotos
            // 
            this.lstFotos.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(38)))), ((int)(((byte)(35)))));
            this.lstFotos.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lstFotos.Dock = System.Windows.Forms.DockStyle.Top;
            this.lstFotos.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.lstFotos.ForeColor = System.Drawing.Color.White;
            this.lstFotos.ItemHeight = 17;
            this.lstFotos.Location = new System.Drawing.Point(8, 94);
            this.lstFotos.Name = "lstFotos";
            this.lstFotos.Size = new System.Drawing.Size(274, 104);
            this.lstFotos.TabIndex = 3;
            this.lstFotos.SelectedIndexChanged += new System.EventHandler(this.lstFotos_SelectedIndexChanged);
            // 
            // lblContadorFotos
            // 
            this.lblContadorFotos.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblContadorFotos.Font = new System.Drawing.Font("Bahnschrift Light", 9.5F);
            this.lblContadorFotos.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(145)))), ((int)(((byte)(175)))), ((int)(((byte)(158)))));
            this.lblContadorFotos.Location = new System.Drawing.Point(8, 74);
            this.lblContadorFotos.Name = "lblContadorFotos";
            this.lblContadorFotos.Size = new System.Drawing.Size(274, 20);
            this.lblContadorFotos.TabIndex = 2;
            this.lblContadorFotos.Text = "0 fotos";
            this.lblContadorFotos.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // panelFotosBotones
            // 
            this.panelFotosBotones.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(34)))), ((int)(((byte)(30)))));
            this.panelFotosBotones.Controls.Add(this.btnQuitarFoto);
            this.panelFotosBotones.Controls.Add(this.btnAgregarFoto);
            this.panelFotosBotones.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelFotosBotones.Location = new System.Drawing.Point(8, 34);
            this.panelFotosBotones.Name = "panelFotosBotones";
            this.panelFotosBotones.Size = new System.Drawing.Size(274, 40);
            this.panelFotosBotones.TabIndex = 1;
            // 
            // btnQuitarFoto
            // 
            this.btnQuitarFoto.DisabledState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.btnQuitarFoto.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.btnQuitarFoto.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.btnQuitarFoto.Enabled = false;
            this.btnQuitarFoto.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(158)))), ((int)(((byte)(45)))), ((int)(((byte)(40)))));
            this.btnQuitarFoto.Font = new System.Drawing.Font("Bahnschrift Light", 10F);
            this.btnQuitarFoto.ForeColor = System.Drawing.Color.White;
            this.btnQuitarFoto.HoverState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(55)))), ((int)(((byte)(48)))));
            this.btnQuitarFoto.Location = new System.Drawing.Point(130, 0);
            this.btnQuitarFoto.Name = "btnQuitarFoto";
            this.btnQuitarFoto.Size = new System.Drawing.Size(144, 40);
            this.btnQuitarFoto.TabIndex = 1;
            this.btnQuitarFoto.Text = "Quitar foto";
            this.btnQuitarFoto.Click += new System.EventHandler(this.btnQuitarFoto_Click);
            // 
            // btnAgregarFoto
            // 
            this.btnAgregarFoto.DisabledState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(65)))), ((int)(((byte)(55)))));
            this.btnAgregarFoto.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(60)))), ((int)(((byte)(50)))));
            this.btnAgregarFoto.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(120)))), ((int)(((byte)(108)))));
            this.btnAgregarFoto.Dock = System.Windows.Forms.DockStyle.Left;
            this.btnAgregarFoto.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(39)))), ((int)(((byte)(174)))), ((int)(((byte)(96)))));
            this.btnAgregarFoto.Font = new System.Drawing.Font("Bahnschrift Light", 10F);
            this.btnAgregarFoto.ForeColor = System.Drawing.Color.White;
            this.btnAgregarFoto.HoverState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(46)))), ((int)(((byte)(196)))), ((int)(((byte)(110)))));
            this.btnAgregarFoto.Location = new System.Drawing.Point(0, 0);
            this.btnAgregarFoto.Name = "btnAgregarFoto";
            this.btnAgregarFoto.Size = new System.Drawing.Size(130, 40);
            this.btnAgregarFoto.TabIndex = 0;
            this.btnAgregarFoto.Text = "Agregar foto";
            this.btnAgregarFoto.Click += new System.EventHandler(this.btnAgregarFoto_Click);
            // 
            // lblFotosTitulo
            // 
            this.lblFotosTitulo.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblFotosTitulo.Font = new System.Drawing.Font("Bahnschrift SemiBold", 11F, System.Drawing.FontStyle.Bold);
            this.lblFotosTitulo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(210)))), ((int)(((byte)(235)))), ((int)(((byte)(218)))));
            this.lblFotosTitulo.Location = new System.Drawing.Point(8, 8);
            this.lblFotosTitulo.Name = "lblFotosTitulo";
            this.lblFotosTitulo.Size = new System.Drawing.Size(274, 26);
            this.lblFotosTitulo.TabIndex = 0;
            this.lblFotosTitulo.Text = "FOTOS";
            this.lblFotosTitulo.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // panelAntiParpadeo3
            // 
            this.panelAntiParpadeo3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(34)))), ((int)(((byte)(30)))));
            this.panelAntiParpadeo3.ColumnCount = 8;
            this.panelAntiParpadeo3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 60F));
            this.panelAntiParpadeo3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.panelAntiParpadeo3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 86F));
            this.panelAntiParpadeo3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 90F));
            this.panelAntiParpadeo3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 70F));
            this.panelAntiParpadeo3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 110F));
            this.panelAntiParpadeo3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 1F));
            this.panelAntiParpadeo3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 120F));
            this.panelAntiParpadeo3.Controls.Add(this.lblSubtituloFiltro, 0, 0);
            this.panelAntiParpadeo3.Controls.Add(this.lblFiltroObra, 0, 1);
            this.panelAntiParpadeo3.Controls.Add(this.cmbFiltroObra, 1, 1);
            this.panelAntiParpadeo3.Controls.Add(this.lblFiltroPrecio, 2, 1);
            this.panelAntiParpadeo3.Controls.Add(this.txtFiltroPrecio, 3, 1);
            this.panelAntiParpadeo3.Controls.Add(this.lblFiltroPagado, 4, 1);
            this.panelAntiParpadeo3.Controls.Add(this.cmbFiltroPagado, 5, 1);
            this.panelAntiParpadeo3.Controls.Add(this.lblListado, 6, 1);
            this.panelAntiParpadeo3.Controls.Add(this.btnLimpiar, 7, 1);
            this.panelAntiParpadeo3.Controls.Add(this.txtBusqueda, 1, 2);
            this.panelAntiParpadeo3.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelAntiParpadeo3.GrowStyle = System.Windows.Forms.TableLayoutPanelGrowStyle.AddColumns;
            this.panelAntiParpadeo3.Location = new System.Drawing.Point(0, 130);
            this.panelAntiParpadeo3.Name = "panelAntiParpadeo3";
            this.panelAntiParpadeo3.Padding = new System.Windows.Forms.Padding(0, 6, 4, 6);
            this.panelAntiParpadeo3.RowCount = 3;
            this.panelAntiParpadeo3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 18F));
            this.panelAntiParpadeo3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.panelAntiParpadeo3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.panelAntiParpadeo3.Size = new System.Drawing.Size(1180, 90);
            this.panelAntiParpadeo3.TabIndex = 2;
            // 
            // lblSubtituloFiltro
            // 
            this.lblSubtituloFiltro.AutoSize = true;
            this.panelAntiParpadeo3.SetColumnSpan(this.lblSubtituloFiltro, 8);
            this.lblSubtituloFiltro.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSubtituloFiltro.Font = new System.Drawing.Font("Bahnschrift SemiBold", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblSubtituloFiltro.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(155)))), ((int)(((byte)(190)))), ((int)(((byte)(170)))));
            this.lblSubtituloFiltro.Location = new System.Drawing.Point(3, 6);
            this.lblSubtituloFiltro.Name = "lblSubtituloFiltro";
            this.lblSubtituloFiltro.Padding = new System.Windows.Forms.Padding(14, 0, 6, 0);
            this.lblSubtituloFiltro.Size = new System.Drawing.Size(1170, 18);
            this.lblSubtituloFiltro.TabIndex = 4;
            this.lblSubtituloFiltro.Text = "Filtrar canaletas / plegados";
            this.lblSubtituloFiltro.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            // 
            // lblFiltroObra
            // 
            this.lblFiltroObra.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblFiltroObra.Font = new System.Drawing.Font("Bahnschrift Light", 10F);
            this.lblFiltroObra.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(145)))), ((int)(((byte)(175)))), ((int)(((byte)(158)))));
            this.lblFiltroObra.Location = new System.Drawing.Point(3, 24);
            this.lblFiltroObra.Name = "lblFiltroObra";
            this.lblFiltroObra.Padding = new System.Windows.Forms.Padding(14, 0, 6, 0);
            this.lblFiltroObra.Size = new System.Drawing.Size(54, 32);
            this.lblFiltroObra.TabIndex = 0;
            this.lblFiltroObra.Text = "Obra:";
            this.lblFiltroObra.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // cmbFiltroObra
            // 
            this.cmbFiltroObra.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbFiltroObra.BackColor = System.Drawing.Color.Transparent;
            this.cmbFiltroObra.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(75)))), ((int)(((byte)(62)))));
            this.cmbFiltroObra.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.cmbFiltroObra.DropDownHeight = 220;
            this.cmbFiltroObra.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbFiltroObra.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(50)))), ((int)(((byte)(40)))));
            this.cmbFiltroObra.FocusedColor = System.Drawing.Color.FromArgb(((int)(((byte)(39)))), ((int)(((byte)(174)))), ((int)(((byte)(96)))));
            this.cmbFiltroObra.FocusedState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(39)))), ((int)(((byte)(174)))), ((int)(((byte)(96)))));
            this.cmbFiltroObra.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.cmbFiltroObra.ForeColor = System.Drawing.Color.White;
            this.cmbFiltroObra.HoverState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(120)))), ((int)(((byte)(80)))));
            this.cmbFiltroObra.IntegralHeight = false;
            this.cmbFiltroObra.ItemHeight = 26;
            this.cmbFiltroObra.Location = new System.Drawing.Point(63, 26);
            this.cmbFiltroObra.Margin = new System.Windows.Forms.Padding(3, 2, 8, 2);
            this.cmbFiltroObra.Name = "cmbFiltroObra";
            this.cmbFiltroObra.Size = new System.Drawing.Size(628, 32);
            this.cmbFiltroObra.TabIndex = 1;
            this.cmbFiltroObra.SelectedIndexChanged += new System.EventHandler(this.cmbFiltroObra_SelectedIndexChanged);
            // 
            // lblFiltroPrecio
            // 
            this.lblFiltroPrecio.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblFiltroPrecio.Font = new System.Drawing.Font("Bahnschrift Light", 10F);
            this.lblFiltroPrecio.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(145)))), ((int)(((byte)(175)))), ((int)(((byte)(158)))));
            this.lblFiltroPrecio.Location = new System.Drawing.Point(702, 24);
            this.lblFiltroPrecio.Name = "lblFiltroPrecio";
            this.lblFiltroPrecio.Padding = new System.Windows.Forms.Padding(14, 0, 6, 0);
            this.lblFiltroPrecio.Size = new System.Drawing.Size(80, 32);
            this.lblFiltroPrecio.TabIndex = 2;
            this.lblFiltroPrecio.Text = "Precio:";
            this.lblFiltroPrecio.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtFiltroPrecio
            // 
            this.txtFiltroPrecio.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtFiltroPrecio.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(75)))), ((int)(((byte)(62)))));
            this.txtFiltroPrecio.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txtFiltroPrecio.DefaultText = "";
            this.txtFiltroPrecio.DisabledState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this.txtFiltroPrecio.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(55)))), ((int)(((byte)(44)))));
            this.txtFiltroPrecio.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(100)))), ((int)(((byte)(100)))));
            this.txtFiltroPrecio.DisabledState.PlaceholderForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(100)))), ((int)(((byte)(100)))));
            this.txtFiltroPrecio.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(50)))), ((int)(((byte)(40)))));
            this.txtFiltroPrecio.FocusedState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(39)))), ((int)(((byte)(174)))), ((int)(((byte)(96)))));
            this.txtFiltroPrecio.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtFiltroPrecio.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(225)))), ((int)(((byte)(225)))), ((int)(((byte)(225)))));
            this.txtFiltroPrecio.HoverState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(120)))), ((int)(((byte)(80)))));
            this.txtFiltroPrecio.Location = new System.Drawing.Point(788, 28);
            this.txtFiltroPrecio.Margin = new System.Windows.Forms.Padding(3, 2, 8, 2);
            this.txtFiltroPrecio.Name = "txtFiltroPrecio";
            this.txtFiltroPrecio.PlaceholderText = "0.00";
            this.txtFiltroPrecio.SelectedText = "";
            this.txtFiltroPrecio.Size = new System.Drawing.Size(79, 24);
            this.txtFiltroPrecio.TabIndex = 3;
            this.txtFiltroPrecio.TextChanged += new System.EventHandler(this.txtFiltroPrecio_TextChanged);
            // 
            // lblFiltroPagado
            // 
            this.lblFiltroPagado.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblFiltroPagado.Font = new System.Drawing.Font("Bahnschrift Light", 10F);
            this.lblFiltroPagado.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(145)))), ((int)(((byte)(175)))), ((int)(((byte)(158)))));
            this.lblFiltroPagado.Location = new System.Drawing.Point(878, 24);
            this.lblFiltroPagado.Name = "lblFiltroPagado";
            this.lblFiltroPagado.Padding = new System.Windows.Forms.Padding(14, 0, 6, 0);
            this.lblFiltroPagado.Size = new System.Drawing.Size(64, 32);
            this.lblFiltroPagado.TabIndex = 8;
            this.lblFiltroPagado.Text = "Pagado:";
            this.lblFiltroPagado.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // cmbFiltroPagado
            // 
            this.cmbFiltroPagado.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbFiltroPagado.BackColor = System.Drawing.Color.Transparent;
            this.cmbFiltroPagado.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(75)))), ((int)(((byte)(62)))));
            this.cmbFiltroPagado.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.cmbFiltroPagado.DropDownHeight = 120;
            this.cmbFiltroPagado.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbFiltroPagado.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(50)))), ((int)(((byte)(40)))));
            this.cmbFiltroPagado.FocusedColor = System.Drawing.Color.FromArgb(((int)(((byte)(39)))), ((int)(((byte)(174)))), ((int)(((byte)(96)))));
            this.cmbFiltroPagado.FocusedState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(39)))), ((int)(((byte)(174)))), ((int)(((byte)(96)))));
            this.cmbFiltroPagado.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.cmbFiltroPagado.ForeColor = System.Drawing.Color.White;
            this.cmbFiltroPagado.HoverState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(120)))), ((int)(((byte)(80)))));
            this.cmbFiltroPagado.IntegralHeight = false;
            this.cmbFiltroPagado.ItemHeight = 26;
            this.cmbFiltroPagado.Location = new System.Drawing.Point(948, 26);
            this.cmbFiltroPagado.Margin = new System.Windows.Forms.Padding(3, 2, 8, 2);
            this.cmbFiltroPagado.Name = "cmbFiltroPagado";
            this.cmbFiltroPagado.Size = new System.Drawing.Size(99, 32);
            this.cmbFiltroPagado.TabIndex = 9;
            this.cmbFiltroPagado.SelectedIndexChanged += new System.EventHandler(this.cmbFiltroPagado_SelectedIndexChanged);
            // 
            // lblListado
            // 
            this.lblListado.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblListado.AutoSize = true;
            this.lblListado.Font = new System.Drawing.Font("Bahnschrift SemiBold", 10F, System.Drawing.FontStyle.Bold);
            this.lblListado.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(145)))), ((int)(((byte)(175)))), ((int)(((byte)(158)))));
            this.lblListado.Location = new System.Drawing.Point(1058, 31);
            this.lblListado.Name = "lblListado";
            this.lblListado.Size = new System.Drawing.Size(1, 17);
            this.lblListado.TabIndex = 6;
            this.lblListado.Text = "LISTADO";
            this.lblListado.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btnLimpiar
            // 
            this.btnLimpiar.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.btnLimpiar.DisabledState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.btnLimpiar.DisabledState.CustomBorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.btnLimpiar.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.btnLimpiar.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.btnLimpiar.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(80)))), ((int)(((byte)(70)))));
            this.btnLimpiar.Font = new System.Drawing.Font("Bahnschrift Light", 10F);
            this.btnLimpiar.ForeColor = System.Drawing.Color.White;
            this.btnLimpiar.HoverState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(75)))), ((int)(((byte)(95)))), ((int)(((byte)(82)))));
            this.btnLimpiar.Location = new System.Drawing.Point(1060, 27);
            this.btnLimpiar.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.btnLimpiar.Name = "btnLimpiar";
            this.btnLimpiar.Size = new System.Drawing.Size(112, 26);
            this.btnLimpiar.TabIndex = 7;
            this.btnLimpiar.Text = "Limpiar filtros";
            this.btnLimpiar.Click += new System.EventHandler(this.btnLimpiar_Click);
            // 
            // txtBusqueda
            // 
            this.txtBusqueda.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtBusqueda.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(75)))), ((int)(((byte)(62)))));
            this.panelAntiParpadeo3.SetColumnSpan(this.txtBusqueda, 5);
            this.txtBusqueda.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txtBusqueda.DefaultText = "";
            this.txtBusqueda.DisabledState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this.txtBusqueda.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(32)))), ((int)(((byte)(29)))));
            this.txtBusqueda.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(100)))), ((int)(((byte)(100)))));
            this.txtBusqueda.DisabledState.PlaceholderForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(100)))), ((int)(((byte)(100)))));
            this.txtBusqueda.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(22)))), ((int)(((byte)(28)))), ((int)(((byte)(25)))));
            this.txtBusqueda.FocusedState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(39)))), ((int)(((byte)(174)))), ((int)(((byte)(96)))));
            this.txtBusqueda.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtBusqueda.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(225)))), ((int)(((byte)(225)))), ((int)(((byte)(225)))));
            this.txtBusqueda.HoverState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(120)))), ((int)(((byte)(80)))));
            this.txtBusqueda.Location = new System.Drawing.Point(63, 58);
            this.txtBusqueda.Margin = new System.Windows.Forms.Padding(3, 2, 8, 2);
            this.txtBusqueda.Name = "txtBusqueda";
            this.txtBusqueda.PlaceholderForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(150)))), ((int)(((byte)(170)))), ((int)(((byte)(156)))));
            this.txtBusqueda.PlaceholderText = "Buscar por descripción";
            this.txtBusqueda.SelectedText = "";
            this.txtBusqueda.Size = new System.Drawing.Size(984, 24);
            this.txtBusqueda.TabIndex = 5;
            this.txtBusqueda.TextChanged += new System.EventHandler(this.txtBusqueda_TextChanged);
            // 
            // panelAntiParpadeo2
            // 
            this.panelAntiParpadeo2.ColumnCount = 8;
            this.panelAntiParpadeo2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 85F));
            this.panelAntiParpadeo2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.panelAntiParpadeo2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 60F));
            this.panelAntiParpadeo2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 90F));
            this.panelAntiParpadeo2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 110F));
            this.panelAntiParpadeo2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 10F));
            this.panelAntiParpadeo2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 90F));
            this.panelAntiParpadeo2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 100F));
            this.panelAntiParpadeo2.Controls.Add(this.lblObra, 0, 0);
            this.panelAntiParpadeo2.Controls.Add(this.cmbObra, 1, 0);
            this.panelAntiParpadeo2.Controls.Add(this.lblPrecio, 2, 0);
            this.panelAntiParpadeo2.Controls.Add(this.txtPrecio, 3, 0);
            this.panelAntiParpadeo2.Controls.Add(this.chkPagado, 4, 0);
            this.panelAntiParpadeo2.Controls.Add(this.lblDescripcion, 0, 1);
            this.panelAntiParpadeo2.Controls.Add(this.txtDescripcion, 1, 1);
            this.panelAntiParpadeo2.Controls.Add(this.btnGuardar, 6, 1);
            this.panelAntiParpadeo2.Controls.Add(this.btnEliminar, 7, 1);
            this.panelAntiParpadeo2.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelAntiParpadeo2.GrowStyle = System.Windows.Forms.TableLayoutPanelGrowStyle.AddColumns;
            this.panelAntiParpadeo2.Location = new System.Drawing.Point(0, 42);
            this.panelAntiParpadeo2.Name = "panelAntiParpadeo2";
            this.panelAntiParpadeo2.Padding = new System.Windows.Forms.Padding(0, 8, 4, 8);
            this.panelAntiParpadeo2.RowCount = 2;
            this.panelAntiParpadeo2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.panelAntiParpadeo2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.panelAntiParpadeo2.Size = new System.Drawing.Size(1180, 88);
            this.panelAntiParpadeo2.TabIndex = 1;
            // 
            // lblObra
            // 
            this.lblObra.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblObra.Font = new System.Drawing.Font("Bahnschrift Light", 10.5F);
            this.lblObra.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(155)))), ((int)(((byte)(185)))), ((int)(((byte)(165)))));
            this.lblObra.Location = new System.Drawing.Point(3, 8);
            this.lblObra.Name = "lblObra";
            this.lblObra.Padding = new System.Windows.Forms.Padding(14, 0, 6, 0);
            this.lblObra.Size = new System.Drawing.Size(79, 36);
            this.lblObra.TabIndex = 0;
            this.lblObra.Text = "Obra:";
            this.lblObra.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // cmbObra
            // 
            this.cmbObra.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbObra.BackColor = System.Drawing.Color.Transparent;
            this.cmbObra.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(75)))), ((int)(((byte)(62)))));
            this.cmbObra.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.cmbObra.DropDownHeight = 220;
            this.cmbObra.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbObra.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(50)))), ((int)(((byte)(40)))));
            this.cmbObra.FocusedColor = System.Drawing.Color.FromArgb(((int)(((byte)(39)))), ((int)(((byte)(174)))), ((int)(((byte)(96)))));
            this.cmbObra.FocusedState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(39)))), ((int)(((byte)(174)))), ((int)(((byte)(96)))));
            this.cmbObra.Font = new System.Drawing.Font("Segoe UI", 10.5F);
            this.cmbObra.ForeColor = System.Drawing.Color.White;
            this.cmbObra.HoverState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(120)))), ((int)(((byte)(80)))));
            this.cmbObra.IntegralHeight = false;
            this.cmbObra.ItemHeight = 28;
            this.cmbObra.Location = new System.Drawing.Point(88, 10);
            this.cmbObra.Margin = new System.Windows.Forms.Padding(3, 2, 8, 2);
            this.cmbObra.Name = "cmbObra";
            this.cmbObra.Size = new System.Drawing.Size(620, 34);
            this.cmbObra.TabIndex = 1;
            // 
            // lblPrecio
            // 
            this.lblPrecio.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPrecio.Font = new System.Drawing.Font("Bahnschrift Light", 10.5F);
            this.lblPrecio.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(155)))), ((int)(((byte)(185)))), ((int)(((byte)(165)))));
            this.lblPrecio.Location = new System.Drawing.Point(719, 8);
            this.lblPrecio.Name = "lblPrecio";
            this.lblPrecio.Padding = new System.Windows.Forms.Padding(8, 0, 4, 0);
            this.lblPrecio.Size = new System.Drawing.Size(54, 36);
            this.lblPrecio.TabIndex = 4;
            this.lblPrecio.Text = "Precio:";
            this.lblPrecio.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtPrecio
            // 
            this.txtPrecio.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtPrecio.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.txtPrecio.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(75)))), ((int)(((byte)(62)))));
            this.txtPrecio.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txtPrecio.DefaultText = "";
            this.txtPrecio.DisabledState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this.txtPrecio.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(55)))), ((int)(((byte)(44)))));
            this.txtPrecio.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(100)))), ((int)(((byte)(100)))));
            this.txtPrecio.DisabledState.PlaceholderForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(100)))), ((int)(((byte)(100)))));
            this.txtPrecio.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(50)))), ((int)(((byte)(40)))));
            this.txtPrecio.FocusedState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(39)))), ((int)(((byte)(174)))), ((int)(((byte)(96)))));
            this.txtPrecio.Font = new System.Drawing.Font("Segoe UI", 10.5F);
            this.txtPrecio.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(225)))), ((int)(((byte)(225)))), ((int)(((byte)(225)))));
            this.txtPrecio.HoverState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(120)))), ((int)(((byte)(80)))));
            this.txtPrecio.Location = new System.Drawing.Point(779, 11);
            this.txtPrecio.Margin = new System.Windows.Forms.Padding(3, 2, 8, 2);
            this.txtPrecio.Name = "txtPrecio";
            this.txtPrecio.PlaceholderText = "0.00";
            this.txtPrecio.SelectedText = "";
            this.txtPrecio.Size = new System.Drawing.Size(79, 30);
            this.txtPrecio.TabIndex = 5;
            // 
            // chkPagado
            // 
            this.chkPagado.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.chkPagado.AutoSize = true;
            this.chkPagado.CheckedState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(39)))), ((int)(((byte)(174)))), ((int)(((byte)(96)))));
            this.chkPagado.CheckedState.BorderRadius = 2;
            this.chkPagado.CheckedState.BorderThickness = 0;
            this.chkPagado.CheckedState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(39)))), ((int)(((byte)(174)))), ((int)(((byte)(96)))));
            this.chkPagado.Font = new System.Drawing.Font("Segoe UI", 10.5F);
            this.chkPagado.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(210)))), ((int)(((byte)(235)))), ((int)(((byte)(218)))));
            this.chkPagado.Location = new System.Drawing.Point(869, 11);
            this.chkPagado.Name = "chkPagado";
            this.chkPagado.Size = new System.Drawing.Size(86, 30);
            this.chkPagado.TabIndex = 8;
            this.chkPagado.Text = "   Pagado";
            this.chkPagado.UncheckedState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(120)))), ((int)(((byte)(108)))));
            this.chkPagado.UncheckedState.BorderRadius = 2;
            this.chkPagado.UncheckedState.BorderThickness = 0;
            this.chkPagado.UncheckedState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(120)))), ((int)(((byte)(108)))));
            this.chkPagado.UseVisualStyleBackColor = true;
            // 
            // lblDescripcion
            // 
            this.lblDescripcion.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDescripcion.Font = new System.Drawing.Font("Bahnschrift Light", 10.5F);
            this.lblDescripcion.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(155)))), ((int)(((byte)(185)))), ((int)(((byte)(165)))));
            this.lblDescripcion.Location = new System.Drawing.Point(3, 44);
            this.lblDescripcion.Name = "lblDescripcion";
            this.lblDescripcion.Padding = new System.Windows.Forms.Padding(14, 0, 6, 0);
            this.lblDescripcion.Size = new System.Drawing.Size(79, 36);
            this.lblDescripcion.TabIndex = 2;
            this.lblDescripcion.Text = "Descripción:";
            this.lblDescripcion.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtDescripcion
            // 
            this.txtDescripcion.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtDescripcion.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.txtDescripcion.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(75)))), ((int)(((byte)(62)))));
            this.panelAntiParpadeo2.SetColumnSpan(this.txtDescripcion, 5);
            this.txtDescripcion.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txtDescripcion.DefaultText = "";
            this.txtDescripcion.DisabledState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this.txtDescripcion.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(55)))), ((int)(((byte)(44)))));
            this.txtDescripcion.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(100)))), ((int)(((byte)(100)))));
            this.txtDescripcion.DisabledState.PlaceholderForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(100)))), ((int)(((byte)(100)))));
            this.txtDescripcion.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(50)))), ((int)(((byte)(40)))));
            this.txtDescripcion.FocusedState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(39)))), ((int)(((byte)(174)))), ((int)(((byte)(96)))));
            this.txtDescripcion.Font = new System.Drawing.Font("Segoe UI", 10.5F);
            this.txtDescripcion.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(225)))), ((int)(((byte)(225)))), ((int)(((byte)(225)))));
            this.txtDescripcion.HoverState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(120)))), ((int)(((byte)(80)))));
            this.txtDescripcion.Location = new System.Drawing.Point(88, 47);
            this.txtDescripcion.Margin = new System.Windows.Forms.Padding(3, 2, 8, 2);
            this.txtDescripcion.Name = "txtDescripcion";
            this.txtDescripcion.PlaceholderText = "Describir trabajo";
            this.txtDescripcion.SelectedText = "";
            this.txtDescripcion.Size = new System.Drawing.Size(890, 30);
            this.txtDescripcion.TabIndex = 3;
            // 
            // btnGuardar
            // 
            this.btnGuardar.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.btnGuardar.DisabledState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(65)))), ((int)(((byte)(55)))));
            this.btnGuardar.DisabledState.CustomBorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(65)))), ((int)(((byte)(55)))));
            this.btnGuardar.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(60)))), ((int)(((byte)(50)))));
            this.btnGuardar.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(120)))), ((int)(((byte)(108)))));
            this.btnGuardar.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(39)))), ((int)(((byte)(174)))), ((int)(((byte)(96)))));
            this.btnGuardar.Font = new System.Drawing.Font("Bahnschrift Light", 11F);
            this.btnGuardar.ForeColor = System.Drawing.Color.White;
            this.btnGuardar.HoverState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(46)))), ((int)(((byte)(196)))), ((int)(((byte)(110)))));
            this.btnGuardar.Location = new System.Drawing.Point(990, 45);
            this.btnGuardar.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.btnGuardar.Name = "btnGuardar";
            this.btnGuardar.Size = new System.Drawing.Size(82, 34);
            this.btnGuardar.TabIndex = 6;
            this.btnGuardar.Text = "Guardar";
            this.btnGuardar.Click += new System.EventHandler(this.btnGuardar_Click);
            // 
            // btnEliminar
            // 
            this.btnEliminar.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.btnEliminar.DisabledState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.btnEliminar.DisabledState.CustomBorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.btnEliminar.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.btnEliminar.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.btnEliminar.Enabled = false;
            this.btnEliminar.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(158)))), ((int)(((byte)(45)))), ((int)(((byte)(40)))));
            this.btnEliminar.Font = new System.Drawing.Font("Bahnschrift Light", 11F);
            this.btnEliminar.ForeColor = System.Drawing.Color.White;
            this.btnEliminar.HoverState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(55)))), ((int)(((byte)(48)))));
            this.btnEliminar.Location = new System.Drawing.Point(1080, 45);
            this.btnEliminar.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.btnEliminar.Name = "btnEliminar";
            this.btnEliminar.Size = new System.Drawing.Size(92, 34);
            this.btnEliminar.TabIndex = 7;
            this.btnEliminar.Text = "Eliminar";
            this.btnEliminar.Click += new System.EventHandler(this.btnEliminar_Click);
            // 
            // panelAntiParpadeo1
            // 
            this.panelAntiParpadeo1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(55)))), ((int)(((byte)(40)))));
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
            this.panelAntiParpadeo1.Size = new System.Drawing.Size(1180, 42);
            this.panelAntiParpadeo1.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label1.Font = new System.Drawing.Font("Bahnschrift SemiBold", 13F, System.Drawing.FontStyle.Bold);
            this.label1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(210)))), ((int)(((byte)(235)))), ((int)(((byte)(218)))));
            this.label1.Location = new System.Drawing.Point(17, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(1146, 42);
            this.label1.TabIndex = 0;
            this.label1.Text = "CANALETAS / PLEGADOS";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // Vista_CanaletasPlegados
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(38)))), ((int)(((byte)(35)))));
            this.ClientSize = new System.Drawing.Size(1180, 580);
            this.Controls.Add(this.guna2Panel1);
            this.Controls.Add(this.panelFotos);
            this.Controls.Add(this.panelAntiParpadeo3);
            this.Controls.Add(this.panelAntiParpadeo2);
            this.Controls.Add(this.panelAntiParpadeo1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "Vista_CanaletasPlegados";
            this.Text = "Gestión de Canaletas / Plegados";
            this.guna2Panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvCanaletas)).EndInit();
            this.panelFotos.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.picPreview)).EndInit();
            this.panelFotosBotones.ResumeLayout(false);
            this.panelAntiParpadeo3.ResumeLayout(false);
            this.panelAntiParpadeo3.PerformLayout();
            this.panelAntiParpadeo2.ResumeLayout(false);
            this.panelAntiParpadeo2.PerformLayout();
            this.panelAntiParpadeo1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private WP_Inventario.PanelAntiParpadeo panelAntiParpadeo1;
        private System.Windows.Forms.Label label1;
        private WP_Inventario.PanelAntiParpadeo panelAntiParpadeo2;
        private System.Windows.Forms.Label lblObra;
        private Guna.UI2.WinForms.Guna2ComboBox cmbObra;
        private System.Windows.Forms.Label lblDescripcion;
        private Guna.UI2.WinForms.Guna2TextBox txtDescripcion;
        private System.Windows.Forms.Label lblPrecio;
        private Guna.UI2.WinForms.Guna2TextBox txtPrecio;
        private Guna.UI2.WinForms.Guna2CheckBox chkPagado;
        private Guna.UI2.WinForms.Guna2Button btnGuardar;
        private Guna.UI2.WinForms.Guna2Button btnEliminar;
        private WP_Inventario.PanelAntiParpadeo panelAntiParpadeo3;
        private System.Windows.Forms.Label lblFiltroObra;
        private Guna.UI2.WinForms.Guna2ComboBox cmbFiltroObra;
        private System.Windows.Forms.Label lblFiltroPrecio;
        private Guna.UI2.WinForms.Guna2TextBox txtFiltroPrecio;
        private System.Windows.Forms.Label lblFiltroPagado;
        private Guna.UI2.WinForms.Guna2ComboBox cmbFiltroPagado;
        private System.Windows.Forms.Label lblSubtituloFiltro;
        private Guna.UI2.WinForms.Guna2TextBox txtBusqueda;
        private System.Windows.Forms.Label lblListado;
        private Guna.UI2.WinForms.Guna2Button btnLimpiar;
        private Guna.UI2.WinForms.Guna2Panel guna2Panel1;
        private Guna.UI2.WinForms.Guna2VScrollBar guna2VScrollBar1;
        private Guna.UI2.WinForms.Guna2DataGridView dgvCanaletas;
        private System.Windows.Forms.Panel panelFotos;
        private System.Windows.Forms.Label lblFotosTitulo;
        private System.Windows.Forms.Panel panelFotosBotones;
        private Guna.UI2.WinForms.Guna2Button btnAgregarFoto;
        private Guna.UI2.WinForms.Guna2Button btnQuitarFoto;
        private System.Windows.Forms.Label lblContadorFotos;
        private System.Windows.Forms.ListBox lstFotos;
        private System.Windows.Forms.PictureBox picPreview;
    }
}
