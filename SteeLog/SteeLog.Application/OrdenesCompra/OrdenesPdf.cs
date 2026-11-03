using System.Collections.Generic;
namespace SteeLog.Core.Application.OrdenesCompra
{
    public interface IOrdenesPendientesPdf
    {
        string SugerirNombreArchivo();
        void Generar(string rutaDestino, IReadOnlyList<PendienteLectura> items);
    }
}
