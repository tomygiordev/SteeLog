using System;
using System.Collections.Generic;
namespace SteeLog.Core.Application.OrdenesCompra
{
    public sealed class MarcaPdf
    {
        public byte[] LogoPng { get; set; }
        public DateTime FechaLocal { get; set; }
        public string Pie { get; set; }
    }
    public interface IRecursosPdf { MarcaPdf Capturar(); }
    public interface IOrdenesPendientesPdf
    {
        string SugerirNombreArchivo();
        void Generar(string rutaDestino, IReadOnlyList<PendienteLectura> items);
    }
}
