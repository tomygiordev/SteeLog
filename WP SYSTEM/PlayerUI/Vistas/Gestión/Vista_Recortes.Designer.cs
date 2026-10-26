namespace WP_Inventario.Vistas
{
    partial class Vista_Recortes
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            this.panelAntiParpadeo1 = new WP_Inventario.PanelAntiParpadeo();
            this.label1 = new System.Windows.Forms.Label();
            this.panelAntiParpadeo2 = new WP_Inventario.PanelAntiParpadeo();
            this.label2 = new System.Windows.Forms.Label();
            this.cmbCategorias = new Guna.UI2.WinForms.Guna2ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.cmbMateriales = new Guna.UI2.WinForms.Guna2ComboBox();
            this.btnGuardar = new Guna.UI2.WinForms.Guna2Button();
            this.btnEliminar = new Guna.UI2.WinForms.Guna2Button();
            this.panelAntiParpadeo3 = new WP_Inventario.PanelAntiParpadeo();
            this.label4 = new System.Windows.Forms.Label();
            this.txtAncho = new Guna.UI2.WinForms.Guna2TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.txtAltura = new Guna.UI2.WinForms.Guna2TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.txtCantidad = new Guna.UI2.WinForms.Guna2TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.cmbEstado = new Guna.UI2.WinForms.Guna2ComboBox();
            this.panelAntiParpadeo4 = new WP_Inventario.PanelAntiParpadeo();
            this.label7 = new System.Windows.Forms.Label();
            this.txtBusquedaRecortes = new Guna.UI2.WinForms.Guna2TextBox();
            this.guna2Panel1 = new Guna.UI2.WinForms.Guna2Panel();
            this.guna2VScrollBar1 = new Guna.UI2.WinForms.Guna2VScrollBar();
            this.dgvRecortes = new Guna.UI2.WinForms.Guna2DataGridView();
            this.panelAntiParpadeo1.SuspendLayout();
            this.panelAntiParpadeo2.SuspendLayout();
            this.panelAntiParpadeo3.SuspendLayout();
            this.panelAntiParpadeo4.SuspendLayout();
            this.guna2Panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvRecortes)).BeginInit();
            this.SuspendLayout();
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
            this.panelAntiParpadeo1.Size = new System.Drawing.Size(800, 42);
            this.panelAntiParpadeo1.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label1.Font = new System.Drawing.Font("Bahnschrift SemiBold", 13F, System.Drawing.FontStyle.Bold);
            this.label1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(210)))), ((int)(((byte)(235)))), ((int)(((byte)(218)))));
            this.label1.Location = new System.Drawing.Point(17, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(766, 42);
            this.label1.TabIndex = 0;
            this.label1.Text = "RECORTES";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // panelAntiParpadeo2
            // 
            this.panelAntiParpadeo2.ColumnCount = 6;
            this.panelAntiParpadeo2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 90F));
            this.panelAntiParpadeo2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F));
            this.panelAntiParpadeo2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 90F));
            this.panelAntiParpadeo2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F));
            this.panelAntiParpadeo2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.panelAntiParpadeo2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.panelAntiParpadeo2.Controls.Add(this.label2, 0, 0);
            this.panelAntiParpadeo2.Controls.Add(this.cmbCategorias, 1, 0);
            this.panelAntiParpadeo2.Controls.Add(this.label3, 2, 0);
            this.panelAntiParpadeo2.Controls.Add(this.cmbMateriales, 3, 0);
            this.panelAntiParpadeo2.Controls.Add(this.btnGuardar, 4, 0);
            this.panelAntiParpadeo2.Controls.Add(this.btnEliminar, 5, 0);
            this.panelAntiParpadeo2.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelAntiParpadeo2.GrowStyle = System.Windows.Forms.TableLayoutPanelGrowStyle.AddColumns;
            this.panelAntiParpadeo2.Location = new System.Drawing.Point(0, 42);
            this.panelAntiParpadeo2.Name = "panelAntiParpadeo2";
            this.panelAntiParpadeo2.Padding = new System.Windows.Forms.Padding(0, 8, 4, 8);
            this.panelAntiParpadeo2.RowCount = 1;
            this.panelAntiParpadeo2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.panelAntiParpadeo2.Size = new System.Drawing.Size(800, 52);
            this.panelAntiParpadeo2.TabIndex = 1;
            // 
            // label2
            // 
            this.label2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label2.Font = new System.Drawing.Font("Bahnschrift Light", 10.5F);
            this.label2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(155)))), ((int)(((byte)(185)))), ((int)(((byte)(165)))));
            this.label2.Location = new System.Drawing.Point(3, 8);
            this.label2.Name = "label2";
            this.label2.Padding = new System.Windows.Forms.Padding(14, 0, 6, 0);
            this.label2.Size = new System.Drawing.Size(84, 36);
            this.label2.TabIndex = 0;
            this.label2.Text = "Categoría:";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // cmbCategorias
            // 
            this.cmbCategorias.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbCategorias.BackColor = System.Drawing.Color.Transparent;
            this.cmbCategorias.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(75)))), ((int)(((byte)(62)))));
            this.cmbCategorias.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.cmbCategorias.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbCategorias.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(50)))), ((int)(((byte)(40)))));
            this.cmbCategorias.FocusedColor = System.Drawing.Color.FromArgb(((int)(((byte)(39)))), ((int)(((byte)(174)))), ((int)(((byte)(96)))));
            this.cmbCategorias.FocusedState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(39)))), ((int)(((byte)(174)))), ((int)(((byte)(96)))));
            this.cmbCategorias.Font = new System.Drawing.Font("Segoe UI", 10.5F);
            this.cmbCategorias.ForeColor = System.Drawing.Color.White;
            this.cmbCategorias.HoverState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(120)))), ((int)(((byte)(80)))));
            this.cmbCategorias.ItemHeight = 28;
            this.cmbCategorias.Location = new System.Drawing.Point(93, 10);
            this.cmbCategorias.Margin = new System.Windows.Forms.Padding(3, 2, 8, 2);
            this.cmbCategorias.Name = "cmbCategorias";
            this.cmbCategorias.Size = new System.Drawing.Size(173, 34);
            this.cmbCategorias.TabIndex = 1;
            // 
            // label3
            // 
            this.label3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label3.Font = new System.Drawing.Font("Bahnschrift Light", 10.5F);
            this.label3.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(155)))), ((int)(((byte)(185)))), ((int)(((byte)(165)))));
            this.label3.Location = new System.Drawing.Point(277, 8);
            this.label3.Name = "label3";
            this.label3.Padding = new System.Windows.Forms.Padding(14, 0, 6, 0);
            this.label3.Size = new System.Drawing.Size(84, 36);
            this.label3.TabIndex = 2;
            this.label3.Text = "Material:";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // cmbMateriales
            // 
            this.cmbMateriales.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbMateriales.BackColor = System.Drawing.Color.Transparent;
            this.cmbMateriales.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(75)))), ((int)(((byte)(62)))));
            this.cmbMateriales.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.cmbMateriales.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbMateriales.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(50)))), ((int)(((byte)(40)))));
            this.cmbMateriales.FocusedColor = System.Drawing.Color.FromArgb(((int)(((byte)(39)))), ((int)(((byte)(174)))), ((int)(((byte)(96)))));
            this.cmbMateriales.FocusedState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(39)))), ((int)(((byte)(174)))), ((int)(((byte)(96)))));
            this.cmbMateriales.Font = new System.Drawing.Font("Segoe UI", 10.5F);
            this.cmbMateriales.ForeColor = System.Drawing.Color.White;
            this.cmbMateriales.HoverState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(120)))), ((int)(((byte)(80)))));
            this.cmbMateriales.ItemHeight = 28;
            this.cmbMateriales.Location = new System.Drawing.Point(367, 10);
            this.cmbMateriales.Margin = new System.Windows.Forms.Padding(3, 2, 8, 2);
            this.cmbMateriales.Name = "cmbMateriales";
            this.cmbMateriales.Size = new System.Drawing.Size(173, 34);
            this.cmbMateriales.TabIndex = 3;
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
            this.btnGuardar.Location = new System.Drawing.Point(552, 9);
            this.btnGuardar.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.btnGuardar.Name = "btnGuardar";
            this.btnGuardar.Size = new System.Drawing.Size(115, 34);
            this.btnGuardar.TabIndex = 4;
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
            this.btnEliminar.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(158)))), ((int)(((byte)(45)))), ((int)(((byte)(40)))));
            this.btnEliminar.Font = new System.Drawing.Font("Bahnschrift Light", 11F);
            this.btnEliminar.ForeColor = System.Drawing.Color.White;
            this.btnEliminar.HoverState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(55)))), ((int)(((byte)(48)))));
            this.btnEliminar.Location = new System.Drawing.Point(675, 9);
            this.btnEliminar.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.btnEliminar.Name = "btnEliminar";
            this.btnEliminar.Size = new System.Drawing.Size(117, 34);
            this.btnEliminar.TabIndex = 5;
            this.btnEliminar.Text = "Eliminar";
            this.btnEliminar.Click += new System.EventHandler(this.btnEliminar_Click);
            // 
            // panelAntiParpadeo3
            // 
            this.panelAntiParpadeo3.ColumnCount = 8;
            this.panelAntiParpadeo3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 90F));
            this.panelAntiParpadeo3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 24F));
            this.panelAntiParpadeo3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 90F));
            this.panelAntiParpadeo3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 24F));
            this.panelAntiParpadeo3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 90F));
            this.panelAntiParpadeo3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16F));
            this.panelAntiParpadeo3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 80F));
            this.panelAntiParpadeo3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 36F));
            this.panelAntiParpadeo3.Controls.Add(this.label4, 0, 0);
            this.panelAntiParpadeo3.Controls.Add(this.txtAncho, 1, 0);
            this.panelAntiParpadeo3.Controls.Add(this.label5, 2, 0);
            this.panelAntiParpadeo3.Controls.Add(this.txtAltura, 3, 0);
            this.panelAntiParpadeo3.Controls.Add(this.label6, 4, 0);
            this.panelAntiParpadeo3.Controls.Add(this.txtCantidad, 5, 0);
            this.panelAntiParpadeo3.Controls.Add(this.label8, 6, 0);
            this.panelAntiParpadeo3.Controls.Add(this.cmbEstado, 7, 0);
            this.panelAntiParpadeo3.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelAntiParpadeo3.GrowStyle = System.Windows.Forms.TableLayoutPanelGrowStyle.AddColumns;
            this.panelAntiParpadeo3.Location = new System.Drawing.Point(0, 94);
            this.panelAntiParpadeo3.Name = "panelAntiParpadeo3";
            this.panelAntiParpadeo3.Padding = new System.Windows.Forms.Padding(0, 8, 4, 8);
            this.panelAntiParpadeo3.RowCount = 1;
            this.panelAntiParpadeo3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.panelAntiParpadeo3.Size = new System.Drawing.Size(800, 52);
            this.panelAntiParpadeo3.TabIndex = 2;
            // 
            // label4
            // 
            this.label4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label4.Font = new System.Drawing.Font("Bahnschrift Light", 10.5F);
            this.label4.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(155)))), ((int)(((byte)(185)))), ((int)(((byte)(165)))));
            this.label4.Location = new System.Drawing.Point(3, 8);
            this.label4.Name = "label4";
            this.label4.Padding = new System.Windows.Forms.Padding(14, 0, 6, 0);
            this.label4.Size = new System.Drawing.Size(84, 36);
            this.label4.TabIndex = 0;
            this.label4.Text = "Ancho (m):";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtAncho
            // 
            this.txtAncho.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtAncho.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.txtAncho.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(75)))), ((int)(((byte)(62)))));
            this.txtAncho.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txtAncho.DefaultText = "";
            this.txtAncho.DisabledState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this.txtAncho.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(55)))), ((int)(((byte)(44)))));
            this.txtAncho.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(100)))), ((int)(((byte)(100)))));
            this.txtAncho.DisabledState.PlaceholderForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(100)))), ((int)(((byte)(100)))));
            this.txtAncho.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(50)))), ((int)(((byte)(40)))));
            this.txtAncho.FocusedState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(39)))), ((int)(((byte)(174)))), ((int)(((byte)(96)))));
            this.txtAncho.Font = new System.Drawing.Font("Segoe UI", 10.5F);
            this.txtAncho.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(225)))), ((int)(((byte)(225)))), ((int)(((byte)(225)))));
            this.txtAncho.HoverState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(120)))), ((int)(((byte)(80)))));
            this.txtAncho.Location = new System.Drawing.Point(93, 11);
            this.txtAncho.Margin = new System.Windows.Forms.Padding(3, 2, 8, 2);
            this.txtAncho.Name = "txtAncho";
            this.txtAncho.PlaceholderText = "ej: 1.5";
            this.txtAncho.SelectedText = "";
            this.txtAncho.Size = new System.Drawing.Size(96, 30);
            this.txtAncho.TabIndex = 1;
            // 
            // label5
            // 
            this.label5.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label5.Font = new System.Drawing.Font("Bahnschrift Light", 10.5F);
            this.label5.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(155)))), ((int)(((byte)(185)))), ((int)(((byte)(165)))));
            this.label5.Location = new System.Drawing.Point(200, 8);
            this.label5.Name = "label5";
            this.label5.Padding = new System.Windows.Forms.Padding(14, 0, 6, 0);
            this.label5.Size = new System.Drawing.Size(84, 36);
            this.label5.TabIndex = 2;
            this.label5.Text = "Altura (m):";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtAltura
            // 
            this.txtAltura.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtAltura.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.txtAltura.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(75)))), ((int)(((byte)(62)))));
            this.txtAltura.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txtAltura.DefaultText = "";
            this.txtAltura.DisabledState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this.txtAltura.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(55)))), ((int)(((byte)(44)))));
            this.txtAltura.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(100)))), ((int)(((byte)(100)))));
            this.txtAltura.DisabledState.PlaceholderForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(100)))), ((int)(((byte)(100)))));
            this.txtAltura.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(50)))), ((int)(((byte)(40)))));
            this.txtAltura.FocusedState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(39)))), ((int)(((byte)(174)))), ((int)(((byte)(96)))));
            this.txtAltura.Font = new System.Drawing.Font("Segoe UI", 10.5F);
            this.txtAltura.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(225)))), ((int)(((byte)(225)))), ((int)(((byte)(225)))));
            this.txtAltura.HoverState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(120)))), ((int)(((byte)(80)))));
            this.txtAltura.Location = new System.Drawing.Point(290, 11);
            this.txtAltura.Margin = new System.Windows.Forms.Padding(3, 2, 8, 2);
            this.txtAltura.Name = "txtAltura";
            this.txtAltura.PlaceholderText = "ej: 2.0";
            this.txtAltura.SelectedText = "";
            this.txtAltura.Size = new System.Drawing.Size(96, 30);
            this.txtAltura.TabIndex = 3;
            // 
            // label6
            // 
            this.label6.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label6.Font = new System.Drawing.Font("Bahnschrift Light", 10.5F);
            this.label6.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(155)))), ((int)(((byte)(185)))), ((int)(((byte)(165)))));
            this.label6.Location = new System.Drawing.Point(397, 8);
            this.label6.Name = "label6";
            this.label6.Padding = new System.Windows.Forms.Padding(14, 0, 6, 0);
            this.label6.Size = new System.Drawing.Size(84, 36);
            this.label6.TabIndex = 4;
            this.label6.Text = "Cantidad:";
            this.label6.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtCantidad
            // 
            this.txtCantidad.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtCantidad.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.txtCantidad.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(75)))), ((int)(((byte)(62)))));
            this.txtCantidad.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txtCantidad.DefaultText = "1";
            this.txtCantidad.DisabledState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this.txtCantidad.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(55)))), ((int)(((byte)(44)))));
            this.txtCantidad.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(100)))), ((int)(((byte)(100)))));
            this.txtCantidad.DisabledState.PlaceholderForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(100)))), ((int)(((byte)(100)))));
            this.txtCantidad.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(50)))), ((int)(((byte)(40)))));
            this.txtCantidad.FocusedState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(39)))), ((int)(((byte)(174)))), ((int)(((byte)(96)))));
            this.txtCantidad.Font = new System.Drawing.Font("Segoe UI", 10.5F);
            this.txtCantidad.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(225)))), ((int)(((byte)(225)))), ((int)(((byte)(225)))));
            this.txtCantidad.HoverState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(120)))), ((int)(((byte)(80)))));
            this.txtCantidad.Location = new System.Drawing.Point(487, 11);
            this.txtCantidad.Margin = new System.Windows.Forms.Padding(3, 2, 8, 2);
            this.txtCantidad.Name = "txtCantidad";
            this.txtCantidad.PlaceholderText = "ej: 1";
            this.txtCantidad.SelectedText = "";
            this.txtCantidad.Size = new System.Drawing.Size(60, 30);
            this.txtCantidad.TabIndex = 5;
            this.txtCantidad.TextChanged += new System.EventHandler(this.txtCantidad_TextChanged);
            // 
            // label8
            // 
            this.label8.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label8.Font = new System.Drawing.Font("Bahnschrift Light", 10.5F);
            this.label8.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(155)))), ((int)(((byte)(185)))), ((int)(((byte)(165)))));
            this.label8.Location = new System.Drawing.Point(558, 8);
            this.label8.Name = "label8";
            this.label8.Padding = new System.Windows.Forms.Padding(14, 0, 6, 0);
            this.label8.Size = new System.Drawing.Size(74, 36);
            this.label8.TabIndex = 6;
            this.label8.Text = "Estado:";
            this.label8.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // cmbEstado
            // 
            this.cmbEstado.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbEstado.BackColor = System.Drawing.Color.Transparent;
            this.cmbEstado.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(75)))), ((int)(((byte)(62)))));
            this.cmbEstado.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.cmbEstado.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbEstado.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(50)))), ((int)(((byte)(40)))));
            this.cmbEstado.FocusedColor = System.Drawing.Color.FromArgb(((int)(((byte)(39)))), ((int)(((byte)(174)))), ((int)(((byte)(96)))));
            this.cmbEstado.FocusedState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(39)))), ((int)(((byte)(174)))), ((int)(((byte)(96)))));
            this.cmbEstado.Font = new System.Drawing.Font("Segoe UI", 10.5F);
            this.cmbEstado.ForeColor = System.Drawing.Color.White;
            this.cmbEstado.HoverState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(120)))), ((int)(((byte)(80)))));
            this.cmbEstado.ItemHeight = 28;
            this.cmbEstado.Location = new System.Drawing.Point(638, 10);
            this.cmbEstado.Margin = new System.Windows.Forms.Padding(3, 2, 8, 2);
            this.cmbEstado.Name = "cmbEstado";
            this.cmbEstado.Size = new System.Drawing.Size(150, 34);
            this.cmbEstado.TabIndex = 7;
            // 
            // panelAntiParpadeo4
            // 
            this.panelAntiParpadeo4.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(34)))), ((int)(((byte)(30)))));
            this.panelAntiParpadeo4.ColumnCount = 2;
            this.panelAntiParpadeo4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.panelAntiParpadeo4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 280F));
            this.panelAntiParpadeo4.Controls.Add(this.label7, 0, 0);
            this.panelAntiParpadeo4.Controls.Add(this.txtBusquedaRecortes, 1, 0);
            this.panelAntiParpadeo4.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelAntiParpadeo4.GrowStyle = System.Windows.Forms.TableLayoutPanelGrowStyle.AddColumns;
            this.panelAntiParpadeo4.Location = new System.Drawing.Point(0, 146);
            this.panelAntiParpadeo4.Margin = new System.Windows.Forms.Padding(0);
            this.panelAntiParpadeo4.Name = "panelAntiParpadeo4";
            this.panelAntiParpadeo4.Padding = new System.Windows.Forms.Padding(14, 0, 14, 0);
            this.panelAntiParpadeo4.RowCount = 1;
            this.panelAntiParpadeo4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.panelAntiParpadeo4.Size = new System.Drawing.Size(800, 36);
            this.panelAntiParpadeo4.TabIndex = 3;
            // 
            // label7
            // 
            this.label7.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("Bahnschrift SemiBold", 10F, System.Drawing.FontStyle.Bold);
            this.label7.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(145)))), ((int)(((byte)(175)))), ((int)(((byte)(158)))));
            this.label7.Location = new System.Drawing.Point(17, 9);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(156, 17);
            this.label7.TabIndex = 0;
            this.label7.Text = "LISTADO DE RECORTES";
            this.label7.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtBusquedaRecortes
            // 
            this.txtBusquedaRecortes.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtBusquedaRecortes.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(75)))), ((int)(((byte)(62)))));
            this.txtBusquedaRecortes.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txtBusquedaRecortes.DefaultText = "";
            this.txtBusquedaRecortes.DisabledState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this.txtBusquedaRecortes.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(55)))), ((int)(((byte)(44)))));
            this.txtBusquedaRecortes.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(100)))), ((int)(((byte)(100)))));
            this.txtBusquedaRecortes.DisabledState.PlaceholderForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(100)))), ((int)(((byte)(100)))));
            this.txtBusquedaRecortes.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(50)))), ((int)(((byte)(40)))));
            this.txtBusquedaRecortes.FocusedState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(39)))), ((int)(((byte)(174)))), ((int)(((byte)(96)))));
            this.txtBusquedaRecortes.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtBusquedaRecortes.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(225)))), ((int)(((byte)(225)))), ((int)(((byte)(225)))));
            this.txtBusquedaRecortes.HoverState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(120)))), ((int)(((byte)(80)))));
            this.txtBusquedaRecortes.Location = new System.Drawing.Point(523, 3);
            this.txtBusquedaRecortes.Margin = new System.Windows.Forms.Padding(3, 3, 0, 3);
            this.txtBusquedaRecortes.Name = "txtBusquedaRecortes";
            this.txtBusquedaRecortes.PlaceholderForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(150)))), ((int)(((byte)(170)))), ((int)(((byte)(156)))));
            this.txtBusquedaRecortes.PlaceholderText = "Buscar material...";
            this.txtBusquedaRecortes.SelectedText = "";
            this.txtBusquedaRecortes.Size = new System.Drawing.Size(263, 30);
            this.txtBusquedaRecortes.TabIndex = 1;
            // 
            // guna2Panel1
            // 
            this.guna2Panel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.guna2Panel1.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(52)))), ((int)(((byte)(44)))));
            this.guna2Panel1.Controls.Add(this.guna2VScrollBar1);
            this.guna2Panel1.Controls.Add(this.dgvRecortes);
            this.guna2Panel1.Location = new System.Drawing.Point(0, 182);
            this.guna2Panel1.Name = "guna2Panel1";
            this.guna2Panel1.Size = new System.Drawing.Size(800, 268);
            this.guna2Panel1.TabIndex = 4;
            // 
            // guna2VScrollBar1
            // 
            this.guna2VScrollBar1.BindingContainer = this.dgvRecortes;
            this.guna2VScrollBar1.Dock = System.Windows.Forms.DockStyle.Right;
            this.guna2VScrollBar1.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(50)))), ((int)(((byte)(40)))));
            this.guna2VScrollBar1.InUpdate = false;
            this.guna2VScrollBar1.LargeChange = 10;
            this.guna2VScrollBar1.Location = new System.Drawing.Point(782, 0);
            this.guna2VScrollBar1.Minimum = 1;
            this.guna2VScrollBar1.Name = "guna2VScrollBar1";
            this.guna2VScrollBar1.ScrollbarSize = 18;
            this.guna2VScrollBar1.Size = new System.Drawing.Size(18, 268);
            this.guna2VScrollBar1.TabIndex = 1;
            this.guna2VScrollBar1.ThumbColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(85)))), ((int)(((byte)(65)))));
            this.guna2VScrollBar1.Value = 1;
            // 
            // dgvRecortes
            // 
            this.dgvRecortes.AllowUserToAddRows = false;
            this.dgvRecortes.AllowUserToDeleteRows = false;
            this.dgvRecortes.AllowUserToResizeColumns = false;
            this.dgvRecortes.AllowUserToResizeRows = false;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(38)))), ((int)(((byte)(35)))));
            this.dgvRecortes.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
            this.dgvRecortes.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(38)))), ((int)(((byte)(35)))));
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(55)))), ((int)(((byte)(40)))));
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Bahnschrift Light", 10.5F);
            dataGridViewCellStyle2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(195)))), ((int)(((byte)(215)))), ((int)(((byte)(202)))));
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(55)))), ((int)(((byte)(40)))));
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(195)))), ((int)(((byte)(215)))), ((int)(((byte)(202)))));
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvRecortes.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
            this.dgvRecortes.ColumnHeadersHeight = 32;
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(38)))), ((int)(((byte)(35)))));
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Bahnschrift Light", 11F);
            dataGridViewCellStyle3.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle3.Padding = new System.Windows.Forms.Padding(14, 0, 14, 0);
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(47)))), ((int)(((byte)(84)))), ((int)(((byte)(57)))));
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvRecortes.DefaultCellStyle = dataGridViewCellStyle3;
            this.dgvRecortes.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvRecortes.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(50)))), ((int)(((byte)(43)))));
            this.dgvRecortes.Location = new System.Drawing.Point(0, 0);
            this.dgvRecortes.Name = "dgvRecortes";
            this.dgvRecortes.ReadOnly = true;
            this.dgvRecortes.RowHeadersVisible = false;
            this.dgvRecortes.RowTemplate.DefaultCellStyle.Font = new System.Drawing.Font("Bahnschrift Light", 11F);
            this.dgvRecortes.RowTemplate.Height = 36;
            this.dgvRecortes.Size = new System.Drawing.Size(800, 268);
            this.dgvRecortes.TabIndex = 0;
            this.dgvRecortes.Theme = Guna.UI2.WinForms.Enums.DataGridViewPresetThemes.Dark;
            this.dgvRecortes.ThemeStyle.AlternatingRowsStyle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(38)))), ((int)(((byte)(35)))));
            this.dgvRecortes.ThemeStyle.AlternatingRowsStyle.Font = null;
            this.dgvRecortes.ThemeStyle.AlternatingRowsStyle.ForeColor = System.Drawing.Color.Empty;
            this.dgvRecortes.ThemeStyle.AlternatingRowsStyle.SelectionBackColor = System.Drawing.Color.Empty;
            this.dgvRecortes.ThemeStyle.AlternatingRowsStyle.SelectionForeColor = System.Drawing.Color.Empty;
            this.dgvRecortes.ThemeStyle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(38)))), ((int)(((byte)(35)))));
            this.dgvRecortes.ThemeStyle.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(50)))), ((int)(((byte)(43)))));
            this.dgvRecortes.ThemeStyle.HeaderStyle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(55)))), ((int)(((byte)(40)))));
            this.dgvRecortes.ThemeStyle.HeaderStyle.BorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            this.dgvRecortes.ThemeStyle.HeaderStyle.Font = new System.Drawing.Font("Bahnschrift Light", 10.5F);
            this.dgvRecortes.ThemeStyle.HeaderStyle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(195)))), ((int)(((byte)(215)))), ((int)(((byte)(202)))));
            this.dgvRecortes.ThemeStyle.HeaderStyle.HeaightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.dgvRecortes.ThemeStyle.HeaderStyle.Height = 32;
            this.dgvRecortes.ThemeStyle.ReadOnly = true;
            this.dgvRecortes.ThemeStyle.RowsStyle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(38)))), ((int)(((byte)(35)))));
            this.dgvRecortes.ThemeStyle.RowsStyle.BorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            this.dgvRecortes.ThemeStyle.RowsStyle.Font = new System.Drawing.Font("Bahnschrift Light", 11F);
            this.dgvRecortes.ThemeStyle.RowsStyle.ForeColor = System.Drawing.Color.White;
            this.dgvRecortes.ThemeStyle.RowsStyle.Height = 36;
            this.dgvRecortes.ThemeStyle.RowsStyle.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(47)))), ((int)(((byte)(84)))), ((int)(((byte)(57)))));
            this.dgvRecortes.ThemeStyle.RowsStyle.SelectionForeColor = System.Drawing.Color.White;
            // 
            // Vista_Recortes
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(38)))), ((int)(((byte)(35)))));
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.guna2Panel1);
            this.Controls.Add(this.panelAntiParpadeo4);
            this.Controls.Add(this.panelAntiParpadeo3);
            this.Controls.Add(this.panelAntiParpadeo2);
            this.Controls.Add(this.panelAntiParpadeo1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "Vista_Recortes";
            this.Text = "Gestión de Recortes";
            this.panelAntiParpadeo1.ResumeLayout(false);
            this.panelAntiParpadeo2.ResumeLayout(false);
            this.panelAntiParpadeo3.ResumeLayout(false);
            this.panelAntiParpadeo4.ResumeLayout(false);
            this.panelAntiParpadeo4.PerformLayout();
            this.guna2Panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvRecortes)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private WP_Inventario.PanelAntiParpadeo panelAntiParpadeo1;
        private System.Windows.Forms.Label label1;
        private WP_Inventario.PanelAntiParpadeo panelAntiParpadeo2;
        private System.Windows.Forms.Label label2;
        private Guna.UI2.WinForms.Guna2ComboBox cmbCategorias;
        private System.Windows.Forms.Label label3;
        private Guna.UI2.WinForms.Guna2ComboBox cmbMateriales;
        private Guna.UI2.WinForms.Guna2Button btnGuardar;
        private Guna.UI2.WinForms.Guna2Button btnEliminar;
        private WP_Inventario.PanelAntiParpadeo panelAntiParpadeo3;
        private System.Windows.Forms.Label label4;
        private Guna.UI2.WinForms.Guna2TextBox txtAncho;
        private System.Windows.Forms.Label label5;
        private Guna.UI2.WinForms.Guna2TextBox txtAltura;
        private System.Windows.Forms.Label label6;
        private Guna.UI2.WinForms.Guna2TextBox txtCantidad;
        private System.Windows.Forms.Label label8;
        private Guna.UI2.WinForms.Guna2ComboBox cmbEstado;
        private WP_Inventario.PanelAntiParpadeo panelAntiParpadeo4;
        private System.Windows.Forms.Label label7;
        private Guna.UI2.WinForms.Guna2TextBox txtBusquedaRecortes;
        private Guna.UI2.WinForms.Guna2Panel guna2Panel1;
        private Guna.UI2.WinForms.Guna2VScrollBar guna2VScrollBar1;
        private Guna.UI2.WinForms.Guna2DataGridView dgvRecortes;
    }
}
