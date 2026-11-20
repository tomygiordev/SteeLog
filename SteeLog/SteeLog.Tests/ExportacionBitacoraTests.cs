using System;
using System.Collections.Generic;
using SteeLog.Core.Application.Bitacora;
using SteeLog.Infrastructure.Respaldos;
using Xunit;

public class ExportacionBitacoraTests
{
    private static readonly DateTime Generado = new DateTime(2026, 11, 20, 16, 0, 0);

    private static List<BitacoraRegistro> Registros() => new List<BitacoraRegistro>
    {
        new BitacoraRegistro
        {
            Id = 1, Fecha = new DateTime(2026, 11, 19, 10, 5, 0), Usuario = "admin@steelog", Rol = "Admin", Equipo = "NOTE", VersionApp = "1.0",
            Origen = "BD", Accion = "UPDATE", Entidad = "materiales", EntidadId = "7",
            DatosAntes = "{\"stock\": 5, \"nombre\": \"Chapa | lisa\"}", DatosDespues = "{\"stock\": 3, \"nombre\": \"Chapa\\nlisa\"}"
        },
        new BitacoraRegistro
        {
            Id = 2, Fecha = new DateTime(2026, 11, 19, 11, 0, 0), Usuario = "admin@steelog", Origen = "APP", Accion = "RESPALDO",
            Entidad = "respaldos", Descripcion = "Copia de seguridad Manual: x.dump (1 MB)"
        }
    };

    [Fact]
    public void TextoIncluyeEncabezadoRegistrosYCambios()
    {
        var t = ExportacionBitacora.ATexto(Registros(), Generado);
        Assert.Contains("BITÁCORA DE ACTIVIDAD", t);
        Assert.Contains("Registros: 2", t);
        Assert.Contains("[19/11/2026 10:05:00] UPDATE - materiales #7", t);
        Assert.Contains("Usuario: admin@steelog | Rol: Admin | Equipo: NOTE", t);
        Assert.Contains("- stock: 5 -> 3", t);
        Assert.Contains("Copia de seguridad Manual: x.dump (1 MB)", t);
        Assert.Contains("Rol: -", t);
    }

    [Fact]
    public void MarkdownArmaTablaSinRomperColumnas()
    {
        var md = ExportacionBitacora.AMarkdown(Registros(), Generado);
        Assert.StartsWith("# Bitácora de actividad", md);
        Assert.Contains("| Fecha | Usuario | Rol | Equipo | Origen | Acción | Entidad | ID | Resumen |", md);
        Assert.Contains("| 19/11/2026 11:00:00 | admin@steelog | - | - | APP | RESPALDO | respaldos | - | Copia de seguridad Manual: x.dump (1 MB) |", md);
        Assert.Contains("## Detalle de cambios", md);
        Assert.Contains("| stock | 5 | 3 |", md);
        // La barra vertical se escapa y los saltos de línea no parten la fila.
        Assert.Contains("Chapa \\| lisa", md);
        foreach (var linea in md.Split('\n'))
            if (linea.StartsWith("| nombre", StringComparison.Ordinal)) Assert.EndsWith("|", linea.TrimEnd('\r'));
    }

    [Fact]
    public void SinRegistrosNoFalla()
    {
        Assert.Contains("Registros: 0", ExportacionBitacora.ATexto(new List<BitacoraRegistro>(), Generado));
        Assert.DoesNotContain("Detalle de cambios", ExportacionBitacora.AMarkdown(new List<BitacoraRegistro>(), Generado));
    }

    [Fact]
    public void RaicesDelEquipoIncluyenProgramFilesDeLosDiscos()
    {
        var raices = LocalizadorHerramientas.RaicesDelEquipo();
        Assert.NotEmpty(raices);
        Assert.Contains(raices, r => r.EndsWith("Program Files", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(raices.Count, new HashSet<string>(raices, StringComparer.OrdinalIgnoreCase).Count);
    }
}
