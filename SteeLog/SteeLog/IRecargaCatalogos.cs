namespace SteeLog
{
    /// <summary>
    /// Las vistas cacheadas que listan catálogos en combos (obras, ubicaciones, proveedores, fletes)
    /// implementan esta interfaz: VistaGeneral la invoca al volver a mostrarlas para que recarguen SOLO
    /// los catálogos, conservando lo que el usuario estaba cargando y sin avisos ni efectos sobre otros estados.
    /// </summary>
    public interface IRecargaCatalogos
    {
        void RecargarCatalogos();
    }
}
