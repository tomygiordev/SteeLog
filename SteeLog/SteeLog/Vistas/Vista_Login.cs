using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using SteeLog.Servicios;

namespace SteeLog.Vistas
{
    public class Vista_Login : Form
    {
        private readonly Func<string, string, Task<AuthResult>> authenticate;
        private bool loginInProgress;
        private bool loginClosed;
        private static readonly Color FocusColor = Color.FromArgb(169, 192, 177);
        private Panel panelCard;
        private PictureBox picLogo;
        private Label lblSubtitulo;
        private Label lblUsuario;
        private Label lblPassword;
        private TextBox txtEmail;
        private TextBox txtPassword;
        private Guna.UI2.WinForms.Guna2Button btnLogin;
        private Label lblError;
        private Label lblVersion;

        public Vista_Login() : this(SupabaseAuthService.SignInAsync) { }

        internal Vista_Login(Func<string, string, Task<AuthResult>> authenticate)
        {
            this.authenticate = authenticate ?? throw new ArgumentNullException(nameof(authenticate));
            InitUI();
        }

        protected override bool ProcessDialogKey(Keys keyData)
        {
            if (loginInProgress && (keyData & Keys.KeyCode) == Keys.Enter) return true;
            return base.ProcessDialogKey(keyData);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            loginClosed = true;
            base.OnFormClosed(e);
        }

        private void InitUI()
        {
            this.Text = "SteeLog — Iniciar sesión";
            this.Icon = Properties.Resources.SteeLogIcon;

            this.Size = new Size(420, 520);
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(11, 7, 17);


            // Card central
            panelCard = new Panel
            {
                Size = new Size(340, 400),
                BackColor = Color.FromArgb(22, 28, 25),
                TabIndex = 0,
                Location = new Point(40, 60)
            };

            picLogo = new PictureBox
            {
                Image = Properties.Resources.SteeLogLogoLight,
                Location = new Point(40, 20),
                Size = new Size(260, 48),
                SizeMode = PictureBoxSizeMode.Zoom,
                TabStop = false,
                AccessibleName = "SteeLog"
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
                BorderStyle = BorderStyle.FixedSingle,
                AccessibleName = "Usuario",
                TabIndex = 0
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
                UseSystemPasswordChar = true,
                AccessibleName = "Contraseña",
                TabIndex = 1
            };

            lblError = new Label
            {
                Text = "",
                Font = new Font("Bahnschrift Light", 9.5F),
                ForeColor = Color.FromArgb(240, 128, 128),
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
                ForeColor = Color.FromArgb(22, 28, 25),
                DisabledState = { FillColor = Color.FromArgb(49, 91, 67), ForeColor = Color.White },
                AccessibleName = "Ingresar",
                TabIndex = 2,
                Font = new Font("Bahnschrift Light", 12F),
                Location = new Point(30, 290),
                Size = new Size(280, 42),
                BorderRadius = 4
            };
            btnLogin.Click += BtnLogin_Click;
            this.AcceptButton = btnLogin;

            lblVersion = new Label
            {
                Text = "v" + AppVersion.Current,
                Font = new Font("Bahnschrift Light", 8.5F),
                ForeColor = Color.FromArgb(169, 192, 177),
                Location = new Point(0, 370),
                Size = new Size(340, 18),
                TextAlign = ContentAlignment.MiddleCenter
            };

            panelCard.Controls.AddRange(new Control[] {
                picLogo, lblSubtitulo,
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
                ForeColor = Color.FromArgb(169, 192, 177),
                AccessibleName = "Cerrar aplicación",
                TabIndex = 1,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 10F),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(40, 58, 48);
            btnClose.Paint += (sender, e) =>
            {
                if (btnClose.Focused)
                    using (var pen = new Pen(FocusColor, 2))
                        e.Graphics.DrawRectangle(pen, 1, 1, btnClose.Width - 3, btnClose.Height - 3);
            };
            btnClose.Enter += (sender, e) => btnClose.Invalidate();
            btnClose.Leave += (sender, e) => btnClose.Invalidate();
            btnClose.Click += (s, e) => Application.Exit();
            this.Controls.Add(btnClose);
            panelCard.Paint += (sender, e) =>
            {
                foreach (Control control in new Control[] { txtEmail, txtPassword, btnLogin })
                {
                    if (!(control is TextBox) && !control.Focused) continue;
                    var bounds = control.Bounds;
                    bounds.Inflate(3, 3);
                    using (var pen = new Pen(control.Focused ? Color.White : FocusColor, control.Focused ? 2 : 1))
                        e.Graphics.DrawRectangle(pen, bounds);
                }
            };
            foreach (Control control in new Control[] { txtEmail, txtPassword, btnLogin })
            {
                control.Enter += (sender, e) => panelCard.Invalidate();
                control.Leave += (sender, e) => panelCard.Invalidate();
            }
        }

        private async void BtnLogin_Click(object sender, EventArgs e)
        {
            if (loginInProgress || loginClosed || IsDisposed || Disposing) return;
            loginInProgress = true;
            bool authenticationStarted = false;
            try
            {
                string email = txtEmail.Text.Trim();
                string password = txtPassword.Text;
                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                {
                    MostrarError("Completá usuario y contraseña.");
                    return;
                }
                if (!email.Contains("@"))
                {
                    string norm = email.ToLower();
                    if (norm == "admin") email = "admin@tesis.local";
                    else if (norm == "galpon" || norm == "operador") email = "galpon@tesis.local";
                }
                authenticationStarted = true;
                btnLogin.Enabled = false;
                btnLogin.Text = "Verificando...";
                lblError.Visible = false;
                var result = await authenticate(email, password);
                if (loginClosed || IsDisposed || Disposing) return;
                if (result.Success)
                {
                    SessionManager.SetSession(result.Email, result.AccessToken, result.RefreshToken, result.ExpiresIn);
                    CredentialStore.Save(email, password);
                    DialogResult = DialogResult.OK;
                    Close();
                }
                else MostrarError(result.ErrorMessage);
            }
            catch (Exception ex)
            {
                if (!loginClosed && !IsDisposed && !Disposing)
                    MostrarError("Error de conexión: " + ex.Message);
            }
            finally
            {
                loginInProgress = false;
                if (!loginClosed && !IsDisposed && !Disposing)
                {
                    btnLogin.Enabled = true;
                    btnLogin.Text = "Ingresar";
                    if (authenticationStarted && lblError.Visible && ContainsFocus)
                        txtPassword.Focus();
                }
            }
        }


        private void MostrarError(string mensaje)
        {
            lblError.Text = mensaje;
            lblError.Visible = true;
        }
    }
}
