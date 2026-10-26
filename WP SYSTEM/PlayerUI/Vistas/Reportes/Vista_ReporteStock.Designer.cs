namespace WP_Inventario.Vistas
{
    partial class Vista_ReporteStock
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
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend1 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Series series1 = new System.Windows.Forms.DataVisualization.Charting.Series();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            this.panelDashboard = new System.Windows.Forms.TableLayoutPanel();
            this.panelTotal = new Guna.UI2.WinForms.Guna2Panel();
            this.lblTotalVal = new System.Windows.Forms.Label();
            this.lblTotalTit = new System.Windows.Forms.Label();
            this.panelBajo = new Guna.UI2.WinForms.Guna2Panel();
            this.lblBajoVal = new System.Windows.Forms.Label();
            this.lblBajoTit = new System.Windows.Forms.Label();
            this.chartCategorias = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.materiales = new WP_Inventario.PanelAntiParpadeo();
            this.btnExportarExcel = new Guna.UI2.WinForms.Guna2Button();
            this.btnExportarPDF = new Guna.UI2.WinForms.Guna2Button();
            this.filtrarMaterial = new Guna.UI2.WinForms.Guna2Button();
            this.toggleStockBajo = new Guna.UI2.WinForms.Guna2ToggleSwitch();
            this.lblSoloStockBajo = new System.Windows.Forms.Label();
            this.txtFiltroMaterial = new Guna.UI2.WinForms.Guna2TextBox();
            this.dgvMateriales = new Guna.UI2.WinForms.Guna2DataGridView();
            this.label9 = new System.Windows.Forms.Label();
            this.panelAntiParpadeo6 = new WP_Inventario.PanelAntiParpadeo();
            this.label8 = new System.Windows.Forms.Label();
            this.panelAntiParpadeo1 = new WP_Inventario.PanelAntiParpadeo();
            this.label1 = new System.Windows.Forms.Label();
            this.panelDashboard.SuspendLayout();
            this.panelTotal.SuspendLayout();
            this.panelBajo.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.chartCategorias)).BeginInit();
            this.materiales.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvMateriales)).BeginInit();
            this.panelAntiParpadeo6.SuspendLayout();
            this.panelAntiParpadeo1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelDashboard
            // 
            this.panelDashboard.ColumnCount = 3;
            this.panelDashboard.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.panelDashboard.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.panelDashboard.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.panelDashboard.Controls.Add(this.panelTotal, 0, 0);
            this.panelDashboard.Controls.Add(this.panelBajo, 1, 0);
            this.panelDashboard.Controls.Add(this.chartCategorias, 2, 0);
            this.panelDashboard.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelDashboard.Location = new System.Drawing.Point(0, 83);
            this.panelDashboard.Name = "panelDashboard";
            this.panelDashboard.RowCount = 1;
            this.panelDashboard.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.panelDashboard.Size = new System.Drawing.Size(796, 109);
            this.panelDashboard.TabIndex = 38;
            // 
            // panelTotal
            // 
            this.panelTotal.BorderColor = System.Drawing.Color.Gray;
            this.panelTotal.BorderRadius = 10;
            this.panelTotal.BorderThickness = 1;
            this.panelTotal.Controls.Add(this.lblTotalVal);
            this.panelTotal.Controls.Add(this.lblTotalTit);
            this.panelTotal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelTotal.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(50)))), ((int)(((byte)(40)))));
            this.panelTotal.Location = new System.Drawing.Point(10, 10);
            this.panelTotal.Margin = new System.Windows.Forms.Padding(10);
            this.panelTotal.Name = "panelTotal";
            this.panelTotal.Size = new System.Drawing.Size(245, 89);
            this.panelTotal.TabIndex = 0;
            // 
            // lblTotalVal
            // 
            this.lblTotalVal.AutoSize = true;
            this.lblTotalVal.BackColor = System.Drawing.Color.Transparent;
            this.lblTotalVal.Font = new System.Drawing.Font("Bahnschrift SemiBold", 25.75F, System.Drawing.FontStyle.Bold);
            this.lblTotalVal.ForeColor = System.Drawing.Color.White;
            this.lblTotalVal.Location = new System.Drawing.Point(12, 40);
            this.lblTotalVal.Name = "lblTotalVal";
            this.lblTotalVal.Size = new System.Drawing.Size(37, 42);
            this.lblTotalVal.TabIndex = 1;
            this.lblTotalVal.Text = "0";
            // 
            // lblTotalTit
            // 
            this.lblTotalTit.AutoSize = true;
            this.lblTotalTit.BackColor = System.Drawing.Color.Transparent;
            this.lblTotalTit.Font = new System.Drawing.Font("Bahnschrift Light", 13.75F);
            this.lblTotalTit.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblTotalTit.Location = new System.Drawing.Point(15, 15);
            this.lblTotalTit.Name = "lblTotalTit";
            this.lblTotalTit.Size = new System.Drawing.Size(145, 23);
            this.lblTotalTit.TabIndex = 0;
            this.lblTotalTit.Text = "Total Materiales";
            // 
            // panelBajo
            // 
            this.panelBajo.BorderColor = System.Drawing.Color.Gray;
            this.panelBajo.BorderRadius = 10;
            this.panelBajo.BorderThickness = 1;
            this.panelBajo.Controls.Add(this.lblBajoVal);
            this.panelBajo.Controls.Add(this.lblBajoTit);
            this.panelBajo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelBajo.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(20)))), ((int)(((byte)(20)))));
            this.panelBajo.Location = new System.Drawing.Point(275, 10);
            this.panelBajo.Margin = new System.Windows.Forms.Padding(10);
            this.panelBajo.Name = "panelBajo";
            this.panelBajo.Size = new System.Drawing.Size(245, 89);
            this.panelBajo.TabIndex = 1;
            // 
            // lblBajoVal
            // 
            this.lblBajoVal.AutoSize = true;
            this.lblBajoVal.BackColor = System.Drawing.Color.Transparent;
            this.lblBajoVal.Font = new System.Drawing.Font("Bahnschrift SemiBold", 25.75F, System.Drawing.FontStyle.Bold);
            this.lblBajoVal.ForeColor = System.Drawing.Color.White;
            this.lblBajoVal.Location = new System.Drawing.Point(12, 40);
            this.lblBajoVal.Name = "lblBajoVal";
            this.lblBajoVal.Size = new System.Drawing.Size(37, 42);
            this.lblBajoVal.TabIndex = 1;
            this.lblBajoVal.Text = "0";
            // 
            // lblBajoTit
            // 
            this.lblBajoTit.AutoSize = true;
            this.lblBajoTit.BackColor = System.Drawing.Color.Transparent;
            this.lblBajoTit.Font = new System.Drawing.Font("Bahnschrift Light", 13.75F);
            this.lblBajoTit.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(150)))), ((int)(((byte)(150)))));
            this.lblBajoTit.Location = new System.Drawing.Point(15, 15);
            this.lblBajoTit.Name = "lblBajoTit";
            this.lblBajoTit.Size = new System.Drawing.Size(126, 23);
            this.lblBajoTit.TabIndex = 0;
            this.lblBajoTit.Text = "En Stock Bajo";
            // 
            // chartCategorias
            // 
            this.chartCategorias.BackColor = System.Drawing.Color.Transparent;
            chartArea1.BackColor = System.Drawing.Color.Transparent;
            chartArea1.Name = "ChartArea1";
            this.chartCategorias.ChartAreas.Add(chartArea1);
            this.chartCategorias.Dock = System.Windows.Forms.DockStyle.Fill;
            legend1.BackColor = System.Drawing.Color.Transparent;
            legend1.ForeColor = System.Drawing.Color.White;
            legend1.Name = "Legend1";
            this.chartCategorias.Legends.Add(legend1);
            this.chartCategorias.Location = new System.Drawing.Point(533, 3);
            this.chartCategorias.Name = "chartCategorias";
            this.chartCategorias.Palette = System.Windows.Forms.DataVisualization.Charting.ChartColorPalette.SeaGreen;
            series1.ChartArea = "ChartArea1";
            series1.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Pie;
            series1.Legend = "Legend1";
            series1.Name = "Series1";
            this.chartCategorias.Series.Add(series1);
            this.chartCategorias.Size = new System.Drawing.Size(260, 103);
            this.chartCategorias.TabIndex = 3;
            // 
            // materiales
            // 
            this.materiales.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.materiales.AutoSize = true;
            this.materiales.CellBorderStyle = System.Windows.Forms.TableLayoutPanelCellBorderStyle.Single;
            this.materiales.ColumnCount = 7;
            this.materiales.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.materiales.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.materiales.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.materiales.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.materiales.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.materiales.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.materiales.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.materiales.Controls.Add(this.btnExportarExcel, 4, 0);
            this.materiales.Controls.Add(this.btnExportarPDF, 3, 0);
            this.materiales.Controls.Add(this.filtrarMaterial, 2, 0);
            this.materiales.Controls.Add(this.toggleStockBajo, 6, 0);
            this.materiales.Controls.Add(this.lblSoloStockBajo, 5, 0);
            this.materiales.Controls.Add(this.txtFiltroMaterial, 1, 0);
            this.materiales.Controls.Add(this.dgvMateriales, 0, 1);
            this.materiales.Controls.Add(this.label9, 0, 0);
            this.materiales.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(63)))), ((int)(((byte)(43)))));
            this.materiales.Location = new System.Drawing.Point(0, 198);
            this.materiales.Name = "materiales";
            this.materiales.Padding = new System.Windows.Forms.Padding(0, 0, 5, 0);
            this.materiales.RowCount = 2;
            this.materiales.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.materiales.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 70F));
            this.materiales.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.materiales.Size = new System.Drawing.Size(796, 252);
            this.materiales.TabIndex = 37;
            // 
            // btnExportarExcel
            // 
            this.btnExportarExcel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnExportarExcel.DisabledState.BorderColor = System.Drawing.Color.DarkGray;
            this.btnExportarExcel.DisabledState.CustomBorderColor = System.Drawing.Color.DarkGray;
            this.btnExportarExcel.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(169)))), ((int)(((byte)(169)))), ((int)(((byte)(169)))));
            this.btnExportarExcel.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(141)))), ((int)(((byte)(141)))), ((int)(((byte)(141)))));
            this.btnExportarExcel.FillColor = System.Drawing.Color.SeaGreen;
            this.btnExportarExcel.Font = new System.Drawing.Font("Bahnschrift Light", 11.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnExportarExcel.ForeColor = System.Drawing.Color.White;
            this.btnExportarExcel.Location = new System.Drawing.Point(557, 4);
            this.btnExportarExcel.Name = "btnExportarExcel";
            this.btnExportarExcel.Size = new System.Drawing.Size(55, 23);
            this.btnExportarExcel.TabIndex = 26;
            this.btnExportarExcel.Text = "Excel";
            this.btnExportarExcel.Click += new System.EventHandler(this.btnExportarExcel_Click);
            // 
            // btnExportarPDF
            // 
            this.btnExportarPDF.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnExportarPDF.DisabledState.BorderColor = System.Drawing.Color.DarkGray;
            this.btnExportarPDF.DisabledState.CustomBorderColor = System.Drawing.Color.DarkGray;
            this.btnExportarPDF.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(169)))), ((int)(((byte)(169)))), ((int)(((byte)(169)))));
            this.btnExportarPDF.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(141)))), ((int)(((byte)(141)))), ((int)(((byte)(141)))));
            this.btnExportarPDF.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(47)))), ((int)(((byte)(84)))), ((int)(((byte)(57)))));
            this.btnExportarPDF.Font = new System.Drawing.Font("Bahnschrift Light", 11.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnExportarPDF.ForeColor = System.Drawing.Color.White;
            this.btnExportarPDF.Location = new System.Drawing.Point(507, 4);
            this.btnExportarPDF.Name = "btnExportarPDF";
            this.btnExportarPDF.Size = new System.Drawing.Size(43, 23);
            this.btnExportarPDF.TabIndex = 25;
            this.btnExportarPDF.Text = "PDF";
            this.btnExportarPDF.Click += new System.EventHandler(this.btnExportarPDF_Click);
            // 
            // filtrarMaterial
            // 
            this.filtrarMaterial.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.filtrarMaterial.DisabledState.BorderColor = System.Drawing.Color.DarkGray;
            this.filtrarMaterial.DisabledState.CustomBorderColor = System.Drawing.Color.DarkGray;
            this.filtrarMaterial.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(169)))), ((int)(((byte)(169)))), ((int)(((byte)(169)))));
            this.filtrarMaterial.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(141)))), ((int)(((byte)(141)))), ((int)(((byte)(141)))));
            this.filtrarMaterial.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.filtrarMaterial.Font = new System.Drawing.Font("Bahnschrift Light", 11.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.filtrarMaterial.ForeColor = System.Drawing.Color.White;
            this.filtrarMaterial.Image = global::WP_Inventario.Properties.Resources.image__1_;
            this.filtrarMaterial.Location = new System.Drawing.Point(457, 4);
            this.filtrarMaterial.Name = "filtrarMaterial";
            this.filtrarMaterial.Size = new System.Drawing.Size(43, 23);
            this.filtrarMaterial.TabIndex = 24;
            this.filtrarMaterial.Click += new System.EventHandler(this.filtrarMaterial_Click);
            // 
            // toggleStockBajo
            // 
            this.toggleStockBajo.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.toggleStockBajo.Animated = true;
            this.toggleStockBajo.AutoRoundedCorners = true;
            this.toggleStockBajo.CheckedState.BorderColor = System.Drawing.Color.SeaGreen;
            this.toggleStockBajo.CheckedState.FillColor = System.Drawing.Color.SeaGreen;
            this.toggleStockBajo.CheckedState.InnerBorderColor = System.Drawing.Color.White;
            this.toggleStockBajo.CheckedState.InnerColor = System.Drawing.Color.White;
            this.toggleStockBajo.Location = new System.Drawing.Point(752, 6);
            this.toggleStockBajo.Name = "toggleStockBajo";
            this.toggleStockBajo.Size = new System.Drawing.Size(35, 20);
            this.toggleStockBajo.TabIndex = 27;
            this.toggleStockBajo.UncheckedState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(125)))), ((int)(((byte)(137)))), ((int)(((byte)(149)))));
            this.toggleStockBajo.UncheckedState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(125)))), ((int)(((byte)(137)))), ((int)(((byte)(149)))));
            this.toggleStockBajo.UncheckedState.InnerBorderColor = System.Drawing.Color.White;
            this.toggleStockBajo.UncheckedState.InnerColor = System.Drawing.Color.White;
            this.toggleStockBajo.CheckedChanged += new System.EventHandler(this.toggleStockBajo_CheckedChanged);
            // 
            // lblSoloStockBajo
            // 
            this.lblSoloStockBajo.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblSoloStockBajo.AutoSize = true;
            this.lblSoloStockBajo.Font = new System.Drawing.Font("Bahnschrift Light", 11.5F);
            this.lblSoloStockBajo.ForeColor = System.Drawing.Color.White;
            this.lblSoloStockBajo.Location = new System.Drawing.Point(619, 7);
            this.lblSoloStockBajo.Name = "lblSoloStockBajo";
            this.lblSoloStockBajo.Size = new System.Drawing.Size(126, 19);
            this.lblSoloStockBajo.TabIndex = 28;
            this.lblSoloStockBajo.Text = "Solo Stock Bajo:";
            // 
            // txtFiltroMaterial
            // 
            this.txtFiltroMaterial.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.txtFiltroMaterial.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(65)))));
            this.txtFiltroMaterial.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txtFiltroMaterial.DefaultText = "";
            this.txtFiltroMaterial.DisabledState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.txtFiltroMaterial.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(226)))), ((int)(((byte)(226)))), ((int)(((byte)(226)))));
            this.txtFiltroMaterial.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.txtFiltroMaterial.DisabledState.PlaceholderForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.txtFiltroMaterial.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtFiltroMaterial.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(50)))), ((int)(((byte)(40)))));
            this.txtFiltroMaterial.FocusedState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(39)))), ((int)(((byte)(174)))), ((int)(((byte)(96)))));
            this.txtFiltroMaterial.Font = new System.Drawing.Font("Segoe UI", 10.75F);
            this.txtFiltroMaterial.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(230)))), ((int)(((byte)(230)))));
            this.txtFiltroMaterial.HoverState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(39)))), ((int)(((byte)(174)))), ((int)(((byte)(96)))));
            this.txtFiltroMaterial.Location = new System.Drawing.Point(165, 5);
            this.txtFiltroMaterial.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.txtFiltroMaterial.Name = "txtFiltroMaterial";
            this.txtFiltroMaterial.PlaceholderText = "";
            this.txtFiltroMaterial.SelectedText = "";
            this.txtFiltroMaterial.Size = new System.Drawing.Size(285, 23);
            this.txtFiltroMaterial.TabIndex = 23;
            this.txtFiltroMaterial.TextChanged += new System.EventHandler(this.txtFiltroMaterial_TextChanged);
            // 
            // dgvMateriales
            // 
            this.dgvMateriales.AllowUserToAddRows = false;
            this.dgvMateriales.AllowUserToDeleteRows = false;
            this.dgvMateriales.AllowUserToResizeColumns = false;
            this.dgvMateriales.AllowUserToResizeRows = false;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(38)))), ((int)(((byte)(35)))));
            this.dgvMateriales.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
            this.dgvMateriales.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(38)))), ((int)(((byte)(35)))));
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(135)))), ((int)(((byte)(84)))));
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvMateriales.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
            this.dgvMateriales.ColumnHeadersHeight = 4;
            this.dgvMateriales.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.EnableResizing;
            this.materiales.SetColumnSpan(this.dgvMateriales, 7);
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(38)))), ((int)(((byte)(35)))));
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Bahnschrift SemiBold SemiConden", 13.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle3.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle3.Padding = new System.Windows.Forms.Padding(20, 0, 20, 0);
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(38)))), ((int)(((byte)(35)))));
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvMateriales.DefaultCellStyle = dataGridViewCellStyle3;
            this.dgvMateriales.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvMateriales.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(38)))), ((int)(((byte)(35)))));
            this.dgvMateriales.Location = new System.Drawing.Point(4, 36);
            this.dgvMateriales.Name = "dgvMateriales";
            this.dgvMateriales.ReadOnly = true;
            this.dgvMateriales.RowHeadersVisible = false;
            this.dgvMateriales.RowTemplate.DefaultCellStyle.Font = new System.Drawing.Font("Bahnschrift Light", 13.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.dgvMateriales.RowTemplate.Height = 50;
            this.dgvMateriales.Size = new System.Drawing.Size(783, 212);
            this.dgvMateriales.TabIndex = 22;
            this.dgvMateriales.Theme = Guna.UI2.WinForms.Enums.DataGridViewPresetThemes.Dark;
            this.dgvMateriales.ThemeStyle.AlternatingRowsStyle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(38)))), ((int)(((byte)(35)))));
            this.dgvMateriales.ThemeStyle.AlternatingRowsStyle.Font = null;
            this.dgvMateriales.ThemeStyle.AlternatingRowsStyle.ForeColor = System.Drawing.Color.Empty;
            this.dgvMateriales.ThemeStyle.AlternatingRowsStyle.SelectionBackColor = System.Drawing.Color.Empty;
            this.dgvMateriales.ThemeStyle.AlternatingRowsStyle.SelectionForeColor = System.Drawing.Color.Empty;
            this.dgvMateriales.ThemeStyle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(38)))), ((int)(((byte)(35)))));
            this.dgvMateriales.ThemeStyle.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(38)))), ((int)(((byte)(35)))));
            this.dgvMateriales.ThemeStyle.HeaderStyle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(135)))), ((int)(((byte)(84)))));
            this.dgvMateriales.ThemeStyle.HeaderStyle.BorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            this.dgvMateriales.ThemeStyle.HeaderStyle.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.dgvMateriales.ThemeStyle.HeaderStyle.ForeColor = System.Drawing.Color.White;
            this.dgvMateriales.ThemeStyle.HeaderStyle.HeaightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.EnableResizing;
            this.dgvMateriales.ThemeStyle.HeaderStyle.Height = 4;
            this.dgvMateriales.ThemeStyle.ReadOnly = true;
            this.dgvMateriales.ThemeStyle.RowsStyle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(38)))), ((int)(((byte)(35)))));
            this.dgvMateriales.ThemeStyle.RowsStyle.BorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            this.dgvMateriales.ThemeStyle.RowsStyle.Font = new System.Drawing.Font("Bahnschrift SemiBold SemiConden", 13.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.dgvMateriales.ThemeStyle.RowsStyle.ForeColor = System.Drawing.Color.White;
            this.dgvMateriales.ThemeStyle.RowsStyle.Height = 50;
            this.dgvMateriales.ThemeStyle.RowsStyle.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(38)))), ((int)(((byte)(35)))));
            this.dgvMateriales.ThemeStyle.RowsStyle.SelectionForeColor = System.Drawing.Color.White;
            this.dgvMateriales.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.dgvMateriales_CellFormatting);
            // 
            // label9
            // 
            this.label9.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label9.AutoSize = true;
            this.label9.Font = new System.Drawing.Font("Bahnschrift Light", 11.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label9.ForeColor = System.Drawing.Color.White;
            this.label9.Location = new System.Drawing.Point(4, 7);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(154, 19);
            this.label9.TabIndex = 0;
            this.label9.Text = "Buscar por nombre:";
            this.label9.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // panelAntiParpadeo6
            // 
            this.panelAntiParpadeo6.ColumnCount = 1;
            this.panelAntiParpadeo6.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.panelAntiParpadeo6.Controls.Add(this.label8, 0, 0);
            this.panelAntiParpadeo6.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelAntiParpadeo6.GrowStyle = System.Windows.Forms.TableLayoutPanelGrowStyle.AddColumns;
            this.panelAntiParpadeo6.Location = new System.Drawing.Point(0, 51);
            this.panelAntiParpadeo6.Margin = new System.Windows.Forms.Padding(3, 3, 3, 0);
            this.panelAntiParpadeo6.Name = "panelAntiParpadeo6";
            this.panelAntiParpadeo6.Padding = new System.Windows.Forms.Padding(10, 0, 10, 0);
            this.panelAntiParpadeo6.RowCount = 1;
            this.panelAntiParpadeo6.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.panelAntiParpadeo6.Size = new System.Drawing.Size(796, 32);
            this.panelAntiParpadeo6.TabIndex = 36;
            // 
            // label8
            // 
            this.label8.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label8.AutoSize = true;
            this.label8.Font = new System.Drawing.Font("Bahnschrift SemiBold", 16F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label8.ForeColor = System.Drawing.SystemColors.Control;
            this.label8.ImageAlign = System.Drawing.ContentAlignment.BottomLeft;
            this.label8.Location = new System.Drawing.Point(13, 2);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(770, 27);
            this.label8.TabIndex = 3;
            this.label8.Text = "Stock de Materiales";
            this.label8.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            // 
            // panelAntiParpadeo1
            // 
            this.panelAntiParpadeo1.ColumnCount = 1;
            this.panelAntiParpadeo1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.panelAntiParpadeo1.Controls.Add(this.label1, 0, 0);
            this.panelAntiParpadeo1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelAntiParpadeo1.GrowStyle = System.Windows.Forms.TableLayoutPanelGrowStyle.AddColumns;
            this.panelAntiParpadeo1.Location = new System.Drawing.Point(0, 0);
            this.panelAntiParpadeo1.Name = "panelAntiParpadeo1";
            this.panelAntiParpadeo1.Padding = new System.Windows.Forms.Padding(40, 0, 40, 0);
            this.panelAntiParpadeo1.RowCount = 1;
            this.panelAntiParpadeo1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.panelAntiParpadeo1.Size = new System.Drawing.Size(796, 51);
            this.panelAntiParpadeo1.TabIndex = 13;
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Bahnschrift SemiBold", 22F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.SystemColors.Control;
            this.label1.Location = new System.Drawing.Point(43, 7);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(710, 36);
            this.label1.TabIndex = 3;
            this.label1.Text = "REPORTE DE STOCK";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // Vista_ReporteStock
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(38)))), ((int)(((byte)(35)))));
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.materiales);
            this.Controls.Add(this.panelDashboard);
            this.Controls.Add(this.panelAntiParpadeo6);
            this.Controls.Add(this.panelAntiParpadeo1);
            this.Name = "Vista_ReporteStock";
            this.Padding = new System.Windows.Forms.Padding(0, 0, 4, 0);
            this.Text = "Reporte Stock";
            this.panelDashboard.ResumeLayout(false);
            this.panelTotal.ResumeLayout(false);
            this.panelTotal.PerformLayout();
            this.panelBajo.ResumeLayout(false);
            this.panelBajo.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.chartCategorias)).EndInit();
            this.materiales.ResumeLayout(false);
            this.materiales.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvMateriales)).EndInit();
            this.panelAntiParpadeo6.ResumeLayout(false);
            this.panelAntiParpadeo6.PerformLayout();
            this.panelAntiParpadeo1.ResumeLayout(false);
            this.panelAntiParpadeo1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private PanelAntiParpadeo panelAntiParpadeo1;
        private System.Windows.Forms.Label label1;
        private PanelAntiParpadeo panelAntiParpadeo6;
        private System.Windows.Forms.Label label8;
        private PanelAntiParpadeo materiales;
        private System.Windows.Forms.Label label9;
        private Guna.UI2.WinForms.Guna2DataGridView dgvMateriales;
        private Guna.UI2.WinForms.Guna2TextBox txtFiltroMaterial;
        private Guna.UI2.WinForms.Guna2Button filtrarMaterial;
        private Guna.UI2.WinForms.Guna2Button btnExportarPDF;
        private Guna.UI2.WinForms.Guna2Button btnExportarExcel;
        private Guna.UI2.WinForms.Guna2ToggleSwitch toggleStockBajo;
        private System.Windows.Forms.Label lblSoloStockBajo;
        private System.Windows.Forms.TableLayoutPanel panelDashboard;
        private Guna.UI2.WinForms.Guna2Panel panelTotal;
        private System.Windows.Forms.Label lblTotalVal;
        private System.Windows.Forms.Label lblTotalTit;
        private Guna.UI2.WinForms.Guna2Panel panelBajo;
        private System.Windows.Forms.Label lblBajoVal;
        private System.Windows.Forms.Label lblBajoTit;
        private System.Windows.Forms.DataVisualization.Charting.Chart chartCategorias;
    }
}

