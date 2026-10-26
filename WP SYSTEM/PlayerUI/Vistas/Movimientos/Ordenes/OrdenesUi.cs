using Guna.UI2.WinForms;
using System.Drawing;
using System.Windows.Forms;

namespace WP_Inventario.Vistas.Movimientos.Ordenes
{
    internal static class OrdenesUi
    {
        public static readonly Color FondoForm = Color.FromArgb(24, 29, 34);
        public static readonly Color FondoPanel = Color.FromArgb(29, 38, 35);
        public static readonly Color FondoInput = Color.FromArgb(35, 50, 40);
        public static readonly Color FondoBoton = Color.FromArgb(42, 68, 52);
        public static readonly Color Borde = Color.FromArgb(55, 75, 62);
        public static readonly Color Texto = Color.White;
        public static readonly Color TextoMuted = Color.FromArgb(155, 185, 165);
        public static readonly Color Hover = Color.FromArgb(55, 90, 68);

        public static void AplicarEstiloGrid(Guna2DataGridView grid)
        {
            grid.EnableHeadersVisualStyles = false;
            grid.BackgroundColor = FondoPanel;
            grid.BorderStyle = BorderStyle.None;
            grid.GridColor = FondoPanel;
            grid.RowHeadersVisible = false;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            grid.RowTemplate.Height = 34;
            grid.ColumnHeadersHeight = 36;
            grid.ThemeStyle.BackColor = FondoPanel;
            grid.ThemeStyle.AlternatingRowsStyle.BackColor = FondoPanel;
            grid.ThemeStyle.RowsStyle.BackColor = FondoPanel;
            grid.ThemeStyle.RowsStyle.ForeColor = Texto;
            grid.ThemeStyle.RowsStyle.SelectionBackColor = Color.FromArgb(45, 55, 52);
            grid.ThemeStyle.RowsStyle.SelectionForeColor = Texto;
            grid.ThemeStyle.HeaderStyle.BackColor = Color.FromArgb(35, 63, 43);
            grid.ThemeStyle.HeaderStyle.ForeColor = Texto;
            grid.ThemeStyle.HeaderStyle.Height = 36;
            grid.ThemeStyle.GridColor = FondoPanel;
            grid.DefaultCellStyle.BackColor = FondoPanel;
            grid.DefaultCellStyle.ForeColor = Texto;
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(45, 55, 52);
            grid.DefaultCellStyle.SelectionForeColor = Texto;
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(35, 63, 43);
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(35, 63, 43);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Texto;
        }

        public static Guna2Button CrearBoton(string texto, bool primario = false)
        {
            var boton = new Guna2Button();
            boton.Text = texto;
            boton.BorderRadius = 0;
            boton.FillColor = primario ? Color.FromArgb(39, 174, 96) : FondoBoton;
            boton.HoverState.FillColor = primario ? Color.FromArgb(52, 191, 110) : Hover;
            boton.Font = new Font("Bahnschrift SemiBold", 10.75F, FontStyle.Bold);
            boton.ForeColor = Texto;
            boton.Size = new Size(180, 36);
            return boton;
        }

        public static Guna2TextBox CrearTextBox(bool multilinea = false, bool soloLectura = false)
        {
            var txt = new Guna2TextBox();
            txt.BorderColor = Borde;
            txt.FillColor = FondoInput;
            txt.FocusedState.BorderColor = Color.FromArgb(39, 174, 96);
            txt.HoverState.BorderColor = Color.FromArgb(60, 120, 80);
            txt.ForeColor = Texto;
            txt.Font = new Font("Segoe UI", 10.5F);
            txt.Multiline = multilinea;
            txt.ReadOnly = soloLectura;
            return txt;
        }

        public static Guna2ComboBox CrearCombo()
        {
            var combo = new Guna2ComboBox();
            combo.BorderColor = Borde;
            combo.FillColor = FondoInput;
            combo.FocusedColor = Color.FromArgb(39, 174, 96);
            combo.ForeColor = Texto;
            combo.Font = new Font("Segoe UI", 10.5F);
            combo.DrawMode = DrawMode.OwnerDrawFixed;
            combo.DropDownStyle = ComboBoxStyle.DropDownList;
            combo.ItemHeight = 30;
            return combo;
        }

        public static Label CrearLabel(string texto)
        {
            return new Label
            {
                AutoSize = true,
                Text = texto,
                Font = new Font("Bahnschrift Light", 10.5F),
                ForeColor = TextoMuted,
                Margin = new Padding(3, 8, 3, 4)
            };
        }

        public static Panel CrearCard()
        {
            return new Panel
            {
                Dock = DockStyle.Top,
                Height = 180,
                Padding = new Padding(18),
                Margin = new Padding(18, 12, 18, 0),
                BackColor = FondoPanel
            };
        }
    }
}
