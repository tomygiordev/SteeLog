namespace WP_Inventario
{
    partial class VistaGeneral
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(VistaGeneral));
            this.panelSideMenu = new System.Windows.Forms.Panel();
            this.btnSalir = new System.Windows.Forms.Button();
            this.btnInventario = new System.Windows.Forms.Button();
            this.panelInventario = new System.Windows.Forms.Panel();
            this.btnInventarioConteo = new System.Windows.Forms.Button();
            this.btnInventarioGeneral = new System.Windows.Forms.Button();
            this.panelReportes = new System.Windows.Forms.Panel();
            this.button10 = new System.Windows.Forms.Button();
            this.button12 = new System.Windows.Forms.Button();
            this.button13 = new System.Windows.Forms.Button();
            this.btnReportes = new System.Windows.Forms.Button();
            this.panelMovimientos = new System.Windows.Forms.Panel();
            this.btnEntrada = new System.Windows.Forms.Button();
            this.btnHistorial = new System.Windows.Forms.Button();
            this.btnMovimientos = new System.Windows.Forms.Button();
            this.panelGestion = new System.Windows.Forms.Panel();
            this.btnRecortes = new System.Windows.Forms.Button();
            this.btnHerramientas = new System.Windows.Forms.Button();
            this.btnInsumos = new System.Windows.Forms.Button();
            this.btnFletes = new System.Windows.Forms.Button();
            this.Ubicaciones = new System.Windows.Forms.Button();
            this.Obras = new System.Windows.Forms.Button();
            this.btnProveedores = new System.Windows.Forms.Button();
            this.btnCategorias = new System.Windows.Forms.Button();
            this.btnMateriales = new System.Windows.Forms.Button();
            this.btnGestion = new System.Windows.Forms.Button();
            this.panelLogo = new System.Windows.Forms.Panel();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.panelChildForm = new System.Windows.Forms.Panel();
            this.panelMaximizar = new Guna.UI2.WinForms.Guna2Panel();
            this.guna2VScrollBar2 = new Guna.UI2.WinForms.Guna2VScrollBar();
            this.panelSideMenu.SuspendLayout();
            this.panelReportes.SuspendLayout();
            this.panelMovimientos.SuspendLayout();
            this.panelGestion.SuspendLayout();
            this.panelLogo.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // panelSideMenu
            // 
            this.panelSideMenu.AutoScroll = true;
            this.panelSideMenu.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(11)))), ((int)(((byte)(7)))), ((int)(((byte)(17)))));
            this.panelSideMenu.Controls.Add(this.btnSalir);
            this.panelSideMenu.Controls.Add(this.panelInventario);
            this.panelSideMenu.Controls.Add(this.btnInventario);
            this.panelSideMenu.Controls.Add(this.panelReportes);
            this.panelSideMenu.Controls.Add(this.btnReportes);
            this.panelSideMenu.Controls.Add(this.panelMovimientos);
            this.panelSideMenu.Controls.Add(this.btnMovimientos);
            this.panelSideMenu.Controls.Add(this.panelGestion);
            this.panelSideMenu.Controls.Add(this.btnGestion);
            this.panelSideMenu.Controls.Add(this.panelLogo);
            this.panelSideMenu.Dock = System.Windows.Forms.DockStyle.Left;
            this.panelSideMenu.Location = new System.Drawing.Point(0, 40);
            this.panelSideMenu.Name = "panelSideMenu";
            this.panelSideMenu.Size = new System.Drawing.Size(250, 560);
            this.panelSideMenu.TabIndex = 0;
            // 
            // btnSalir
            // 
            this.btnSalir.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.btnSalir.FlatAppearance.BorderSize = 0;
            this.btnSalir.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(33)))), ((int)(((byte)(38)))));
            this.btnSalir.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(24)))), ((int)(((byte)(29)))), ((int)(((byte)(34)))));
            this.btnSalir.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSalir.ForeColor = System.Drawing.Color.Silver;
            this.btnSalir.Image = ((System.Drawing.Image)(resources.GetObject("btnSalir.Image")));
            this.btnSalir.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnSalir.Location = new System.Drawing.Point(0, 604);
            this.btnSalir.Name = "btnSalir";
            this.btnSalir.Padding = new System.Windows.Forms.Padding(5, 0, 0, 0);
            this.btnSalir.Size = new System.Drawing.Size(233, 45);
            this.btnSalir.TabIndex = 9;
            this.btnSalir.Text = "Salir";
            this.btnSalir.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnSalir.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnSalir.UseVisualStyleBackColor = true;
            this.btnSalir.Click += new System.EventHandler(this.btnExit_Click);
            // 
            // btnInventario
            // 
            this.btnInventario.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnInventario.FlatAppearance.BorderSize = 0;
            this.btnInventario.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(33)))), ((int)(((byte)(38)))));
            this.btnInventario.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(24)))), ((int)(((byte)(29)))), ((int)(((byte)(34)))));
            this.btnInventario.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnInventario.ForeColor = System.Drawing.Color.Silver;
            this.btnInventario.Image = ((System.Drawing.Image)(resources.GetObject("btnInventario.Image")));
            this.btnInventario.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnInventario.Location = new System.Drawing.Point(0, 559);
            this.btnInventario.Name = "btnInventario";
            this.btnInventario.Padding = new System.Windows.Forms.Padding(5, 0, 0, 0);
            this.btnInventario.Size = new System.Drawing.Size(233, 45);
            this.btnInventario.TabIndex = 8;
            this.btnInventario.Text = "Inventario";
            this.btnInventario.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnInventario.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnInventario.UseVisualStyleBackColor = true;
            this.btnInventario.Click += new System.EventHandler(this.btnEqualizer_Click);
            //
            // panelInventario
            //
            this.panelInventario.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(38)))), ((int)(((byte)(39)))));
            this.panelInventario.Controls.Add(this.btnInventarioGeneral);
            this.panelInventario.Controls.Add(this.btnInventarioConteo);
            this.panelInventario.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelInventario.Location = new System.Drawing.Point(0, 604);
            this.panelInventario.Name = "panelInventario";
            this.panelInventario.Size = new System.Drawing.Size(233, 80);
            this.panelInventario.TabIndex = 11;
            //
            // btnInventarioConteo
            //
            this.btnInventarioConteo.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnInventarioConteo.FlatAppearance.BorderSize = 0;
            this.btnInventarioConteo.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(15)))), ((int)(((byte)(20)))));
            this.btnInventarioConteo.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(34)))), ((int)(((byte)(35)))));
            this.btnInventarioConteo.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnInventarioConteo.ForeColor = System.Drawing.Color.Silver;
            this.btnInventarioConteo.Location = new System.Drawing.Point(0, 40);
            this.btnInventarioConteo.Name = "btnInventarioConteo";
            this.btnInventarioConteo.Padding = new System.Windows.Forms.Padding(35, 0, 0, 0);
            this.btnInventarioConteo.Size = new System.Drawing.Size(233, 40);
            this.btnInventarioConteo.TabIndex = 1;
            this.btnInventarioConteo.Text = "Conteo";
            this.btnInventarioConteo.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnInventarioConteo.UseVisualStyleBackColor = true;
            this.btnInventarioConteo.Click += new System.EventHandler(this.btnInventarioConteo_Click);
            //
            // btnInventarioGeneral
            //
            this.btnInventarioGeneral.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnInventarioGeneral.FlatAppearance.BorderSize = 0;
            this.btnInventarioGeneral.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(15)))), ((int)(((byte)(20)))));
            this.btnInventarioGeneral.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(34)))), ((int)(((byte)(35)))));
            this.btnInventarioGeneral.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnInventarioGeneral.ForeColor = System.Drawing.Color.Silver;
            this.btnInventarioGeneral.Location = new System.Drawing.Point(0, 0);
            this.btnInventarioGeneral.Name = "btnInventarioGeneral";
            this.btnInventarioGeneral.Padding = new System.Windows.Forms.Padding(35, 0, 0, 0);
            this.btnInventarioGeneral.Size = new System.Drawing.Size(233, 40);
            this.btnInventarioGeneral.TabIndex = 0;
            this.btnInventarioGeneral.Text = "General";
            this.btnInventarioGeneral.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnInventarioGeneral.UseVisualStyleBackColor = true;
            this.btnInventarioGeneral.Click += new System.EventHandler(this.btnInventarioGeneral_Click);
            // 
            // panelReportes
            // 
            this.panelReportes.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(38)))), ((int)(((byte)(39)))));
            this.panelReportes.Controls.Add(this.button10);
            this.panelReportes.Controls.Add(this.button12);
            this.panelReportes.Controls.Add(this.button13);
            this.panelReportes.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelReportes.Location = new System.Drawing.Point(0, 429);
            this.panelReportes.Name = "panelReportes";
            this.panelReportes.Size = new System.Drawing.Size(233, 130);
            this.panelReportes.TabIndex = 7;
            // 
            // button10
            // 
            this.button10.Dock = System.Windows.Forms.DockStyle.Top;
            this.button10.FlatAppearance.BorderSize = 0;
            this.button10.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(15)))), ((int)(((byte)(20)))));
            this.button10.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(34)))), ((int)(((byte)(35)))));
            this.button10.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button10.ForeColor = System.Drawing.Color.Silver;
            this.button10.Location = new System.Drawing.Point(0, 80);
            this.button10.Name = "button10";
            this.button10.Padding = new System.Windows.Forms.Padding(35, 0, 0, 0);
            this.button10.Size = new System.Drawing.Size(233, 40);
            this.button10.TabIndex = 2;
            this.button10.Text = "Informe de obras";
            this.button10.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.button10.UseVisualStyleBackColor = true;
            this.button10.Click += new System.EventHandler(this.button10_Click);
            // 
            // button12
            // 
            this.button12.Dock = System.Windows.Forms.DockStyle.Top;
            this.button12.FlatAppearance.BorderSize = 0;
            this.button12.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(15)))), ((int)(((byte)(20)))));
            this.button12.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(34)))), ((int)(((byte)(35)))));
            this.button12.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button12.ForeColor = System.Drawing.Color.Silver;
            this.button12.Location = new System.Drawing.Point(0, 40);
            this.button12.Name = "button12";
            this.button12.Padding = new System.Windows.Forms.Padding(35, 0, 0, 0);
            this.button12.Size = new System.Drawing.Size(233, 40);
            this.button12.TabIndex = 1;
            this.button12.Text = "Kardex";
            this.button12.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.button12.UseVisualStyleBackColor = true;
            this.button12.Click += new System.EventHandler(this.button12_Click);
            // 
            // button13
            // 
            this.button13.Dock = System.Windows.Forms.DockStyle.Top;
            this.button13.FlatAppearance.BorderSize = 0;
            this.button13.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(15)))), ((int)(((byte)(20)))));
            this.button13.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(34)))), ((int)(((byte)(35)))));
            this.button13.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button13.ForeColor = System.Drawing.Color.Silver;
            this.button13.Location = new System.Drawing.Point(0, 0);
            this.button13.Name = "button13";
            this.button13.Padding = new System.Windows.Forms.Padding(35, 0, 0, 0);
            this.button13.Size = new System.Drawing.Size(233, 40);
            this.button13.TabIndex = 0;
            this.button13.Text = "Stock";
            this.button13.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.button13.UseVisualStyleBackColor = true;
            this.button13.Click += new System.EventHandler(this.button13_Click);
            // 
            // btnReportes
            // 
            this.btnReportes.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnReportes.FlatAppearance.BorderSize = 0;
            this.btnReportes.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(33)))), ((int)(((byte)(38)))));
            this.btnReportes.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(24)))), ((int)(((byte)(29)))), ((int)(((byte)(34)))));
            this.btnReportes.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnReportes.ForeColor = System.Drawing.Color.Silver;
            this.btnReportes.Image = ((System.Drawing.Image)(resources.GetObject("btnReportes.Image")));
            this.btnReportes.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnReportes.Location = new System.Drawing.Point(0, 384);
            this.btnReportes.Name = "btnReportes";
            this.btnReportes.Padding = new System.Windows.Forms.Padding(5, 0, 0, 0);
            this.btnReportes.Size = new System.Drawing.Size(233, 45);
            this.btnReportes.TabIndex = 6;
            this.btnReportes.Text = "Reportes";
            this.btnReportes.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnReportes.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnReportes.UseVisualStyleBackColor = true;
            this.btnReportes.Click += new System.EventHandler(this.btnTools_Click);
            // 
            // panelMovimientos
            // 
            this.panelMovimientos.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(38)))), ((int)(((byte)(39)))));
            this.panelMovimientos.Controls.Add(this.btnHistorial);
            this.panelMovimientos.Controls.Add(this.btnEntrada);
            this.panelMovimientos.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelMovimientos.Location = new System.Drawing.Point(0, 384);
            this.panelMovimientos.Name = "panelMovimientos";
            this.panelMovimientos.Size = new System.Drawing.Size(233, 80);
            this.panelMovimientos.TabIndex = 10;
            // 
            // btnEntrada
            // 
            this.btnEntrada.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnEntrada.FlatAppearance.BorderSize = 0;
            this.btnEntrada.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(15)))), ((int)(((byte)(20)))));
            this.btnEntrada.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(34)))), ((int)(((byte)(35)))));
            this.btnEntrada.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnEntrada.ForeColor = System.Drawing.Color.Silver;
            this.btnEntrada.Location = new System.Drawing.Point(0, 0);
            this.btnEntrada.Name = "btnEntrada";
            this.btnEntrada.Padding = new System.Windows.Forms.Padding(35, 0, 0, 0);
            this.btnEntrada.Size = new System.Drawing.Size(233, 40);
            this.btnEntrada.TabIndex = 0;
            this.btnEntrada.Text = "Entrada";
            this.btnEntrada.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnEntrada.UseVisualStyleBackColor = true;
            this.btnEntrada.Click += new System.EventHandler(this.btnEntrada_Click);
            // 
            // btnHistorial
            // 
            this.btnHistorial.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnHistorial.FlatAppearance.BorderSize = 0;
            this.btnHistorial.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(15)))), ((int)(((byte)(20)))));
            this.btnHistorial.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(34)))), ((int)(((byte)(35)))));
            this.btnHistorial.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnHistorial.ForeColor = System.Drawing.Color.Silver;
            this.btnHistorial.Location = new System.Drawing.Point(0, 40);
            this.btnHistorial.Name = "btnHistorial";
            this.btnHistorial.Padding = new System.Windows.Forms.Padding(35, 0, 0, 0);
            this.btnHistorial.Size = new System.Drawing.Size(233, 40);
            this.btnHistorial.TabIndex = 1;
            this.btnHistorial.Text = "Historial";
            this.btnHistorial.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnHistorial.UseVisualStyleBackColor = true;
            this.btnHistorial.Click += new System.EventHandler(this.btnHistorial_Click);
            // 
            // btnMovimientos
            // 
            this.btnMovimientos.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnMovimientos.FlatAppearance.BorderSize = 0;
            this.btnMovimientos.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(33)))), ((int)(((byte)(38)))));
            this.btnMovimientos.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(24)))), ((int)(((byte)(29)))), ((int)(((byte)(34)))));
            this.btnMovimientos.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnMovimientos.ForeColor = System.Drawing.Color.Silver;
            this.btnMovimientos.Image = ((System.Drawing.Image)(resources.GetObject("btnMovimientos.Image")));
            this.btnMovimientos.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnMovimientos.Location = new System.Drawing.Point(0, 339);
            this.btnMovimientos.Name = "btnMovimientos";
            this.btnMovimientos.Padding = new System.Windows.Forms.Padding(5, 0, 0, 0);
            this.btnMovimientos.Size = new System.Drawing.Size(233, 45);
            this.btnMovimientos.TabIndex = 3;
            this.btnMovimientos.Text = "Movimientos";
            this.btnMovimientos.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnMovimientos.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnMovimientos.UseVisualStyleBackColor = true;
            this.btnMovimientos.Click += new System.EventHandler(this.btnMovimientos_Click);
            // 
            // panelGestion
            // 
            this.panelGestion.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(38)))), ((int)(((byte)(39)))));
            this.panelGestion.Controls.Add(this.btnFletes);
            this.panelGestion.Controls.Add(this.btnInsumos);
            this.panelGestion.Controls.Add(this.btnRecortes);
            this.panelGestion.Controls.Add(this.btnHerramientas);
            this.panelGestion.Controls.Add(this.Ubicaciones);
            this.panelGestion.Controls.Add(this.Obras);
            this.panelGestion.Controls.Add(this.btnProveedores);
            this.panelGestion.Controls.Add(this.btnCategorias);
            this.panelGestion.Controls.Add(this.btnMateriales);
            this.panelGestion.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelGestion.Location = new System.Drawing.Point(0, 131);
            this.panelGestion.Name = "panelGestion";
            this.panelGestion.Size = new System.Drawing.Size(233, 360);
            this.panelGestion.TabIndex = 2;
            // 
            // btnRecortes
            // 
            this.btnRecortes.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnRecortes.FlatAppearance.BorderSize = 0;
            this.btnRecortes.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(15)))), ((int)(((byte)(20)))));
            this.btnRecortes.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(34)))), ((int)(((byte)(35)))));
            this.btnRecortes.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRecortes.ForeColor = System.Drawing.Color.Silver;
            this.btnRecortes.Location = new System.Drawing.Point(0, 240);
            this.btnRecortes.Name = "btnRecortes";
            this.btnRecortes.Padding = new System.Windows.Forms.Padding(35, 0, 0, 0);
            this.btnRecortes.Size = new System.Drawing.Size(233, 40);
            this.btnRecortes.TabIndex = 9;
            this.btnRecortes.Text = "Recortes";
            this.btnRecortes.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnRecortes.UseVisualStyleBackColor = true;
            this.btnRecortes.Click += new System.EventHandler(this.btnRecortes_Click);
            // 
            // btnHerramientas
            // 
            this.btnHerramientas.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnHerramientas.FlatAppearance.BorderSize = 0;
            this.btnHerramientas.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(15)))), ((int)(((byte)(20)))));
            this.btnHerramientas.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(34)))), ((int)(((byte)(35)))));
            this.btnHerramientas.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnHerramientas.ForeColor = System.Drawing.Color.Silver;
            this.btnHerramientas.Location = new System.Drawing.Point(0, 200);
            this.btnHerramientas.Name = "btnHerramientas";
            this.btnHerramientas.Padding = new System.Windows.Forms.Padding(35, 0, 0, 0);
            this.btnHerramientas.Size = new System.Drawing.Size(233, 40);
            this.btnHerramientas.TabIndex = 8;
            this.btnHerramientas.Text = "Herramientas";
            this.btnHerramientas.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnHerramientas.UseVisualStyleBackColor = true;
            this.btnHerramientas.Click += new System.EventHandler(this.btnHerramientas_Click);
            // 
            // btnInsumos
            // 
            this.btnInsumos.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnInsumos.FlatAppearance.BorderSize = 0;
            this.btnInsumos.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(15)))), ((int)(((byte)(20)))));
            this.btnInsumos.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(34)))), ((int)(((byte)(35)))));
            this.btnInsumos.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnInsumos.ForeColor = System.Drawing.Color.Silver;
            this.btnInsumos.Location = new System.Drawing.Point(0, 280);
            this.btnInsumos.Name = "btnInsumos";
            this.btnInsumos.Padding = new System.Windows.Forms.Padding(35, 0, 0, 0);
            this.btnInsumos.Size = new System.Drawing.Size(233, 40);
            this.btnInsumos.TabIndex = 9;
            this.btnInsumos.Text = "Insumos";
            this.btnInsumos.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnInsumos.UseVisualStyleBackColor = true;
            this.btnInsumos.Click += new System.EventHandler(this.btnInsumos_Click);
            // 
            // btnFletes
            // 
            this.btnFletes.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnFletes.FlatAppearance.BorderSize = 0;
            this.btnFletes.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(15)))), ((int)(((byte)(20)))));
            this.btnFletes.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(34)))), ((int)(((byte)(35)))));
            this.btnFletes.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnFletes.ForeColor = System.Drawing.Color.Silver;
            this.btnFletes.Location = new System.Drawing.Point(0, 320);
            this.btnFletes.Name = "btnFletes";
            this.btnFletes.Padding = new System.Windows.Forms.Padding(35, 0, 0, 0);
            this.btnFletes.Size = new System.Drawing.Size(233, 40);
            this.btnFletes.TabIndex = 10;
            this.btnFletes.Text = "Fletes";
            this.btnFletes.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnFletes.UseVisualStyleBackColor = true;
            this.btnFletes.Click += new System.EventHandler(this.btnFletes_Click);
            // 
            // Ubicaciones
            // 
            this.Ubicaciones.Dock = System.Windows.Forms.DockStyle.Top;
            this.Ubicaciones.FlatAppearance.BorderSize = 0;
            this.Ubicaciones.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(15)))), ((int)(((byte)(20)))));
            this.Ubicaciones.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(34)))), ((int)(((byte)(35)))));
            this.Ubicaciones.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.Ubicaciones.ForeColor = System.Drawing.Color.Silver;
            this.Ubicaciones.Location = new System.Drawing.Point(0, 160);
            this.Ubicaciones.Name = "Ubicaciones";
            this.Ubicaciones.Padding = new System.Windows.Forms.Padding(35, 0, 0, 0);
            this.Ubicaciones.Size = new System.Drawing.Size(233, 40);
            this.Ubicaciones.TabIndex = 7;
            this.Ubicaciones.Text = "Ubicaciones";
            this.Ubicaciones.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.Ubicaciones.UseVisualStyleBackColor = true;
            this.Ubicaciones.Click += new System.EventHandler(this.Ubicaciones_Click);
            // 
            // Obras
            // 
            this.Obras.Dock = System.Windows.Forms.DockStyle.Top;
            this.Obras.FlatAppearance.BorderSize = 0;
            this.Obras.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(15)))), ((int)(((byte)(20)))));
            this.Obras.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(34)))), ((int)(((byte)(35)))));
            this.Obras.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.Obras.ForeColor = System.Drawing.Color.Silver;
            this.Obras.Location = new System.Drawing.Point(0, 120);
            this.Obras.Name = "Obras";
            this.Obras.Padding = new System.Windows.Forms.Padding(35, 0, 0, 0);
            this.Obras.Size = new System.Drawing.Size(233, 40);
            this.Obras.TabIndex = 6;
            this.Obras.Text = "Obras";
            this.Obras.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.Obras.UseVisualStyleBackColor = true;
            this.Obras.Click += new System.EventHandler(this.Obras_Click);
            // 
            // btnProveedores
            // 
            this.btnProveedores.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnProveedores.FlatAppearance.BorderSize = 0;
            this.btnProveedores.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(15)))), ((int)(((byte)(20)))));
            this.btnProveedores.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(34)))), ((int)(((byte)(35)))));
            this.btnProveedores.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnProveedores.ForeColor = System.Drawing.Color.Silver;
            this.btnProveedores.Location = new System.Drawing.Point(0, 80);
            this.btnProveedores.Name = "btnProveedores";
            this.btnProveedores.Padding = new System.Windows.Forms.Padding(35, 0, 0, 0);
            this.btnProveedores.Size = new System.Drawing.Size(233, 40);
            this.btnProveedores.TabIndex = 3;
            this.btnProveedores.Text = "Proveedores";
            this.btnProveedores.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnProveedores.UseVisualStyleBackColor = true;
            this.btnProveedores.Click += new System.EventHandler(this.button5_Click);
            // 
            // btnCategorias
            // 
            this.btnCategorias.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnCategorias.FlatAppearance.BorderSize = 0;
            this.btnCategorias.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(15)))), ((int)(((byte)(20)))));
            this.btnCategorias.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(34)))), ((int)(((byte)(35)))));
            this.btnCategorias.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCategorias.ForeColor = System.Drawing.Color.Silver;
            this.btnCategorias.Location = new System.Drawing.Point(0, 40);
            this.btnCategorias.Name = "btnCategorias";
            this.btnCategorias.Padding = new System.Windows.Forms.Padding(35, 0, 0, 0);
            this.btnCategorias.Size = new System.Drawing.Size(233, 40);
            this.btnCategorias.TabIndex = 1;
            this.btnCategorias.Text = "Categorías";
            this.btnCategorias.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnCategorias.UseVisualStyleBackColor = true;
            this.btnCategorias.Click += new System.EventHandler(this.button3_Click);
            // 
            // btnMateriales
            // 
            this.btnMateriales.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnMateriales.FlatAppearance.BorderSize = 0;
            this.btnMateriales.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(15)))), ((int)(((byte)(20)))));
            this.btnMateriales.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(34)))), ((int)(((byte)(35)))));
            this.btnMateriales.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnMateriales.ForeColor = System.Drawing.Color.Silver;
            this.btnMateriales.Location = new System.Drawing.Point(0, 0);
            this.btnMateriales.Name = "btnMateriales";
            this.btnMateriales.Padding = new System.Windows.Forms.Padding(35, 0, 0, 0);
            this.btnMateriales.Size = new System.Drawing.Size(233, 40);
            this.btnMateriales.TabIndex = 0;
            this.btnMateriales.Text = "Materiales";
            this.btnMateriales.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnMateriales.UseVisualStyleBackColor = true;
            this.btnMateriales.Click += new System.EventHandler(this.button2_Click);
            // 
            // btnGestion
            // 
            this.btnGestion.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnGestion.FlatAppearance.BorderSize = 0;
            this.btnGestion.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(33)))), ((int)(((byte)(38)))));
            this.btnGestion.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(24)))), ((int)(((byte)(29)))), ((int)(((byte)(34)))));
            this.btnGestion.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnGestion.ForeColor = System.Drawing.Color.Silver;
            this.btnGestion.Image = ((System.Drawing.Image)(resources.GetObject("btnGestion.Image")));
            this.btnGestion.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnGestion.Location = new System.Drawing.Point(0, 86);
            this.btnGestion.Name = "btnGestion";
            this.btnGestion.Padding = new System.Windows.Forms.Padding(5, 0, 0, 0);
            this.btnGestion.Size = new System.Drawing.Size(233, 45);
            this.btnGestion.TabIndex = 1;
            this.btnGestion.Text = "Gestión";
            this.btnGestion.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnGestion.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnGestion.UseVisualStyleBackColor = true;
            this.btnGestion.Click += new System.EventHandler(this.btnMedia_Click);
            // 
            // panelLogo
            // 
            this.panelLogo.Controls.Add(this.pictureBox1);
            this.panelLogo.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelLogo.Location = new System.Drawing.Point(0, 0);
            this.panelLogo.Name = "panelLogo";
            this.panelLogo.Size = new System.Drawing.Size(233, 86);
            this.panelLogo.TabIndex = 0;
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.Location = new System.Drawing.Point(58, -12);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(134, 154);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            // 
            // panelChildForm
            // 
            this.panelChildForm.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(38)))), ((int)(((byte)(35)))));
            this.panelChildForm.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("panelChildForm.BackgroundImage")));
            this.panelChildForm.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.panelChildForm.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelChildForm.Location = new System.Drawing.Point(250, 40);
            this.panelChildForm.Name = "panelChildForm";
            this.panelChildForm.Size = new System.Drawing.Size(700, 560);
            this.panelChildForm.TabIndex = 2;
            this.panelChildForm.Paint += new System.Windows.Forms.PaintEventHandler(this.panelChildForm_Paint);
            // 
            // panelMaximizar
            // 
            this.panelMaximizar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(11)))), ((int)(((byte)(7)))), ((int)(((byte)(17)))));
            this.panelMaximizar.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelMaximizar.Location = new System.Drawing.Point(0, 0);
            this.panelMaximizar.Name = "panelMaximizar";
            this.panelMaximizar.Size = new System.Drawing.Size(950, 40);
            this.panelMaximizar.TabIndex = 3;
            this.panelMaximizar.Paint += new System.Windows.Forms.PaintEventHandler(this.panelMaximizar_Paint);
            this.panelMaximizar.MouseDown += new System.Windows.Forms.MouseEventHandler(this.panelMaximizar_MouseDown);
            // 
            // guna2VScrollBar2
            // 
            this.guna2VScrollBar2.BindingContainer = this.panelSideMenu;
            this.guna2VScrollBar2.FillColor = System.Drawing.Color.Transparent;
            this.guna2VScrollBar2.InUpdate = false;
            this.guna2VScrollBar2.LargeChange = 560;
            this.guna2VScrollBar2.Location = new System.Drawing.Point(232, 40);
            this.guna2VScrollBar2.Maximum = 688;
            this.guna2VScrollBar2.Name = "guna2VScrollBar2";
            this.guna2VScrollBar2.ScrollbarSize = 18;
            this.guna2VScrollBar2.Size = new System.Drawing.Size(18, 560);
            this.guna2VScrollBar2.SmallChange = 5;
            this.guna2VScrollBar2.TabIndex = 1;
            this.guna2VScrollBar2.ThumbColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.guna2VScrollBar2.Value = 65;
            // 
            // VistaGeneral
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.ClientSize = new System.Drawing.Size(950, 600);
            this.Controls.Add(this.guna2VScrollBar2);
            this.Controls.Add(this.panelChildForm);
            this.Controls.Add(this.panelSideMenu);
            this.Controls.Add(this.panelMaximizar);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MinimumSize = new System.Drawing.Size(950, 600);
            this.Name = "VistaGeneral";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.panelSideMenu.ResumeLayout(false);
            this.panelReportes.ResumeLayout(false);
            this.panelMovimientos.ResumeLayout(false);
            this.panelGestion.ResumeLayout(false);
            this.panelLogo.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Panel panelSideMenu;
        private System.Windows.Forms.Panel panelGestion;
        private System.Windows.Forms.Button btnProveedores;
        private System.Windows.Forms.Button btnCategorias;
        private System.Windows.Forms.Button btnMateriales;
        private System.Windows.Forms.Button btnGestion;
        private System.Windows.Forms.Button btnInventario;
        private System.Windows.Forms.Panel panelReportes;
        private System.Windows.Forms.Button button10;
        private System.Windows.Forms.Button button12;
        private System.Windows.Forms.Button button13;
        private System.Windows.Forms.Button btnReportes;
        private System.Windows.Forms.Button btnMovimientos;
        private System.Windows.Forms.Panel panelMovimientos;
        private System.Windows.Forms.Button btnEntrada;
        private System.Windows.Forms.Button btnHistorial;
        private System.Windows.Forms.Panel panelLogo;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Panel panelChildForm;
        private System.Windows.Forms.Button Obras;
        private System.Windows.Forms.Button btnSalir;
        private Guna.UI2.WinForms.Guna2Panel panelMaximizar;
        private Guna.UI2.WinForms.Guna2VScrollBar guna2VScrollBar2;
        private System.Windows.Forms.Button Ubicaciones;
        private System.Windows.Forms.Button btnHerramientas;
        private System.Windows.Forms.Button btnRecortes;
        private System.Windows.Forms.Button btnInsumos;
        private System.Windows.Forms.Button btnFletes;
        private System.Windows.Forms.Panel panelInventario;
        private System.Windows.Forms.Button btnInventarioGeneral;
        private System.Windows.Forms.Button btnInventarioConteo;
    }
}


