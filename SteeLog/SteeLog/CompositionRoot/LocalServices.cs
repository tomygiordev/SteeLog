using System;
using SteeLog.Infrastructure.Configuration;
using SteeLog.Infrastructure.Persistence;
namespace SteeLog.CompositionRoot
{
    internal sealed class LocalServices
    {
        internal LocalPaths Paths { get; }
        internal LocalConfiguration Configuration { get; }
        internal PostgresConnectionFactory Connections { get; }
        internal LocalServices(string executableDirectory, string localApplicationData)
        {
            Paths = new LocalPaths(executableDirectory, localApplicationData);
            Configuration = LocalConfiguration.Load(Paths.ExecutableDirectory);
            Connections = new PostgresConnectionFactory(Configuration.ConnectionString);
        }
        internal static LocalServices Create() => new LocalServices(AppDomain.CurrentDomain.BaseDirectory, Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
    }
}
