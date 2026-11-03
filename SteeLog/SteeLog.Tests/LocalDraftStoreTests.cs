using System;
using System.IO;
using System.Text;
using SteeLog.Infrastructure.Files;
using Xunit;

namespace SteeLog.Tests
{
    public class LocalDraftStoreTests
    {
        [Fact]
        public void ReemplazaYRecuperaDatosSinDejarTemporales()
        {
            var directorio = Path.Combine(Path.GetTempPath(), "steelog_draft_test_" + Guid.NewGuid().ToString("N"));
            try
            {
                var store = new LocalDraftStore(Path.Combine(directorio, "movimiento_draft.json"));
                Assert.Null(store.Leer());
                store.Guardar(Encoding.UTF8.GetBytes("primero"));
                store.Guardar(Encoding.UTF8.GetBytes("segundo"));
                Assert.Equal("segundo", Encoding.UTF8.GetString(store.Leer()));
                Assert.Single(Directory.GetFiles(directorio));
                store.Eliminar();
                Assert.Null(store.Leer());
            }
            finally { if (Directory.Exists(directorio)) Directory.Delete(directorio, true); }
        }
    }
}
