namespace WP_Inventario.Modelos
{
    public static class OrdenCompraEstados
    {
        public const string Pendiente = "Pendiente";
        public const string RecibidaCompleta = "Recibida Completa";
        public const string RecibidaParcial = "Recibida Parcial";

        public static readonly string[] Todos = new[]
        {
            Pendiente,
            RecibidaCompleta,
            RecibidaParcial
        };

        public static bool EsEditable(string estado)
        {
            return estado == Pendiente;
        }

        public static bool PermiteEditarCabecera(string estado)
        {
            return estado == Pendiente || estado == RecibidaParcial || estado == RecibidaCompleta;
        }

        public static bool PermiteVerRevision(string estado)
        {
            return estado == Pendiente || estado == RecibidaParcial || estado == RecibidaCompleta;
        }

        public static bool PermiteRecepcion(string estado)
        {
            return estado == Pendiente || estado == RecibidaParcial;
        }
    }
}
