using System;
using SteeLog.Presentation;
using Xunit;

namespace SteeLog.Tests
{
    public class MensajeEliminacionTests
    {
        // Imita PostgresException sin depender de Npgsql: solo expone SqlState.
        private sealed class ErrorConCodigo : Exception
        {
            public ErrorConCodigo(string codigo) : base("23503: update or delete on table \"x\" violates foreign key constraint") { SqlState = codigo; }
            public string SqlState { get; }
        }

        [Fact]
        public void FkVioladaNoTraducidaNoSeMuestraCruda()
        {
            var mensaje = CatalogosPresenter.MensajeEliminacion(new InvalidOperationException("envoltura", new ErrorConCodigo("23503")));
            Assert.Equal("No se puede eliminar porque está en uso por otros registros.", mensaje);
            Assert.DoesNotContain("foreign key", mensaje);
        }

        [Fact]
        public void OtrosErroresConservanSuDetalle()
        {
            Assert.Equal("No se pudo eliminar: otro", CatalogosPresenter.MensajeEliminacion(new InvalidOperationException("otro")));
            Assert.StartsWith("No se pudo eliminar: ", CatalogosPresenter.MensajeEliminacion(new ErrorConCodigo("42P01")));
        }
    }
}
