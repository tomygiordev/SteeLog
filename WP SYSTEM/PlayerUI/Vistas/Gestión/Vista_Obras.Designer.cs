namespace WP_Inventario.Vistas
{
    partial class Vista_Obras
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle6 = new System.Windows.Forms.DataGridViewCellStyle();
            this.guna2Panel1 = new Guna.UI2.WinForms.Guna2Panel();
            this.guna2VScrollBar1 = new Guna.UI2.WinForms.Guna2VScrollBar();
            this.dgvObras = new Guna.UI2.WinForms.Guna2DataGridView();
            this.panelAntiParpadeo4 = new WP_Inventario.PanelAntiParpadeo();
            this.label3 = new System.Windows.Forms.Label();
            this.panelAntiParpadeo2 = new WP_Inventario.PanelAntiParpadeo();
            this.eliminarObraBoton = new Guna.UI2.WinForms.Guna2Button();
            this.guna2Button1 = new Guna.UI2.WinForms.Guna2Button();
            this.txtObra = new Guna.UI2.WinForms.Guna2TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.chkActiva = new Guna.UI2.WinForms.Guna2CustomCheckBox();
            this.lblActiva = new System.Windows.Forms.Label();
            this.panelAntiParpadeo1 = new WP_Inventario.PanelAntiParpadeo();
            this.label1 = new System.Windows.Forms.Label();
            this.guna2Panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvObras)).BeginInit();
            this.panelAntiParpadeo4.SuspendLayout();
            this.panelAntiParpadeo2.SuspendLayout();
            this.panelAntiParpadeo1.SuspendLayout();
            this.SuspendLayout();
            // 
            // guna2Panel1
            // 
            this.guna2Panel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
                | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this.guna2Panel1.BorderColor = System.Drawing.Color.FromArgb(38, 52, 44);
            this.guna2Panel1.BorderRadius = 0;
            this.guna2Panel1.BorderThickness = 0;
            this.guna2Panel1.Controls.Add(this.guna2VScrollBar1);
            this.guna2Panel1.Controls.Add(this.dgvObras);
            this.guna2Panel1.Location = new System.Drawing.Point(0, 130);
            this.guna2Panel1.Name = "guna2Panel1";
            this.guna2Panel1.Padding = new System.Windows.Forms.Padding(0);
            this.guna2Panel1.Size = new System.Drawing.Size(800, 320);
            this.guna2Panel1.TabIndex = 35;
            // 
            // guna2VScrollBar1
            // 
            this.guna2VScrollBar1.BindingContainer = this.dgvObras;
            this.guna2VScrollBar1.Dock = System.Windows.Forms.DockStyle.Right;
            this.guna2VScrollBar1.FillColor = System.Drawing.Color.FromArgb(35, 50, 40);
            this.guna2VScrollBar1.InUpdate = false;
            this.guna2VScrollBar1.LargeChange = 10;
            this.guna2VScrollBar1.Location = new System.Drawing.Point(784, 0);
            this.guna2VScrollBar1.Minimum = 1;
            this.guna2VScrollBar1.Name = "guna2VScrollBar1";
            this.guna2VScrollBar1.ScrollbarSize = 16;
            this.guna2VScrollBar1.Size = new System.Drawing.Size(16, 320);
            this.guna2VScrollBar1.TabIndex = 20;
            this.guna2VScrollBar1.ThumbColor = System.Drawing.Color.FromArgb(55, 85, 65);
            this.guna2VScrollBar1.Value = 1;
            // 
            // dgvObras
            // 
            this.dgvObras.AllowUserToAddRows = false;
            this.dgvObras.AllowUserToDeleteRows = false;
            this.dgvObras.AllowUserToResizeColumns = false;
            this.dgvObras.AllowUserToResizeRows = false;
            dataGridViewCellStyle4.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(38)))), ((int)(((byte)(35)))));
            this.dgvObras.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle4;
            this.dgvObras.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(38)))), ((int)(((byte)(35)))));
            dataGridViewCellStyle5.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle5.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(63)))), ((int)(((byte)(43)))));
            dataGridViewCellStyle5.Font = new System.Drawing.Font("Bahnschrift Light", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle5.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle5.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(63)))), ((int)(((byte)(43)))));
            dataGridViewCellStyle5.SelectionForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle5.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvObras.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle5;
            this.dgvObras.ColumnHeadersHeight = 35;
            this.dgvObras.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dataGridViewCellStyle6.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle6.BackColor = System.Drawing.Color.FromArgb(29, 38, 35);
            dataGridViewCellStyle6.Font = new System.Drawing.Font("Bahnschrift Light", 11F);
            dataGridViewCellStyle6.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle6.Padding = new System.Windows.Forms.Padding(14, 0, 14, 0);
            dataGridViewCellStyle6.SelectionBackColor = System.Drawing.Color.FromArgb(47, 84, 57);
            dataGridViewCellStyle6.SelectionForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle6.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvObras.DefaultCellStyle = dataGridViewCellStyle6;
            this.dgvObras.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvObras.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(38)))), ((int)(((byte)(35)))));
            this.dgvObras.Location = new System.Drawing.Point(7, 7);
            this.dgvObras.Name = "dgvObras";
            this.dgvObras.ReadOnly = true;
            this.dgvObras.RowHeadersVisible = false;
            this.dgvObras.RowTemplate.DefaultCellStyle.Font = new System.Drawing.Font("Bahnschrift Light", 11F);
            this.dgvObras.RowTemplate.Height = 36;
            this.dgvObras.Size = new System.Drawing.Size(784, 320);
            this.dgvObras.TabIndex = 19;
            this.dgvObras.Theme = Guna.UI2.WinForms.Enums.DataGridViewPresetThemes.Dark;
            this.dgvObras.ThemeStyle.AlternatingRowsStyle.BackColor = System.Drawing.Color.FromArgb(29, 38, 35);
            this.dgvObras.ThemeStyle.AlternatingRowsStyle.Font = null;
            this.dgvObras.ThemeStyle.AlternatingRowsStyle.ForeColor = System.Drawing.Color.Empty;
            this.dgvObras.ThemeStyle.AlternatingRowsStyle.SelectionBackColor = System.Drawing.Color.Empty;
            this.dgvObras.ThemeStyle.AlternatingRowsStyle.SelectionForeColor = System.Drawing.Color.Empty;
            this.dgvObras.ThemeStyle.BackColor = System.Drawing.Color.FromArgb(29, 38, 35);
            this.dgvObras.ThemeStyle.GridColor = System.Drawing.Color.FromArgb(38, 50, 43);
            this.dgvObras.ThemeStyle.HeaderStyle.BackColor = System.Drawing.Color.FromArgb(32, 55, 40);
            this.dgvObras.ThemeStyle.HeaderStyle.BorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            this.dgvObras.ThemeStyle.HeaderStyle.Font = new System.Drawing.Font("Bahnschrift Light", 10.5F);
            this.dgvObras.ThemeStyle.HeaderStyle.ForeColor = System.Drawing.Color.FromArgb(195, 215, 202);
            this.dgvObras.ThemeStyle.HeaderStyle.HeaightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.dgvObras.ThemeStyle.HeaderStyle.Height = 35;
            this.dgvObras.ThemeStyle.ReadOnly = true;
            this.dgvObras.ThemeStyle.RowsStyle.BackColor = System.Drawing.Color.FromArgb(29, 38, 35);
            this.dgvObras.ThemeStyle.RowsStyle.BorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            this.dgvObras.ThemeStyle.RowsStyle.Font = new System.Drawing.Font("Bahnschrift Light", 11F);
            this.dgvObras.ThemeStyle.RowsStyle.ForeColor = System.Drawing.Color.White;
            this.dgvObras.ThemeStyle.RowsStyle.Height = 36;
            this.dgvObras.ThemeStyle.RowsStyle.SelectionBackColor = System.Drawing.Color.FromArgb(47, 84, 57);
            this.dgvObras.ThemeStyle.RowsStyle.SelectionForeColor = System.Drawing.Color.White;
            this.dgvObras.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvObras_CellClick);
            // 
            // panelAntiParpadeo4
            // 
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
            this.panelAntiParpadeo4.TabIndex = 36;
            this.label3.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Bahnschrift SemiBold", 10F, System.Drawing.FontStyle.Bold);
            this.label3.ForeColor = System.Drawing.Color.FromArgb(145, 175, 158);
            this.label3.Location = new System.Drawing.Point(17, 9);
            this.label3.Name = "label3";
            this.label3.TabIndex = 3;
            this.label3.Text = "LISTADO DE OBRAS";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // panelAntiParpadeo2
            // 
            this.panelAntiParpadeo2.ColumnCount = 6;
            this.panelAntiParpadeo2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 90F));
            this.panelAntiParpadeo2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 60F));
            this.panelAntiParpadeo2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 96F));
            this.panelAntiParpadeo2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.panelAntiParpadeo2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.panelAntiParpadeo2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.panelAntiParpadeo2.Controls.Add(this.eliminarObraBoton, 5, 0);
            this.panelAntiParpadeo2.Controls.Add(this.guna2Button1, 4, 0);
            this.panelAntiParpadeo2.Controls.Add(this.txtObra, 1, 0);
            this.panelAntiParpadeo2.Controls.Add(this.label2, 0, 0);
            this.panelAntiParpadeo2.Controls.Add(this.lblActiva, 2, 0);
            this.panelAntiParpadeo2.Controls.Add(this.chkActiva, 3, 0);
            this.panelAntiParpadeo2.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelAntiParpadeo2.GrowStyle = System.Windows.Forms.TableLayoutPanelGrowStyle.AddColumns;
            this.panelAntiParpadeo2.Location = new System.Drawing.Point(0, 42);
            this.panelAntiParpadeo2.Name = "panelAntiParpadeo2";
            this.panelAntiParpadeo2.Padding = new System.Windows.Forms.Padding(0, 8, 4, 8);
            this.panelAntiParpadeo2.RowCount = 1;
            this.panelAntiParpadeo2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.panelAntiParpadeo2.Size = new System.Drawing.Size(800, 52);
            this.panelAntiParpadeo2.TabIndex = 33;
            // 
            // eliminarObraBoton
            // 
            this.eliminarObraBoton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.eliminarObraBoton.BorderRadius = 0;
            this.eliminarObraBoton.DisabledState.BorderColor = System.Drawing.Color.FromArgb(50, 50, 50);
            this.eliminarObraBoton.DisabledState.CustomBorderColor = System.Drawing.Color.FromArgb(50, 50, 50);
            this.eliminarObraBoton.DisabledState.FillColor = System.Drawing.Color.FromArgb(40, 40, 40);
            this.eliminarObraBoton.DisabledState.ForeColor = System.Drawing.Color.FromArgb(80, 80, 80);
            this.eliminarObraBoton.FillColor = System.Drawing.Color.FromArgb(158, 45, 40);
            this.eliminarObraBoton.Font = new System.Drawing.Font("Bahnschrift Light", 11F);
            this.eliminarObraBoton.ForeColor = System.Drawing.Color.White;
            this.eliminarObraBoton.HoverState.FillColor = System.Drawing.Color.FromArgb(180, 55, 48);
            this.eliminarObraBoton.Location = new System.Drawing.Point(392, 9);
            this.eliminarObraBoton.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.eliminarObraBoton.Name = "eliminarObraBoton";
            this.eliminarObraBoton.Size = new System.Drawing.Size(130, 34);
            this.eliminarObraBoton.TabIndex = 4;
            this.eliminarObraBoton.Text = "Eliminar";
            this.eliminarObraBoton.Click += new System.EventHandler(this.eliminarObraBoton_Click_1);
            //
            // guna2Button1
            //
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
            this.guna2Button1.TabIndex = 0;
            this.guna2Button1.Text = "Guardar";
            this.guna2Button1.Click += new System.EventHandler(this.guna2Button1_Click);
            // 
            // txtObra
            // 
            this.txtObra.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtObra.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.txtObra.BorderColor = System.Drawing.Color.FromArgb(55, 75, 62);
            this.txtObra.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txtObra.DefaultText = "";
            this.txtObra.DisabledState.BorderColor = System.Drawing.Color.FromArgb(60, 60, 60);
            this.txtObra.DisabledState.FillColor = System.Drawing.Color.FromArgb(40, 55, 44);
            this.txtObra.DisabledState.ForeColor = System.Drawing.Color.FromArgb(100, 100, 100);
            this.txtObra.DisabledState.PlaceholderForeColor = System.Drawing.Color.FromArgb(100, 100, 100);
            this.txtObra.FillColor = System.Drawing.Color.FromArgb(35, 50, 40);
            this.txtObra.FocusedState.BorderColor = System.Drawing.Color.FromArgb(39, 174, 96);
            this.txtObra.Font = new System.Drawing.Font("Segoe UI", 10.5F);
            this.txtObra.ForeColor = System.Drawing.Color.FromArgb(225, 225, 225);
            this.txtObra.HoverState.BorderColor = System.Drawing.Color.FromArgb(60, 120, 80);
            this.txtObra.Location = new System.Drawing.Point(93, 10);
            this.txtObra.Margin = new System.Windows.Forms.Padding(3, 2, 8, 2);
            this.txtObra.Name = "txtObra";
            this.txtObra.PlaceholderText = "Ej: Edificio San Juan 450";
            this.txtObra.SelectedText = "";
            this.txtObra.Size = new System.Drawing.Size(300, 30);
            this.txtObra.TabIndex = 1;
            this.txtObra.TextChanged += new System.EventHandler(this.txtObra_TextChanged);
            this.txtObra.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtObra_KeyDown);
            // 
            // chkActiva
            // 
            this.chkActiva.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.chkActiva.Checked = true;
            this.chkActiva.CheckedState.BorderColor = System.Drawing.Color.FromArgb(39, 174, 96);
            this.chkActiva.CheckedState.BorderRadius = 0;
            this.chkActiva.CheckedState.BorderThickness = 0;
            this.chkActiva.CheckedState.FillColor = System.Drawing.Color.FromArgb(39, 174, 96);
            this.chkActiva.Location = new System.Drawing.Point(222, 14);
            this.chkActiva.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
            this.chkActiva.Name = "chkActiva";
            this.chkActiva.Size = new System.Drawing.Size(22, 22);
            this.chkActiva.TabIndex = 2;
            this.chkActiva.UncheckedState.BorderColor = System.Drawing.Color.FromArgb(75, 100, 84);
            this.chkActiva.UncheckedState.BorderRadius = 0;
            this.chkActiva.UncheckedState.BorderThickness = 2;
            this.chkActiva.UncheckedState.FillColor = System.Drawing.Color.FromArgb(35, 50, 40);

            // lblActiva
            this.lblActiva.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblActiva.AutoSize = false;
            this.lblActiva.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblActiva.Font = new System.Drawing.Font("Bahnschrift Light", 10.5F);
            this.lblActiva.ForeColor = System.Drawing.Color.FromArgb(155, 185, 165);
            this.lblActiva.Location = new System.Drawing.Point(448, 17);
            this.lblActiva.Name = "lblActiva";
            this.lblActiva.Padding = new System.Windows.Forms.Padding(8, 0, 4, 0);
            this.lblActiva.TabIndex = 10;
            this.lblActiva.Text = "Activa:";
            this.lblActiva.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // label2
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right));
            this.label2.AutoSize = false;
            this.label2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label2.Font = new System.Drawing.Font("Bahnschrift Light", 10.5F);
            this.label2.ForeColor = System.Drawing.Color.FromArgb(155, 185, 165);
            this.label2.Location = new System.Drawing.Point(43, 17);
            this.label2.Name = "label2";
            this.label2.Padding = new System.Windows.Forms.Padding(14, 0, 6, 0);
            this.label2.TabIndex = 0;
            this.label2.Text = "Nombre:";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

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
            this.panelAntiParpadeo1.TabIndex = 32;
            this.panelAntiParpadeo1.Paint += new System.Windows.Forms.PaintEventHandler(this.panelAntiParpadeo1_Paint);

            // label1
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right));
            this.label1.AutoSize = false;
            this.label1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label1.Font = new System.Drawing.Font("Bahnschrift SemiBold", 13F, System.Drawing.FontStyle.Bold);
            this.label1.ForeColor = System.Drawing.Color.FromArgb(210, 235, 218);
            this.label1.Location = new System.Drawing.Point(14, 0);
            this.label1.Name = "label1";
            this.label1.TabIndex = 3;
            this.label1.Text = "OBRAS";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // Vista_Obras
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(38)))), ((int)(((byte)(35)))));
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.guna2Panel1);
            this.Controls.Add(this.panelAntiParpadeo4);
            this.Controls.Add(this.panelAntiParpadeo2);
            this.Controls.Add(this.panelAntiParpadeo1);
            this.Name = "Vista_Obras";
            this.Text = "Gestión de Obras";
            this.guna2Panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvObras)).EndInit();
            this.panelAntiParpadeo4.ResumeLayout(false);
            this.panelAntiParpadeo4.PerformLayout();
            this.panelAntiParpadeo2.ResumeLayout(false);
            this.panelAntiParpadeo2.PerformLayout();
            this.panelAntiParpadeo1.ResumeLayout(false);
            this.panelAntiParpadeo1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private Guna.UI2.WinForms.Guna2Panel guna2Panel1;
        private Guna.UI2.WinForms.Guna2VScrollBar guna2VScrollBar1;
        private Guna.UI2.WinForms.Guna2DataGridView dgvObras;
        private PanelAntiParpadeo panelAntiParpadeo4;
        private System.Windows.Forms.Label label3;
        private Guna.UI2.WinForms.Guna2Button guna2Button1;
        private PanelAntiParpadeo panelAntiParpadeo2;
        private Guna.UI2.WinForms.Guna2TextBox txtObra;
        private System.Windows.Forms.Label label2;
        private PanelAntiParpadeo panelAntiParpadeo1;
        private System.Windows.Forms.Label label1;
        private Guna.UI2.WinForms.Guna2Button eliminarObraBoton;
        private Guna.UI2.WinForms.Guna2CustomCheckBox chkActiva;
        private System.Windows.Forms.Label lblActiva;
    }
}
