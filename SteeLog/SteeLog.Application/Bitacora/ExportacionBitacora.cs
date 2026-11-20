using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace SteeLog.Core.Application.Bitacora
{
    /// <summary>Exporta registros de la bitácora a texto plano (.txt) o Markdown (.md), con el detalle de cada cambio.</summary>
    public static class ExportacionBitacora
    {
        private const string FormatoFecha = "dd/MM/yyyy HH:mm:ss";

        public static string ATexto(IReadOnlyList<BitacoraRegistro> registros, DateTime generado)
        {
            if (registros == null) throw new ArgumentNullException(nameof(registros));
            var sb = new StringBuilder();
            sb.AppendLine("BITÁCORA DE ACTIVIDAD - SteeLog");
            sb.AppendLine("Generado: " + generado.ToString(FormatoFecha, CultureInfo.InvariantCulture) + " | Registros: " + registros.Count);
            sb.AppendLine(new string('=', 72));
            foreach (var r in registros)
            {
                sb.AppendLine();
                sb.AppendLine("[" + r.Fecha.ToString(FormatoFecha, CultureInfo.InvariantCulture) + "] " + Valor(r.Accion) + " - " + Valor(r.Entidad) +
                              (string.IsNullOrWhiteSpace(r.EntidadId) ? string.Empty : " #" + r.EntidadId));
                sb.AppendLine("  Usuario: " + Valor(r.Usuario) + " | Rol: " + Valor(r.Rol) + " | Equipo: " + Valor(r.Equipo) +
                              " | Versión: " + Valor(r.VersionApp) + " | Origen: " + Valor(r.Origen));
                var resumen = Linea(r.Resumen);
                if (resumen.Length > 0) sb.AppendLine("  " + resumen);
                foreach (var c in ResumenBitacora.Cambios(r))
                    sb.AppendLine("    - " + c.Campo + ": " + Valor(Linea(c.Antes)) + " -> " + Valor(Linea(c.Despues)));
            }
            return sb.ToString();
        }

        public static string AMarkdown(IReadOnlyList<BitacoraRegistro> registros, DateTime generado)
        {
            if (registros == null) throw new ArgumentNullException(nameof(registros));
            var sb = new StringBuilder();
            sb.AppendLine("# Bitácora de actividad - SteeLog");
            sb.AppendLine();
            sb.AppendLine("Generado: " + generado.ToString(FormatoFecha, CultureInfo.InvariantCulture) + " · Registros: " + registros.Count);
            sb.AppendLine();
            sb.AppendLine("| Fecha | Usuario | Rol | Equipo | Origen | Acción | Entidad | ID | Resumen |");
            sb.AppendLine("|---|---|---|---|---|---|---|---|---|");
            foreach (var r in registros)
            {
                sb.AppendLine("| " + string.Join(" | ", new[]
                {
                    r.Fecha.ToString(FormatoFecha, CultureInfo.InvariantCulture), Celda(r.Usuario), Celda(r.Rol), Celda(r.Equipo), Celda(r.Origen),
                    Celda(r.Accion), Celda(r.Entidad), Celda(r.EntidadId), Celda(r.Resumen)
                }) + " |");
            }
            var conCambios = registros.Select(r => new { r, cambios = ResumenBitacora.Cambios(r) }).Where(x => x.cambios.Count > 0).ToList();
            if (conCambios.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("## Detalle de cambios");
                foreach (var x in conCambios)
                {
                    sb.AppendLine();
                    sb.AppendLine("### " + x.r.Fecha.ToString(FormatoFecha, CultureInfo.InvariantCulture) + " · " + Celda(x.r.Accion) + " " + Celda(x.r.Entidad) +
                                  (string.IsNullOrWhiteSpace(x.r.EntidadId) ? string.Empty : " #" + Celda(x.r.EntidadId)));
                    sb.AppendLine();
                    sb.AppendLine("| Campo | Antes | Después |");
                    sb.AppendLine("|---|---|---|");
                    foreach (var c in x.cambios) sb.AppendLine("| " + Celda(c.Campo) + " | " + Celda(c.Antes) + " | " + Celda(c.Despues) + " |");
                }
            }
            return sb.ToString();
        }

        private static string Valor(string s) => string.IsNullOrWhiteSpace(s) ? "-" : s;

        private static string Linea(string s) => string.IsNullOrEmpty(s) ? string.Empty : s.Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' ').Trim();

        // Una celda de tabla Markdown: una sola línea y sin romper las columnas.
        private static string Celda(string s)
        {
            var t = Linea(s);
            if (t.Length == 0) return "-";
            return t.Replace("\\", "\\\\").Replace("|", "\\|");
        }
    }
}
