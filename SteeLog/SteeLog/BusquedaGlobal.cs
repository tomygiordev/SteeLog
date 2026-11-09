using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using SteeLog.Core.Application.Inventario;
using SteeLog.Core.Application.Activos;
using SteeLog.Presentation;
using SteeLog.Servicios;
using System.Data;

namespace SteeLog
{
    public class BusquedaGlobal : IDisposable
    {
        private readonly IServiciosVista servicios;
        private readonly Form _parentForm;
        private readonly Panel _panelMaximizar;
        private readonly Action<Form> _openChildForm;
        private Guna.UI2.WinForms.Guna2TextBox _txtBusqueda;
        // Fondo del panel de resultados y color del texto "sin resultados" (contraste >= 4.5:1).
        internal static readonly Color ColorFondoPanel = Color.FromArgb(25, 30, 28);
        internal static readonly Color ColorSinResultados = Color.FromArgb(200, 200, 200);
        private Panel _panelResultados;
        private Timer _timerDebounce;
        internal sealed class Snapshot
        {
            internal IReadOnlyList<MaterialInventarioLectura> Materiales;
            internal IReadOnlyList<RecorteLectura> Recortes;
            internal IReadOnlyList<HerramientaLectura> Herramientas;
            internal IReadOnlyList<InsumoLectura> Insumos;
        }
        private readonly object _cacheGate = new object();
        private readonly Func<Task<Snapshot>> _loader;
        private Task<Snapshot> _loading;
        private readonly System.Threading.CancellationTokenSource _cierre = new System.Threading.CancellationTokenSource();
        private readonly System.Threading.CancellationToken _tokenCierre;
        private Snapshot _cache;
        private DateTime _cacheTimestampUtc = DateTime.MinValue;
        private Timer _timerLostFocus;
        private bool _disposed;
        private int _searchVersion = 0;

        public BusquedaGlobal(IServiciosVista servicios, Form parentForm, Panel panelMaximizar, Action<Form> openChildForm)
            : this(servicios, parentForm, panelMaximizar, openChildForm, (Func<Task<Snapshot>>)null)
        {
        }

        internal BusquedaGlobal(IServiciosVista servicios, Form parentForm, Panel panelMaximizar, Action<Form> openChildForm, Func<Snapshot> loader)
            : this(servicios, parentForm, panelMaximizar, openChildForm, loader == null ? null : (Func<Task<Snapshot>>)(() => Task.Run(loader))) { }

        private BusquedaGlobal(IServiciosVista servicios, Form parentForm, Panel panelMaximizar, Action<Form> openChildForm, Func<Task<Snapshot>> loader)
        {
            this.servicios = servicios ?? throw new ArgumentNullException(nameof(servicios));
            _tokenCierre = _cierre.Token;
            _loader = loader ?? CargarSnapshotAsync;
            _parentForm = parentForm;
            _panelMaximizar = panelMaximizar;
            _openChildForm = openChildForm;

            CrearTextBoxBusqueda();
            CrearPanelResultados();
            ConfigurarDebounce();
            ConfigurarAtajo();
            _parentForm.Disposed += ParentDisposed;
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
            _txtBusqueda.LostFocus += TxtBusqueda_LostFocus;
            _panelMaximizar.Resize += PanelMaximizar_Resize;

            _panelMaximizar.Controls.Add(_txtBusqueda);
            _txtBusqueda.BringToFront();
        }

        private void CrearPanelResultados()
        {
            _panelResultados = new Panel
            {
                Size = new Size(460, 0),
                BackColor = ColorFondoPanel,
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
            _parentForm.KeyPreview = true;
            _parentForm.KeyDown += ParentKeyDown;
        }

        private void ParentKeyDown(object sender, KeyEventArgs e)
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
        }

        private void PanelMaximizar_Resize(object sender, EventArgs e)
        {
            _txtBusqueda.Location = new Point(_panelMaximizar.Width - _txtBusqueda.Width - 200,
                (_panelMaximizar.Height - _txtBusqueda.Height) / 2);
        }

        private void TxtBusqueda_LostFocus(object sender, EventArgs e)
        {
            ++_searchVersion;
            _timerDebounce.Stop();
            if (_timerLostFocus != null) { _timerLostFocus.Stop(); _timerLostFocus.Dispose(); }
            _timerLostFocus = new Timer { Interval = 200 };
            _timerLostFocus.Tick += (s, args) =>
            {
                _timerLostFocus.Stop();
                _timerLostFocus.Dispose();
                _timerLostFocus = null;
                if (!_txtBusqueda.Focused) OcultarResultados();
            };
            _timerLostFocus.Start();
        }

