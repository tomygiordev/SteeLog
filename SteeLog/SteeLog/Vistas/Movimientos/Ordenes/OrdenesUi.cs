using Guna.UI2.WinForms;
using SteeLog.UI;
using System.Drawing;
using System.Windows.Forms;

namespace SteeLog.Vistas.Movimientos.Ordenes
{
    internal static class OrdenesUi
    {
        public static readonly Color FondoForm = SteeLog.UI.Tema.Fondo;
        public static readonly Color FondoPanel = SteeLog.UI.Tema.Superficie;
        public static readonly Color FondoInput = SteeLog.UI.Tema.FondoInput;
        public static readonly Color FondoBoton = SteeLog.UI.Tema.Secundario;
        public static readonly Color Borde = SteeLog.UI.Tema.Borde;
        public static readonly Color Texto = SteeLog.UI.Tema.Texto;
        public static readonly Color TextoMuted = SteeLog.UI.Tema.TextoSecundario;
        public static readonly Color Hover = SteeLog.UI.Tema.SecundarioHover;

        public static void AplicarEstiloGrid(Guna2DataGridView grid)
        {
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            SteeLog.UI.Tema.Grilla(grid);
        }

        public static Guna2Button CrearBoton(string texto, bool primario = false)
        {
            var boton = new Guna2Button();
            boton.Text = texto;
            boton.Size = new Size(180, SteeLog.UI.Tema.AltoControl);
            SteeLog.UI.Tema.Boton(boton, primario ? SteeLog.UI.Variante.Primario : SteeLog.UI.Variante.Secundario);
            return boton;
        }

        public static Guna2TextBox CrearTextBox(bool multilinea = false, bool soloLectura = false)
        {
            var txt = new Guna2TextBox();
            txt.Multiline = multilinea;
            txt.ReadOnly = soloLectura;
            txt.Height = SteeLog.UI.Tema.AltoControl;
            SteeLog.UI.Tema.Input(txt);
            return txt;
        }

        public static Guna2ComboBox CrearCombo()
        {
            var combo = new Guna2ComboBox();
            combo.DrawMode = DrawMode.OwnerDrawFixed;
            combo.DropDownStyle = ComboBoxStyle.DropDownList;
            combo.ItemHeight = 28;
            combo.Height = SteeLog.UI.Tema.AltoControl;
            SteeLog.UI.Tema.Combo(combo);
            return combo;
        }

        public static Label CrearLabel(string texto)
        {
            return new Label
            {
                AutoSize = true,
                Text = texto,
                Font = SteeLog.UI.Tema.FuenteTexto,
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
