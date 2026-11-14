using System.Text;
using Npgsql;

namespace SteeLog.Infrastructure.Persistence
{
    /// <summary>Quién abre la conexión. Llega a PostgreSQL como app.usuario / app.rol / app.equipo / app.version.</summary>
    public sealed class IdentificacionConexion
    {
        private const int MaximoValor = 200;
        public string Usuario { get; set; }
        public string Rol { get; set; }
        public string Equipo { get; set; }
        public string Version { get; set; }

        /// <summary>Valor del parámetro de arranque "options" (formato libpq: "-c clave=valor -c ...").</summary>
        internal string ComoOpciones()
        {
            var sb = new StringBuilder();
            Agregar(sb, "app.usuario", Usuario); Agregar(sb, "app.rol", Rol);
            Agregar(sb, "app.equipo", Equipo); Agregar(sb, "app.version", Version);
            return sb.ToString();
        }

        private static void Agregar(StringBuilder sb, string clave, string valor)
        {
            if (sb.Length > 0) sb.Append(' ');
            sb.Append("-c ").Append(clave).Append('=');
            if (valor == null) return;
            if (valor.Length > MaximoValor) valor = valor.Substring(0, MaximoValor);
            foreach (var c in valor)
            {
                // En libpq la barra invertida escapa espacios y barras; los caracteres de control se reemplazan.
                if (c < ' ' || c == '\u007f') { sb.Append("\\ "); continue; }
                if (c == '\\' || c == ' ') sb.Append('\\');
                sb.Append(c);
            }
        }

        /// <summary>Devuelve la cadena de conexión con la identidad agregada al parámetro Options (conservando el que ya tuviera).</summary>
        public static string Aplicar(string cadena, IdentificacionConexion id)
        {
            if (id == null) return cadena;
            var builder = new NpgsqlConnectionStringBuilder(cadena);
            var propias = id.ComoOpciones();
            builder.Options = string.IsNullOrWhiteSpace(builder.Options) ? propias : builder.Options + " " + propias;
            return builder.ConnectionString;
        }
    }
}
