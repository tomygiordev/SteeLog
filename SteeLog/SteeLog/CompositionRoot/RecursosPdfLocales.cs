using System;
using System.IO;
using System.Reflection;
using SteeLog.Core.Application.OrdenesCompra;
namespace SteeLog.CompositionRoot
{
    internal sealed class RecursosPdfLocales : IRecursosPdf
    {
        public MarcaPdf Capturar()
        {
            var fecha = DateTime.Now;
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("SteeLog.Resources.Branding.steelog-logo-dark.png"))
            {
                if (stream == null) throw new InvalidOperationException("Falta el recurso de identidad obligatorio.");
                using (var buffer = new MemoryStream()) {
                    stream.CopyTo(buffer);
                    return new MarcaPdf { LogoPng = buffer.ToArray(), FechaLocal = fecha, Pie = "SteeLog - Sistema de Gestión de Inventario — Generado " + fecha.ToString("dd/MM/yyyy HH:mm") };
                }
            }
        }
    }
}
