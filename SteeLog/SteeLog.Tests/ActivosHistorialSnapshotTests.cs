using System;
using SteeLog.Core.Application.Canaletas;
using SteeLog.Infrastructure.Activos;
using Xunit;

namespace SteeLog.Tests
{
    public class ActivosHistorialSnapshotTests
    {
        [Fact]
        public void HerramientasHistorialGuardaSnapshotDeNombre()
        {
            var sql = NpgsqlHerramientas.SqlRegistrarHistorial;
            Assert.Contains("nombre_herramienta", sql);
            Assert.Contains("SELECT nombre FROM public.herramientas WHERE id_herramienta=@id", sql);
        }

        [Fact]
        public void InsumosHistorialGuardaSnapshotDeNombre()
        {
            var sql = NpgsqlInsumos.SqlRegistrarHistorial;
            Assert.Contains("nombre_insumo", sql);
            Assert.Contains("SELECT nombre FROM public.insumos WHERE id_insumo=@id", sql);
            Assert.Contains("snapshot_completo", sql);
        }

        [Fact]
        public void FiltroDeFotosListaSoloExtensionesQueLaValidacionAcepta()
        {
            var f = OperacionesCanaleta.FiltroDialogoFotos;
            Assert.DoesNotContain("*.*", f);
            foreach (var ext in new[] { "*.jpg", "*.jpeg", "*.png", "*.webp" }) Assert.Contains(ext, f);
            Assert.Equal(2, f.Split('|').Length);
        }
    }
}
