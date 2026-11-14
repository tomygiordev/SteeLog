using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace SteeLog.Core.Application.Bitacora
{
    public sealed class CambioBitacora
    {
        public string Campo { get; set; }
        public string Antes { get; set; }
        public string Despues { get; set; }
    }

    /// <summary>Resumen legible y detalle campo a campo de una fila de la bitácora.</summary>
    public static class ResumenBitacora
    {
        public static string De(BitacoraRegistro r)
        {
            if (r == null) return string.Empty;
            if (!string.IsNullOrWhiteSpace(r.Descripcion)) return r.Descripcion.Replace("\r", " ").Replace("\n", " ");
            var accion = (r.Accion ?? string.Empty).ToUpperInvariant();
            var ent = string.IsNullOrEmpty(r.Entidad) ? "?" : r.Entidad;
            var idTxt = string.IsNullOrEmpty(r.EntidadId) ? string.Empty : " #" + r.EntidadId;
            var antes = LeerJson(r.DatosAntes); var despues = LeerJson(r.DatosDespues);
            switch (accion)
            {
                case "INSERT": return "Alta en " + ent + idTxt + Identificador(despues);
                case "DELETE": return "Baja en " + ent + idTxt + Identificador(antes);
                case "UPDATE":
                    {
                        var claves = Claves(despues, antes);
                        var partes = claves.Take(3).Select(k => k + ": " + Corto(ValorDe(antes, k)) + " → " + Corto(ValorDe(despues, k))).ToList();
                        var resto = claves.Count > 3 ? " (+" + (claves.Count - 3).ToString(CultureInfo.InvariantCulture) + " más)" : string.Empty;
                        return ent + idTxt + (partes.Count > 0 ? ": " + string.Join(", ", partes) : " modificado") + resto;
                    }
                default: return (r.Accion ?? string.Empty) + (string.IsNullOrEmpty(r.Entidad) ? string.Empty : " - " + r.Entidad);
            }
        }

        /// <summary>Detalle campo por campo (INSERT sin "antes", DELETE sin "después").</summary>
        public static List<CambioBitacora> Cambios(BitacoraRegistro r)
        {
            var resultado = new List<CambioBitacora>();
            if (r == null) return resultado;
            var antes = LeerJson(r.DatosAntes); var despues = LeerJson(r.DatosDespues);
            var accion = (r.Accion ?? string.Empty).ToUpperInvariant();
            if (accion == "INSERT") antes = new List<KeyValuePair<string, string>>();
            else if (accion == "DELETE") despues = new List<KeyValuePair<string, string>>();
            foreach (var k in Claves(antes, despues))
                resultado.Add(new CambioBitacora { Campo = k, Antes = ValorDe(antes, k) ?? string.Empty, Despues = ValorDe(despues, k) ?? string.Empty });
            return resultado;
        }

        private static List<string> Claves(List<KeyValuePair<string, string>> primero, List<KeyValuePair<string, string>> segundo)
        {
            var claves = new List<string>();
            foreach (var p in primero) if (!claves.Contains(p.Key)) claves.Add(p.Key);
            foreach (var p in segundo) if (!claves.Contains(p.Key)) claves.Add(p.Key);
            return claves;
        }

        private static string ValorDe(List<KeyValuePair<string, string>> d, string k)
        { foreach (var p in d) if (p.Key == k) return p.Value; return null; }

        private static string Identificador(List<KeyValuePair<string, string>> datos)
        {
            foreach (var c in new[] { "nombre", "descripcion", "name", "titulo" })
            {
                var v = ValorDe(datos, c);
                if (!string.IsNullOrWhiteSpace(v)) return " (" + c + ": " + Corto(v) + ")";
            }
            return string.Empty;
        }

        private static string Corto(string v)
        {
            if (v == null) return "NULL";
            v = v.Replace("\r", " ").Replace("\n", " ");
            return v.Length > 40 ? v.Substring(0, 37) + "..." : v;
        }

        /// <summary>Aplana un objeto JSON a campo -> texto conservando el orden. Si no es un objeto, un único "(valor)"; si es inválido, "(json)".</summary>
        public static List<KeyValuePair<string, string>> LeerJson(string json)
        {
            var res = new List<KeyValuePair<string, string>>();
            if (string.IsNullOrWhiteSpace(json)) return res;
            try
            {
                int i = 0; Blancos(json, ref i);
                if (i >= json.Length) return res;
                if (json[i] != '{')
                {
                    var c = json[i];
                    if (c != '"' && c != '[' && c != '-' && !char.IsDigit(c) && !json.TrimEnd().EndsWith("true") && !json.TrimEnd().EndsWith("false") && !json.TrimEnd().EndsWith("null")) throw new FormatException();
                    res.Add(new KeyValuePair<string, string>("(valor)", Valor(json, ref i))); return res;
                }
                i++;
                while (true)
                {
                    Blancos(json, ref i);
                    if (i >= json.Length) throw new FormatException();
                    if (json[i] == '}') break;
                    if (json[i] == ',') { i++; continue; }
                    if (json[i] != '"') throw new FormatException();
                    var clave = Cadena(json, ref i); Blancos(json, ref i);
                    if (i >= json.Length || json[i] != ':') throw new FormatException();
                    i++; Blancos(json, ref i);
                    res.Add(new KeyValuePair<string, string>(clave, Valor(json, ref i)));
                }
                return res;
            }
            catch (FormatException)
            {
                return new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("(json)", json) };
            }
        }

        private static void Blancos(string s, ref int i) { while (i < s.Length && char.IsWhiteSpace(s[i])) i++; }

        private static string Valor(string s, ref int i)
        {
            if (i >= s.Length) throw new FormatException();
            if (s[i] == '"') return Cadena(s, ref i);
            int inicio = i;
            if (s[i] == '{' || s[i] == '[')
            {
                int nivel = 0;
                while (i < s.Length)
                {
                    char c = s[i];
                    if (c == '"') { Cadena(s, ref i); continue; }
                    i++;
                    if (c == '{' || c == '[') nivel++;
                    else if (c == '}' || c == ']') { nivel--; if (nivel == 0) return s.Substring(inicio, i - inicio); }
                }
                throw new FormatException();
            }
            while (i < s.Length && s[i] != ',' && s[i] != '}' && s[i] != ']') i++;
            var crudo = s.Substring(inicio, i - inicio).Trim();
            if (crudo.Length == 0) throw new FormatException();
            return crudo == "null" ? "NULL" : crudo;
        }

        private static string Cadena(string s, ref int i)
        {
            var sb = new StringBuilder(); i++;
            while (i < s.Length)
            {
                char c = s[i++];
                if (c == '"') return sb.ToString();
                if (c != '\\') { sb.Append(c); continue; }
                if (i >= s.Length) break;
                char e = s[i++];
                switch (e)
                {
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'u':
                        int cp;
                        if (i + 4 > s.Length || !int.TryParse(s.Substring(i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out cp)) throw new FormatException();
                        sb.Append((char)cp); i += 4; break;
                    default: sb.Append(e); break;
                }
            }
            throw new FormatException();
        }
    }
}
