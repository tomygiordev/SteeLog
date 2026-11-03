using System.Collections.Generic;
namespace SteeLog.Core.Application.Conteos
{
    public sealed class SeccionConteo { public string Nombre { get; set; } public int Orden { get; set; } }
    public interface IConteoPdf
    {
        string SugerirNombreArchivo(ConteoLectura conteo);
        void Generar(string ruta, ConteoLectura conteo, IReadOnlyList<DetalleConteoLectura> detalles, IReadOnlyDictionary<int, SeccionConteo> secciones, bool esAdmin);
    }
}
