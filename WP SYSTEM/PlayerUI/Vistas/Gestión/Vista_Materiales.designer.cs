namespace WP_Inventario.Vistas
{
    partial class Vista_Materiales
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle16 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle17 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle18 = new System.Windows.Forms.DataGridViewCellStyle();

            this.guna2ContextMenuStrip1 = new Guna.UI2.WinForms.Guna2ContextMenuStrip();
            this.categoríaToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.cantidadToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.minimoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();

            this.panelAntiParpadeo1 = new WP_Inventario.PanelAntiParpadeo();
            this.label1 = new System.Windows.Forms.Label();

            this.panelAntiParpadeo8 = new WP_Inventario.PanelAntiParpadeo();
            this.label2 = new System.Windows.Forms.Label();
            this.nombreMaterial = new Guna.UI2.WinForms.Guna2TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.stAct = new Guna.UI2.WinForms.Guna2NumericUpDown();
            this.label3 = new System.Windows.Forms.Label();
            this.cmbCategoria = new Guna.UI2.WinForms.Guna2ComboBox();
            this.label7 = new System.Windows.Forms.Label();
            this.stMin = new Guna.UI2.WinForms.Guna2NumericUpDown();
            this.btnProveedores = new Guna.UI2.WinForms.Guna2Button();
            this.panelAntiParpadeo5 = new WP_Inventario.PanelAntiParpadeo();
            this.guna2Button1 = new Guna.UI2.WinForms.Guna2Button();
            this.eliminarMaterialBoton = new Guna.UI2.WinForms.Guna2Button();

            this.panelAntiParpadeo6 = new WP_Inventario.PanelAntiParpadeo();
            this.label8 = new System.Windows.Forms.Label();
            this.txtFiltroMaterial = new Guna.UI2.WinForms.Guna2TextBox();
            this.filtrarMaterial = new Guna.UI2.WinForms.Guna2Button();

            this.materiales = new WP_Inventario.PanelAntiParpadeo();
            this.dgvMateriales = new Guna.UI2.WinForms.Guna2DataGridView();
            this.label9 = new System.Windows.Forms.Label();

            this.guna2ContextMenuStrip1.SuspendLayout();
            this.panelAntiParpadeo1.SuspendLayout();
            this.panelAntiParpadeo8.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.stAct)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.stMin)).BeginInit();
            this.panelAntiParpadeo5.SuspendLayout();
            this.panelAntiParpadeo6.SuspendLayout();
            this.materiales.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvMateriales)).BeginInit();
            this.SuspendLayout();

            // -------------------------------------------------------
            // guna2ContextMenuStrip1
            // -------------------------------------------------------
            this.guna2ContextMenuStrip1.BackColor = System.Drawing.Color.FromArgb(38, 52, 44);
            this.guna2ContextMenuStrip1.Font = new System.Drawing.Font("Bahnschrift Light", 11F);
            this.guna2ContextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.categoríaToolStripMenuItem,
                this.cantidadToolStripMenuItem,
                this.minimoToolStripMenuItem });
            this.guna2ContextMenuStrip1.Name = "guna2ContextMenuStrip1";
            this.guna2ContextMenuStrip1.RenderStyle.ArrowColor = System.Drawing.Color.White;
            this.guna2ContextMenuStrip1.RenderStyle.BorderColor = System.Drawing.Color.FromArgb(50, 80, 60);
            this.guna2ContextMenuStrip1.RenderStyle.ColorTable = null;
            this.guna2ContextMenuStrip1.RenderStyle.RoundedEdges = false;
            this.guna2ContextMenuStrip1.RenderStyle.SelectionArrowColor = System.Drawing.Color.White;
            this.guna2ContextMenuStrip1.RenderStyle.SelectionBackColor = System.Drawing.Color.FromArgb(39, 174, 96);
            this.guna2ContextMenuStrip1.RenderStyle.SelectionForeColor = System.Drawing.Color.White;
            this.guna2ContextMenuStrip1.RenderStyle.SeparatorColor = System.Drawing.Color.FromArgb(50, 70, 57);
            this.guna2ContextMenuStrip1.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.guna2ContextMenuStrip1.ShowImageMargin = false;
            this.guna2ContextMenuStrip1.Size = new System.Drawing.Size(125, 100);

            this.categoríaToolStripMenuItem.Name = "categoríaToolStripMenuItem";
            this.categoríaToolStripMenuItem.Size = new System.Drawing.Size(124, 24);
            this.categoríaToolStripMenuItem.Text = "Categoría";

            this.cantidadToolStripMenuItem.Name = "cantidadToolStripMenuItem";
            this.cantidadToolStripMenuItem.Size = new System.Drawing.Size(124, 24);
            this.cantidadToolStripMenuItem.Text = "Cantidad";

            this.minimoToolStripMenuItem.Name = "minimoToolStripMenuItem";
            this.minimoToolStripMenuItem.Size = new System.Drawing.Size(124, 24);
            this.minimoToolStripMenuItem.Text = "Minimo";

            // -------------------------------------------------------
            // panelAntiParpadeo1 — Encabezado
            // -------------------------------------------------------
            this.panelAntiParpadeo1.BackColor = System.Drawing.Color.FromArgb(32, 55, 40);
            this.panelAntiParpadeo1.ColumnCount = 1;
            this.panelAntiParpadeo1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.panelAntiParpadeo1.Controls.Add(this.label1, 0, 0);
            this.panelAntiParpadeo1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelAntiParpadeo1.Location = new System.Drawing.Point(0, 0);
            this.panelAntiParpadeo1.Name = "panelAntiParpadeo1";
            this.panelAntiParpadeo1.Padding = new System.Windows.Forms.Padding(14, 0, 14, 0);
            this.panelAntiParpadeo1.RowCount = 1;
            this.panelAntiParpadeo1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.panelAntiParpadeo1.Size = new System.Drawing.Size(800, 42);
            this.panelAntiParpadeo1.TabIndex = 13;

            // label1
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right));
            this.label1.AutoSize = false;
            this.label1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label1.Font = new System.Drawing.Font("Bahnschrift SemiBold", 13F, System.Drawing.FontStyle.Bold);
            this.label1.ForeColor = System.Drawing.Color.FromArgb(210, 235, 218);
            this.label1.Location = new System.Drawing.Point(14, 0);
            this.label1.Name = "label1";
            this.label1.TabIndex = 0;
            this.label1.Text = "MATERIALES";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // -------------------------------------------------------
            // panelAntiParpadeo8 — Formulario de campos
            // 4 columnas x 3 filas
            // -------------------------------------------------------
            this.panelAntiParpadeo8.ColumnCount = 4;
            this.panelAntiParpadeo8.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 90F));
            this.panelAntiParpadeo8.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.panelAntiParpadeo8.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 108F));
            this.panelAntiParpadeo8.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.panelAntiParpadeo8.Controls.Add(this.label2, 0, 0);
            this.panelAntiParpadeo8.Controls.Add(this.nombreMaterial, 1, 0);
            this.panelAntiParpadeo8.Controls.Add(this.label5, 2, 0);
            this.panelAntiParpadeo8.Controls.Add(this.stAct, 3, 0);
            this.panelAntiParpadeo8.Controls.Add(this.label3, 0, 1);
            this.panelAntiParpadeo8.Controls.Add(this.cmbCategoria, 1, 1);
            this.panelAntiParpadeo8.Controls.Add(this.label7, 2, 1);
            this.panelAntiParpadeo8.Controls.Add(this.stMin, 3, 1);
            this.panelAntiParpadeo8.Controls.Add(this.btnProveedores, 0, 2);
            this.panelAntiParpadeo8.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelAntiParpadeo8.Location = new System.Drawing.Point(0, 42);
            this.panelAntiParpadeo8.Name = "panelAntiParpadeo8";
            this.panelAntiParpadeo8.Padding = new System.Windows.Forms.Padding(0, 6, 4, 4);
            this.panelAntiParpadeo8.RowCount = 3;
            this.panelAntiParpadeo8.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.panelAntiParpadeo8.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.panelAntiParpadeo8.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
            this.panelAntiParpadeo8.Size = new System.Drawing.Size(800, 128);
            this.panelAntiParpadeo8.TabIndex = 33;

            // label2 — "Nombre:"
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right));
            this.label2.AutoSize = false;
            this.label2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label2.Font = new System.Drawing.Font("Bahnschrift Light", 10.5F);
            this.label2.ForeColor = System.Drawing.Color.FromArgb(155, 185, 165);
            this.label2.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0);
            this.label2.Name = "label2";
            this.label2.Padding = new System.Windows.Forms.Padding(14, 0, 6, 0);
            this.label2.TabIndex = 1;
            this.label2.Text = "Nombre:";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // nombreMaterial
            this.nombreMaterial.Anchor = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right));
            this.nombreMaterial.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.nombreMaterial.BorderColor = System.Drawing.Color.FromArgb(55, 75, 62);
            this.nombreMaterial.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.nombreMaterial.DefaultText = "";
            this.nombreMaterial.DisabledState.BorderColor = System.Drawing.Color.FromArgb(60, 60, 60);
            this.nombreMaterial.DisabledState.FillColor = System.Drawing.Color.FromArgb(40, 55, 44);
            this.nombreMaterial.DisabledState.ForeColor = System.Drawing.Color.FromArgb(100, 100, 100);
            this.nombreMaterial.DisabledState.PlaceholderForeColor = System.Drawing.Color.FromArgb(100, 100, 100);
            this.nombreMaterial.FillColor = System.Drawing.Color.FromArgb(35, 50, 40);
            this.nombreMaterial.FocusedState.BorderColor = System.Drawing.Color.FromArgb(39, 174, 96);
            this.nombreMaterial.Font = new System.Drawing.Font("Segoe UI", 10.5F);
            this.nombreMaterial.ForeColor = System.Drawing.Color.FromArgb(225, 225, 225);
            this.nombreMaterial.HoverState.BorderColor = System.Drawing.Color.FromArgb(60, 120, 80);
            this.nombreMaterial.Location = new System.Drawing.Point(91, 6);
            this.nombreMaterial.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            this.nombreMaterial.Name = "nombreMaterial";
            this.nombreMaterial.PlaceholderText = "Ej: Cemento Portland";
            this.nombreMaterial.SelectedText = "";
            this.nombreMaterial.Size = new System.Drawing.Size(400, 30);
            this.nombreMaterial.TabIndex = 2;

            // label5 — "Stock Actual:"
            this.label5.Anchor = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right));
            this.label5.AutoSize = false;
            this.label5.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label5.Font = new System.Drawing.Font("Bahnschrift Light", 10.5F);
            this.label5.ForeColor = System.Drawing.Color.FromArgb(155, 185, 165);
            this.label5.Name = "label5";
            this.label5.Padding = new System.Windows.Forms.Padding(8, 0, 6, 0);
            this.label5.TabIndex = 7;
            this.label5.Text = "Stock Actual:";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // stAct
            this.stAct.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.stAct.BackColor = System.Drawing.Color.Transparent;
            this.stAct.BorderColor = System.Drawing.Color.FromArgb(55, 75, 62);
            this.stAct.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.stAct.FocusedState.BorderColor = System.Drawing.Color.FromArgb(39, 174, 96);
            this.stAct.Font = new System.Drawing.Font("Segoe UI", 10.5F);
            this.stAct.Increment = new decimal(new int[] { 5, 0, 0, 65536 });
            this.stAct.Location = new System.Drawing.Point(525, 4);
            this.stAct.Margin = new System.Windows.Forms.Padding(3, 5, 6, 5);
            this.stAct.Maximum = new decimal(new int[] { 1410065407, 2, 0, 0 });
            this.stAct.Name = "stAct";
            this.stAct.Padding = new System.Windows.Forms.Padding(0, 0, 4, 0);
            this.stAct.Size = new System.Drawing.Size(136, 30);
            this.stAct.TabIndex = 8;
            this.stAct.UpDownButtonFillColor = System.Drawing.Color.FromArgb(35, 50, 40);

            // label3 — "Categoría:"
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right));
            this.label3.AutoSize = false;
            this.label3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label3.Font = new System.Drawing.Font("Bahnschrift Light", 10.5F);
            this.label3.ForeColor = System.Drawing.Color.FromArgb(155, 185, 165);
            this.label3.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0);
            this.label3.Name = "label3";
            this.label3.Padding = new System.Windows.Forms.Padding(14, 0, 6, 0);
            this.label3.TabIndex = 5;
            this.label3.Text = "Categoría:";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // cmbCategoria
            this.cmbCategoria.Anchor = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right));
            this.cmbCategoria.BackColor = System.Drawing.Color.Transparent;
            this.cmbCategoria.BorderColor = System.Drawing.Color.FromArgb(55, 75, 62);
            this.cmbCategoria.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.cmbCategoria.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbCategoria.DropDownWidth = 280;
            this.cmbCategoria.MaxDropDownItems = 10;
            this.cmbCategoria.FillColor = System.Drawing.Color.FromArgb(35, 50, 40);
            this.cmbCategoria.FocusedColor = System.Drawing.Color.FromArgb(39, 174, 96);
            this.cmbCategoria.FocusedState.BorderColor = System.Drawing.Color.FromArgb(39, 174, 96);
            this.cmbCategoria.Font = new System.Drawing.Font("Segoe UI", 10.5F);
            this.cmbCategoria.ForeColor = System.Drawing.Color.FromArgb(225, 225, 225);
            this.cmbCategoria.ItemHeight = 26;
            this.cmbCategoria.Location = new System.Drawing.Point(91, 46);
            this.cmbCategoria.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            this.cmbCategoria.Name = "cmbCategoria";
            this.cmbCategoria.Size = new System.Drawing.Size(300, 30);
            this.cmbCategoria.TabIndex = 6;

            // label7 — "Mínimo Stock:"
            this.label7.Anchor = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right));
            this.label7.AutoSize = false;
            this.label7.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label7.Font = new System.Drawing.Font("Bahnschrift Light", 10.5F);
            this.label7.ForeColor = System.Drawing.Color.FromArgb(155, 185, 165);
            this.label7.Name = "label7";
            this.label7.Padding = new System.Windows.Forms.Padding(8, 0, 6, 0);
            this.label7.TabIndex = 11;
            this.label7.Text = "Stock Mínimo:";
            this.label7.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // stMin
            this.stMin.Anchor = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right));
            this.stMin.BackColor = System.Drawing.Color.Transparent;
            this.stMin.BorderColor = System.Drawing.Color.FromArgb(55, 75, 62);
            this.stMin.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.stMin.FocusedState.BorderColor = System.Drawing.Color.FromArgb(39, 174, 96);
            this.stMin.Font = new System.Drawing.Font("Segoe UI", 10.5F);
            this.stMin.Increment = new decimal(new int[] { 5, 0, 0, 65536 });
            this.stMin.Location = new System.Drawing.Point(525, 44);
            this.stMin.Margin = new System.Windows.Forms.Padding(3, 5, 6, 5);
            this.stMin.Maximum = new decimal(new int[] { 1410065407, 2, 0, 0 });
            this.stMin.Name = "stMin";
            this.stMin.Padding = new System.Windows.Forms.Padding(0, 0, 4, 0);
            this.stMin.Size = new System.Drawing.Size(136, 30);
            this.stMin.TabIndex = 12;
            this.stMin.UpDownButtonFillColor = System.Drawing.Color.FromArgb(35, 50, 40);

            // btnProveedores — estilo secundario (span 4 columnas)
            this.btnProveedores.Anchor = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right));
            this.panelAntiParpadeo8.SetColumnSpan(this.btnProveedores, 4);
            this.btnProveedores.BorderRadius = 0;
            this.btnProveedores.BorderColor = System.Drawing.Color.FromArgb(55, 90, 68);
            this.btnProveedores.BorderThickness = 1;
            this.btnProveedores.DisabledState.BorderColor = System.Drawing.Color.FromArgb(45, 60, 50);
            this.btnProveedores.DisabledState.CustomBorderColor = System.Drawing.Color.FromArgb(45, 60, 50);
            this.btnProveedores.DisabledState.FillColor = System.Drawing.Color.FromArgb(35, 48, 40);
            this.btnProveedores.DisabledState.ForeColor = System.Drawing.Color.FromArgb(90, 110, 95);
            this.btnProveedores.FillColor = System.Drawing.Color.FromArgb(35, 50, 40);
            this.btnProveedores.Font = new System.Drawing.Font("Bahnschrift Light", 10.5F);
            this.btnProveedores.ForeColor = System.Drawing.Color.FromArgb(150, 190, 165);
            this.btnProveedores.HoverState.FillColor = System.Drawing.Color.FromArgb(42, 62, 50);
            this.btnProveedores.Location = new System.Drawing.Point(160, 86);
            this.btnProveedores.Margin = new System.Windows.Forms.Padding(160, 3, 160, 3);
            this.btnProveedores.Name = "btnProveedores";
            this.btnProveedores.Size = new System.Drawing.Size(450, 30);
            this.btnProveedores.TabIndex = 15;
            this.btnProveedores.Text = "Seleccionar Proveedores (0)";
            this.btnProveedores.Click += new System.EventHandler(this.btnProveedores_Click);

            // -------------------------------------------------------
            // panelAntiParpadeo5 — Botones de acción
            // -------------------------------------------------------
            this.panelAntiParpadeo5.ColumnCount = 2;
            this.panelAntiParpadeo5.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.panelAntiParpadeo5.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.panelAntiParpadeo5.Controls.Add(this.guna2Button1, 0, 0);
            this.panelAntiParpadeo5.Controls.Add(this.eliminarMaterialBoton, 1, 0);
            this.panelAntiParpadeo5.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelAntiParpadeo5.GrowStyle = System.Windows.Forms.TableLayoutPanelGrowStyle.AddColumns;
            this.panelAntiParpadeo5.Location = new System.Drawing.Point(0, 170);
            this.panelAntiParpadeo5.Name = "panelAntiParpadeo5";
            this.panelAntiParpadeo5.Padding = new System.Windows.Forms.Padding(100, 6, 100, 6);
            this.panelAntiParpadeo5.RowCount = 1;
            this.panelAntiParpadeo5.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.panelAntiParpadeo5.Size = new System.Drawing.Size(800, 48);
            this.panelAntiParpadeo5.TabIndex = 35;

            // guna2Button1 — "Guardar Material" (acción primaria)
            this.guna2Button1.Anchor = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right));
            this.guna2Button1.BorderRadius = 0;
            this.guna2Button1.DisabledState.BorderColor = System.Drawing.Color.FromArgb(50, 65, 55);
            this.guna2Button1.DisabledState.CustomBorderColor = System.Drawing.Color.FromArgb(50, 65, 55);
            this.guna2Button1.DisabledState.FillColor = System.Drawing.Color.FromArgb(45, 60, 50);
            this.guna2Button1.DisabledState.ForeColor = System.Drawing.Color.FromArgb(100, 120, 108);
            this.guna2Button1.FillColor = System.Drawing.Color.FromArgb(39, 174, 96);
            this.guna2Button1.Font = new System.Drawing.Font("Bahnschrift Light", 11F);
            this.guna2Button1.ForeColor = System.Drawing.Color.White;
            this.guna2Button1.HoverState.FillColor = System.Drawing.Color.FromArgb(46, 196, 110);
            this.guna2Button1.Location = new System.Drawing.Point(103, 9);
            this.guna2Button1.Margin = new System.Windows.Forms.Padding(3, 0, 6, 0);
            this.guna2Button1.Name = "guna2Button1";
            this.guna2Button1.Size = new System.Drawing.Size(291, 36);
            this.guna2Button1.TabIndex = 0;
            this.guna2Button1.Text = "Guardar Material";
            this.guna2Button1.Click += new System.EventHandler(this.guna2Button1_Click);

            // eliminarMaterialBoton — "Eliminar Material" (acción destructiva)
            this.eliminarMaterialBoton.Anchor = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right));
            this.eliminarMaterialBoton.BorderRadius = 0;
            this.eliminarMaterialBoton.DisabledState.BorderColor = System.Drawing.Color.FromArgb(50, 50, 50);
            this.eliminarMaterialBoton.DisabledState.CustomBorderColor = System.Drawing.Color.FromArgb(50, 50, 50);
            this.eliminarMaterialBoton.DisabledState.FillColor = System.Drawing.Color.FromArgb(40, 40, 40);
            this.eliminarMaterialBoton.DisabledState.ForeColor = System.Drawing.Color.FromArgb(80, 80, 80);
            this.eliminarMaterialBoton.FillColor = System.Drawing.Color.FromArgb(158, 45, 40);
            this.eliminarMaterialBoton.Font = new System.Drawing.Font("Bahnschrift Light", 11F);
            this.eliminarMaterialBoton.ForeColor = System.Drawing.Color.White;
            this.eliminarMaterialBoton.HoverState.FillColor = System.Drawing.Color.FromArgb(180, 55, 48);
            this.eliminarMaterialBoton.Location = new System.Drawing.Point(403, 9);
            this.eliminarMaterialBoton.Margin = new System.Windows.Forms.Padding(6, 0, 3, 0);
            this.eliminarMaterialBoton.Name = "eliminarMaterialBoton";
            this.eliminarMaterialBoton.Size = new System.Drawing.Size(291, 36);
            this.eliminarMaterialBoton.TabIndex = 1;
            this.eliminarMaterialBoton.Text = "Eliminar Material";
            this.eliminarMaterialBoton.Click += new System.EventHandler(this.eliminarMaterialBoton_Click);

            // -------------------------------------------------------
            // panelAntiParpadeo6 — Encabezado del listado + búsqueda
            // -------------------------------------------------------
            this.panelAntiParpadeo6.BackColor = System.Drawing.Color.FromArgb(26, 34, 30);
            this.panelAntiParpadeo6.ColumnCount = 3;
            this.panelAntiParpadeo6.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.panelAntiParpadeo6.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.panelAntiParpadeo6.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.panelAntiParpadeo6.Controls.Add(this.label8, 0, 0);
            this.panelAntiParpadeo6.Controls.Add(this.txtFiltroMaterial, 1, 0);
            this.panelAntiParpadeo6.Controls.Add(this.filtrarMaterial, 2, 0);
            this.panelAntiParpadeo6.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelAntiParpadeo6.GrowStyle = System.Windows.Forms.TableLayoutPanelGrowStyle.AddColumns;
            this.panelAntiParpadeo6.Location = new System.Drawing.Point(0, 218);
            this.panelAntiParpadeo6.Margin = new System.Windows.Forms.Padding(0);
            this.panelAntiParpadeo6.Name = "panelAntiParpadeo6";
            this.panelAntiParpadeo6.Padding = new System.Windows.Forms.Padding(12, 4, 12, 4);
            this.panelAntiParpadeo6.RowCount = 1;
            this.panelAntiParpadeo6.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.panelAntiParpadeo6.Size = new System.Drawing.Size(800, 36);
            this.panelAntiParpadeo6.TabIndex = 36;

            // label8 — "Listado de Materiales"
            this.label8.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label8.AutoSize = true;
            this.label8.Font = new System.Drawing.Font("Bahnschrift SemiBold", 10F, System.Drawing.FontStyle.Bold);
            this.label8.ForeColor = System.Drawing.Color.FromArgb(145, 175, 158);
            this.label8.Location = new System.Drawing.Point(15, 8);
            this.label8.Margin = new System.Windows.Forms.Padding(3, 0, 12, 0);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(148, 17);
            this.label8.TabIndex = 3;
            this.label8.Text = "LISTADO DE MATERIALES";
            this.label8.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // txtFiltroMaterial
            this.txtFiltroMaterial.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.txtFiltroMaterial.BorderColor = System.Drawing.Color.FromArgb(45, 60, 50);
            this.txtFiltroMaterial.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txtFiltroMaterial.DefaultText = "";
            this.txtFiltroMaterial.DisabledState.BorderColor = System.Drawing.Color.FromArgb(40, 55, 45);
            this.txtFiltroMaterial.DisabledState.FillColor = System.Drawing.Color.FromArgb(30, 40, 34);
            this.txtFiltroMaterial.DisabledState.ForeColor = System.Drawing.Color.FromArgb(90, 90, 90);
            this.txtFiltroMaterial.DisabledState.PlaceholderForeColor = System.Drawing.Color.FromArgb(90, 90, 90);
            this.txtFiltroMaterial.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtFiltroMaterial.FillColor = System.Drawing.Color.FromArgb(32, 44, 37);
            this.txtFiltroMaterial.FocusedState.BorderColor = System.Drawing.Color.FromArgb(39, 174, 96);
            this.txtFiltroMaterial.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.txtFiltroMaterial.ForeColor = System.Drawing.Color.FromArgb(210, 210, 210);
            this.txtFiltroMaterial.HoverState.BorderColor = System.Drawing.Color.FromArgb(60, 100, 75);
            this.txtFiltroMaterial.Location = new System.Drawing.Point(175, 5);
            this.txtFiltroMaterial.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.txtFiltroMaterial.Name = "txtFiltroMaterial";
            this.txtFiltroMaterial.PlaceholderText = "Buscar material por nombre...";
            this.txtFiltroMaterial.SelectedText = "";
            this.txtFiltroMaterial.Size = new System.Drawing.Size(500, 26);
            this.txtFiltroMaterial.TabIndex = 23;
            this.txtFiltroMaterial.TextChanged += new System.EventHandler(this.guna2TextBox1_TextChanged_1);

            // filtrarMaterial — botón de filtros avanzados
            this.filtrarMaterial.Anchor = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right));
            this.filtrarMaterial.BorderRadius = 0;
            this.filtrarMaterial.DisabledState.BorderColor = System.Drawing.Color.DarkGray;
            this.filtrarMaterial.DisabledState.CustomBorderColor = System.Drawing.Color.DarkGray;
            this.filtrarMaterial.DisabledState.FillColor = System.Drawing.Color.FromArgb(45, 60, 50);
            this.filtrarMaterial.DisabledState.ForeColor = System.Drawing.Color.FromArgb(90, 90, 90);
            this.filtrarMaterial.FillColor = System.Drawing.Color.FromArgb(35, 63, 43);
            this.filtrarMaterial.Font = new System.Drawing.Font("Bahnschrift Light", 10F);
            this.filtrarMaterial.ForeColor = System.Drawing.Color.White;
            this.filtrarMaterial.HoverState.FillColor = System.Drawing.Color.FromArgb(44, 78, 54);
            this.filtrarMaterial.Image = global::WP_Inventario.Properties.Resources.image__1_;
            this.filtrarMaterial.Location = new System.Drawing.Point(681, 4);
            this.filtrarMaterial.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.filtrarMaterial.Name = "filtrarMaterial";
            this.filtrarMaterial.Size = new System.Drawing.Size(46, 26);
            this.filtrarMaterial.TabIndex = 24;
            this.filtrarMaterial.Click += new System.EventHandler(this.filtrarMaterial_Click);

            // -------------------------------------------------------
            // materiales — Contenedor del DataGridView
            // -------------------------------------------------------
            this.materiales.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.materiales.ColumnCount = 1;
            this.materiales.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.materiales.Controls.Add(this.dgvMateriales, 0, 0);
            this.materiales.Location = new System.Drawing.Point(0, 254);
            this.materiales.Margin = new System.Windows.Forms.Padding(0);
            this.materiales.Name = "materiales";
            this.materiales.Padding = new System.Windows.Forms.Padding(0);
            this.materiales.RowCount = 1;
            this.materiales.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.materiales.Size = new System.Drawing.Size(800, 196);
            this.materiales.TabIndex = 37;

            // label9 (mantenido como campo, no visible en layout)
            this.label9.AutoSize = true;
            this.label9.Font = new System.Drawing.Font("Bahnschrift Light", 10.5F);
            this.label9.ForeColor = System.Drawing.Color.FromArgb(155, 185, 165);
            this.label9.Location = new System.Drawing.Point(0, 0);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(0, 0);
            this.label9.TabIndex = 0;
            this.label9.Text = "Buscar por nombre:";

            // -------------------------------------------------------
            // dgvMateriales
            // -------------------------------------------------------
            this.dgvMateriales.AllowUserToAddRows = false;
            this.dgvMateriales.AllowUserToDeleteRows = false;
            this.dgvMateriales.AllowUserToResizeColumns = false;
            this.dgvMateriales.AllowUserToResizeRows = false;
            dataGridViewCellStyle16.BackColor = System.Drawing.Color.FromArgb(29, 38, 35);
            this.dgvMateriales.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle16;
            this.dgvMateriales.BackgroundColor = System.Drawing.Color.FromArgb(29, 38, 35);
            dataGridViewCellStyle17.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle17.BackColor = System.Drawing.Color.FromArgb(32, 55, 40);
            dataGridViewCellStyle17.Font = new System.Drawing.Font("Bahnschrift Light", 10.5F);
            dataGridViewCellStyle17.ForeColor = System.Drawing.Color.FromArgb(195, 215, 202);
            dataGridViewCellStyle17.SelectionBackColor = System.Drawing.Color.FromArgb(32, 55, 40);
            dataGridViewCellStyle17.SelectionForeColor = System.Drawing.Color.FromArgb(195, 215, 202);
            dataGridViewCellStyle17.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvMateriales.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle17;
            this.dgvMateriales.ColumnHeadersHeight = 32;
            dataGridViewCellStyle18.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle18.BackColor = System.Drawing.Color.FromArgb(29, 38, 35);
            dataGridViewCellStyle18.Font = new System.Drawing.Font("Bahnschrift Light", 11F);
            dataGridViewCellStyle18.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle18.Padding = new System.Windows.Forms.Padding(14, 0, 14, 0);
            dataGridViewCellStyle18.SelectionBackColor = System.Drawing.Color.FromArgb(47, 84, 57);
            dataGridViewCellStyle18.SelectionForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle18.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvMateriales.DefaultCellStyle = dataGridViewCellStyle18;
            this.dgvMateriales.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvMateriales.GridColor = System.Drawing.Color.FromArgb(38, 50, 43);
            this.dgvMateriales.Location = new System.Drawing.Point(0, 0);
            this.dgvMateriales.Name = "dgvMateriales";
            this.dgvMateriales.ReadOnly = true;
            this.dgvMateriales.RowHeadersVisible = false;
            this.dgvMateriales.RowTemplate.DefaultCellStyle.Font = new System.Drawing.Font("Bahnschrift Light", 11F);
            this.dgvMateriales.RowTemplate.Height = 36;
            this.dgvMateriales.Size = new System.Drawing.Size(800, 196);
            this.dgvMateriales.TabIndex = 22;
            this.dgvMateriales.Theme = Guna.UI2.WinForms.Enums.DataGridViewPresetThemes.Dark;
            this.dgvMateriales.ThemeStyle.AlternatingRowsStyle.BackColor = System.Drawing.Color.FromArgb(29, 38, 35);
            this.dgvMateriales.ThemeStyle.AlternatingRowsStyle.Font = null;
            this.dgvMateriales.ThemeStyle.AlternatingRowsStyle.ForeColor = System.Drawing.Color.Empty;
            this.dgvMateriales.ThemeStyle.AlternatingRowsStyle.SelectionBackColor = System.Drawing.Color.Empty;
            this.dgvMateriales.ThemeStyle.AlternatingRowsStyle.SelectionForeColor = System.Drawing.Color.Empty;
            this.dgvMateriales.ThemeStyle.BackColor = System.Drawing.Color.FromArgb(29, 38, 35);
            this.dgvMateriales.ThemeStyle.GridColor = System.Drawing.Color.FromArgb(38, 50, 43);
            this.dgvMateriales.ThemeStyle.HeaderStyle.BackColor = System.Drawing.Color.FromArgb(32, 55, 40);
            this.dgvMateriales.ThemeStyle.HeaderStyle.BorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            this.dgvMateriales.ThemeStyle.HeaderStyle.Font = new System.Drawing.Font("Bahnschrift Light", 10.5F);
            this.dgvMateriales.ThemeStyle.HeaderStyle.ForeColor = System.Drawing.Color.FromArgb(195, 215, 202);
            this.dgvMateriales.ThemeStyle.HeaderStyle.HeaightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.dgvMateriales.ThemeStyle.HeaderStyle.Height = 32;
            this.dgvMateriales.ThemeStyle.ReadOnly = true;
            this.dgvMateriales.ThemeStyle.RowsStyle.BackColor = System.Drawing.Color.FromArgb(29, 38, 35);
            this.dgvMateriales.ThemeStyle.RowsStyle.BorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            this.dgvMateriales.ThemeStyle.RowsStyle.Font = new System.Drawing.Font("Bahnschrift Light", 11F);
            this.dgvMateriales.ThemeStyle.RowsStyle.ForeColor = System.Drawing.Color.White;
            this.dgvMateriales.ThemeStyle.RowsStyle.Height = 36;
            this.dgvMateriales.ThemeStyle.RowsStyle.SelectionBackColor = System.Drawing.Color.FromArgb(47, 84, 57);
            this.dgvMateriales.ThemeStyle.RowsStyle.SelectionForeColor = System.Drawing.Color.White;
            this.dgvMateriales.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvMateriales_CellClick);
            this.dgvMateriales.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvMateriales_CellDoubleClick);
            this.dgvMateriales.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.dgvMateriales_CellFormatting);

            // -------------------------------------------------------
            // Vista_Materiales — Form
            // -------------------------------------------------------
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(29, 38, 35);
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.materiales);
            this.Controls.Add(this.panelAntiParpadeo6);
            this.Controls.Add(this.panelAntiParpadeo5);
            this.Controls.Add(this.panelAntiParpadeo8);
            this.Controls.Add(this.panelAntiParpadeo1);
            this.Name = "Vista_Materiales";
            this.Padding = new System.Windows.Forms.Padding(0);
            this.Text = "Gestión de Materiales";

            this.guna2ContextMenuStrip1.ResumeLayout(false);
            this.panelAntiParpadeo1.ResumeLayout(false);
            this.panelAntiParpadeo8.ResumeLayout(false);
            this.panelAntiParpadeo8.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.stAct)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.stMin)).EndInit();
            this.panelAntiParpadeo5.ResumeLayout(false);
            this.panelAntiParpadeo6.ResumeLayout(false);
            this.panelAntiParpadeo6.PerformLayout();
            this.materiales.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvMateriales)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private PanelAntiParpadeo panelAntiParpadeo1;
        private System.Windows.Forms.Label label1;
        private PanelAntiParpadeo panelAntiParpadeo8;
        private Guna.UI2.WinForms.Guna2NumericUpDown stMin;
        private System.Windows.Forms.Label label7;
        private Guna.UI2.WinForms.Guna2NumericUpDown stAct;
        private System.Windows.Forms.Label label5;
        private Guna.UI2.WinForms.Guna2ComboBox cmbCategoria;
        private System.Windows.Forms.Label label3;
        private Guna.UI2.WinForms.Guna2TextBox nombreMaterial;
        private System.Windows.Forms.Label label2;
        private PanelAntiParpadeo panelAntiParpadeo5;
        private Guna.UI2.WinForms.Guna2Button guna2Button1;
        private Guna.UI2.WinForms.Guna2Button eliminarMaterialBoton;
        private PanelAntiParpadeo panelAntiParpadeo6;
        private System.Windows.Forms.Label label8;
        private Guna.UI2.WinForms.Guna2TextBox txtFiltroMaterial;
        private Guna.UI2.WinForms.Guna2Button filtrarMaterial;
        private PanelAntiParpadeo materiales;
        private System.Windows.Forms.Label label9;
        private Guna.UI2.WinForms.Guna2DataGridView dgvMateriales;
        private Guna.UI2.WinForms.Guna2ContextMenuStrip guna2ContextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem categoríaToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem cantidadToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem minimoToolStripMenuItem;
        private Guna.UI2.WinForms.Guna2Button btnProveedores;
    }
}
