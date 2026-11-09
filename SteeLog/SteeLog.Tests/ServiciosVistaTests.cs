using System;
using System.Threading;
using System.Threading.Tasks;
using SteeLog.Core.Application.Borradores;
using SteeLog.Core.Application.Identidad;
using SteeLog.Servicios;
using Xunit;

namespace SteeLog.Tests
{
    public sealed class ServiciosVistaTests
    {
        [Theory]
        [InlineData("  ADMIN@TESIS.LOCAL ", UserRole.Admin, "admin", false)]
        [InlineData(" GALPON@tesis.local", UserRole.Galpon, "galpon", true)]
        public void SesionConservaNormalizacionRolYVisibilidad(string email, UserRole role, string username, bool ocultar)
        {
            var servicios = new ServiciosVistaDePrueba();
            Assert.False(servicios.Sesion.IsLoggedIn);
            Assert.Null(servicios.Sesion.Email);
            Assert.Null(servicios.Sesion.Username);
            Assert.Equal(UserRole.None, servicios.Sesion.Rol);
            Assert.True(servicios.Sesion.DebeOcultarCantidadesStockMateriales);
            servicios.Sesion.SetSession(email);
            Assert.True(servicios.Sesion.IsLoggedIn);
            Assert.True(servicios.Sesion.IsLoggedIn);
            Assert.Equal(role, servicios.Sesion.Rol);
            Assert.Equal(role, servicios.Sesion.Role);
            Assert.Equal(username, servicios.Sesion.Username);
            Assert.Equal(username + "@tesis.local", servicios.Sesion.Email);
            Assert.Equal(ocultar, servicios.Sesion.DebeOcultarCantidadesStockMateriales);
            Assert.Throws<InvalidOperationException>(() => servicios.Sesion.SetSession("otro@tesis.local"));
            Assert.Equal(role, servicios.Sesion.Rol);
            servicios.Sesion.Clear();
            Assert.False(servicios.Sesion.IsLoggedIn);
            Assert.Equal(UserRole.None, servicios.Sesion.Rol);
        }

        [Fact]
        public void IdentidadEsNulaSinSesionEstableDuranteLaSesionYNuevaTrasRelogin()
        {
            var servicios = new ServiciosVistaDePrueba();
            Assert.Null(servicios.Sesion.Identidad);
            servicios.Sesion.SetSession("admin@tesis.local");
            var primera = servicios.Sesion.Identidad;
            Assert.NotNull(primera);
            Assert.Same(servicios.Sesion.Identidad, primera);
            Assert.Same(primera, servicios.Sesion.Identidad);
            servicios.Sesion.Clear();
            Assert.Null(servicios.Sesion.Identidad);
            servicios.Sesion.SetSession("admin@tesis.local");
            Assert.NotNull(servicios.Sesion.Identidad);
            Assert.NotSame(primera, servicios.Sesion.Identidad);
        }

        [Theory]
        [InlineData(-1, "Sin stock")]
        [InlineData(0, "Sin stock")]
        [InlineData(1, "Hay stock")]
        public void EstadoStockConservaSemantica(decimal stock, string esperado)
            => Assert.Equal(esperado, new ServiciosVistaDePrueba().Sesion.ObtenerEstadoStockMaterial(stock));

        [Fact]
        public async Task DoblesDePruebaNoCompartenSesionCredencialesNiBorradores()
        {
            var uno = new ServiciosVistaDePrueba();
            var otro = new ServiciosVistaDePrueba();
            uno.Sesion.SetSession("admin@tesis.local");
            uno.Credenciales.Save("usuario", "clave-test");
            uno.Borrador.Guardar(new MovimientoDraft { NroRemito = "test" });
            Assert.False(otro.Sesion.IsLoggedIn);
            Assert.Null(otro.Credenciales.Load().email);
            Assert.Null(otro.Borrador.Leer());
            Assert.Equal("test", uno.Borrador.Leer().NroRemito);
            Assert.Equal("usuario", uno.Credenciales.Load().email);
            Assert.False((await uno.Autenticacion.SignInAsync("x", "x")).Success);
            uno.Credenciales.Clear();
            Assert.Null(uno.Credenciales.Load().password);
        }

        [Fact]
        public void LoginUsaAutenticacionSesionYCredencialesInyectadas()
        {
            Exception error = null;
            var hilo = new Thread(() => {
                try
                {
                    var servicios = new ServiciosVistaDePrueba(authentication: new AutenticacionAceptada());
                    using (var vista = new SteeLog.Vistas.Vista_Login(servicios))
                    {
                        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                        ((System.Windows.Forms.TextBox)vista.GetType().GetField("txtEmail", flags).GetValue(vista)).Text = "admin";
                        ((System.Windows.Forms.TextBox)vista.GetType().GetField("txtPassword", flags).GetValue(vista)).Text = "clave-test";
                        vista.GetType().GetMethod("BtnLogin_Click", flags).Invoke(vista, new object[] { null, EventArgs.Empty });
                        Assert.True(servicios.Sesion.IsLoggedIn);
                        Assert.Equal(UserRole.Admin, servicios.Sesion.Rol);
                        Assert.Equal("admin@tesis.local", servicios.Credenciales.Load().email);
                        Assert.Equal("clave-test", servicios.Credenciales.Load().password);
                        Assert.Equal(System.Windows.Forms.DialogResult.OK, vista.DialogResult);
                    }
                }
                catch (Exception ex) { error = ex; }
            });
            hilo.SetApartmentState(ApartmentState.STA);
            hilo.Start();
            Assert.True(hilo.Join(TimeSpan.FromSeconds(15)));
            Assert.Null(error);
        }
        private sealed class AutenticacionAceptada : IAuthentication
        {
            public Task<Identity> AuthenticateAsync(string username, string password)
                => Task.FromResult(new Identity(username, Role.Admin));
        }
    }
}
