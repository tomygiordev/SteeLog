namespace WP_Inventario.Vistas
{
    partial class Vista_Herramientas
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle6 = new System.Windows.Forms.DataGridViewCellStyle();
            this.panelTitulo = new WP_Inventario.PanelAntiParpadeo();
            this.lblTitulo = new System.Windows.Forms.Label();
            this.panelEntrada = new WP_Inventario.PanelAntiParpadeo();
            this.lblNombre = new System.Windows.Forms.Label();
            this.txtNombre = new Guna.UI2.WinForms.Guna2TextBox();
            this.lblEstado = new System.Windows.Forms.Label();
            this.cboEstado = new Guna.UI2.WinForms.Guna2ComboBox();
            this.lblObra = new System.Windows.Forms.Label();
            this.cboObra = new Guna.UI2.WinForms.Guna2ComboBox();
            this.panelEntrada2 = new WP_Inventario.PanelAntiParpadeo();
            this.lblObservaciones = new System.Windows.Forms.Label();
            this.txtObservaciones = new Guna.UI2.WinForms.Guna2TextBox();
            this.btnGuardar = new Guna.UI2.WinForms.Guna2Button();
            this.btnEliminar = new Guna.UI2.WinForms.Guna2Button();
            this.btnPDF = new Guna.UI2.WinForms.Guna2Button();
            this.panelTabs = new WP_Inventario.PanelAntiParpadeo();
            this.btnVerHerramientas = new Guna.UI2.WinForms.Guna2Button();
            this.btnVerHistorial = new Guna.UI2.WinForms.Guna2Button();
            this.panelGrid = new Guna.UI2.WinForms.Guna2Panel();
            this.guna2VScrollBar1 = new Guna.UI2.WinForms.Guna2VScrollBar();
            this.dgvHerramientas = new Guna.UI2.WinForms.Guna2DataGridView();
            this.panelGridHistorial = new Guna.UI2.WinForms.Guna2Panel();
            this.guna2VScrollBar2 = new Guna.UI2.WinForms.Guna2VScrollBar();
            this.dgvHistorial = new Guna.UI2.WinForms.Guna2DataGridView();
            this.panelFiltro = new WP_Inventario.PanelAntiParpadeo();
            this.lblFiltro = new System.Windows.Forms.Label();
            this.cboFiltroHerramienta = new Guna.UI2.WinForms.Guna2ComboBox();
            this.lblBuscar = new System.Windows.Forms.Label();
            this.txtBuscar = new Guna.UI2.WinForms.Guna2TextBox();
            this.lblFiltroEstado = new System.Windows.Forms.Label();
            this.cboFiltroEstado = new Guna.UI2.WinForms.Guna2ComboBox();
            this.lblFiltroObra = new System.Windows.Forms.Label();
            this.cboFiltroObra = new Guna.UI2.WinForms.Guna2ComboBox();
            this.panelTitulo.SuspendLayout();
            this.panelEntrada.SuspendLayout();
            this.panelEntrada2.SuspendLayout();
            this.panelTabs.SuspendLayout();
            this.panelFiltro.SuspendLayout();
            this.panelGrid.SuspendLayout();
            this.panelGridHistorial.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvHerramientas)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvHistorial)).BeginInit();
            this.SuspendLayout();
            //
            // panelTitulo
            //
            this.panelTitulo.BackColor = System.Drawing.Color.FromArgb(32, 55, 40);
            this.panelTitulo.ColumnCount = 1;
            this.panelTitulo.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.panelTitulo.Controls.Add(this.lblTitulo, 0, 0);
            this.panelTitulo.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTitulo.GrowStyle = System.Windows.Forms.TableLayoutPanelGrowStyle.AddColumns;
            this.panelTitulo.Location = new System.Drawing.Point(0, 0);
            this.panelTitulo.Name = "panelTitulo";
            this.panelTitulo.Padding = new System.Windows.Forms.Padding(14, 0, 14, 0);
            this.panelTitulo.RowCount = 1;
            this.panelTitulo.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.panelTitulo.Size = new System.Drawing.Size(800, 42);
            this.panelTitulo.TabIndex = 0;
            //
            // lblTitulo
            //
            this.lblTitulo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTitulo.Font = new System.Drawing.Font("Bahnschrift SemiBold", 13F, System.Drawing.FontStyle.Bold);
            this.lblTitulo.ForeColor = System.Drawing.Color.FromArgb(210, 235, 218);
            this.lblTitulo.Location = new System.Drawing.Point(17, 0);
            this.lblTitulo.Name = "lblTitulo";
            this.lblTitulo.Size = new System.Drawing.Size(766, 42);
            this.lblTitulo.TabIndex = 0;
            this.lblTitulo.Text = "HERRAMIENTAS";
            this.lblTitulo.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // panelEntrada
            // 
            this.panelEntrada.ColumnCount = 6;
            this.panelEntrada.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.panelEntrada.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 35F));
            this.panelEntrada.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.panelEntrada.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F));
            this.panelEntrada.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.panelEntrada.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 35F));
            this.panelEntrada.Controls.Add(this.lblNombre, 0, 0);
            this.panelEntrada.Controls.Add(this.txtNombre, 1, 0);
            this.panelEntrada.Controls.Add(this.lblEstado, 2, 0);
            this.panelEntrada.Controls.Add(this.cboEstado, 3, 0);
            this.panelEntrada.Controls.Add(this.lblObra, 4, 0);
            this.panelEntrada.Controls.Add(this.cboObra, 5, 0);
            this.panelEntrada.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelEntrada.GrowStyle = System.Windows.Forms.TableLayoutPanelGrowStyle.AddColumns;
            this.panelEntrada.Location = new System.Drawing.Point(0, 42);
            this.panelEntrada.Name = "panelEntrada";
            this.panelEntrada.Padding = new System.Windows.Forms.Padding(14, 0, 14, 0);
            this.panelEntrada.RowCount = 1;
            this.panelEntrada.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.panelEntrada.Size = new System.Drawing.Size(800, 50);
            this.panelEntrada.TabIndex = 1;
            //
            // lblNombre
            //
            this.lblNombre.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNombre.Font = new System.Drawing.Font("Bahnschrift Light", 10.5F);
            this.lblNombre.ForeColor = System.Drawing.Color.FromArgb(155, 185, 165);
            this.lblNombre.Location = new System.Drawing.Point(3, 0);
            this.lblNombre.Name = "lblNombre";
            this.lblNombre.Padding = new System.Windows.Forms.Padding(14, 0, 6, 0);
            this.lblNombre.Size = new System.Drawing.Size(84, 50);
            this.lblNombre.TabIndex = 0;
            this.lblNombre.Text = "Nombre:";
            this.lblNombre.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtNombre
            // 
            this.txtNombre.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtNombre.BorderColor = System.Drawing.Color.FromArgb(55, 75, 62);
            this.txtNombre.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txtNombre.DefaultText = "";
            this.txtNombre.DisabledState.BorderColor = System.Drawing.Color.FromArgb(60, 60, 60);
            this.txtNombre.DisabledState.FillColor = System.Drawing.Color.FromArgb(40, 55, 44);
            this.txtNombre.DisabledState.ForeColor = System.Drawing.Color.FromArgb(100, 100, 100);
            this.txtNombre.DisabledState.PlaceholderForeColor = System.Drawing.Color.FromArgb(100, 100, 100);
            this.txtNombre.FillColor = System.Drawing.Color.FromArgb(35, 50, 40);
            this.txtNombre.FocusedState.BorderColor = System.Drawing.Color.FromArgb(39, 174, 96);
            this.txtNombre.Font = new System.Drawing.Font("Segoe UI", 10.5F);
            this.txtNombre.ForeColor = System.Drawing.Color.FromArgb(225, 225, 225);
            this.txtNombre.HoverState.BorderColor = System.Drawing.Color.FromArgb(60, 120, 80);
            this.txtNombre.Location = new System.Drawing.Point(101, 8);
            this.txtNombre.Name = "txtNombre";
            this.txtNombre.PlaceholderText = "Ej: Taladro Percutor";
            this.txtNombre.SelectedText = "";
            this.txtNombre.Size = new System.Drawing.Size(190, 34);
            this.txtNombre.TabIndex = 1;
            //
            // lblEstado
            //
            this.lblEstado.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblEstado.Font = new System.Drawing.Font("Bahnschrift Light", 10.5F);
            this.lblEstado.ForeColor = System.Drawing.Color.FromArgb(155, 185, 165);
            this.lblEstado.Location = new System.Drawing.Point(297, 0);
            this.lblEstado.Name = "lblEstado";
            this.lblEstado.Padding = new System.Windows.Forms.Padding(14, 0, 6, 0);
            this.lblEstado.Size = new System.Drawing.Size(64, 50);
            this.lblEstado.TabIndex = 2;
            this.lblEstado.Text = "Estado:";
            this.lblEstado.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // cboEstado
            //
            this.cboEstado.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.cboEstado.BackColor = System.Drawing.Color.Transparent;
            this.cboEstado.BorderColor = System.Drawing.Color.FromArgb(55, 75, 62);
            this.cboEstado.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.cboEstado.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboEstado.FillColor = System.Drawing.Color.FromArgb(35, 50, 40);
            this.cboEstado.FocusedColor = System.Drawing.Color.FromArgb(39, 174, 96);
            this.cboEstado.FocusedState.BorderColor = System.Drawing.Color.FromArgb(39, 174, 96);
            this.cboEstado.Font = new System.Drawing.Font("Segoe UI", 10.5F);
            this.cboEstado.ForeColor = System.Drawing.Color.FromArgb(225, 225, 225);
            this.cboEstado.HoverState.BorderColor = System.Drawing.Color.FromArgb(60, 120, 80);
            this.cboEstado.ItemHeight = 28;
            this.cboEstado.Location = new System.Drawing.Point(367, 8);
            this.cboEstado.Margin = new System.Windows.Forms.Padding(3, 2, 8, 2);
            this.cboEstado.Name = "cboEstado";
            this.cboEstado.Size = new System.Drawing.Size(155, 34);
            this.cboEstado.TabIndex = 3;
            //
            // lblObra
            //
            this.lblObra.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblObra.Font = new System.Drawing.Font("Bahnschrift Light", 10.5F);
            this.lblObra.ForeColor = System.Drawing.Color.FromArgb(155, 185, 165);
            this.lblObra.Location = new System.Drawing.Point(528, 0);
            this.lblObra.Name = "lblObra";
            this.lblObra.Padding = new System.Windows.Forms.Padding(14, 0, 6, 0);
            this.lblObra.Size = new System.Drawing.Size(84, 50);
            this.lblObra.TabIndex = 4;
            this.lblObra.Text = "Obra:";
            this.lblObra.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // cboObra
            //
            this.cboObra.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.cboObra.BackColor = System.Drawing.Color.Transparent;
            this.cboObra.BorderColor = System.Drawing.Color.FromArgb(55, 75, 62);
            this.cboObra.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.cboObra.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboObra.FillColor = System.Drawing.Color.FromArgb(35, 50, 40);
            this.cboObra.FocusedColor = System.Drawing.Color.FromArgb(39, 174, 96);
            this.cboObra.FocusedState.BorderColor = System.Drawing.Color.FromArgb(39, 174, 96);
            this.cboObra.Font = new System.Drawing.Font("Segoe UI", 10.5F);
            this.cboObra.ForeColor = System.Drawing.Color.FromArgb(225, 225, 225);
            this.cboObra.HoverState.BorderColor = System.Drawing.Color.FromArgb(60, 120, 80);
            this.cboObra.ItemHeight = 28;
            this.cboObra.Location = new System.Drawing.Point(581, 8);
            this.cboObra.Margin = new System.Windows.Forms.Padding(3, 2, 8, 2);
            this.cboObra.Name = "cboObra";
            this.cboObra.Size = new System.Drawing.Size(196, 34);
            this.cboObra.TabIndex = 5;
            // 
            // panelEntrada2
            // 
            this.panelEntrada2.ColumnCount = 5;
            this.panelEntrada2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.panelEntrada2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 40F));
            this.panelEntrada2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.panelEntrada2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.panelEntrada2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.panelEntrada2.Controls.Add(this.lblObservaciones, 0, 0);
            this.panelEntrada2.Controls.Add(this.txtObservaciones, 1, 0);
            this.panelEntrada2.Controls.Add(this.btnGuardar, 2, 0);
            this.panelEntrada2.Controls.Add(this.btnEliminar, 3, 0);
            this.panelEntrada2.Controls.Add(this.btnPDF, 4, 0);
            this.panelEntrada2.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelEntrada2.GrowStyle = System.Windows.Forms.TableLayoutPanelGrowStyle.AddColumns;
            this.panelEntrada2.Location = new System.Drawing.Point(0, 92);
            this.panelEntrada2.Name = "panelEntrada2";
            this.panelEntrada2.Padding = new System.Windows.Forms.Padding(14, 0, 14, 0);
            this.panelEntrada2.RowCount = 1;
            this.panelEntrada2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.panelEntrada2.Size = new System.Drawing.Size(800, 50);
            this.panelEntrada2.TabIndex = 2;
            //
            // lblObservaciones
            //
            this.lblObservaciones.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblObservaciones.Font = new System.Drawing.Font("Bahnschrift Light", 10.5F);
            this.lblObservaciones.ForeColor = System.Drawing.Color.FromArgb(155, 185, 165);
            this.lblObservaciones.Location = new System.Drawing.Point(3, 0);
            this.lblObservaciones.Name = "lblObservaciones";
            this.lblObservaciones.Padding = new System.Windows.Forms.Padding(14, 0, 6, 0);
            this.lblObservaciones.Size = new System.Drawing.Size(117, 50);
            this.lblObservaciones.TabIndex = 0;
            this.lblObservaciones.Text = "Observaciones:";
            this.lblObservaciones.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // txtObservaciones
            //
            this.txtObservaciones.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtObservaciones.BorderColor = System.Drawing.Color.FromArgb(55, 75, 62);
            this.txtObservaciones.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txtObservaciones.DefaultText = "";
            this.txtObservaciones.DisabledState.BorderColor = System.Drawing.Color.FromArgb(60, 60, 60);
            this.txtObservaciones.DisabledState.FillColor = System.Drawing.Color.FromArgb(40, 55, 44);
            this.txtObservaciones.DisabledState.ForeColor = System.Drawing.Color.FromArgb(100, 100, 100);
            this.txtObservaciones.DisabledState.PlaceholderForeColor = System.Drawing.Color.FromArgb(100, 100, 100);
            this.txtObservaciones.FillColor = System.Drawing.Color.FromArgb(35, 50, 40);
            this.txtObservaciones.FocusedState.BorderColor = System.Drawing.Color.FromArgb(39, 174, 96);
            this.txtObservaciones.Font = new System.Drawing.Font("Segoe UI", 10.5F);
            this.txtObservaciones.ForeColor = System.Drawing.Color.FromArgb(225, 225, 225);
            this.txtObservaciones.HoverState.BorderColor = System.Drawing.Color.FromArgb(60, 120, 80);
            this.txtObservaciones.Location = new System.Drawing.Point(146, 8);
            this.txtObservaciones.Name = "txtObservaciones";
            this.txtObservaciones.PlaceholderText = "Marca, modelo o detalles...";
            this.txtObservaciones.SelectedText = "";
            this.txtObservaciones.Size = new System.Drawing.Size(245, 34);
            this.txtObservaciones.TabIndex = 1;
            // 
            // btnGuardar
            // 
            this.btnGuardar.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.btnGuardar.DisabledState.BorderColor = System.Drawing.Color.FromArgb(50, 65, 55);
            this.btnGuardar.DisabledState.CustomBorderColor = System.Drawing.Color.FromArgb(50, 65, 55);
            this.btnGuardar.DisabledState.FillColor = System.Drawing.Color.FromArgb(45, 60, 50);
            this.btnGuardar.DisabledState.ForeColor = System.Drawing.Color.FromArgb(100, 120, 108);
            this.btnGuardar.FillColor = System.Drawing.Color.FromArgb(39, 174, 96);
            this.btnGuardar.Font = new System.Drawing.Font("Bahnschrift Light", 11F);
            this.btnGuardar.ForeColor = System.Drawing.Color.White;
            this.btnGuardar.HoverState.FillColor = System.Drawing.Color.FromArgb(46, 196, 110);
            this.btnGuardar.Location = new System.Drawing.Point(397, 9);
            this.btnGuardar.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.btnGuardar.Name = "btnGuardar";
            this.btnGuardar.Size = new System.Drawing.Size(116, 32);
            this.btnGuardar.TabIndex = 2;
            this.btnGuardar.Text = "Guardar";
            this.btnGuardar.Click += new System.EventHandler(this.btnGuardar_Click);
            //
            // btnEliminar
            //
            this.btnEliminar.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.btnEliminar.DisabledState.BorderColor = System.Drawing.Color.FromArgb(50, 50, 50);
            this.btnEliminar.DisabledState.CustomBorderColor = System.Drawing.Color.FromArgb(50, 50, 50);
            this.btnEliminar.DisabledState.FillColor = System.Drawing.Color.FromArgb(40, 40, 40);
            this.btnEliminar.DisabledState.ForeColor = System.Drawing.Color.FromArgb(80, 80, 80);
            this.btnEliminar.FillColor = System.Drawing.Color.FromArgb(158, 45, 40);
            this.btnEliminar.Font = new System.Drawing.Font("Bahnschrift Light", 11F);
            this.btnEliminar.ForeColor = System.Drawing.Color.White;
            this.btnEliminar.HoverState.FillColor = System.Drawing.Color.FromArgb(180, 55, 48);
            this.btnEliminar.Location = new System.Drawing.Point(519, 9);
            this.btnEliminar.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.btnEliminar.Name = "btnEliminar";
            this.btnEliminar.Size = new System.Drawing.Size(116, 32);
            this.btnEliminar.TabIndex = 3;
            this.btnEliminar.Text = "Eliminar";
            this.btnEliminar.Click += new System.EventHandler(this.btnEliminar_Click);
            //
            // btnPDF
            //
            this.btnPDF.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.btnPDF.DisabledState.BorderColor = System.Drawing.Color.FromArgb(50, 65, 55);
            this.btnPDF.DisabledState.CustomBorderColor = System.Drawing.Color.FromArgb(50, 65, 55);
            this.btnPDF.DisabledState.FillColor = System.Drawing.Color.FromArgb(45, 60, 50);
            this.btnPDF.DisabledState.ForeColor = System.Drawing.Color.FromArgb(100, 120, 108);
            this.btnPDF.FillColor = System.Drawing.Color.FromArgb(47, 84, 57);
            this.btnPDF.Font = new System.Drawing.Font("Bahnschrift Light", 11F);
            this.btnPDF.ForeColor = System.Drawing.Color.White;
            this.btnPDF.HoverState.FillColor = System.Drawing.Color.FromArgb(55, 100, 68);
            this.btnPDF.Location = new System.Drawing.Point(641, 9);
            this.btnPDF.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.btnPDF.Name = "btnPDF";
            this.btnPDF.Size = new System.Drawing.Size(116, 32);
            this.btnPDF.TabIndex = 4;
            this.btnPDF.Text = "PDF";
            this.btnPDF.Click += new System.EventHandler(this.btnPDF_Click);
            // 
            // panelTabs
            // 
            this.panelTabs.ColumnCount = 3;
            this.panelTabs.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F));
            this.panelTabs.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F));
            this.panelTabs.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 40F));
            this.panelTabs.Controls.Add(this.btnVerHerramientas, 0, 0);
            this.panelTabs.Controls.Add(this.btnVerHistorial, 1, 0);
            this.panelTabs.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTabs.GrowStyle = System.Windows.Forms.TableLayoutPanelGrowStyle.AddColumns;
            this.panelTabs.Location = new System.Drawing.Point(0, 142);
            this.panelTabs.Name = "panelTabs";
            this.panelTabs.Padding = new System.Windows.Forms.Padding(20, 5, 20, 5);
            this.panelTabs.RowCount = 1;
            this.panelTabs.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.panelTabs.Size = new System.Drawing.Size(800, 42);
            this.panelTabs.TabIndex = 3;
            // 
            // btnVerHerramientas
            // 
            this.btnVerHerramientas.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.btnVerHerramientas.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(135)))), ((int)(((byte)(84)))));
            this.btnVerHerramientas.Font = new System.Drawing.Font("Bahnschrift SemiBold", 13F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnVerHerramientas.ForeColor = System.Drawing.Color.White;
            this.btnVerHerramientas.Location = new System.Drawing.Point(23, 6);
            this.btnVerHerramientas.Name = "btnVerHerramientas";
            this.btnVerHerramientas.Size = new System.Drawing.Size(222, 30);
            this.btnVerHerramientas.TabIndex = 0;
            this.btnVerHerramientas.Text = "Ver Herramientas";
            this.btnVerHerramientas.Click += new System.EventHandler(this.btnVerHerramientas_Click);
            // 
            // btnVerHistorial
            // 
            this.btnVerHistorial.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.btnVerHistorial.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(47)))), ((int)(((byte)(84)))), ((int)(((byte)(57)))));
            this.btnVerHistorial.Font = new System.Drawing.Font("Bahnschrift SemiBold", 13F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnVerHistorial.ForeColor = System.Drawing.Color.White;
            this.btnVerHistorial.Location = new System.Drawing.Point(251, 6);
            this.btnVerHistorial.Name = "btnVerHistorial";
            this.btnVerHistorial.Size = new System.Drawing.Size(222, 30);
            this.btnVerHistorial.TabIndex = 1;
            this.btnVerHistorial.Text = "Ver Historial";
            this.btnVerHistorial.Click += new System.EventHandler(this.btnVerHistorial_Click);
            // 
            // panelGrid
            // 
            this.panelGrid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panelGrid.BorderColor = System.Drawing.Color.FromArgb(38, 52, 44);
            this.panelGrid.Controls.Add(this.guna2VScrollBar1);
            this.panelGrid.Controls.Add(this.dgvHerramientas);
            this.panelGrid.Location = new System.Drawing.Point(0, 224);
            this.panelGrid.Name = "panelGrid";
            this.panelGrid.Padding = new System.Windows.Forms.Padding(0);
            this.panelGrid.Size = new System.Drawing.Size(776, 240);
            this.panelGrid.TabIndex = 4;
            // 
            // guna2VScrollBar1
            // 
            this.guna2VScrollBar1.BindingContainer = this.dgvHerramientas;
            this.guna2VScrollBar1.Dock = System.Windows.Forms.DockStyle.Right;
            this.guna2VScrollBar1.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(50)))), ((int)(((byte)(40)))));
            this.guna2VScrollBar1.InUpdate = false;
            this.guna2VScrollBar1.LargeChange = 10;
            this.guna2VScrollBar1.Location = new System.Drawing.Point(751, 7);
            this.guna2VScrollBar1.Minimum = 1;
            this.guna2VScrollBar1.Name = "guna2VScrollBar1";
            this.guna2VScrollBar1.ScrollbarSize = 18;
            this.guna2VScrollBar1.Size = new System.Drawing.Size(18, 226);
            this.guna2VScrollBar1.TabIndex = 1;
            this.guna2VScrollBar1.ThumbColor = System.Drawing.Color.FromArgb(55, 85, 65);
            this.guna2VScrollBar1.Value = 1;
            //
            // dgvHerramientas
            // 
            this.dgvHerramientas.AllowUserToAddRows = false;
            this.dgvHerramientas.AllowUserToDeleteRows = false;
            this.dgvHerramientas.AllowUserToResizeColumns = false;
            this.dgvHerramientas.AllowUserToResizeRows = false;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(38)))), ((int)(((byte)(35)))));
            this.dgvHerramientas.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
            this.dgvHerramientas.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(38)))), ((int)(((byte)(35)))));
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.FromArgb(32, 55, 40);
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Bahnschrift Light", 10.5F);
            dataGridViewCellStyle2.ForeColor = System.Drawing.Color.FromArgb(195, 215, 202);
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.Color.FromArgb(32, 55, 40);
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.Color.FromArgb(195, 215, 202);
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvHerramientas.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
            this.dgvHerramientas.ColumnHeadersHeight = 32;
            this.dgvHerramientas.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = System.Drawing.Color.FromArgb(29, 38, 35);
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Bahnschrift Light", 11F);
            dataGridViewCellStyle3.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle3.Padding = new System.Windows.Forms.Padding(14, 0, 14, 0);
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.Color.FromArgb(47, 84, 57);
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvHerramientas.DefaultCellStyle = dataGridViewCellStyle3;
            this.dgvHerramientas.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvHerramientas.GridColor = System.Drawing.Color.FromArgb(38, 50, 43);
            this.dgvHerramientas.Location = new System.Drawing.Point(0, 0);
            this.dgvHerramientas.Name = "dgvHerramientas";
            this.dgvHerramientas.ReadOnly = true;
            this.dgvHerramientas.RowHeadersVisible = false;
            this.dgvHerramientas.RowTemplate.DefaultCellStyle.Font = new System.Drawing.Font("Bahnschrift Light", 11F);
            this.dgvHerramientas.RowTemplate.Height = 36;
            this.dgvHerramientas.Size = new System.Drawing.Size(762, 226);
            this.dgvHerramientas.TabIndex = 0;
            this.dgvHerramientas.Theme = Guna.UI2.WinForms.Enums.DataGridViewPresetThemes.Dark;
            this.dgvHerramientas.ThemeStyle.AlternatingRowsStyle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(38)))), ((int)(((byte)(35)))));
            this.dgvHerramientas.ThemeStyle.AlternatingRowsStyle.Font = null;
            this.dgvHerramientas.ThemeStyle.AlternatingRowsStyle.ForeColor = System.Drawing.Color.Empty;
            this.dgvHerramientas.ThemeStyle.AlternatingRowsStyle.SelectionBackColor = System.Drawing.Color.Empty;
            this.dgvHerramientas.ThemeStyle.AlternatingRowsStyle.SelectionForeColor = System.Drawing.Color.Empty;
            this.dgvHerramientas.ThemeStyle.BackColor = System.Drawing.Color.FromArgb(29, 38, 35);
            this.dgvHerramientas.ThemeStyle.GridColor = System.Drawing.Color.FromArgb(38, 50, 43);
            this.dgvHerramientas.ThemeStyle.HeaderStyle.BackColor = System.Drawing.Color.FromArgb(32, 55, 40);
            this.dgvHerramientas.ThemeStyle.HeaderStyle.BorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            this.dgvHerramientas.ThemeStyle.HeaderStyle.Font = new System.Drawing.Font("Bahnschrift Light", 10.5F);
            this.dgvHerramientas.ThemeStyle.HeaderStyle.ForeColor = System.Drawing.Color.FromArgb(195, 215, 202);
            this.dgvHerramientas.ThemeStyle.HeaderStyle.HeaightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.dgvHerramientas.ThemeStyle.HeaderStyle.Height = 32;
            this.dgvHerramientas.ThemeStyle.ReadOnly = true;
            this.dgvHerramientas.ThemeStyle.RowsStyle.BackColor = System.Drawing.Color.FromArgb(29, 38, 35);
            this.dgvHerramientas.ThemeStyle.RowsStyle.BorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            this.dgvHerramientas.ThemeStyle.RowsStyle.Font = new System.Drawing.Font("Bahnschrift Light", 11F);
            this.dgvHerramientas.ThemeStyle.RowsStyle.ForeColor = System.Drawing.Color.White;
            this.dgvHerramientas.ThemeStyle.RowsStyle.Height = 36;
            this.dgvHerramientas.ThemeStyle.RowsStyle.SelectionBackColor = System.Drawing.Color.FromArgb(47, 84, 57);
            this.dgvHerramientas.ThemeStyle.RowsStyle.SelectionForeColor = System.Drawing.Color.White;
            this.dgvHerramientas.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvHerramientas_CellClick);
            // 
            // panelGridHistorial
            // 
            this.panelGridHistorial.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panelGridHistorial.BorderColor = System.Drawing.Color.FromArgb(38, 52, 44);
            this.panelGridHistorial.BorderThickness = 1;
            this.panelGridHistorial.Controls.Add(this.guna2VScrollBar2);
            this.panelGridHistorial.Controls.Add(this.dgvHistorial);
            this.panelGridHistorial.Location = new System.Drawing.Point(0, 224);
            this.panelGridHistorial.Name = "panelGridHistorial";
            this.panelGridHistorial.Padding = new System.Windows.Forms.Padding(0);
            this.panelGridHistorial.Size = new System.Drawing.Size(776, 240);
            this.panelGridHistorial.TabIndex = 5;
            this.panelGridHistorial.Visible = false;
            // 
            // guna2VScrollBar2
            // 
            this.guna2VScrollBar2.BindingContainer = this.dgvHistorial;
            this.guna2VScrollBar2.Dock = System.Windows.Forms.DockStyle.Right;
            this.guna2VScrollBar2.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(50)))), ((int)(((byte)(40)))));
            this.guna2VScrollBar2.InUpdate = false;
            this.guna2VScrollBar2.LargeChange = 10;
            this.guna2VScrollBar2.Location = new System.Drawing.Point(751, 7);
            this.guna2VScrollBar2.Minimum = 1;
            this.guna2VScrollBar2.Name = "guna2VScrollBar2";
            this.guna2VScrollBar2.ScrollbarSize = 18;
            this.guna2VScrollBar2.Size = new System.Drawing.Size(18, 226);
            this.guna2VScrollBar2.TabIndex = 1;
            this.guna2VScrollBar2.ThumbColor = System.Drawing.Color.FromArgb(55, 85, 65);
            this.guna2VScrollBar2.Value = 1;
            // 
            // dgvHistorial
            // 
            this.dgvHistorial.AllowUserToAddRows = false;
            this.dgvHistorial.AllowUserToDeleteRows = false;
            this.dgvHistorial.AllowUserToResizeColumns = false;
            this.dgvHistorial.AllowUserToResizeRows = false;
            dataGridViewCellStyle4.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(38)))), ((int)(((byte)(35)))));
            this.dgvHistorial.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle4;
            this.dgvHistorial.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(38)))), ((int)(((byte)(35)))));
            dataGridViewCellStyle5.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle5.BackColor = System.Drawing.Color.FromArgb(32, 55, 40);
            dataGridViewCellStyle5.Font = new System.Drawing.Font("Bahnschrift Light", 10.5F);
            dataGridViewCellStyle5.ForeColor = System.Drawing.Color.FromArgb(195, 215, 202);
            dataGridViewCellStyle5.SelectionBackColor = System.Drawing.Color.FromArgb(32, 55, 40);
            dataGridViewCellStyle5.SelectionForeColor = System.Drawing.Color.FromArgb(195, 215, 202);
            dataGridViewCellStyle5.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvHistorial.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle5;
            this.dgvHistorial.ColumnHeadersHeight = 32;
            this.dgvHistorial.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dataGridViewCellStyle6.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle6.BackColor = System.Drawing.Color.FromArgb(29, 38, 35);
            dataGridViewCellStyle6.Font = new System.Drawing.Font("Bahnschrift Light", 11F);
            dataGridViewCellStyle6.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle6.Padding = new System.Windows.Forms.Padding(14, 0, 14, 0);
            dataGridViewCellStyle6.SelectionBackColor = System.Drawing.Color.FromArgb(47, 84, 57);
            dataGridViewCellStyle6.SelectionForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle6.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvHistorial.DefaultCellStyle = dataGridViewCellStyle6;
            this.dgvHistorial.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvHistorial.GridColor = System.Drawing.Color.FromArgb(38, 50, 43);
            this.dgvHistorial.Location = new System.Drawing.Point(0, 0);
            this.dgvHistorial.Name = "dgvHistorial";
            this.dgvHistorial.ReadOnly = true;
            this.dgvHistorial.RowHeadersVisible = false;
            this.dgvHistorial.RowTemplate.DefaultCellStyle.Font = new System.Drawing.Font("Bahnschrift Light", 11F);
            this.dgvHistorial.RowTemplate.Height = 36;
            this.dgvHistorial.Size = new System.Drawing.Size(762, 226);
            this.dgvHistorial.TabIndex = 0;
            this.dgvHistorial.Theme = Guna.UI2.WinForms.Enums.DataGridViewPresetThemes.Dark;
            this.dgvHistorial.ThemeStyle.AlternatingRowsStyle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(38)))), ((int)(((byte)(35)))));
            this.dgvHistorial.ThemeStyle.AlternatingRowsStyle.Font = null;
            this.dgvHistorial.ThemeStyle.AlternatingRowsStyle.ForeColor = System.Drawing.Color.Empty;
            this.dgvHistorial.ThemeStyle.AlternatingRowsStyle.SelectionBackColor = System.Drawing.Color.Empty;
            this.dgvHistorial.ThemeStyle.AlternatingRowsStyle.SelectionForeColor = System.Drawing.Color.Empty;
            this.dgvHistorial.ThemeStyle.BackColor = System.Drawing.Color.FromArgb(29, 38, 35);
            this.dgvHistorial.ThemeStyle.GridColor = System.Drawing.Color.FromArgb(38, 50, 43);
            this.dgvHistorial.ThemeStyle.HeaderStyle.BackColor = System.Drawing.Color.FromArgb(32, 55, 40);
            this.dgvHistorial.ThemeStyle.HeaderStyle.BorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            this.dgvHistorial.ThemeStyle.HeaderStyle.Font = new System.Drawing.Font("Bahnschrift Light", 10.5F);
            this.dgvHistorial.ThemeStyle.HeaderStyle.ForeColor = System.Drawing.Color.FromArgb(195, 215, 202);
            this.dgvHistorial.ThemeStyle.HeaderStyle.HeaightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.dgvHistorial.ThemeStyle.HeaderStyle.Height = 32;
            this.dgvHistorial.ThemeStyle.ReadOnly = true;
            this.dgvHistorial.ThemeStyle.RowsStyle.BackColor = System.Drawing.Color.FromArgb(29, 38, 35);
            this.dgvHistorial.ThemeStyle.RowsStyle.BorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            this.dgvHistorial.ThemeStyle.RowsStyle.Font = new System.Drawing.Font("Bahnschrift Light", 11F);
            this.dgvHistorial.ThemeStyle.RowsStyle.ForeColor = System.Drawing.Color.White;
            this.dgvHistorial.ThemeStyle.RowsStyle.Height = 36;
            this.dgvHistorial.ThemeStyle.RowsStyle.SelectionBackColor = System.Drawing.Color.FromArgb(47, 84, 57);
            this.dgvHistorial.ThemeStyle.RowsStyle.SelectionForeColor = System.Drawing.Color.White;
            // 
            // panelFiltro
            // 
            this.panelFiltro.ColumnCount = 8;
            this.panelFiltro.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.panelFiltro.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.panelFiltro.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.panelFiltro.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.panelFiltro.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.panelFiltro.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.panelFiltro.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.panelFiltro.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.panelFiltro.Controls.Add(this.lblFiltro, 0, 0);
            this.panelFiltro.Controls.Add(this.cboFiltroHerramienta, 1, 0);
            this.panelFiltro.Controls.Add(this.lblBuscar, 2, 0);
            this.panelFiltro.Controls.Add(this.txtBuscar, 3, 0);
            this.panelFiltro.Controls.Add(this.lblFiltroEstado, 4, 0);
            this.panelFiltro.Controls.Add(this.cboFiltroEstado, 5, 0);
            this.panelFiltro.Controls.Add(this.lblFiltroObra, 6, 0);
            this.panelFiltro.Controls.Add(this.cboFiltroObra, 7, 0);
            this.panelFiltro.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelFiltro.Location = new System.Drawing.Point(0, 184);
            this.panelFiltro.Name = "panelFiltro";
            this.panelFiltro.Padding = new System.Windows.Forms.Padding(20, 0, 20, 0);
            this.panelFiltro.RowCount = 1;
            this.panelFiltro.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.panelFiltro.Size = new System.Drawing.Size(800, 40);
            this.panelFiltro.TabIndex = 6;
            this.panelFiltro.Visible = false;
            //
            // lblFiltro
            //
            this.lblFiltro.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblFiltro.AutoSize = true;
            this.lblFiltro.Font = new System.Drawing.Font("Bahnschrift Light", 10.5F);
            this.lblFiltro.ForeColor = System.Drawing.Color.FromArgb(155, 185, 165);
            this.lblFiltro.Location = new System.Drawing.Point(23, 10);
            this.lblFiltro.Name = "lblFiltro";
            this.lblFiltro.Size = new System.Drawing.Size(114, 18);
            this.lblFiltro.TabIndex = 0;
            this.lblFiltro.Text = "Filtrar por herr.:";
            // 
            // cboFiltroHerramienta
            // 
            this.cboFiltroHerramienta.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.cboFiltroHerramienta.BackColor = System.Drawing.Color.Transparent;
            this.cboFiltroHerramienta.BorderColor = System.Drawing.Color.FromArgb(55, 75, 62);
            this.cboFiltroHerramienta.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.cboFiltroHerramienta.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboFiltroHerramienta.FillColor = System.Drawing.Color.FromArgb(35, 50, 40);
            this.cboFiltroHerramienta.FocusedColor = System.Drawing.Color.FromArgb(39, 174, 96);
            this.cboFiltroHerramienta.FocusedState.BorderColor = System.Drawing.Color.FromArgb(39, 174, 96);
            this.cboFiltroHerramienta.HoverState.BorderColor = System.Drawing.Color.FromArgb(60, 120, 80);
            this.cboFiltroHerramienta.Font = new System.Drawing.Font("Segoe UI", 10.75F);
            this.cboFiltroHerramienta.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(230)))), ((int)(((byte)(230)))));
            this.cboFiltroHerramienta.ItemHeight = 28;
            this.cboFiltroHerramienta.Location = new System.Drawing.Point(143, 3);
            this.cboFiltroHerramienta.Name = "cboFiltroHerramienta";
            this.cboFiltroHerramienta.Size = new System.Drawing.Size(119, 34);
            this.cboFiltroHerramienta.TabIndex = 1;
            this.cboFiltroHerramienta.SelectedIndexChanged += new System.EventHandler(this.cboFiltroHerramienta_SelectedIndexChanged);
            // 
            // lblBuscar
            // 
            this.lblBuscar.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblBuscar.AutoSize = true;
            this.lblBuscar.Font = new System.Drawing.Font("Bahnschrift Light", 10.5F);
            this.lblBuscar.ForeColor = System.Drawing.Color.FromArgb(155, 185, 165);
            this.lblBuscar.Location = new System.Drawing.Point(268, 10);
            this.lblBuscar.Name = "lblBuscar";
            this.lblBuscar.Size = new System.Drawing.Size(55, 18);
            this.lblBuscar.TabIndex = 2;
            this.lblBuscar.Text = "Buscar:";
            // 
            // txtBuscar
            // 
            this.txtBuscar.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtBuscar.BorderColor = System.Drawing.Color.FromArgb(55, 75, 62);
            this.txtBuscar.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txtBuscar.DefaultText = "";
            this.txtBuscar.DisabledState.BorderColor = System.Drawing.Color.FromArgb(60, 60, 60);
            this.txtBuscar.DisabledState.FillColor = System.Drawing.Color.FromArgb(40, 55, 44);
            this.txtBuscar.DisabledState.ForeColor = System.Drawing.Color.FromArgb(100, 100, 100);
            this.txtBuscar.DisabledState.PlaceholderForeColor = System.Drawing.Color.FromArgb(100, 100, 100);
            this.txtBuscar.FillColor = System.Drawing.Color.FromArgb(35, 50, 40);
            this.txtBuscar.FocusedState.BorderColor = System.Drawing.Color.FromArgb(39, 174, 96);
            this.txtBuscar.Font = new System.Drawing.Font("Segoe UI", 10.5F);
            this.txtBuscar.ForeColor = System.Drawing.Color.FromArgb(225, 225, 225);
            this.txtBuscar.HoverState.BorderColor = System.Drawing.Color.FromArgb(60, 120, 80);
            this.txtBuscar.Location = new System.Drawing.Point(329, 3);
            this.txtBuscar.Name = "txtBuscar";
            this.txtBuscar.PlaceholderText = "Nombre...";
            this.txtBuscar.SelectedText = "";
            this.txtBuscar.Size = new System.Drawing.Size(119, 34);
            this.txtBuscar.TabIndex = 3;
            this.txtBuscar.TextChanged += new System.EventHandler(this.txtBuscar_TextChanged);
            // 
            // lblFiltroEstado
            // 
            this.lblFiltroEstado.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblFiltroEstado.AutoSize = true;
            this.lblFiltroEstado.Font = new System.Drawing.Font("Bahnschrift Light", 10.5F);
            this.lblFiltroEstado.ForeColor = System.Drawing.Color.FromArgb(155, 185, 165);
            this.lblFiltroEstado.Location = new System.Drawing.Point(454, 10);
            this.lblFiltroEstado.Name = "lblFiltroEstado";
            this.lblFiltroEstado.Size = new System.Drawing.Size(53, 18);
            this.lblFiltroEstado.TabIndex = 4;
            this.lblFiltroEstado.Text = "Estado:";
            // 
            // cboFiltroEstado
            // 
            this.cboFiltroEstado.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.cboFiltroEstado.BackColor = System.Drawing.Color.Transparent;
            this.cboFiltroEstado.BorderColor = System.Drawing.Color.FromArgb(55, 75, 62);
            this.cboFiltroEstado.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.cboFiltroEstado.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboFiltroEstado.FillColor = System.Drawing.Color.FromArgb(35, 50, 40);
            this.cboFiltroEstado.FocusedColor = System.Drawing.Color.FromArgb(39, 174, 96);
            this.cboFiltroEstado.FocusedState.BorderColor = System.Drawing.Color.FromArgb(39, 174, 96);
            this.cboFiltroEstado.HoverState.BorderColor = System.Drawing.Color.FromArgb(60, 120, 80);
            this.cboFiltroEstado.Font = new System.Drawing.Font("Segoe UI", 10.75F);
            this.cboFiltroEstado.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(230)))), ((int)(((byte)(230)))));
            this.cboFiltroEstado.ItemHeight = 28;
            this.cboFiltroEstado.Location = new System.Drawing.Point(513, 3);
            this.cboFiltroEstado.Name = "cboFiltroEstado";
            this.cboFiltroEstado.Size = new System.Drawing.Size(119, 34);
            this.cboFiltroEstado.TabIndex = 5;
            this.cboFiltroEstado.SelectedIndexChanged += new System.EventHandler(this.cboFiltroEstado_SelectedIndexChanged);
            // 
            // lblFiltroObra
            // 
            this.lblFiltroObra.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblFiltroObra.AutoSize = true;
            this.lblFiltroObra.Font = new System.Drawing.Font("Bahnschrift Light", 10.5F);
            this.lblFiltroObra.ForeColor = System.Drawing.Color.FromArgb(155, 185, 165);
            this.lblFiltroObra.Location = new System.Drawing.Point(638, 10);
            this.lblFiltroObra.Name = "lblFiltroObra";
            this.lblFiltroObra.Size = new System.Drawing.Size(43, 18);
            this.lblFiltroObra.TabIndex = 6;
            this.lblFiltroObra.Text = "Obra:";
            // 
            // cboFiltroObra
            // 
            this.cboFiltroObra.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.cboFiltroObra.BackColor = System.Drawing.Color.Transparent;
            this.cboFiltroObra.BorderColor = System.Drawing.Color.FromArgb(55, 75, 62);
            this.cboFiltroObra.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.cboFiltroObra.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboFiltroObra.FillColor = System.Drawing.Color.FromArgb(35, 50, 40);
            this.cboFiltroObra.FocusedColor = System.Drawing.Color.FromArgb(39, 174, 96);
            this.cboFiltroObra.FocusedState.BorderColor = System.Drawing.Color.FromArgb(39, 174, 96);
            this.cboFiltroObra.HoverState.BorderColor = System.Drawing.Color.FromArgb(60, 120, 80);
            this.cboFiltroObra.Font = new System.Drawing.Font("Segoe UI", 10.75F);
            this.cboFiltroObra.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(230)))), ((int)(((byte)(230)))));
            this.cboFiltroObra.ItemHeight = 28;
            this.cboFiltroObra.Location = new System.Drawing.Point(687, 3);
            this.cboFiltroObra.Name = "cboFiltroObra";
            this.cboFiltroObra.Size = new System.Drawing.Size(90, 34);
            this.cboFiltroObra.TabIndex = 7;
            this.cboFiltroObra.SelectedIndexChanged += new System.EventHandler(this.cboFiltroObra_SelectedIndexChanged);
            // 
            // Vista_Herramientas
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(38)))), ((int)(((byte)(35)))));
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.panelGridHistorial);
            this.Controls.Add(this.panelGrid);
            this.Controls.Add(this.panelFiltro);
            this.Controls.Add(this.panelTabs);
            this.Controls.Add(this.panelEntrada2);
            this.Controls.Add(this.panelEntrada);
            this.Controls.Add(this.panelTitulo);
            this.Name = "Vista_Herramientas";
            this.Text = "Gestión de Herramientas";
            this.Load += new System.EventHandler(this.Vista_Herramientas_Load);
            this.panelTitulo.ResumeLayout(false);
            this.panelTitulo.PerformLayout();
            this.panelEntrada.ResumeLayout(false);
            this.panelEntrada.PerformLayout();
            this.panelEntrada2.ResumeLayout(false);
            this.panelEntrada2.PerformLayout();
            this.panelTabs.ResumeLayout(false);
            this.panelFiltro.ResumeLayout(false);
            this.panelFiltro.PerformLayout();
            this.panelGrid.ResumeLayout(false);
            this.panelGridHistorial.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvHerramientas)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvHistorial)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion

        private PanelAntiParpadeo panelTitulo;
        private System.Windows.Forms.Label lblTitulo;
        private PanelAntiParpadeo panelEntrada;
        private System.Windows.Forms.Label lblNombre;
        private Guna.UI2.WinForms.Guna2TextBox txtNombre;
        private System.Windows.Forms.Label lblEstado;
        private Guna.UI2.WinForms.Guna2ComboBox cboEstado;
        private System.Windows.Forms.Label lblObra;
        private Guna.UI2.WinForms.Guna2ComboBox cboObra;
        private PanelAntiParpadeo panelEntrada2;
        private System.Windows.Forms.Label lblObservaciones;
        private Guna.UI2.WinForms.Guna2TextBox txtObservaciones;
        private Guna.UI2.WinForms.Guna2Button btnGuardar;
        private Guna.UI2.WinForms.Guna2Button btnEliminar;
        private Guna.UI2.WinForms.Guna2Button btnPDF;
        private PanelAntiParpadeo panelTabs;
        private Guna.UI2.WinForms.Guna2Button btnVerHerramientas;
        private Guna.UI2.WinForms.Guna2Button btnVerHistorial;
        private Guna.UI2.WinForms.Guna2Panel panelGrid;
        private Guna.UI2.WinForms.Guna2VScrollBar guna2VScrollBar1;
        private Guna.UI2.WinForms.Guna2DataGridView dgvHerramientas;
        private Guna.UI2.WinForms.Guna2Panel panelGridHistorial;
        private Guna.UI2.WinForms.Guna2VScrollBar guna2VScrollBar2;
        private Guna.UI2.WinForms.Guna2DataGridView dgvHistorial;
        private PanelAntiParpadeo panelFiltro;
        private System.Windows.Forms.Label lblFiltro;
        private Guna.UI2.WinForms.Guna2ComboBox cboFiltroHerramienta;
        private System.Windows.Forms.Label lblBuscar;
        private Guna.UI2.WinForms.Guna2TextBox txtBuscar;
        private System.Windows.Forms.Label lblFiltroEstado;
        private Guna.UI2.WinForms.Guna2ComboBox cboFiltroEstado;
        private System.Windows.Forms.Label lblFiltroObra;
        private Guna.UI2.WinForms.Guna2ComboBox cboFiltroObra;
    }
}

