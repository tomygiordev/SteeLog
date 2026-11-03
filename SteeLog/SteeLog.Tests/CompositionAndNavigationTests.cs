using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using SteeLog.Presentation;
using Xunit;

public class CompositionAndNavigationTests
{
    sealed class Probe : IDisposable { public bool Disposed; public void Dispose() { Disposed = true; } }
    [Fact] public async Task Executor_creates_distinct_scopes_and_disposes_on_success_error_and_cancellation()
    {
        var created = new List<Probe>();
        var services = new ServiceCollection();
        services.AddScoped(_ => { var probe = new Probe(); created.Add(probe); return probe; });
        using (var provider = services.BuildServiceProvider()) {
            var type = typeof(IUseCaseExecutor).Assembly.GetType("SteeLog.CompositionRoot.UseCaseExecutor", true);
            var executor = (IUseCaseExecutor)Activator.CreateInstance(type, BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { provider.GetRequiredService<IServiceScopeFactory>() }, null);
            Assert.Equal(1, await executor.ExecuteAsync<Probe, int>(p => Task.FromResult(1)));
            await Assert.ThrowsAsync<InvalidOperationException>(() => executor.ExecuteAsync<Probe, int>(p => throw new InvalidOperationException()));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => executor.ExecuteAsync<Probe, int>(p => Task.FromCanceled<int>(new CancellationToken(true))));
            Assert.Equal(3, created.Count); Assert.NotSame(created[0], created[1]); Assert.NotSame(created[1], created[2]);
            Assert.All(created, p => Assert.True(p.Disposed));
        }
    }
    [Fact] public void Composition_caches_configuration_and_factory()
    {
        var path = Path.Combine(Path.GetTempPath(), "steelog-f2-composition-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(path);
        try {
            File.WriteAllText(Path.Combine(path, "appsettings.json"), "{\"ConnectionStrings\":{\"DefaultConnection\":\"Host=localhost;Database=demo\"}}");
            var type = typeof(IUseCaseExecutor).Assembly.GetType("SteeLog.CompositionRoot.LocalServices", true);
            var services = Activator.CreateInstance(type, BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { path, path }, null);
            var credentials = type.GetProperty("Credentials", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(services);
            Assert.IsType<SteeLog.Core.Application.Identidad.OptionalCredentialStore>(credentials);
            var configuration = type.GetProperty("Configuration", BindingFlags.Instance | BindingFlags.NonPublic);
            var factory = type.GetProperty("Connections", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.Same(configuration.GetValue(services), configuration.GetValue(services));
            Assert.Same(factory.GetValue(services), factory.GetValue(services));
                        var provider = (ServiceProvider)type.GetField("provider", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(services);
            ((IDisposable)services).Dispose();
            Assert.Throws<ObjectDisposedException>(() => provider.CreateScope());
        } finally { Directory.Delete(path, true); }
    }
    sealed class Cached { public bool Disposed; }
    [Fact] public void Navigation_does_not_construct_when_cached_and_recreates_disposed()
    {
        var cache = new Dictionary<Type, Cached>(); int constructions = 0;
        Func<Cached> factory = () => { constructions++; return new Cached(); };
        var first = NavigationCache.GetOrCreate(cache, factory, item => item.Disposed); cache[typeof(Cached)] = first;
        var second = NavigationCache.GetOrCreate(cache, factory, item => item.Disposed);
        Assert.Same(first, second); Assert.Equal(1, constructions);
        first.Disposed = true; var third = NavigationCache.GetOrCreate(cache, factory, item => item.Disposed);
        Assert.NotSame(first, third); Assert.Equal(2, constructions);
    }
}
