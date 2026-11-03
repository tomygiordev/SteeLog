namespace SteeLog.Domain.OrdenesCompra
{
    public static class EstadosOrden
    {
        public const string Pendiente = "Pendiente";
        public const string RecibidaCompleta = "Recibida Completa";
        public const string RecibidaParcial = "Recibida Parcial";
        public static readonly string[] Todos = { Pendiente, RecibidaCompleta, RecibidaParcial };
        public static bool EsEditable(string estado) => estado == Pendiente;
        public static bool PermiteEditarCabecera(string estado) => estado == Pendiente || estado == RecibidaParcial || estado == RecibidaCompleta;
        public static bool PermiteVerRevision(string estado) => PermiteEditarCabecera(estado);
        public static bool PermiteRecepcion(string estado) => estado == Pendiente || estado == RecibidaParcial;
    }
}
