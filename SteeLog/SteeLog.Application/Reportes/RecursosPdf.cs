using System;

namespace SteeLog.Core.Application.Reportes
{
    public sealed class MarcaPdf
    {
        public byte[] LogoPng { get; set; }
        public DateTime FechaLocal { get; set; }
        public string Pie { get; set; }
    }

    public interface IRecursosPdf
    {
        MarcaPdf Capturar();
    }
}
