using System;
using System.Collections.Generic;
namespace SteeLog.Core.Application.Reportes
{
    public sealed class ReporteStockColumnaPdf { public string Nombre { get; set; } public string Titulo { get; set; } }
    public sealed class ReporteStockFilaPdf
    {
        public string Seccion { get; set; }
        public bool StockBajo { get; set; }
        public List<string> Valores { get; set; } = new List<string>();
    }
    public sealed class ReporteStockSnapshot
    {
        public DateTime? FechaDatos { get; set; }
        public string TotalMateriales { get; set; }
        public string TotalStockBajo { get; set; }
        public List<ReporteStockColumnaPdf> Columnas { get; set; } = new List<ReporteStockColumnaPdf>();
        public List<ReporteStockFilaPdf> Filas { get; set; } = new List<ReporteStockFilaPdf>();
    }
    public interface IReporteStockPdf { void Generar(string ruta, ReporteStockSnapshot snapshot); }
}
