namespace SteeLog.Core.Application.Borradores
{
    public interface IDraftStore
    {
        byte[] Leer();
        void Guardar(byte[] contenido);
        void Eliminar();
    }
}