        private void TxtBusqueda_TextChanged(object sender, EventArgs e)
        {
            ++_searchVersion;
            _timerDebounce.Stop();
            if (_txtBusqueda.Text.Trim().Length < 2)
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

        private async void EjecutarBusqueda(string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2) return;

            int currentVersion = ++_searchVersion;
            bool ocultarCantidades = servicios.Sesion.DebeOcultarCantidadesStockMateriales;

            List<ResultadoBusqueda> resultados;
            try
            {
                resultados = await Task.Run(async () =>
                {
                    Snapshot snapshot = await ObtenerSnapshotAsync().ConfigureAwait(false);

                    var candidatos = new List<(ResultadoBusqueda item, int estricto, int fuzzy)>();

                    foreach (var m in snapshot.Materiales)
                    {
                        string textoBusqueda = $"material materiales {m.Nombre} {m.Categoria}";
                        var puntajes = CalcularPuntajes(query, m.Nombre, textoBusqueda);
                        if (puntajes.estricto > 0 || puntajes.fuzzy > 0)
                        {
                            candidatos.Add((new ResultadoBusqueda
                            {
                                Nombre = m.Nombre,
                                Tipo = "Material",
                                Extra = ocultarCantidades
                                    ? servicios.Sesion.ObtenerEstadoStockMaterial(m.Stock)
                                    : $"Stock: {m.Stock:N0}",
                                Detalles = ocultarCantidades
                                    ? $"Categoría: {m.Categoria}\nDisponibilidad: {servicios.Sesion.ObtenerEstadoStockMaterial(m.Stock)}"
                                    : $"Categoría: {m.Categoria}\nStock Actual: {m.Stock:N0}\nStock Mínimo: {m.Minimo:N0}",
                                Icono = "📦",
                                Color = Color.FromArgb(39, 174, 96)
                            }, puntajes.estricto, puntajes.fuzzy));
                        }
                    }

                    foreach (var r in snapshot.Recortes)
                    {
                        string dimensiones = FormatearDimensiones(r);
                        if (r.Ancho > 0) dimensiones += " " + r.Ancho;
                        if (r.Altura > 0) dimensiones += " " + r.Altura;
                        if (r.Medida > 0) dimensiones += " " + r.Medida.ToString("0.##");
                        string textoBusqueda = $"recorte recortes {r.Material} {r.Categoria} {dimensiones}";
                        var puntajes = CalcularPuntajes(query, r.Material, textoBusqueda);
                        if (puntajes.estricto > 0 || puntajes.fuzzy > 0)
                        {
                            string dimensionesDisplay = FormatearDimensiones(r);
                            candidatos.Add((new ResultadoBusqueda
                            {
                                Nombre = r.Material,
                                Tipo = "Recorte",
                                Extra = $"{dimensionesDisplay} • Cantidad: {r.Cantidad:N0}",
                                Detalles = $"Material: {r.Material}\nCategoría: {r.Categoria}\nDimensiones: {dimensionesDisplay}\nCantidad: {r.Cantidad:N0}",
                                Icono = "✂️",
                                Color = Color.Orange
                            }, puntajes.estricto, puntajes.fuzzy));
                        }
                    }

                    foreach (var h in snapshot.Herramientas)
                    {
                        string textoBusqueda = $"herramienta herramientas {h.Nombre} {h.Estado} {h.Obra} {h.Observaciones}";
                        var puntajes = CalcularPuntajes(query, h.Nombre, textoBusqueda);
                        if (puntajes.estricto > 0 || puntajes.fuzzy > 0)
                        {
                            candidatos.Add((new ResultadoBusqueda
                            {
                                Nombre = h.Nombre,
                                Tipo = "Herramienta",
                                Extra = h.Estado + (string.IsNullOrEmpty(h.Obra) ? "" : $" | {h.Obra}"),
                                Detalles = $"Estado: {h.Estado}\nUbicación actual: {(string.IsNullOrEmpty(h.Obra) ? "Galpón" : h.Obra)}\nObservaciones: {(string.IsNullOrEmpty(h.Observaciones) ? "Ninguna" : h.Observaciones)}",
                                Icono = "🔧",
                                Color = Color.FromArgb(52, 152, 219)
                            }, puntajes.estricto, puntajes.fuzzy));
                        }
                    }

                    foreach (var ins in snapshot.Insumos)
                    {
                        string textoBusqueda = $"insumo insumos {ins.Nombre} {ins.Obra} {ins.Observaciones}";
                        var puntajes = CalcularPuntajes(query, ins.Nombre, textoBusqueda);
                        if (puntajes.estricto > 0 || puntajes.fuzzy > 0)
                        {
                            string estadoInsumo = ins.Consumido ? "Consumido" : (string.IsNullOrEmpty(ins.Obra) ? "Disponible" : "En Obra");
                            candidatos.Add((new ResultadoBusqueda
                            {
                                Nombre = ins.Nombre,
                                Tipo = "Insumo",
                                Extra = ins.Consumido ? "Consumido" : (ins.Obra ?? "Galpón"),
                                Detalles = $"Estado: {estadoInsumo}\nUbicación: {(string.IsNullOrEmpty(ins.Obra) ? "Galpón" : ins.Obra)}",
                                Icono = "🧱",
                                Color = Color.FromArgb(243, 156, 18)
                            }, puntajes.estricto, puntajes.fuzzy));
                        }
                    }

                    bool hayCoincidenciaEstricta = candidatos.Any(c => c.estricto > 0);
                    var res = new List<ResultadoBusqueda>();
                    if (hayCoincidenciaEstricta)
                    {
                        foreach (var c in candidatos)
                        {
                            if (c.estricto > 0)
                            {
                                c.item.Puntaje = c.estricto;
                                res.Add(c.item);
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
                                res.Add(c.item);
                            }
                        }
                    }

                    return res.OrderByDescending(r => r.Puntaje).ThenBy(r => r.Nombre).ToList();
                });
            }
            catch
            {
                if (!_disposed && currentVersion == _searchVersion && !_parentForm.IsDisposed)
                    MostrarErrorBusqueda();
                return;
            }

            if (_disposed || currentVersion != _searchVersion || _parentForm.IsDisposed) return;

            MostrarResultados(resultados, query);
        }

