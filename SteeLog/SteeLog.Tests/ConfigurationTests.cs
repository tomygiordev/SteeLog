using System;
using System.IO;
using SteeLog.Infrastructure.Configuration;
using SteeLog.Infrastructure.Persistence;
using Xunit;

public class ConfigurationTests
{
    [Fact]
    public void Factory_creates_distinct_closed_connections()
    {
        var factory = new PostgresConnectionFactory("Host=localhost;Database=demo;Username=demo;Password=demo");
        using (var a = factory.Create()) using (var b = factory.Create())
        { Assert.NotSame(a, b); Assert.Equal(System.Data.ConnectionState.Closed, a.State); Assert.Equal(System.Data.ConnectionState.Closed, b.State); }
    }
    [Fact]
    public void Factory_sanitizes_invalid_connection_string()
    {
        var error = Assert.Throws<InvalidOperationException>(() => new PostgresConnectionFactory("secret-not-a-valid-key=private"));
        Assert.DoesNotContain("private", error.Message);
    }
    [Fact]
    public void Loads_external_configuration_and_preserves_storage_location()
    {
        var directory = Path.Combine(Path.GetTempPath(), "steelog-f1-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            File.WriteAllText(Path.Combine(directory, "appsettings.json"), "{\"ConnectionStrings\":{\"DefaultConnection\":\"Host=localhost;Database=demo\"}}");
            var config = LocalConfiguration.Load(directory);
            Assert.Contains("Database=demo", config.ConnectionString);
            var paths = new LocalPaths(directory, directory);
            Assert.Equal(Path.Combine(directory, "SteeLog", "Storage"), paths.StorageDirectory);
            File.Delete(Path.Combine(directory, "appsettings.json"));
        }
        finally { Directory.Delete(directory, true); }
    }
    [Fact]
    public void Invalid_json_is_sanitized()
    {
        var directory = Path.Combine(Path.GetTempPath(), "steelog-f1-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try { File.WriteAllText(Path.Combine(directory, "appsettings.json"), "{secret"); var error = Assert.Throws<InvalidOperationException>(() => LocalConfiguration.Load(directory)); Assert.DoesNotContain("secret", error.Message); }
        finally { Directory.Delete(directory, true); }
    }
    [Fact]
    public void Missing_file_and_connection_are_sanitized()
    {
        var directory = Path.Combine(Path.GetTempPath(), "steelog-f1-secret-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var missing = Assert.Throws<InvalidOperationException>(() => LocalConfiguration.Load(directory));
            Assert.DoesNotContain(directory, missing.ToString());
            File.WriteAllText(Path.Combine(directory, "appsettings.json"), "{}");
            var config = LocalConfiguration.Load(directory);
            Assert.Throws<InvalidOperationException>(() => new PostgresConnectionFactory(config.ConnectionString));
        }
        finally { Directory.Delete(directory, true); }
    }
}
