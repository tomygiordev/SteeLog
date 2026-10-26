namespace WP_Inventario.Vistas.Movimientos
{
    partial class ModalInsumos
    {
        private System.ComponentModel.IContainer components = null;
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) { components.Dispose(); }
            base.Dispose(disposing);
        }
        private void InitializeComponent()
        {
            System.Windows.Forms.DataGridViewCellStyle cs1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle cs2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle cs3 = new System.Windows.Forms.DataGridViewCellStyle();
            this.lblTitulo = new System.Windows.Forms.Label();
            this.lblBuscar = new System.Windows.Forms.Label();
            this.txtBuscar = new Guna.UI2.WinForms.Guna2TextBox();
            this.dgvInsumos = new Guna.UI2.WinForms.Guna2DataGridView();
            this.panelBotones = new System.Windows.Forms.FlowLayoutPanel();
            this.btnConfirmar = new Guna.UI2.WinForms.Guna2Button();
            this.btnCancelar = new Guna.UI2.WinForms.Guna2Button();
            this.panelBotones.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvInsumos)).BeginInit();
            this.SuspendLayout();
            // lblTitulo
            this.lblTitulo.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblTitulo.Font = new System.Drawing.Font("Bahnschrift SemiBold", 16F, System.Drawing.FontStyle.Bold);
            this.lblTitulo.ForeColor = System.Drawing.Color.White;
            this.lblTitulo.Location = new System.Drawing.Point(0, 0);
            this.lblTitulo.Name = "lblTitulo";
            this.lblTitulo.Padding = new System.Windows.Forms.Padding(15, 10, 15, 0);
            this.lblTitulo.Size = new System.Drawing.Size(530, 45);
            this.lblTitulo.TabIndex = 0;
            this.lblTitulo.Text = "Seleccionar Insumo";
            this.lblTitulo.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // lblBuscar
            this.lblBuscar.Font = new System.Drawing.Font("Bahnschrift Light", 12F);
            this.lblBuscar.ForeColor = System.Drawing.Color.FromArgb(200, 200, 200);
            this.lblBuscar.Location = new System.Drawing.Point(15, 50);
            this.lblBuscar.Name = "lblBuscar";
            this.lblBuscar.Size = new System.Drawing.Size(70, 30);
            this.lblBuscar.TabIndex = 1;
            this.lblBuscar.Text = "Buscar:";
            this.lblBuscar.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // txtBuscar
            this.txtBuscar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.txtBuscar.BorderColor = System.Drawing.Color.FromArgb(60, 60, 65);
            this.txtBuscar.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txtBuscar.DefaultText = "";
            this.txtBuscar.FillColor = System.Drawing.Color.FromArgb(35, 50, 40);
            this.txtBuscar.FocusedState.BorderColor = System.Drawing.Color.FromArgb(39, 174, 96);
            this.txtBuscar.Font = new System.Drawing.Font("Segoe UI", 10.75F);
            this.txtBuscar.ForeColor = System.Drawing.Color.FromArgb(230, 230, 230);
            this.txtBuscar.HoverState.BorderColor = System.Drawing.Color.FromArgb(39, 174, 96);
            this.txtBuscar.Location = new System.Drawing.Point(90, 48);
            this.txtBuscar.Name = "txtBuscar";
            this.txtBuscar.PlaceholderText = "Escriba para filtrar...";
            this.txtBuscar.SelectedText = "";
            this.txtBuscar.Size = new System.Drawing.Size(425, 34);
            this.txtBuscar.TabIndex = 2;
            // dgvInsumos
            this.dgvInsumos.AllowUserToAddRows = false;
            this.dgvInsumos.AllowUserToDeleteRows = false;
            this.dgvInsumos.AllowUserToResizeRows = false;
            this.dgvInsumos.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            cs1.BackColor = System.Drawing.Color.FromArgb(29, 38, 35);
            this.dgvInsumos.AlternatingRowsDefaultCellStyle = cs1;
            this.dgvInsumos.BackgroundColor = System.Drawing.Color.FromArgb(29, 38, 35);
            cs2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            cs2.BackColor = System.Drawing.Color.FromArgb(25, 135, 84);
            cs2.Font = new System.Drawing.Font("Bahnschrift SemiBold", 11F, System.Drawing.FontStyle.Bold);
            cs2.ForeColor = System.Drawing.Color.White;
            cs2.SelectionBackColor = System.Drawing.Color.FromArgb(25, 135, 84);
            cs2.SelectionForeColor = System.Drawing.Color.White;
            cs2.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvInsumos.ColumnHeadersDefaultCellStyle = cs2;
            this.dgvInsumos.ColumnHeadersHeight = 35;
            this.dgvInsumos.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            cs3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            cs3.BackColor = System.Drawing.Color.FromArgb(29, 38, 35);
            cs3.Font = new System.Drawing.Font("Bahnschrift Light", 11.75F);
            cs3.ForeColor = System.Drawing.Color.White;
            cs3.SelectionBackColor = System.Drawing.Color.FromArgb(47, 84, 57);
            cs3.SelectionForeColor = System.Drawing.Color.White;
            cs3.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvInsumos.DefaultCellStyle = cs3;
            this.dgvInsumos.GridColor = System.Drawing.Color.FromArgb(40, 55, 45);
            this.dgvInsumos.Location = new System.Drawing.Point(15, 90);
            this.dgvInsumos.Name = "dgvInsumos";
            this.dgvInsumos.ReadOnly = true;
            this.dgvInsumos.RowHeadersVisible = false;
            this.dgvInsumos.RowTemplate.Height = 38;
            this.dgvInsumos.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvInsumos.Size = new System.Drawing.Size(500, 250);
            this.dgvInsumos.TabIndex = 3;
            this.dgvInsumos.Theme = Guna.UI2.WinForms.Enums.DataGridViewPresetThemes.Dark;
            this.dgvInsumos.ThemeStyle.BackColor = System.Drawing.Color.FromArgb(29, 38, 35);
            this.dgvInsumos.ThemeStyle.GridColor = System.Drawing.Color.FromArgb(40, 55, 45);
            this.dgvInsumos.ThemeStyle.HeaderStyle.BackColor = System.Drawing.Color.FromArgb(25, 135, 84);
            this.dgvInsumos.ThemeStyle.HeaderStyle.BorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            this.dgvInsumos.ThemeStyle.HeaderStyle.ForeColor = System.Drawing.Color.White;
            this.dgvInsumos.ThemeStyle.HeaderStyle.HeaightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.dgvInsumos.ThemeStyle.HeaderStyle.Height = 35;
            this.dgvInsumos.ThemeStyle.ReadOnly = true;
            this.dgvInsumos.ThemeStyle.RowsStyle.BackColor = System.Drawing.Color.FromArgb(29, 38, 35);
            this.dgvInsumos.ThemeStyle.RowsStyle.ForeColor = System.Drawing.Color.White;
            this.dgvInsumos.ThemeStyle.RowsStyle.Height = 38;
            this.dgvInsumos.ThemeStyle.RowsStyle.SelectionBackColor = System.Drawing.Color.FromArgb(47, 84, 57);
            this.dgvInsumos.ThemeStyle.RowsStyle.SelectionForeColor = System.Drawing.Color.White;
            // panelBotones
            this.panelBotones.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.panelBotones.Controls.Add(this.btnConfirmar);
            this.panelBotones.Controls.Add(this.btnCancelar);
            this.panelBotones.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.panelBotones.Location = new System.Drawing.Point(15, 348);
            this.panelBotones.Name = "panelBotones";
            this.panelBotones.Size = new System.Drawing.Size(500, 45);
            this.panelBotones.TabIndex = 4;
            // btnConfirmar
            this.btnConfirmar.FillColor = System.Drawing.Color.FromArgb(25, 135, 84);
            this.btnConfirmar.Font = new System.Drawing.Font("Bahnschrift SemiBold", 12F, System.Drawing.FontStyle.Bold);
            this.btnConfirmar.ForeColor = System.Drawing.Color.White;
            this.btnConfirmar.Location = new System.Drawing.Point(360, 3);
            this.btnConfirmar.Name = "btnConfirmar";
            this.btnConfirmar.Size = new System.Drawing.Size(137, 38);
            this.btnConfirmar.TabIndex = 0;
            this.btnConfirmar.Text = "Confirmar";
            this.btnConfirmar.Click += new System.EventHandler(this.btnConfirmar_Click);
            // btnCancelar
            this.btnCancelar.FillColor = System.Drawing.Color.FromArgb(100, 100, 100);
            this.btnCancelar.Font = new System.Drawing.Font("Bahnschrift SemiBold", 12F, System.Drawing.FontStyle.Bold);
            this.btnCancelar.ForeColor = System.Drawing.Color.White;
            this.btnCancelar.Location = new System.Drawing.Point(217, 3);
            this.btnCancelar.Name = "btnCancelar";
            this.btnCancelar.Size = new System.Drawing.Size(137, 38);
            this.btnCancelar.TabIndex = 1;
            this.btnCancelar.Text = "Cancelar";
            this.btnCancelar.Click += new System.EventHandler(this.btnCancelar_Click);
            // ModalInsumos
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(29, 38, 35);
            this.ClientSize = new System.Drawing.Size(530, 400);
            this.Controls.Add(this.panelBotones);
            this.Controls.Add(this.dgvInsumos);
            this.Controls.Add(this.txtBuscar);
            this.Controls.Add(this.lblBuscar);
            this.Controls.Add(this.lblTitulo);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ModalInsumos";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Seleccionar Insumo";
            this.panelBotones.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvInsumos)).EndInit();
            this.ResumeLayout(false);
        }
        private System.Windows.Forms.Label lblTitulo;
        private System.Windows.Forms.Label lblBuscar;
        private Guna.UI2.WinForms.Guna2TextBox txtBuscar;
        private Guna.UI2.WinForms.Guna2DataGridView dgvInsumos;
        private System.Windows.Forms.FlowLayoutPanel panelBotones;
        private Guna.UI2.WinForms.Guna2Button btnConfirmar;
        private Guna.UI2.WinForms.Guna2Button btnCancelar;
    }
}
