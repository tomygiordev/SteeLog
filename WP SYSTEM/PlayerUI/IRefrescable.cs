namespace WP_Inventario
{
    /// <summary>
    /// Las vistas que implementen esta interfaz se refrescan automáticamente
    /// cuando VistaGeneral las recupera del caché y las muestra de nuevo.
    /// </summary>
    public interface IRefrescable
    {
        void Refrescar();
    }
}
