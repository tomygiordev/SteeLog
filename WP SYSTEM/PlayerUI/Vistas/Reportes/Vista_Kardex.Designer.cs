namespace WP_Inventario.Vistas
{
    partial class Vista_Kardex
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
            this.materiales = new WP_Inventario.PanelAntiParpadeo();
            this.txtBuscadorMaterial = new Guna.UI2.WinForms.Guna2TextBox();
            this.dgvKardex = new Guna.UI2.WinForms.Guna2DataGridView();
            this.labelSelect = new System.Windows.Forms.Label();
            this.panelAntiParpadeo1 = new WP_Inventario.PanelAntiParpadeo();
            this.labelTitle = new System.Windows.Forms.Label();
            this.lblMaterialNombre = new System.Windows.Forms.Label();
            this.materiales.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvKardex)).BeginInit();
            this.panelAntiParpadeo1.SuspendLayout();
            this.SuspendLayout();
            // 
            // materiales
            // 
            this.materiales.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.materiales.AutoSize = true;
            this.materiales.CellBorderStyle = System.Windows.Forms.TableLayoutPanelCellBorderStyle.Single;
            this.materiales.ColumnCount = 2;
            this.materiales.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.materiales.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.materiales.Controls.Add(this.txtBuscadorMaterial, 1, 0);
            this.materiales.Controls.Add(this.dgvKardex, 0, 1);
            this.materiales.Controls.Add(this.labelSelect, 0, 0);
            this.materiales.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(63)))), ((int)(((byte)(43)))));
            this.materiales.Location = new System.Drawing.Point(0, 90);
            this.materiales.Name = "materiales";
            this.materiales.Padding = new System.Windows.Forms.Padding(0, 0, 5, 0);
            this.materiales.RowCount = 2;
            this.materiales.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50F));
            this.materiales.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.materiales.Size = new System.Drawing.Size(796, 360);
            this.materiales.TabIndex = 37;
            // 
            // txtBuscadorMaterial
            // 
            this.txtBuscadorMaterial.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtBuscadorMaterial.BackColor = System.Drawing.Color.Transparent;
            this.txtBuscadorMaterial.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(65)))));
            this.txtBuscadorMaterial.BorderRadius = 8;
            this.txtBuscadorMaterial.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txtBuscadorMaterial.DefaultText = "";
            this.txtBuscadorMaterial.DisabledState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.txtBuscadorMaterial.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(226)))), ((int)(((byte)(226)))), ((int)(((byte)(226)))));
            this.txtBuscadorMaterial.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.txtBuscadorMaterial.DisabledState.PlaceholderForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.txtBuscadorMaterial.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(45)))), ((int)(((byte)(40)))));
            this.txtBuscadorMaterial.FocusedState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(39)))), ((int)(((byte)(174)))), ((int)(((byte)(96)))));
            this.txtBuscadorMaterial.Font = new System.Drawing.Font("Segoe UI", 11.75F);
            this.txtBuscadorMaterial.ForeColor = System.Drawing.Color.White;
            this.txtBuscadorMaterial.HoverState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(39)))), ((int)(((byte)(174)))), ((int)(((byte)(96)))));
            this.txtBuscadorMaterial.Location = new System.Drawing.Point(175, 7);
            this.txtBuscadorMaterial.Margin = new System.Windows.Forms.Padding(4, 5, 10, 5);
            this.txtBuscadorMaterial.Name = "txtBuscadorMaterial";
            this.txtBuscadorMaterial.PasswordChar = '\0';
            this.txtBuscadorMaterial.PlaceholderText = "🔍 Buscar material por nombre / categoría...";
            this.txtBuscadorMaterial.SelectedText = "";
            this.txtBuscadorMaterial.Size = new System.Drawing.Size(606, 36);
            this.txtBuscadorMaterial.TabIndex = 6;
            // 
            // dgvKardex
            // 
            this.dgvKardex.AllowUserToAddRows = false;
            this.dgvKardex.AllowUserToDeleteRows = false;
            this.dgvKardex.AllowUserToResizeColumns = false;
            this.dgvKardex.AllowUserToResizeRows = false;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(44)))), ((int)(((byte)(48)))), ((int)(((byte)(52)))));
            this.dgvKardex.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
            this.dgvKardex.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(38)))), ((int)(((byte)(35)))));
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(16)))), ((int)(((byte)(18)))));
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvKardex.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
            this.dgvKardex.ColumnHeadersHeight = 35;
            this.dgvKardex.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.EnableResizing;
            this.materiales.SetColumnSpan(this.dgvKardex, 2);
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(37)))), ((int)(((byte)(41)))));
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Bahnschrift SemiBold SemiConden", 13.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle3.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(114)))), ((int)(((byte)(117)))), ((int)(((byte)(119)))));
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvKardex.DefaultCellStyle = dataGridViewCellStyle3;
            this.dgvKardex.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvKardex.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(56)))), ((int)(((byte)(62)))));
            this.dgvKardex.Location = new System.Drawing.Point(4, 55);
            this.dgvKardex.Name = "dgvKardex";
            this.dgvKardex.ReadOnly = true;
            this.dgvKardex.RowHeadersVisible = false;
            this.dgvKardex.RowTemplate.DefaultCellStyle.Font = new System.Drawing.Font("Bahnschrift Light", 13.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.dgvKardex.RowTemplate.Height = 30;
            this.dgvKardex.Size = new System.Drawing.Size(783, 301);
            this.dgvKardex.TabIndex = 22;
            this.dgvKardex.Theme = Guna.UI2.WinForms.Enums.DataGridViewPresetThemes.Dark;
            this.dgvKardex.ThemeStyle.AlternatingRowsStyle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(44)))), ((int)(((byte)(48)))), ((int)(((byte)(52)))));
            this.dgvKardex.ThemeStyle.AlternatingRowsStyle.Font = null;
            this.dgvKardex.ThemeStyle.AlternatingRowsStyle.ForeColor = System.Drawing.Color.Empty;
            this.dgvKardex.ThemeStyle.AlternatingRowsStyle.SelectionBackColor = System.Drawing.Color.Empty;
            this.dgvKardex.ThemeStyle.AlternatingRowsStyle.SelectionForeColor = System.Drawing.Color.Empty;
            this.dgvKardex.ThemeStyle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(38)))), ((int)(((byte)(35)))));
            this.dgvKardex.ThemeStyle.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(56)))), ((int)(((byte)(62)))));
            this.dgvKardex.ThemeStyle.HeaderStyle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(16)))), ((int)(((byte)(18)))));
            this.dgvKardex.ThemeStyle.HeaderStyle.BorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            this.dgvKardex.ThemeStyle.HeaderStyle.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.dgvKardex.ThemeStyle.HeaderStyle.ForeColor = System.Drawing.Color.White;
            this.dgvKardex.ThemeStyle.HeaderStyle.HeaightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.EnableResizing;
            this.dgvKardex.ThemeStyle.HeaderStyle.Height = 35;
            this.dgvKardex.ThemeStyle.ReadOnly = true;
            this.dgvKardex.ThemeStyle.RowsStyle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(37)))), ((int)(((byte)(41)))));
            this.dgvKardex.ThemeStyle.RowsStyle.BorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            this.dgvKardex.ThemeStyle.RowsStyle.Font = new System.Drawing.Font("Bahnschrift SemiBold SemiConden", 13.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.dgvKardex.ThemeStyle.RowsStyle.ForeColor = System.Drawing.Color.White;
            this.dgvKardex.ThemeStyle.RowsStyle.Height = 30;
            this.dgvKardex.ThemeStyle.RowsStyle.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(114)))), ((int)(((byte)(117)))), ((int)(((byte)(119)))));
            this.dgvKardex.ThemeStyle.RowsStyle.SelectionForeColor = System.Drawing.Color.White;
            // 
            // labelSelect
            // 
            this.labelSelect.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.labelSelect.AutoSize = true;
            this.labelSelect.Font = new System.Drawing.Font("Bahnschrift Light", 13.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelSelect.ForeColor = System.Drawing.Color.White;
            this.labelSelect.Location = new System.Drawing.Point(4, 16);
            this.labelSelect.Name = "labelSelect";
            this.labelSelect.Size = new System.Drawing.Size(164, 19);
            this.labelSelect.TabIndex = 0;
            this.labelSelect.Text = "Seleccionar Material:";
            this.labelSelect.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // panelAntiParpadeo1
            // 
            this.panelAntiParpadeo1.ColumnCount = 1;
            this.panelAntiParpadeo1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.panelAntiParpadeo1.Controls.Add(this.labelTitle, 0, 0);
            this.panelAntiParpadeo1.Controls.Add(this.lblMaterialNombre, 0, 1);
            this.panelAntiParpadeo1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelAntiParpadeo1.GrowStyle = System.Windows.Forms.TableLayoutPanelGrowStyle.AddColumns;
            this.panelAntiParpadeo1.Location = new System.Drawing.Point(0, 0);
            this.panelAntiParpadeo1.Name = "panelAntiParpadeo1";
            this.panelAntiParpadeo1.Padding = new System.Windows.Forms.Padding(40, 0, 40, 0);
            this.panelAntiParpadeo1.RowCount = 2;
            this.panelAntiParpadeo1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.panelAntiParpadeo1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.panelAntiParpadeo1.Size = new System.Drawing.Size(796, 80);
            this.panelAntiParpadeo1.TabIndex = 13;
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
            this.labelTitle.Text = "KARDEX DE MATERIALES";
            this.labelTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblMaterialNombre
            // 
            this.lblMaterialNombre.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblMaterialNombre.AutoSize = true;
            this.lblMaterialNombre.Font = new System.Drawing.Font("Bahnschrift Light", 16F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblMaterialNombre.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblMaterialNombre.Location = new System.Drawing.Point(43, 48);
            this.lblMaterialNombre.Name = "lblMaterialNombre";
            this.lblMaterialNombre.Size = new System.Drawing.Size(710, 23);
            this.lblMaterialNombre.TabIndex = 4;
            this.lblMaterialNombre.Text = "Seleccione un material";
            this.lblMaterialNombre.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // Vista_Kardex
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(38)))), ((int)(((byte)(35)))));
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.materiales);
            this.Controls.Add(this.panelAntiParpadeo1);
            this.Name = "Vista_Kardex";
            this.Padding = new System.Windows.Forms.Padding(0, 0, 4, 0);
            this.Text = "Kardex";
            this.materiales.ResumeLayout(false);
            this.materiales.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvKardex)).EndInit();
            this.panelAntiParpadeo1.ResumeLayout(false);
            this.panelAntiParpadeo1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private PanelAntiParpadeo panelAntiParpadeo1;
        private System.Windows.Forms.Label labelTitle;
        private System.Windows.Forms.Label lblMaterialNombre;
        private PanelAntiParpadeo materiales;
        private System.Windows.Forms.Label labelSelect;
        private Guna.UI2.WinForms.Guna2DataGridView dgvKardex;
        private Guna.UI2.WinForms.Guna2TextBox txtBuscadorMaterial;
    }
}

