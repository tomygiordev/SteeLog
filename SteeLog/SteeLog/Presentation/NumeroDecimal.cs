using System.Globalization;

namespace SteeLog.Presentation
{
    public static class NumeroDecimal
    {
        public static bool TryParse(string texto, out decimal valor)
        {
            valor = 0m;
            if (texto == null) return false;
            texto = texto.Trim();
            int inicio = texto.StartsWith("-") ? 1 : 0;
            int coma = -1, puntos = 0, digitos = 0;
            for (int i = inicio; i < texto.Length; i++)
            {
                char c = texto[i];
                if (c >= '0' && c <= '9') digitos++;
                else if (c == '.') puntos++;
                else if (c == ',' && coma < 0) coma = i;
                else return false;
            }
            if (digitos == 0) return false;

            // Un solo punto sin coma conserva su significado decimal histórico.
            if (puntos > 0 && (coma >= 0 || puntos > 1))
            {
                int finEntero = coma >= 0 ? coma : texto.Length;
                int grupo = 0;
                bool primero = true;
                for (int i = inicio; i < finEntero; i++)
                {
                    if (texto[i] != '.') { grupo++; continue; }
                    if (grupo < 1 || (primero ? grupo > 3 : grupo != 3)) return false;
                    primero = false;
                    grupo = 0;
                }
                if (grupo != 3) return false;
                if (coma >= 0 && texto.IndexOf('.', coma) >= 0) return false;
                texto = texto.Replace(".", "");
            }

            return decimal.TryParse(texto.Replace(',', '.'),
                NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out valor);
        }
    }
}
