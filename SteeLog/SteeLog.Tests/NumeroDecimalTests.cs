using System.Globalization;
using SteeLog.Presentation;
using Xunit;

public class NumeroDecimalTests
{
    [Theory]
    [InlineData("0", "0")]
    [InlineData("-0", "0")]
    [InlineData("1234", "1234")]
    [InlineData("  -12,50  ", "-12.50")]
    [InlineData("\t\r\n2.5\u00a0", "2.5")]
    [InlineData("1.500", "1.5")]
    [InlineData("-1.500", "-1.5")]
    [InlineData("1.50", "1.5")]
    [InlineData("1234.567", "1234.567")]
    [InlineData("1,500", "1.5")]
    [InlineData(".5", "0.5")]
    [InlineData(",5", "0.5")]
    [InlineData("-.5", "-0.5")]
    [InlineData("-,5", "-0.5")]
    [InlineData("5.", "5")]
    [InlineData("5,", "5")]
    [InlineData("1.500,50", "1500.50")]
    [InlineData("-1.500,50", "-1500.50")]
    [InlineData("12.345,6", "12345.6")]
    [InlineData("123.456,789", "123456.789")]
    [InlineData("1.500.000", "1500000")]
    [InlineData("-1.500.000", "-1500000")]
    [InlineData("12.345.678,90", "12345678.90")]
    [InlineData("123.456.789", "123456789")]
    [InlineData("0.000,50", "0.50")]
    [InlineData("000.001.000", "1000")]
    [InlineData("1.000,", "1000")]
    [InlineData("79228162514264337593543950335", "79228162514264337593543950335")]
    [InlineData("-79228162514264337593543950335", "-79228162514264337593543950335")]
    [InlineData("79.228.162.514.264.337.593.543.950.335,0", "79228162514264337593543950335")]
    [InlineData("0,0000000000000000000000000001", "0.0000000000000000000000000001")]
    public void Acepta_formatos_conservadores_sin_depender_de_la_cultura(string texto, string esperado)
    {
        ConCadaCultura(() =>
        {
            Assert.True(NumeroDecimal.TryParse(texto, out decimal valor));
            Assert.Equal(decimal.Parse(esperado, CultureInfo.InvariantCulture), valor);
        });
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t\r\n")]
    [InlineData("-")]
    [InlineData(".")]
    [InlineData(",")]
    [InlineData("-.")]
    [InlineData("-,")]
    [InlineData("+")]
    [InlineData("+1")]
    [InlineData("--1")]
    [InlineData("1-")]
    [InlineData("1-2")]
    [InlineData("- 1")]
    [InlineData("1 500")]
    [InlineData("1\t500")]
    [InlineData("1\n500")]
    [InlineData("1\u00a0500")]
    [InlineData("$1,50")]
    [InlineData("1,50 $")]
    [InlineData("€1")]
    [InlineData("(1,50)")]
    [InlineData("1e3")]
    [InlineData("1E-07")]
    [InlineData("NaN")]
    [InlineData("Infinity")]
    [InlineData("uno")]
    [InlineData("1m")]
    [InlineData("١٢٣")]
    [InlineData("１２３")]
    [InlineData("1,,5")]
    [InlineData("1,5,0")]
    [InlineData("1,500.50")]
    [InlineData("1.50,5")]
    [InlineData("1234.500,5")]
    [InlineData("1.500.00")]
    [InlineData("1.50.000")]
    [InlineData("1234.500.000")]
    [InlineData(".500,50")]
    [InlineData("1.,50")]
    [InlineData("1..500,50")]
    [InlineData("1.500.,50")]
    [InlineData("1.500,5.0")]
    [InlineData("1.500.000.")]
    [InlineData(".500.000")]
    [InlineData("1..500")]
    [InlineData("1.500..000")]
    [InlineData("79228162514264337593543950336")]
    [InlineData("-79228162514264337593543950336")]
    [InlineData("79.228.162.514.264.337.593.543.950.336,0")]
    public void Rechaza_formato_invalido_y_desborde_dejando_valor_cero(string texto)
    {
        ConCadaCultura(() =>
        {
            decimal valor = 123m;
            Assert.False(NumeroDecimal.TryParse(texto, out valor));
            Assert.Equal(0m, valor);
        });
    }

    [Theory]
    [InlineData("0.00")]
    [InlineData("0.##")]
    [InlineData("G")]
    public void Conserva_textos_generados_sin_agrupacion_ni_exponentes(string formato)
    {
        ConCadaCultura(() =>
        {
            foreach (decimal original in new[] { 0m, 1m, -1.5m, 1500.50m, 1500000m, decimal.MaxValue })
            {
                string texto = original.ToString(formato, CultureInfo.CurrentCulture);
                Assert.True(decimal.TryParse(texto.Replace(',', '.'), NumberStyles.Any,
                    CultureInfo.InvariantCulture, out decimal anterior));
                Assert.True(NumeroDecimal.TryParse(texto, out decimal actual));
                Assert.Equal(anterior, actual);
            }
        });
    }

    [Fact]
    public void Documenta_por_que_no_se_migran_consumidores_de_G29()
    {
        ConCadaCultura(() =>
        {
            string texto = 0.0000001m.ToString("G29", CultureInfo.CurrentCulture);
            Assert.Contains("E", texto);
            Assert.True(decimal.TryParse(texto.Replace(',', '.'), NumberStyles.Any,
                CultureInfo.InvariantCulture, out decimal anterior));
            Assert.Equal(0.0000001m, anterior);
            Assert.False(NumeroDecimal.TryParse(texto, out _));
        });
    }

    private static void ConCadaCultura(System.Action comprobar)
    {
        var anterior = CultureInfo.CurrentCulture;
        try
        {
            foreach (string nombre in new[] { "es-AR", "en-US", "fr-FR", "" })
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(nombre);
                comprobar();
            }
        }
        finally { CultureInfo.CurrentCulture = anterior; }
    }
}
