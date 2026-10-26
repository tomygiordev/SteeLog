using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using WP_Inventario.Datos;
using WP_Inventario.Modelos;
using WP_Inventario.Servicios;
using System.Data;

namespace WP_Inventario
{
    public class BusquedaGlobal
    {
        private readonly Form _parentForm;
        private readonly Panel _panelMaximizar;
        private readonly Action<Form> _openChildForm;
        private Guna.UI2.WinForms.Guna2TextBox _txtBusqueda;
        private Panel _panelResultados;
        private Timer _timerDebounce;
        private ComunDatos _comunDatos;

        public BusquedaGlobal(Form parentForm, Panel panelMaximizar, Action<Form> openChildForm)
        {
            _parentForm = parentForm;
            _panelMaximizar = panelMaximizar;
            _openChildForm = openChildForm;
            _comunDatos = new ComunDatos();

            CrearTextBoxBusqueda();
            CrearPanelResultados();
            ConfigurarDebounce();
            ConfigurarAtajo();
        }

        private void CrearTextBoxBusqueda()
        {
            _txtBusqueda = new Guna.UI2.WinForms.Guna2TextBox
            {
                PlaceholderText = "🔍 Buscar material, herramienta, insumo... (Ctrl+K)",
                Font = new Font("Bahnschrift Light", 10f),
                ForeColor = Color.FromArgb(200, 200, 200),
                FillColor = Color.FromArgb(25, 30, 28),
                BorderColor = Color.FromArgb(50, 60, 55),
                BorderRadius = 8,
                Size = new Size(350, 34),
                Anchor = AnchorStyles.None,
                Cursor = Cursors.IBeam,
                Visible = true
            };

            _txtBusqueda.HoverState.BorderColor = Color.FromArgb(39, 174, 96);
            _txtBusqueda.FocusedState.BorderColor = Color.FromArgb(39, 174, 96);

            // Alinear más a la derecha (dejando margen para botones de ventana)
            int margenDerecho = 200;
            _txtBusqueda.Location = new Point(
                _panelMaximizar.Width - _txtBusqueda.Width - margenDerecho,
                (_panelMaximizar.Height - _txtBusqueda.Height) / 2
            );

            _txtBusqueda.TextChanged += TxtBusqueda_TextChanged;
            _txtBusqueda.KeyDown += TxtBusqueda_KeyDown;
            _txtBusqueda.LostFocus += (s, e) =>
            {
                // Give a small delay so clicking on results works
                var timer = new Timer { Interval = 200 };
                timer.Tick += (ts, te) =>
                {
                    timer.Stop();
                    timer.Dispose();
                    if (!_txtBusqueda.Focused)
                        OcultarResultados();
                };
                timer.Start();
            };

            // Mantener alineación a la derecha en resize
            _panelMaximizar.Resize += (s, e) =>
            {
                int mDerecho = 200;
                _txtBusqueda.Location = new Point(
                    _panelMaximizar.Width - _txtBusqueda.Width - mDerecho,
                    (_panelMaximizar.Height - _txtBusqueda.Height) / 2
                );
            };

            _panelMaximizar.Controls.Add(_txtBusqueda);
            _txtBusqueda.BringToFront();
        }

        private void CrearPanelResultados()
        {
            _panelResultados = new Panel
            {
                Size = new Size(460, 0),
                BackColor = Color.FromArgb(25, 30, 28),
                Visible = false,
                AutoScroll = true
            };

            _panelResultados.MouseEnter += (s, e) =>
            {
                _cursorSobreResultados = true;
                SetScrollBarVisible(true);
            };
            _panelResultados.MouseLeave += (s, e) =>
            {
                _cursorSobreResultados = false;
                SetScrollBarVisible(false);
            };
            _panelResultados.Scroll += (s, e) =>
            {
                SetScrollBarVisible(true);
                ReiniciarTimerOcultarScroll();
            };

            _parentForm.Controls.Add(_panelResultados);
            _panelResultados.BringToFront();
        }

