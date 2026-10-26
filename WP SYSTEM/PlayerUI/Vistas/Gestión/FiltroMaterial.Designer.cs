namespace WP_Inventario.Vistas
{
    partial class FiltroMaterial
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
            this.panelAntiParpadeo1 = new WP_Inventario.PanelAntiParpadeo();
            this.LimpiarFiltros = new Guna.UI2.WinForms.Guna2Button();
            this.guna2ControlBox1 = new Guna.UI2.WinForms.Guna2ControlBox();
            this.label7 = new System.Windows.Forms.Label();
            this.hastaCantidadFiltro = new Guna.UI2.WinForms.Guna2NumericUpDown();
            this.desdeCantidadFiltro = new Guna.UI2.WinForms.Guna2NumericUpDown();
            this.label6 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.flCategoria = new System.Windows.Forms.CheckedListBox();
            this.panelAntiParpadeo2 = new WP_Inventario.PanelAntiParpadeo();
            this.label8 = new System.Windows.Forms.Label();
            this.sionoBajo = new Guna.UI2.WinForms.Guna2ToggleSwitch();
            this.AplicarFiltros = new Guna.UI2.WinForms.Guna2Button();
            this.label9 = new System.Windows.Forms.Label();
            this.clbProveedoresFiltro = new System.Windows.Forms.CheckedListBox();
            this.panelAntiParpadeo1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.hastaCantidadFiltro)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.desdeCantidadFiltro)).BeginInit();
            this.panelAntiParpadeo2.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelAntiParpadeo1
            // 
            this.panelAntiParpadeo1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(30)))), ((int)(((byte)(25)))));
            this.panelAntiParpadeo1.ColumnCount = 2;
            this.panelAntiParpadeo1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.panelAntiParpadeo1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 195F));
            this.panelAntiParpadeo1.Controls.Add(this.LimpiarFiltros, 0, 10);
            this.panelAntiParpadeo1.Controls.Add(this.guna2ControlBox1, 1, 0);
            this.panelAntiParpadeo1.Controls.Add(this.label7, 0, 4);
            this.panelAntiParpadeo1.Controls.Add(this.hastaCantidadFiltro, 1, 3);
            this.panelAntiParpadeo1.Controls.Add(this.desdeCantidadFiltro, 1, 2);
            this.panelAntiParpadeo1.Controls.Add(this.label6, 0, 3);
            this.panelAntiParpadeo1.Controls.Add(this.label5, 0, 2);
            this.panelAntiParpadeo1.Controls.Add(this.label3, 0, 1);
            this.panelAntiParpadeo1.Controls.Add(this.flCategoria, 0, 5);
            this.panelAntiParpadeo1.Controls.Add(this.label9, 0, 6);
            this.panelAntiParpadeo1.Controls.Add(this.clbProveedoresFiltro, 0, 7);
            this.panelAntiParpadeo1.Controls.Add(this.panelAntiParpadeo2, 0, 8);
            this.panelAntiParpadeo1.Controls.Add(this.AplicarFiltros, 0, 9);
            this.panelAntiParpadeo1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelAntiParpadeo1.Location = new System.Drawing.Point(0, 0);
            this.panelAntiParpadeo1.Name = "panelAntiParpadeo1";
            this.panelAntiParpadeo1.Padding = new System.Windows.Forms.Padding(10, 0, 10, 10);
            this.panelAntiParpadeo1.RowCount = 11;
            this.panelAntiParpadeo1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F)); // 0: ControlBox
            this.panelAntiParpadeo1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 45F)); // 1: Título Stock
            this.panelAntiParpadeo1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 45F)); // 2: Desde Stock
            this.panelAntiParpadeo1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 45F)); // 3: Hasta Stock
            this.panelAntiParpadeo1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F)); // 4: Título Categorías
            this.panelAntiParpadeo1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 110F)); // 5: ListBox Categorías
            this.panelAntiParpadeo1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F)); // 6: Título Proveedores
            this.panelAntiParpadeo1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 110F)); // 7: ListBox Proveedores
            this.panelAntiParpadeo1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 45F));  // 8: Switch Stock Bajo
            this.panelAntiParpadeo1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50F));  // 9: Aplicar
            this.panelAntiParpadeo1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50F));  // 10: Borrar
            this.panelAntiParpadeo1.Size = new System.Drawing.Size(284, 650);
            this.panelAntiParpadeo1.TabIndex = 0;
            this.panelAntiParpadeo1.Paint += new System.Windows.Forms.PaintEventHandler(this.panelAntiParpadeo1_Paint);
            // 
            // LimpiarFiltros
            // 
            this.LimpiarFiltros.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.panelAntiParpadeo1.SetColumnSpan(this.LimpiarFiltros, 2);
            this.LimpiarFiltros.BorderRadius = 5;
            this.LimpiarFiltros.DisabledState.BorderColor = System.Drawing.Color.DarkGray;
            this.LimpiarFiltros.DisabledState.CustomBorderColor = System.Drawing.Color.DarkGray;
            this.LimpiarFiltros.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(169)))), ((int)(((byte)(169)))), ((int)(((byte)(169)))));
            this.LimpiarFiltros.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(141)))), ((int)(((byte)(141)))), ((int)(((byte)(141)))));
            this.LimpiarFiltros.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.LimpiarFiltros.Font = new System.Drawing.Font("Bahnschrift SemiLight", 12F);
            this.LimpiarFiltros.ForeColor = System.Drawing.Color.White;
            this.LimpiarFiltros.Location = new System.Drawing.Point(13, 608);
            this.LimpiarFiltros.Name = "LimpiarFiltros";
            this.LimpiarFiltros.Size = new System.Drawing.Size(258, 32);
            this.LimpiarFiltros.TabIndex = 22;
            this.LimpiarFiltros.Text = "BORRAR FILTROS";
            this.LimpiarFiltros.Click += new System.EventHandler(this.LimpiarFiltros_Click);
            // 
            // guna2ControlBox1
            // 
            this.guna2ControlBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.guna2ControlBox1.FillColor = System.Drawing.Color.Transparent;
            this.guna2ControlBox1.IconColor = System.Drawing.Color.Gray;
            this.guna2ControlBox1.Location = new System.Drawing.Point(234, 3);
            this.guna2ControlBox1.Name = "guna2ControlBox1";
            this.guna2ControlBox1.Size = new System.Drawing.Size(37, 27);
            this.guna2ControlBox1.TabIndex = 14;
            this.guna2ControlBox1.HoverState.IconColor = System.Drawing.Color.White;
            this.guna2ControlBox1.HoverState.FillColor = System.Drawing.Color.FromArgb(180, 50, 50);
            // 
            // label7
            // 
            this.label7.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.label7.AutoSize = true;
            this.panelAntiParpadeo1.SetColumnSpan(this.label7, 2);
            this.label7.Font = new System.Drawing.Font("Bahnschrift SemiBold", 11F, System.Drawing.FontStyle.Bold);
            this.label7.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(135)))), ((int)(((byte)(84)))));
            this.label7.Location = new System.Drawing.Point(13, 209);
            this.label7.Name = "label7";
            this.label7.Padding = new System.Windows.Forms.Padding(10, 5, 0, 5);
            this.label7.Size = new System.Drawing.Size(100, 33);
            this.label7.TabIndex = 16;
            this.label7.Text = "Categorías";
            this.label7.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            // 
            // hastaCantidadFiltro
            // 
            this.hastaCantidadFiltro.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.hastaCantidadFiltro.BackColor = System.Drawing.Color.Transparent;
            this.hastaCantidadFiltro.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(65)))));
            this.hastaCantidadFiltro.BorderRadius = 4;
            this.hastaCantidadFiltro.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.hastaCantidadFiltro.DecimalPlaces = 2;
            this.hastaCantidadFiltro.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(50)))), ((int)(((byte)(40)))));
            this.hastaCantidadFiltro.Font = new System.Drawing.Font("Bahnschrift Light", 11F);
            this.hastaCantidadFiltro.ForeColor = System.Drawing.Color.White;
            this.hastaCantidadFiltro.Increment = new decimal(new int[] {
            5,
            0,
            0,
            65536});
            this.hastaCantidadFiltro.Location = new System.Drawing.Point(82, 178);
            this.hastaCantidadFiltro.Maximum = new decimal(new int[] {
            1410065407,
            2,
            0,
            0});
            this.hastaCantidadFiltro.Size = new System.Drawing.Size(189, 32);
            this.hastaCantidadFiltro.TabIndex = 13;
            this.hastaCantidadFiltro.FocusedState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(39)))), ((int)(((byte)(174)))), ((int)(((byte)(96)))));
            this.hastaCantidadFiltro.UpDownButtonFillColor = System.Drawing.Color.FromArgb(((int)(((byte)(47)))), ((int)(((byte)(84)))), ((int)(((byte)(57)))));
            this.hastaCantidadFiltro.UpDownButtonForeColor = System.Drawing.Color.White;
            // 
            // desdeCantidadFiltro
            // 
            this.desdeCantidadFiltro.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.desdeCantidadFiltro.BackColor = System.Drawing.Color.Transparent;
            this.desdeCantidadFiltro.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(65)))));
            this.desdeCantidadFiltro.BorderRadius = 4;
            this.desdeCantidadFiltro.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.desdeCantidadFiltro.DecimalPlaces = 2;
            this.desdeCantidadFiltro.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(50)))), ((int)(((byte)(40)))));
            this.desdeCantidadFiltro.Font = new System.Drawing.Font("Bahnschrift Light", 11F);
            this.desdeCantidadFiltro.ForeColor = System.Drawing.Color.White;
            this.desdeCantidadFiltro.Increment = new decimal(new int[] {
            5,
            0,
            0,
            65536});
            this.desdeCantidadFiltro.Location = new System.Drawing.Point(82, 142);
            this.desdeCantidadFiltro.Maximum = new decimal(new int[] {
            1410065407,
            2,
            0,
            0});
            this.desdeCantidadFiltro.Size = new System.Drawing.Size(189, 32);
            this.desdeCantidadFiltro.TabIndex = 11;
            this.desdeCantidadFiltro.FocusedState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(39)))), ((int)(((byte)(174)))), ((int)(((byte)(96)))));
            this.desdeCantidadFiltro.UpDownButtonFillColor = System.Drawing.Color.FromArgb(((int)(((byte)(47)))), ((int)(((byte)(84)))), ((int)(((byte)(57)))));
            this.desdeCantidadFiltro.UpDownButtonForeColor = System.Drawing.Color.White;
            // 
            // label6
            // 
            this.label6.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Bahnschrift Light", 10F);
            this.label6.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.label6.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.label6.Location = new System.Drawing.Point(13, 186);
            this.label6.Name = "label6";
            this.label6.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.label6.Size = new System.Drawing.Size(56, 17);
            this.label6.TabIndex = 12;
            this.label6.Text = "Hasta:";
            this.label6.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label5
            // 
            this.label5.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Bahnschrift Light", 10F);
            this.label5.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.label5.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.label5.Location = new System.Drawing.Point(13, 150);
            this.label5.Name = "label5";
            this.label5.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.label5.Size = new System.Drawing.Size(60, 17);
            this.label5.TabIndex = 10;
            this.label5.Text = "Desde:";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.label3.AutoSize = true;
            this.panelAntiParpadeo1.SetColumnSpan(this.label3, 2);
            this.label3.Font = new System.Drawing.Font("Bahnschrift SemiBold", 11F, System.Drawing.FontStyle.Bold);
            this.label3.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(135)))), ((int)(((byte)(84)))));
            this.label3.Location = new System.Drawing.Point(13, 107);
            this.label3.Name = "label3";
            this.label3.Padding = new System.Windows.Forms.Padding(10, 15, 0, 0);
            this.label3.Size = new System.Drawing.Size(126, 33);
            this.label3.TabIndex = 9;
            this.label3.Text = "Rango de Stock";
            this.label3.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            // 
            // flCategoria
            // 
            this.flCategoria.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(50)))), ((int)(((byte)(40)))));
            this.flCategoria.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.flCategoria.CheckOnClick = true;
            this.panelAntiParpadeo1.SetColumnSpan(this.flCategoria, 2);
            this.flCategoria.Font = new System.Drawing.Font("Bahnschrift Light", 10.5F);
            this.flCategoria.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(230)))), ((int)(((byte)(230)))));
            this.flCategoria.FormattingEnabled = true;
            this.flCategoria.Location = new System.Drawing.Point(13, 245);
            this.flCategoria.Name = "flCategoria";
            this.flCategoria.Size = new System.Drawing.Size(258, 80);
            this.flCategoria.TabIndex = 17;
            this.flCategoria.SelectedIndexChanged += new System.EventHandler(this.flCategoria_SelectedIndexChanged);
            // 
            // label9
            // 
            this.label9.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.label9.AutoSize = true;
            this.panelAntiParpadeo1.SetColumnSpan(this.label9, 2);
            this.label9.Font = new System.Drawing.Font("Bahnschrift SemiBold", 11F, System.Drawing.FontStyle.Bold);
            this.label9.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(135)))), ((int)(((byte)(84)))));
            this.label9.Location = new System.Drawing.Point(13, 332);
            this.label9.Name = "label9";
            this.label9.Padding = new System.Windows.Forms.Padding(10, 5, 0, 5);
            this.label9.Size = new System.Drawing.Size(107, 30);
            this.label9.TabIndex = 23;
            this.label9.Text = "Proveedores";
            this.label9.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            // 
            // clbProveedoresFiltro
            // 
            this.clbProveedoresFiltro.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(50)))), ((int)(((byte)(40)))));
            this.clbProveedoresFiltro.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.clbProveedoresFiltro.CheckOnClick = true;
            this.panelAntiParpadeo1.SetColumnSpan(this.clbProveedoresFiltro, 2);
            this.clbProveedoresFiltro.Font = new System.Drawing.Font("Bahnschrift Light", 10.5F);
            this.clbProveedoresFiltro.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(230)))), ((int)(((byte)(230)))));
            this.clbProveedoresFiltro.FormattingEnabled = true;
            this.clbProveedoresFiltro.Location = new System.Drawing.Point(13, 365);
            this.clbProveedoresFiltro.Name = "clbProveedoresFiltro";
            this.clbProveedoresFiltro.Size = new System.Drawing.Size(258, 85);
            this.clbProveedoresFiltro.TabIndex = 24;
            // 
            // panelAntiParpadeo2
            // 
            this.panelAntiParpadeo2.ColumnCount = 2;
            this.panelAntiParpadeo1.SetColumnSpan(this.panelAntiParpadeo2, 2);
            this.panelAntiParpadeo2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 62.23022F));
            this.panelAntiParpadeo2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 37.76978F));
            this.panelAntiParpadeo2.Controls.Add(this.label8, 0, 0);
            this.panelAntiParpadeo2.Controls.Add(this.sionoBajo, 1, 0);
            this.panelAntiParpadeo2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelAntiParpadeo2.Location = new System.Drawing.Point(13, 465);
            this.panelAntiParpadeo2.Name = "panelAntiParpadeo2";
            this.panelAntiParpadeo2.RowCount = 1;
            this.panelAntiParpadeo2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.panelAntiParpadeo2.Size = new System.Drawing.Size(258, 34);
            this.panelAntiParpadeo2.TabIndex = 18;
            // 
            // label8
            // 
            this.label8.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label8.AutoSize = true;
            this.label8.Font = new System.Drawing.Font("Bahnschrift Light", 11.5F);
            this.label8.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(230)))), ((int)(((byte)(230)))));
            this.label8.Location = new System.Drawing.Point(3, 0);
            this.label8.Name = "label8";
            this.label8.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.label8.Size = new System.Drawing.Size(154, 34);
            this.label8.TabIndex = 17;
            this.label8.Text = "Solo Stock Bajo";
            this.label8.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // sionoBajo
            // 
            this.sionoBajo.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.sionoBajo.Animated = true;
            this.sionoBajo.CheckedState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(47)))), ((int)(((byte)(84)))), ((int)(((byte)(57)))));
            this.sionoBajo.CheckedState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(47)))), ((int)(((byte)(84)))), ((int)(((byte)(57)))));
            this.sionoBajo.CheckedState.InnerBorderColor = System.Drawing.Color.White;
            this.sionoBajo.CheckedState.InnerColor = System.Drawing.Color.White;
            this.sionoBajo.Location = new System.Drawing.Point(183, 11);
            this.sionoBajo.Name = "sionoBajo";
            this.sionoBajo.Size = new System.Drawing.Size(52, 21);
            this.sionoBajo.TabIndex = 18;
            this.sionoBajo.UncheckedState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(125)))), ((int)(((byte)(137)))), ((int)(((byte)(149)))));
            this.sionoBajo.UncheckedState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(125)))), ((int)(((byte)(137)))), ((int)(((byte)(149)))));
            this.sionoBajo.UncheckedState.InnerBorderColor = System.Drawing.Color.White;
            this.sionoBajo.UncheckedState.InnerColor = System.Drawing.Color.White;
            this.sionoBajo.CheckedChanged += new System.EventHandler(this.sionoBajo_CheckedChanged);
            // 
            // AplicarFiltros
            // 
            this.AplicarFiltros.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.panelAntiParpadeo1.SetColumnSpan(this.AplicarFiltros, 2);
            this.AplicarFiltros.BorderRadius = 5;
            this.AplicarFiltros.DisabledState.BorderColor = System.Drawing.Color.DarkGray;
            this.AplicarFiltros.DisabledState.CustomBorderColor = System.Drawing.Color.DarkGray;
            this.AplicarFiltros.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(169)))), ((int)(((byte)(169)))), ((int)(((byte)(169)))));
            this.AplicarFiltros.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(141)))), ((int)(((byte)(141)))), ((int)(((byte)(141)))));
            this.AplicarFiltros.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(47)))), ((int)(((byte)(84)))), ((int)(((byte)(57)))));
            this.AplicarFiltros.Font = new System.Drawing.Font("Bahnschrift SemiBold", 12F, System.Drawing.FontStyle.Bold);
            this.AplicarFiltros.ForeColor = System.Drawing.Color.White;
            this.AplicarFiltros.Location = new System.Drawing.Point(13, 570);
            this.AplicarFiltros.Name = "AplicarFiltros";
            this.AplicarFiltros.Size = new System.Drawing.Size(258, 34);
            this.AplicarFiltros.TabIndex = 19;
            this.AplicarFiltros.Text = "APLICAR FILTROS";
            this.AplicarFiltros.Click += new System.EventHandler(this.AplicarFiltros_Click);
            // 
            // FiltroMaterial
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 650);
            this.Controls.Add(this.panelAntiParpadeo1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "FiltroMaterial";
            this.Text = "FiltroMaterial";
            this.Load += new System.EventHandler(this.FiltroMaterial_Load);
            this.panelAntiParpadeo1.ResumeLayout(false);
            this.panelAntiParpadeo1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.hastaCantidadFiltro)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.desdeCantidadFiltro)).EndInit();
            this.panelAntiParpadeo2.ResumeLayout(false);
            this.panelAntiParpadeo2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private PanelAntiParpadeo panelAntiParpadeo1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label6;
        private Guna.UI2.WinForms.Guna2NumericUpDown hastaCantidadFiltro;
        private System.Windows.Forms.Label label5;
        private Guna.UI2.WinForms.Guna2NumericUpDown desdeCantidadFiltro;
        private Guna.UI2.WinForms.Guna2ControlBox guna2ControlBox1;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.CheckedListBox flCategoria;
        private PanelAntiParpadeo panelAntiParpadeo2;
        private System.Windows.Forms.Label label8;
        private Guna.UI2.WinForms.Guna2ToggleSwitch sionoBajo;
        private Guna.UI2.WinForms.Guna2Button LimpiarFiltros;
        private Guna.UI2.WinForms.Guna2Button AplicarFiltros;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.CheckedListBox clbProveedoresFiltro;
    }
}
