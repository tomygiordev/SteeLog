namespace WP_Inventario.Vistas.Movimientos
{
    partial class Vista_HistorialMovimientos
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
            this.labelTitle = new System.Windows.Forms.Label();
            this.lblSubtitulo = new System.Windows.Forms.Label();
            this.materiales = new WP_Inventario.PanelAntiParpadeo();
            this.lblDesde = new System.Windows.Forms.Label();
            this.dtpDesde = new Guna.UI2.WinForms.Guna2DateTimePicker();
            this.lblHasta = new System.Windows.Forms.Label();
            this.dtpHasta = new Guna.UI2.WinForms.Guna2DateTimePicker();
            this.lblTipo = new System.Windows.Forms.Label();
            this.cmbTipoMov = new Guna.UI2.WinForms.Guna2ComboBox();
            this.lblBuscar = new System.Windows.Forms.Label();
            this.txtBuscarMaterial = new Guna.UI2.WinForms.Guna2TextBox();
            this.btnBuscar = new Guna.UI2.WinForms.Guna2Button();
            this.btnExportar = new Guna.UI2.WinForms.Guna2Button();
            this.dgvHistorial = new Guna.UI2.WinForms.Guna2DataGridView();
            this.panelTitulo.SuspendLayout();
            this.materiales.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvHistorial)).BeginInit();
            this.SuspendLayout();
            // 
            // panelTitulo
            // 
            this.panelTitulo.ColumnCount = 1;
            this.panelTitulo.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.panelTitulo.Controls.Add(this.labelTitle, 0, 0);
            this.panelTitulo.Controls.Add(this.lblSubtitulo, 0, 1);
            this.panelTitulo.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTitulo.GrowStyle = System.Windows.Forms.TableLayoutPanelGrowStyle.AddColumns;
            this.panelTitulo.Location = new System.Drawing.Point(0, 0);
            this.panelTitulo.Name = "panelTitulo";
            this.panelTitulo.Padding = new System.Windows.Forms.Padding(40, 0, 40, 0);
            this.panelTitulo.RowCount = 2;
            this.panelTitulo.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.panelTitulo.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.panelTitulo.Size = new System.Drawing.Size(796, 80);
            this.panelTitulo.TabIndex = 13;
            // 
            // labelTitle
            // 
            this.labelTitle.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.labelTitle.AutoSize = true;
            this.labelTitle.Font = new System.Drawing.Font("Bahnschrift SemiBold", 22F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelTitle.ForeColor = System.Drawing.SystemColors.Control;
            this.labelTitle.Location = new System.Drawing.Point(43, 3);
            this.labelTitle.Name = "labelTitle";
            this.labelTitle.Size = new System.Drawing.Size(710, 33);
            this.labelTitle.TabIndex = 3;
            this.labelTitle.Text = "HISTORIAL DE MOVIMIENTOS";
            this.labelTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblSubtitulo
            // 
            this.lblSubtitulo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblSubtitulo.AutoSize = true;
            this.lblSubtitulo.Font = new System.Drawing.Font("Bahnschrift Light", 16F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblSubtitulo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblSubtitulo.Location = new System.Drawing.Point(43, 48);
            this.lblSubtitulo.Name = "lblSubtitulo";
            this.lblSubtitulo.Size = new System.Drawing.Size(710, 23);
            this.lblSubtitulo.TabIndex = 4;
            this.lblSubtitulo.Text = "";
            this.lblSubtitulo.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // materiales
            // 
            this.materiales.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.materiales.AutoSize = true;
            this.materiales.CellBorderStyle = System.Windows.Forms.TableLayoutPanelCellBorderStyle.Single;
            this.materiales.ColumnCount = 8;
            this.materiales.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());           // 0: lblDesde
            this.materiales.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());           // 1: dtpDesde
            this.materiales.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());           // 2: lblHasta
            this.materiales.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());           // 3: dtpHasta
            this.materiales.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());           // 4: lblTipo
            this.materiales.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());           // 5: cmbTipoMov
            this.materiales.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F)); // 6: txtBuscar
            this.materiales.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());           // 7: btnExportar
            this.materiales.Controls.Add(this.lblDesde, 0, 0);
            this.materiales.Controls.Add(this.dtpDesde, 1, 0);
            this.materiales.Controls.Add(this.lblHasta, 2, 0);
            this.materiales.Controls.Add(this.dtpHasta, 3, 0);
            this.materiales.Controls.Add(this.lblTipo, 4, 0);
            this.materiales.Controls.Add(this.cmbTipoMov, 5, 0);
            this.materiales.Controls.Add(this.txtBuscarMaterial, 6, 0);
            this.materiales.Controls.Add(this.btnExportar, 7, 0);
            this.materiales.Controls.Add(this.dgvHistorial, 0, 1);
            this.materiales.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(63)))), ((int)(((byte)(43)))));
            this.materiales.Location = new System.Drawing.Point(0, 80);
            this.materiales.Name = "materiales";
            this.materiales.Padding = new System.Windows.Forms.Padding(0, 0, 5, 0);
            this.materiales.RowCount = 2;
            this.materiales.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.materiales.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.materiales.Size = new System.Drawing.Size(796, 370);
            this.materiales.TabIndex = 37;
            // 
            // lblDesde
            // 
            this.lblDesde.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblDesde.AutoSize = true;
            this.lblDesde.Font = new System.Drawing.Font("Bahnschrift Light", 11.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblDesde.ForeColor = System.Drawing.Color.White;
            this.lblDesde.Location = new System.Drawing.Point(4, 11);
            this.lblDesde.Name = "lblDesde";
            this.lblDesde.Size = new System.Drawing.Size(55, 19);
            this.lblDesde.TabIndex = 0;
            this.lblDesde.Text = "Desde:";
            this.lblDesde.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // dtpDesde
            // 
            this.dtpDesde.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.dtpDesde.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(65)))));
            this.dtpDesde.Checked = true;
            this.dtpDesde.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(50)))), ((int)(((byte)(40)))));
            this.dtpDesde.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.dtpDesde.ForeColor = System.Drawing.Color.White;
            this.dtpDesde.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpDesde.HoverState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(63)))), ((int)(((byte)(43)))));
            this.dtpDesde.Location = new System.Drawing.Point(63, 5);
            this.dtpDesde.MaxDate = new System.DateTime(9998, 12, 31, 0, 0, 0, 0);
            this.dtpDesde.MinDate = new System.DateTime(1753, 1, 1, 0, 0, 0, 0);
            this.dtpDesde.Name = "dtpDesde";
            this.dtpDesde.Size = new System.Drawing.Size(120, 30);
            this.dtpDesde.TabIndex = 1;
            this.dtpDesde.Value = new System.DateTime(2024, 1, 1, 0, 0, 0, 0);
            // 
            // lblHasta
            // 
            this.lblHasta.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblHasta.AutoSize = true;
            this.lblHasta.Font = new System.Drawing.Font("Bahnschrift Light", 11.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblHasta.ForeColor = System.Drawing.Color.White;
            this.lblHasta.Location = new System.Drawing.Point(190, 11);
            this.lblHasta.Name = "lblHasta";
            this.lblHasta.Size = new System.Drawing.Size(53, 19);
            this.lblHasta.TabIndex = 2;
            this.lblHasta.Text = "Hasta:";
            this.lblHasta.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // dtpHasta
            // 
            this.dtpHasta.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.dtpHasta.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(65)))));
            this.dtpHasta.Checked = true;
            this.dtpHasta.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(50)))), ((int)(((byte)(40)))));
            this.dtpHasta.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.dtpHasta.ForeColor = System.Drawing.Color.White;
            this.dtpHasta.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpHasta.HoverState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(63)))), ((int)(((byte)(43)))));
            this.dtpHasta.Location = new System.Drawing.Point(247, 5);
            this.dtpHasta.MaxDate = new System.DateTime(9998, 12, 31, 0, 0, 0, 0);
            this.dtpHasta.MinDate = new System.DateTime(1753, 1, 1, 0, 0, 0, 0);
            this.dtpHasta.Name = "dtpHasta";
            this.dtpHasta.Size = new System.Drawing.Size(120, 30);
            this.dtpHasta.TabIndex = 3;
            this.dtpHasta.Value = new System.DateTime(2024, 1, 1, 0, 0, 0, 0);
            // 
            // lblTipo
            // 
            this.lblTipo.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblTipo.AutoSize = true;
            this.lblTipo.Font = new System.Drawing.Font("Bahnschrift Light", 11.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTipo.ForeColor = System.Drawing.Color.White;
            this.lblTipo.Location = new System.Drawing.Point(374, 11);
            this.lblTipo.Name = "lblTipo";
            this.lblTipo.Size = new System.Drawing.Size(43, 19);
            this.lblTipo.TabIndex = 4;
            this.lblTipo.Text = "Tipo:";
            this.lblTipo.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // cmbTipoMov
            // 
            this.cmbTipoMov.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.cmbTipoMov.BackColor = System.Drawing.Color.Transparent;
            this.cmbTipoMov.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(65)))));
            this.cmbTipoMov.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.cmbTipoMov.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbTipoMov.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(50)))), ((int)(((byte)(40)))));
            this.cmbTipoMov.FocusedColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(63)))), ((int)(((byte)(43)))));
            this.cmbTipoMov.FocusedState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(63)))), ((int)(((byte)(43)))));
            this.cmbTipoMov.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.cmbTipoMov.ForeColor = System.Drawing.Color.White;
            this.cmbTipoMov.HoverState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(63)))), ((int)(((byte)(43)))));
            this.cmbTipoMov.ItemHeight = 25;
            this.cmbTipoMov.Location = new System.Drawing.Point(421, 5);
            this.cmbTipoMov.Name = "cmbTipoMov";
            this.cmbTipoMov.Size = new System.Drawing.Size(130, 31);
            this.cmbTipoMov.TabIndex = 5;
            // 
            // txtBuscarMaterial
            // 
            this.txtBuscarMaterial.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.txtBuscarMaterial.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(65)))));
            this.txtBuscarMaterial.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txtBuscarMaterial.DefaultText = "";
            this.txtBuscarMaterial.DisabledState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.txtBuscarMaterial.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(226)))), ((int)(((byte)(226)))), ((int)(((byte)(226)))));
            this.txtBuscarMaterial.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.txtBuscarMaterial.DisabledState.PlaceholderForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.txtBuscarMaterial.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtBuscarMaterial.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(50)))), ((int)(((byte)(40)))));
            this.txtBuscarMaterial.FocusedState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(39)))), ((int)(((byte)(174)))), ((int)(((byte)(96)))));
            this.txtBuscarMaterial.Font = new System.Drawing.Font("Segoe UI", 10.75F);
            this.txtBuscarMaterial.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(230)))), ((int)(((byte)(230)))));
            this.txtBuscarMaterial.HoverState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(39)))), ((int)(((byte)(174)))), ((int)(((byte)(96)))));
            this.txtBuscarMaterial.Location = new System.Drawing.Point(558, 5);
            this.txtBuscarMaterial.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.txtBuscarMaterial.Name = "txtBuscarMaterial";
            this.txtBuscarMaterial.PlaceholderText = "Remito";
            this.txtBuscarMaterial.SelectedText = "";
            this.txtBuscarMaterial.Size = new System.Drawing.Size(100, 23);
            this.txtBuscarMaterial.TabIndex = 6;
            // 
            // btnBuscar
            // 
            this.btnBuscar.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBuscar.DisabledState.BorderColor = System.Drawing.Color.DarkGray;
            this.btnBuscar.DisabledState.CustomBorderColor = System.Drawing.Color.DarkGray;
            this.btnBuscar.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(169)))), ((int)(((byte)(169)))), ((int)(((byte)(169)))));
            this.btnBuscar.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(141)))), ((int)(((byte)(141)))), ((int)(((byte)(141)))));
            this.btnBuscar.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.btnBuscar.Font = new System.Drawing.Font("Bahnschrift Light", 11.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnBuscar.ForeColor = System.Drawing.Color.White;
            this.btnBuscar.Image = global::WP_Inventario.Properties.Resources.image__1_;
            this.btnBuscar.Location = new System.Drawing.Point(664, 5);
            this.btnBuscar.Name = "btnBuscar";
            this.btnBuscar.Size = new System.Drawing.Size(43, 30);
            this.btnBuscar.TabIndex = 7;
            this.btnBuscar.Click += new System.EventHandler(this.btnBuscar_Click);
            // 
            // btnExportar
            // 
            this.btnExportar.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnExportar.DisabledState.BorderColor = System.Drawing.Color.DarkGray;
            this.btnExportar.DisabledState.CustomBorderColor = System.Drawing.Color.DarkGray;
            this.btnExportar.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(169)))), ((int)(((byte)(169)))), ((int)(((byte)(169)))));
            this.btnExportar.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(141)))), ((int)(((byte)(141)))), ((int)(((byte)(141)))));
            this.btnExportar.FillColor = System.Drawing.Color.SeaGreen;
            this.btnExportar.Font = new System.Drawing.Font("Bahnschrift Light", 11.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnExportar.ForeColor = System.Drawing.Color.White;
            this.btnExportar.Location = new System.Drawing.Point(714, 5);
            this.btnExportar.Name = "btnExportar";
            this.btnExportar.Size = new System.Drawing.Size(55, 30);
            this.btnExportar.TabIndex = 8;
            this.btnExportar.Text = "Excel";
            this.btnExportar.Click += new System.EventHandler(this.btnExportar_Click);
            // 
            // dgvHistorial
            // 
            this.dgvHistorial.AllowUserToAddRows = false;
            this.dgvHistorial.AllowUserToDeleteRows = false;
            this.dgvHistorial.AllowUserToResizeColumns = false;
            this.dgvHistorial.AllowUserToResizeRows = false;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(44)))), ((int)(((byte)(48)))), ((int)(((byte)(52)))));
            this.dgvHistorial.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
            this.dgvHistorial.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(38)))), ((int)(((byte)(35)))));
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(16)))), ((int)(((byte)(18)))));
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvHistorial.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
            this.dgvHistorial.ColumnHeadersHeight = 35;
            this.dgvHistorial.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.EnableResizing;
            this.materiales.SetColumnSpan(this.dgvHistorial, 8);
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(37)))), ((int)(((byte)(41)))));
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Bahnschrift SemiBold SemiConden", 13.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle3.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(114)))), ((int)(((byte)(117)))), ((int)(((byte)(119)))));
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvHistorial.DefaultCellStyle = dataGridViewCellStyle3;
            this.dgvHistorial.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvHistorial.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(56)))), ((int)(((byte)(62)))));
            this.dgvHistorial.Location = new System.Drawing.Point(4, 45);
            this.dgvHistorial.Name = "dgvHistorial";
            this.dgvHistorial.ReadOnly = true;
            this.dgvHistorial.RowHeadersVisible = false;
            this.dgvHistorial.RowTemplate.DefaultCellStyle.Font = new System.Drawing.Font("Bahnschrift Light", 13.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.dgvHistorial.RowTemplate.Height = 30;
            this.dgvHistorial.Size = new System.Drawing.Size(783, 321);
            this.dgvHistorial.TabIndex = 22;
            this.dgvHistorial.Theme = Guna.UI2.WinForms.Enums.DataGridViewPresetThemes.Dark;
            this.dgvHistorial.ThemeStyle.AlternatingRowsStyle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(44)))), ((int)(((byte)(48)))), ((int)(((byte)(52)))));
            this.dgvHistorial.ThemeStyle.AlternatingRowsStyle.Font = null;
            this.dgvHistorial.ThemeStyle.AlternatingRowsStyle.ForeColor = System.Drawing.Color.Empty;
            this.dgvHistorial.ThemeStyle.AlternatingRowsStyle.SelectionBackColor = System.Drawing.Color.Empty;
            this.dgvHistorial.ThemeStyle.AlternatingRowsStyle.SelectionForeColor = System.Drawing.Color.Empty;
            this.dgvHistorial.ThemeStyle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(38)))), ((int)(((byte)(35)))));
            this.dgvHistorial.ThemeStyle.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(56)))), ((int)(((byte)(62)))));
            this.dgvHistorial.ThemeStyle.HeaderStyle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(16)))), ((int)(((byte)(18)))));
            this.dgvHistorial.ThemeStyle.HeaderStyle.BorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            this.dgvHistorial.ThemeStyle.HeaderStyle.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.dgvHistorial.ThemeStyle.HeaderStyle.ForeColor = System.Drawing.Color.White;
            this.dgvHistorial.ThemeStyle.HeaderStyle.HeaightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.EnableResizing;
            this.dgvHistorial.ThemeStyle.HeaderStyle.Height = 35;
            this.dgvHistorial.ThemeStyle.ReadOnly = true;
            this.dgvHistorial.ThemeStyle.RowsStyle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(37)))), ((int)(((byte)(41)))));
            this.dgvHistorial.ThemeStyle.RowsStyle.BorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            this.dgvHistorial.ThemeStyle.RowsStyle.Font = new System.Drawing.Font("Bahnschrift SemiBold SemiConden", 13.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.dgvHistorial.ThemeStyle.RowsStyle.ForeColor = System.Drawing.Color.White;
            this.dgvHistorial.ThemeStyle.RowsStyle.Height = 30;
            this.dgvHistorial.ThemeStyle.RowsStyle.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(114)))), ((int)(((byte)(117)))), ((int)(((byte)(119)))));
            this.dgvHistorial.ThemeStyle.RowsStyle.SelectionForeColor = System.Drawing.Color.White;
            // 
            // Vista_HistorialMovimientos
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(38)))), ((int)(((byte)(35)))));
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.materiales);
            this.Controls.Add(this.panelTitulo);
            this.Name = "Vista_HistorialMovimientos";
            this.Padding = new System.Windows.Forms.Padding(0, 0, 4, 0);
            this.Text = "Historial Movimientos";
            this.panelTitulo.ResumeLayout(false);
            this.panelTitulo.PerformLayout();
            this.materiales.ResumeLayout(false);
            this.materiales.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvHistorial)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private PanelAntiParpadeo panelTitulo;
        private System.Windows.Forms.Label labelTitle;
        private System.Windows.Forms.Label lblSubtitulo;
        private PanelAntiParpadeo materiales;
        private System.Windows.Forms.Label lblDesde;
        private Guna.UI2.WinForms.Guna2DateTimePicker dtpDesde;
        private System.Windows.Forms.Label lblHasta;
        private Guna.UI2.WinForms.Guna2DateTimePicker dtpHasta;
        private System.Windows.Forms.Label lblTipo;
        private Guna.UI2.WinForms.Guna2ComboBox cmbTipoMov;
        private System.Windows.Forms.Label lblBuscar;
        private Guna.UI2.WinForms.Guna2TextBox txtBuscarMaterial;
        private Guna.UI2.WinForms.Guna2Button btnBuscar;
        private Guna.UI2.WinForms.Guna2Button btnExportar;
        private Guna.UI2.WinForms.Guna2DataGridView dgvHistorial;
    }
}
