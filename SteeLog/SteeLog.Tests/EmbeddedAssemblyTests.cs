using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Mono.Cecil;
using Xunit;
public class EmbeddedAssemblyTests
{
    [Theory]
    [InlineData("SteeLog.Domain")]
    [InlineData("SteeLog.Application")]
    [InlineData("SteeLog.Infrastructure")]
    [InlineData("Npgsql")]
    [InlineData("itextsharp")]
    [InlineData("Microsoft.Extensions.DependencyInjection")]
    [InlineData("Microsoft.Extensions.DependencyInjection.Abstractions")]
    public void Costura_resource_contains_loadable_expected_assembly(string expected)
    {
        var path = Environment.GetEnvironmentVariable("STEELOG_HOST_ASSEMBLY");
        using (var host = AssemblyDefinition.ReadAssembly(path))
        {
            var resource = Assert.IsType<EmbeddedResource>(host.MainModule.Resources.Single(r => r.Name == "costura." + expected.ToLowerInvariant() + ".dll.compressed"));
            using (var compressed = resource.GetResourceStream()) using (var deflate = new DeflateStream(compressed, CompressionMode.Decompress)) using (var bytes = new MemoryStream())
            {
                deflate.CopyTo(bytes); var loaded = System.Reflection.Assembly.Load(bytes.ToArray());
                Assert.Equal(expected, loaded.GetName().Name); Assert.NotEmpty(loaded.GetTypes());
            }
            Assert.Contains(host.MainModule.Resources, r => r.Name == "SteeLog.appsettings.json");
        }
    }
}
