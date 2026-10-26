using System;
using System.Drawing;
using System.Windows.Forms;
using WP_Inventario.Servicios;

namespace WP_Inventario.Vistas
{
    public class Vista_Login : Form
    {
        private Panel panelCard;
        private Label lblTitulo;
        private Label lblSubtitulo;
        private Label lblUsuario;
        private Label lblPassword;
        private TextBox txtEmail;
        private TextBox txtPassword;
        private Guna.UI2.WinForms.Guna2Button btnLogin;
        private Label lblError;
        private Label lblVersion;

        public Vista_Login()
        {
            InitUI();
        }

        private void InitUI()
        {
            this.Text = "SteeLog — Iniciar sesión";
            this.Size = new Size(420, 520);
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(11, 7, 17);
            this.KeyPreview = true;
            this.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) BtnLogin_Click(null, null); };

            // Card central
            panelCard = new Panel
            {
                Size = new Size(340, 400),
                BackColor = Color.FromArgb(22, 28, 25),
                Location = new Point(40, 60)
            };

            lblTitulo = new Label
            {
                Text = "STEELOG",
                Font = new Font("Bahnschrift SemiBold", 18F),
                ForeColor = Color.FromArgb(39, 174, 96),
                Location = new Point(0, 30),
                Size = new Size(340, 34),
                TextAlign = ContentAlignment.MiddleCenter
            };

            lblSubtitulo = new Label
            {
                Text = "Iniciar sesión",
                Font = new Font("Bahnschrift Light", 11F),
                ForeColor = Color.FromArgb(120, 150, 130),
                Location = new Point(0, 68),
                Size = new Size(340, 22),
                TextAlign = ContentAlignment.MiddleCenter
            };

            lblUsuario = new Label
            {
                Text = "Usuario",
                Font = new Font("Bahnschrift Light", 10F),
                ForeColor = Color.FromArgb(155, 185, 165),
                Location = new Point(30, 110),
                Size = new Size(280, 18)
            };

            txtEmail = new TextBox
            {
                Location = new Point(30, 132),
                Size = new Size(280, 28),
                Font = new Font("Segoe UI", 11F),
                BackColor = Color.FromArgb(35, 45, 40),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            lblPassword = new Label
            {
                Text = "Contraseña",
                Font = new Font("Bahnschrift Light", 10F),
                ForeColor = Color.FromArgb(155, 185, 165),
                Location = new Point(30, 178),
                Size = new Size(280, 18)
            };

            txtPassword = new TextBox
            {
                Location = new Point(30, 200),
                Size = new Size(280, 28),
                Font = new Font("Segoe UI", 11F),
                BackColor = Color.FromArgb(35, 45, 40),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                UseSystemPasswordChar = true
            };

            lblError = new Label
            {
                Text = "",
                Font = new Font("Bahnschrift Light", 9.5F),
                ForeColor = Color.FromArgb(220, 80, 80),
                Location = new Point(30, 242),
                Size = new Size(280, 36),
                TextAlign = ContentAlignment.MiddleCenter,
                Visible = false
            };

            btnLogin = new Guna.UI2.WinForms.Guna2Button
            {
                Text = "Ingresar",
                FillColor = Color.FromArgb(39, 174, 96),
                HoverState = { FillColor = Color.FromArgb(46, 196, 110) },
                ForeColor = Color.White,
                Font = new Font("Bahnschrift Light", 12F),
                Location = new Point(30, 290),
                Size = new Size(280, 42),
                BorderRadius = 4
            };
            btnLogin.Click += BtnLogin_Click;

            lblVersion = new Label
            {
                Text = "v" + AppVersion.Current,
                Font = new Font("Bahnschrift Light", 8.5F),
                ForeColor = Color.FromArgb(60, 80, 68),
                Location = new Point(0, 370),
                Size = new Size(340, 18),
                TextAlign = ContentAlignment.MiddleCenter
            };

            panelCard.Controls.AddRange(new Control[] {
                lblTitulo, lblSubtitulo,
                lblUsuario, txtEmail,
                lblPassword, txtPassword,
                lblError, btnLogin, lblVersion
            });

            this.Controls.Add(panelCard);

            // Botón cerrar
            var btnClose = new Button
            {
                Text = "✕",
                Size = new Size(30, 30),
                Location = new Point(this.Width - 40, 8),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(100, 100, 100),
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 10F),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => Application.Exit();
            this.Controls.Add(btnClose);
        }

        private async void BtnLogin_Click(object sender, EventArgs e)
        {
            string email = txtEmail.Text.Trim();
            string password = txtPassword.Text;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                MostrarError("Completá usuario y contraseña.");
                return;
            }

            // Normalizar usuarios locales de demostración para tesis
            if (!email.Contains("@"))
            {
                string norm = email.ToLower();
                email = (norm == "galpon" || norm == "operador") ? norm + "@tesis.local" : (norm == "admin" ? "admin@tesis.local" : email + "@tesis.local");
            }

            btnLogin.Enabled = false;
            btnLogin.Text = "Verificando...";
            lblError.Visible = false;

            try
            {
                var result = await SupabaseAuthService.SignInAsync(email, password);

                if (result.Success)
                {
                    SessionManager.SetSession(result.Email, result.AccessToken, result.RefreshToken, result.ExpiresIn);
                    CredentialStore.Save(email, password);
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    MostrarError(result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                MostrarError("Error de conexión: " + ex.Message);
            }
            finally
            {
                btnLogin.Enabled = true;
                btnLogin.Text = "Ingresar";
            }
        }

        private void MostrarError(string mensaje)
        {
            lblError.Text = mensaje;
            lblError.Visible = true;
        }
    }
}
