using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using iTextSharp.text.pdf;
using PdfTextExtractor = iTextSharp.text.pdf.parser.PdfTextExtractor;
using SteeLog.Core.Application.Conteos;
using SteeLog.Core.Application.Reportes;
using SteeLog.Infrastructure.Pdf;
using Xunit;

namespace SteeLog.Tests
{
    public class ConteoPdfTests
    {
        private sealed class Recursos : IRecursosPdf
        {
            public bool LogoInvalido;
            public int Lecturas;
            public MarcaPdf Capturar()
            {
                Lecturas++;
                byte[] logo;
                using (var input = typeof(SteeLog.BusquedaGlobal).Assembly.GetManifestResourceStream("SteeLog.Resources.Branding.steelog-logo-dark.png"))
                using (var buffer = new MemoryStream())
                {
                    if (input == null) throw new InvalidOperationException("Falta el logo embebido.");
                    input.CopyTo(buffer);
                    logo = buffer.ToArray();
                }
                return new MarcaPdf { LogoPng = LogoInvalido ? new byte[] { 1, 2, 3 } : logo, FechaLocal = new DateTime(2026, 11, 3, 12, 30, 0), Pie = "SteeLog - Conteo de prueba" };
            }
        }

        private static ConteoLectura Cabecera(string estado) => new ConteoLectura {
            Id = 27, Inicio = new DateTime(2026, 11, 3, 10, 0, 0), Fin = estado == "abierto" ? (DateTime?)null : new DateTime(2026, 11, 3, 11, 0, 0),
            UsuarioInicio = "galpon@tesis.local", UsuarioFin = "galpon@tesis.local", Estado = estado, Observaciones = "Control de prueba"
        };

        private static DetalleConteoLectura[] Detalles() => new[] {
            new DetalleConteoLectura { Id=1, ConteoId=27, MaterialId=10, Nombre="Perfil A", Categoria="Perfiles", Cantidad=2.25m, Stock=10.50m, Diferencia=-8.25m, FechaCarga=new DateTime(2026,11,3,10,1,0) },
            new DetalleConteoLectura { Id=2, ConteoId=27, MaterialId=20, Nombre="Placa B", Categoria="Placas", Cantidad=6.75m, Stock=2.50m, Diferencia=4.25m, FechaCarga=new DateTime(2026,11,3,10,2,0) },
            new DetalleConteoLectura { Id=3, ConteoId=27, MaterialId=30, Nombre="Tornillo C", Categoria="Tornillos", Cantidad=3m, Stock=3m, Diferencia=0m, FechaCarga=new DateTime(2026,11,3,10,3,0) }
        };

        private static IReadOnlyDictionary<int, SeccionConteo> Secciones() => new Dictionary<int, SeccionConteo> {
            [10] = new SeccionConteo { Nombre="Seccion dos", Orden=2 }, [20] = new SeccionConteo { Nombre="Seccion uno", Orden=1 }
        };

        private static string Texto(string archivo)
        {
            using (var reader = new PdfReader(archivo))
            {
                var pagina = reader.GetPageSizeWithRotation(1);
                Assert.True(pagina.Width > pagina.Height);
                var texto = new StringBuilder();
                for (int i=1; i<=reader.NumberOfPages; i++)
                {
                    var paginaTexto = PdfTextExtractor.GetTextFromPage(reader,i);
                    if (paginaTexto.Contains("OBSERVACIONES")) Assert.Contains("Control de prueba",paginaTexto);
                    texto.AppendLine(paginaTexto);
                }
                return texto.ToString();
            }
        }

        private static string Archivo() => Path.Combine(Path.GetTempPath(), "steelog-conteo-pdf-" + Guid.NewGuid().ToString("N") + ".pdf");

        [Fact]
        public void FinalizadoAdminConservaDiferenciasTotalesYOrdenDeSecciones()
        {
            string archivo = Archivo();
            try
            {
                var pdf = new ConteoInventarioPDF(new Recursos());
                pdf.Generar(archivo,Cabecera("finalizado"),Detalles(),Secciones(),true);
                string texto = Texto(archivo);
                Assert.Contains("Stock sistema",texto);
                Assert.Contains("-8,25",texto);
                Assert.Contains("+4,25",texto);
                Assert.Contains("-4,00",texto);
                Assert.Contains("12,00",texto);
                Assert.Contains("16,00",texto);
                Assert.Contains("RESUMEN DE DIFERENCIAS",texto);
                Assert.True(texto.IndexOf("Placa B",StringComparison.Ordinal) < texto.IndexOf("Perfil A",StringComparison.Ordinal));
                Assert.True(texto.IndexOf("Perfil A",StringComparison.Ordinal) < texto.IndexOf("Tornillo C",StringComparison.Ordinal));
                Assert.Equal("Conteo_27_finalizado_20261103_123000.pdf",pdf.SugerirNombreArchivo(Cabecera("finalizado")));
            }
            finally { if (File.Exists(archivo)) File.Delete(archivo); }
        }

        [Theory]
        [InlineData("abierto",true)]
        [InlineData("cancelado",true)]
        [InlineData("finalizado",false)]
        public void TablaDeCincoColumnasConservaHoraYCantidadSinComparacion(string estado,bool admin)
        {
            string archivo = Archivo();
            try
            {
                new ConteoInventarioPDF(new Recursos()).Generar(archivo,Cabecera(estado),Detalles(),Secciones(),admin);
                string texto = Texto(archivo);
                Assert.Contains("Hora carga",texto);
                Assert.Contains("Contado",texto);
                Assert.Contains("Perfil A",texto);
                Assert.Contains("Placa B",texto);
                Assert.Contains("Tornillo C",texto);
                Assert.Contains("2,25",texto);
                Assert.Contains("6,75",texto);
                Assert.Contains("03/11/2026 10:03",texto);
                Assert.DoesNotContain("Stock sistema",texto);
                Assert.DoesNotContain("RESUMEN DE DIFERENCIAS",texto);
                Assert.DoesNotContain("-8,25",texto);
            }
            finally { if (File.Exists(archivo)) File.Delete(archivo); }
        }

        [Fact]
        public void LogoInvalidoNoDejaArchivoBloqueadoYSePuedeReintentar()
        {
            string archivo = Archivo();
            try
            {
                Assert.ThrowsAny<Exception>(() => new ConteoInventarioPDF(new Recursos { LogoInvalido=true }).Generar(archivo,Cabecera("abierto"),Detalles(),Secciones(),false));
                using (var stream = new FileStream(archivo,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None)) stream.SetLength(0);
                new ConteoInventarioPDF(new Recursos()).Generar(archivo,Cabecera("abierto"),Detalles(),Secciones(),false);
                Assert.Contains("Perfil A",Texto(archivo));
            }
            finally { if (File.Exists(archivo)) File.Delete(archivo); }
        }

        [Fact]
        public void RutaVaciaSeRechazaAntesDeLeerRecursos()
        {
            var recursos = new Recursos();
            Assert.Throws<ArgumentException>(() => new ConteoInventarioPDF(recursos).Generar(" ",Cabecera("abierto"),Detalles(),Secciones(),false));
            Assert.Equal(0,recursos.Lecturas);
        }
    }
}
