using System;
using SteeLog.Domain.Activos;
using SteeLog.Presentation;
using Xunit;
namespace SteeLog.Tests
{
    public class HerramientasDomainTests
    {
        [Fact] public void IdentidadUsaIdRealSinDuplicarPrefijoExistente()
        {
            var nueva=new Herramienta(0,"Taladro","Óptima",null,null); nueva.AsignarIdentidad(23); Assert.Equal("23Taladro",nueva.Nombre);
            var existente=new Herramienta(0,"123 Taladro","Regular",null,null); existente.AsignarIdentidad(25); Assert.Equal("123 Taladro",existente.Nombre);
        }
        [Fact] public void PrefijoNoDesbordaYNoAsignaIdentidadSiFalla()
        {
            var h=new Herramienta(0,new string('x',255),"Óptima",null,null);
            Assert.Throws<ArgumentException>(()=>h.AsignarIdentidad(10)); Assert.Equal(0,h.Id);
        }
        [Fact] public void TrasladoPreservaEstadoHistoricoYExigePertenencia()
        {
            var h=new Herramienta(3,"3Taladro","En Obra",7,"obs"); h.EnviarAGalpon();
            Assert.Null(h.ObraId); Assert.Equal("En Obra",h.Estado); Assert.Throws<InvalidOperationException>(()=>h.EnviarAGalpon());
            h.Editar(h.Nombre,"En Obra",null,"modificada"); Assert.Equal("En Obra",h.Estado);
            Assert.Throws<ArgumentException>(()=>h.Editar(h.Nombre,"Otro",null,null));
        }
        [Fact] public void CargasDesactualizadasYAlCerrarNoSonVigentes()
        {
            var cargas=new CargasHerramientas(); int a=cargas.Iniciar(); int b=cargas.Iniciar();
            Assert.False(cargas.EsActual(a)); Assert.True(cargas.EsActual(b)); cargas.Cerrar(); Assert.False(cargas.EsActual(b));
        }
    }
}
