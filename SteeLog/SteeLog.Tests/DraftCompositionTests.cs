using System;
using System.IO;
using System.Reflection;
using System.Text;
using SteeLog.Core.Application.Borradores;
using SteeLog.Presentation;
using Xunit;
namespace SteeLog.Tests
{
    public class DraftCompositionTests
    {
        [Fact]
        public void ComposicionDePruebaUsaRutaTemporalInyectada()
        {
            string root=Path.Combine(Path.GetTempPath(),"steelog_draft_comp_"+Guid.NewGuid().ToString("N"));
            var type=typeof(IUseCaseExecutor).Assembly.GetType("SteeLog.CompositionRoot.LocalServices",true);
            var services=Activator.CreateInstance(type,BindingFlags.NonPublic|BindingFlags.Instance,null,new object[]{root,root},null);
            try
            {
                var store=(IDraftStore)type.GetProperty("MovimientoDrafts",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(services);
                store.Guardar(Encoding.UTF8.GetBytes("temporal"));
                Assert.Equal("temporal",File.ReadAllText(Path.Combine(root,"SteeLog","movimiento_draft.json")));
            }
            finally { ((IDisposable)services).Dispose(); if(Directory.Exists(root)) Directory.Delete(root,true); }
        }
    }
}