        [DllImport("user32.dll")]
        private static extern int ShowScrollBar(IntPtr hWnd, int wBar, int bShow);

        private const int SB_VERT = 1;

        private bool _cursorSobreResultados = false;
        private Timer _timerOcultarScroll;

        private void SetScrollBarVisible(bool visible)
        {
            if (_panelResultados != null && _panelResultados.IsHandleCreated)
                ShowScrollBar(_panelResultados.Handle, SB_VERT, visible ? 1 : 0);
        }

        private void ReiniciarTimerOcultarScroll()
        {
            if (_timerOcultarScroll != null)
            {
                _timerOcultarScroll.Stop();
                _timerOcultarScroll.Dispose();
            }
            _timerOcultarScroll = new Timer { Interval = 1500 };
            _timerOcultarScroll.Tick += (s, e) =>
            {
                _timerOcultarScroll.Stop();
                _timerOcultarScroll.Dispose();
                _timerOcultarScroll = null;
                if (!_cursorSobreResultados) SetScrollBarVisible(false);
            };
            _timerOcultarScroll.Start();
        }

        private void PosicionarPanelResultados()
        {
            // Position below the search box, centered
            var screenPoint = _txtBusqueda.PointToScreen(Point.Empty);
            var parentPoint = _parentForm.PointToClient(screenPoint);

            _panelResultados.Location = new Point(
                parentPoint.X + (_txtBusqueda.Width / 2) - (_panelResultados.Width / 2),
                parentPoint.Y + _txtBusqueda.Height + 5
            );
        }

        private void ConfigurarDebounce()
        {
            _timerDebounce = new Timer { Interval = 300 };
            _timerDebounce.Tick += (s, e) =>
            {
                _timerDebounce.Stop();
                EjecutarBusqueda(_txtBusqueda.Text.Trim());
            };
        }

        private void ConfigurarAtajo()
        {
            // Register Ctrl+K globally on the parent form
            _parentForm.KeyPreview = true;
            _parentForm.KeyDown += (s, e) =>
            {
                if (e.Control && e.KeyCode == Keys.K)
                {
                    e.SuppressKeyPress = true;
                    _txtBusqueda.Focus();
                    _txtBusqueda.SelectAll();
                }
                if (e.KeyCode == Keys.Escape && _txtBusqueda.Focused)
                {
                    e.SuppressKeyPress = true;
                    OcultarResultados();
                    _txtBusqueda.Text = "";
                    _parentForm.ActiveControl = null;
                }
            };
        }

        private void TxtBusqueda_TextChanged(object sender, EventArgs e)
        {
            _timerDebounce.Stop();
            if (string.IsNullOrWhiteSpace(_txtBusqueda.Text))
            {
                OcultarResultados();
                return;
            }
            _timerDebounce.Start();
        }

