namespace WP_Inventario.Vistas.Movimientos
{
    partial class ModalRecortes
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

        private void InitializeComponent()
        {
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            this.lblMaterial            = new System.Windows.Forms.Label();
            this.lblFiltro              = new System.Windows.Forms.Label();
            this.lblAncho               = new System.Windows.Forms.Label();
            this.txtAnchoBuscado        = new Guna.UI2.WinForms.Guna2TextBox();
            this.lblX                   = new System.Windows.Forms.Label();
            this.lblAltura              = new System.Windows.Forms.Label();
            this.txtAlturaBuscada       = new Guna.UI2.WinForms.Guna2TextBox();
            this.btnCrearNuevo          = new Guna.UI2.WinForms.Guna2Button();
            this.lblInfo                = new System.Windows.Forms.Label();
            this.dgvRecortesDisponibles = new Guna.UI2.WinForms.Guna2DataGridView();
            this.pnlCantidad            = new System.Windows.Forms.Panel();
            this.label2                 = new System.Windows.Forms.Label();
            this.txtCantidad            = new Guna.UI2.WinForms.Guna2TextBox();
            this.btnSumarTodo           = new Guna.UI2.WinForms.Guna2Button();
            this.btnCancelar            = new Guna.UI2.WinForms.Guna2Button();
            this.btnSeleccionarRecorte  = new Guna.UI2.WinForms.Guna2Button();
            ((System.ComponentModel.ISupportInitialize)(this.dgvRecortesDisponibles)).BeginInit();
            this.pnlCantidad.SuspendLayout();
            this.SuspendLayout();

            // ── lblMaterial ─────────────────────────────────────────────
            this.lblMaterial.AutoSize = true;
            this.lblMaterial.Font = new System.Drawing.Font("Bahnschrift SemiBold", 15F, System.Drawing.FontStyle.Bold);
            this.lblMaterial.ForeColor = System.Drawing.Color.White;
            this.lblMaterial.Location = new System.Drawing.Point(20, 18);
            this.lblMaterial.MaximumSize = new System.Drawing.Size(560, 0);
            this.lblMaterial.Name = "lblMaterial";
            this.lblMaterial.TabIndex = 0;
            this.lblMaterial.Text = "Material: [NOMBRE]";

            // ── lblFiltro ────────────────────────────────────────────────
            this.lblFiltro.AutoSize = true;
            this.lblFiltro.Font = new System.Drawing.Font("Bahnschrift", 11F);
            this.lblFiltro.ForeColor = System.Drawing.Color.FromArgb(180, 210, 190);
            this.lblFiltro.Location = new System.Drawing.Point(20, 68);
            this.lblFiltro.Name = "lblFiltro";
            this.lblFiltro.TabIndex = 1;
            this.lblFiltro.Text = "Filtrar por dimensiones:";

            // ── lblAncho ─────────────────────────────────────────────────
            this.lblAncho.AutoSize = true;
            this.lblAncho.Font = new System.Drawing.Font("Bahnschrift Light", 10F);
            this.lblAncho.ForeColor = System.Drawing.Color.FromArgb(155, 185, 165);
            this.lblAncho.Location = new System.Drawing.Point(20, 100);
            this.lblAncho.Name = "lblAncho";
            this.lblAncho.TabIndex = 2;
            this.lblAncho.Text = "Ancho:";

            // ── txtAnchoBuscado ──────────────────────────────────────────
            this.txtAnchoBuscado.BorderColor = System.Drawing.Color.FromArgb(60, 90, 70);
            this.txtAnchoBuscado.BorderThickness = 1;
            this.txtAnchoBuscado.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txtAnchoBuscado.DefaultText = "";
            this.txtAnchoBuscado.FillColor = System.Drawing.Color.FromArgb(35, 50, 40);
            this.txtAnchoBuscado.FocusedState.BorderColor = System.Drawing.Color.FromArgb(25, 135, 84);
            this.txtAnchoBuscado.Font = new System.Drawing.Font("Bahnschrift", 13F);
            this.txtAnchoBuscado.ForeColor = System.Drawing.Color.White;
            this.txtAnchoBuscado.HoverState.BorderColor = System.Drawing.Color.FromArgb(60, 140, 90);
            this.txtAnchoBuscado.Location = new System.Drawing.Point(20, 120);
            this.txtAnchoBuscado.Name = "txtAnchoBuscado";
            this.txtAnchoBuscado.PasswordChar = '\0';
            this.txtAnchoBuscado.PlaceholderText = "ej: 1.5";
            this.txtAnchoBuscado.SelectedText = "";
            this.txtAnchoBuscado.Size = new System.Drawing.Size(160, 40);
            this.txtAnchoBuscado.TabIndex = 3;

            // ── lblX ─────────────────────────────────────────────────────
            this.lblX.AutoSize = true;
            this.lblX.Font = new System.Drawing.Font("Bahnschrift", 16F);
            this.lblX.ForeColor = System.Drawing.Color.FromArgb(145, 175, 158);
            this.lblX.Location = new System.Drawing.Point(190, 126);
            this.lblX.Name = "lblX";
            this.lblX.TabIndex = 9;
            this.lblX.Text = "×";

            // ── lblAltura ─────────────────────────────────────────────────
            this.lblAltura.AutoSize = true;
            this.lblAltura.Font = new System.Drawing.Font("Bahnschrift Light", 10F);
            this.lblAltura.ForeColor = System.Drawing.Color.FromArgb(155, 185, 165);
            this.lblAltura.Location = new System.Drawing.Point(215, 100);
            this.lblAltura.Name = "lblAltura";
            this.lblAltura.TabIndex = 10;
            this.lblAltura.Text = "Altura:";

            // ── txtAlturaBuscada ─────────────────────────────────────────
            this.txtAlturaBuscada.BorderColor = System.Drawing.Color.FromArgb(60, 90, 70);
            this.txtAlturaBuscada.BorderThickness = 1;
            this.txtAlturaBuscada.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txtAlturaBuscada.DefaultText = "";
            this.txtAlturaBuscada.FillColor = System.Drawing.Color.FromArgb(35, 50, 40);
            this.txtAlturaBuscada.FocusedState.BorderColor = System.Drawing.Color.FromArgb(25, 135, 84);
            this.txtAlturaBuscada.Font = new System.Drawing.Font("Bahnschrift", 13F);
            this.txtAlturaBuscada.ForeColor = System.Drawing.Color.White;
            this.txtAlturaBuscada.HoverState.BorderColor = System.Drawing.Color.FromArgb(60, 140, 90);
            this.txtAlturaBuscada.Location = new System.Drawing.Point(215, 120);
            this.txtAlturaBuscada.Name = "txtAlturaBuscada";
            this.txtAlturaBuscada.PasswordChar = '\0';
            this.txtAlturaBuscada.PlaceholderText = "ej: 2.0";
            this.txtAlturaBuscada.SelectedText = "";
            this.txtAlturaBuscada.Size = new System.Drawing.Size(160, 40);
            this.txtAlturaBuscada.TabIndex = 4;

            // ── btnCrearNuevo ────────────────────────────────────────────
            this.btnCrearNuevo.BorderRadius = 0;
            this.btnCrearNuevo.FillColor = System.Drawing.Color.FromArgb(39, 174, 96);
            this.btnCrearNuevo.Font = new System.Drawing.Font("Bahnschrift Light", 12F);
            this.btnCrearNuevo.ForeColor = System.Drawing.Color.White;
            this.btnCrearNuevo.Location = new System.Drawing.Point(390, 120);
            this.btnCrearNuevo.Name = "btnCrearNuevo";
            this.btnCrearNuevo.Size = new System.Drawing.Size(190, 40);
            this.btnCrearNuevo.TabIndex = 5;
            this.btnCrearNuevo.Text = "CREAR Y USAR";
            this.btnCrearNuevo.Click += new System.EventHandler(this.btnCrearNuevo_Click);

            // ── lblInfo ──────────────────────────────────────────────────
            this.lblInfo.AutoSize = true;
            this.lblInfo.Font = new System.Drawing.Font("Bahnschrift Light", 10F);
            this.lblInfo.ForeColor = System.Drawing.Color.FromArgb(180, 210, 150);
            this.lblInfo.Location = new System.Drawing.Point(20, 174);
            this.lblInfo.Name = "lblInfo";
            this.lblInfo.TabIndex = 6;
            this.lblInfo.Text = "Recortes disponibles:";

            // ── dgvRecortesDisponibles ───────────────────────────────────
            this.dgvRecortesDisponibles.AllowUserToAddRows = false;
            this.dgvRecortesDisponibles.AllowUserToDeleteRows = false;
            this.dgvRecortesDisponibles.AllowUserToResizeColumns = false;
            this.dgvRecortesDisponibles.AllowUserToResizeRows = false;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(29, 38, 35);
            this.dgvRecortesDisponibles.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
            this.dgvRecortesDisponibles.BackgroundColor = System.Drawing.Color.FromArgb(29, 38, 35);
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.FromArgb(25, 135, 84);
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Bahnschrift SemiLight", 12F);
            dataGridViewCellStyle2.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvRecortesDisponibles.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
            this.dgvRecortesDisponibles.ColumnHeadersHeight = 34;
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = System.Drawing.Color.FromArgb(29, 38, 35);
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Bahnschrift SemiLight", 12F);
            dataGridViewCellStyle3.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.Color.FromArgb(35, 110, 65);
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvRecortesDisponibles.DefaultCellStyle = dataGridViewCellStyle3;
            this.dgvRecortesDisponibles.EnableHeadersVisualStyles = false;
            this.dgvRecortesDisponibles.GridColor = System.Drawing.Color.FromArgb(40, 55, 45);
            this.dgvRecortesDisponibles.Location = new System.Drawing.Point(20, 196);
            this.dgvRecortesDisponibles.Name = "dgvRecortesDisponibles";
            this.dgvRecortesDisponibles.ReadOnly = true;
            this.dgvRecortesDisponibles.RowHeadersVisible = false;
            this.dgvRecortesDisponibles.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvRecortesDisponibles.Size = new System.Drawing.Size(560, 240);
            this.dgvRecortesDisponibles.TabIndex = 7;
            this.dgvRecortesDisponibles.Theme = Guna.UI2.WinForms.Enums.DataGridViewPresetThemes.Dark;
            this.dgvRecortesDisponibles.ThemeStyle.AlternatingRowsStyle.BackColor = System.Drawing.Color.FromArgb(33, 44, 38);
            this.dgvRecortesDisponibles.ThemeStyle.AlternatingRowsStyle.Font = null;
            this.dgvRecortesDisponibles.ThemeStyle.AlternatingRowsStyle.ForeColor = System.Drawing.Color.Empty;
            this.dgvRecortesDisponibles.ThemeStyle.AlternatingRowsStyle.SelectionBackColor = System.Drawing.Color.Empty;
            this.dgvRecortesDisponibles.ThemeStyle.AlternatingRowsStyle.SelectionForeColor = System.Drawing.Color.Empty;
            this.dgvRecortesDisponibles.ThemeStyle.BackColor = System.Drawing.Color.FromArgb(29, 38, 35);
            this.dgvRecortesDisponibles.ThemeStyle.GridColor = System.Drawing.Color.FromArgb(40, 55, 45);
            this.dgvRecortesDisponibles.ThemeStyle.HeaderStyle.BackColor = System.Drawing.Color.FromArgb(25, 135, 84);
            this.dgvRecortesDisponibles.ThemeStyle.HeaderStyle.BorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            this.dgvRecortesDisponibles.ThemeStyle.HeaderStyle.Font = new System.Drawing.Font("Bahnschrift SemiLight", 12F);
            this.dgvRecortesDisponibles.ThemeStyle.HeaderStyle.ForeColor = System.Drawing.Color.White;
            this.dgvRecortesDisponibles.ThemeStyle.HeaderStyle.HeaightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.EnableResizing;
            this.dgvRecortesDisponibles.ThemeStyle.HeaderStyle.Height = 34;
            this.dgvRecortesDisponibles.ThemeStyle.ReadOnly = true;
            this.dgvRecortesDisponibles.ThemeStyle.RowsStyle.BackColor = System.Drawing.Color.FromArgb(29, 38, 35);
            this.dgvRecortesDisponibles.ThemeStyle.RowsStyle.BorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            this.dgvRecortesDisponibles.ThemeStyle.RowsStyle.Font = new System.Drawing.Font("Bahnschrift SemiLight", 12F);
            this.dgvRecortesDisponibles.ThemeStyle.RowsStyle.ForeColor = System.Drawing.Color.White;
            this.dgvRecortesDisponibles.ThemeStyle.RowsStyle.Height = 30;
            this.dgvRecortesDisponibles.ThemeStyle.RowsStyle.SelectionBackColor = System.Drawing.Color.FromArgb(35, 110, 65);
            this.dgvRecortesDisponibles.ThemeStyle.RowsStyle.SelectionForeColor = System.Drawing.Color.White;

            // ── pnlCantidad ──────────────────────────────────────────────
            this.pnlCantidad.BackColor = System.Drawing.Color.FromArgb(35, 50, 40);
            this.pnlCantidad.Location = new System.Drawing.Point(20, 450);
            this.pnlCantidad.Name = "pnlCantidad";
            this.pnlCantidad.Size = new System.Drawing.Size(560, 55);
            this.pnlCantidad.TabIndex = 8;

            // ── label2 (dentro de pnlCantidad) ───────────────────────────
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Bahnschrift", 12F);
            this.label2.ForeColor = System.Drawing.Color.FromArgb(180, 220, 190);
            this.label2.Location = new System.Drawing.Point(12, 16);
            this.label2.Name = "label2";
            this.label2.TabIndex = 0;
            this.label2.Text = "Cantidad a mover:";

            // ── txtCantidad (dentro de pnlCantidad) ──────────────────────
            this.txtCantidad.BorderColor = System.Drawing.Color.FromArgb(60, 90, 70);
            this.txtCantidad.BorderThickness = 1;
            this.txtCantidad.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txtCantidad.DefaultText = "1";
            this.txtCantidad.FillColor = System.Drawing.Color.FromArgb(23, 35, 28);
            this.txtCantidad.FocusedState.BorderColor = System.Drawing.Color.FromArgb(25, 135, 84);
            this.txtCantidad.Font = new System.Drawing.Font("Bahnschrift", 14F);
            this.txtCantidad.ForeColor = System.Drawing.Color.White;
            this.txtCantidad.HoverState.BorderColor = System.Drawing.Color.FromArgb(60, 140, 90);
            this.txtCantidad.Location = new System.Drawing.Point(175, 10);
            this.txtCantidad.Name = "txtCantidad";
            this.txtCantidad.PasswordChar = '\0';
            this.txtCantidad.PlaceholderText = "1";
            this.txtCantidad.SelectedText = "";
            this.txtCantidad.Size = new System.Drawing.Size(80, 36);
            this.txtCantidad.TabIndex = 1;
            this.txtCantidad.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;

            // ── btnSumarTodo (dentro de pnlCantidad) ─────────────────────
            this.btnSumarTodo.BorderRadius = 0;
            this.btnSumarTodo.FillColor = System.Drawing.Color.FromArgb(50, 75, 55);
            this.btnSumarTodo.Font = new System.Drawing.Font("Bahnschrift Light", 11F);
            this.btnSumarTodo.ForeColor = System.Drawing.Color.FromArgb(180, 230, 190);
            this.btnSumarTodo.Location = new System.Drawing.Point(268, 10);
            this.btnSumarTodo.Name = "btnSumarTodo";
            this.btnSumarTodo.Size = new System.Drawing.Size(280, 36);
            this.btnSumarTodo.TabIndex = 2;
            this.btnSumarTodo.Text = "CARGAR STOCK COMPLETO";
            this.btnSumarTodo.Click += new System.EventHandler(this.btnSumarTodo_Click);

            this.pnlCantidad.Controls.Add(this.label2);
            this.pnlCantidad.Controls.Add(this.txtCantidad);
            this.pnlCantidad.Controls.Add(this.btnSumarTodo);

            // ── btnCancelar ──────────────────────────────────────────────
            this.btnCancelar.BorderRadius = 0;
            this.btnCancelar.FillColor = System.Drawing.Color.FromArgb(60, 60, 60);
            this.btnCancelar.Font = new System.Drawing.Font("Bahnschrift Light", 13F);
            this.btnCancelar.ForeColor = System.Drawing.Color.White;
            this.btnCancelar.Location = new System.Drawing.Point(20, 518);
            this.btnCancelar.Name = "btnCancelar";
            this.btnCancelar.Size = new System.Drawing.Size(270, 50);
            this.btnCancelar.TabIndex = 9;
            this.btnCancelar.Text = "CANCELAR";
            this.btnCancelar.Click += new System.EventHandler(this.btnCancelar_Click);

            // ── btnSeleccionarRecorte ────────────────────────────────────
            this.btnSeleccionarRecorte.BorderRadius = 0;
            this.btnSeleccionarRecorte.FillColor = System.Drawing.Color.FromArgb(25, 135, 84);
            this.btnSeleccionarRecorte.Font = new System.Drawing.Font("Bahnschrift SemiLight", 13F);
            this.btnSeleccionarRecorte.ForeColor = System.Drawing.Color.White;
            this.btnSeleccionarRecorte.Location = new System.Drawing.Point(310, 518);
            this.btnSeleccionarRecorte.Name = "btnSeleccionarRecorte";
            this.btnSeleccionarRecorte.Size = new System.Drawing.Size(270, 50);
            this.btnSeleccionarRecorte.TabIndex = 10;
            this.btnSeleccionarRecorte.Text = "✓  USAR RECORTE";
            this.btnSeleccionarRecorte.Click += new System.EventHandler(this.btnSeleccionarRecorte_Click);

            // ── ModalRecortes ────────────────────────────────────────────
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(23, 30, 28);
            this.ClientSize = new System.Drawing.Size(600, 586);
            this.Controls.Add(this.btnSeleccionarRecorte);
            this.Controls.Add(this.btnCancelar);
            this.Controls.Add(this.pnlCantidad);
            this.Controls.Add(this.dgvRecortesDisponibles);
            this.Controls.Add(this.lblInfo);
            this.Controls.Add(this.btnCrearNuevo);
            this.Controls.Add(this.txtAlturaBuscada);
            this.Controls.Add(this.lblX);
            this.Controls.Add(this.txtAnchoBuscado);
            this.Controls.Add(this.lblAltura);
            this.Controls.Add(this.lblAncho);
            this.Controls.Add(this.lblFiltro);
            this.Controls.Add(this.lblMaterial);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ModalRecortes";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Selección de Recorte";
            ((System.ComponentModel.ISupportInitialize)(this.dgvRecortesDisponibles)).EndInit();
            this.pnlCantidad.ResumeLayout(false);
            this.pnlCantidad.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Label           lblMaterial;
        private System.Windows.Forms.Label           lblFiltro;
        private System.Windows.Forms.Label           lblAncho;
        private Guna.UI2.WinForms.Guna2TextBox       txtAnchoBuscado;
        private System.Windows.Forms.Label           lblX;
        private System.Windows.Forms.Label           lblAltura;
        private Guna.UI2.WinForms.Guna2TextBox       txtAlturaBuscada;
        private Guna.UI2.WinForms.Guna2Button        btnCrearNuevo;
        private System.Windows.Forms.Label           lblInfo;
        private Guna.UI2.WinForms.Guna2DataGridView  dgvRecortesDisponibles;
        private System.Windows.Forms.Panel           pnlCantidad;
        private System.Windows.Forms.Label           label2;
        private Guna.UI2.WinForms.Guna2TextBox       txtCantidad;
        private Guna.UI2.WinForms.Guna2Button        btnSumarTodo;
        private Guna.UI2.WinForms.Guna2Button        btnCancelar;
        private Guna.UI2.WinForms.Guna2Button        btnSeleccionarRecorte;
    }
}