        private async Task<Snapshot> CargarSnapshotAsync()
        {
            var token = _tokenCierre;
            token.ThrowIfCancellationRequested();
            var executor = servicios.Executor;
            var materiales = executor.ExecuteAsync<ConsultarMateriales, IReadOnlyList<MaterialInventarioLectura>>(x => x.ListarAsync(null, token));
            var recortes = executor.ExecuteAsync<ConsultarRecortes, IReadOnlyList<RecorteLectura>>(x => x.ListarAsync(token: token));
            var herramientas = executor.ExecuteAsync<ConsultarHerramientas, IReadOnlyList<HerramientaLectura>>(x => x.HerramientasAsync(token));
            var insumos = executor.ExecuteAsync<ConsultarInsumos, IReadOnlyList<InsumoLectura>>(x => x.InsumosAsync(token));
            await Task.WhenAll(materiales, recortes, herramientas, insumos).ConfigureAwait(false);
            return new Snapshot { Materiales = await materiales.ConfigureAwait(false), Recortes = await recortes.ConfigureAwait(false),
                Herramientas = await herramientas.ConfigureAwait(false), Insumos = await insumos.ConfigureAwait(false) };
        }

        private async Task<Snapshot> ObtenerSnapshotAsync()
        {
            Task<Snapshot> loading;
            lock (_cacheGate)
            {
                if (_cache != null && (DateTime.UtcNow - _cacheTimestampUtc).TotalSeconds <= 10) return _cache;
                loading = _loading ?? (_loading = _loader());
            }
            try
            {
                var nuevo = await loading.ConfigureAwait(false);
                if (nuevo == null || nuevo.Materiales == null || nuevo.Recortes == null || nuevo.Herramientas == null || nuevo.Insumos == null)
                    throw new InvalidOperationException("No se pudieron cargar los datos de búsqueda.");
                lock (_cacheGate)
                {
                    if (ReferenceEquals(_loading, loading)) { _cache = nuevo; _cacheTimestampUtc = DateTime.UtcNow; _loading = null; }
                }
                return nuevo;
            }
            catch
            {
                lock (_cacheGate) { if (ReferenceEquals(_loading, loading)) _loading = null; }
                throw;
            }
        }

        private void LimpiarResultados()
        {
            while (_panelResultados.Controls.Count > 0)
            {
                var control = _panelResultados.Controls[0];
                _panelResultados.Controls.RemoveAt(0);
                control.Dispose();
            }
        }

        private void MostrarErrorBusqueda()
        {
            LimpiarResultados();
            _panelResultados.Controls.Add(new Label { Text = "No se pudo realizar la búsqueda. Intente nuevamente.",
                ForeColor = Color.White, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter });
            _panelResultados.Size = new Size(460, 50);
            PosicionarPanelResultados();
            _panelResultados.Visible = true;
        }

        private void ParentDisposed(object sender, EventArgs e) { Dispose(); }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _cierre.Cancel();
            _cierre.Dispose();
            ++_searchVersion;
            _parentForm.Disposed -= ParentDisposed;
            _parentForm.KeyDown -= ParentKeyDown;
            _panelMaximizar.Resize -= PanelMaximizar_Resize;
            _txtBusqueda.TextChanged -= TxtBusqueda_TextChanged;
            _txtBusqueda.KeyDown -= TxtBusqueda_KeyDown;
            _txtBusqueda.LostFocus -= TxtBusqueda_LostFocus;
            foreach (var timer in new[] { _timerDebounce, _timerLostFocus, _timerOcultarScroll })
                if (timer != null) { timer.Stop(); timer.Dispose(); }
            _txtBusqueda.Dispose();
            _panelResultados.Dispose();
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

        private static string FormatearDimensiones(RecorteLectura r)
        {
            if (r.Ancho > 0 && r.Altura > 0)
                return $"{r.Ancho} x {r.Altura}";
            if (r.Medida > 0)
                return r.Medida.ToString("0.##");
            if (r.Ancho > 0)
                return r.Ancho.ToString("0.##");
            if (r.Altura > 0)
                return r.Altura.ToString("0.##");
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
            LimpiarResultados();

            if (resultados.Count == 0)
            {
                // No results message
                var lblNoResults = new Label
                {
                    Text = $"No se encontraron resultados para \"{query}\"",
                    Font = new Font("Bahnschrift Light", 10f),
                    ForeColor = ColorSinResultados,
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
            ++_searchVersion;
            _timerDebounce.Stop();
            _panelResultados.Visible = false;
            LimpiarResultados();
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