        private void TxtBusqueda_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                OcultarResultados();
                _txtBusqueda.Text = "";
                _parentForm.ActiveControl = null;
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Down && _panelResultados.Visible && _panelResultados.Controls.Count > 0)
            {
                _panelResultados.Controls[0].Focus();
                e.SuppressKeyPress = true;
            }
        }

        private void EjecutarBusqueda(string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2) return;

            List<ResultadoBusqueda> resultados;
            try
            {
                var materiales = new MaterialDatos().obtenerMateriales() ?? new List<Material>();
                var recortes = new RecorteDatos().ObtenerRecortes() ?? new List<Recorte>();
                var herramientas = new HerramientaDatos().GetHerramientas() ?? new List<Herramienta>();
                var insumos = new InsumoDatos().GetInsumos() ?? new List<Insumo>();

                var candidatos = new List<(ResultadoBusqueda item, int estricto, int fuzzy)>();

                foreach (var m in materiales)
                {
                    string textoBusqueda = $"material materiales {m.nombre} {m.nombre_categoria}";
                    var puntajes = CalcularPuntajes(query, m.nombre, textoBusqueda);
                    if (puntajes.estricto > 0 || puntajes.fuzzy > 0)
                    {
                        bool ocultarCantidades = SessionManager.DebeOcultarCantidadesStockMateriales;
                        candidatos.Add((new ResultadoBusqueda
                        {
                            Nombre = m.nombre,
                            Tipo = "Material",
                            Extra = ocultarCantidades
                                ? SessionManager.ObtenerEstadoStockMaterial(m.stockActual)
                                : $"Stock: {m.stockActual:N0}",
                            Detalles = ocultarCantidades
                                ? $"Categoría: {m.nombre_categoria}\nDisponibilidad: {SessionManager.ObtenerEstadoStockMaterial(m.stockActual)}"
                                : $"Categoría: {m.nombre_categoria}\nStock Actual: {m.stockActual:N0}\nStock Mínimo: {m.stockMinimo:N0}",
                            Icono = "📦",
                            Color = Color.FromArgb(39, 174, 96)
                        }, puntajes.estricto, puntajes.fuzzy));
                    }
                }

                foreach (var r in recortes)
                {
                    string dimensiones = FormatearDimensiones(r);
                    if (r.ancho > 0) dimensiones += " " + r.ancho;
                    if (r.altura > 0) dimensiones += " " + r.altura;
                    if (r.medida > 0) dimensiones += " " + r.medida.ToString("0.##");
                    string textoBusqueda = $"recorte recortes {r.nombre_material} {r.nombre_categoria} {dimensiones}";
                    var puntajes = CalcularPuntajes(query, r.nombre_material, textoBusqueda);
                    if (puntajes.estricto > 0 || puntajes.fuzzy > 0)
                    {
                        string dimensionesDisplay = FormatearDimensiones(r);
                        candidatos.Add((new ResultadoBusqueda
                        {
                            Nombre = r.nombre_material,
                            Tipo = "Recorte",
                            Extra = $"{dimensionesDisplay} • Cantidad: {r.cantidad:N0}",
                            Detalles = $"Material: {r.nombre_material}\nCategoría: {r.nombre_categoria}\nDimensiones: {dimensionesDisplay}\nCantidad: {r.cantidad:N0}",
                            Icono = "✂️",
                            Color = Color.Orange
                        }, puntajes.estricto, puntajes.fuzzy));
                    }
                }

                foreach (var h in herramientas)
                {
                    string textoBusqueda = $"herramienta herramientas {h.nombre} {h.estado} {h.nombre_obra} {h.observaciones}";
                    var puntajes = CalcularPuntajes(query, h.nombre, textoBusqueda);
                    if (puntajes.estricto > 0 || puntajes.fuzzy > 0)
                    {
                        candidatos.Add((new ResultadoBusqueda
                        {
                            Nombre = h.nombre,
                            Tipo = "Herramienta",
                            Extra = h.estado + (string.IsNullOrEmpty(h.nombre_obra) ? "" : $" | {h.nombre_obra}"),
                            Detalles = $"Estado: {h.estado}\nUbicación actual: {(string.IsNullOrEmpty(h.nombre_obra) ? "Galpón" : h.nombre_obra)}\nObservaciones: {(string.IsNullOrEmpty(h.observaciones) ? "Ninguna" : h.observaciones)}",
                            Icono = "🔧",
                            Color = Color.FromArgb(52, 152, 219)
                        }, puntajes.estricto, puntajes.fuzzy));
                    }
                }

                foreach (var ins in insumos)
                {
                    string textoBusqueda = $"insumo insumos {ins.nombre} {ins.nombre_obra} {ins.observaciones}";
                    var puntajes = CalcularPuntajes(query, ins.nombre, textoBusqueda);
                    if (puntajes.estricto > 0 || puntajes.fuzzy > 0)
                    {
                        string estadoInsumo = ins.consumido ? "Consumido" : (string.IsNullOrEmpty(ins.nombre_obra) ? "Disponible" : "En Obra");
                        candidatos.Add((new ResultadoBusqueda
                        {
                            Nombre = ins.nombre,
                            Tipo = "Insumo",
                            Extra = ins.consumido ? "Consumido" : (ins.nombre_obra ?? "Galpón"),
                            Detalles = $"Estado: {estadoInsumo}\nUbicación: {(string.IsNullOrEmpty(ins.nombre_obra) ? "Galpón" : ins.nombre_obra)}",
                            Icono = "🧱",
                            Color = Color.FromArgb(243, 156, 18)
                        }, puntajes.estricto, puntajes.fuzzy));
                    }
                }

                bool hayCoincidenciaEstricta = candidatos.Any(c => c.estricto > 0);
                resultados = new List<ResultadoBusqueda>();
                if (hayCoincidenciaEstricta)
                {
                    foreach (var c in candidatos)
                    {
                        if (c.estricto > 0)
                        {
                            c.item.Puntaje = c.estricto;
                            resultados.Add(c.item);
                        }
                    }
                }
                else
                {
                    foreach (var c in candidatos)
                    {
                        if (c.fuzzy > 0)
                        {
                            c.item.Puntaje = c.fuzzy;
                            resultados.Add(c.item);
                        }
                    }
                }
            }
            catch
            {
                resultados = new List<ResultadoBusqueda>();
            }

            MostrarResultados(resultados.OrderByDescending(r => r.Puntaje).ThenBy(r => r.Nombre).ToList(), query);
        }

        private static (int estricto, int fuzzy) CalcularPuntajes(string query, string nombre, string textoCompleto)
        {
            string nombreNormalizado = NormalizarTexto(nombre);
            string textoNormalizado = NormalizarTexto(textoCompleto);
            var terminos = ObtenerTerminos(query);
            if (terminos.Length == 0) return (0, 0);

            int puntajeEstricto = 0;
            bool estrictoPasa = true;
            foreach (var termino in terminos)
            {
                if (textoNormalizado.Contains(termino))
                {
                    if (nombreNormalizado == termino) puntajeEstricto += 100;
                    else if (nombreNormalizado.StartsWith(termino)) puntajeEstricto += 80;
                    else if (nombreNormalizado.Contains(termino)) puntajeEstricto += 60;
                    else puntajeEstricto += 40;
                }
                else
                {
                    estrictoPasa = false;
                    break;
                }
            }
            if (estrictoPasa) return (puntajeEstricto, 0);

            int puntajeFuzzy = 0;
            foreach (var termino in terminos)
            {
                int scoreTermino = PuntajeFuzzyTermino(termino, nombreNormalizado, textoNormalizado);
                if (scoreTermino == 0) return (0, 0);
                puntajeFuzzy += scoreTermino;
            }
            return (0, puntajeFuzzy);
        }

        private static int PuntajeFuzzyTermino(string termino, string nombreNorm, string textoNorm)
        {
            if (termino.Length == 0) return 0;

            if (EsSubsecuencia(termino, nombreNorm)) return 30;
            if (EsSubsecuencia(termino, textoNorm)) return 15;

            int tolerancia = termino.Length >= 8 ? 2 : (termino.Length >= 4 ? 1 : 0);
            if (tolerancia == 0) return 0;

            int distNombre = DistanciaMinimaPalabra(termino, nombreNorm);
            if (distNombre >= 0 && distNombre <= tolerancia) return 20;

            int distTexto = DistanciaMinimaPalabra(termino, textoNorm);
            if (distTexto >= 0 && distTexto <= tolerancia) return 10;

            return 0;
        }

        private static bool EsSubsecuencia(string termino, string texto)
        {
            if (termino.Length == 0) return true;
            if (termino.Length > texto.Length) return false;
            int idx = 0;
            for (int i = 0; i < texto.Length && idx < termino.Length; i++)
            {
                if (texto[i] == termino[idx]) idx++;
            }
            return idx == termino.Length;
        }

        private static int DistanciaMinimaPalabra(string termino, string texto)
        {
            if (string.IsNullOrEmpty(texto)) return -1;
            int mejor = int.MaxValue;
            int inicio = 0;
            for (int i = 0; i <= texto.Length; i++)
            {
                if (i == texto.Length || texto[i] == ' ')
                {
                    if (i > inicio)
                    {
                        int dist = Levenshtein(termino, texto.Substring(inicio, i - inicio));
                        if (dist < mejor) mejor = dist;
                        if (mejor == 0) return 0;
                    }
                    inicio = i + 1;
                }
            }
            return mejor == int.MaxValue ? -1 : mejor;
        }

        private static int Levenshtein(string a, string b)
        {
            if (a.Length == 0) return b.Length;
            if (b.Length == 0) return a.Length;

            int[] prev = new int[b.Length + 1];
            int[] curr = new int[b.Length + 1];
            for (int j = 0; j <= b.Length; j++) prev[j] = j;

            for (int i = 1; i <= a.Length; i++)
            {
                curr[0] = i;
                for (int j = 1; j <= b.Length; j++)
                {
                    int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    int del = prev[j] + 1;
                    int ins = curr[j - 1] + 1;
                    int sub = prev[j - 1] + cost;
                    curr[j] = del < ins ? (del < sub ? del : sub) : (ins < sub ? ins : sub);
                }
                var tmp = prev; prev = curr; curr = tmp;
            }
            return prev[b.Length];
        }

        private static string[] ObtenerTerminos(string texto)
        {
            return NormalizarTexto(texto)
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Distinct()
                .ToArray();
        }

        private static string FormatearDimensiones(Recorte r)
        {
            if (r.ancho > 0 && r.altura > 0)
                return $"{r.ancho} x {r.altura}";
            if (r.medida > 0)
                return r.medida.ToString("0.##");
            if (r.ancho > 0)
                return r.ancho.ToString("0.##");
            if (r.altura > 0)
                return r.altura.ToString("0.##");
            return "-";
        }

        private static string NormalizarTexto(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return string.Empty;

            string descompuesto = texto.ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var resultado = new StringBuilder(descompuesto.Length);
            bool espacioPendiente = false;

            foreach (char caracter in descompuesto)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(caracter) == UnicodeCategory.NonSpacingMark)
                    continue;

                if (char.IsLetterOrDigit(caracter))
                {
                    if (espacioPendiente && resultado.Length > 0)
                        resultado.Append(' ');
                    resultado.Append(caracter);
                    espacioPendiente = false;
                }
                else
                {
                    espacioPendiente = true;
                }
            }

            return resultado.ToString();
        }

        private void MostrarResultados(List<ResultadoBusqueda> resultados, string query)
        {
            _panelResultados.Controls.Clear();

            if (resultados.Count == 0)
            {
                // No results message
                var lblNoResults = new Label
                {
                    Text = $"No se encontraron resultados para \"{query}\"",
                    Font = new Font("Bahnschrift Light", 10f),
                    ForeColor = Color.FromArgb(120, 120, 120),
                    Size = new Size(440, 45),
                    TextAlign = ContentAlignment.MiddleCenter,
                    BackColor = Color.Transparent,
                    Dock = DockStyle.Top
                };
                _panelResultados.Controls.Add(lblNoResults);
                int height = 50;
                _panelResultados.Size = new Size(460, height);
                PosicionarPanelResultados();
                _panelResultados.Visible = true;
                return;
            }

            int nombresDistintos = resultados.Select(r => r.Nombre).Distinct().Count();
            bool modoExpandido = nombresDistintos <= 2 && resultados.Count > 8;
            int cap = modoExpandido ? 50 : 8;
            int maxHeight = modoExpandido ? 620 : 400;

            var limitedResults = resultados.Take(cap).ToList();
            int itemHeight = 48;
            int totalHeight = Math.Min(limitedResults.Count * itemHeight + 10, maxHeight);

            if (resultados.Count > cap)
            {
                var lblMore = new Label
                {
                    Text = $"  Mostrando {cap} de {resultados.Count} resultados",
                    Font = new Font("Bahnschrift Light", 8.5f),
                    ForeColor = Color.FromArgb(100, 100, 100),
                    Size = new Size(440, 20),
                    TextAlign = ContentAlignment.MiddleLeft,
                    BackColor = Color.Transparent,
                    Dock = DockStyle.Top
                };
                _panelResultados.Controls.Add(lblMore);
                totalHeight = Math.Min(totalHeight + 20, maxHeight);
            }

            // Add items in reverse so Dock = Top stacks correctly
            for (int i = limitedResults.Count - 1; i >= 0; i--)
            {
                var item = limitedResults[i];
                var panel = CrearItemResultado(item);
                _panelResultados.Controls.Add(panel);
            }

            _panelResultados.Size = new Size(460, totalHeight);
            PosicionarPanelResultados();
            if (!_cursorSobreResultados) SetScrollBarVisible(false);
            _panelResultados.Visible = true;
        }

        private Panel CrearItemResultado(ResultadoBusqueda item)
        {
            var panel = new Panel
            {
                Size = new Size(440, 48),
                BackColor = Color.FromArgb(25, 30, 28),
                Dock = DockStyle.Top,
                Cursor = Cursors.Hand,
                Padding = new Padding(10, 0, 10, 0)
            };

            // Accent bar left
            var accentBar = new Panel
            {
                Size = new Size(3, 30),
                BackColor = item.Color,
                Location = new Point(5, 9)
            };

            // Icon
            var lblIcon = new Label
            {
                Text = item.Icono,
                Font = new Font("Segoe UI", 14f),
                Size = new Size(35, 48),
                Location = new Point(15, 0),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };

            // Name
            var lblName = new Label
            {
                Text = item.Nombre,
                Font = new Font("Bahnschrift", 11f),
                ForeColor = Color.FromArgb(220, 220, 220),
                Size = new Size(380, 28),
                Location = new Point(55, 3),
                TextAlign = ContentAlignment.BottomLeft,
                BackColor = Color.Transparent,
                AutoEllipsis = true
            };

            // Type badge
            string badgeText = item.Tipo;
            if (!string.IsNullOrEmpty(item.Extra))
                badgeText += $" • {item.Extra}";

            var lblType = new Label
            {
                Text = badgeText,
                Font = new Font("Bahnschrift Light", 8.5f),
                ForeColor = item.Color,
                Size = new Size(380, 18),
                Location = new Point(55, 28),
                TextAlign = ContentAlignment.TopLeft,
                BackColor = Color.Transparent
            };

            // Hover effect
            var allControls = new Control[] { panel, lblIcon, lblName, lblType, accentBar };
            foreach (var ctrl in allControls)
            {
                ctrl.MouseEnter += (s, e) => panel.BackColor = Color.FromArgb(35, 45, 40);
                ctrl.MouseLeave += (s, e) => panel.BackColor = Color.FromArgb(25, 30, 28);
                ctrl.Click += (s, e) => ItemClicked(item);
            }

            panel.Controls.Add(accentBar);
            panel.Controls.Add(lblIcon);
            panel.Controls.Add(lblName);
            panel.Controls.Add(lblType);

            return panel;
        }

        private void ItemClicked(ResultadoBusqueda item)
        {
            OcultarResultados();
            _txtBusqueda.Text = "";

            // Show Toast with details
            ToastType type = ToastType.Info;
            if (item.Tipo == "Material") type = ToastType.Success;
            if (item.Tipo == "Insumo") type = ToastType.Warning;

            ToastNotification.ShowCenterToast($"[{item.Tipo}] {item.Nombre}", item.Detalles, type);
        }

        private void OcultarResultados()
        {
            _panelResultados.Visible = false;
            _panelResultados.Controls.Clear();
        }

        private class ResultadoBusqueda
        {
            public string Nombre { get; set; }
            public string Tipo { get; set; }
            public string Extra { get; set; }
            public string Detalles { get; set; }
            public string Icono { get; set; }
            public Color Color { get; set; }
            public int Puntaje { get; set; }
        }
    }
}
